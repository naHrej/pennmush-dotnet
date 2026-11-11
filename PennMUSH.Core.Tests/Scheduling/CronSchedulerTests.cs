namespace PennMUSH.Core.Tests.Scheduling;

using System;
using System.IO;
using Xunit;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.MUSHcode.Functions;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Scheduling;
using PennMUSH.Core.Types;

public class CronSchedulerTests
{
    private readonly ObjectDatabase _db;
    private readonly ActionQueue _queue;
    private readonly CronScheduler _scheduler;
    private readonly CommandDispatcher _dispatcher;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly MUSHObject _wizard;
    private readonly MUSHObject _mortal;

    public CronSchedulerTests()
    {
        _db = new ObjectDatabase();
        var room = _db.CreateObject("Room Zero", ObjectType.Room, Dbref.Nothing);
        _wizard = _db.CreateObject("God", ObjectType.Player, Dbref.Nothing);
        _wizard.Location = room.Id;
        _wizard.AddFlag("WIZARD");

        _mortal = _db.CreateObject("Mortal", ObjectType.Player, Dbref.Nothing);
        _mortal.Location = room.Id;

        _queue = new ActionQueue();
        _scheduler = new CronScheduler();

        _registry = new FunctionRegistry(_db);
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;
        _registry.Scheduler = _scheduler;
        CronFunctions.Initialize(_scheduler);

        _dispatcher = new CommandDispatcher(_evaluator);
        _dispatcher.Register(new CronCommandHandler(_scheduler, _queue));
    }

    [Fact]
    public void CronExpression_ParsesStandard5FieldsAndCalculatesNextOccurrence()
    {
        // Every 15 minutes: */15 * * * *
        Assert.True(CronExpression.TryParse("*/15 * * * *", out var cron, out string? error), error);
        Assert.NotNull(cron);

        var from = new DateTimeOffset(2026, 11, 4, 10, 7, 30, TimeSpan.Zero);
        var next = cron.GetNextOccurrence(from);

        Assert.Equal(10, next.Hour);
        Assert.Equal(15, next.Minute);
        Assert.Equal(0, next.Second);

        // Daily at midnight: 0 0 * * *
        var midnightCron = CronExpression.Parse("0 0 * * *");
        var nextMidnight = midnightCron.GetNextOccurrence(from);
        Assert.Equal(new DateTimeOffset(2026, 11, 5, 0, 0, 0, TimeSpan.Zero), nextMidnight);

        // Weekdays at noon: 0 12 * * 1-5
        var weekdayCron = CronExpression.Parse("0 12 * * 1-5");
        var nextWeekday = weekdayCron.GetNextOccurrence(from);
        Assert.Equal(12, nextWeekday.Hour);
        Assert.Equal(0, nextWeekday.Minute);
        Assert.True((int)nextWeekday.DayOfWeek >= 1 && (int)nextWeekday.DayOfWeek <= 5);
    }

    [Fact]
    public void CronScheduler_Lifecycle_AddPauseResumeAndProcessDueJobs()
    {
        var now = new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);
        bool added = _scheduler.AddOrUpdateJob("daily_cleanup", "0 0 * * *", _wizard.Id, _wizard.Id, "say cleaning", out string? error, false, now);
        Assert.True(added, error);

        var job = _scheduler.GetJob("daily_cleanup");
        Assert.NotNull(job);
        Assert.False(job.IsPaused);
        Assert.Equal(new DateTimeOffset(2026, 11, 5, 0, 0, 0, TimeSpan.Zero), job.NextRun);

        // Pause job
        Assert.True(_scheduler.PauseJob("daily_cleanup"));
        Assert.True(job.IsPaused);

        // When paused, ProcessDueJobs does not trigger
        var triggered1 = _scheduler.ProcessDueJobs(new DateTimeOffset(2026, 11, 5, 1, 0, 0, TimeSpan.Zero), _queue);
        Assert.Empty(triggered1);
        Assert.Equal(0, _queue.Count);

        // Resume job
        Assert.True(_scheduler.ResumeJob("daily_cleanup", now));
        Assert.False(job.IsPaused);

