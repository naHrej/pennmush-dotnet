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
            context.Notify(Feedback.GetWhat);
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify(Feedback.YouAreNowhere);
            return false;
        }

        string targetName = context.Arguments.Trim();
        var targetObj = db.FindInContents(execObj.Location, targetName);
        if (targetObj == null)
        {
            context.Notify(Feedback.ICantFindThatHere);
            return false;
        }

        if (targetObj.Type != ObjectType.Thing)
        {
            context.Notify(Feedback.YouCantPickThatUp);
            return false;
        }

        if (!targetObj.EvalLock(LockType.Basic, context.Executor, db))
        {
            context.Notify(Feedback.YouCantPickThatUp);
            return false;
        }

        db.MoveObject(targetObj.Id, context.Executor);
        context.Notify(Feedback.Taken);
        context.NotifyExcept(execObj.Location, context.Executor, Feedback.ValueTakesValue.Text(("value", $"{execObj.Name}"), ("value2", $"{targetObj.Name}")));
        return true;
    }
}
