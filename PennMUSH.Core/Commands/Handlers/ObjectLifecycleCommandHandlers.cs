namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Linq;
using System.Text;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class DestroyCommandHandler : ICommandHandler
{
    public string Name => "@destroy";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify("Destroy what?");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        var target = ResolveTarget(context.Arguments.Trim(), context.Executor, db);
        if (target == null)
        {
            context.Notify("I can't find that object.");
            return false;
        }

        if (target.Id.Value <= 2)
        {
            context.Notify("You cannot destroy fundamental system objects (#0, #1, #2).");
            return false;
        }

        if (execObj == null || (!execObj.IsWizard && target.Owner != context.Executor))
        {
            context.Notify("Permission denied.");
            return false;
        }

        target.AddFlag("GOING");
        context.Notify($"The wrecking ball comes for {target.Name} ({target.Id}).");
        return true;
    }

    private static MUSHObject? ResolveTarget(string nameOrRef, Dbref executor, ObjectDatabase db)
    {
        if (nameOrRef.Equals("here", StringComparison.OrdinalIgnoreCase))
        {
            var exec = db.GetObject(executor);
            return exec != null && exec.Location.IsValid ? db.GetObject(exec.Location) : null;
        }
        if (nameOrRef.StartsWith('#') && int.TryParse(nameOrRef.Substring(1), out int id))
            return db.GetObject(new Dbref(id));
        return db.FindInContents(executor, nameOrRef) ?? db.FindObject(nameOrRef);
    }
}

public class UndestroyCommandHandler : ICommandHandler
{
    public string Name => "@undestroy";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify("Undestroy what?");
            return false;
        }

        var actor = db.GetObject(context.Executor);
        var target = ResolveTarget(context.Arguments.Trim(), context.Executor, db);
        if (target == null)
        {
            context.Notify("I can't find that object.");
            return false;
        }

        if (actor == null || (!actor.IsWizard && target.Owner != context.Executor))
        {
            context.Notify("Alas, your efforts of mercy are in vain.");
            return false;
        }

        if (!target.RemoveFlag("GOING"))
        {
            context.Notify("That can't be undestroyed.");
            return false;
        }

        string spared = $"{target.Name} ({target.Id}) has been spared from destruction.";
        var owner = db.GetObject(target.Owner);
        if (owner != null && owner.Id != context.Executor)
        {
            context.NotifyTarget(owner.Id, $"Your {spared}");
            context.Notify($"{owner.Name}'s {spared}");
        }
        else
        {
            context.Notify($"Your {spared}");
        }

        return true;
    }

    private static MUSHObject? ResolveTarget(string nameOrRef, Dbref executor, ObjectDatabase db)
    {
        if (nameOrRef.Equals("here", StringComparison.OrdinalIgnoreCase))
        {
            var exec = db.GetObject(executor);
            return exec != null && exec.Location.IsValid ? db.GetObject(exec.Location) : null;
        }
        if (nameOrRef.StartsWith('#') && int.TryParse(nameOrRef.Substring(1), out int id))
            return db.GetObject(new Dbref(id));
        return db.FindInContents(executor, nameOrRef) ?? db.FindObject(nameOrRef);
    }
}

public class CloneCommandHandler : ICommandHandler
{
    public string Name => "@clone";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify("Clone what?");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify("You are nowhere.");
            return false;
        }

        var target = db.FindInContents(context.Executor, context.Arguments.Trim()) ??
                     db.FindInContents(execObj.Location, context.Arguments.Trim()) ??
                     db.FindObject(context.Arguments.Trim());

        if (target == null)
        {
            context.Notify("I can't find that to clone.");
            return false;
        }

        if (target.Type == ObjectType.Player)
        {
            context.Notify("You cannot clone players.");
            return false;
        }

        var clone = db.CreateObject(target.Name, target.Type, context.Executor);
        clone.Location = execObj.Location;
        clone.Home = target.Home.IsValid ? target.Home : execObj.Location;

        // Copy flags
        foreach (var flag in target.Flags)
            clone.AddFlag(flag);

        // Copy attributes
        foreach (var attr in target.Attributes)
            clone.SetAttribute(attr.Name, attr.Value, context.Executor, attr.Flags);

        // Copy locks
        foreach (var l in target.Locks)
            clone.SetLock(l.Type, l.Key, context.Executor, l.Flags);

        context.Notify($"Cloned: {clone.Name} ({clone.Id}).");
        context.NotifyExcept(execObj.Location, context.Executor, $"{execObj.Name} creates {clone.Name}.");
        return true;
    }
}

public class ChownCommandHandler : ICommandHandler
{
    public string Name => "@chown";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify("Usage: @chown <object>=<player>");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        var target = db.FindObject(context.LeftArg.Trim());
        if (target == null)
        {
            context.Notify("I can't find that object.");
            return false;
        }

        var newOwner = db.FindPlayer(context.RightArg.Trim());
        if (newOwner == null)
        {
            context.Notify("I can't find that player.");
            return false;
        }

