namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class ZoneCommandHandler : ICommandHandler
{
    public string Name => "@zone";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg))
        {
            context.Notify(Feedback.WhatObjectDoYouWantToZone);
            return false;
        }

        Dbref targetRef = ResolveDbref(context.LeftArg, context.Executor);
        var targetObj = db.GetObject(targetRef);
        if (targetObj == null)
        {
            context.Notify(Feedback.ICantSeeThatHere);
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || (!execObj.IsWizard && execObj.Id != targetObj.Owner))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        if (string.IsNullOrWhiteSpace(context.RightArg) || context.RightArg.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            targetObj.ClearZones();
            context.Notify(Feedback.ZoneCleared);
            return true;
        }

        var tokens = context.RightArg.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var validZones = new List<Dbref>();

        foreach (var token in tokens)
        {
            Dbref zRef = ResolveDbref(token, context.Executor);
            var zObj = db.GetObject(zRef);
            if (zObj == null)
            {
                context.Notify(Feedback.ICantSeeZoneObject, ("object", $"{token}"));
                return false;
            }

            validZones.Add(zRef);
        }

        targetObj.SetZones(validZones);
        context.Notify(Feedback.ZoneSet);
        return true;
    }

    private static Dbref ResolveDbref(string s, Dbref executor)
    {
        if (s.Equals("me", StringComparison.OrdinalIgnoreCase)) return executor;
        if (s.StartsWith('#') && int.TryParse(s.Substring(1), out int id)) return new Dbref(id);
        if (int.TryParse(s, out int plainId)) return new Dbref(plainId);
        return Dbref.Nothing;
    }
}

internal static class ZoneOperations
{
    public static bool Controls(MUSHObject actor, MUSHObject target) =>
        actor.IsWizard || actor.Id == target.Id || target.Owner == actor.Id;

    public static bool WouldLoop(Dbref thing, Dbref zone, ObjectDatabase db)
    {
        var seen = new HashSet<Dbref>();
        var pending = new Queue<Dbref>();
        pending.Enqueue(zone);
        while (pending.Count > 0)
        {
            Dbref id = pending.Dequeue();
            if (!seen.Add(id))
                continue;
            if (id == thing)
                return true;
            var obj = db.GetObject(id);
            if (obj == null)
                continue;
            foreach (Dbref next in obj.Zones)
                pending.Enqueue(next);
        }
        return false;
    }

    public static string? MoveToZone(CommandContext context, ObjectDatabase db, MUSHObject thing, MUSHObject? zone, bool preserve)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return Feedback.PermissionDenied.Text();
        if (!Controls(actor, thing))
            return Feedback.YouDontControlThat.Text();
        if (zone == null)
        {
            thing.ClearZones();
            return null;
        }
        if (WouldLoop(thing.Id, zone.Id, db))
            return Feedback.ThatWouldCauseAZoneLoop.Text();
        if (!Controls(actor, zone) && !zone.EvalLock(LockType.Chzone, context.Executor, db))
            return Feedback.PermissionDenied.Text();

        bool keepPrivileges = preserve && actor.IsWizard;
        if (thing.Type != ObjectType.Player && !keepPrivileges)
        {
            thing.RemoveFlag("WIZARD");
            thing.RemoveFlag("ROYALTY");
            thing.RemoveFlag("TRUST");
            foreach (string power in thing.Powers.ToList())
                thing.RemovePower(power);
        }

        thing.Zone = zone.Id;
        if (zone.GetLock(LockType.Zone) == null && zone.Owner.IsValid)
            zone.SetLock(LockType.Zone, new DbrefLockNode(zone.Owner), context.Executor);
        return null;
    }

    public static MUSHObject? Find(ObjectDatabase db, Dbref executor, string name)
    {
        name = name.Trim();
        if (name.Equals("me", StringComparison.OrdinalIgnoreCase))
            return db.GetObject(executor);
        if (name.StartsWith('#') && int.TryParse(name.Substring(1), out int id))
            return db.GetObject(new Dbref(id));
        return db.MatchObject(executor, name);
    }
}

