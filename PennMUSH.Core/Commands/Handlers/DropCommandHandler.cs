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
            context.Notify("Drop what?");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify("You are nowhere.");
            return false;
        }

        string targetName = context.Arguments.Trim();
        var targetObj = db.FindInContents(context.Executor, targetName);
        if (targetObj == null)
        {
            context.Notify("You don't have that.");
            return false;
        }

        if (!targetObj.EvalLock(LockType.Drop, context.Executor, db))
        {
            context.Notify("You can't seem to get rid of that.");
            return false;
        }

        var here = db.GetObject(execObj.Location);
        if (here != null && here.Type == ObjectType.Room && !here.EvalLock(LockType.Drop, context.Executor, db))
        {
            context.Notify("You can't seem to drop things here.");
            return false;
        }

        db.MoveObject(targetObj.Id, execObj.Location);
        context.Notify("Dropped.");
        context.NotifyExcept(execObj.Location, context.Executor, $"{execObj.Name} drops {targetObj.Name}.");
        return true;
    }
}
