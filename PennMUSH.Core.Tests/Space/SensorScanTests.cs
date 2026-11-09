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
        Assert.Equal("Contact #2", report.Name);
        Assert.Equal("Contact #2", observer.Contacts[2].Name);
        Assert.False(report.CanScan);
    }

    [Fact]
    public void ClassifiedStoppedContact_ReportsFacing()
    {
        var manager = new SpaceManager();
        var observer = Ship(manager, 1, 0, 0, 0);
        var target = Ship(manager, 2, 10, 0, 0);
        target.Course = new SphericalCoordinates(180, 5, 0);
        target.Speed = 0;

        manager.UpdateContacts();

        var report = observer.Contacts[2].Report!;
        Assert.True(report.Resolution > SensorScan.ClassThreshold);
        Assert.Equal(0, report.Speed);
        Assert.False(report.IsMoving);
        Assert.Equal(180, report.HeadingYaw);
        Assert.Equal(5, report.HeadingPitch);
    }

    [Fact]
    public void SlowContactBelowClassification_OmitsHeading()
    {
        var manager = new SpaceManager();
        var observer = Ship(manager, 1, 50 * SpaceConstants.PARSEC, 0, 0);
        var target = Ship(manager, 2, 51 * SpaceConstants.PARSEC, 0, 0);
        target.Course = new SphericalCoordinates(12, 3, 0);
        target.Speed = SensorScan.MovingSpeed;

        manager.UpdateContacts();

        var report = observer.Contacts[2].Report!;
        Assert.InRange(report.Resolution, SensorScan.DetectionThreshold, SensorScan.ClassThreshold);
        Assert.False(report.IsMoving);
        Assert.Null(report.HeadingYaw);
        Assert.Null(report.HeadingPitch);
    }

    [Fact]
    public void ContactAboveMovingSpeed_ReportsHeadingBeforeClassification()
    {
        var manager = new SpaceManager();
        var observer = Ship(manager, 1, 50 * SpaceConstants.PARSEC, 0, 0);
        var target = Ship(manager, 2, 51 * SpaceConstants.PARSEC, 0, 0);
        target.Course = new SphericalCoordinates(12, 3, 0);
        target.Speed = SensorScan.MovingSpeed + 0.01;

        manager.UpdateContacts();

        var report = observer.Contacts[2].Report!;
        Assert.True(report.Resolution <= SensorScan.ClassThreshold);
        Assert.True(report.IsMoving);
        Assert.Equal(12, report.HeadingYaw);
        Assert.Equal(3, report.HeadingPitch);
    }

    [Fact]
    public void ClassifiedContact_UsesTheClassUntilTheShipNameResolves()
    {
        SensorReport? classified = null;
        for (var tech = 0.05; tech <= 2.0; tech += 0.05)
        {
            var manager = new SpaceManager();
            var observer = Ship(manager, 1, 0, 0, 0);
            observer.Sensors.SensorTech = tech;
            var target = Ship(manager, 2, 10, 0, 0);
            target.Name = "Secret";
            target.Sensors.ClassName = "Courier";
            manager.UpdateContacts();
            if (!observer.Contacts.TryGetValue(2, out var contact) || contact.Report is null)
                continue;
            var report = contact.Report;
            if (report.Resolution > SensorScan.ClassThreshold && report.Resolution <= SensorScan.IdentityThreshold)
            {
                classified = report;
                break;
            }
        }

        Assert.NotNull(classified);
        Assert.Equal("Courier", classified.ClassName);
        Assert.Equal("Courier Class Contact", classified.Name);
        Assert.False(classified.CanScan);
        Assert.Null(classified.CargoCapacity);
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
        Assert.Equal("Contact #2", report.Name);
        Assert.Null(report.ClassName);
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
    public void DistantContact_StaysListedWhenTheScoreStillClearsDetection()
    {
        var manager = new SpaceManager();
        var observer = Ship(manager, 1, 0, 0, 0);
        var faint = Ship(manager, 2, SpaceConstants.PARSEC * 101, 0, 0);
        var bright = Ship(manager, 3, SpaceConstants.PARSEC * 101, 0, 0);
        bright.Sensors.Displacement = 1e22;

        manager.UpdateContacts();

        Assert.False(observer.Contacts.ContainsKey(faint.Id));
        Assert.True(observer.Contacts[bright.Id].Resolution >= SensorScan.DetectionThreshold);
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
