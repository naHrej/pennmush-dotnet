using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PennMUSH.Core;
using PennMUSH.Server.Engine;
using PennMUSH.Server.Network;
using PennMUSH.Server.Session;

var builder = WebApplication.CreateBuilder(args);

string dbPath = builder.Configuration["db"]
    ?? builder.Configuration["Database:Path"]
    ?? "mush.db";
string? configPath = builder.Configuration["config"]
    ?? builder.Configuration["Config:Path"];
string? importFlatfilePath = builder.Configuration["import-flatfile"]
    ?? builder.Configuration["ImportFlatfile"];
bool importOnly = args.Any(a => string.Equals(a, "--import-only", StringComparison.OrdinalIgnoreCase));

// Register Core MUSH Server Host (handles hot-reboots with zero connection drops)
builder.Services.AddSingleton(_ => new MUSHServerHost(dbPath, configPath, importFlatfilePath));
builder.Services.AddSingleton(sp => sp.GetRequiredService<MUSHServerHost>().Server);

// Register Telnet Listener Background Service (default port 4201)
builder.Services.AddHostedService<TelnetListenerService>(sp =>
{
    var host = sp.GetRequiredService<MUSHServerHost>();
    var logger = sp.GetRequiredService<ILogger<TelnetListenerService>>();
    int telnetPort = builder.Configuration.GetValue<int>("Telnet:Port", 4201);
    return new TelnetListenerService(host, logger, telnetPort);
});

var app = builder.Build();
var game = app.Services.GetRequiredService<MUSHServerHost>();
if (importOnly)
{
    Console.WriteLine($"Imported {game.Database.Count} objects into '{dbPath}'.");
    game.Dispose();
    return;
}
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
game.ProcessExit = () => lifetime.StopApplication();

// Enable WebSockets & Static Files (for stylesheets & assets)
app.UseWebSockets();
app.UseStaticFiles();

// Health Check Endpoint
app.MapGet("/health", (MUSHServerHost host) =>
{
    var server = host.Server;
    var body = server.Run(() => new
    {
        status = "Healthy",
        version = $"PennMUSH .NET {ServerVersion.Text}",
        objects = server.Database.Count,
        connectedSessions = host.Connections.Count,
        channels = server.Channels.GetAllChannels().Select(c => c.Name).ToArray()
    });
    return Results.Ok(body);
});

// Admin Hot-Reboot Endpoint
app.MapPost("/reboot", (MUSHServerHost host) =>
{
    host.Reboot();
    return Results.Ok(new { status = "Reboot completed", preservedConnections = host.Connections.Count });
});

app.MapGet("/html/stylesheet.css", async (HttpContext context, MUSHServerHost host) =>
{
    string? css = ScreenStylesheet.Load(host.Server.GameDirectory);
    if (css == null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    context.Response.Headers["Access-Control-Allow-Origin"] = "*";
    context.Response.ContentType = "text/css; charset=utf-8";
    await context.Response.WriteAsync(css);
});

// WebSocket Gateway Endpoints (/ws and /game for backward compatibility)
var handleWebSocket = async (HttpContext context, MUSHServerHost host) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var conn = new MUSHConnection(webSocket);
        conn.Host = context.Connection.RemoteIpAddress?.ToString() ?? "";
        conn.Capabilities.IsSsl = context.Request.IsHttps;
        conn.PublicBaseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        host.RegisterConnection(conn);

        var buffer = new byte[4096];
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await host.HandleInputAsync(conn, message);
                }
            }
        }
        catch
        {
            // Socket error / disconnect
        }
        finally
        {
            host.UnregisterConnection(conn);
        }
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Expected WebSocket connection.");
    }
};

app.Map("/ws", handleWebSocket);
app.Map("/game", handleWebSocket);

// Optional API endpoint (/api) for legacy dual-socket MUjs configurations
app.Map("/api", async (HttpContext context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var ws = await context.WebSockets.AcceptWebSocketAsync();
        var buf = new byte[1024];
        while (ws.State == WebSocketState.Open)
        {
            var r = await ws.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None);
            if (r.MessageType == WebSocketMessageType.Close) break;
        }
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsync("MUjs API Endpoint Active.");
    }
});

app.MapFallback(async (HttpContext context, MUSHServerHost host) =>
{
    string body = "";
    using (var reader = new StreamReader(context.Request.Body))
        body = await reader.ReadToEndAsync();
    var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var header in context.Request.Headers)
        headers[header.Key] = header.Value.ToString();
    var server = host.Server;
    var exchange = server.Run(() => server.Http.Handle(
        server.Dispatcher,
        server.Evaluator,
        server.Database,
        context.Request.Method,
        context.Request.Path + context.Request.QueryString,
        body,
        headers));
    if (!exchange.Handled)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    context.Response.StatusCode = exchange.Reply.StatusCode;
    context.Response.ContentType = exchange.Reply.MediaType;
    foreach (string header in exchange.Reply.Headers)
    {
        int split = header.IndexOf(':');
        if (split > 0)
            context.Response.Headers[header[..split].Trim()] = header[(split + 1)..].Trim();
    }

    if (!HttpMethods.IsHead(context.Request.Method))
        await context.Response.WriteAsync(exchange.Reply.Body.ToString());
});

app.Run();
