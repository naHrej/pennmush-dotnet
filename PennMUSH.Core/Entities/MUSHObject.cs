namespace PennMUSH.Core.Entities;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Database;
using PennMUSH.Core.Flags;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Types;

public class MUSHObject
{
    private readonly Dictionary<string, AttributeEntry> _attributes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _flags = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _powers = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Dbref> _parents = new();
    private readonly HashSet<Dbref> _zones = new();
    private readonly Dictionary<string, LockEntry> _locks = new(StringComparer.OrdinalIgnoreCase);

    public Dbref Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public ObjectType Type { get; set; } = ObjectType.None;
    public Dbref Location { get; set; } = Dbref.Nothing;
    public Dbref Contents { get; set; } = Dbref.Nothing;
    public Dbref Exits { get; set; } = Dbref.Nothing;
    public Dbref Next { get; set; } = Dbref.Nothing;
    public Dbref Owner { get; set; } = Dbref.Nothing;
    public Dbref Home { get; set; } = Dbref.Nothing;
    public int Pennies { get; set; } = 0;
    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModificationTime { get; set; } = DateTimeOffset.UtcNow;

    public IReadOnlyCollection<AttributeEntry> Attributes => _attributes.Values;
    public IReadOnlySet<string> Flags => _flags;
    public IReadOnlySet<string> Powers => _powers;
    public IReadOnlyList<Dbref> Parents => _parents;
    public IReadOnlySet<Dbref> Zones => _zones;
    public IReadOnlyCollection<LockEntry> Locks => _locks.Values;

    public LockEntry? GetLock(string type) => _locks.TryGetValue(type, out var l) ? l : null;
    public void SetLock(string type, LockNode key, Dbref creator, LockFlags flags = LockFlags.None)
    {
        _locks[type] = new LockEntry(type, key, creator, flags);
        ModificationTime = DateTimeOffset.UtcNow;
    }
    public bool ClearLock(string type) => _locks.Remove(type);
    public bool EvalLock(string type, Dbref enactor, ObjectDatabase db)
    {
        if (_locks.TryGetValue(type, out var entry))
        {
            return entry.Key.Evaluate(enactor, Id, db);
        }
        return true; // Default unlocked if no lock set
    }

    public Dbref Parent
    {
        get => _parents.Count > 0 ? _parents[0] : Dbref.Nothing;
        set
        {
            _parents.Clear();
            if (value.IsValid) _parents.Add(value);
            ModificationTime = DateTimeOffset.UtcNow;
        }
    }

    public Dbref Zone
    {
        get => _zones.Count > 0 ? System.Linq.Enumerable.First(_zones) : Dbref.Nothing;
        set
        {
            _zones.Clear();
            if (value.IsValid) _zones.Add(value);
            ModificationTime = DateTimeOffset.UtcNow;
        }
    }

    public MUSHObject(Dbref id, string name, ObjectType type, Dbref owner)
    {
        Id = id;
        Name = name;
        Type = type;
        Owner = owner;
    }

    public AttributeErrorCode SetAttribute(string name, string value, Dbref setter, AttributeFlags flags = AttributeFlags.None, bool wizardOverride = false)
    {
        if (!AttributeEntry.IsValidName(name))
            return AttributeErrorCode.BadName;

        if (_attributes.TryGetValue(name, out var existing))
        {
            if (existing.Flags.HasFlag(AttributeFlags.Safe))
                return AttributeErrorCode.Safe;
            if (existing.Flags.HasFlag(AttributeFlags.Locked) && !wizardOverride)
                return AttributeErrorCode.Locked;

            existing.Value = value;
            existing.Creator = setter;
            existing.Flags = flags;
        }
        else
        {
            _attributes[name] = new AttributeEntry(name.ToUpperInvariant(), value, setter, flags);
        }

        ModificationTime = DateTimeOffset.UtcNow;
        return AttributeErrorCode.Okay;
    }

    public AttributeEntry? GetAttribute(string name, ObjectDatabase? db = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (_attributes.TryGetValue(name, out var entry))
            return entry;

        if (db == null || _parents.Count == 0)
            return null;

        // Traverse parent DAG with cycle detection
        var queue = new Queue<Dbref>(_parents);
        var seen = new HashSet<Dbref> { Id };

        while (queue.Count > 0)
        {
            var pRef = queue.Dequeue();
            if (!seen.Add(pRef)) continue;

            var pObj = db.GetObject(pRef);
            if (pObj == null) continue;

            var inherited = pObj.GetAttribute(name);
            if (inherited != null)
                return inherited;

            foreach (var ancestorRef in pObj.Parents)
            {
                if (!seen.Contains(ancestorRef))
                    queue.Enqueue(ancestorRef);
            }
        }

        return null;
    }

    public bool HasAttribute(string name, ObjectDatabase? db = null)
    {
        return GetAttribute(name, db) != null;
    }

    public bool AddParent(Dbref parent, ObjectDatabase? db = null)
    {
        if (!parent.IsValid || _parents.Contains(parent)) return false;
        if (db != null && WouldCauseParentCycle(Id, parent, db)) return false;

        _parents.Add(parent);
        ModificationTime = DateTimeOffset.UtcNow;
        return true;
    }

    public bool RemoveParent(Dbref parent)
    {
        bool removed = _parents.Remove(parent);
        if (removed) ModificationTime = DateTimeOffset.UtcNow;
        return removed;
    }

    public void ClearParents()
    {
        _parents.Clear();
        ModificationTime = DateTimeOffset.UtcNow;
    }

