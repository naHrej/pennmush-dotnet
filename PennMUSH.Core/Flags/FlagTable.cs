namespace PennMUSH.Core.Flags;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public static class FlagTable
{
    private static readonly Dictionary<string, FlagDefinition> FlagsByName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, FlagDefinition> PowersByName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> FlagAliases = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> PowerAliases = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<FlagDefinition> AllFlags = new();

    static FlagTable()
    {
        // Canonical Flags from hdrs/flag_tab.h
        RegisterFlag(new FlagDefinition { Name = "CHOWN_OK", Letter = 'C', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "DARK", Letter = 'D', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "GOING", Letter = 'G', TargetType = ObjectType.None, Permissions = FlagPrivileges.Internal, NegatePermissions = FlagPrivileges.Internal });
        RegisterFlag(new FlagDefinition { Name = "HAVEN", Letter = 'H', TargetType = ObjectType.Player, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "TRUST", Letter = 'I', TargetType = ObjectType.None, Permissions = FlagPrivileges.Inherit, NegatePermissions = FlagPrivileges.Inherit });
        RegisterFlag(new FlagDefinition { Name = "LINK_OK", Letter = 'L', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "OPAQUE", Letter = 'O', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "QUIET", Letter = 'Q', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "STICKY", Letter = 'S', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "UNFINDABLE", Letter = 'U', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "VISUAL", Letter = 'V', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "WIZARD", Letter = 'W', TargetType = ObjectType.None, Permissions = FlagPrivileges.Wizard | FlagPrivileges.Inherit | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard | FlagPrivileges.Inherit });
        RegisterFlag(new FlagDefinition { Name = "SAFE", Letter = 'X', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "AUDIBLE", Letter = 'a', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "DEBUG", Letter = 'b', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "NO_WARN", Letter = 'w', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "ENTER_OK", Letter = 'e', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "HALT", Letter = 'h', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "NO_COMMAND", Letter = 'n', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "LIGHT", Letter = 'l', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "ROYALTY", Letter = 'r', TargetType = ObjectType.None, Permissions = FlagPrivileges.Royalty | FlagPrivileges.Inherit | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Royalty | FlagPrivileges.Inherit });
        RegisterFlag(new FlagDefinition { Name = "TRANSPARENT", Letter = 't', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "VERBOSE", Letter = 'v', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "ANSI", Letter = 'A', TargetType = ObjectType.Player, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "COLOR", Letter = 'C', TargetType = ObjectType.Player, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "MONITOR", Letter = 'M', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "NOSPOOF", Letter = '"', TargetType = ObjectType.Player, Permissions = FlagPrivileges.Any | FlagPrivileges.Odark, NegatePermissions = FlagPrivileges.Any | FlagPrivileges.Odark });
        RegisterFlag(new FlagDefinition { Name = "SHARED", Letter = 'Z', TargetType = ObjectType.Player, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "GAGGED", Letter = 'g', TargetType = ObjectType.Player, Permissions = FlagPrivileges.Wizard, NegatePermissions = FlagPrivileges.Wizard });
        RegisterFlag(new FlagDefinition { Name = "MYOPIC", Letter = 'm', TargetType = ObjectType.Player, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "TERSE", Letter = 'x', TargetType = ObjectType.None, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "FIXED", Letter = 'F', TargetType = ObjectType.Player, Permissions = FlagPrivileges.Wizard, NegatePermissions = FlagPrivileges.Wizard });
        RegisterFlag(new FlagDefinition { Name = "SUSPECT", Letter = 's', TargetType = ObjectType.Player, Permissions = FlagPrivileges.Wizard | FlagPrivileges.Mdark | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard | FlagPrivileges.Mdark });
        RegisterFlag(new FlagDefinition { Name = "DESTROY_OK", Letter = 'd', TargetType = ObjectType.Thing, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "PUPPET", Letter = 'p', TargetType = ObjectType.Thing, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "NO_LEAVE", Letter = 'N', TargetType = ObjectType.Thing, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "ABODE", Letter = 'A', TargetType = ObjectType.Room, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "FLOATING", Letter = 'F', TargetType = ObjectType.Room, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "JUMP_OK", Letter = 'J', TargetType = ObjectType.Room, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "NO_TEL", Letter = 'N', TargetType = ObjectType.Room, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });
        RegisterFlag(new FlagDefinition { Name = "CLOUDY", Letter = 'x', TargetType = ObjectType.Exit, Permissions = FlagPrivileges.Any, NegatePermissions = FlagPrivileges.Any });

        // Aliases from hdrs/flag_tab.h
        FlagAliases["INHERIT"] = "TRUST";
        FlagAliases["TRACE"] = "DEBUG";
        FlagAliases["NOWARN"] = "NO_WARN";
        FlagAliases["NOCOMMAND"] = "NO_COMMAND";
        FlagAliases["LISTENER"] = "MONITOR";
        FlagAliases["WATCHER"] = "MONITOR";
        FlagAliases["ZONE"] = "SHARED";
        FlagAliases["COLOUR"] = "COLOR";
        FlagAliases["DEST_OK"] = "DESTROY_OK";
        FlagAliases["TEL_OK"] = "JUMP_OK";
        FlagAliases["TELOK"] = "JUMP_OK";
        FlagAliases["TEL-OK"] = "JUMP_OK";

        // Powers from hdrs/flag_tab.h
        RegisterPower(new FlagDefinition { Name = "Announce", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Boot", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Builder", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Chat_Privs", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Functions", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Guest", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Halt", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Hide", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Idle", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Immortal", Permissions = FlagPrivileges.Wizard, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Link_Anywhere", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Login", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Long_Fingers", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "No_Pay", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "No_Quota", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Open_Anywhere", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Pemit_All", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Player_Create", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Poll", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Queue", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Quotas", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Search", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "See_All", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "See_Queue", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Tport_Anything", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Tport_Anywhere", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });
        RegisterPower(new FlagDefinition { Name = "Can_spoof", Permissions = FlagPrivileges.Wizard | FlagPrivileges.Log, NegatePermissions = FlagPrivileges.Wizard, IsPower = true });

        // Power aliases
        PowerAliases["@wall"] = "Announce";
        PowerAliases["wall"] = "Announce";
        PowerAliases["Can_nspemit"] = "Can_spoof";
    }

    private static void RegisterFlag(FlagDefinition def)
    {
        FlagsByName[def.Name] = def;
        AllFlags.Add(def);
    }

    private static void RegisterPower(FlagDefinition def)
    {
        PowersByName[def.Name] = def;
    }

    public static IReadOnlyList<FlagDefinition> Definitions => AllFlags;
    public static IEnumerable<string> PowerNames => PowersByName.Keys;

    public static FlagDefinition? FindFlag(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (FlagAliases.TryGetValue(name, out var aliasTarget))
            name = aliasTarget;
        return FlagsByName.TryGetValue(name, out var def) ? def : null;
    }

    public static FlagDefinition? FindFlagByLetter(char letter, ObjectType type)
    {
        foreach (var def in AllFlags)
        {
            if (def.Letter == letter)
            {
                if (def.TargetType == ObjectType.None || def.TargetType == type)
                    return def;
            }
        }
        return null;
    }

    public static FlagDefinition? FindPower(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (PowerAliases.TryGetValue(name, out var aliasTarget))
            name = aliasTarget;
        return PowersByName.TryGetValue(name, out var def) ? def : null;
    }

    public static bool CanSetFlag(MUSHObject executor, MUSHObject target, FlagDefinition flag, bool negate, out string error)
    {
        error = string.Empty;
        var perms = negate ? flag.NegatePermissions : flag.Permissions;

        if (perms.HasFlag(FlagPrivileges.Internal))
        {
            error = "Only the game can set that flag.";
            return false;
        }

        if (perms.HasFlag(FlagPrivileges.Disabled))
        {
            error = "That flag is disabled.";
            return false;
        }

        if (flag.TargetType != ObjectType.None && flag.TargetType != target.Type)
        {
            error = "That flag cannot be set on that type of object.";
            return false;
        }

        if (executor.IsGod)
            return true;

        if (perms.HasFlag(FlagPrivileges.Wizard) && !executor.IsWizard)
        {
            error = "Permission denied.";
            return false;
        }

        if (perms.HasFlag(FlagPrivileges.Royalty) && !executor.IsRoyalty)
        {
            error = "Permission denied.";
            return false;
        }

        if (perms.HasFlag(FlagPrivileges.Owned) && executor.Id != target.Owner && !executor.IsWizard)
        {
            error = "Permission denied.";
            return false;
        }

        return true;
    }
}
