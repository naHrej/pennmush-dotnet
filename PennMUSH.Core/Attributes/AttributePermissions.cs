namespace PennMUSH.Core.Attributes;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public static class AttributePermissions
{
    public static bool Controls(MUSHObject? actor, MUSHObject? target)
    {
        if (actor == null || target == null) return false;
        if (actor.IsGod || actor.IsWizard) return true;
        if (actor.Id == target.Id || target.Owner == actor.Id) return true;
        return actor.Owner.IsValid && target.Owner == actor.Owner;
    }

    public static bool IsNearby(MUSHObject? a, MUSHObject? b)
    {
        if (a == null || b == null) return false;
        if (a.Id == b.Id) return true;
        if (a.Location == b.Id || b.Location == a.Id) return true;
        return a.Location.IsValid && a.Location == b.Location;
    }

    public static bool CanSeeAll(MUSHObject? looker)
        => looker != null && (looker.IsGod || looker.IsWizard || looker.IsRoyalty || looker.HasPower("See_All"));

    private static bool CanReadSingleEntry(MUSHObject looker, MUSHObject target, AttributeEntry entry, bool canSeeObj, bool isNearby, bool seeAll)
    {
        if (entry.Flags.HasFlag(AttributeFlags.Internal))
            return false;
        if (seeAll)
            return true;
        if (entry.Flags.HasFlag(AttributeFlags.MDark))
            return false;

        if (canSeeObj)
            return true;

        if ((entry.Flags.HasFlag(AttributeFlags.Visual) || entry.Flags.HasFlag(AttributeFlags.Public))
            && (!entry.Flags.HasFlag(AttributeFlags.Nearby) || isNearby))
        {
            return true;
        }

        if (entry.Creator == looker.Id)
            return true;
        if (looker.Owner.IsValid && entry.Creator == looker.Owner)
            return true;

        return false;
    }

    /// <summary>
    /// Checks whether <paramref name="looker"/> may read <paramref name="attr"/> on <paramref name="target"/>
    /// (stored on <paramref name="sourceObj"/>, which equals <paramref name="target"/> for own attributes or an ancestor for inherited ones).
    /// Restrictions on branch prefixes propagate to children; access-granting flags (Visual/Public) stay on the attribute where set.
    /// </summary>
    public static bool CanRead(MUSHObject? looker, MUSHObject? target, AttributeEntry? attr, MUSHObject? sourceObj = null)
    {
        if (looker == null || target == null || attr == null)
            return false;

        MUSHObject branchOwner = sourceObj ?? target;
        if (branchOwner.Id != target.Id && attr.Flags.HasFlag(AttributeFlags.Private))
            return false;

        bool seeAll = CanSeeAll(looker);
        bool canSeeObj = Controls(looker, target) || target.HasFlag("VISUAL");
        bool nearby = IsNearby(looker, target);

        if (!CanReadSingleEntry(looker, target, attr, canSeeObj, nearby, seeAll))
            return false;

        int tick = attr.Name.IndexOf('`');
        if (tick < 0)
            return true;

        int start = 0;
        while (true)
        {
            int idx = attr.Name.IndexOf('`', start);
            if (idx < 0) break;

            string prefix = attr.Name.Substring(0, idx);
            var branch = branchOwner.GetAttribute(prefix);
            if (branch == null)
                return false;
            if (branchOwner.Id != target.Id && branch.Flags.HasFlag(AttributeFlags.Private))
                return false;
            if (!CanReadSingleEntry(looker, target, branch, canSeeObj, nearby, seeAll))
                return false;

            start = idx + 1;
        }

        return true;
    }

    public static AttributeEntry? GetReadableAttribute(MUSHObject? looker, MUSHObject target, string attrName, ObjectDatabase? db, bool includeParents)
    {
        if (looker == null) return null;
        var entry = includeParents
            ? target.GetAttributeWithSource(attrName, db, out var sourceObj)
            : GetOwnAttribute(target, attrName, out sourceObj);
        if (entry == null) return null;
        return CanRead(looker, target, entry, sourceObj) ? entry : null;
    }

    private static AttributeEntry? GetOwnAttribute(MUSHObject target, string attrName, out MUSHObject sourceObj)
    {
        sourceObj = target;
        return target.GetAttribute(attrName);
    }

    public static IEnumerable<(AttributeEntry Entry, MUSHObject Source)> EnumerateAttributes(MUSHObject target, ObjectDatabase? db, bool includeParents)
    {
        if (!includeParents || db == null || target.Parents.Count == 0)
        {
            foreach (var attr in target.Attributes)
                yield return (attr, target);
            yield break;
        }

        var yieldedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var attr in target.Attributes)
        {
            if (yieldedNames.Add(attr.Name))
                yield return (attr, target);
        }

        var queue = new Queue<Dbref>(target.Parents);
        var seen = new HashSet<Dbref> { target.Id };
        while (queue.Count > 0)
        {
            var pRef = queue.Dequeue();
            if (!seen.Add(pRef)) continue;
            var pObj = db.GetObject(pRef);
            if (pObj == null) continue;

            foreach (var attr in pObj.Attributes)
            {
                if (attr.Flags.HasFlag(AttributeFlags.Private))
                    continue;
                if (yieldedNames.Add(attr.Name))
                    yield return (attr, pObj);
            }

            foreach (var ancestorRef in pObj.Parents)
            {
                if (!seen.Contains(ancestorRef))
                    queue.Enqueue(ancestorRef);
            }
        }
    }
}
