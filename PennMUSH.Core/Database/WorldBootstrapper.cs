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
        god.SetAttribute("DESCRIBE", "You see Number One.", god.Id, PennMUSH.Core.Attributes.StandardAttributes.DefaultFlags("DESCRIBE"), wizardOverride: true);
        god.SetAttribute("LAST", "Wed Jan 01 00:00:00 1970", god.Id, PennMUSH.Core.Attributes.StandardAttributes.DefaultFlags("LAST"), wizardOverride: true);
        god.SetAttribute("LASTFAILED", " ", god.Id, PennMUSH.Core.Attributes.StandardAttributes.DefaultFlags("LASTFAILED"), wizardOverride: true);
        god.SetAttribute("LASTIP", "127.0.0.1", god.Id, PennMUSH.Core.Attributes.StandardAttributes.DefaultFlags("LASTIP"), wizardOverride: true);
        god.SetAttribute("LASTSITE", "127.0.0.1", god.Id, PennMUSH.Core.Attributes.StandardAttributes.DefaultFlags("LASTSITE"), wizardOverride: true);
        god.SetAttribute("MAILCURF", "0", god.Id, PennMUSH.Core.Attributes.StandardAttributes.DefaultFlags("MAILCURF"), wizardOverride: true);
        god.SetAttribute("MAILFOLDERS", "0:INBOX:0 ", god.Id, PennMUSH.Core.Attributes.StandardAttributes.DefaultFlags("MAILFOLDERS"), wizardOverride: true);

        // Ensure room zero is owned by God
        roomZero.Owner = god.Id;

        // #2 - Master Room
        var masterRoom = db.CreateObject("Master Room", ObjectType.Room, god.Id);
        masterRoom.Location = roomZero.Id;
        masterRoom.SetAttribute("DESCRIBE", "The Master Room. Actions and exits here are global.", god.Id);

        // #3 - System Object (global $name registry)
        var sysObj = db.CreateObject("System Object", ObjectType.Thing, god.Id);
        db.MoveObject(sysObj.Id, masterRoom.Id);
        sysObj.Home = masterRoom.Id;
        sysObj.AddFlag("SAFE");
        sysObj.AddFlag("VISUAL");
        sysObj.AddFlag("NO_COMMAND");
        sysObj.SetAttribute("DESCRIBE", "Global object registry. Set attributes like NAME=#dbref to register $name references.", god.Id);
        sysObj.SetAttribute("SYSTEM", sysObj.Id.ToString(), god.Id);

        return god;
    }
}
