namespace PennMUSH.Space.Sensors;

using PennMUSH.Space;

/// <summary>
/// Aspace sensor scoring from space_iterate.c: signature, resolution, grid visibility,
/// and the contact level that both detects a body and limits its sensor report.
/// </summary>
public static class SensorScan
{
    public const double DetectionThreshold = 0.01;
    public const double ClassThreshold = 0.25;
    public const double IdentityThreshold = 0.50;
    public const double AxisLimit = SpaceConstants.PARSEC * 100.0;
    private const double SrsDistanceScale = 10101.010101;

    public static double GridVisibility(CartesianCoordinates position, bool dockedOrLanded)
    {
        if (dockedOrLanded)
            return 1.0;

        var px = position.X / SpaceConstants.PARSEC;
        var py = position.Y / SpaceConstants.PARSEC;
        var pz = position.Z / SpaceConstants.PARSEC;
        var dx = Math.Abs(px - RoundToGrid(px));
        var dy = Math.Abs(py - RoundToGrid(py));
        var dz = Math.Abs(pz - RoundToGrid(pz));
        var visibility = 1.1 - (1.0 / (1.0 + dx * dx + dy * dy + dz * dz));
        return Math.Clamp(visibility, 0.0, 1.0);
    }

    public static bool TryDetect(SpaceObject observer, SpaceObject target, out SpaceContact? contact)
    {
        contact = null;
        if (observer.Id == target.Id || observer.Sensors.Kind == SpaceBodyKind.None || target.Sensors.Kind == SpaceBodyKind.None)
            return false;
        if (observer.Sensors.Space != target.Sensors.Space || observer.Sensors.Location != target.Sensors.Location)
            return false;

        var dx = target.Position.X - observer.Position.X;
        var dy = target.Position.Y - observer.Position.Y;
        var dz = target.Position.Z - observer.Position.Z;
        if (Math.Abs(dx) > AxisLimit || Math.Abs(dy) > AxisLimit || Math.Abs(dz) > AxisLimit)
            return false;

        var resolution = ContactResolution(observer, target, dx, dy, dz);
        if (resolution < DetectionThreshold)
            return false;

        var range = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        var report = BuildReport(observer, target, resolution, range);
        contact = new SpaceContact(target.Id)
        {
            Name = report.Name ?? string.Empty,
            Bearing = report.Bearing,
            Elevation = report.Elevation,
            Range = range,
            Resolution = resolution,
            Report = report
        };
        return true;
    }

    public static double ContactResolution(SpaceObject observer, SpaceObject target, double dx, double dy, double dz)
    {
        var observerVisibility = GridVisibility(observer.Position, observer.Sensors.Docked || observer.Sensors.Landed);
        var targetVisibility = GridVisibility(target.Position, target.Sensors.Docked || target.Sensors.Landed);
        var (observerLrs, observerSrs) = Resolution(observer.Sensors);
        var (targetLrs, targetSrs) = Signature(target);

        var distanceSquared = dx * dx + dy * dy + dz * dz;
        var level = (observerSrs + 0.01) * targetSrs / (0.1 + distanceSquared / SrsDistanceScale);

        var px = dx / SpaceConstants.PARSEC;
        var py = dy / SpaceConstants.PARSEC;
        var pz = dz / SpaceConstants.PARSEC;
        level += observerLrs * targetLrs / (1.0 + (px * px + py * py + pz * pz) * 99.0);
        level *= observerVisibility * targetVisibility;

        if (level < DetectionThreshold)
            return level;
        if (target.Sensors.CloakActive && observer.Sensors.SensorTech < 2.0)
            level *= CloakLevel(target.Sensors, targetVisibility);
        return level;
    }

    public static (double LongRange, double ShortRange) Signature(SpaceObject body)
    {
        var sensors = body.Sensors;
        var stealth = sensors.Stealth <= 0 ? 1.0 : sensors.Stealth;
        var basis = Math.Pow(Math.Max(0.0, sensors.Displacement), 1.0 / 3.0) / stealth / 100.0;
        var longRange = basis * (body.Speed * body.Speed + 1.0);
        var shortRange = basis * 10.0 * (1.0 + sensors.MainPower + sensors.AuxPower / 10.0 + sensors.BatteryPower / 100.0);
        if (sensors.ElectronicWarfare)
        {
            longRange /= EcmFactor(sensors, longRangeBand: true);
            shortRange /= EcmFactor(sensors, longRangeBand: false);
        }
        return (longRange, shortRange);
    }

