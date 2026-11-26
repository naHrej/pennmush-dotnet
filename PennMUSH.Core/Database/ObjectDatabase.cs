namespace PennMUSH.Core.Database;

using System.Collections.Generic;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class ObjectDatabase
{
    private readonly List<MUSHObject?> _objects = new();
    private readonly HashSet<Dbref> _connected = new();
    private readonly ObjectIndex _index = new();

    public ChangeTracker Changes { get; } = new();

    /// <summary>Raised when a player moves, including a direct location write. Not raised while a database is loading.</summary>
    public Action<MUSHObject, Dbref, Dbref>? PlayerLocationChanged { get; set; }

    /// <summary>Raised when an object is destroyed/recycled.</summary>
    public event Action<Dbref>? ObjectDestroyed;

    /// <summary>Dbref of the System Object used for global $name object resolution (default #3).</summary>
    public Dbref SystemObjectId { get; set; } = new Dbref(3);

    /// <summary>Dbref of the global speech mogrifier object for say/pose/semipose (default #-1).</summary>
    public Dbref SpeechMogrifierId { get; set; } = Dbref.Nothing;

    /// <summary>Dbref of the Master Room used for global exits and $commands (default #2).</summary>
    public Dbref MasterRoomId { get; set; } = new Dbref(2);

    /// <summary>Dbref of the Default Home room (default #0).</summary>
    public Dbref DefaultHomeId { get; set; } = new Dbref(0);

    /// <summary>Dbref of the Base Room (default #0).</summary>
    public Dbref BaseRoomId { get; set; } = new Dbref(0);

    /// <summary>Dbref of the Player Start room (default #0).</summary>
    public Dbref PlayerStartId { get; set; } = new Dbref(0);

    /// <summary>Singular currency name (default "Penny").</summary>
    public string MoneySingular { get; set; } = "Penny";

    /// <summary>Plural currency name (default "Pennies").</summary>
    public string MoneyPlural { get; set; } = "Pennies";

    public bool EmptyAttrs { get; set; } = true;
    public bool ReadRemoteDesc { get; set; } = false;
    public int MaxAttrsPerObj { get; set; } = 2048;
    public int MaxParents { get; set; } = 10;
    public int MaxDbref { get; set; } = 0;
    public int ObjectCost { get; set; } = 10;
    public int RoomCost { get; set; } = 10;
    public int ExitCost { get; set; } = 1;
    public int LinkCost { get; set; } = 1;
    public int FindCost { get; set; } = 100;
    public int QueueCost { get; set; } = 10;
    public int QuotaCost { get; set; } = 1;
    public int MaxPennies { get; set; } = 100000;
    public int PlayerNameLen { get; set; } = 15;
    public bool PlayerNameSpaces { get; set; } = false;
    public bool OnlyAsciiInNames { get; set; } = true;

    public bool LinkToObject { get; set; } = true;
    public bool ReallySafe { get; set; } = true;
    public bool CommaExitList { get; set; } = true;
    public bool ExPublicAttribs { get; set; } = true;
    public bool FlagsOnExamine { get; set; } = true;
    public bool Startups { get; set; } = true;
    public int ChanTitleLen { get; set; } = 80;
    public int MaxChannels { get; set; } = 200;
    public Dbref AncestorRoom { get; set; } = Dbref.Nothing;
    public Dbref AncestorThing { get; set; } = Dbref.Nothing;
    public Dbref AncestorExit { get; set; } = Dbref.Nothing;
    public Dbref AncestorPlayer { get; set; } = Dbref.Nothing;

    public Dbref GetAncestor(ObjectType type) => type switch
    {
        ObjectType.Room => AncestorRoom,
        ObjectType.Thing => AncestorThing,
        ObjectType.Exit => AncestorExit,
        ObjectType.Player => AncestorPlayer,
        _ => Dbref.Nothing
    };

    public string FormatMoneyUnit(int amount)
        => (amount == 1 ? MoneySingular : MoneyPlural).ToLowerInvariant();

    public int Count => _objects.Count;
    public Dbref Top => new Dbref(_objects.Count);

    public void Clear()
    {
        _objects.Clear();
        _connected.Clear();
        _index.Clear();
        Changes.Clear();
    }

    public void SetConnected(Dbref id, bool connected)
    {
        if (connected)
            _connected.Add(id);
        else
            _connected.Remove(id);
    }

    public bool IsConnected(Dbref id) => _connected.Contains(id);

    public IEnumerable<MUSHObject> GetAllObjects() => _objects.Where(o => o != null)!;

    public void SetObject(MUSHObject obj)
    {
        while (_objects.Count <= obj.Id.Value)
        {
            _objects.Add(null);
        }
        Watch(obj);
        _objects[obj.Id.Value] = obj;
        Changes.MarkObject(obj.Id);
        if (MUSHObject.Quiet == 0)
            _index.Update(obj, GetExits);
    }

    public MUSHObject CreateObject(string name, ObjectType type, Dbref owner)
    {
        var id = new Dbref(_objects.Count);
        var obj = new MUSHObject(id, name, type, owner);
        Watch(obj);
        _objects.Add(obj);
        Changes.MarkObject(obj.Id);
        if (MUSHObject.Quiet == 0)
            _index.Update(obj, GetExits);
        return obj;
    }

    public void RebuildIndexes() => _index.Rebuild(GetAllObjects(), GetExits);

    public IEnumerable<MUSHObject> ByType(ObjectType type) => _index.OfType(type, GetById);

    public IEnumerable<MUSHObject> ByOwner(Dbref owner) => _index.OwnedBy(owner, GetById);

    public IEnumerable<MUSHObject> ByZone(Dbref zone) => _index.InZone(zone, GetById);

    public IEnumerable<MUSHObject> ChildrenOf(Dbref parent) => _index.ChildrenOf(parent, GetById);

    private void Watch(MUSHObject obj)
    {
        obj.Changed = () =>
        {
            Changes.MarkObject(obj.Id);
            if (MUSHObject.Quiet != 0)
                return;
            var move = _index.Update(obj, GetExits);
            if (move.Moved)
                PlayerLocationChanged?.Invoke(obj, new Dbref(move.FromRoom), new Dbref(move.ToRoom));
            DropDescendantAttributes(obj);
        };
    }

    private void DropDescendantAttributes(MUSHObject obj)
    {
        var seen = new HashSet<int> { obj.Id.Value };
        var pending = new Queue<Dbref>();
        foreach (var child in _index.ChildrenOf(obj.Id, GetById))
            pending.Enqueue(child.Id);

        while (pending.Count > 0)
        {
            var id = pending.Dequeue();
            if (!seen.Add(id.Value))
                continue;
            var child = GetObject(id);
            if (child == null)
                continue;
            child.DropInheritedAttributes();
            foreach (var grand in _index.ChildrenOf(id, GetById))
                pending.Enqueue(grand.Id);
        }
    }

    private MUSHObject? GetById(int id) => GetObject(new Dbref(id));

    public MUSHObject? GetObject(Dbref id)
    {
        if (!id.IsValid || id.Value >= _objects.Count)
            return null;

        return _objects[id.Value];
    }

    public bool Contains(Dbref id)
    {
        return id.IsValid && id.Value < _objects.Count && _objects[id.Value] != null;
    }

    public bool DestroyObject(Dbref id)
    {
        if (!Contains(id))
            return false;

        var existing = _objects[id.Value];
        if (existing == null)
            return false;

        existing.Type = ObjectType.Garbage;
        existing.Name = "Garbage";
        ObjectDestroyed?.Invoke(id);
        return true;
    }

    public void MoveObject(Dbref targetRef, Dbref destinationRef)
    {
        var target = GetObject(targetRef);
        if (target == null) return;

        // Remove from old container
        if (target.Location.IsValid)
        {
            var oldLoc = GetObject(target.Location);
            if (oldLoc != null)
            {
                if (oldLoc.Contents == targetRef)
                {
                    oldLoc.Contents = target.Next;
                }
                else
                {
                    var currRef = oldLoc.Contents;
                    while (currRef.IsValid)
                    {
                        var curr = GetObject(currRef);
                        if (curr == null) break;
                        if (curr.Next == targetRef)
                        {
                            curr.Next = target.Next;
                            break;
                        }
                        currRef = curr.Next;
                    }
                }
            }
        }

        // Add to new container
        target.Location = destinationRef;
        if (destinationRef.IsValid)
        {
            var newLoc = GetObject(destinationRef);
            if (newLoc != null)
            {
                target.Next = newLoc.Contents;
                newLoc.Contents = targetRef;
            }
            else
            {
                target.Next = Dbref.Nothing;
            }
        }
        else
        {
            target.Next = Dbref.Nothing;
        }
    }

    public void AttachExit(Dbref roomRef, Dbref exitRef)
    {
        var room = GetObject(roomRef);
        var exit = GetObject(exitRef);
        if (room == null || exit == null) return;

        exit.Exits = roomRef; // Source room stored in exit.Exits
        exit.Next = room.Exits;
        room.Exits = exitRef;
    }

    public void RemoveExit(Dbref roomRef, Dbref exitRef)
    {
        var room = GetObject(roomRef);
        var exit = GetObject(exitRef);
        if (room == null || exit == null) return;

        if (room.Exits == exitRef)
        {
            room.Exits = exit.Next;
        }
        else
        {
            var currRef = room.Exits;
            while (currRef.IsValid)
            {
                var curr = GetObject(currRef);
                if (curr == null) break;
                if (curr.Next == exitRef)
                {
                    curr.Next = exit.Next;
                    break;
                }
                currRef = curr.Next;
            }
        }

        exit.Exits = Dbref.Nothing;
        exit.Next = Dbref.Nothing;
    }

    public void LinkExit(Dbref exitRef, Dbref destinationRef)
    {
        var exit = GetObject(exitRef);
        if (exit == null) return;

        exit.Location = destinationRef; // Exit destination stored in exit.Location
    }

    public IEnumerable<MUSHObject> GetContents(Dbref containerRef)
    {
        var container = GetObject(containerRef);
        if (container == null) yield break;

        var seen = new HashSet<Dbref>();
        var currRef = container.Contents;
        while (currRef.IsValid && seen.Add(currRef))
        {
            var curr = GetObject(currRef);
            if (curr == null) yield break;
            yield return curr;
            currRef = curr.Next;
        }
    }

    public IEnumerable<MUSHObject> GetExits(Dbref roomRef)
    {
        var room = GetObject(roomRef);
        if (room == null) yield break;

        var seen = new HashSet<Dbref>();
        var currRef = room.Exits;
        while (currRef.IsValid && seen.Add(currRef))
        {
            var curr = GetObject(currRef);
            if (curr == null) yield break;
            yield return curr;
            currRef = curr.Next;
        }
    }

    public MUSHObject? FindExit(Dbref roomRef, string nameOrAlias)
    {
        if (string.IsNullOrWhiteSpace(nameOrAlias)) return null;
        string search = nameOrAlias.Trim();

        return _index.FindExit(roomRef, search, GetById);
    }

    public MUSHObject? FindInContents(Dbref containerRef, string nameOrDbref)
    {
        if (string.IsNullOrWhiteSpace(nameOrDbref)) return null;
        string search = nameOrDbref.Trim();

        if (search.StartsWith('#') && int.TryParse(search.Substring(1), out int id))
        {
            var obj = GetObject(new Dbref(id));
            if (obj != null && obj.Location == containerRef) return obj;
        }

        foreach (var item in GetContents(containerRef))
        {
            if (item.Name.Equals(search, StringComparison.OrdinalIgnoreCase))
                return item;
        }

        // Prefix match fallback
        foreach (var item in GetContents(containerRef))
        {
            if (item.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase))
                return item;
        }

        return null;
    }

    public MUSHObject? FindPlayer(string nameOrDbref)
    {
        if (string.IsNullOrWhiteSpace(nameOrDbref)) return null;
        string search = nameOrDbref.Trim();
        if (search.StartsWith('#') && int.TryParse(search.Substring(1), out int id))
        {
            var cand = GetObject(new Dbref(id));
            if (cand != null && cand.Type == ObjectType.Player) return cand;
        }

        return _index.FindPlayer(search, GetById);
    }

    public MUSHObject? FindObject(string nameOrDbref)
    {
        if (string.IsNullOrWhiteSpace(nameOrDbref)) return null;
        string search = nameOrDbref.Trim();
        if (search.StartsWith('#') && int.TryParse(search.Substring(1), out int id))
        {
            return GetObject(new Dbref(id));
        }

        return _index.FindByName(search, GetById);
    }

    public MUSHObject? ResolveGlobalObject(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
        string attrName = name.Trim().TrimStart('$');
        if (attrName.Length == 0)
            return null;

        var sysObj = GetObject(SystemObjectId);
        var attr = sysObj?.GetAttribute(attrName, this);
        if (attr == null || string.IsNullOrWhiteSpace(attr.Value))
            return null;

        string raw = attr.Value.Trim();
        if (raw.StartsWith('#'))
            raw = raw.Substring(1);
        return int.TryParse(raw, out int id) ? GetObject(new Dbref(id)) : null;
    }

    public MUSHObject? MatchObject(Dbref executor, string nameOrDbref)
    {
        if (string.IsNullOrWhiteSpace(nameOrDbref)) return null;
        string search = nameOrDbref.Trim();

        var execObj = GetObject(executor);

        if (search.Equals("here", StringComparison.OrdinalIgnoreCase))
        {
            return execObj != null && execObj.Location.IsValid ? GetObject(execObj.Location) : null;
        }

        if (search.Equals("me", StringComparison.OrdinalIgnoreCase))
        {
            return execObj;
        }

        if (search.StartsWith('*'))
        {
            return FindPlayer(search.Substring(1));
        }

        if (search.StartsWith('$') && search.Length > 1)
        {
            return ResolveGlobalObject(search.Substring(1));
        }

        if (search.StartsWith('#') && int.TryParse(search.Substring(1), out int id))
        {
            return GetObject(new Dbref(id));
        }

        if (int.TryParse(search, out int plainId))
        {
            return GetObject(new Dbref(plainId));
        }

        // 1. Check contents of current room (other players, objects)
        if (execObj != null && execObj.Location.IsValid)
        {
            var inRoom = FindInContents(execObj.Location, search);
            if (inRoom != null) return inRoom;

            // Check if current room matches
            var room = GetObject(execObj.Location);
            if (room != null && (room.Name.Equals(search, StringComparison.OrdinalIgnoreCase) ||
                                 room.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase)))
            {
                return room;
            }

            // Check exits in room
            var exit = FindExit(execObj.Location, search);
            if (exit != null) return exit;
        }

        // 2. Check inventory
        var inInv = FindInContents(executor, search);
        if (inInv != null) return inInv;

        // 3. Check executor by name
        if (execObj != null && (execObj.Name.Equals(search, StringComparison.OrdinalIgnoreCase) ||
                                execObj.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase)))
        {
            return execObj;
        }

        // 4. Check player by name globally
        var player = FindPlayer(search);
        if (player != null) return player;

        // 5. Global object lookup
        return FindObject(search);
    }
}