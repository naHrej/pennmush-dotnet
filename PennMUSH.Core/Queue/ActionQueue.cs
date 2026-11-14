namespace PennMUSH.Core.Queue;

using System;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;

public class ActionQueue
{
    private readonly List<QueueEntry> _entries = new();
    private readonly object _lock = new();
    private int _nextPid = 1;
    public int MaxQueueEntriesPerPlayer { get; set; } = 500;

    public int Count
    {
        get
        {
            lock (_lock) return _entries.Count;
        }
    }

    public QueueEntry? Enqueue(Dbref executor, Dbref enactor, string command, TimeSpan delay = default, string? semaphore = null, string[]? args = null, IDictionary<string, string>? registers = null)
    {
        lock (_lock)
        {
            if (MaxQueueEntriesPerPlayer > 0 && _entries.Count(e => e.Executor == executor) >= MaxQueueEntriesPerPlayer)
            {
                return null;
            }

            var entry = new QueueEntry
            {
                Pid = _nextPid++,
                Executor = executor,
                Enactor = enactor,
                Command = command,
                ScheduledTime = DateTimeOffset.UtcNow.Add(delay),
                Semaphore = string.IsNullOrWhiteSpace(semaphore) ? null : semaphore.Trim(),
                Arguments = args ?? Array.Empty<string>(),
                Registers = registers != null ? new Dictionary<string, string>(registers, StringComparer.OrdinalIgnoreCase) : new(StringComparer.OrdinalIgnoreCase)
            };
            _entries.Add(entry);
            return entry;
        }
    }

    public int Halt(Dbref target)
    {
        lock (_lock)
        {
            return _entries.RemoveAll(e => e.Executor == target);
        }
    }

    public int AllHalt()
    {
        lock (_lock)
        {
            int count = _entries.Count;
            _entries.Clear();
            return count;
        }
    }

    public int DrainSemaphore(string semaphore)
    {
        if (string.IsNullOrWhiteSpace(semaphore)) return 0;
        string sem = semaphore.Trim();
        lock (_lock)
        {
            return _entries.RemoveAll(e => !string.IsNullOrEmpty(e.Semaphore) && e.Semaphore.Equals(sem, StringComparison.OrdinalIgnoreCase));
        }
    }

    public int NotifySemaphore(string semaphore)
    {
        if (string.IsNullOrWhiteSpace(semaphore)) return 0;
        string sem = semaphore.Trim();

        lock (_lock)
        {
            var matched = _entries.FirstOrDefault(e => !string.IsNullOrEmpty(e.Semaphore) && e.Semaphore.Equals(sem, StringComparison.OrdinalIgnoreCase));
            if (matched != null)
            {
                int index = _entries.IndexOf(matched);
                if (index != -1)
                {
                    _entries[index] = new QueueEntry
                    {
                        Pid = matched.Pid,
                        Executor = matched.Executor,
                        Enactor = matched.Enactor,
                        Command = matched.Command,
                        ScheduledTime = DateTimeOffset.UtcNow.AddSeconds(-1),
                        Semaphore = null,
                        Arguments = matched.Arguments,
                        Registers = matched.Registers
                    };
                    return 1;
                }
            }
            return 0;
        }
    }

    public List<CommandContext> ProcessReady(DateTimeOffset now, CommandDispatcher dispatcher, ObjectDatabase db)
    {
        List<QueueEntry> ready;
        lock (_lock)
        {
            ready = _entries.Where(e => string.IsNullOrEmpty(e.Semaphore) && e.ScheduledTime <= now).OrderBy(e => e.ScheduledTime).ToList();
            foreach (var r in ready)
                _entries.Remove(r);
        }

        var results = new List<CommandContext>();
        foreach (var entry in ready)
        {
            var ctx = dispatcher.Dispatch(entry.Executor, entry.Command, db);
            results.Add(ctx);
        }

        return results;
    }

    public IReadOnlyList<QueueEntry> Snapshot()
    {
        lock (_lock)
            return _entries.ToList();
    }

    public List<CommandContext> Kick(int count, CommandDispatcher dispatcher, ObjectDatabase db)
    {
        List<QueueEntry> chosen;
        lock (_lock)
        {
            chosen = _entries
                .Where(entry => string.IsNullOrEmpty(entry.Semaphore))
                .OrderBy(entry => entry.ScheduledTime)
                .ThenBy(entry => entry.Pid)
                .Take(Math.Max(0, count))
                .ToList();
            foreach (var entry in chosen)
                _entries.Remove(entry);
        }

        var results = new List<CommandContext>();
        foreach (var entry in chosen)
            results.Add(dispatcher.Dispatch(entry.Executor, entry.Command, db));
        return results;
    }
}
