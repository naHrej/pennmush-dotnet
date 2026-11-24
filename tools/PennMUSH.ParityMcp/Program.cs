using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using PennMUSH.ParityMcp.Fixtures;
using PennMUSH.ParityMcp.Sandboxes;

if (args.Contains("--smoke-test", StringComparer.OrdinalIgnoreCase))
{
    await using var sandboxes = new ParitySandboxManager();
    var runner = new ParityRunner(sandboxes);
    var res = await runner.EvalSingleAsync("think [add(2,3)]");
    Console.WriteLine($"C: '{res.COutput}' | .NET: '{res.DotnetOutput}' | Status: {res.Status}");
    return res.Status == "match" ? 0 : 1;
}

if (args.Length >= 3 && args[0].Equals("--run", StringComparison.OrdinalIgnoreCase))
{
    await using var sandboxes = new ParitySandboxManager();
    var runner = new ParityRunner(sandboxes);
    var summary = await runner.RunSuiteAsync(args[1], args[2], args.Length > 3 ? args[3] : null);
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(summary, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    return 0;
}

if (args.Length >= 1 && (args[0].Equals("--run-all", StringComparison.OrdinalIgnoreCase) || args[0].Equals("--run-category", StringComparison.OrdinalIgnoreCase)))
{
    bool byCat = args[0].Equals("--run-category", StringComparison.OrdinalIgnoreCase);
    string? catFilter = byCat && args.Length >= 2 ? args[1] : null;
    string outPath = byCat
        ? (args.Length >= 3 ? args[2] : Path.Combine(Path.GetTempPath(), "parity-report.json"))
        : (args.Length >= 2 ? args[1] : Path.Combine(Path.GetTempPath(), "parity-report.json"));
    await using var sandboxes = new ParitySandboxManager();
    var runner = new ParityRunner(sandboxes);
    var results = new List<PennMUSH.ParityMcp.Fixtures.ParitySuiteSummary>();
    foreach (var suite in runner.ListSuites(catFilter))
    {
        await sandboxes.ResetAsync();
        var summary = await runner.RunSuiteAsync(suite.Category, suite.Group);
        results.Add(summary);
        Console.WriteLine($"[{summary.Category}/{summary.Group}] total={summary.Total} pass={summary.Passed} mismatch={summary.Mismatched} missing={summary.MissingInDotnet} skip={summary.Skipped}");
    }
    var dir = Path.GetDirectoryName(outPath);
    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    File.WriteAllText(outPath, System.Text.Json.JsonSerializer.Serialize(results, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Wrote full report to {outPath}");
    return 0;
}

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(opts => opts.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddSingleton<ParitySandboxManager>();
builder.Services.AddSingleton<ParityRunner>();
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
return 0;
