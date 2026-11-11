namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class WaitCommandHandler : ICommandHandler
{
    private readonly ActionQueue _queue;

    public string Name => "@wait";

    public WaitCommandHandler(ActionQueue queue)
    {
        _queue = queue;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (context.CommandName.Equals("@drain", StringComparison.OrdinalIgnoreCase))
        {
            string sem = string.IsNullOrWhiteSpace(context.LeftArg) ? context.Arguments.Trim() : context.LeftArg.Trim();
            if (string.IsNullOrWhiteSpace(sem))
            {
                context.Notify("Drain what semaphore?");
                return false;
            }

            int count = _queue.DrainSemaphore(sem);
            context.Notify($"Drained {count} process(es) on semaphore '{sem}'.");
            return true;
        }

        if (context.CommandName.Equals("@notify", StringComparison.OrdinalIgnoreCase))
        {
            string sem = string.IsNullOrWhiteSpace(context.LeftArg) ? context.Arguments.Trim() : context.LeftArg.Trim();
            if (string.IsNullOrWhiteSpace(sem))
            {
                context.Notify("Notify what semaphore?");
                return false;
            }

            int count = _queue.NotifySemaphore(sem);
            context.Notify($"Notified {count} process(es) on semaphore '{sem}'.");
            return true;
        }

        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify("What do you want to wait on?");
            return false;
        }

        string left = context.LeftArg.Trim();
        string commandToRun = context.RightArg;

        if (double.TryParse(left, out double seconds))
        {
            var delay = TimeSpan.FromSeconds(Math.Max(0, seconds));
            var entry = _queue.Enqueue(context.Executor, context.Enactor, commandToRun, delay);
            if (entry == null)
            {
                context.Notify("Queue limit exceeded.");
                return false;
            }
            context.Notify($"Queued with PID {entry.Pid}.");
            return true;
        }
        else
        {
            // Semaphore wait
            var entry = _queue.Enqueue(context.Executor, context.Enactor, commandToRun, semaphore: left);
            if (entry == null)
            {
                context.Notify("Queue limit exceeded.");
                return false;
            }
            context.Notify($"Queued on semaphore '{left}' with PID {entry.Pid}.");
            return true;
        }
    }
}
