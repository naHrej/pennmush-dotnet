namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class EmitCommandHandler : ICommandHandler
{
    public string Name => "@emit";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify("Emit what?");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify("You are nowhere.");
            return false;
        }

        context.NotifyRoom(execObj.Location, context.Arguments);
        return true;
    }
}

public class PemitCommandHandler : ICommandHandler
{
    public string Name => "@pemit";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify("Usage: @pemit <target>=<message>");
            return false;
        }

        var target = ResolveTarget(context.LeftArg, context.Executor, db);
        if (target == null)
        {
            context.Notify("I can't find that target.");
            return false;
        }

        if (target.Type == ObjectType.Room)
        {
            context.NotifyRoom(target.Id, context.RightArg);
        }
        else
        {
            bool prompt = context.CommandName.Equals("@prompt", StringComparison.OrdinalIgnoreCase);
            context.NotifyTarget(target.Id, context.RightArg, prompt);
        }

        if (!context.Switches.Contains("silent"))
        {
            context.Notify($"Message sent to {target.Name}.");
        }

        return true;
    }

    private static MUSHObject? ResolveTarget(string nameOrRef, Dbref executor, ObjectDatabase db)
    {
        string trimmed = nameOrRef.Trim();
        if (trimmed.Equals("me", StringComparison.OrdinalIgnoreCase))
            return db.GetObject(executor);

        if (trimmed.StartsWith('#') && int.TryParse(trimmed.Substring(1), out int id))
            return db.GetObject(new Dbref(id));

        return db.FindPlayer(trimmed) ?? db.FindObject(trimmed);
    }
}

public class RemitCommandHandler : ICommandHandler
{
    public string Name => "@remit";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify("Usage: @remit <room>=<message>");
            return false;
        }

        string roomStr = context.LeftArg.Trim();
        Dbref roomRef = Dbref.Nothing;
        if (roomStr.StartsWith('#') && int.TryParse(roomStr.Substring(1), out int id))
        {
            roomRef = new Dbref(id);
        }

        var room = db.GetObject(roomRef);
        if (room == null || room.Type != ObjectType.Room)
        {
            context.Notify("That's not a valid room.");
            return false;
        }

        context.NotifyRoom(room.Id, context.RightArg);
        context.Notify($"Message sent to {room.Name}.");
        return true;
    }
}

public class OemitCommandHandler : ICommandHandler
{
    public string Name => "@oemit";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify("Usage: @oemit <target>=<message>");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify("You are nowhere.");
            return false;
        }

        string targetStr = context.LeftArg.Trim();
        Dbref targetRef = Dbref.Nothing;
        if (targetStr.Equals("me", StringComparison.OrdinalIgnoreCase))
        {
            targetRef = context.Executor;
        }
        else if (targetStr.StartsWith('#') && int.TryParse(targetStr.Substring(1), out int id))
        {
            targetRef = new Dbref(id);
        }
        else
        {
            var match = db.FindInContents(execObj.Location, targetStr) ?? db.FindPlayer(targetStr);
            if (match != null) targetRef = match.Id;
        }

        if (!targetRef.IsValid)
        {
            context.Notify("I can't find that target.");
            return false;
        }

        context.NotifyExcept(execObj.Location, targetRef, context.RightArg);
        return true;
    }
}
