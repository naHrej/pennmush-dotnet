namespace PennMUSH.Core.Commands.Handlers;

using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Types;

public class WcheckCommandHandler : ICommandHandler
{
    public string Name => "@wcheck";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        if (context.Switches.Contains("all"))
        {
            if (!actor.IsWizard)
            {
                context.Notify(Feedback.YoudBetterCheckYourWizbitFirst);
                return false;
            }
            context.Notify(Feedback.RunningDatabaseTopologyWarningChecks);
            foreach (var obj in db.GetAllObjects())
            {
                if (obj.Type == ObjectType.Garbage)
                    continue;
                var owner = db.GetObject(obj.Owner);
                if (owner == null || !db.IsConnected(owner.Id) || owner.HasFlag("NOWARN"))
                    continue;
                Check(context, db, owner, obj);
            }
            context.Notify(Feedback.WarningChecksComplete);
            return true;
        }

        if (context.Switches.Contains("me"))
        {
            if (!db.IsConnected(actor.Id))
                return false;
            foreach (var obj in db.GetAllObjects())
            {
                if (obj.Type != ObjectType.Garbage && obj.Owner == actor.Id)
                    Check(context, db, actor, obj);
            }
            context.Notify(Feedback.WcheckComplete);
            return true;
        }

        string name = (context.LeftArg.Length > 0 ? context.LeftArg : context.Arguments).Trim();
        var match = NearbyMatch.Find(db, actor, name, out bool ambiguous);
        if (ambiguous)
        {
            context.Notify(Feedback.IDontKnowWhichOneYouMean);
            return false;
        }
        if (match == null)
        {
            context.Notify(Feedback.IDontSeeThatObject);
            return false;
        }
        if (!(SeesAll(actor) || actor.Owner == match.Owner))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }
        if (match.Type == ObjectType.Garbage)
        {
            context.Notify(Feedback.WhyWouldYouWantToBeWarnedAboutGarbage);
            return false;
        }

        Check(context, db, actor, match);
        context.Notify(Feedback.WcheckComplete);
        return true;
    }

    private static bool SeesAll(MUSHObject actor) =>
        actor.IsRoyalty || actor.HasPower("SEE_ALL") || actor.HasPower("See_All");

    private static void Check(CommandContext context, ObjectDatabase db, MUSHObject checker, MUSHObject obj)
    {
        if (obj.HasFlag("GOING") || obj.HasFlag("NOWARN"))
            return;

        uint flags = checker.Owner == obj.Owner
            ? (obj.Warnings != 0 ? obj.Warnings : checker.Warnings)
            : checker.Warnings;
        if (flags == 0)
            return;

        if ((flags & WarningBits.LockProbs) != 0)
        {
            foreach (var entry in obj.Locks)
            {
                if (LockRefersToGarbage(entry.Key, db))
                    Complain(context, obj, "lock-checks", $"lock {entry.Type} refers to an invalid object");
            }
        }

        bool described = obj.GetAttribute("DESCRIBE", db) != null;
        if (obj.Type == ObjectType.Room && (flags & WarningBits.RoomDesc) != 0 && !described)
            Complain(context, obj, "room-desc", "room has no description");
        if (obj.Type == ObjectType.Player && (flags & WarningBits.PlayerDesc) != 0 && !described)
            Complain(context, obj, "my-desc", "player is missing description");
        if (obj.Type == ObjectType.Thing && obj.Location != checker.Id)
        {
            if ((flags & WarningBits.ThingDesc) != 0 && !described)
                Complain(context, obj, "thing-desc", "thing is missing description");
            if ((flags & WarningBits.ThingMsgs) != 0)
                MessageCheck(context, db, obj, "thing-msgs", "possibly unlocked thing missing succ/osucc/drop/odrop", requireDrop: true);
        }
        if (obj.Type == ObjectType.Exit)
            CheckExit(context, db, obj, flags);
    }

    private static void CheckExit(CommandContext context, ObjectDatabase db, MUSHObject exit, uint flags)
    {
        if ((flags & WarningBits.ExitUnlinked) != 0 && !exit.Location.IsValid)
            Complain(context, exit, "exit-unlinked", "exit is unlinked; anyone can steal it");
        if (exit.HasFlag("DARK"))
            return;
        if ((flags & WarningBits.ExitMsgs) != 0)
            MessageCheck(context, db, exit, "exit-msgs", "possibly unlocked exit missing succ/osucc/odrop", requireDrop: false);
        if ((flags & WarningBits.ExitDesc) != 0 && exit.GetAttribute("DESCRIBE", db) == null)
            Complain(context, exit, "exit-desc", "exit is missing description");

        var source = Source(db, exit);
        var destination = exit.Location.IsValid ? db.GetObject(exit.Location) : null;
        if (source == null || source.Type != ObjectType.Room || destination == null || destination.Type != ObjectType.Room || source.Id == destination.Id)
            return;

        int count = 0;
        foreach (var back in ExitsOf(db, destination))
        {
            if (back.Location == source.Id)
                count++;
        }
        if (count <= 1 && (flags & WarningBits.ExitOneway) != 0 && count == 0)
            Complain(context, exit, "exit-oneway", "exit has no return exit");
        else if (count > 1 && (flags & WarningBits.ExitMultiple) != 0)
            Complain(context, exit, "exit-multiple", $"exit has multiple ({count}) return exits");
    }

    private static void MessageCheck(CommandContext context, ObjectDatabase db, MUSHObject obj, string name, string unlocked, bool requireDrop)
    {
        bool locked = obj.GetLock(LockType.Basic) != null;
        if (!locked)
        {
            bool missing = obj.GetAttribute("SUCCESS", db) == null
                || obj.GetAttribute("OSUCCESS", db) == null
                || obj.GetAttribute("ODROP", db) == null
                || (requireDrop && obj.GetAttribute("DROP", db) == null);
            if (missing)
                Complain(context, obj, name, unlocked);
        }
        else if (obj.GetAttribute("FAILURE", db) == null)
        {
            Complain(context, obj, name, name == "exit-msgs"
                ? "possibly locked exit missing fail"
                : "possibly locked thing missing fail");
        }
    }

    private static System.Collections.Generic.IEnumerable<MUSHObject> ExitsOf(ObjectDatabase db, MUSHObject room)
    {
        var next = room.Exits;
        while (next.IsValid)
        {
            var exit = db.GetObject(next);
            if (exit == null)
                yield break;
            yield return exit;
            next = exit.Next;
        }
    }

    private static MUSHObject? Source(ObjectDatabase db, MUSHObject exit)
    {
        if (exit.Home.IsValid)
        {
            var home = db.GetObject(exit.Home);
            if (home != null && home.Type == ObjectType.Room)
                return home;
        }
        if (exit.Exits.IsValid)
        {
            var source = db.GetObject(exit.Exits);
            if (source != null && source.Type == ObjectType.Room)
                return source;
        }
        return null;
    }

    private static bool LockRefersToGarbage(LockNode node, ObjectDatabase db) => node switch
    {
        DbrefLockNode dbref => !db.Contains(dbref.Target) || db.GetObject(dbref.Target)?.Type == ObjectType.Garbage,
        NotLockNode not => LockRefersToGarbage(not.Child, db),
        AndLockNode and => LockRefersToGarbage(and.Left, db) || LockRefersToGarbage(and.Right, db),
        OrLockNode or => LockRefersToGarbage(or.Left, db) || LockRefersToGarbage(or.Right, db),
        _ => false
    };

    private static void Complain(CommandContext context, MUSHObject obj, string name, string detail)
    {
        context.Notify(Feedback.WarningFor, ("value", $"{name}"), ("value2", $"{obj.Name}"), ("value3", $"{obj.Id.Value}"));
        context.Notify(detail);
    }
}