        if (execObj == null || (!execObj.IsWizard && target.Owner != context.Executor))
        {
            context.Notify("Permission denied.");
            return false;
        }

        target.Owner = newOwner.Id;
        context.Notify($"Owner of {target.Name} ({target.Id}) changed to {newOwner.Name}.");
        return true;
    }
}

public class WipeCommandHandler : ICommandHandler
{
    public string Name => "@wipe";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify("Usage: @wipe <object>[/<pattern>]");
            return false;
        }

        string raw = context.Arguments.Trim();
        string objStr = raw;
        string pattern = "*";

        int slash = raw.IndexOf('/');
        if (slash != -1)
        {
            objStr = raw.Substring(0, slash).Trim();
            pattern = raw.Substring(slash + 1).Trim();
        }

        var target = db.FindObject(objStr);
        if (target == null)
        {
            context.Notify("I can't find that object.");
            return false;
        }

        var toWipe = target.Attributes
            .Where(a => PatternMatcher.Match(pattern, a.Name, out _))
            .Select(a => a.Name)
            .ToList();

        foreach (var attr in toWipe)
        {
            target.ClearAttribute(attr);
        }

        context.Notify($"Wiped {toWipe.Count} attribute(s) on {target.Name}.");
        return true;
    }
}

public class LockCommandHandler : ICommandHandler
{
    public string Name => "@lock";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify("Usage: @lock[/<type>] <object>=<key>");
            return false;
        }

        var target = db.FindObject(context.LeftArg.Trim());
        if (target == null)
        {
            context.Notify("I can't find that object.");
            return false;
        }

        string lockType = LockCommands.LockCommandType(context);
        var lockNode = LockParser.Parse(context.RightArg.Trim());

        target.SetLock(lockType, lockNode, context.Executor);
        context.Notify($"Locked ({lockType}) on {target.Name}.");
        return true;
    }
}

public class UnlockCommandHandler : ICommandHandler
{
    public string Name => "@unlock";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify("Usage: @unlock[/<type>] <object>");
            return false;
        }

        var target = db.FindObject(context.Arguments.Trim());
        if (target == null)
        {
            context.Notify("I can't find that object.");
            return false;
        }

        string lockType = LockCommands.LockCommandType(context);
        if (target.ClearLock(lockType))
        {
            context.Notify($"Unlocked ({lockType}) on {target.Name}.");
            return true;
        }

        context.Notify($"No such lock ({lockType}) set.");
        return false;
    }
}

public class StatsCommandHandler : ICommandHandler
{
    public string Name => "@stats";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        int players = 0, rooms = 0, exits = 0, things = 0, garbage = 0;
        foreach (var obj in db.GetAllObjects())
        {
            if (obj.HasFlag("GOING")) { garbage++; continue; }
            switch (obj.Type)
            {
                case ObjectType.Player: players++; break;
                case ObjectType.Room: rooms++; break;
                case ObjectType.Exit: exits++; break;
                case ObjectType.Thing: things++; break;
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Total objects: {db.Count}");
        sb.AppendLine($"  Players: {players,-6} Rooms: {rooms,-6} Exits: {exits,-6} Things: {things,-6} Garbage: {garbage}");
        context.Notify(sb.ToString().TrimEnd());
        return true;
    }
}

public class ScoreCommandHandler : ICommandHandler
{
    public string Name => "score";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var execObj = db.GetObject(context.Executor);
        if (execObj == null) return false;

        context.Notify($"You have {execObj.Pennies} pennies.");
        return true;
    }
}

public class DoingCommandHandler : ICommandHandler
{
    public string Name => "doing";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var execObj = db.GetObject(context.Executor);
        if (execObj == null) return false;

        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            var doing = execObj.GetAttribute("DOING");
            context.Notify(doing != null ? $"Your DOING is: {doing.Value}" : "You have no DOING set.");
            return true;
        }

        execObj.SetAttribute("DOING", context.Arguments.Trim(), context.Executor);
        context.Notify($"DOING set to: {context.Arguments.Trim()}");
        return true;
    }
}

public class VersionCommandHandler : ICommandHandler
{
    public string Name => "@version";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        context.Notify("PennMUSH .NET (C# 13 / .NET 10)");
        context.Notify("Modern virtual world engine with C3 DAG inheritance, async softcode, and cron scheduler.");
        context.Notify("Feature-parity reimplementation of PennMUSH 1.8+.");
        return true;
    }
}

internal static class LockCommands
{
    public static string LockCommandType(CommandContext context)
    {
        if (context.CommandName.Equals("@elock", StringComparison.OrdinalIgnoreCase)
            || context.CommandName.Equals("@eunlock", StringComparison.OrdinalIgnoreCase))
            return LockType.Enter;
        if (context.CommandName.Equals("@ulock", StringComparison.OrdinalIgnoreCase)
            || context.CommandName.Equals("@uunlock", StringComparison.OrdinalIgnoreCase))
            return LockType.Use;
        return context.Switches.FirstOrDefault() ?? "Default";
    }
}
