namespace PennMUSH.Core.Commands.Handlers;

using System.Globalization;
using System.Text;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Communication;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Flags;
using PennMUSH.Core.Locks;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class CopyAttributeCommandHandler : ICommandHandler
{
    public string Name => "@cpattr";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        bool move = context.CommandName.Equals("@mvattr", StringComparison.OrdinalIgnoreCase);
        if (!TryParseCopy(context, db, out var source, out var sourceAttr, out var destinations, out var actor))
            return false;

        var entry = source!.GetAttribute(sourceAttr);
        if (entry == null)
        {
            context.Notify(Feedback.NoSuchAttribute);
            return false;
        }

        foreach (var (dest, destAttr) in destinations)
        {
            if (dest.Id == source.Id && destAttr.Equals(entry.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            var err = dest.SetAttribute(destAttr, entry.Value, context.Executor, entry.Flags, actor!.IsWizard);
            if (err != AttributeErrorCode.Okay)
            {
                context.Notify(err == AttributeErrorCode.Locked
                    ? $"{Describe(dest)}/{destAttr} is locked."
                    : $"Could not set {Describe(dest)}/{destAttr}: {err}");
                return false;
            }
        }

        if (move)
        {
            var cleared = source.ClearAttribute(entry.Name, actor!.IsWizard);
            if (cleared == AttributeErrorCode.Locked)
            {
                context.Notify(Feedback.IsLocked, ("value", $"{Describe(source)}"), ("value2", $"{entry.Name}"));
                return false;
            }
            context.Notify(Feedback.Moved);
        }
        else
        {
            context.Notify(Feedback.Copied);
        }

        return true;
    }

    private static bool TryParseCopy(
        CommandContext context,
        ObjectDatabase db,
        out MUSHObject? source,
        out string sourceAttr,
        out List<(MUSHObject Dest, string Attr)> destinations,
        out MUSHObject? actor)
    {
        source = null;
        sourceAttr = "";
        destinations = new List<(MUSHObject, string)>();
        actor = db.GetObject(context.Executor);
        if (actor == null)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        if (!TrySplitAttr(context.LeftArg, out var sourceName, out sourceAttr)
            || string.IsNullOrWhiteSpace(context.RightArg))
        {
            string name = string.IsNullOrEmpty(context.CommandName) ? "@cpattr" : context.CommandName;
            context.Notify(Feedback.UsageObjectAttributeObjectAttribute, ("value", $"{name}"));
            return false;
        }

        source = db.MatchObject(context.Executor, sourceName);
        if (source == null)
        {
            context.Notify(Feedback.ICantFindTheSource);
            return false;
        }
        if (!Controls(actor, source))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        foreach (var piece in SplitComma(context.RightArg))
        {
            string destName;
            string destAttr;
            if (piece.Contains('/'))
            {
                if (!TrySplitAttr(piece, out destName, out destAttr))
                {
                    context.Notify(Feedback.BadDestination, ("value", $"{piece}"));
                    return false;
                }
            }
            else
            {
                destName = piece;
                destAttr = sourceAttr;
            }

            var dest = db.MatchObject(context.Executor, destName);
            if (dest == null)
            {
                context.Notify(Feedback.ICantFind, ("value", $"{destName}"));
                return false;
            }
            if (!Controls(actor, dest))
            {
                context.Notify(Feedback.PermissionDenied);
                return false;
            }
            destinations.Add((dest, destAttr));
        }

        return destinations.Count > 0;
    }

    internal static bool Controls(MUSHObject actor, MUSHObject target) =>
        actor.IsWizard || target.Owner == actor.Id || target.Id == actor.Id;

    internal static bool TrySplitAttr(string text, out string objectName, out string attribute)
    {
        objectName = "";
        attribute = "";
        if (string.IsNullOrWhiteSpace(text))
            return false;
        int slash = text.IndexOf('/');
        if (slash <= 0 || slash == text.Length - 1)
            return false;
        objectName = text[..slash].Trim();
        attribute = text[(slash + 1)..].Trim();
        return objectName.Length > 0 && AttributeEntry.IsValidName(attribute);
    }

    internal static List<string> SplitComma(string text)
    {
        return text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    internal static string Describe(MUSHObject obj) => $"{obj.Name} ({obj.Id})";
}

public class EditAttributeCommandHandler : ICommandHandler
{
    public string Name => "@edit";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }
        if (!CopyAttributeCommandHandler.TrySplitAttr(context.LeftArg, out var objectName, out var attrName)
            || string.IsNullOrWhiteSpace(context.RightArg) || !context.RightArg.Contains(','))
        {
            context.Notify(Feedback.UsageEditObjectAttributeOldNew);
            return false;
        }

        var target = db.MatchObject(context.Executor, objectName);
        if (target == null)
        {
            context.Notify(Feedback.ICantFindThatObject);
            return false;
        }
        if (!CopyAttributeCommandHandler.Controls(actor, target))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        var entry = target.GetAttribute(attrName);
        if (entry == null)
        {
            context.Notify(Feedback.NoSuchAttribute);
            return false;
        }

        int comma = context.RightArg.IndexOf(',');
        string oldText = context.RightArg[..comma];
        string newText = context.RightArg[(comma + 1)..];
        if (oldText.Length == 0)
        {
            context.Notify(Feedback.NothingToReplace);
            return false;
        }

        string updated = entry.Value.Replace(oldText, newText, StringComparison.Ordinal);
        if (updated == entry.Value)
        {
            context.Notify(Feedback.NothingToChange);
            return true;
        }

        var err = target.SetAttribute(entry.Name, updated, context.Executor, entry.Flags, actor.IsWizard);
        if (err == AttributeErrorCode.Locked)
        {
            context.Notify(Feedback.IsLocked2, ("value", $"{entry.Name}"));
            return false;
        }
        if (err != AttributeErrorCode.Okay)
        {
            context.Notify(Feedback.CouldNotEdit, ("value", $"{err}"));
            return false;
        }

        context.Notify(Feedback.Edited);
        return true;
    }
}

