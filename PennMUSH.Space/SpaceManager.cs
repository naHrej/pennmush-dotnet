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

    public bool ManConsole(int consoleId, int playerId)
    {
        var console = FindConsole(consoleId);
        if (console == null)
            return false;

        console.OperatorId = playerId;
        return true;
    }

    public bool UnmanConsole(int consoleId, int playerId)
    {
        var console = FindConsole(consoleId);
        if (console == null || console.OperatorId != playerId)
            return false;

        console.OperatorId = null;
        return true;
    }

    public IEnumerable<SpaceObject> ShipsMannedBy(int playerId)
    {
        return _objects.Values.Where(ship =>
            ship.IsActive && ship.Consoles.Any(console => console.OperatorId == playerId));
    }
}
