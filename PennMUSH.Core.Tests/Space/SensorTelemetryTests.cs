namespace PennMUSH.Core.Tests.Space;

using System.Text.Json;
using PennMUSH.Core.Types;
using PennMUSH.Server.Engine;
using PennMUSH.Server.Session;
using PennMUSH.Space;
using PennMUSH.Space.Sensors;

public class SensorTelemetryTests
{
    [Fact]
    public async Task PublishSensorContacts_GoesToThePlayerManningAConsole()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"sensor_telemetry_{Guid.NewGuid():N}.db");
        try
        {
            var server = new MUSHServer(dbPath);
            var helmLines = new List<string>();
            var bystanderLines = new List<string>();
            var helm = new MUSHConnection(line =>
            {
                helmLines.Add(line);
                return Task.CompletedTask;
            });
            var bystander = new MUSHConnection(line =>
            {
                bystanderLines.Add(line);
                return Task.CompletedTask;
            });
            helm.PlayerId = new Dbref(4);
            bystander.PlayerId = new Dbref(9);
            server.RegisterConnection(helm);
            server.RegisterConnection(bystander);

            var ship = server.Space.CreateObject(10);
            ship.IsActive = true;
            ship.Name = "Horizon";
            ship.Position = new CartesianCoordinates(0, 0, 0);
            ship.AddConsole(20, "helm");
            ship.AddConsole(21, "tactical");

            var target = server.Space.CreateObject(5);
            target.IsActive = true;
            target.Name = "Relay";
            target.Speed = 2;
            target.Course = new SphericalCoordinates(45, -10, 0);
            target.Sensors.ClassName = "Courier";
            target.Sensors.Displacement = 2_500_000;
            target.Position = new CartesianCoordinates(10, 0, 0);

            await server.PublishSensorContactsAsync();
            Assert.DoesNotContain(helmLines, line => line.Contains("space.telemetry"));

            Assert.True(server.Space.ManConsole(20, helm.PlayerId.Value));
            await server.PublishSensorContactsAsync();

            var message = helmLines.Last(line => line.Contains("space.telemetry"));
            using var doc = JsonDocument.Parse(message);
            var payload = doc.RootElement.GetProperty("payload");
            var contact = payload.GetProperty("contacts").EnumerateArray().Single();
            var report = contact.GetProperty("report");

            Assert.Equal("10", payload.GetProperty("shipId").GetString());
            Assert.Equal("Horizon", payload.GetProperty("name").GetString());
            Assert.Equal("Relay", report.GetProperty("name").GetString());
            Assert.Equal("Courier", report.GetProperty("className").GetString());
            Assert.Equal(2_500_000, report.GetProperty("displacement").GetDouble());
            Assert.True(report.GetProperty("resolution").GetDouble() > SensorScan.IdentityThreshold);
            Assert.Equal(45, report.GetProperty("headingYaw").GetDouble());
            Assert.Equal(-10, report.GetProperty("headingPitch").GetDouble());
            Assert.DoesNotContain(bystanderLines, line => line.Contains("space.telemetry"));

            Assert.True(server.Space.UnmanConsole(20, helm.PlayerId.Value));
            Assert.False(server.Space.ShipsMannedBy(helm.PlayerId.Value).Any());

            server.UnregisterConnection(helm);
            server.UnregisterConnection(bystander);
            server.Stop();
        }
        finally
        {
            foreach (var path in new[] { dbPath, dbPath + "-wal", dbPath + "-shm" })
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
        }
    }
}
