namespace PennMUSH.Space.Sensors;

/// <summary>
/// The aspace inputs that decide a body's signature and an observer's resolution.
/// </summary>
public sealed class SensorState
{
    public SpaceBodyKind Kind { get; set; } = SpaceBodyKind.Ship;
    public string ClassName { get; set; } = string.Empty;
    public int Space { get; set; }
    public int Location { get; set; }

    public double Displacement { get; set; } = 1_000_000;
    public double Stealth { get; set; } = 1;
    public double SensorTech { get; set; } = 1;
    public double CargoCapacity { get; set; }
    public bool HasDockingBay { get; set; }
    public bool HasLandingPad { get; set; }
    public bool DockingDoorsOpen { get; set; }
    public bool LandingDoorsOpen { get; set; }

    public bool SrsActive { get; set; } = true;
    public bool LrsActive { get; set; } = true;
    public double SrsDamage { get; set; } = 1;
    public double LrsDamage { get; set; } = 1;

    public double MainPower { get; set; }
    public double AuxPower { get; set; }
    public double BatteryPower { get; set; }
    public double TotalPower { get; set; }

    public bool ElectronicWarfare { get; set; }
    public double EcmAllocation { get; set; }
    public double EccmAllocation { get; set; }
    public double EwDamage { get; set; } = 1;

    public bool CloakActive { get; set; }
    public double CloakAllocation { get; set; }
    public double CloakTech { get; set; } = 1;
    public double CloakCost { get; set; } = 1;
    public double BeamOutput { get; set; }
    public double MissileOutput { get; set; }
    public bool Tractored { get; set; }
    public bool Tractoring { get; set; }

    public bool Docked { get; set; }
    public bool Landed { get; set; }
}
