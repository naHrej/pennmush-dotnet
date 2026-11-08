namespace PennMUSH.Space.Sensors;

/// <summary>
/// What one ship is allowed to know about a contact at the current resolution.
/// Course and speed are available as soon as the contact exists and is moving.
/// Mass, class, and name arrive only after the score clears aspace's thresholds.
/// </summary>
public sealed class SensorReport
{
    public double Resolution { get; init; }
    public double ResolutionPercent { get; init; }
    public SpaceBodyKind Kind { get; init; }
    public double Bearing { get; init; }
    public double Elevation { get; init; }
    public double Range { get; init; }
    public double Speed { get; init; }
    public bool IsMoving { get; init; }
    public double? HeadingYaw { get; init; }
    public double? HeadingPitch { get; init; }
    public string? ClassName { get; init; }
    public double? Displacement { get; init; }
    public string? Name { get; init; }
    public double? CargoCapacity { get; init; }
    public bool? DockingDoorsOpen { get; init; }
    public bool? LandingDoorsOpen { get; init; }
    public bool CanScan { get; init; }
}
