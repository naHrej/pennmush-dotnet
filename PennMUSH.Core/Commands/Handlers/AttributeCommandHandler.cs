namespace PennMUSH.Core.Commands.Handlers;

using PennMUSH.Core.Attributes;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;

public class AttributeCommandHandler : ICommandHandler
{
    public string Name => "@attribute";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        bool wizard = actor != null && actor.IsWizard;
        if (context.Switches.Contains("access"))
            return wizard ? Access(context, db, actor!) : Deny(context);
        if (context.Switches.Contains("decompile"))
            return Decompile(context);
        if (context.Switches.Contains("delete"))
            return wizard ? Delete(context) : Deny(context);
        if (context.Switches.Contains("rename"))
            return wizard ? Rename(context) : Deny(context);
        if (context.Switches.Contains("limit"))
            return wizard ? Limit(context, enumList: false) : Deny(context);
        if (context.Switches.Contains("enum"))
            return wizard ? Limit(context, enumList: true) : Deny(context);
        return Info(context);
    }

    private static bool Access(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        string name = context.LeftArg.Trim().ToUpperInvariant();
        if (name.Length == 0)
        {
            context.Notify("Which attribute do you mean?");
            return false;
        }

        if (!StandardAttributes.TryParseFlags(context.RightArg.Trim(), out var flags))
        {
            context.Notify("I don't understand those permissions.");
            return false;
        }

        bool exists = StandardAttributes.ExistsExact(name);
        if (exists && StandardAttributes.IsInternal(name))
        {
            context.Notify("That attribute's permissions can not be changed.");
            return false;
        }

        if (!exists && !AttributeEntry.IsValidName(name))
        {
            context.Notify("Invalid attribute name.");
            return false;
        }

        StandardAttributes.SetAccess(name, flags, actor.Id);
        if (context.Switches.Contains("retroactive"))
            StandardAttributes.ApplyRetroactive(db, name, StandardAttributes.FlagsOf(name), actor.Id);
        context.Notify($"{name} -- Attribute permissions now: {StandardAttributes.DescribeFlags(StandardAttributes.FlagsOf(name))}");
        return true;
    }

    private static bool Delete(CommandContext context)
    {
        string name = context.LeftArg.Trim();
        if (name.Length == 0)
        {
            context.Notify("Which attribute do you mean?");
            return false;
        }

        if (!StandardAttributes.TryLookup(name, out string canonical) || !StandardAttributes.ExistsExact(name.ToUpperInvariant()))
        {
            context.Notify("That attribute isn't in the attribute table");
            return false;
        }

        Describe(context, canonical);
        int count = StandardAttributes.Delete(name);
        if (count <= 0)
            context.Notify($"Failed to remove {name.ToUpperInvariant()} from attribute table.");
        else if (count == 1)
            context.Notify($"Removed {canonical} from attribute table.");
        else
            context.Notify($"Removed {canonical} and {count - 1} alias(es) from attribute table.");
        return count > 0;
    }

    private static bool Rename(CommandContext context)
    {
        string oldName = context.LeftArg.Trim();
        string newName = context.RightArg.Trim();
        if (oldName.Length == 0 || newName.Length == 0)
        {
            context.Notify("Which attributes do you mean?");
            return false;
        }

        newName = newName.ToUpperInvariant();
        oldName = oldName.ToUpperInvariant();
        if (!AttributeEntry.IsValidName(newName))
        {
            context.Notify("Invalid attribute name.");
            return false;
        }

        if (StandardAttributes.ExistsExact(newName))
        {
            context.Notify($"The name {newName} is already used in the attribute table.");
            return false;
        }

        if (!StandardAttributes.ExistsExact(oldName))
        {
            context.Notify("That attribute isn't in the attribute table");
            return false;
        }

        StandardAttributes.Rename(oldName, newName);
        context.Notify($"Renamed {oldName} to {newName} in attribute table.");
        return true;
    }

    private static bool Limit(CommandContext context, bool enumList)
    {
        string raw = context.LeftArg.Trim();
        string pattern = context.RightArg.Trim();
        char delim = ' ';
        if (enumList && pattern.Length > 0 && raw.Contains(' '))
        {
            int space = raw.IndexOf(' ');
            if (space != 1)
            {
                context.Notify("Delimiter must be one character.");
                return false;
            }

            delim = raw[0];
            raw = raw[(space + 1)..].Trim();
        }

        if (pattern.Length > 0 && !enumList && !StandardAttributes.TryCompileLimit(pattern))
        {
            context.Notify("Invalid Regular Expression.");
            return false;
        }

        if (raw.Length == 0)
        {
            context.Notify("Which attribute do you mean?");
            return false;
        }

        if (!StandardAttributes.TrySetRestriction(raw, pattern, enumList, delim, out string message))
        {
            context.Notify(message);
            return false;
        }

        context.Notify(message);
        return true;
    }

    private static bool Decompile(CommandContext context)
    {
        context.Notify("@@ Standard Attributes:");
        foreach (string line in StandardAttributes.Decompile(context.LeftArg, context.Switches.Contains("retroactive")))
            context.Notify(line);
        return true;
    }

    private static bool Info(CommandContext context)
    {
        string name = context.LeftArg.Trim();
        if (name.Length == 0)
        {
            context.Notify("Which attribute do you mean?");
            return false;
        }

        if (!StandardAttributes.TryLookup(name, out string canonical))
        {
            context.Notify("That attribute isn't in the attribute table");
            return false;
        }

        Describe(context, canonical);
        return true;
    }

    private static void Describe(CommandContext context, string canonical)
    {
        context.Notify($"{"Attribute",9}: {canonical}");
        if (StandardAttributes.HasRestriction(canonical, out bool isEnum, out string shown))
            context.Notify($"{(isEnum ? "Enum" : "Limit"),9}: {shown}");
        context.Notify($"{"Flags",9}: {StandardAttributes.DescribeFlags(StandardAttributes.FlagsOf(canonical))}");
        context.Notify($"{"Creator",9}: #{StandardAttributes.CreatorOf(canonical).Value}");
    }

    private static bool Deny(CommandContext context)
    {
        context.Notify("Permission denied.");
        return false;
    }
}
