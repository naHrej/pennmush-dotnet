namespace PennMUSH.Core.Commands;

using System;
using System.Linq;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public static class PlayerCreator
{
    public static Dbref Create(
        ObjectDatabase db,
        MUSHObject actor,
        string name,
        string password,
        string? reuse,
        IPlayerPasswords? passwords,
        Dbref home,
        Dbref start,
        int pennies,
        Action<string> tell)
    {
        if (!actor.IsWizard && !actor.HasPower("Player_Create"))
        {
            tell("You do not have the power over body and mind!");
            return Dbref.Nothing;
        }

        if (!OkName(name))
        {
            tell($"Failure creating '{name}' (bad name)");
            return Dbref.Nothing;
        }
        if (db.FindPlayer(name) != null)
        {
            tell($"Failure creating '{name}' (name in use)");
            return Dbref.Nothing;
        }
        if (!OkPassword(password))
        {
            tell($"Failure creating '{name}' (bad password)");
            return Dbref.Nothing;
        }

        MUSHObject player;
        if (!string.IsNullOrWhiteSpace(reuse))
        {
            if (!actor.IsWizard)
            {
                tell("Permission denied.");
                return Dbref.Nothing;
            }
            if (!Dbref.TryParse(reuse.Trim(), out var reused))
            {
                tell("That is not a valid dbref.");
                return Dbref.Nothing;
            }
            var existing = db.GetObject(reused);
            if (existing == null || existing.Type != ObjectType.Garbage)
            {
                tell("That is not a valid dbref.");
                return Dbref.Nothing;
            }
            player = existing;
        }
        else
        {
            player = db.CreateObject(name, ObjectType.Player, Dbref.Nothing);
        }

        player.Name = name;
        player.Type = ObjectType.Player;
        player.Owner = player.Id;
        player.Home = home;
        player.Pennies = pennies;
        player.Warnings = WarningBits.Normal;
        player.RemoveFlag("GOING");
        passwords?.Set(player.Id, password);
        if (db.GetObject(start) != null)
            db.MoveObject(player.Id, start);
        else
            player.Location = start;

        tell($"New player '{player.Name}' ({player.Id}) created with password '{password}'");
        return player.Id;
    }

    public static bool OkName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 256)
            return false;
        if (char.IsWhiteSpace(name[0]) || char.IsWhiteSpace(name[^1]))
            return false;
        if (name.Equals("me", StringComparison.OrdinalIgnoreCase)
            || name.Equals("here", StringComparison.OrdinalIgnoreCase)
            || name.Equals("home", StringComparison.OrdinalIgnoreCase))
            return false;
        if (name[0] is '#' or '*')
            return false;
        const string extra = " `$_-.,'";
        return name.All(ch => char.IsLetterOrDigit(ch) || extra.Contains(ch));
    }

    public static bool OkPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;
        return password.All(ch => !char.IsWhiteSpace(ch) && !char.IsControl(ch) && ch != '=');
    }
}
