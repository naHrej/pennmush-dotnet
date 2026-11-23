namespace PennMUSH.Server.Session;

using PennMUSH.Core.Types;

/// <summary>
/// Connections grouped by the room their player is standing in.
/// Updated on connect, disconnect, and when that player moves.
/// </summary>
public sealed class RoomSessions
{
    private readonly Dictionary<int, List<MUSHConnection>> _byRoom = new();
    private readonly Dictionary<string, int> _roomOf = new();

    public void Place(MUSHConnection conn, Dbref room)
    {
        Remove(conn);
        if (!room.IsValid)
            return;
        if (!_byRoom.TryGetValue(room.Value, out var list))
            _byRoom[room.Value] = list = new List<MUSHConnection>();
        list.Add(conn);
        _roomOf[conn.Id] = room.Value;
    }

    public void Remove(MUSHConnection conn)
    {
        if (!_roomOf.Remove(conn.Id, out int room))
            return;
        if (!_byRoom.TryGetValue(room, out var list))
            return;
        list.Remove(conn);
        if (list.Count == 0)
            _byRoom.Remove(room);
    }

    public MUSHConnection[] In(Dbref room)
    {
        if (!room.IsValid || !_byRoom.TryGetValue(room.Value, out var list) || list.Count == 0)
            return Array.Empty<MUSHConnection>();
        return list.ToArray();
    }
}
