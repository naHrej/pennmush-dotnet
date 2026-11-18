namespace PennMUSH.Core.Commands.Handlers;

using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Flags;
using PennMUSH.Core.Types;

public class FlagCommandHandler : ICommandHandler
{
    private static readonly (string Name, FlagPrivileges Bit)[] PermissionNames =
    {
        ("trusted", FlagPrivileges.Inherit),
        ("owned", FlagPrivileges.Owned),
        ("royalty", FlagPrivileges.Royalty),
        ("wizard", FlagPrivileges.Wizard),
        ("god", FlagPrivileges.God),
        ("internal", FlagPrivileges.Internal),
        ("dark", FlagPrivileges.Dark),
        ("mdark", FlagPrivileges.Mdark),
        ("odark", FlagPrivileges.Odark),
        ("disabled", FlagPrivileges.Disabled),
        ("log", FlagPrivileges.Log),
        ("event", FlagPrivileges.Event),
    };

    private static readonly (string Name, ObjectType Bit)[] TypeNames =
    {
        ("PLAYER", ObjectType.Player),
        ("ROOM", ObjectType.Room),
        ("EXIT", ObjectType.Exit),
        ("THING", ObjectType.Thing),
    };

    public string Name => "@flag";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor != null && actor.HasFlag("GAGGED"))
        {
            context.Notify(Feedback.YouCannotDoThatWhileGagged);
            return false;
        }

        if (context.Switches.Contains("list"))
            return List(context, actor, decompile: false);
        if (context.Switches.Contains("decompile"))
            return List(context, actor, decompile: true);
        if (context.Switches.Contains("add"))
            return Add(context, actor);
        if (context.Switches.Contains("delete"))
            return Delete(context, db, actor);
        if (context.Switches.Contains("alias"))
            return Alias(context, actor);
        if (context.Switches.Contains("restrict"))
            return Restrict(context, actor);
        if (context.Switches.Contains("disable"))
            return SetDisabled(context, actor, disable: true);
        if (context.Switches.Contains("enable"))
            return SetDisabled(context, actor, disable: false);
        if (context.Switches.Contains("letter"))
            return Letter(context, actor);
        if (context.Switches.Contains("type"))
            return Type(context, db, actor);
        if (context.Switches.Contains("debug"))
            return Debug(context, actor);

        return Info(context, actor, context.LeftArg);
    }

    private static bool List(CommandContext context, MUSHObject? actor, bool decompile)
    {
        string body = decompile
            ? Decompile(actor)
            : string.Join(", ", Visible(actor, context.LeftArg).Select(FormatListEntry));
        context.Notify(decompile ? "@@ Flags: " + body : "Flags: " + body);
        return true;
    }

    private static bool Add(CommandContext context, MUSHObject? actor)
    {
        if (!God(actor))
        {
            context.Notify(Feedback.YouDontHaveEnoughMagicForThat);
            return false;
        }

        string name = context.LeftArg.Trim();
        if (!ValidNewName(context, name, alias: false))
            return false;

        var existing = FlagTable.FindFlag(name);
        if (existing != null)
        {
            Describe(context, existing);
            return true;
        }

        var fields = Fields(context.RightArg);
        char letter = '\0';
        if (fields.Length > 0 && fields[0].Length > 0)
        {
            if (fields[0].Length != 1)
            {
                context.Notify(Feedback.FlagCharactersMustBeSingleCharacters);
                return false;
            }

            letter = fields[0][0];
        }

        if (!TryTypes(context, fields.Length > 1 ? fields[1] : null, out ObjectType type))
            return false;
        if (!TryPermissions(context, fields.Length > 2 ? fields[2] : null, out FlagPrivileges perms))
            return false;
        FlagPrivileges negate = perms;
        if (fields.Length > 3 && !TryPermissions(context, fields[3], out negate))
            return false;

        var conflict = FlagTable.LetterConflict(letter, type, null);
        if (conflict != null)
        {
            context.Notify(Feedback.LetterConflictsWithTheFlag, ("flag", $"{conflict.Name}"));
            return false;
        }

        var created = new FlagDefinition
        {
            Name = name.ToUpperInvariant(),
            Letter = letter,
            TargetType = type,
            Permissions = perms,
            NegatePermissions = negate,
        };
        FlagTable.AddFlag(created);
        Describe(context, created);
        return true;
    }

    private static bool Restrict(CommandContext context, MUSHObject? actor)
    {
        if (!God(actor))
        {
            context.Notify(Feedback.YouDontHaveEnoughMagicForThat);
            return false;
        }

        var flag = Find(context.LeftArg);
        if (flag == null)
        {
            context.Notify(Feedback.NoSuchFlag);
            return false;
        }

        if (string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify(Feedback.HowDoYouWantToRestrictThatFlag);
            return false;
        }

        var fields = Fields(context.RightArg);
        if (!TryPermissions(context, fields[0], out FlagPrivileges perms))
            return false;
        string negateSource = fields.Length > 1 ? fields[1] : fields[0];
        if (!TryPermissions(context, negateSource, out FlagPrivileges negate))
            return false;

        flag.Permissions = perms;
        flag.NegatePermissions = negate;
        context.Notify(Feedback.PermissionsOnFlagSet, ("value", $"{flag.Name}"));
        return true;
    }

    private static bool Type(CommandContext context, ObjectDatabase db, MUSHObject? actor)
    {
        if (!God(actor))
        {
            context.Notify(Feedback.YouDontHaveEnoughMagicForThat);
            return false;
        }

        var flag = Find(context.LeftArg);
        if (flag == null)
        {
            context.Notify(Feedback.NoSuchFlag);
            return false;
        }

        if (string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify(Feedback.WhatTypeDoYouWantToMakeThatFlag);
            return false;
        }

        if (!TryTypes(context, context.RightArg.Trim(), out ObjectType type))
            return false;

        if (type != ObjectType.None)
        {
            foreach (var obj in db.GetAllObjects())
            {
                if (obj.HasFlag(flag.Name) && !type.HasFlag(obj.Type))
                {
                    context.Notify(Feedback.ObjectsOfOtherTypesAlreadyHaveThisFlagSetSearchForThemAndRemove);
                    return false;
                }
            }
        }

        flag.TargetType = type;
        context.Notify(Feedback.TypeOfFlagSet, ("value", $"{flag.Name}"));
        return true;
    }

    private static bool Alias(CommandContext context, MUSHObject? actor)
    {
        if (!God(actor))
        {
            context.Notify(Feedback.YouDontLookLikeGod);
            return false;
        }

        string alias = context.RightArg.Trim();
        if (alias.Length == 0)
        {
            context.Notify(Feedback.YouMustProvideANameForTheAlias);
            return false;
        }

        bool remove = alias[0] == '!';
        if (remove)
            alias = alias[1..].Trim();
        var flag = Find(context.LeftArg);
        if (flag == null)
        {
            context.Notify(Feedback.IDontKnowThatFlag);
            return false;
        }

        if (flag.Permissions.HasFlag(FlagPrivileges.Disabled))
        {
            context.Notify(Feedback.ThatFlagIsDisabled);
            return false;
        }

        if (alias.Length <= 1)
        {
            context.Notify(Feedback.FlagAliasesMustBeLongerThanOneCharacter);
            return false;
        }

        if (alias.Contains(' '))
        {
            context.Notify(Feedback.FlagAliasesMayNotContainSpaces);
            return false;
        }

        if (!PennMUSH.Core.Attributes.AttributeEntry.IsValidName(alias))
        {
            context.Notify(Feedback.ThatsNotAValidFlagName);
            return false;
        }

        string canonical = alias.ToUpperInvariant();
        if (canonical.Equals(flag.Name, StringComparison.OrdinalIgnoreCase))
        {
            context.Notify(remove
                ? "That's the flag's name, not an alias."
                : $"That alias already matches the flag {flag.Name}.");
            return false;
        }

        var matched = FlagTable.FindFlag(canonical);
        if (remove)
        {
            if (matched == null || !matched.Name.Equals(flag.Name, StringComparison.OrdinalIgnoreCase) || !FlagTable.IsAlias(canonical))
            {
                context.Notify(Feedback.ThatIsntAnAliasOfTheFlag, ("flag", $"{flag.Name}"));
                return false;
            }

            FlagTable.RemoveAlias(canonical);
        }
        else
        {
            if (matched != null)
            {
                context.Notify(Feedback.ThatAliasAlreadyMatchesTheFlag, ("flag", $"{matched.Name}"));
                return false;
            }

            FlagTable.AddAlias(canonical, flag.Name);
        }

        Describe(context, flag);
        return true;
    }

    private static bool Letter(CommandContext context, MUSHObject? actor)
    {
        if (!God(actor))
        {
            context.Notify(Feedback.YouDontLookLikeGod);
            return false;
        }

        var flag = Find(context.LeftArg);
        if (flag == null)
        {
            context.Notify(Feedback.IDontKnowThatFlag);
            return false;
        }

        string raw = context.RightArg.Trim();
        if (raw.Length > 1)
        {
            context.Notify(Feedback.FlagCharactersMustBeSingleCharacters);
            return false;
        }

        char letter = raw.Length == 0 ? '\0' : raw[0];
        var conflict = FlagTable.LetterConflict(letter, flag.TargetType, flag);
        if (conflict != null)
        {
            context.Notify(Feedback.LetterConflictsWithTheFlag, ("flag", $"{conflict.Name}"));
            return false;
        }

        flag.Letter = letter;
        context.Notify(letter == '\0'
            ? $"Letter for flag {flag.Name} cleared."
            : $"Letter for flag {flag.Name} set to '{letter}'.");
        return true;
    }

    private static bool SetDisabled(CommandContext context, MUSHObject? actor, bool disable)
    {
        if (!God(actor))
        {
            context.Notify(Feedback.YouDontLookLikeGod);
            return false;
        }

        var flag = Find(context.LeftArg);
        if (flag == null)
        {
            context.Notify(Feedback.IDontKnowThatFlag);
            return false;
        }

        bool already = flag.Permissions.HasFlag(FlagPrivileges.Disabled);
        if (disable && already)
        {
            context.Notify(Feedback.ThatFlagIsAlreadyDisabled);
            return false;
        }

        if (!disable && !already)
        {
            context.Notify(Feedback.ThatFlagIsNotDisabled);
            return false;
        }

        flag.Permissions = disable
            ? flag.Permissions | FlagPrivileges.Disabled
            : flag.Permissions & ~FlagPrivileges.Disabled;
        context.Notify(disable ? $"Flag {flag.Name} disabled." : $"Flag {flag.Name} enabled.");
        return true;
    }

    private static bool Delete(CommandContext context, ObjectDatabase db, MUSHObject? actor)
    {
        if (!God(actor))
        {
            context.Notify(Feedback.YouDontLookLikeGod);
            return false;
        }

        var flag = Find(context.LeftArg);
        if (flag == null)
        {
            context.Notify(Feedback.IDontKnowThatFlag);
            return false;
        }

        if (flag.Permissions.HasFlag(FlagPrivileges.Internal))
        {
            context.Notify(Feedback.ThereAreProbablyEasierWaysToCrashYourMush);
            return false;
        }

        foreach (var obj in db.GetAllObjects())
            obj.RemoveFlag(flag.Name);
        string name = flag.Name;
        FlagTable.RemoveFlag(flag);
        context.Notify(Feedback.FlagDeleted, ("flag", $"{name}"));
        return true;
    }

    private static bool Debug(CommandContext context, MUSHObject? actor)
    {
        if (actor == null || !actor.IsWizard)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        var flags = FlagTable.Definitions;
        context.Notify(Feedback.FlagspaceNameFlag);
        context.Notify(Feedback.FlagCount, ("value", $"{flags.Count}"));
        for (int i = 0; i < flags.Count; i++)
            context.Notify($"Flag {i,2}: {flags[i].Name}. Bit position: {i}");
        return true;
    }

    private static bool Info(CommandContext context, MUSHObject? actor, string name)
    {
        var flag = Find(name);
        if (flag == null || (!God(actor) && flag.Permissions.HasFlag(FlagPrivileges.Disabled)))
        {
            context.Notify(Feedback.NoSuchFlag);
            return false;
        }

        Describe(context, flag);
        return true;
    }

    private static void Describe(CommandContext context, FlagDefinition flag)
    {
        context.Notify($"{"Name",9}: {flag.Name}");
        context.Notify($"{"Character",9}: {(flag.Letter == '\0' ? "" : flag.Letter.ToString())}");
        context.Notify($"{"Aliases",9}: {string.Join(' ', FlagTable.AliasesOf(flag.Name))}");
        context.Notify($"{"Type(s)",9}: {FormatTypes(flag.TargetType)}");
        context.Notify($"{"Perms",9}: {FormatPermissions(flag.Permissions)}");
        context.Notify($"{"ResetPrms",9}: {FormatPermissions(flag.NegatePermissions)}");
    }

    private static bool ValidNewName(CommandContext context, string name, bool alias)
    {
        if (name.Length == 0)
        {
            context.Notify(alias ? "You must provide a name for the alias." : "You must provide a name for the flag.");
            return false;
        }

        if (name.Contains(' '))
        {
            context.Notify(alias ? "Flag aliases may not contain spaces." : "Flag names may not contain spaces.");
            return false;
        }

        if (name.Length <= 1)
        {
            context.Notify(alias
                ? "Flag aliases must be longer than one character."
                : "Flag names must be longer than one character.");
            return false;
        }

        if (!PennMUSH.Core.Attributes.AttributeEntry.IsValidName(name))
        {
            context.Notify(Feedback.ThatsNotAValidFlagName);
            return false;
        }

        return true;
    }

    private static bool TryTypes(CommandContext context, string? text, out ObjectType type)
    {
        type = ObjectType.None;
        if (string.IsNullOrWhiteSpace(text) || text.Equals("any", StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (string token in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Equals("any", StringComparison.OrdinalIgnoreCase))
                continue;
            var match = TypeNames.FirstOrDefault(entry => entry.Name.Equals(token, StringComparison.OrdinalIgnoreCase));
            if (match.Name == null)
            {
                context.Notify(Feedback.IDontUnderstandTheListOfTypes);
                return false;
            }

            type |= match.Bit;
        }

        return true;
    }

    private static bool TryPermissions(CommandContext context, string? text, out FlagPrivileges perms)
    {
        perms = FlagPrivileges.None;
        if (string.IsNullOrWhiteSpace(text) || text.Equals("any", StringComparison.OrdinalIgnoreCase))
        {
            perms = FlagPrivileges.Any;
            return true;
        }

        foreach (string token in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Equals("any", StringComparison.OrdinalIgnoreCase))
            {
                perms |= FlagPrivileges.Any;
                continue;
            }

            var match = PermissionNames.FirstOrDefault(entry => entry.Name.Equals(token, StringComparison.OrdinalIgnoreCase));
            if (match.Name == null)
            {
                context.Notify(Feedback.IDontUnderstandThosePermissions);
                return false;
            }

            perms |= match.Bit;
        }

        if (perms == FlagPrivileges.None || perms.HasFlag(FlagPrivileges.Internal) || perms.HasFlag(FlagPrivileges.Disabled))
        {
            context.Notify(Feedback.IDontUnderstandThosePermissions);
            return false;
        }

        return true;
    }

    private static IEnumerable<FlagDefinition> Visible(MUSHObject? actor, string pattern)
    {
        bool god = God(actor);
        bool wizard = actor != null && actor.IsWizard;
        foreach (var flag in FlagTable.Definitions.OrderBy(flag => flag.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (flag.Permissions.HasFlag(FlagPrivileges.Internal))
                continue;
            if (!god && flag.Permissions.HasFlag(FlagPrivileges.Disabled))
                continue;
            if (!wizard && (flag.Permissions.HasFlag(FlagPrivileges.Dark) || flag.Permissions.HasFlag(FlagPrivileges.Mdark)))
                continue;
            if (!Wild(pattern, flag.Name))
                continue;
            yield return flag;
        }
    }

    private static string Decompile(MUSHObject? actor)
    {
        var lines = new List<string>();
        foreach (var flag in Visible(actor, ""))
        {
            int args = 0;
            var text = new System.Text.StringBuilder();
            text.Append("@flag/add ");
            text.Append(flag.Name);
            if (flag.Letter != '\0')
            {
                text.Append('=');
                text.Append(flag.Letter);
                args = 1;
            }

            const ObjectType all = ObjectType.Player | ObjectType.Thing | ObjectType.Room | ObjectType.Exit;
            if ((flag.TargetType & all) != all)
            {
                Pad(text, ref args, 2);
                text.Append(FormatTypes(flag.TargetType));
            }

            string perms = FormatPermissions(flag.Permissions);
            if (perms.Length > 0)
            {
                Pad(text, ref args, 3);
                text.Append(perms);
            }

            string negate = FormatPermissions(flag.NegatePermissions);
            if (negate.Length > 0)
            {
                Pad(text, ref args, 4);
                text.Append(negate);
            }

            lines.Add(text.ToString());
        }

        return lines.Count == 0 ? "" : "\n" + string.Join('\n', lines);
    }

    private static void Pad(System.Text.StringBuilder text, ref int args, int target)
    {
        if (args == 0)
        {
            text.Append('=');
            args++;
        }

        for (; args < target; args++)
            text.Append(',');
    }

    private static string FormatListEntry(FlagDefinition flag)
    {
        string text = flag.Letter == '\0' ? flag.Name : $"{flag.Name} ({flag.Letter})";
        if (flag.Permissions.HasFlag(FlagPrivileges.Disabled))
            text += " (disabled)";
        return text;
    }

    private static string FormatTypes(ObjectType type)
    {
        return string.Join(' ', TypeNames.Where(entry => type.HasFlag(entry.Bit)).Select(entry => entry.Name));
    }

    private static string FormatPermissions(FlagPrivileges perms)
    {
        return string.Join(' ', PermissionNames.Where(entry => perms.HasFlag(entry.Bit)).Select(entry => entry.Name));
    }

    private static bool Wild(string pattern, string name)
    {
        pattern = pattern.Trim();
        if (pattern.Length == 0)
            return true;
        return Wild(pattern.AsSpan(), name.AsSpan());
    }

    private static bool Wild(ReadOnlySpan<char> pattern, ReadOnlySpan<char> name)
    {
        int pi = 0;
        int ni = 0;
        int star = -1;
        int mark = -1;
        while (ni < name.Length)
        {
            if (pi < pattern.Length && (pattern[pi] == '?' || char.ToUpperInvariant(pattern[pi]) == char.ToUpperInvariant(name[ni])))
            {
                pi++;
                ni++;
            }
            else if (pi < pattern.Length && pattern[pi] == '*')
            {
                star = pi++;
                mark = ni;
            }
            else if (star >= 0)
            {
                pi = star + 1;
                ni = ++mark;
            }
            else
            {
                return false;
            }
        }

        while (pi < pattern.Length && pattern[pi] == '*')
            pi++;
        return pi == pattern.Length;
    }

    private static string[] Fields(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<string>();
        return text.Split(',').Select(field => field.Trim()).ToArray();
    }

    private static FlagDefinition? Find(string name)
    {
        name = name.Trim();
        return name.Length == 0 ? null : FlagTable.FindFlag(name);
    }

    private static bool God(MUSHObject? actor) => actor != null && actor.IsGod;
}
