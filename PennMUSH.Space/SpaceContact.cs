namespace PennMUSH.Space;

using PennMUSH.Space.Sensors;

/// <summary>
/// One row in a ship's sensor contact list. The name stays blank until the
/// resolution report is allowed to reveal it.
/// </summary>
public sealed class SpaceContact
{
    public int Id { get; }
    public string Name { get; set; } = string.Empty;
    public double Bearing { get; set; }
    public double Elevation { get; set; }
    public double Range { get; set; }
    public double Resolution { get; set; }
    public SensorReport? Report { get; set; }

    public SpaceContact(int id)
    {
        Id = id;
    }
}
