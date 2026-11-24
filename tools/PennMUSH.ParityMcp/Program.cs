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
