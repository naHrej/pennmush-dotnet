namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Linq;
using PennMUSH.Core.Database;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Scheduling;
using PennMUSH.Core.Types;

public class CronCommandHandler : ICommandHandler
{
    private readonly CronScheduler _scheduler;
    private readonly ActionQueue _queue;

    public string Name => "@cron";

    public CronCommandHandler(CronScheduler scheduler, ActionQueue queue)
    {
        _scheduler = scheduler;
        _queue = queue;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var executor = db.GetObject(context.Executor);
        if (executor == null) return false;

        // Permissions: Wizard or Royalty to manage cron jobs
        if (!executor.IsWizard && !executor.IsGod && !executor.HasFlag("ROYALTY"))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        // 1. @cron/add <id>=<expr>,<cmd_or_attr>
        if (context.Switches.Contains("add") || context.Switches.Contains("set"))
        {
            string id = context.LeftArg.Trim();
            string right = context.RightArg;

            int comma = right.IndexOf(',');
            if (string.IsNullOrEmpty(id) || comma <= 0)
            {
                context.Notify(Feedback.UsageCronAddIdCronExpressionCommandOrTargetAttr);
                return false;
            }

            string expression = right.Substring(0, comma).Trim();
            string action = right.Substring(comma + 1).Trim();

            Dbref target = executor.Id;
            string commandToRun = action;

            // Check if action is in format <#dbref>/<ATTR>
            int slash = action.IndexOf('/');
            if (slash > 0 && action.StartsWith('#'))
            {
                string targetStr = action.Substring(0, slash);
                string attrName = action.Substring(slash + 1).Trim();
                if (Dbref.TryParse(targetStr, out var parsedRef) && parsedRef.IsValid)
                {
                    target = parsedRef;
                    commandToRun = $"@trigger {targetStr}/{attrName}";
                }
            }

            if (_scheduler.AddOrUpdateJob(id, expression, executor.Id, target, commandToRun, out string? error))
            {
                var job = _scheduler.GetJob(id)!;
                context.Notify($"Cron job '{id}' added. Next run at: {job.NextRun:yyyy-MM-dd HH:mm:ss} UTC.");
                return true;
            }

            context.Notify(Feedback.ErrorCreatingCronJob, ("job", $"{error}"));
            return false;
        }

        // 2. @cron/del or @cron/delete
        if (context.Switches.Contains("del") || context.Switches.Contains("delete"))
        {
            string id = context.Arguments.Trim();
            if (_scheduler.DeleteJob(id))
            {
                context.Notify(Feedback.CronJobDeleted, ("job", $"{id}"));
                return true;
            }
            context.Notify(Feedback.CronJobNotFound, ("job", $"{id}"));
            return false;
        }

        // 3. @cron/pause
        if (context.Switches.Contains("pause"))
        {
            string id = context.Arguments.Trim();
            if (_scheduler.PauseJob(id))
            {
                context.Notify(Feedback.CronJobPaused, ("job", $"{id}"));
                return true;
            }
            context.Notify(Feedback.CronJobNotFound, ("job", $"{id}"));
            return false;
        }

        // 4. @cron/resume
        if (context.Switches.Contains("resume"))
        {
            string id = context.Arguments.Trim();
            if (_scheduler.ResumeJob(id))
            {
                var job = _scheduler.GetJob(id)!;
                context.Notify($"Cron job '{id}' resumed. Next run at: {job.NextRun:yyyy-MM-dd HH:mm:ss} UTC.");
                return true;
            }
            context.Notify(Feedback.CronJobNotFound, ("job", $"{id}"));
            return false;
        }

        // 5. @cron/run (run immediately)
        if (context.Switches.Contains("run"))
        {
            string id = context.Arguments.Trim();
            var job = _scheduler.GetJob(id);
            if (job == null)
            {
                context.Notify(Feedback.CronJobNotFound, ("job", $"{id}"));
                return false;
            }

            _queue.Enqueue(job.Target, job.Creator, job.Command);
            job.LastRun = DateTimeOffset.UtcNow;
            context.Notify(Feedback.TriggeredCronJobImmediately, ("job", $"{id}"));
            return true;
        }

        // 6. Default or @cron/list: list all jobs
        var jobs = _scheduler.GetAllJobs();
        if (jobs.Count == 0)
        {
            context.Notify(Feedback.NoCronJobsConfigured);
            return true;
        }

        context.Notify(Feedback.JobIdScheduleStatusTargetNextRunUtc);
        context.Notify(Feedback.Dashes70);
        foreach (var job in jobs)
        {
            string status = job.IsPaused ? "PAUSED" : "ACTIVE";
            string next = job.IsPaused ? "---" : job.NextRun.ToString("yyyy-MM-dd HH:mm");
            context.Notify($"{job.Id,-15} {job.Expression,-15} {status,-7} {job.Target,-6} {next}");
        }
        context.Notify(Feedback.Dashes70);

        return true;
    }
}
