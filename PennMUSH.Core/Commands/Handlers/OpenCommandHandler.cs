namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class OpenCommandHandler : ICommandHandler
{
    public string Name => "@open";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg))
        {
            context.Notify(Feedback.OpenWhat);
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify(Feedback.YouCannotOpenExitsFromHere);
            return false;
        }

        string exitName = context.LeftArg.Trim();
        var exitObj = db.CreateObject(exitName, ObjectType.Exit, context.Executor);
        db.AttachExit(execObj.Location, exitObj.Id);

        if (!string.IsNullOrWhiteSpace(context.RightArg))
        {
            Dbref destRef = ResolveDbref(context.RightArg, context.Executor);
            var destObj = db.GetObject(destRef);
            if (destObj != null)
            {
                db.LinkExit(exitObj.Id, destRef);
                context.Notify(Feedback.OpenedAndLinkedTo, ("value", $"{destObj.Name}"), ("value2", $"{destObj.Id}"));
                return true;
            }
        }

        context.Notify(Feedback.Opened);
        return true;
    }

    private static Dbref ResolveDbref(string s, Dbref executor)
    {
        if (s.Equals("me", StringComparison.OrdinalIgnoreCase)) return executor;
        if (s.StartsWith('#') && int.TryParse(s.Substring(1), out int id)) return new Dbref(id);
        if (int.TryParse(s, out int plainId)) return new Dbref(plainId);
        return Dbref.Nothing;
    }
}
