namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Types;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;

public class TeleportCommandHandler : ICommandHandler
{
    private readonly LookCommandHandler? _lookHandler;

    public string Name => "@teleport";

    public TeleportCommandHandler(LookCommandHandler? lookHandler = null)
    {
        _lookHandler = lookHandler;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        string targetStr;
        string destStr;

        if (string.IsNullOrWhiteSpace(context.RightArg))
        {
            targetStr = "me";
            destStr = context.LeftArg;
        }
        else
        {
            targetStr = context.LeftArg;
            destStr = context.RightArg;
        }

        if (string.IsNullOrWhiteSpace(destStr))
        {
            context.Notify(Feedback.WhereDoYouWantToTeleportTo);
            return false;
        }

        var target = db.MatchObject(context.Executor, targetStr.Trim());
        var dest = db.MatchObject(context.Executor, destStr.Trim());

        if (target == null || dest == null)
        {
            context.Notify(Feedback.ICantFindThat);
            return false;
        }

        db.MoveObject(target.Id, dest.Id);
        context.Notify(Feedback.Teleported);
        if (target.Type == ObjectType.Player)
            _lookHandler?.ShowHere(context, db, target.Id, LookKind.Automatic);
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