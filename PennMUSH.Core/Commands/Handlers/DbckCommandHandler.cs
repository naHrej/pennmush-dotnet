namespace PennMUSH.Core.Commands.Handlers;

using System.Collections.Generic;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Types;

public class DbckCommandHandler : ICommandHandler
{
    public string Name => "@dbck";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;
        if (!actor.IsWizard)
        {
            context.Notify("Silly mortal, chicks are for kids!");
            return false;
        }

        context.Notify("GAME: Performing database consistency check.");
        Repair(context, db);
        context.Notify("GAME: Database consistency check complete.");
        return true;
    }

    private static void Repair(CommandContext context, ObjectDatabase db)
    {
        var god = db.GetObject(new Dbref(1));
        Dbref godId = god != null && god.Type == ObjectType.Player ? god.Id : new Dbref(1);
        var home = db.GetObject(new Dbref(0));
        Dbref defaultHome = home != null && home.Type == ObjectType.Room ? home.Id : Dbref.Nothing;
        var objects = new List<MUSHObject>(db.GetAllObjects());

        foreach (var obj in objects)
        {
            if (obj.Type == ObjectType.Garbage)
            {
                if (obj.Next.IsValid && db.GetObject(obj.Next)?.Type != ObjectType.Garbage)
                    obj.Next = Dbref.Nothing;
                continue;
            }

            var zone = obj.Zone.IsValid ? db.GetObject(obj.Zone) : null;
            if (zone == null || zone.Type == ObjectType.Garbage)
                obj.Zone = Dbref.Nothing;
            var parent = obj.Parent.IsValid ? db.GetObject(obj.Parent) : null;
            if (obj.Parent.IsValid && (parent == null || parent.Type == ObjectType.Garbage))
                obj.Parent = Dbref.Nothing;

            var owner = db.GetObject(obj.Owner);
            if (owner == null || owner.Type != ObjectType.Player)
                obj.Owner = godId;

            if (obj.Next.IsValid && (db.GetObject(obj.Next) == null || db.GetObject(obj.Next)!.Type == ObjectType.Garbage))
                obj.Next = Dbref.Nothing;

            foreach (var attr in obj.Attributes)
            {
                var creator = db.GetObject(attr.Creator);
                if (creator == null || creator.Type == ObjectType.Garbage)
                    attr.Creator = godId;
            }

            if (obj.Type is ObjectType.Player or ObjectType.Thing)
            {
                var objHome = db.GetObject(obj.Home);
                if (objHome == null || objHome.Type == ObjectType.Garbage || objHome.Type == ObjectType.Exit)
                    obj.Home = defaultHome;
                var loc = db.GetObject(obj.Location);
                if (loc == null || loc.Type == ObjectType.Garbage || loc.Type == ObjectType.Exit)
                    db.MoveObject(obj.Id, obj.Home);
            }
            else if (obj.Type == ObjectType.Exit)
            {
                if (obj.Contents.IsValid)
                    obj.Contents = Dbref.Nothing;
                var dest = db.GetObject(obj.Location);
                if (dest != null && dest.Type == ObjectType.Garbage)
                    obj.Location = RoomSource(db, obj) ?? obj.Home;
                var source = db.GetObject(obj.Home);
                if (source == null || source.Type != ObjectType.Room)
                    db.DestroyObject(obj.Id);
            }
            else if (obj.Type == ObjectType.Room)
            {
                var dropto = db.GetObject(obj.Location);
                if (dropto != null && (dropto.Type == ObjectType.Garbage || dropto.Type == ObjectType.Exit))
                    obj.Location = Dbref.Nothing;
            }
        }

        var warned = new HashSet<int>();
        foreach (var obj in db.GetAllObjects())
        {
            if (obj.Type == ObjectType.Garbage || !obj.Zone.IsValid || obj.Zone == obj.Id)
                continue;
            var seen = new HashSet<int> { obj.Id.Value };
            var cursor = db.GetObject(obj.Zone);
            while (cursor != null && cursor.Zone.IsValid && cursor.Zone != cursor.Id)
            {
                if (!seen.Add(cursor.Id.Value))
                {
                    if (warned.Add(obj.Id.Value))
                        context.NotifyTarget(obj.Owner, $"You own an object in a circular zone chain: {obj.Name}(#{obj.Id.Value})");
                    break;
                }
                if (cursor.Id == obj.Id)
                    break;
                cursor = db.GetObject(cursor.Zone);
            }
        }

        var zones = new HashSet<int>();
        foreach (var obj in db.GetAllObjects())
        {
            if (obj.Type != ObjectType.Garbage && obj.Zone.IsValid)
                zones.Add(obj.Zone.Value);
        }
        foreach (int id in zones)
        {
            var zone = db.GetObject(new Dbref(id));
            if (zone != null && zone.Type != ObjectType.Garbage && zone.GetLock(LockType.Zone) == null)
                context.NotifyTarget(zone.Owner, $"You own an object without a @lock/zone being used as a zone: {zone.Name}(#{zone.Id.Value})");
        }

        foreach (var room in db.GetAllObjects())
        {
            if (room.Type != ObjectType.Room || room.Id.Value == 0 || room.HasFlag("GOING") || room.HasFlag("FLOATING") || room.Exits.IsValid)
                continue;
            context.NotifyTarget(room.Owner, $"You own a disconnected room, {room.Name}(#{room.Id.Value})");
        }
    }

    private static Dbref? RoomSource(ObjectDatabase db, MUSHObject exit)
    {
        var source = db.GetObject(exit.Exits);
        if (source != null && source.Type == ObjectType.Room)
            return source.Id;
        var home = db.GetObject(exit.Home);
        if (home != null && home.Type == ObjectType.Room)
            return home.Id;
        return null;
    }
}
