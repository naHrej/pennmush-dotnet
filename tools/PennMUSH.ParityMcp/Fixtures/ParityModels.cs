namespace PennMUSH.ParityMcp.Fixtures;

using System.Text.Json.Serialization;

public sealed class ParitySuiteFile
{
    [JsonPropertyName("group")]
    public string Group { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "function";

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("cases")]
    public List<ParityCase> Cases { get; set; } = new();
}

public sealed class ParityCase
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("setup")]
    public List<string>? Setup { get; set; }

    [JsonPropertyName("input")]
    public string Input { get; set; } = "";

    [JsonPropertyName("compare")]
    public string Compare { get; set; } = "exact";

    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }

    [JsonPropertyName("skip_reason")]
    public string? SkipReason { get; set; }
}

public sealed record ParityCaseResult(
    string Id,
    string Input,
    string Status,
    string COutput,
    string DotnetOutput,
    string? Note = null);

public sealed record ParitySuiteSummary(
    string Category,
    string Group,
    int Total,
    int Passed,
    int Mismatched,
    int MissingInDotnet,
    int Skipped,
    List<ParityCaseResult> Failures);
