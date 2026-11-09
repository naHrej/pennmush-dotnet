namespace PennMUSH.Space;

using PennMUSH.Space.Sensors;

public sealed class SpaceManager
{
    private readonly Dictionary<int, SpaceObject> _objects = new();

    public IReadOnlyDictionary<int, SpaceObject> Objects => _objects;

    public SpaceObject CreateObject(int id)
    {
        if (_objects.ContainsKey(id))
            throw new InvalidOperationException($"Space object {id} already exists.");

        var ship = new SpaceObject(id);
        _objects[id] = ship;
        return ship;
    }

    public ShipConsole AddConsole(int shipId, int consoleId, int roomId, params string[] modes)
    {
        if (!_objects.TryGetValue(shipId, out var ship))
            throw new InvalidOperationException($"Space object {shipId} does not exist.");
        if (FindConsole(consoleId) != null)
            throw new InvalidOperationException($"Console {consoleId} is already bound to a ship.");

        return ship.AddConsole(consoleId, roomId, modes);
    }

    public void UpdateContacts()
    {
        foreach (var obj in _objects.Values)
        {
            if (!obj.IsActive)
            {
                obj.Contacts.Clear();
                continue;
            }

            var seen = new HashSet<int>();
            foreach (var target in _objects.Values)
            {
                if (!target.IsActive || target.Id == obj.Id)
                    continue;
                if (!SensorScan.TryDetect(obj, target, out var contact) || contact is null)
                    continue;

                seen.Add(target.Id);
                obj.Contacts[contact.Id] = contact;
            }

            foreach (var id in obj.Contacts.Keys.Where(id => !seen.Contains(id)).ToArray())
                obj.Contacts.Remove(id);
        }
    }

    public ShipConsole? FindConsole(int consoleId)
    {
        foreach (var ship in _objects.Values)
        {
            var console = ship.Consoles.FirstOrDefault(item => item.Id == consoleId);
            if (console != null)
                return console;
        }

        return null;
    }

    public bool ManConsole(int consoleId, int playerId, int playerRoomId)
    {
        var console = FindConsole(consoleId);
        if (console == null || playerRoomId != console.RoomId)
            return false;
        if (console.OperatorId is int other && other != playerId)
            return false;

        var already = ConsoleMannedBy(playerId);
        if (already != null && already.Id != console.Id)
            return false;

        console.OperatorId = playerId;
        return true;
    }

    public bool SelectMode(int consoleId, int playerId, string mode)
    {
        var console = FindConsole(consoleId);
        if (console == null || console.OperatorId != playerId || !console.OffersMode(mode))
            return false;

        console.ActiveMode = console.Modes.First(item =>
            string.Equals(item, mode, StringComparison.OrdinalIgnoreCase));
        return true;
    }

    public bool UnmanConsole(int consoleId, int playerId)
    {
        var console = FindConsole(consoleId);
        if (console == null || console.OperatorId != playerId)
            return false;

        console.OperatorId = null;
        console.ActiveMode = null;
        return true;
    }

    public IEnumerable<SpaceObject> ShipsMannedBy(int playerId)
    {
        var console = ConsoleMannedBy(playerId);
        if (console == null || !console.Ship.IsActive)
            return [];

        return [console.Ship];
    }

    private ShipConsole? ConsoleMannedBy(int playerId)
    {
        foreach (var ship in _objects.Values)
        {
            var console = ship.Consoles.FirstOrDefault(item => item.OperatorId == playerId);
            if (console != null)
                return console;
        }

        return null;
    }
}
