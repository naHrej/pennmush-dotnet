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
    private readonly List<StoredListener> _listeners = new();
    private Dictionary<string, InheritedAttribute>? _inherited;
    private string _name = string.Empty;
    private ObjectType _type = ObjectType.None;
    private Dbref _location = Dbref.Nothing;
    private Dbref _contents = Dbref.Nothing;
    private Dbref _exits = Dbref.Nothing;
    private Dbref _next = Dbref.Nothing;
    private Dbref _owner = Dbref.Nothing;
    private Dbref _home = Dbref.Nothing;
    private int _pennies;
    private uint _warnings;

    /// <summary>
    /// While greater than zero on this thread, mutations do not bump
    /// <see cref="ModificationTime"/> or notify <see cref="Changed"/>.
    /// Database load holds this. It is per thread so one load does not
    /// freeze indexes on another database.
    /// </summary>
    [ThreadStatic]
    internal static int Quiet;

    /// <summary>Invoked after a tracked field changes. The database uses this to mark the object dirty.</summary>
    internal Action? Changed { get; set; }

    public Dbref Id { get; init; }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
                return;
            _name = value ?? string.Empty;
            Touch();
        }
    }

    public ObjectType Type
    {
        get => _type;
        set
        {
            if (_type == value)
                return;
            _type = value;
            Touch();
        }
    }

    public Dbref Location
    {
        get => _location;
        set
        {
            if (_location == value)
                return;
            _location = value;
            Touch();
        }
    }

    public Dbref Contents
    {
        get => _contents;
        set
        {
            if (_contents == value)
                return;
            _contents = value;
            Touch();
        }
    }

    public Dbref Exits
    {
        get => _exits;
        set
        {
            if (_exits == value)
                return;
            _exits = value;
            Touch();
        }
    }

    public Dbref Next
    {
        get => _next;
        set
        {
            if (_next == value)
                return;
            _next = value;
            Touch();
        }
    }

    public Dbref Owner
    {
        get => _owner;
        set
        {
            if (_owner == value)
                return;
            _owner = value;
            Touch();
        }
    }

    public Dbref Home
    {
        get => _home;
        set
        {
            if (_home == value)
                return;
            _home = value;
            Touch();
        }
    }

    public int Pennies
    {
        get => _pennies;
        set
        {
            if (_pennies == value)
                return;
            _pennies = value;
            Touch();
        }
    }

    public uint Warnings
    {
        get => _warnings;
        set
        {
            if (_warnings == value)
                return;
            _warnings = value;
            Touch();
        }
    }

    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModificationTime { get; set; } = DateTimeOffset.UtcNow;

    private void Touch()
    {
        if (Quiet > 0)
            return;
        ModificationTime = DateTimeOffset.UtcNow;
        Changed?.Invoke();
    }

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
        Touch();
    }
    public bool ClearLock(string type)
    {
        if (!_locks.Remove(type))
            return false;
        Touch();
        return true;
    }
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
            DropInheritedAttributes();
            Touch();
        }
    }

    public Dbref Zone
    {
        get => _zones.Count > 0 ? System.Linq.Enumerable.First(_zones) : Dbref.Nothing;
        set
        {
            _zones.Clear();
            if (value.IsValid) _zones.Add(value);
            Touch();
        }
    }

    public MUSHObject(Dbref id, string name, ObjectType type, Dbref owner)
    {
        Id = id;
        _name = name;
        _type = type;
        _owner = owner;
    }

    public AttributeErrorCode SetAttribute(string name, string value, Dbref setter, AttributeFlags flags = AttributeFlags.None, bool wizardOverride = false, IEnumerable<string>? customFlags = null)
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
            if (customFlags != null)
                existing.SetCustomFlags(customFlags);
        }
        else
        {
            _attributes[name] = new AttributeEntry(name.ToUpperInvariant(), value, setter, flags, customFlags);
        }

        NoteAttributeValue();
        Touch();
        return AttributeErrorCode.Okay;
    }

    public AttributeEntry? GetAttribute(string name, ObjectDatabase? db = null)
        => GetAttributeWithSource(name, db, out _);

    public AttributeEntry? GetAttributeWithSource(string name, ObjectDatabase? db, out MUSHObject? sourceObj)
    {
        sourceObj = null;
        if (string.IsNullOrWhiteSpace(name))
            return null;

        if (_attributes.TryGetValue(name, out var entry))
        {
            sourceObj = this;
            return entry;
        }

        if (db == null || _parents.Count == 0)
            return null;

        _inherited ??= new Dictionary<string, InheritedAttribute>(StringComparer.OrdinalIgnoreCase);
        if (_inherited.TryGetValue(name, out var cached))
        {
            sourceObj = cached.Source;
            return cached.Entry;
        }

        // Traverse parent DAG with cycle detection. A parent's own attributes
        // are read directly; the queue carries that parent's parents onward.
        var queue = new Queue<Dbref>(_parents);
        var seen = new HashSet<Dbref> { Id };
        AttributeEntry? found = null;

        while (queue.Count > 0)
        {
            var pRef = queue.Dequeue();
            if (!seen.Add(pRef)) continue;

            var pObj = db.GetObject(pRef);
            if (pObj == null) continue;

            var inherited = pObj.GetAttribute(name);
            if (inherited != null && !inherited.Flags.HasFlag(AttributeFlags.Private))
            {
                sourceObj = pObj;
                found = inherited;
                break;
            }

            foreach (var ancestorRef in pObj.Parents)
            {
                if (!seen.Contains(ancestorRef))
                    queue.Enqueue(ancestorRef);
            }
        }

        _inherited[name] = new InheritedAttribute(found, sourceObj);
        return found;
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
        DropInheritedAttributes();
        Touch();
        return true;
    }

    public bool RemoveParent(Dbref parent)
    {
        bool removed = _parents.Remove(parent);
        if (removed)
        {
            DropInheritedAttributes();
            Touch();
        }
        return removed;
    }

    public void ClearParents()
    {
        _parents.Clear();
        DropInheritedAttributes();
        Touch();
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
        DropInheritedAttributes();
        Touch();
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
        if (added) Touch();
        return added;
    }

    public bool RemoveZone(Dbref zone)
    {
        bool removed = _zones.Remove(zone);
        if (removed) Touch();
        return removed;
    }

    public void ClearZones()
    {
        _zones.Clear();
        Touch();
    }

    public void SetZones(IEnumerable<Dbref> zones)
    {
        _zones.Clear();
        foreach (var z in zones)
        {
            if (z.IsValid) _zones.Add(z);
        }
        Touch();
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
            NoteAttributeValue();
            Touch();
            return AttributeErrorCode.Okay;
        }

        return AttributeErrorCode.Okay;
    }

    public void WriteAttributeValue(string name, string value, Dbref setter, AttributeFlags flagsIfNew)
    {
        string stored = name.ToUpperInvariant();
        if (_attributes.TryGetValue(stored, out var existing))
        {
            existing.Value = value;
            existing.Creator = setter;
        }
        else
        {
            _attributes[stored] = new AttributeEntry(stored, value, setter, flagsIfNew);
        }

        NoteAttributeValue();
        Touch();
    }

    public void AddAttributeFlags(string name, AttributeFlags flags)
    {
        if (_attributes.TryGetValue(name, out var existing))
        {
            existing.Flags |= flags;
            NoteAttributeValue();
            Touch();
        }
    }

    public void RemoveAttributeFlags(string name, AttributeFlags flags)
    {
        if (_attributes.TryGetValue(name, out var existing))
        {
            existing.Flags &= ~flags;
            NoteAttributeValue();
            Touch();
        }
    }

    public bool AddAttributeCustomFlag(string name, string flag)
    {
        if (_attributes.TryGetValue(name, out var existing) && existing.AddCustomFlag(flag))
        {
            NoteAttributeValue();
            Touch();
            return true;
        }
        return false;
    }

    public bool RemoveAttributeCustomFlag(string name, string flag)
    {
        if (_attributes.TryGetValue(name, out var existing) && existing.RemoveCustomFlag(flag))
        {
            NoteAttributeValue();
            Touch();
            return true;
        }
        return false;
    }

    public bool RemoveAttribute(string name)
    {
        bool removed = _attributes.Remove(name);
        if (removed)
        {
            NoteAttributeValue();
            Touch();
        }
        return removed;
    }

    public AttributeErrorCode SetAttributeFlags(string name, AttributeFlags flags)
    {
        if (string.IsNullOrWhiteSpace(name) || !_attributes.TryGetValue(name, out var existing))
            return AttributeErrorCode.NotFound;
        existing.Flags = flags;
        NoteAttributeValue();
        Touch();
        return AttributeErrorCode.Okay;
    }

    public AttributeErrorCode SetAttributeCreator(string name, Dbref creator)
    {
        if (string.IsNullOrWhiteSpace(name) || !_attributes.TryGetValue(name, out var existing))
            return AttributeErrorCode.NotFound;
        existing.Creator = creator;
        Touch();
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
        if (added) Touch();
        return added;
    }

    public bool RemoveFlag(string flagName)
    {
        if (string.IsNullOrWhiteSpace(flagName)) return false;
        var f = FlagTable.FindFlag(flagName);
        string canonical = f != null ? f.Name : flagName.ToUpperInvariant();
        bool removed = _flags.Remove(canonical);
        if (removed) Touch();
        return removed;
    }

    public bool AddPower(string powerName)
    {
        if (string.IsNullOrWhiteSpace(powerName)) return false;
        var p = FlagTable.FindPower(powerName);
        string canonical = p != null ? p.Name : powerName;
        bool added = _powers.Add(canonical);
        if (added) Touch();
        return added;
    }

    public bool RemovePower(string powerName)
    {
        if (string.IsNullOrWhiteSpace(powerName)) return false;
        var p = FlagTable.FindPower(powerName);
        string canonical = p != null ? p.Name : powerName;
        bool removed = _powers.Remove(canonical);
        if (removed) Touch();
        return removed;
    }

    public bool IsGod => Id.Value == 1;
    public bool IsWizard => IsGod || HasFlag("WIZARD");
    public bool IsRoyalty => IsWizard || HasFlag("ROYALTY");

    /// <summary>$ and ^ patterns on this object's own attributes, in attribute order.</summary>
    internal IReadOnlyList<StoredListener> Listeners => _listeners;

    internal void DropInheritedAttributes() => _inherited = null;

    private void NoteAttributeValue()
    {
        DropInheritedAttributes();
        _listeners.Clear();
        foreach (var attr in _attributes.Values)
        {
            string val = attr.Value ?? string.Empty;
            if (val.Length == 0)
                continue;
            if (val[0] == '$')
            {
                int colon = FindUnescapedColon(val);
                if (colon <= 1)
                    continue;
                string pattern = val.Substring(1, colon - 1).Replace("\\:", ":").Trim();
                if (pattern.Length == 0)
                    continue;
                _listeners.Add(new StoredListener(
                    attr.Name,
                    pattern,
                    val[(colon + 1)..].Trim(),
                    Command: true,
                    Regexp: attr.Flags.HasFlag(AttributeFlags.Regexp),
                    Case: attr.Flags.HasFlag(AttributeFlags.Case)));
            }
            else if (val[0] == '^')
            {
                int colon = FindUnescapedColon(val);
                string pattern = (colon > 1 ? val[1..colon] : val[1..]).Replace("\\:", ":").Trim();
                if (pattern.Length == 0)
                    continue;
                _listeners.Add(new StoredListener(
                    attr.Name,
                    pattern,
                    string.Empty,
                    Command: false,
                    Regexp: attr.Flags.HasFlag(AttributeFlags.Regexp),
                    Case: attr.Flags.HasFlag(AttributeFlags.Case)));
            }
        }
    }

    private static int FindUnescapedColon(string text)
    {
        for (int i = 1; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 1 < text.Length)
            {
                i++;
                continue;
            }
            if (text[i] == ':')
                return i;
        }
        return -1;
    }
}

/// <summary>A parsed $command or ^listen pattern stored on the object that owns the attribute.</summary>
internal readonly record struct StoredListener(string AttributeName, string Pattern, string Action, bool Command, bool Regexp = false, bool Case = false);

internal readonly record struct InheritedAttribute(AttributeEntry? Entry, MUSHObject? Source);