public class GrepCommandHandler : ICommandHandler
{
    public string Name => "@grep";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify(Feedback.UsageGrepObjectText);
            return false;
        }

        var target = db.MatchObject(context.Executor, context.LeftArg.Trim());
        if (target == null)
        {
            context.Notify(Feedback.ICantFindThatObject);
            return false;
        }

        string needle = context.RightArg;
        var hits = target.Attributes
            .Where(attr => attr.Name.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || attr.Value.Contains(needle, StringComparison.OrdinalIgnoreCase))
            .OrderBy(attr => attr.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (hits.Count == 0)
        {
            context.Notify(Feedback.NoMatch);
            return true;
        }

        context.Notify(string.Join("\n", hits.Select(attr => $"{attr.Name}: {attr.Value}")));
        return true;
    }
}

public class AttributeLockCommandHandler : ICommandHandler
{
    public string Name => "@atrlock";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !CopyAttributeCommandHandler.TrySplitAttr(
                string.IsNullOrWhiteSpace(context.LeftArg) ? context.Arguments : context.LeftArg,
                out var objectName, out var attrName))
        {
            context.Notify(Feedback.UsageAtrlockObjectAttributeOnOff);
            return false;
        }

        var target = db.MatchObject(context.Executor, objectName);
        if (target == null)
        {
            context.Notify(Feedback.ICantFindThatObject);
            return false;
        }
        if (!CopyAttributeCommandHandler.Controls(actor, target))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        var entry = target.GetAttribute(attrName);
        if (entry == null)
        {
            context.Notify(Feedback.NoSuchAttribute);
            return false;
        }

        string mode = context.RightArg.Trim();
        AttributeFlags flags = entry.Flags;
        if (mode.Equals("on", StringComparison.OrdinalIgnoreCase))
            flags |= AttributeFlags.Locked;
        else if (mode.Equals("off", StringComparison.OrdinalIgnoreCase))
            flags &= ~AttributeFlags.Locked;
        else if (mode.Length > 0)
        {
            context.Notify(Feedback.UsageAtrlockObjectAttributeOnOff);
            return false;
        }

        if (flags != entry.Flags)
        {
            var err = target.SetAttributeFlags(entry.Name, flags);
            if (err != AttributeErrorCode.Okay)
            {
                context.Notify(Feedback.NoSuchAttribute);
                return false;
            }
        }

        bool locked = flags.HasFlag(AttributeFlags.Locked);
        context.Notify($"{entry.Name} is {(locked ? "locked" : "unlocked")}.");
        return true;
    }
}

