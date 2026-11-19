namespace PennMUSH.Core.Commands.Handlers;

using System.Text;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Flags;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Types;

public class DecompileCommandHandler : ICommandHandler
{
    private const string TinyFuguePrefix = "FugueEdit > ";

    private static readonly Dictionary<string, LockFlags> LockPrototypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Basic"] = LockFlags.Private,
        ["Enter"] = LockFlags.Private,
        ["Use"] = LockFlags.Private,
        ["Zone"] = LockFlags.Private,
        ["Page"] = LockFlags.Private,
        ["Teleport"] = LockFlags.Private,
        ["Speech"] = LockFlags.Private,
        ["Listen"] = LockFlags.Private,
        ["Command"] = LockFlags.Private,
        ["Parent"] = LockFlags.Private,
        ["Link"] = LockFlags.Private,
        ["Leave"] = LockFlags.Private,
        ["Drop"] = LockFlags.Private,
        ["Give"] = LockFlags.Private,
        ["From"] = LockFlags.Private,
        ["Pay"] = LockFlags.Private,
        ["Receive"] = LockFlags.Private,
        ["Mail"] = LockFlags.Private,
        ["Follow"] = LockFlags.Private,
        ["Examine"] = LockFlags.Private | LockFlags.Owner,
        ["Chzone"] = LockFlags.Private,
        ["Forward"] = LockFlags.Private | LockFlags.Owner,
        ["Control"] = LockFlags.Private | LockFlags.Owner,
        ["Dropto"] = LockFlags.Private,
        ["Destroy"] = LockFlags.Private | LockFlags.Owner,
        ["Interact"] = LockFlags.Private,
        ["MailForward"] = LockFlags.Private,
        ["Take"] = LockFlags.Private,
        ["Open"] = LockFlags.Private,
        ["Filter"] = LockFlags.Private,
        ["InFilter"] = LockFlags.Private,
        ["DropIn"] = LockFlags.Private,
        ["Chown"] = LockFlags.Private | LockFlags.Owner,
    };

    public string Name => "@decompile";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        string source = context.LeftArg.Trim();
        if (source.Length == 0)
        {
            context.Notify(Feedback.WhatDoYouWantToDecompile);
            return false;
        }

        bool tinyFugue = context.Switches.Contains("tf");
        bool byDbref = (context.Switches.Contains("db") || tinyFugue) && !context.Switches.Contains("name");
        bool flags = context.Switches.Contains("flags");
        bool attribs = context.Switches.Contains("attribs");
        if (!flags && !attribs)
        {
            flags = true;
            attribs = true;
        }

        string prefix = tinyFugue ? TinyFuguePrefixOf(actor) : PrefixAfterEquals(context);
        AttrFlagMode flagMode = tinyFugue
            ? AttrFlagMode.None
            : context.Switches.Contains("skipdefaults") ? AttrFlagMode.SkipDefaults : AttrFlagMode.All;

        int slash = source.IndexOf('/');
        string objectName = slash < 0 ? source : source[..slash];
        string patternText = slash < 0 ? "" : source[(slash + 1)..];
        if (objectName.Length == 0)
        {
            context.Notify(Feedback.WhatDoYouWantToDecompile);
            return false;
        }

        var target = db.MatchObject(context.Executor, objectName);
        if (target == null)
        {
            context.Notify(Feedback.ICantSeeThatHere);
            return false;
        }

        if (target.Type == ObjectType.Garbage)
        {
            context.Notify(Feedback.GarbageIsGarbage);
            return false;
        }

        string label = byDbref ? target.Id.ToString() : ObjectLabel(target, objectName);
        if (patternText.Length > 0)
        {
            foreach (string pattern in patternText.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                PrintAttributes(context, db, actor, target, label, prefix, pattern, flagMode);
            return true;
        }

        if (!flags)
        {
            PrintAttributes(context, db, actor, target, label, prefix, "**", flagMode);
            return true;
        }

        if (!CanExamine(actor, target))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        PrintHeader(context, db, target, prefix);
        PrintCreate(context, target, prefix);
        PrintLink(context, target, label, prefix);
        foreach (var zone in target.Zones.OrderBy(zone => zone.Value))
        {
            if (zone.IsValid)
                context.Notify($"{prefix}@chzone {label} = {zone}");
        }

        foreach (var parent in target.Parents)
        {
            if (parent.IsValid)
                context.Notify($"{prefix}@parent {label}={parent}");
        }

        PrintLocks(context, target, label, prefix, flagMode != AttrFlagMode.All);
        PrintObjectFlags(context, actor, target, label, prefix);
        if (attribs)
            PrintAttributes(context, db, actor, target, label, prefix, "**", flagMode);
        return true;
    }

    private static void PrintHeader(CommandContext context, ObjectDatabase db, MUSHObject target, string prefix)
    {
        string shown = ShortName(target.Name);
        if (target.Type == ObjectType.Exit && target.Exits.IsValid)
        {
            var source = db.GetObject(target.Exits);
            if (source != null)
            {
                context.Notify($"{prefix}@@ {shown} ({target.Id}), in {source.Name} ({source.Id})");
                return;
            }
        }

        context.Notify($"{prefix}@@ {shown} ({target.Id})");
    }

    private static void PrintCreate(CommandContext context, MUSHObject target, string prefix)
    {
        switch (target.Type)
        {
            case ObjectType.Thing:
                context.Notify($"{prefix}@create {target.Name}");
                break;
            case ObjectType.Room:
                context.Notify($"{prefix}@dig/teleport {target.Name}");
                break;
            case ObjectType.Exit:
                context.Notify($"{prefix}@open {target.Name}");
                break;
        }
    }

    private static void PrintLink(CommandContext context, MUSHObject target, string label, string prefix)
    {
        bool mobile = target.Type is ObjectType.Player or ObjectType.Thing;
        Dbref destination = mobile ? target.Home : target.Type == ObjectType.Exit ? target.Location : target.Home;
        if (destination.IsValid)
            context.Notify($"{prefix}@link {label} = {destination}");
    }

    private static void PrintLocks(CommandContext context, MUSHObject target, string label, string prefix, bool skipDefaults)
    {
        foreach (var entry in target.Locks)
        {
            string key = Decompose(entry.Key.Unparse(context.Executor));
            if (LockPrototypes.TryGetValue(entry.Type, out var prototype))
            {
                string type = CanonicalLock(entry.Type);
                context.Notify($"{prefix}@lock/{type} {label}={key}");
                if (skipDefaults && entry.Flags == prototype)
                    continue;
                string flags = LockFlagText(entry.Flags);
                if (flags.Length > 0)
                    context.Notify($"{prefix}@lset {label}/{type}={flags}");
                if (prototype.HasFlag(LockFlags.Private) && !entry.Flags.HasFlag(LockFlags.Private))
                    context.Notify($"{prefix}@lset {label}/{type}=!no_inherit");
            }
            else
            {
                context.Notify($"{prefix}@lock/user:{entry.Type} {label}={key}");
                string flags = LockFlagText(entry.Flags);
                if (flags.Length > 0)
                    context.Notify($"{prefix}@lset {label}/{entry.Type}={flags}");
            }
        }
    }

    private static void PrintObjectFlags(CommandContext context, MUSHObject actor, MUSHObject target, string label, string prefix)
    {
        foreach (var definition in FlagTable.Definitions)
        {
            if (definition.IsPower || !target.HasFlag(definition.Name) || !CanSeeFlag(actor, target, definition))
                continue;
            context.Notify($"{prefix}@set {label} = {definition.Name}");
        }

        foreach (string name in FlagTable.PowerNames)
        {
            var definition = FlagTable.FindPower(name);
            if (definition == null || !target.HasPower(definition.Name) || !CanSeeFlag(actor, target, definition))
                continue;
            context.Notify($"{prefix}@power {label} = {definition.Name}");
        }
    }

    private static void PrintAttributes(
        CommandContext context,
        ObjectDatabase db,
        MUSHObject actor,
        MUSHObject target,
        string label,
        string prefix,
        string pattern,
        AttrFlagMode flagMode)
    {
        int printed = 0;
        foreach (var attr in target.Attributes)
        {
            if (attr.Name.Contains('`'))
                continue;
            if (!Matches(pattern, attr.Name) || !CanRead(db, actor, target, attr))
                continue;
            if (attr.Flags.HasFlag(AttributeFlags.NoDump))
                continue;

            printed++;
            if (attr.Name.Equals("MONIKER", StringComparison.OrdinalIgnoreCase))
            {
                context.Notify($"{prefix}@MONIKER {label}={Decompose(attr.Value)}");
            }
            else if (NeedsSetForm(attr.Value))
            {
                context.Notify($"{prefix}@set {label}={attr.Name}:{Decompose(attr.Value)}");
            }
            else
            {
                context.Notify($"{prefix}&{attr.Name} {label}={attr.Value}");
            }

            if (flagMode == AttrFlagMode.None)
                continue;
            string flags = AttributeFlagText(attr, flagMode);
            if (flags.Length > 0)
                context.Notify($"{prefix}@set {label}/{attr.Name}={flags}");
        }

        if (printed == 0)
            context.Notify(Feedback.DecompileNoAttributesMatch, ("prefix", prefix), ("pattern", pattern));
    }

    private static string AttributeFlagText(AttributeEntry attr, AttrFlagMode flagMode)
    {
        var stored = attr.Flags & ~AttributeFlags.Root;
        if (flagMode == AttrFlagMode.SkipDefaults && StandardAttributes.ExistsExact(attr.Name))
        {
            var standard = StandardAttributes.FlagsOf(attr.Name);
            var withoutBuiltins = standard & ~AttributeFlags.PrefixMatch & ~AttributeFlags.Root;
            if (attr.Flags == standard || attr.Flags == withoutBuiltins)
                return "";
        }

        return StandardAttributes.DescribeFlags(stored);
    }

    private static bool CanRead(ObjectDatabase db, MUSHObject actor, MUSHObject target, AttributeEntry attr)
    {
        if (attr.Flags.HasFlag(AttributeFlags.Internal))
            return false;
        if (actor.IsWizard || actor.HasPower("See_All"))
            return true;
        if (attr.Flags.HasFlag(AttributeFlags.MDark))
            return false;
        if (Controls(actor, target))
            return true;
        if (attr.Flags.HasFlag(AttributeFlags.Visual) && (!attr.Flags.HasFlag(AttributeFlags.Nearby) || Nearby(actor, target)))
            return true;
        var creator = db.GetObject(attr.Creator);
        return actor.HasFlag("TRUST") && creator != null && creator.Owner == actor.Owner;
    }

    private static bool CanExamine(MUSHObject actor, MUSHObject target)
        => actor.IsWizard || actor.HasPower("See_All") || Controls(actor, target) || target.HasFlag("VISUAL");

    private static bool Controls(MUSHObject actor, MUSHObject target)
        => actor.IsWizard || actor.Id == target.Id || target.Owner == actor.Id || actor.HasPower("Long_Fingers");

    private static bool Nearby(MUSHObject actor, MUSHObject target)
        => actor.Id == target.Id
            || actor.Location == target.Id
            || target.Location == actor.Id
            || (actor.Location.IsValid && actor.Location == target.Location);

    private static bool CanSeeFlag(MUSHObject actor, MUSHObject target, FlagDefinition definition)
    {
        var perms = definition.Permissions;
        if (perms.HasFlag(FlagPrivileges.Internal) || perms.HasFlag(FlagPrivileges.Disabled))
            return false;
        if (actor.IsGod)
            return true;
        if (perms.HasFlag(FlagPrivileges.Dark))
            return false;
        if (actor.IsWizard || actor.HasPower("See_All"))
            return true;
        if (perms.HasFlag(FlagPrivileges.Mdark))
            return actor.Owner == target.Owner;
        return true;
    }

    private static string ObjectLabel(MUSHObject target, string typed)
    {
        return target.Type switch
        {
            ObjectType.Player when typed.Equals("me", StringComparison.OrdinalIgnoreCase) => "me",
            ObjectType.Player => "*" + target.Name,
            ObjectType.Room => "here",
            ObjectType.Exit => ShortName(target.Name),
            _ => target.Name
        };
    }

    private static string ShortName(string name)
    {
        int semi = name.IndexOf(';');
        return semi < 0 ? name : name[..semi];
    }

    private static string PrefixAfterEquals(CommandContext context)
    {
        int equals = context.RawCommand.IndexOf('=');
        return equals < 0 ? "" : context.RawCommand[(equals + 1)..];
    }

    private static string TinyFuguePrefixOf(MUSHObject actor)
    {
        var prefix = actor.GetAttribute("TFPREFIX");
        return prefix == null || prefix.Value.Length == 0 ? TinyFuguePrefix : prefix.Value;
    }

    private static string CanonicalLock(string type)
        => LockPrototypes.Keys.First(name => name.Equals(type, StringComparison.OrdinalIgnoreCase));

    private static string LockFlagText(LockFlags flags)
    {
        var names = new List<string>();
        if (flags.HasFlag(LockFlags.Visual))
            names.Add("visual");
        if (flags.HasFlag(LockFlags.Private))
            names.Add("no_inherit");
        if (flags.HasFlag(LockFlags.NoClone))
            names.Add("no_clone");
        if (flags.HasFlag(LockFlags.Wizard))
            names.Add("wizard");
        if (flags.HasFlag(LockFlags.Locked))
            names.Add("locked");
        return string.Join(' ', names);
    }

    private static bool NeedsSetForm(string value)
        => value.Length > 0 && (value[0] == ' ' || value[^1] == ' ' || value.Contains('\n') || value.Contains('\t'));

    internal static string Decompose(string value)
    {
        var text = new StringBuilder();
        int index = 0;
        bool atStart = true;
        while (index < value.Length)
        {
            if (value[index] == ' ')
            {
                int spaces = 0;
                while (index < value.Length && value[index] == ' ')
                {
                    spaces++;
                    index++;
                }

                AppendSpaces(text, spaces, atStart, index >= value.Length);
                atStart = false;
                continue;
            }

            atStart = false;
            char current = value[index++];
            if (current == '\n')
                text.Append("%r");
            else if (current == '\t')
                text.Append("%t");
            else
            {
                if (IsEscaped(current))
                    text.Append('\\');
                text.Append(current);
            }
        }

        return text.ToString();
    }

    private static void AppendSpaces(StringBuilder text, int spaces, bool atStart, bool trailing)
    {
        if (spaces >= 5)
        {
            text.Append("[space(").Append(spaces).Append(")]");
            return;
        }

        if (trailing)
        {
            spaces--;
            if (spaces > 0 && atStart)
            {
                spaces--;
                text.Append("%b");
            }

            while (spaces > 0)
            {
                text.Append(' ');
                spaces--;
                if (spaces > 0)
                {
                    spaces--;
                    text.Append("%b");
                }
            }

            text.Append("%b");
            return;
        }

        if (atStart)
        {
            spaces--;
            text.Append("%b");
        }

        while (spaces > 0)
        {
            text.Append(' ');
            spaces--;
            if (spaces > 0)
            {
                spaces--;
                text.Append("%b");
            }
        }
    }

    private static bool IsEscaped(char value)
        => value is '(' or ')' or '[' or ']' or '{' or '}' or '$' or '^' or '%' or ',' or ';' or '\\';

    private static bool Matches(string pattern, string name)
    {
        if (!pattern.Contains('*') && !pattern.Contains('?'))
            return name.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        return Wild(pattern, name);
    }

    private static bool Wild(string pattern, string text)
    {
        int p = 0;
        int t = 0;
        int star = -1;
        int mark = 0;
        while (t < text.Length)
        {
            if (p < pattern.Length && (pattern[p] == '?' || char.ToUpperInvariant(pattern[p]) == char.ToUpperInvariant(text[t])))
            {
                p++;
                t++;
            }
            else if (p < pattern.Length && pattern[p] == '*')
            {
                star = p++;
                mark = t;
            }
            else if (star >= 0)
            {
                p = star + 1;
                t = ++mark;
            }
            else
            {
                return false;
            }
        }

        while (p < pattern.Length && pattern[p] == '*')
            p++;
        return p == pattern.Length;
    }

    private enum AttrFlagMode
    {
        All,
        SkipDefaults,
        None
    }
}
