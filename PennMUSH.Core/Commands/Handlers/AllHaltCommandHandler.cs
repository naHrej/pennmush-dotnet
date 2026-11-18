namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class AllHaltCommandHandler : ICommandHandler
{
    private readonly ActionQueue _queue;

    public string Name => "@allhalt";

    public AllHaltCommandHandler(ActionQueue queue)
    {
        _queue = queue;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.IsWizard)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        int count = _queue.AllHalt();
        context.Notify(Feedback.HaltedAllProcessEsInTheQueue, ("value", $"{count}"));
        return true;
    }
}
