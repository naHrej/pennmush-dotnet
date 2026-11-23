namespace PennMUSH.Core.Scheduling;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class CronScheduler
{
    private readonly ConcurrentDictionary<string, CronJob> _jobs = new(StringComparer.OrdinalIgnoreCase);

    public Action? Changed { get; set; }

    private void Touch() => Changed?.Invoke();

    public bool AddOrUpdateJob(string id, string expression, Dbref creator, Dbref target, string command, out string? error, bool paused = false, DateTimeOffset? now = null)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(id))
        {
            error = "Job ID cannot be empty.";
            return false;
        }

        if (!CronExpression.TryParse(expression, out var parsedExpr, out error))
        {
            return false;
        }

        var baseTime = now ?? DateTimeOffset.UtcNow;
        var nextRun = parsedExpr!.GetNextOccurrence(baseTime);

        var job = new CronJob(id.Trim(), expression.Trim(), parsedExpr, creator, target, command, nextRun)
        {
            IsPaused = paused
        };

        _jobs[id.Trim()] = job;
        Touch();
        return true;
    }

    public bool DeleteJob(string id)
    {
        if (!_jobs.TryRemove(id.Trim(), out _))
            return false;
        Touch();
        return true;
    }

    public CronJob? GetJob(string id)
    {
        return _jobs.TryGetValue(id.Trim(), out var job) ? job : null;
    }

    public IReadOnlyList<CronJob> GetAllJobs()
    {
        return _jobs.Values.OrderBy(j => j.NextRun).ToList();
    }

    public bool PauseJob(string id)
    {
        var job = GetJob(id);
        if (job == null) return false;
        job.IsPaused = true;
        Touch();
        return true;
    }

    public bool ResumeJob(string id, DateTimeOffset? now = null)
    {
        var job = GetJob(id);
        if (job == null) return false;
        job.IsPaused = false;
        var baseTime = now ?? DateTimeOffset.UtcNow;
        job.NextRun = job.ParsedExpression.GetNextOccurrence(baseTime);
        Touch();
        return true;
    }

    public List<CronJob> ProcessDueJobs(DateTimeOffset now, ActionQueue queue)
    {
        var triggered = new List<CronJob>();

        foreach (var job in _jobs.Values)
        {
            if (!job.IsPaused && job.NextRun <= now)
            {
                queue.Enqueue(job.Target, job.Creator, job.Command);
                job.LastRun = now;
                job.NextRun = job.ParsedExpression.GetNextOccurrence(now);
                triggered.Add(job);
            }
        }

        if (triggered.Count > 0)
            Touch();
        return triggered;
    }
}
