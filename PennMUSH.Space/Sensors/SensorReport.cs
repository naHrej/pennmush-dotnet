namespace PennMUSH.Space.Sensors;

/// <summary>
/// What one ship is allowed to know about a contact at the current resolution.
/// Course is reported for a moving contact, and at classification even when it is stopped.
/// Mass and class arrive at classification. The ship's own name arrives at identification.
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
