namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Text;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Database;
using PennMUSH.Core.Locks;
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
            context.Notify(Feedback.YouAreAlreadyHome);
            return true;
        }

        Dbref oldLoc = execObj.Location;
        if (oldLoc.IsValid)
        {
            context.NotifyExcept(oldLoc, context.Executor, Feedback.GoesHome.Text(("value", $"{execObj.Name}")));
        }

        db.MoveObject(context.Executor, homeRef);
        context.Notify(Feedback.TheresNoPlaceLikeHome);
        context.NotifyExcept(homeRef, context.Executor, Feedback.ArrivesFromHome.Text(("value", $"{execObj.Name}")));

        _lookHandler?.ShowHere(context, db, context.Executor, LookKind.Automatic);

        return true;
    }
}

public class EnterCommandHandler : ICommandHandler
{
    private readonly LookCommandHandler? _lookHandler;
    private readonly CommandDispatcher? _dispatcher;
    private readonly Func<Dbref, bool>? _connected;

    public string Name => "enter";

    public EnterCommandHandler(LookCommandHandler? lookHandler = null, CommandDispatcher? dispatcher = null, Func<Dbref, bool>? connected = null)
    {
        _lookHandler = lookHandler;
        _dispatcher = dispatcher;
        _connected = connected;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify(Feedback.EnterWhat);
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify(Feedback.YouAreNowhere);
            return false;
        }

        var target = db.FindInContents(execObj.Location, context.Arguments.Trim());
        if (target == null)
        {
            context.Notify(Feedback.IDontSeeThatHereToEnter);
            return false;
        }

        if (target.Id == context.Executor)
        {
            context.Notify(Feedback.YouCannotEnterYourself);
            return false;
        }

        if (!target.EvalLock(LockType.Enter, context.Executor, db))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        Dbref oldLoc = execObj.Location;
        db.MoveObject(context.Executor, target.Id);

        context.Notify(Feedback.YouEnter, ("value", $"{target.Name}"));
        context.NotifyExcept(oldLoc, context.Executor, Feedback.ValueEntersValue.Text(("value", $"{execObj.Name}"), ("value2", $"{target.Name}")));
        context.NotifyExcept(target.Id, context.Executor, Feedback.HasEntered.Text(("value", $"{execObj.Name}")));
        _lookHandler?.ShowHere(context, db, context.Executor, LookKind.Automatic);
        FollowerMovement.Bring(context, db, _dispatcher, _connected, oldLoc, $"enter #{target.Id.Value}");
        return true;
    }
}

public class LeaveCommandHandler : ICommandHandler
{
    private readonly LookCommandHandler? _lookHandler;
    private readonly CommandDispatcher? _dispatcher;
    private readonly Func<Dbref, bool>? _connected;

    public string Name => "leave";

    public LeaveCommandHandler(LookCommandHandler? lookHandler = null, CommandDispatcher? dispatcher = null, Func<Dbref, bool>? connected = null)
    {
        _lookHandler = lookHandler;
        _dispatcher = dispatcher;
        _connected = connected;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify(Feedback.YouAreNowhere);
            return false;
        }

        var container = db.GetObject(execObj.Location);
        if (container == null || !container.Location.IsValid || container.Type == ObjectType.Room)
        {
            context.Notify(Feedback.YouCantLeaveThis);
            return false;
        }

        if (!container.EvalLock(LockType.Leave, context.Executor, db))
        {
            context.Notify(Feedback.YouCantLeave);
            return false;
        }

        Dbref dest = container.Location;
        Dbref oldLoc = execObj.Location;
        db.MoveObject(context.Executor, dest);

