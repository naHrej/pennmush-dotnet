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
            context.Notify(Feedback.WhichAttributeDoYouMean);
            return false;
        }

        if (!StandardAttributes.TryParseFlags(context.RightArg.Trim(), out var flags))
        {
            context.Notify(Feedback.IDontUnderstandThosePermissions);
            return false;
        }

        bool exists = StandardAttributes.ExistsExact(name);
        if (exists && StandardAttributes.IsInternal(name))
        {
            context.Notify(Feedback.ThatAttributesPermissionsCanNotBeChanged);
            return false;
        }

        if (!exists && !AttributeEntry.IsValidName(name))
        {
            context.Notify(Feedback.InvalidAttributeName);
            return false;
        }

        StandardAttributes.SetAccess(name, flags, actor.Id);
        if (context.Switches.Contains("retroactive"))
            StandardAttributes.ApplyRetroactive(db, name, StandardAttributes.FlagsOf(name), actor.Id);
        context.Notify(Feedback.AttributePermissionsNow, ("value", $"{name}"), ("value2", $"{StandardAttributes.DescribeFlags(StandardAttributes.FlagsOf(name))}"));
        return true;
    }

    private static bool Delete(CommandContext context)
    {
        string name = context.LeftArg.Trim();
        if (name.Length == 0)
        {
            context.Notify(Feedback.WhichAttributeDoYouMean);
            return false;
        }

        if (!StandardAttributes.TryLookup(name, out string canonical) || !StandardAttributes.ExistsExact(name.ToUpperInvariant()))
        {
            context.Notify(Feedback.ThatAttributeIsntInTheAttributeTable);
            return false;
        }

        Describe(context, canonical);
        int count = StandardAttributes.Delete(name);
        if (count <= 0)
            context.Notify(Feedback.FailedToRemoveFromAttributeTable, ("value", $"{name.ToUpperInvariant()}"));
        else if (count == 1)
            context.Notify(Feedback.RemovedFromAttributeTable, ("value", $"{canonical}"));
        else
            context.Notify(Feedback.RemovedAndAliasEsFromAttributeTable, ("value", $"{canonical}"), ("value2", $"{count - 1}"));
        return count > 0;
    }

    private static bool Rename(CommandContext context)
    {
        string oldName = context.LeftArg.Trim();
        string newName = context.RightArg.Trim();
        if (oldName.Length == 0 || newName.Length == 0)
        {
            context.Notify(Feedback.WhichAttributesDoYouMean);
            return false;
        }

        newName = newName.ToUpperInvariant();
        oldName = oldName.ToUpperInvariant();
        if (!AttributeEntry.IsValidName(newName))
        {
            context.Notify(Feedback.InvalidAttributeName);
            return false;
        }

        if (StandardAttributes.ExistsExact(newName))
        {
            context.Notify(Feedback.TheNameIsAlreadyUsedInTheAttributeTable, ("name", $"{newName}"));
            return false;
        }

        if (!StandardAttributes.ExistsExact(oldName))
        {
            context.Notify(Feedback.ThatAttributeIsntInTheAttributeTable);
            return false;
        }

        StandardAttributes.Rename(oldName, newName);
        context.Notify(Feedback.RenamedToInAttributeTable, ("value", $"{oldName}"), ("value2", $"{newName}"));
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
                context.Notify(Feedback.DelimiterMustBeOneCharacter);
                return false;
            }

            delim = raw[0];
            raw = raw[(space + 1)..].Trim();
        }

        if (pattern.Length > 0 && !enumList && !StandardAttributes.TryCompileLimit(pattern))
        {
            context.Notify(Feedback.InvalidRegularExpression);
            return false;
        }

        if (raw.Length == 0)
        {
            context.Notify(Feedback.WhichAttributeDoYouMean);
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
        context.Notify(Feedback.StandardAttributes);
        foreach (string line in StandardAttributes.Decompile(context.LeftArg, context.Switches.Contains("retroactive")))
            context.Notify(line);
        return true;
    }

    private static bool Info(CommandContext context)
    {
        string name = context.LeftArg.Trim();
        if (name.Length == 0)
        {
            context.Notify(Feedback.WhichAttributeDoYouMean);
            return false;
        }

        if (!StandardAttributes.TryLookup(name, out string canonical))
        {
            context.Notify(Feedback.ThatAttributeIsntInTheAttributeTable);
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
        context.Notify(Feedback.PermissionDenied);
        return false;
    }
}
