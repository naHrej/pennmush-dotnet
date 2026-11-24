namespace PennMUSH.ParityMcp.Tools;

using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PennMUSH.ParityMcp.Fixtures;
using PennMUSH.ParityMcp.Sandboxes;

[McpServerToolType]
public sealed class ParityTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    [McpServerTool(Name = "eval_parity")]
    [Description("Run a MUSHcode expression or command over telnet against both PennMUSH C (Docker) and PennMUSH .NET, returning both outputs and whether they match.")]
    public static async Task<string> EvalParity(
        ParityRunner runner,
        [Description("Command or expression to run (e.g. 'think [add(1,2)]' or '@create Box')")] string input,
        [Description("Optional setup commands to run on both servers before input")] string[]? setup = null,
        [Description("Comparison mode: exact (default), ansi_stripped, regex, lines_unordered, error_equivalent")] string compareMode = "exact",
        [Description("Regex pattern when compareMode is regex")] string? pattern = null,
        [Description("Set true for queued commands (@switch, @dolist, @force, @trigger) so the sentinel waits on the queue")] bool queued = false,
        CancellationToken ct = default)
    {
        var result = await runner.EvalSingleAsync(input, setup, compareMode, pattern, queued, ct);
        return JsonSerializer.Serialize(result, JsonOpts);
    }

    [McpServerTool(Name = "eval_c")]
    [Description("Run a command or expression over telnet on the PennMUSH C oracle only.")]
    public static async Task<string> EvalC(
        ParitySandboxManager sandboxes,
        [Description("Command or expression to execute on PennMUSH C")] string input,
        CancellationToken ct = default)
    {
        string output = await sandboxes.EvalCAsync(input, ct: ct);
        return JsonSerializer.Serialize(new { engine = "pennmush-c", input, output }, JsonOpts);
    }

    [McpServerTool(Name = "eval_dotnet")]
    [Description("Run a command or expression over telnet on the PennMUSH .NET target only.")]
    public static async Task<string> EvalDotnet(
        ParitySandboxManager sandboxes,
        [Description("Command or expression to execute on PennMUSH .NET")] string input,
        CancellationToken ct = default)
    {
        string output = await sandboxes.EvalDotnetAsync(input, ct: ct);
        return JsonSerializer.Serialize(new { engine = "pennmush-dotnet", input, output }, JsonOpts);
    }

    [McpServerTool(Name = "list_parity_suites")]
    [Description("List available parity test suites under tests/parity/ (functions and commands).")]
    public static string ListParitySuites(
        ParityRunner runner,
        [Description("Optional category filter: functions or commands")] string? category = null)
    {
        var suites = runner.ListSuites(category)
            .Select(s => new { category = s.Category, group = s.Group, cases = s.CaseCount, file = s.FilePath });
        return JsonSerializer.Serialize(suites, JsonOpts);
    }

    [McpServerTool(Name = "run_parity_suite")]
    [Description("Run a specific parity suite (category: 'functions' or 'commands', group: e.g. 'testmath') against both PennMUSH C and .NET and return diffs.")]
    public static async Task<string> RunParitySuite(
        ParityRunner runner,
        [Description("Suite category: 'functions' or 'commands'")] string category,
        [Description("Group name (filename without .json, e.g. 'testmath')")] string group,
        [Description("Optional substring filter on case id")] string? caseFilter = null,
        CancellationToken ct = default)
    {
        var summary = await runner.RunSuiteAsync(category, group, caseFilter, ct);
        return JsonSerializer.Serialize(summary, JsonOpts);
    }

    [McpServerTool(Name = "reset_sandboxes")]
    [Description("Reset both PennMUSH C (Docker) and PennMUSH .NET to fresh clean minimal databases.")]
    public static async Task<string> ResetSandboxes(
        ParitySandboxManager sandboxes,
        CancellationToken ct = default)
    {
        await sandboxes.ResetAsync(ct);
        return JsonSerializer.Serialize(new
        {
            status = "reset_complete",
            cPort = sandboxes.CPort,
            dotnetPort = sandboxes.DotnetPort
        }, JsonOpts);
    }
}