        // Trigger due job
        var triggered2 = _scheduler.ProcessDueJobs(new DateTimeOffset(2026, 11, 5, 0, 0, 0, TimeSpan.Zero), _queue);
        Assert.Single(triggered2);
        Assert.Equal(1, _queue.Count);
        Assert.NotNull(job.LastRun);
    }

    [Fact]
    public void CronCommand_WizardAdministration_PermissionsAndOperations()
    {
        // 1. Mortal cannot use @cron
        var ctx1 = _dispatcher.Dispatch(_mortal.Id, "@cron/add test=0 0 * * *,say test", _db);
        Assert.Contains("Permission denied.", ctx1.Notifications);

        // 2. Wizard adds cron job
        var ctx2 = _dispatcher.Dispatch(_wizard.Id, "@cron/add news_broadcast=0 12 * * *,+c Public=Daily news announcement!", _db);
        Assert.Contains(ctx2.Notifications, n => n.Contains("Cron job 'news_broadcast' added."));

        // 3. Wizard lists cron jobs
        var ctx3 = _dispatcher.Dispatch(_wizard.Id, "@cron/list", _db);
        Assert.Contains(ctx3.Notifications, n => n.Contains("news_broadcast"));

        // 4. Wizard runs job immediately
        var ctx4 = _dispatcher.Dispatch(_wizard.Id, "@cron/run news_broadcast", _db);
        Assert.Contains("Triggered cron job 'news_broadcast' immediately.", ctx4.Notifications);
        Assert.Equal(1, _queue.Count);

        // 5. Wizard deletes cron job
        var ctx5 = _dispatcher.Dispatch(_wizard.Id, "@cron/del news_broadcast", _db);
        Assert.Contains("Cron job 'news_broadcast' deleted.", ctx5.Notifications);
    }

    [Fact]
    public void CronFunctions_Introspection_WorksCorrectly()
    {
        _scheduler.AddOrUpdateJob("heartbeat", "*/5 * * * *", _wizard.Id, _wizard.Id, "say tick", out _);
        _scheduler.AddOrUpdateJob("hourly_chime", "0 * * * *", _wizard.Id, _wizard.Id, "say dong", out _);

        var ctx = new EvaluationContext(_wizard.Id, _wizard.Id);

        // cron_list()
        string list = _evaluator.Evaluate("[cron_list()]", ctx);
        Assert.Contains("heartbeat", list);
        Assert.Contains("hourly_chime", list);

        // cron_info(id, field)
        Assert.Equal("*/5 * * * *", _evaluator.Evaluate("[cron_info(heartbeat,schedule)]", ctx));
        Assert.Equal("say tick", _evaluator.Evaluate("[cron_info(heartbeat,command)]", ctx));
        Assert.Equal("0", _evaluator.Evaluate("[cron_info(heartbeat,paused)]", ctx));
    }

    [Fact]
    public void Cron_SqlitePersistence_SurvivesRestart()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"cron_test_{Guid.NewGuid():N}.db");
        string connStr = $"Data Source={dbPath}";

        try
        {
            var sqlite = new SqliteStore(connStr);
            var originalDb = new ObjectDatabase();
            WorldBootstrapper.GenerateBaseWorld(originalDb);

            var origScheduler = new CronScheduler();
            origScheduler.AddOrUpdateJob("backup_job", "0 3 * * *", new Dbref(1), new Dbref(1), "say nightly backup", out _);

            // Save to SQLite
            sqlite.Save(originalDb, origScheduler);

            // Load into fresh database and scheduler
            var loadedDb = new ObjectDatabase();
            var loadedScheduler = new CronScheduler();
            bool loaded = sqlite.Load(loadedDb, loadedScheduler);

            Assert.True(loaded);
            var loadedJob = loadedScheduler.GetJob("backup_job");
            Assert.NotNull(loadedJob);
            Assert.Equal("0 3 * * *", loadedJob.Expression);
            Assert.Equal("say nightly backup", loadedJob.Command);
            Assert.Equal(new Dbref(1), loadedJob.Creator);
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }
}
