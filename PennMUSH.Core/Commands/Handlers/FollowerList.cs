namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

internal static class FollowerList
{
    public const string Followers = "FOLLOWERS";
    public const string Following = "FOLLOWING";
    private static readonly Dbref God = new(1);

    public static List<Dbref> Read(MUSHObject obj, string attribute)
    {
        var entry = obj.GetAttribute(attribute);
        var found = new List<Dbref>();
        if (entry == null || string.IsNullOrWhiteSpace(entry.Value))
            return found;

        foreach (string token in entry.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (token.StartsWith('#') && Dbref.TryParse(token, out var id) && id.IsValid)
                found.Add(id);
        }

        return found;
    }

    public static bool IsFollowing(MUSHObject follower, Dbref leader)
        => Read(follower, Following).Contains(leader);

    public static void Add(MUSHObject leader, MUSHObject follower)
    {
        Append(leader, Followers, follower.Id);
        Append(follower, Following, leader.Id);
    }

    public static void Delete(MUSHObject leader, MUSHObject follower)
    {
        Remove(leader, Followers, follower.Id);
        Remove(follower, Following, leader.Id);
    }

    public static void ClearFollowers(MUSHObject leader, ObjectDatabase db, CommandContext context, bool noisy)
    {
        var entry = leader.GetAttribute(Followers);
        if (entry == null)
            return;

        foreach (var id in Read(leader, Followers))
        {
            var follower = db.GetObject(id);
            if (follower == null)
                continue;
            Remove(follower, Following, leader.Id);
            if (noisy)
                context.NotifyTarget(follower.Id, $"You stop following {leader.Name}.");
        }

        leader.ClearAttribute(Followers);
    }

    public static void ClearFollowing(MUSHObject follower, ObjectDatabase db, CommandContext context, bool noisy)
    {
        var entry = follower.GetAttribute(Following);
        if (entry == null)
            return;

        foreach (var id in Read(follower, Following))
        {
            var leader = db.GetObject(id);
            if (leader == null)
                continue;
            Remove(leader, Followers, follower.Id);
            if (noisy)
                context.NotifyTarget(leader.Id, $"{follower.Name} stops following you.");
        }

        follower.ClearAttribute(Following);
    }

    public static string Names(ObjectDatabase db, IEnumerable<Dbref> ids)
    {
        var names = new List<string>();
        foreach (var id in ids)
        {
            var obj = db.GetObject(id);
            if (obj != null)
                names.Add(obj.Name);
        }

        return string.Join(", ", names);
    }

    public static string Spoken(MUSHObject source, string attribute, string fallback)
    {
        var entry = source.GetAttribute(attribute);
        if (entry == null || string.IsNullOrWhiteSpace(entry.Value))
            return fallback;
        return entry.Value;
    }

    public static void TellOthers(CommandContext context, ObjectDatabase db, MUSHObject actor, MUSHObject other, string attribute)
    {
        var entry = other.GetAttribute(attribute);
        if (entry == null || string.IsNullOrWhiteSpace(entry.Value) || !actor.Location.IsValid)
            return;

        foreach (var neighbor in db.GetContents(actor.Location))
        {
            if (neighbor.Id != actor.Id && neighbor.Id != other.Id)
                context.NotifyTarget(neighbor.Id, entry.Value);
        }
    }

    public static bool TryMatch(ObjectDatabase db, MUSHObject actor, string raw, bool nearbyOnly, out MUSHObject? match, out bool ambiguous)
    {
        match = null;
        ambiguous = false;
        string name = raw.Trim();
        if (name.StartsWith('*'))
            name = name.Substring(1).Trim();
        if (name.Length == 0)
            return false;

        if (name.Equals("me", StringComparison.OrdinalIgnoreCase))
        {
            match = actor;
            return true;
        }

        if (nearbyOnly && name.Equals("here", StringComparison.OrdinalIgnoreCase))
        {
            match = actor.Location.IsValid ? db.GetObject(actor.Location) : null;
            return match != null;
        }

        if (name.StartsWith('#'))
        {
            if (!Dbref.TryParse(name, out var id))
                return false;
            var absolute = db.GetObject(id);
            if (absolute == null)
                return false;
            if (nearbyOnly && !IsNearby(actor, absolute) && !Controls(actor, absolute))
                return false;
            match = absolute;
            return true;
        }

        var pool = new List<MUSHObject>();
        if (actor.Location.IsValid)
        {
            pool.AddRange(db.GetContents(actor.Location));
            pool.AddRange(db.GetExits(actor.Location));
        }

        pool.AddRange(db.GetContents(actor.Id));
        if (!nearbyOnly)
            pool.AddRange(db.GetAllObjects().Where(obj => obj.Type == ObjectType.Player));

        var distinct = pool.GroupBy(obj => obj.Id.Value).Select(group => group.First()).ToList();
        var exact = distinct.Where(obj => Matches(obj, name, exact: true)).ToList();
        if (exact.Count == 1)
        {
            match = exact[0];
            return true;
        }

        if (exact.Count > 1)
        {
            ambiguous = true;
            return false;
        }

        var prefix = distinct.Where(obj => Matches(obj, name, exact: false)).ToList();
        if (prefix.Count == 1)
        {
            match = prefix[0];
            return true;
        }

        if (prefix.Count > 1)
            ambiguous = true;
        return false;
    }

