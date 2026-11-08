namespace PennMUSH.Core.Tests.Space;

using PennMUSH.Space;
using PennMUSH.Space.Sensors;

public class SensorScanTests
{
    [Fact]
    public void UpdateContacts_KeepsANearbyShip()
    {
        var manager = new SpaceManager();
        var observer = Ship(manager, 1, 0, 0, 0);
        Ship(manager, 2, 10, 0, 0);

        manager.UpdateContacts();

        var contact = Assert.Single(observer.Contacts).Value;
        Assert.Equal(10, contact.Range, 3);
        Assert.True(contact.Resolution >= SensorScan.DetectionThreshold);
        Assert.NotNull(contact.Report);
        Assert.Equal(contact.Resolution, contact.Report.Resolution);
    }

    [Fact]
    public void MovingContact_ReportsHeadingAndSpeedBeforeMass()
    {
        var manager = new SpaceManager();
        var observer = Ship(manager, 1, 50 * SpaceConstants.PARSEC, 0, 0);
        var target = Ship(manager, 2, 51 * SpaceConstants.PARSEC, 0, 0);
        target.Name = "Relay";
        target.Speed = 2;
        target.Course = new SphericalCoordinates(45, -10, 0);
        target.Sensors.ClassName = "Courier";

        manager.UpdateContacts();

        var report = observer.Contacts[2].Report!;
        Assert.InRange(report.Resolution, SensorScan.DetectionThreshold, SensorScan.ClassThreshold);
        Assert.Equal(2, report.Speed);
        Assert.True(report.IsMoving);
        Assert.Equal(45, report.HeadingYaw);
        Assert.Equal(-10, report.HeadingPitch);
        Assert.Null(report.Displacement);
        Assert.Null(report.ClassName);
        Assert.Null(report.Name);
        Assert.False(report.CanScan);
    }

    [Fact]
    public void StoppedContact_OmitsHeadingUntilItIsMoving()
    {
        var manager = new SpaceManager();
        var observer = Ship(manager, 1, 0, 0, 0);
        var target = Ship(manager, 2, 10, 0, 0);
        target.Course = new SphericalCoordinates(180, 5, 0);
        target.Speed = 0;

        manager.UpdateContacts();

        var report = observer.Contacts[2].Report!;
        Assert.Equal(0, report.Speed);
        Assert.False(report.IsMoving);
        Assert.Null(report.HeadingYaw);
        Assert.Null(report.HeadingPitch);
    }

    [Fact]
    public void HighResolution_RevealsDisplacementThenName()
    {
        var manager = new SpaceManager();
        var observer = Ship(manager, 1, 0, 0, 0);
        var target = Ship(manager, 2, 10, 0, 0);
        target.Name = "Horizon";
        target.Sensors.ClassName = "Explorer";
        target.Sensors.Displacement = 2_500_000;
        target.Sensors.CargoCapacity = 400;

        manager.UpdateContacts();

        var report = observer.Contacts[2].Report!;
        Assert.True(report.Resolution > SensorScan.IdentityThreshold);
        Assert.Equal(2_500_000, report.Displacement);
        Assert.Equal("Explorer", report.ClassName);
        Assert.Equal("Horizon", report.Name);
        Assert.Equal(400, report.CargoCapacity);
        Assert.True(report.CanScan);
        Assert.Equal("Horizon", observer.Contacts[2].Name);
    }

    [Fact]
    public void Cloak_HidesIdentityEvenWhenTheScoreIsHigh()
    {
        var manager = new SpaceManager();
        var observer = Ship(manager, 1, 0, 0, 0);
        var target = Ship(manager, 2, 10, 0, 0);
        target.Name = "Hidden";
        target.Sensors.CloakActive = true;
        target.Sensors.CloakAllocation = 0.5;
        target.Sensors.TotalPower = 50;

        manager.UpdateContacts();

        var report = observer.Contacts[2].Report!;
        Assert.Null(report.Name);
        Assert.Null(report.Displacement);
        Assert.False(report.CanScan);
    }

    [Fact]
    public void Ecm_LowersTheResolutionTheObserverReceives()
    {
        var plain = Score(ecm: false);
        var jammed = Score(ecm: true);
        Assert.True(jammed < plain);
    }

    [Fact]
    public void Eccm_RaisesTheResolutionTheObserverReceives()
    {
        var plain = Score(ecm: true, eccm: false);
        var burning = Score(ecm: true, eccm: true);
        Assert.True(burning > plain);
    }

    [Fact]
    public void ContactBeyondOneAxisLimit_IsNotListed()
    {
        var manager = new SpaceManager();
        Ship(manager, 1, 0, 0, 0);
        var observer = manager.Objects[1];
        Ship(manager, 2, SpaceConstants.PARSEC * 101, 0, 0);

        manager.UpdateContacts();

        Assert.Empty(observer.Contacts);
    }

    private static double Score(bool ecm, bool eccm = false)
    {
        var manager = new SpaceManager();
        var observer = Ship(manager, 1, 0, 0, 0);
        var target = Ship(manager, 2, 10, 0, 0);
        if (ecm)
        {
            target.Sensors.ElectronicWarfare = true;
            target.Sensors.EcmAllocation = 1;
            target.Sensors.TotalPower = 100;
            target.Sensors.EwDamage = 1;
        }
        if (eccm)
        {
            observer.Sensors.ElectronicWarfare = true;
            observer.Sensors.EccmAllocation = 1;
            observer.Sensors.TotalPower = 100;
            observer.Sensors.EwDamage = 1;
        }

        manager.UpdateContacts();
        return observer.Contacts[2].Resolution;
    }

    private static SpaceObject Ship(SpaceManager manager, int id, double x, double y, double z)
    {
        var ship = manager.CreateObject(id);
        ship.IsActive = true;
        ship.Position = new CartesianCoordinates(x, y, z);
        return ship;
    }
}