    public void SetParents(IEnumerable<Dbref> parents, ObjectDatabase? db = null)
    {
        _parents.Clear();
        foreach (var p in parents)
        {
            if (p.IsValid && !_parents.Contains(p))
            {
                if (db == null || !WouldCauseParentCycle(Id, p, db))
                    _parents.Add(p);
            }
        }
        ModificationTime = DateTimeOffset.UtcNow;
    }

    public static bool WouldCauseParentCycle(Dbref targetRef, Dbref newParentRef, ObjectDatabase db)
    {
        if (targetRef == newParentRef) return true;

        var queue = new Queue<Dbref>();
        queue.Enqueue(newParentRef);
        var seen = new HashSet<Dbref>();

        while (queue.Count > 0)
        {
            var curr = queue.Dequeue();
            if (curr == targetRef) return true;
            if (!seen.Add(curr)) continue;

            var obj = db.GetObject(curr);
            if (obj == null) continue;

            foreach (var p in obj.Parents)
            {
                if (!seen.Contains(p))
                    queue.Enqueue(p);
            }
        }

        return false;
    }

    public bool AddZone(Dbref zone)
    {
        if (!zone.IsValid) return false;
        bool added = _zones.Add(zone);
        if (added) ModificationTime = DateTimeOffset.UtcNow;
        return added;
    }

    public bool RemoveZone(Dbref zone)
    {
        bool removed = _zones.Remove(zone);
        if (removed) ModificationTime = DateTimeOffset.UtcNow;
        return removed;
    }

    public void ClearZones()
    {
        _zones.Clear();
        ModificationTime = DateTimeOffset.UtcNow;
    }

    public void SetZones(IEnumerable<Dbref> zones)
    {
        _zones.Clear();
        foreach (var z in zones)
        {
            if (z.IsValid) _zones.Add(z);
        }
        ModificationTime = DateTimeOffset.UtcNow;
    }

    public bool InZone(Dbref zone) => _zones.Contains(zone);

    public AttributeErrorCode ClearAttribute(string name, bool wizardOverride = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            return AttributeErrorCode.BadName;

        if (_attributes.TryGetValue(name, out var existing))
        {
            if (existing.Flags.HasFlag(AttributeFlags.Safe))
                return AttributeErrorCode.Safe;
            if (existing.Flags.HasFlag(AttributeFlags.Locked) && !wizardOverride)
                return AttributeErrorCode.Locked;

            _attributes.Remove(name);
            ModificationTime = DateTimeOffset.UtcNow;
            return AttributeErrorCode.Okay;
        }

        return AttributeErrorCode.Okay;
    }

    public AttributeErrorCode SetAttributeFlags(string name, AttributeFlags flags)
    {
        if (string.IsNullOrWhiteSpace(name) || !_attributes.TryGetValue(name, out var existing))
            return AttributeErrorCode.NotFound;
        existing.Flags = flags;
        ModificationTime = DateTimeOffset.UtcNow;
        return AttributeErrorCode.Okay;
    }

    public AttributeErrorCode SetAttributeCreator(string name, Dbref creator)
    {
        if (string.IsNullOrWhiteSpace(name) || !_attributes.TryGetValue(name, out var existing))
            return AttributeErrorCode.NotFound;
        existing.Creator = creator;
        ModificationTime = DateTimeOffset.UtcNow;
        return AttributeErrorCode.Okay;
    }

    public bool DeleteAttribute(string name)
    {
        return ClearAttribute(name) == AttributeErrorCode.Okay;
    }

    public bool HasFlag(string flagName)
    {
        if (string.IsNullOrWhiteSpace(flagName)) return false;
        var f = FlagTable.FindFlag(flagName);
        return _flags.Contains(f != null ? f.Name : flagName.ToUpperInvariant());
    }

    public bool HasPower(string powerName)
    {
        if (string.IsNullOrWhiteSpace(powerName)) return false;
        var p = FlagTable.FindPower(powerName);
        return _powers.Contains(p != null ? p.Name : powerName);
    }

    public bool AddFlag(string flagName)
    {
        if (string.IsNullOrWhiteSpace(flagName)) return false;
        var f = FlagTable.FindFlag(flagName);
        string canonical = f != null ? f.Name : flagName.ToUpperInvariant();
        bool added = _flags.Add(canonical);
        if (added) ModificationTime = DateTimeOffset.UtcNow;
        return added;
    }

    public bool RemoveFlag(string flagName)
    {
        if (string.IsNullOrWhiteSpace(flagName)) return false;
        var f = FlagTable.FindFlag(flagName);
        string canonical = f != null ? f.Name : flagName.ToUpperInvariant();
        bool removed = _flags.Remove(canonical);
        if (removed) ModificationTime = DateTimeOffset.UtcNow;
        return removed;
    }

    public bool AddPower(string powerName)
    {
        if (string.IsNullOrWhiteSpace(powerName)) return false;
        var p = FlagTable.FindPower(powerName);
        string canonical = p != null ? p.Name : powerName;
        bool added = _powers.Add(canonical);
        if (added) ModificationTime = DateTimeOffset.UtcNow;
        return added;
    }

    public bool RemovePower(string powerName)
    {
        if (string.IsNullOrWhiteSpace(powerName)) return false;
        var p = FlagTable.FindPower(powerName);
        string canonical = p != null ? p.Name : powerName;
        bool removed = _powers.Remove(canonical);
        if (removed) ModificationTime = DateTimeOffset.UtcNow;
        return removed;
    }

    public bool IsGod => Id.Value == 1;
    public bool IsWizard => IsGod || HasFlag("WIZARD");
    public bool IsRoyalty => IsWizard || HasFlag("ROYALTY");
}