public class AttributeChownCommandHandler : ICommandHandler
{
    public string Name => "@atrchown";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null
            || !CopyAttributeCommandHandler.TrySplitAttr(context.LeftArg, out var objectName, out var attrName)
            || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify(Feedback.UsageAtrchownObjectAttributeOwner);
            return false;
        }

        var target = db.MatchObject(context.Executor, objectName);
        var owner = db.MatchObject(context.Executor, context.RightArg.Trim());
        if (target == null || owner == null)
        {
            context.Notify(Feedback.ICantFindThat);
            return false;
        }
        if (!CopyAttributeCommandHandler.Controls(actor, target))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        var entry = target.GetAttribute(attrName);
        if (entry == null)
        {
            context.Notify(Feedback.NoSuchAttribute);
            return false;
        }
        if (entry.Flags.HasFlag(AttributeFlags.Locked) && !actor.IsWizard)
        {
            context.Notify(Feedback.IsLocked2, ("value", $"{entry.Name}"));
            return false;
        }

        target.SetAttributeCreator(entry.Name, owner.Id);
        context.Notify(Feedback.IsNowOwnedBy, ("value", $"{entry.Name}"), ("value2", $"{owner.Name}"));
        return true;
    }
}

public class LockFlagCommandHandler : ICommandHandler
{
    public string Name => "@lset";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !CopyAttributeCommandHandler.TrySplitAttr(context.LeftArg, out var objectName, out var lockName))
        {
            context.Notify(Feedback.UsageLsetObjectLockFlag);
            return false;
        }

        var target = db.MatchObject(context.Executor, objectName);
        if (target == null)
        {
            context.Notify(Feedback.ICantFindThatObject);
            return false;
        }
        if (!CopyAttributeCommandHandler.Controls(actor, target))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        var entry = target.GetLock(lockName);
        if (entry == null)
        {
            context.Notify(Feedback.NoSuchLock);
            return false;
        }

        var flags = entry.Flags;
        if (!string.IsNullOrWhiteSpace(context.RightArg))
        {
            foreach (var token in context.RightArg.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                bool clear = token.StartsWith('!');
                string name = clear ? token[1..] : token;
                if (!TryParseLockFlag(name, out var flag))
                {
                    context.Notify(Feedback.IDontRecognizeTheLockFlag, ("flag", $"{name}"));
                    return false;
                }
                flags = clear ? flags & ~flag : flags | flag;
            }
            target.SetLock(entry.Type, entry.Key, entry.Creator, flags);
        }

        context.Notify(Feedback.ValueValue4, ("value", $"{entry.Type}"), ("value2", $"{flags}"));
        return true;
    }

    private static bool TryParseLockFlag(string name, out LockFlags flag)
    {
        switch (name.ToLowerInvariant())
        {
            case "visual": flag = LockFlags.Visual; return true;
            case "private": flag = LockFlags.Private; return true;
            case "wizard": flag = LockFlags.Wizard; return true;
            case "locked": flag = LockFlags.Locked; return true;
            case "noclone": flag = LockFlags.NoClone; return true;
            case "owner": flag = LockFlags.Owner; return true;
            default: flag = LockFlags.None; return false;
        }
    }
}

public class MonikerCommandHandler : ICommandHandler
{
    public string Name => "@moniker";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        string objectName = string.IsNullOrWhiteSpace(context.LeftArg) ? context.Arguments.Trim() : context.LeftArg.Trim();
        if (actor == null || string.IsNullOrWhiteSpace(objectName))
        {
            context.Notify(Feedback.UsageMonikerObjectText);
            return false;
        }

        var target = db.MatchObject(context.Executor, objectName);
        if (target == null)
        {
            context.Notify(Feedback.ICantFindThatObject);
            return false;
        }

        bool setting = context.RawCommand.Contains('=');
        if (!setting)
        {
            var current = target.GetAttribute("MONIKER");
            context.Notify(current == null ? "No moniker set." : current.Value);
            return true;
        }

        if (!CopyAttributeCommandHandler.Controls(actor, target))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        if (string.IsNullOrEmpty(context.RightArg))
        {
            target.ClearAttribute("MONIKER", actor.IsWizard);
            context.Notify(Feedback.MonikerCleared);
            return true;
        }

        var err = target.SetAttribute("MONIKER", context.RightArg, context.Executor, wizardOverride: actor.IsWizard);
        if (err == AttributeErrorCode.Locked)
        {
            context.Notify(Feedback.MonikerIsLocked);
            return false;
        }

        context.Notify(Feedback.MonikerSet);
        return true;
    }
}

public class MessageCommandHandler : ICommandHandler
{
    private readonly ExpressionEvaluator? _evaluator;

    public string Name => "@message";

