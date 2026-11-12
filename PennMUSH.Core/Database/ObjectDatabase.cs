namespace PennMUSH.Core.Database;

using System.Collections.Generic;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class ObjectDatabase
{
    private readonly List<MUSHObject?> _objects = new();
    private readonly HashSet<Dbref> _connected = new();

    public int Count => _objects.Count;
    public Dbref Top => new Dbref(_objects.Count);

    public void Clear()
    {
        _objects.Clear();
        _connected.Clear();
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
        _objects[obj.Id.Value] = obj;
    }

    public MUSHObject CreateObject(string name, ObjectType type, Dbref owner)
    {
        var id = new Dbref(_objects.Count);
        var obj = new MUSHObject(id, name, type, owner);
        _objects.Add(obj);
        return obj;
    }

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

        foreach (var exit in GetExits(roomRef))
        {
            // Exit name may contain aliases separated by ';' (e.g. "North;n;out")
            var aliases = exit.Name.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var alias in aliases)
            {
                if (alias.Equals(search, StringComparison.OrdinalIgnoreCase))
                    return exit;
            }
        }

        return null;
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

        return _objects.FirstOrDefault(o => o != null && o.Type == ObjectType.Player &&
            string.Equals(o.Name, search, StringComparison.OrdinalIgnoreCase));
    }

    public MUSHObject? FindObject(string nameOrDbref)
    {
        if (string.IsNullOrWhiteSpace(nameOrDbref)) return null;
        string search = nameOrDbref.Trim();
        if (search.StartsWith('#') && int.TryParse(search.Substring(1), out int id))
        {
            return GetObject(new Dbref(id));
        }

        return _objects.FirstOrDefault(o => o != null && string.Equals(o.Name, search, StringComparison.OrdinalIgnoreCase));
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