namespace PennMUSH.Server.Session;

/// <summary>
/// One unit of output after accent, HTML, and newline policy have been applied.
/// The socket writer sends <see cref="Wire"/> as bytes or one JSON envelope, then
/// any <see cref="JsonPayloads"/> as their own envelopes.
/// </summary>
public readonly record struct OutboundRecord(
    string Wire,
    string? Plain,
    bool PreserveHtml,
    IReadOnlyList<string> JsonPayloads)
{
    public static OutboundRecord Passthrough(string wire)
        => new(wire, null, false, Array.Empty<string>());
}
