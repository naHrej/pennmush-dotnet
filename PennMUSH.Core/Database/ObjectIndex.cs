namespace PennMUSH.Core.Database;

using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

/// <summary>
/// Name, type, owner, zone, parent, and room-exit maps for <see cref="ObjectDatabase"/>.
/// Updated when an object changes. Ids in each list stay in ascending order.
/// </summary>
public sealed class ObjectIndex
{
    private readonly Dictionary<string, List<int>> _names = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<int>> _players = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ObjectType, List<int>> _types = new();
    private readonly Dictionary<int, List<int>> _owners = new();
    private readonly Dictionary<int, List<int>> _zones = new();
    private readonly Dictionary<int, List<int>> _children = new();
    private readonly Dictionary<int, Dictionary<string, int>> _exitAliases = new();
    private readonly Dictionary<int, Keys> _keys = new();

    public void Clear()
    {
        _names.Clear();
        _players.Clear();
        _types.Clear();
        _owners.Clear();
        _zones.Clear();
        _children.Clear();
        _exitAliases.Clear();
        _keys.Clear();
    }

    public PlayerMove Update(MUSHObject obj, Func<Dbref, IEnumerable<MUSHObject>> exits)
    {
        _keys.TryGetValue(obj.Id.Value, out var previous);
        var next = Keys.From(obj);
        Apply(obj.Id.Value, previous, next);
        _keys[obj.Id.Value] = next;
        RebuildExitRooms(obj.Id.Value, previous, next, exits);

        bool wasPlayer = previous != null && previous.Type == ObjectType.Player;
        bool isPlayer = next.Type == ObjectType.Player;
        if (!wasPlayer)
            return default;
        if (isPlayer && previous!.Location == next.Location)
            return default;
        return new PlayerMove(true, previous!.Location, isPlayer ? next.Location : -1);
    }

    public void Rebuild(IEnumerable<MUSHObject> objects, Func<Dbref, IEnumerable<MUSHObject>> exits)
    {
        Clear();
        var rooms = new HashSet<int>();
        foreach (var obj in objects)
        {
            var keys = Keys.From(obj);
            Apply(obj.Id.Value, null, keys);
            _keys[obj.Id.Value] = keys;
            if (keys.ExitSource >= 0)
                rooms.Add(keys.ExitSource);
            if (keys.Type == ObjectType.Room)
                rooms.Add(obj.Id.Value);
        }

        foreach (int room in rooms)
            RebuildRoom(room, exits);
    }

    public MUSHObject? FindPlayer(string name, Func<int, MUSHObject?> get)
        => First(_players, name, get);

    public MUSHObject? FindByName(string name, Func<int, MUSHObject?> get)
        => First(_names, name, get);

    public MUSHObject? FindExit(Dbref room, string alias, Func<int, MUSHObject?> get)
    {
        if (!room.IsValid || string.IsNullOrWhiteSpace(alias))
            return null;
        if (!_exitAliases.TryGetValue(room.Value, out var map))
            return null;
        return map.TryGetValue(alias.Trim(), out int id) ? get(id) : null;
    }

    public IEnumerable<MUSHObject> OfType(ObjectType type, Func<int, MUSHObject?> get)
        => Enumerate(_types, type, get);

    public IEnumerable<MUSHObject> OwnedBy(Dbref owner, Func<int, MUSHObject?> get)
        => owner.IsValid ? Enumerate(_owners, owner.Value, get) : Array.Empty<MUSHObject>();

    public IEnumerable<MUSHObject> InZone(Dbref zone, Func<int, MUSHObject?> get)
        => zone.IsValid ? Enumerate(_zones, zone.Value, get) : Array.Empty<MUSHObject>();

    public IEnumerable<MUSHObject> ChildrenOf(Dbref parent, Func<int, MUSHObject?> get)
        => parent.IsValid ? Enumerate(_children, parent.Value, get) : Array.Empty<MUSHObject>();

    private void Apply(int id, Keys? previous, Keys next)
    {
        if (previous != null)
        {
            Remove(_names, previous.Name, id);
            if (previous.Type == ObjectType.Player)
                Remove(_players, previous.Name, id);
            Remove(_types, previous.Type, id);
            if (previous.Type != ObjectType.Garbage)
            {
                Remove(_owners, previous.Owner, id);
                foreach (int zone in previous.Zones)
                    Remove(_zones, zone, id);
                foreach (int parent in previous.Parents)
                    Remove(_children, parent, id);
            }
        }

        Add(_names, next.Name, id);
        Add(_types, next.Type, id);
        if (next.Type == ObjectType.Player)
            Add(_players, next.Name, id);
        if (next.Type != ObjectType.Garbage)
        {
            if (next.Owner >= 0)
                Add(_owners, next.Owner, id);
            foreach (int zone in next.Zones)
                Add(_zones, zone, id);
            foreach (int parent in next.Parents)
                Add(_children, parent, id);
        }
    }