    public static (double LongRange, double ShortRange) Resolution(SensorState sensors)
    {
        var longRange = sensors.LrsActive ? sensors.SensorTech * sensors.LrsDamage : 0.0;
        var shortRange = sensors.SrsActive ? sensors.SensorTech * sensors.SrsDamage : 0.0;
        if (sensors.ElectronicWarfare)
        {
            longRange *= EccmFactor(sensors, longRangeBand: true);
            shortRange *= EccmFactor(sensors, longRangeBand: false);
        }
        if (sensors.CloakActive)
        {
            longRange /= 10.0;
            shortRange /= 10.0;
        }
        return (longRange, shortRange);
    }

    private static SensorReport BuildReport(SpaceObject observer, SpaceObject target, double resolution, double range)
    {
        var moving = target.Speed != 0.0;
        var identified = resolution > IdentityThreshold && !target.Sensors.CloakActive;
        var classified = resolution > ClassThreshold && !target.Sensors.CloakActive;
        return new SensorReport
        {
            Resolution = resolution,
            ResolutionPercent = Math.Clamp(resolution * 100.0, 0.0, 100.0),
            Kind = target.Sensors.Kind,
            Bearing = SpaceUtils.CalculateBearing(observer.Position, target.Position),
            Elevation = SpaceUtils.CalculateElevation(observer.Position, target.Position),
            Range = range,
            Speed = target.Speed,
            IsMoving = moving,
            HeadingYaw = moving ? target.Course.Bearing : null,
            HeadingPitch = moving ? target.Course.Elevation : null,
            ClassName = classified ? target.Sensors.ClassName : null,
            Displacement = classified ? target.Sensors.Displacement : null,
            Name = identified ? target.Name : null,
            CargoCapacity = identified ? target.Sensors.CargoCapacity : null,
            DockingDoorsOpen = identified && target.Sensors.HasDockingBay ? target.Sensors.DockingDoorsOpen : null,
            LandingDoorsOpen = identified && target.Sensors.HasLandingPad ? target.Sensors.LandingDoorsOpen : null,
            CanScan = identified
        };
    }

    private static double EcmFactor(SensorState sensors, bool longRangeBand)
    {
        return WarfareFactor(sensors.TotalPower, sensors.EcmAllocation, sensors.EwDamage, sensors.SensorTech, longRangeBand);
    }

    private static double EccmFactor(SensorState sensors, bool longRangeBand)
    {
        return WarfareFactor(sensors.TotalPower, sensors.EccmAllocation, sensors.EwDamage, sensors.SensorTech, longRangeBand);
    }

    private static double WarfareFactor(double totalPower, double allocation, double damage, double sensorTech, bool longRangeBand)
    {
        var scale = longRangeBand ? 10.0 : 1.0;
        return Math.Sqrt(1.0 + totalPower * allocation * damage * sensorTech / scale);
    }

    private static double CloakLevel(SensorState sensors, double visibility)
    {
        if (!sensors.CloakActive)
            return 1.0;
        if (sensors.TotalPower <= 0 || sensors.CloakAllocation <= 0 || sensors.CloakTech <= 0)
            return 1.0;

        var level = 0.001 / sensors.TotalPower / sensors.CloakAllocation / sensors.CloakTech * sensors.CloakCost;
        if (sensors.Tractored)
            level *= 100.0;
        if (sensors.Tractoring)
            level *= 100.0;
        if (sensors.BeamOutput > 1.0)
            level *= sensors.BeamOutput;
        if (sensors.MissileOutput > 1.0)
            level *= sensors.MissileOutput;
        if (visibility < 1.0)
            level *= (1.0 - visibility) * 10000.0;
        return Math.Min(1.0, level);
    }

    private static double RoundToGrid(double parsecs)
    {
        return Math.Round(parsecs / 100.0, MidpointRounding.AwayFromZero) * 100.0;
    }
}
