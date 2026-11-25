namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class LinkCommandHandler : ICommandHandler
{
    public string Name => "@link";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg))
        {
            context.Notify(Feedback.LinkWhat);
            return false;
        }

        Dbref exitRef = ResolveDbref(context.LeftArg, context.Executor);
        var exitObj = db.GetObject(exitRef);
        if (exitObj == null || exitObj.Type != ObjectType.Exit)
        {
            context.Notify(Feedback.ICantSeeThatExit);
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || (!execObj.IsWizard && execObj.Id != exitObj.Owner))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        if (string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify(Feedback.LinkItToWhat);
            return false;
        }

        Dbref destRef = ResolveDbref(context.RightArg, context.Executor);
        var destObj = db.GetObject(destRef);
        if (destObj == null)
        {
            context.Notify(Feedback.ICantSeeThatDestination);
            return false;
        }
        if (!db.LinkToObject && destObj.Type != ObjectType.Room)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        db.LinkExit(exitObj.Id, destRef);
        context.Notify(Feedback.Linked);
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