        context.Notify(Feedback.YouLeave, ("value", $"{container.Name}"));
        context.NotifyExcept(oldLoc, context.Executor, Feedback.ValueLeavesValue.Text(("value", $"{execObj.Name}"), ("value2", $"{container.Name}")));
        context.NotifyExcept(dest, context.Executor, Feedback.HasArrivedFrom.Text(("value", $"{execObj.Name}"), ("value2", $"{container.Name}")));
        _lookHandler?.ShowHere(context, db, context.Executor, LookKind.Automatic);
        FollowerMovement.Bring(context, db, _dispatcher, _connected, oldLoc, "leave");
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
            context.Notify(Feedback.UsageGivePlayerAmountOrObject);
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify(Feedback.YouAreNowhere);
            return false;
        }

        string targetName = context.LeftArg.Trim();
        var target = db.FindInContents(execObj.Location, targetName) ?? db.FindPlayer(targetName);
        if (target == null || target.Type != ObjectType.Player)
        {
            context.Notify(Feedback.YouCanOnlyGiveToPlayersWhoAreHere);
            return false;
        }

        if (target.Id == context.Executor)
        {
            context.Notify(Feedback.YouCantGiveToYourself);
            return false;
        }

        // 1. Give money
        if (int.TryParse(context.RightArg.Trim(), out int pennies))
        {
            if (pennies <= 0)
            {
                context.Notify(Feedback.YouMustGiveAPositiveAmount);
                return false;
            }

            if (!execObj.IsWizard && execObj.Pennies < pennies)
            {
                context.Notify(Feedback.YouDontHaveThatManyPennies);
                return false;
            }

            if (!execObj.IsWizard) execObj.Pennies -= pennies;
            target.Pennies += pennies;

            context.Notify(Feedback.YouGivePenniesTo, ("value", $"{pennies}"), ("value2", $"{target.Name}"));
            context.NotifyTarget(target.Id, Feedback.GivesYouPennies.Text(("value", $"{execObj.Name}"), ("value2", $"{pennies}")));
            context.NotifyExcept(execObj.Location, context.Executor, Feedback.GivesSomePenniesTo.Text(("value", $"{execObj.Name}"), ("value2", $"{target.Name}")));
            return true;
        }

        // 2. Give object
        string objName = context.RightArg.Trim();
        var thing = db.FindInContents(context.Executor, objName);
        if (thing == null)
        {
            context.Notify(Feedback.YouDontHaveThatToGive);
            return false;
        }

        if (!thing.EvalLock(LockType.Give, context.Executor, db))
        {
            context.Notify(Feedback.YouCantGiveThatAway);
            return false;
        }

        db.MoveObject(thing.Id, target.Id);
        context.Notify(Feedback.YouGiveTo, ("value", $"{thing.Name}"), ("value2", $"{target.Name}"));
        context.NotifyTarget(target.Id, Feedback.GivesYou.Text(("value", $"{execObj.Name}"), ("value2", $"{thing.Name}")));
        context.NotifyExcept(execObj.Location, context.Executor, Feedback.ValueGivesValueToValue.Text(("value", $"{execObj.Name}"), ("value2", $"{thing.Name}"), ("value3", $"{target.Name}")));
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
        string? attrPattern = null;
        int slash = targetStr.IndexOf('/');
        if (slash > 0)
        {
            attrPattern = targetStr[(slash + 1)..].Trim();
            targetStr = targetStr[..slash].Trim();
        }

        var target = db.MatchObject(context.Executor, targetStr);

        if (target == null)
        {
            context.Notify(Feedback.ICantSeeThatHereToExamine);
            return false;
        }

        if (attrPattern != null)
        {
            var matched = new List<string>();
            foreach (var attr in target.Attributes.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (!AttributePermissions.CanRead(execObj, target, attr, target))
                    continue;
                if (MatchesAttributePattern(attr.Name, attrPattern))
                    matched.Add($"{attr.Name} [{attr.Creator}{AttrFlagLetters(attr)}]: {attr.Value}");
            }
            if (matched.Count > 0)
                context.Notify(string.Join("\n", matched), escapeHtml: true);
            else
                context.Notify("No matching attributes.");
            return true;
        }

        var sb = new StringBuilder();
        var owner = db.GetObject(target.Owner);
        string ownerName = owner != null ? owner.Name : target.Owner.ToString();

        sb.AppendLine($"{target.Name} ({target.Id})");
        sb.AppendLine($"Type: {target.Type}  Owner: {ownerName} ({target.Owner})  Pennies: {target.Pennies}");
        sb.AppendLine($"Flags: {string.Join(" ", target.Flags)}");
        if (target.Powers.Count > 0)
            sb.AppendLine($"Powers: {string.Join(" ", target.Powers)}");
        sb.AppendLine($"Warnings checked: {WarningBits.Unparse(target.Warnings)}");
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
                if (!AttributePermissions.CanRead(execObj, target, attr, target))
                    continue;
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

        context.Notify(sb.ToString().TrimEnd(), escapeHtml: true);
        return true;
    }

    private static bool MatchesAttributePattern(string attrName, string pattern)
    {
        if (pattern == "**")
            return true;
        if (pattern.EndsWith('`') && !pattern.Contains('*') && !pattern.Contains('?'))
        {
            if (!attrName.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
                return false;
            return attrName.IndexOf('`', pattern.Length) < 0;
        }
        if (!pattern.Contains('*') && !pattern.Contains('?'))
            return attrName.Equals(pattern, StringComparison.OrdinalIgnoreCase);

        var regex = new StringBuilder("^");
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            if (c == '*' && i + 1 < pattern.Length && pattern[i + 1] == '*')
            {
                regex.Append(".*");
                i++;
            }
            else if (c == '*')
            {
                regex.Append("[^`]*");
            }
            else if (c == '?')
            {
                regex.Append("[^`]");
            }
            else
            {
                regex.Append(System.Text.RegularExpressions.Regex.Escape(c.ToString()));
            }
        }
        regex.Append('$');
        return System.Text.RegularExpressions.Regex.IsMatch(attrName, regex.ToString(), System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static string AttrFlagLetters(PennMUSH.Core.Attributes.AttributeEntry attr)
    {
        var sb = new StringBuilder();
        if (attr.Flags.HasFlag(PennMUSH.Core.Attributes.AttributeFlags.Root)) sb.Append('`');
        if (attr.Flags.HasFlag(PennMUSH.Core.Attributes.AttributeFlags.NoProg)) sb.Append('$');
        if (attr.Flags.HasFlag(PennMUSH.Core.Attributes.AttributeFlags.Wizard)) sb.Append('w');
        if (attr.Flags.HasFlag(PennMUSH.Core.Attributes.AttributeFlags.Visual)) sb.Append('v');
        if (attr.Flags.HasFlag(PennMUSH.Core.Attributes.AttributeFlags.Locked)) sb.Append('+');
        if (attr.Flags.HasFlag(PennMUSH.Core.Attributes.AttributeFlags.Safe)) sb.Append('S');
        if (attr.Flags.HasFlag(PennMUSH.Core.Attributes.AttributeFlags.Regexp)) sb.Append('R');
        if (attr.Flags.HasFlag(PennMUSH.Core.Attributes.AttributeFlags.Case)) sb.Append('C');
        return sb.ToString();
    }
}
