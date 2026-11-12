namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class GetCommandHandler : ICommandHandler
{
    public string Name => "get";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify("Get what?");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify("You are nowhere.");
            return false;
        }

        string targetName = context.Arguments.Trim();
        var targetObj = db.FindInContents(execObj.Location, targetName);
        if (targetObj == null)
        {
            context.Notify("I can't find that here.");
            return false;
        }

        if (targetObj.Type != ObjectType.Thing)
        {
            context.Notify("You can't pick that up.");
            return false;
        }

        if (!targetObj.EvalLock(LockType.Basic, context.Executor, db))
        {
            context.Notify("You can't pick that up.");
            return false;
        }

        db.MoveObject(targetObj.Id, context.Executor);
        context.Notify("Taken.");
        context.NotifyExcept(execObj.Location, context.Executor, $"{execObj.Name} takes {targetObj.Name}.");
        return true;
    }
}