public class ChzoneCommandHandler : ICommandHandler
{
    public string Name => "@chzone";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg))
        {
            context.Notify(Feedback.UsageChzoneObjectZone);
            return false;
        }

        var thing = ZoneOperations.Find(db, context.Executor, context.LeftArg);
        if (thing == null)
        {
            context.Notify(Feedback.ICantSeeThatHere);
            return false;
        }

        string zoneName = context.RightArg.Trim();
        MUSHObject? zone = null;
        if (zoneName.Length > 0 && !zoneName.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            zone = ZoneOperations.Find(db, context.Executor, zoneName);
            if (zone == null)
            {
                context.Notify(Feedback.ICantSeeThatZone);
                return false;
            }
        }

        string? error = ZoneOperations.MoveToZone(context, db, thing, zone, context.Switches.Contains("preserve"));
        if (error != null)
        {
            context.Notify(error);
            return false;
        }

        context.Notify(zone == null ? Feedback.ZoneCleared : Feedback.ZoneSet);
        return true;
    }
}

public class ChzoneAllCommandHandler : ICommandHandler
{
    public string Name => "@chzoneall";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.IsWizard)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify(Feedback.UsageChzoneallPlayerZone);
            return false;
        }

        var owner = db.FindPlayer(context.LeftArg.Trim()) ?? ZoneOperations.Find(db, context.Executor, context.LeftArg);
        if (owner == null || owner.Type != ObjectType.Player)
        {
            context.Notify(Feedback.ICantFindThatPlayer);
            return false;
        }

        string zoneName = context.RightArg.Trim();
        MUSHObject? zone = null;
        if (!zoneName.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            zone = ZoneOperations.Find(db, context.Executor, zoneName);
            if (zone == null)
            {
                context.Notify(Feedback.ICantSeeThatZone);
                return false;
            }
        }

        bool preserve = context.Switches.Contains("preserve");
        int changed = 0;
        int skipped = 0;
        foreach (var obj in db.ByOwner(owner.Id).ToList())
        {
            if (ZoneOperations.MoveToZone(context, db, obj, zone, preserve) == null)
                changed++;
            else
                skipped++;
        }

        if (skipped == 0)
            context.Notify(Feedback.ZoneUpdatedOnObjects, ("value", $"{changed}"));
        else
            context.Notify(Feedback.ZoneUpdatedOnObjectsWereLeftUnchanged, ("value", $"{changed}"), ("value2", $"{skipped}"));
        return changed > 0;
    }
}

public class ZemitCommandHandler : ICommandHandler
{
    public string Name => "@zemit";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify(Feedback.UsageZemitZoneMessage);
            return false;
        }

        var actor = db.GetObject(context.Executor);
        if (actor == null)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        var zone = ZoneOperations.Find(db, context.Executor, context.LeftArg);
        if (zone == null)
        {
            context.Notify(Feedback.ICantSeeThatZone);
            return false;
        }
        if (!ZoneOperations.Controls(actor, zone))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        var rooms = db.ByZone(zone.Id).Where(obj => obj.Type == ObjectType.Room).ToList();
        if (rooms.Count == 0)
        {
            context.Notify(Feedback.NoRoomsAreInThatZone);
            return false;
        }

        string message = context.RightArg;
        bool spoofable = !SpoofPrefix.Omits(actor, context.CommandName);
        foreach (var room in rooms)
            context.NotifyRoom(room.Id, message, actor.Id, spoofable);

        bool noisy = context.Switches.Contains("noisy");
        bool silent = context.Switches.Contains("silent");
        bool hears = actor.Location.IsValid && rooms.Any(room => room.Id == actor.Location);
        if (noisy || (!silent && !hears))
            context.Notify(Feedback.YouZemitTo, ("value", $"{zone.Name}"));
        return true;
    }
}
