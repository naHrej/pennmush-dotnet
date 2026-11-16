namespace PennMUSH.Core.Database;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public static class WorldBootstrapper
{
    public static MUSHObject GenerateBaseWorld(ObjectDatabase db)
    {
        db.Clear();

        // #0 - Room Zero (Limbo)
        var roomZero = db.CreateObject("Room Zero", ObjectType.Room, new Dbref(1));
        roomZero.SetAttribute("DESCRIBE", "You are in Room Zero.", new Dbref(1));

        // #1 - God (Master Wizard / One)
        var god = db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.Location = roomZero.Id;
        god.Home = roomZero.Id;
        god.Pennies = 1000;
        god.Warnings = WarningBits.Normal;
        god.AddFlag("WIZARD");

        // Ensure room zero is owned by God
        roomZero.Owner = god.Id;

        // #2 - Master Room
        var masterRoom = db.CreateObject("Master Room", ObjectType.Room, god.Id);
        masterRoom.Location = roomZero.Id;
        masterRoom.SetAttribute("DESCRIBE", "The Master Room. Actions and exits here are global.", god.Id);

        return god;
    }
}