    private void RebuildExitRooms(int id, Keys? previous, Keys next, Func<Dbref, IEnumerable<MUSHObject>> exits)
    {
        var rooms = new HashSet<int>();
        bool link = previous == null
            || previous.ExitSource != next.ExitSource
            || previous.Next != next.Next
            || previous.Type != next.Type
            || !string.Equals(previous.Name, next.Name, StringComparison.Ordinal)
            || !string.Equals(previous.Alias, next.Alias, StringComparison.OrdinalIgnoreCase);
        if (link)
        {
            if (previous != null && previous.Type == ObjectType.Exit && previous.ExitSource >= 0)
                rooms.Add(previous.ExitSource);
            if (next.Type == ObjectType.Exit && next.ExitSource >= 0)
                rooms.Add(next.ExitSource);
        }

        if (previous == null || previous.RoomHead != next.RoomHead || previous.Type != next.Type)
        {
            if (previous != null && previous.Type == ObjectType.Room)
                rooms.Add(id);
            if (next.Type == ObjectType.Room)
                rooms.Add(id);
        }

        foreach (int room in rooms)
            RebuildRoom(room, exits);
    }

    private void RebuildRoom(int roomId, Func<Dbref, IEnumerable<MUSHObject>> exits)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var exit in exits(new Dbref(roomId)))
        {
            foreach (string part in exit.Name.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!map.ContainsKey(part))
                    map[part] = exit.Id.Value;
            }

            string? aliasAttr = exit.GetAttribute("ALIAS")?.Value;
            if (!string.IsNullOrWhiteSpace(aliasAttr))
            {
                foreach (string part in aliasAttr.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!map.ContainsKey(part))
                        map[part] = exit.Id.Value;
                }
            }
        }

        if (map.Count == 0)
            _exitAliases.Remove(roomId);
        else
            _exitAliases[roomId] = map;
    }

    private static MUSHObject? First(Dictionary<string, List<int>> map, string name, Func<int, MUSHObject?> get)
    {
        if (!map.TryGetValue(name, out var ids) || ids.Count == 0)
            return null;
        return get(ids[0]);
    }

    private static IEnumerable<MUSHObject> Enumerate<TKey>(Dictionary<TKey, List<int>> map, TKey key, Func<int, MUSHObject?> get)
        where TKey : notnull
    {
        if (!map.TryGetValue(key, out var ids))
            yield break;
        foreach (int id in ids.ToArray())
        {
            var obj = get(id);
            if (obj != null)
                yield return obj;
        }
    }

    private static void Add<TKey>(Dictionary<TKey, List<int>> map, TKey key, int id)
        where TKey : notnull
    {
        if (!map.TryGetValue(key, out var list))
        {
            map[key] = new List<int> { id };
            return;
        }

        int at = list.BinarySearch(id);
        if (at < 0)
            list.Insert(~at, id);
    }

    private static void Remove<TKey>(Dictionary<TKey, List<int>> map, TKey key, int id)
        where TKey : notnull
    {
        if (!map.TryGetValue(key, out var list))
            return;
        int at = list.BinarySearch(id);
        if (at >= 0)
            list.RemoveAt(at);
        if (list.Count == 0)
            map.Remove(key);
    }

    private sealed class Keys
    {
        public string Name = string.Empty;
        public string Alias = string.Empty;
        public ObjectType Type;
        public int Owner = -1;
        public int Location = -1;
        public int ExitSource = -1;
        public int RoomHead = -1;
        public int Next = -1;
        public List<int> Parents = new();
        public List<int> Zones = new();

        public static Keys From(MUSHObject obj) => new()
        {
            Name = obj.Name ?? string.Empty,
            Alias = obj.Type == ObjectType.Exit ? (obj.GetAttribute("ALIAS")?.Value ?? string.Empty) : string.Empty,
            Type = obj.Type,
            Owner = obj.Owner.IsValid ? obj.Owner.Value : -1,
            Location = obj.Location.IsValid ? obj.Location.Value : -1,
            ExitSource = obj.Type == ObjectType.Exit && obj.Exits.IsValid ? obj.Exits.Value : -1,
            RoomHead = obj.Type == ObjectType.Room ? obj.Exits.Value : -1,
            Next = obj.Next.Value,
            Parents = obj.Parents.Where(parent => parent.IsValid).Select(parent => parent.Value).Distinct().ToList(),
            Zones = obj.Zones.Where(zone => zone.IsValid).Select(zone => zone.Value).Distinct().ToList()
        };
    }
}

/// <summary>A player changed rooms. Room ids are -1 when the player is nowhere.</summary>
public readonly record struct PlayerMove(bool Moved, int FromRoom, int ToRoom);
