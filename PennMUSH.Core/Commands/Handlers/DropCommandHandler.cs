namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class DropCommandHandler : ICommandHandler
{
    public string Name => "drop";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify(Feedback.DropWhat);
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify(Feedback.YouAreNowhere);
            return false;
        }

        string targetName = context.Arguments.Trim();
        var targetObj = db.FindInContents(context.Executor, targetName);
        if (targetObj == null)
        {
            context.Notify(Feedback.YouDontHaveThat);
            return false;
        }

        if (!targetObj.EvalLock(LockType.Drop, context.Executor, db))
        {
            context.Notify(Feedback.YouCantSeemToGetRidOfThat);
            return false;
        }

        var here = db.GetObject(execObj.Location);
        if (here != null && here.Type == ObjectType.Room && !here.EvalLock(LockType.Drop, context.Executor, db))
        {
            context.Notify(Feedback.YouCantSeemToDropThingsHere);
            return false;
        }

        db.MoveObject(targetObj.Id, execObj.Location);
        context.Notify(Feedback.Dropped);
        context.NotifyExcept(execObj.Location, context.Executor, Feedback.ValueDropsValue.Text(("value", $"{execObj.Name}"), ("value2", $"{targetObj.Name}")));
        return true;
    }
}
