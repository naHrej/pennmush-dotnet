namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Linq;
using PennMUSH.Core.Database;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class PsCommandHandler : ICommandHandler
{
    private readonly ActionQueue _queue;

    public string Name => "@ps";

    public PsCommandHandler(ActionQueue queue) => _queue = queue;

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
        {
            context.Notify("Permission denied.");
            return false;
        }

        bool all = context.Switches.Contains("all");
        if (all && !actor.IsWizard)
        {
            context.Notify("Permission denied.");
            return false;
        }

        var entries = _queue.Snapshot();
        string arg = (context.Arguments ?? string.Empty).Trim();
        if (arg.Length > 0)
        {
            if (!int.TryParse(arg, out int pid))
            {
                context.Notify("That is not a valid pid!");
                return false;
            }

            var entry = entries.FirstOrDefault(item => item.Pid == pid);
            if (entry == null)
            {
                context.Notify("That is not a valid pid!");
                return false;
            }

            if (!actor.IsWizard && entry.Executor != actor.Id)
            {
                context.Notify("Permission denied.");
                return false;
            }

            context.Notify(Format(db, entry));
            return true;
        }

        if (!all)
            entries = entries.Where(item => item.Executor == actor.Id).ToList();

        if (context.Switches.Contains("count"))
        {
            context.Notify($"Queue entries: {entries.Count}");
            return true;
        }

        var now = DateTimeOffset.UtcNow;
        int commands = entries.Count(item => string.IsNullOrEmpty(item.Semaphore) && item.ScheduledTime <= now);
        int waits = entries.Count(item => string.IsNullOrEmpty(item.Semaphore) && item.ScheduledTime > now);
        int semaphores = entries.Count(item => !string.IsNullOrEmpty(item.Semaphore));
        string totals = $"Command: {commands}  Wait: {waits}  Semaphore: {semaphores}  Total: {entries.Count}";
        if (context.Switches.Contains("summary"))
        {
            context.Notify(totals);
            return true;
        }

        context.Notify(all ? "Queue for all:" : $"Queue for {actor.Name}:");
        if (entries.Count == 0)
            context.Notify("(empty)");
        else
        {
            foreach (var entry in entries.OrderBy(item => item.Pid))
                context.Notify(Format(db, entry));
        }

        context.Notify(totals);
        return true;
    }

    internal static string Format(ObjectDatabase db, QueueEntry entry)
    {
        var executor = db.GetObject(entry.Executor);
        string name = executor?.Name ?? $"#{entry.Executor.Value}";
        string when;
        if (!string.IsNullOrEmpty(entry.Semaphore))
            when = $"semaphore:{entry.Semaphore}";
        else
        {
            var left = entry.ScheduledTime - DateTimeOffset.UtcNow;
            when = left <= TimeSpan.Zero ? "ready" : $"{Math.Ceiling(left.TotalSeconds)}s";
        }

        return $"#{entry.Pid}  {name}  {when}  {entry.Command}";
    }
}

public class KickCommandHandler : ICommandHandler
{
    private readonly ActionQueue _queue;
    private readonly CommandDispatcher _dispatcher;

    public string Name => "@kick";

    public KickCommandHandler(ActionQueue queue, CommandDispatcher dispatcher)
    {
        _queue = queue;
        _dispatcher = dispatcher;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.IsWizard)
        {
            context.Notify("Permission denied.");
            return false;
        }

        string arg = (context.Arguments ?? string.Empty).Trim();
        int count = 1;
        if (arg.Length > 0 && (!int.TryParse(arg, out count) || count < 1))
        {
            context.Notify("Usage: @kick <count>");
            return false;
        }

        var results = _queue.Kick(count, _dispatcher, db);
        if (results.Count == 0)
        {
            context.Notify("Nothing to kick.");
            return true;
        }

        context.Notify($"Kicked {results.Count} command{(results.Count == 1 ? "" : "s")}.");
        foreach (var result in results)
        {
            foreach (var note in result.Notifications)
                context.Notifications.Add(note);
            foreach (var note in result.TargetedNotifications)
                context.TargetedNotifications.Add(note);
        }

        return true;
    }
}
