namespace PennMUSH.ParityMcp.Fixtures;

using System.Text.Json;
using System.Text.RegularExpressions;
using PennMUSH.ParityMcp.Sandboxes;

public sealed class ParityRunner
{
    private static readonly Regex AnsiRegex = new(@"\x1B\[[0-9;?]*[ -/]*[@-~]", RegexOptions.Compiled);

    private readonly ParitySandboxManager _sandboxes;

    public ParityRunner(ParitySandboxManager sandboxes)
    {
        _sandboxes = sandboxes;
    }

    public string ParityRoot => Path.Combine(_sandboxes.RepoRoot, "tests", "parity");

    public IReadOnlyList<(string Category, string Group, string FilePath, int CaseCount)> ListSuites(string? category = null)
    {
        var results = new List<(string, string, string, int)>();
        string[] categories = string.IsNullOrWhiteSpace(category)
            ? new[] { "functions", "commands" }
            : new[] { NormalizeCategoryFolder(category) };

        foreach (string catFolder in categories)
        {
            string dir = Path.Combine(ParityRoot, catFolder);
            if (!Directory.Exists(dir)) continue;

            foreach (string file in Directory.GetFiles(dir, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                var suite = JsonSerializer.Deserialize<ParitySuiteFile>(File.ReadAllText(file));
                if (suite != null)
                    results.Add((catFolder, suite.Group, file, suite.Cases.Count));
            }
        }

        return results;
    }

    public async Task<ParityCaseResult> EvalSingleAsync(
        string input,
        IReadOnlyList<string>? setup = null,
        string compareMode = "exact",
        string? pattern = null,
        CancellationToken ct = default)
    {
        if (setup != null)
        {
            foreach (string cmd in setup)
            {
                if (string.IsNullOrWhiteSpace(cmd)) continue;
                await _sandboxes.EvalCAsync(cmd, ct: ct);
                await _sandboxes.EvalDotnetAsync(cmd, ct: ct);
            }
        }

        string cOut = await _sandboxes.EvalCAsync(input, ct: ct);
        string dOut = await _sandboxes.EvalDotnetAsync(input, ct: ct);

        return EvaluateOutputs("ad-hoc", input, cOut, dOut, compareMode, pattern);
    }

    public async Task<ParitySuiteSummary> RunSuiteAsync(
        string category,
        string group,
        string? caseFilter = null,
        CancellationToken ct = default)
    {
        string catFolder = NormalizeCategoryFolder(category);
        string path = Path.Combine(ParityRoot, catFolder, $"{group}.json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Parity suite not found: {path}");

        var suite = JsonSerializer.Deserialize<ParitySuiteFile>(File.ReadAllText(path))
            ?? throw new InvalidDataException($"Failed to parse parity suite: {path}");

        int passed = 0, mismatched = 0, missing = 0, skipped = 0;
        var failures = new List<ParityCaseResult>();

        foreach (var c in suite.Cases)
        {
            if (!string.IsNullOrWhiteSpace(caseFilter) &&
                !c.Id.Contains(caseFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(c.SkipReason))
            {
                skipped++;
                continue;
            }

            var res = await EvalSingleAsync(c.Input, c.Setup, c.Compare, c.Pattern, ct);
            res = res with { Id = c.Id };

            switch (res.Status)
            {
                case "match":
                    passed++;
                    break;
                case "missing_in_dotnet":
                    missing++;
                    failures.Add(res);
                    break;
                default:
                    mismatched++;
                    failures.Add(res);
                    break;
            }
        }

        return new ParitySuiteSummary(
            catFolder,
            suite.Group,
            passed + mismatched + missing + skipped,
            passed,
            mismatched,
            missing,
            skipped,
            failures);
    }

    public static ParityCaseResult EvaluateOutputs(
        string id,
        string input,
        string cOut,
        string dOut,
        string compareMode = "exact",
        string? pattern = null)
    {
        string normC = NormalizeNewlines(cOut);
        string normD = NormalizeNewlines(dOut);

        if (IsMissingInDotnet(normC, normD))
        {
            return new ParityCaseResult(id, input, "missing_in_dotnet", normC, normD, "Function or command not implemented in C#.");
        }

        bool isMatch = compareMode.ToLowerInvariant() switch
        {
            "ansi_stripped" => string.Equals(StripAnsi(normC), StripAnsi(normD), StringComparison.Ordinal),
            "lines_unordered" => LinesUnorderedEqual(normC, normD),
            "regex" when !string.IsNullOrEmpty(pattern) =>
                Regex.IsMatch(normC, pattern, RegexOptions.Singleline) &&
                Regex.IsMatch(normD, pattern, RegexOptions.Singleline),
            "error_equivalent" => IsError(normC) && IsError(normD),
            _ => string.Equals(normC, normD, StringComparison.Ordinal)
        };

        return isMatch
            ? new ParityCaseResult(id, input, "match", normC, normD)
            : new ParityCaseResult(id, input, "behavior_mismatch", normC, normD);
    }

    private static bool IsMissingInDotnet(string cOut, string dOut)
    {
        bool dotnetMissingFunc = dOut.Contains("#-1 NO SUCH FUNCTION", StringComparison.OrdinalIgnoreCase);
        bool cMissingFunc = cOut.Contains("#-1 NO SUCH FUNCTION", StringComparison.OrdinalIgnoreCase);
        if (dotnetMissingFunc && !cMissingFunc) return true;

        bool dotnetHuh = dOut.Trim().StartsWith("Huh?", StringComparison.OrdinalIgnoreCase);
        bool cHuh = cOut.Trim().StartsWith("Huh?", StringComparison.OrdinalIgnoreCase);
        return dotnetHuh && !cHuh;
    }

    private static bool IsError(string output)
    {
        string trimmed = output.Trim();
        return trimmed.StartsWith("#-1", StringComparison.Ordinal)
            || trimmed.Contains("Permission denied", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("I don't see that", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("No such", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LinesUnorderedEqual(string a, string b)
    {
        var la = a.Split('\n').OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var lb = b.Split('\n').OrderBy(x => x, StringComparer.Ordinal).ToArray();
        return la.SequenceEqual(lb, StringComparer.Ordinal);
    }

    private static string StripAnsi(string value) => AnsiRegex.Replace(value, "");

    private static string NormalizeNewlines(string value) => value.Replace("\r\n", "\n");

    private static string NormalizeCategoryFolder(string category)
    {
        string c = category.Trim().ToLowerInvariant();
        return c switch
        {
            "function" or "functions" => "functions",
            "command" or "commands" => "commands",
            _ => c
        };
    }
}
