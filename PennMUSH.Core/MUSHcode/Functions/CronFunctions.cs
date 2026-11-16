namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Linq;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Scheduling;
using PennMUSH.Core.MUSHcode;

public static class CronFunctions
{
    private static CronScheduler? _scheduler;

    public static void Initialize(CronScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public static void Register(FunctionRegistry registry, CronScheduler? scheduler = null)
    {
        if (scheduler != null) registry.Scheduler = scheduler;

        registry.Register("CRON_LIST", (ctx, args) => CronList(registry, ctx, args));
        registry.Register("CRON_INFO", (ctx, args) => CronInfo(registry, ctx, args));
    }

    public static string CronList(EvaluationContext ctx, string[] args) => CronList(null, ctx, args);

    public static string CronList(FunctionRegistry? reg, EvaluationContext ctx, string[] args)
    {
        var scheduler = reg?.Scheduler ?? _scheduler;
        if (scheduler == null) return string.Empty;
        string pattern = args.Length > 0 ? args[0].Trim() : "*";

        var ids = scheduler.GetAllJobs()
            .Select(j => j.Id)
            .Where(id => PatternMatcher.Match(pattern, id, out _));

        return string.Join(" ", ids);
    }

    public static string CronInfo(EvaluationContext ctx, string[] args) => CronInfo(null, ctx, args);

    public static string CronInfo(FunctionRegistry? reg, EvaluationContext ctx, string[] args)
    {
        var scheduler = reg?.Scheduler ?? _scheduler;
        if (scheduler == null || args.Length == 0) return MushError.NoMatch.Text();
        string id = args[0].Trim();
        var job = scheduler.GetJob(id);
        if (job == null) return MushError.NoMatch.Text();

        string field = args.Length > 1 ? args[1].Trim().ToLowerInvariant() : "schedule";

        return field switch
        {
            "schedule" or "expr" => job.Expression,
            "command" or "cmd" => job.Command,
            "target" => job.Target.ToString(),
            "creator" => job.Creator.ToString(),
            "nextrun" or "next" => job.NextRun.ToUnixTimeSeconds().ToString(),
            "lastrun" or "last" => job.LastRun.HasValue ? job.LastRun.Value.ToUnixTimeSeconds().ToString() : "-1",
            "paused" => job.IsPaused ? "1" : "0",
            _ => job.Expression
        };
    }
}