    public MessageCommandHandler(ExpressionEvaluator? evaluator = null)
    {
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg)
            || !CopyAttributeCommandHandler.TrySplitAttr(context.RightArg, out var objectName, out var attrName))
        {
            context.Notify(Feedback.UsageMessageRemitOemitTargetObjectAttribute);
            return false;
        }

        var target = db.MatchObject(context.Executor, context.LeftArg.Trim());
        var holder = db.MatchObject(context.Executor, objectName);
        if (target == null || holder == null)
        {
            context.Notify(Feedback.ICantFindThat);
            return false;
        }

        var entry = holder.GetAttribute(attrName, db);
        if (entry == null)
        {
            context.Notify(Feedback.NoSuchAttribute);
            return false;
        }

        string text = entry.Value;
        if (_evaluator != null)
            text = _evaluator.Evaluate(text, new EvaluationContext(context.Executor, context.Enactor));

        if (context.Switches.Contains("remit"))
        {
            if (!target.Location.IsValid)
            {
                context.Notify(Feedback.ThatTargetHasNoLocation);
                return false;
            }
            context.NotifyRoom(target.Location, text);
        }
        else if (context.Switches.Contains("oemit"))
        {
            if (!target.Location.IsValid)
            {
                context.Notify(Feedback.ThatTargetHasNoLocation);
                return false;
            }
            context.NotifyExcept(target.Location, target.Id, text);
        }
        else
        {
            context.NotifyTarget(target.Id, text);
        }

        return true;
    }
}

public class FindCommandHandler : ICommandHandler
{
    public string Name => "@find";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        string name = context.RawCommand.Contains('=') ? context.LeftArg.Trim() : context.Arguments.Trim();
        if (!TryRange(context.RightArg, out int low, out int high))
        {
            context.Notify(Feedback.UsageFindNameFromTo);
            return false;
        }

        var hits = db.GetAllObjects()
            .Where(obj => obj.Type != ObjectType.None)
            .Where(obj => actor.IsWizard || obj.Owner == actor.Id || obj.Id == actor.Id)
            .Where(obj => low < 0 || (obj.Id.Value >= low && obj.Id.Value <= high))
            .Where(obj => name.Length == 0 || obj.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(obj => obj.Id.Value)
            .ToList();

        if (hits.Count == 0)
        {
            context.Notify(Feedback.NothingFound);
            return true;
        }

        context.Notify(string.Join("\n", hits.Select(obj => $"{obj.Name} ({obj.Id})")));
        return true;
    }

    private static bool TryRange(string text, out int low, out int high)
    {
        low = -1;
        high = -1;
        if (string.IsNullOrWhiteSpace(text))
            return true;
        var parts = text.Trim().TrimStart('#').Split('-', 2);
        if (parts.Length != 2
            || !int.TryParse(parts[0].Trim().TrimStart('#'), out low)
            || !int.TryParse(parts[1].Trim().TrimStart('#'), out high))
            return false;
        return true;
    }
}

public class WhereIsCommandHandler : ICommandHandler
{
    public string Name => "@whereis";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify(Feedback.UsageWhereisPlayer);
            return false;
        }

        var target = db.FindPlayer(context.Arguments.Trim());
        if (target == null || target.Type != ObjectType.Player)
        {
            context.Notify(Feedback.ICantFindThatPlayer);
            return false;
        }

        var place = db.GetObject(target.Location);
        string where = place == null ? "nowhere" : $"{place.Name} ({place.Id})";
        context.Notify(Feedback.ValueIsInValue, ("value", $"{target.Name}"), ("value2", $"{where}"));
        if (!actor.IsWizard && target.Id != actor.Id)
            context.NotifyTarget(target.Id, Feedback.IsLookingForYou.Text(("value", $"{actor.Name}")));
        return true;
    }
}

public class EntrancesCommandHandler : ICommandHandler
{
    public string Name => "@entrances";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        string name = string.IsNullOrWhiteSpace(context.Arguments) ? "here" : context.Arguments.Trim();
        var place = db.MatchObject(context.Executor, name);
        if (place == null)
        {
            context.Notify(Feedback.ICantFindThatPlace);
            return false;
        }

        bool filter = context.Switches.Count > 0;
        var hits = db.GetAllObjects()
            .Where(obj => obj.Id != place.Id)
            .Where(obj => obj.Home == place.Id || (obj.Type == ObjectType.Exit && obj.Location == place.Id))
            .Where(obj => !filter || TypeSelected(obj.Type, context))
            .OrderBy(obj => obj.Id.Value)
            .ToList();

        if (hits.Count == 0)
        {
            context.Notify(Feedback.NoEntrances);
            return true;
        }

        context.Notify(string.Join("\n", hits.Select(obj => $"{obj.Name} ({obj.Id}) [{obj.Type}]")));
        return true;
    }

    private static bool TypeSelected(ObjectType type, CommandContext context)
    {
        return type switch
        {
            ObjectType.Exit => context.Switches.Contains("exits"),
            ObjectType.Thing => context.Switches.Contains("things"),
            ObjectType.Player => context.Switches.Contains("players"),
            ObjectType.Room => context.Switches.Contains("rooms"),
            _ => false
        };
    }
}

