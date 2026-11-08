namespace PennMUSH.Space;

using PennMUSH.Space.Sensors;

public sealed class SpaceObject
{
    public int Id { get; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public CartesianCoordinates Position { get; set; } = new();
    public SphericalCoordinates Course { get; set; } = new();
    public double Speed { get; set; }
    public SensorState Sensors { get; } = new();
    public Dictionary<int, SpaceContact> Contacts { get; } = new();
    public List<ShipConsole> Consoles { get; } = new();

    public SpaceObject(int id)
    {
        Id = id;
    }

    public ShipConsole AddConsole(int consoleId, string station)
    {
        if (Consoles.Any(console => console.Id == consoleId))
            throw new InvalidOperationException($"Console {consoleId} is already on this ship.");

        var console = new ShipConsole(consoleId, station);
        Consoles.Add(console);
        return console;
    }
}
