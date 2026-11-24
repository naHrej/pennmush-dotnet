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
                context.Notify(Feedback.DrainWhatSemaphore);
                return false;
            }

            int count = _queue.DrainSemaphore(sem);
            context.Notify(Feedback.DrainedProcessEsOnSemaphore, ("value", $"{count}"), ("value2", $"{sem}"));
            return true;
        }

        if (context.CommandName.Equals("@notify", StringComparison.OrdinalIgnoreCase))
        {
            string sem = string.IsNullOrWhiteSpace(context.LeftArg) ? context.Arguments.Trim() : context.LeftArg.Trim();
            if (string.IsNullOrWhiteSpace(sem))
            {
                context.Notify(Feedback.NotifyWhatSemaphore);
                return false;
            }

            int count = _queue.NotifySemaphore(sem);
            context.Notify(Feedback.NotifiedProcessEsOnSemaphore, ("value", $"{count}"), ("value2", $"{sem}"));
            return true;
        }

        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify(Feedback.WhatDoYouWantToWaitOn);
            return false;
        }

        string left = context.LeftArg.Trim();
        string commandToRun = context.RightArg;

        if (double.TryParse(left, out double seconds))
        {
            var delay = TimeSpan.FromSeconds(Math.Max(0, seconds));
            var entry = _queue.Enqueue(context.Executor, context.Enactor, commandToRun, delay, args: context.PositionalArgs, registers: context.Registers);
            if (entry == null)
            {
                context.Notify(Feedback.QueueLimitExceeded);
                return false;
            }
            return true;
        }
        else
        {
            // Semaphore wait
            var entry = _queue.Enqueue(context.Executor, context.Enactor, commandToRun, semaphore: left, args: context.PositionalArgs, registers: context.Registers);
            if (entry == null)
            {
                context.Notify(Feedback.QueueLimitExceeded);
                return false;
            }
            return true;
        }
    }
}
