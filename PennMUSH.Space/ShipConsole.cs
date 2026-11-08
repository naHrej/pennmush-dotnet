namespace PennMUSH.Space;

/// <summary>
/// One console on a ship. The operator is the player manning it, and is empty when the station is unmanned.
/// </summary>
public sealed class ShipConsole
{
    public int Id { get; }
    public string Station { get; }
    public int? OperatorId { get; set; }

    public ShipConsole(int id, string station)
    {
        Id = id;
        Station = station;
    }
}
