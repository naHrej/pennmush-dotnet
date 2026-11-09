namespace PennMUSH.Space;

/// <summary>
/// A MUSH object in a room aboard a ship. One player can man it, then choose one of its modes.
/// </summary>
public sealed class ShipConsole
{
    public int Id { get; }
    public SpaceObject Ship { get; }
    public int RoomId { get; }
    public IReadOnlyList<string> Modes { get; }
    public string? ActiveMode { get; internal set; }
    public int? OperatorId { get; internal set; }

    internal ShipConsole(SpaceObject ship, int id, int roomId, IEnumerable<string> modes)
    {
        Ship = ship;
        Id = id;
        RoomId = roomId;
        Modes = modes
            .Where(mode => !string.IsNullOrWhiteSpace(mode))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool OffersMode(string mode) =>
        Modes.Any(item => string.Equals(item, mode, StringComparison.OrdinalIgnoreCase));
}