public class FirstExitCommandHandler : ICommandHandler
{
    public string Name => "@firstexit";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify(Feedback.UsageFirstexitExit);
            return false;
        }

        var exit = db.MatchObject(context.Executor, context.Arguments.Trim());
        if (exit == null || exit.Type != ObjectType.Exit)
        {
            context.Notify(Feedback.ICantFindThatExit);
            return false;
        }

        var room = db.GetObject(exit.Exits);
        if (room == null)
        {
            context.Notify(Feedback.ThatExitIsNotAttachedToARoom);
            return false;
        }
        if (!CopyAttributeCommandHandler.Controls(actor, room))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        db.RemoveExit(room.Id, exit.Id);
        db.AttachExit(room.Id, exit.Id);
        context.Notify(Feedback.IsNowTheFirstExit, ("value", $"{exit.Name}"));
        return true;
    }
}

public class UptimeCommandHandler : ICommandHandler
{
    public string Name => "@uptime";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var start = new DateTimeOffset(System.Diagnostics.Process.GetCurrentProcess().StartTime);
        var elapsed = DateTimeOffset.Now - start;
        context.Notify($"Up since {start:yyyy-MM-dd HH:mm:ss zzz} ({FormatSpan(elapsed)}).");
        return true;
    }

    private static string FormatSpan(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1)
            return $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m";
        if (elapsed.TotalMinutes >= 1)
            return $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds}s";
        return $"{elapsed.Seconds}s";
    }
}

public class PoorCommandHandler : ICommandHandler
{
    public string Name => "@poor";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.IsGod)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }
        if (!int.TryParse(context.Arguments.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) || amount < 0)
        {
            context.Notify(Feedback.UsagePoorPennies);
            return false;
        }

        int count = 0;
        foreach (var player in db.GetAllObjects().Where(obj => obj.Type == ObjectType.Player))
        {
            player.Pennies = amount;
            count++;
        }

        context.Notify(Feedback.SetPlayerSToPennies, ("value", $"{count}"), ("value2", $"{amount}"));
        return true;
    }
}

public class ListCommandHandler : ICommandHandler
{
    private readonly CommandDispatcher _dispatcher;
    private readonly FunctionRegistry? _functions;
    private readonly MotdBoard? _motd;

    public string Name => "@list";

    public ListCommandHandler(CommandDispatcher dispatcher, FunctionRegistry? functions = null, MotdBoard? motd = null)
    {
        _dispatcher = dispatcher;
        _functions = functions;
        _motd = motd;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        string which = context.Arguments.Trim().ToLowerInvariant();
        bool lower = context.Switches.Contains("lowercase");
        if (context.Switches.Contains("attribs") || which == "attribs")
        {
            string body = string.Join(" ", StandardAttributes.CanonicalNames().OrderBy(name => name, StringComparer.Ordinal));
            if (lower)
                body = body.ToLowerInvariant();
            context.Notify("Attribs: " + body);
            return true;
        }

        if (context.Switches.Contains("motd") || which == "motd")
        {
            var actor = db.GetObject(context.Executor);
            if (_motd == null || actor == null)
            {
                context.Notify(Feedback.UsageListCommandsFlagsPowersFunctionsAttribs);
                return false;
            }

            MotdCommandHandler.Report(context, _motd, actor.IsRoyalty);
            return true;
        }

        IEnumerable<string>? names = which switch
        {
            "commands" => _dispatcher.Table.PrimaryNames,
            "flags" => FlagTable.Definitions.Select(flag => flag.Name),
            "powers" => FlagTable.PowerNames,
            "functions" => _functions?.Names,
            _ => null
        };
        if (names == null)
        {
            context.Notify(which == "functions"
                ? "Functions are not available."
                : "Usage: @list commands|flags|powers|functions|attribs");
            return false;
        }

        var list = names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        context.Notify(list.Count == 0 ? "(empty)" : string.Join("\n", list));
        return true;
    }
}

public class DumpCommandHandler : ICommandHandler
{
    private readonly Action _save;

    public string Name => "@dump";

    public DumpCommandHandler(Action save)
    {
        _save = save;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.IsWizard)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        try
        {
            _save();
        }
        catch (Exception ex)
        {
            context.Notify(Feedback.SaveFailed, ("value", $"{ex.Message}"));
            return false;
        }

        context.Notify(Feedback.DatabaseSaved);
        return true;
    }
}
