namespace PennMUSH.Space;

/// <summary>
/// The space.telemetry payload for one ship. Contacts are what that ship's sensors are allowed to reveal.
/// </summary>
public sealed record SensorTelemetryMessage(string ShipId, string Name, IReadOnlyCollection<SpaceContact> Contacts)
{
    public static SensorTelemetryMessage For(SpaceObject observer) =>
        new(observer.Id.ToString(), observer.Name, observer.Contacts.Values.ToArray());
}
