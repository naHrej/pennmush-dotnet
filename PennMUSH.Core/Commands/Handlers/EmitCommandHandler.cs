namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public static class SpoofPrefix
{
    public static bool Omits(MUSHObject speaker, string commandName)
    {
        bool nospoofCommand = commandName.StartsWith("@ns", StringComparison.OrdinalIgnoreCase);
        return nospoofCommand && (speaker.IsWizard || speaker.HasPower("Can_spoof"));
    }

    public static string ForListener(ObjectDatabase db, MUSHObject speaker, MUSHObject listener, string message)
    {
        bool nospoof = Flagged(db, listener, "NOSPOOF");
        bool paranoid = Flagged(db, listener, "PARANOID");
        if (!nospoof)
            return message;
        if (listener.Id == speaker.Id && !paranoid)
            return message;
        string tag = paranoid
            ? $"[{speaker.Name}(#{speaker.Id.Value})] "
            : $"[{speaker.Name}:] ";
        return tag + message;
    }

    private static bool Flagged(ObjectDatabase db, MUSHObject obj, string flag)
    {
        if (obj.HasFlag(flag))
            return true;
        if (!obj.Owner.IsValid || obj.Owner == obj.Id)
            return false;
        var owner = db.GetObject(obj.Owner);
        return owner != null && owner.HasFlag(flag);
    }
}

public class EmitCommandHandler : ICommandHandler
{
    private readonly ExpressionEvaluator? _evaluator;

    public string Name => "@emit";

    public EmitCommandHandler(ExpressionEvaluator? evaluator = null)
    {
        _evaluator = evaluator;
    }

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

        string message = SpeechMod.Apply(db, _evaluator, context.Executor, context.Arguments, "|");
        context.NotifyRoom(execObj.Location, message, execObj.Id, !SpoofPrefix.Omits(execObj, context.CommandName));
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

        var speaker = db.GetObject(context.Executor);
        string message = context.RightArg;
        bool spoofable = speaker != null && !SpoofPrefix.Omits(speaker, context.CommandName);
        Dbref speakerId = speaker?.Id ?? default;
        bool prompt = context.CommandName.Contains("prompt", StringComparison.OrdinalIgnoreCase);

        if (target.Type == ObjectType.Room)
        {
            context.NotifyRoom(target.Id, message, speakerId, spoofable);
        }
        else
        {
            context.NotifyTarget(target.Id, message, prompt, speakerId, spoofable);
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

        var speaker = db.GetObject(context.Executor);
        string message = context.RightArg;
        bool spoofable = speaker != null && !SpoofPrefix.Omits(speaker, context.CommandName);
        context.NotifyRoom(room.Id, message, speaker?.Id ?? default, spoofable);
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

        context.NotifyExcept(execObj.Location, targetRef, context.RightArg, execObj.Id, !SpoofPrefix.Omits(execObj, context.CommandName));
        return true;
    }
}