    public static bool CanSee(MUSHObject viewer, MUSHObject target, MUSHObject? room)
    {
        if (viewer.IsWizard || viewer.HasPower("SEE_ALL"))
            return true;
        if (DarkLegal(target))
            return false;
        return room == null || !room.HasFlag("DARK") || target.HasFlag("LIGHT");
    }

    private static bool DarkLegal(MUSHObject target)
    {
        if (!target.HasFlag("DARK"))
            return false;
        bool alive = target.Type == ObjectType.Player || target.HasFlag("PUPPET");
        bool canDark = target.IsWizard || target.HasPower("CAN_DARK");
        return canDark || !alive;
    }

    private static bool IsNearby(MUSHObject actor, MUSHObject obj)
    {
        if (obj.Id == actor.Id)
            return true;
        if (actor.Location.IsValid && (obj.Id == actor.Location || obj.Location == actor.Location || obj.Exits == actor.Location))
            return true;
        return obj.Location == actor.Id;
    }

    private static bool Controls(MUSHObject actor, MUSHObject obj)
        => actor.IsWizard || obj.Owner == actor.Owner;

    private static bool Matches(MUSHObject obj, string name, bool exact)
    {
        foreach (string alias in Labels(obj))
        {
            if (exact)
            {
                if (alias.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (alias.StartsWith(name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> Labels(MUSHObject obj)
    {
        if (obj.Type == ObjectType.Exit)
        {
            foreach (string alias in obj.Name.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                yield return alias;
            yield break;
        }

        yield return obj.Name;
    }

    private static void Append(MUSHObject obj, string attribute, Dbref id)
    {
        var ids = Read(obj, attribute);
        if (ids.Contains(id))
            return;
        ids.Add(id);
        obj.SetAttribute(attribute, string.Join(" ", ids), God);
    }

    private static void Remove(MUSHObject obj, string attribute, Dbref id)
    {
        var entry = obj.GetAttribute(attribute);
        if (entry == null)
            return;
        var kept = Read(obj, attribute);
        int index = kept.IndexOf(id);
        if (index >= 0)
            kept.RemoveAt(index);
        obj.SetAttribute(attribute, string.Join(" ", kept), God);
    }
}

internal static class FollowerMovement
{
    public static void Bring(CommandContext context, ObjectDatabase db, CommandDispatcher? dispatcher, Func<Dbref, bool>? connected, Dbref oldLocation, string command)
    {
        if (dispatcher == null || !oldLocation.IsValid)
            return;

        var leader = db.GetObject(context.Executor);
        if (leader == null || leader.Location == oldLocation)
            return;

        foreach (var id in FollowerList.Read(leader, FollowerList.Followers))
        {
            if (id == leader.Id)
                continue;
            var follower = db.GetObject(id);
            if (follower == null || follower.Location != oldLocation)
                continue;
            if (follower.Type == ObjectType.Player)
            {
                if (connected != null && !connected(follower.Id))
                    continue;
            }
            else if (follower.Type != ObjectType.Thing)
            {
                continue;
            }

            var room = db.GetObject(follower.Location);
            if (!FollowerList.CanSee(follower, leader, room))
                continue;

            context.NotifyTarget(follower.Id, $"You follow {leader.Name}.");
            var nested = dispatcher.Dispatch(follower.Id, command, db);
            context.TargetedNotifications.AddRange(nested.TargetedNotifications);
        }
    }
}
