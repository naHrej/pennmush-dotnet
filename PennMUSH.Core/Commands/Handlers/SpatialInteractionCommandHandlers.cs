namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Text;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class HomeCommandHandler : ICommandHandler
{
    private readonly LookCommandHandler? _lookHandler;

    public string Name => "home";

    public HomeCommandHandler(LookCommandHandler? lookHandler = null)
    {
        _lookHandler = lookHandler;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var execObj = db.GetObject(context.Executor);
        if (execObj == null) return false;

        Dbref homeRef = execObj.Home.IsValid ? execObj.Home : new Dbref(0);
        if (execObj.Location == homeRef)
        {
            context.Notify("You are already home.");
            return true;
        }

        Dbref oldLoc = execObj.Location;
        if (oldLoc.IsValid)
        {
            context.NotifyExcept(oldLoc, context.Executor, $"{execObj.Name} goes home.");
        }

        db.MoveObject(context.Executor, homeRef);
        context.Notify("There's no place like home...");
        context.NotifyExcept(homeRef, context.Executor, $"{execObj.Name} arrives from home.");

        if (_lookHandler != null)
        {
            var lookCtx = new CommandContext { Executor = context.Executor, Enactor = context.Enactor };
            _lookHandler.Execute(lookCtx, db);
            foreach (var note in lookCtx.TargetedNotifications) context.TargetedNotifications.Add(note);
            foreach (var note in lookCtx.Notifications) context.Notifications.Add(note);
        }

        return true;
    }
}

public class EnterCommandHandler : ICommandHandler
{
    public string Name => "enter";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify("Enter what?");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify("You are nowhere.");
            return false;
        }

        var target = db.FindInContents(execObj.Location, context.Arguments.Trim());
        if (target == null)
        {
            context.Notify("I don't see that here to enter.");
            return false;
        }

        if (target.Id == context.Executor)
        {
            context.Notify("You cannot enter yourself!");
            return false;
        }

        Dbref oldLoc = execObj.Location;
        db.MoveObject(context.Executor, target.Id);

        context.Notify($"You enter {target.Name}.");
        context.NotifyExcept(oldLoc, context.Executor, $"{execObj.Name} enters {target.Name}.");
        context.NotifyExcept(target.Id, context.Executor, $"{execObj.Name} has entered.");
        return true;
    }
}

public class LeaveCommandHandler : ICommandHandler
{
    public string Name => "leave";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify("You are nowhere.");
            return false;
        }

        var container = db.GetObject(execObj.Location);
        if (container == null || !container.Location.IsValid || container.Type == ObjectType.Room)
        {
            context.Notify("You can't leave this.");
            return false;
        }

        Dbref dest = container.Location;
        Dbref oldLoc = execObj.Location;
        db.MoveObject(context.Executor, dest);

        context.Notify($"You leave {container.Name}.");
        context.NotifyExcept(oldLoc, context.Executor, $"{execObj.Name} leaves {container.Name}.");
        context.NotifyExcept(dest, context.Executor, $"{execObj.Name} has arrived from {container.Name}.");
        return true;
    }
}

public class GiveCommandHandler : ICommandHandler
{
    public string Name => "give";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify("Usage: give <player>=<amount or object>");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify("You are nowhere.");
            return false;
        }

        string targetName = context.LeftArg.Trim();
        var target = db.FindInContents(execObj.Location, targetName) ?? db.FindPlayer(targetName);
        if (target == null || target.Type != ObjectType.Player)
        {
            context.Notify("You can only give to players who are here.");
            return false;
        }

        if (target.Id == context.Executor)
        {
            context.Notify("You can't give to yourself.");
            return false;
        }

        // 1. Give money
        if (int.TryParse(context.RightArg.Trim(), out int pennies))
        {
            if (pennies <= 0)
            {
                context.Notify("You must give a positive amount.");
                return false;
            }

            if (!execObj.IsWizard && execObj.Pennies < pennies)
            {
                context.Notify("You don't have that many pennies.");
                return false;
            }

            if (!execObj.IsWizard) execObj.Pennies -= pennies;
            target.Pennies += pennies;

            context.Notify($"You give {pennies} pennies to {target.Name}.");
            context.NotifyTarget(target.Id, $"{execObj.Name} gives you {pennies} pennies.");
            context.NotifyExcept(execObj.Location, context.Executor, $"{execObj.Name} gives some pennies to {target.Name}.");
            return true;
        }

        // 2. Give object
        string objName = context.RightArg.Trim();
        var thing = db.FindInContents(context.Executor, objName);
        if (thing == null)
        {
            context.Notify("You don't have that to give.");
            return false;
        }

        db.MoveObject(thing.Id, target.Id);
        context.Notify($"You give {thing.Name} to {target.Name}.");
        context.NotifyTarget(target.Id, $"{execObj.Name} gives you {thing.Name}.");
        context.NotifyExcept(execObj.Location, context.Executor, $"{execObj.Name} gives {thing.Name} to {target.Name}.");
        return true;
    }
}

public class ExamineCommandHandler : ICommandHandler
{
    public string Name => "examine";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var execObj = db.GetObject(context.Executor);
        if (execObj == null) return false;

        string targetStr = string.IsNullOrWhiteSpace(context.Arguments) ? "here" : context.Arguments.Trim();
        var target = db.MatchObject(context.Executor, targetStr);

        if (target == null)
        {
            context.Notify("I can't see that here to examine.");
            return false;
        }

        var sb = new StringBuilder();
        var owner = db.GetObject(target.Owner);
        string ownerName = owner != null ? owner.Name : target.Owner.ToString();

        sb.AppendLine($"{target.Name} ({target.Id})");
        sb.AppendLine($"Type: {target.Type}  Owner: {ownerName} ({target.Owner})  Pennies: {target.Pennies}");
        sb.AppendLine($"Flags: {string.Join(" ", target.Flags)}");
        if (target.Powers.Count > 0)
            sb.AppendLine($"Powers: {string.Join(" ", target.Powers)}");
        sb.AppendLine($"Location: {target.Location}  Home: {target.Home}");

        if (!context.CommandName.Equals("brief", StringComparison.OrdinalIgnoreCase))
        {
        // Contents
        var contents = db.GetContents(target.Id).ToList();
        if (contents.Count > 0)
        {
            sb.AppendLine("Contents:");
            foreach (var item in contents)
                sb.AppendLine($"  {item.Name} ({item.Id})");
        }

        // Exits
        if (target.Type == ObjectType.Room)
        {
            var exits = db.GetExits(target.Id);
            if (exits.GetEnumerator().MoveNext())
            {
                sb.AppendLine("Exits:");
                foreach (var exit in db.GetExits(target.Id))
                    sb.AppendLine($"  {exit.Name} ({exit.Id}) -> {exit.Location}");
            }
        }

        // Attributes
        if (target.Attributes.Count > 0)
        {
            sb.AppendLine("Attributes:");
            foreach (var attr in target.Attributes)
            {
                sb.AppendLine($"  {attr.Name} [flags: {attr.Flags}]: {attr.Value}");
            }
        }

        // Locks
        if (target.Locks.Count > 0)
        {
            sb.AppendLine("Locks:");
            foreach (var l in target.Locks)
            {
                sb.AppendLine($"  {l.Type}: {l.Key}");
            }
        }

        }

        context.Notify(sb.ToString().TrimEnd());
        return true;
    }
}
