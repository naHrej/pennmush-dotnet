namespace PennMUSH.Core.Tests.Protocol;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Protocol;
using PennMUSH.Core.Types;
using PennMUSH.Server.Engine;
using PennMUSH.Server.Session;
using Xunit;

public class MujsProtocolTests
{
    [Fact]
    public void ProtocolSerializer_RoundtripsEnvelopesAccurately()
    {
        var hello = new ClientHelloPayload("MUjs", "1.2.0", new[] { "html5", "editor", "styles", "docking" });
        string json = ProtocolSerializer.Serialize("client.hello", hello, "req-1");

        Assert.True(ProtocolSerializer.TryParseType(json, out var type, out var id));
        Assert.Equal("client.hello", type);
        Assert.Equal("req-1", id);

        var deserialized = ProtocolSerializer.DeserializePayload<ClientHelloPayload>(json);
        Assert.NotNull(deserialized);
        Assert.Equal("MUjs", deserialized.Client);
        Assert.Equal("1.2.0", deserialized.Version);
        Assert.Contains("editor", deserialized.Capabilities);
    }

    [Fact]
    public async Task ClientHello_NegotiatesMujsMode_AndReturnsServerHello()
    {
        var server = new MUSHServer();
        var sentMessages = new List<string>();

        var conn = new MUSHConnection(msg =>
        {
            sentMessages.Add(msg);
            return Task.CompletedTask;
        });

        server.RegisterConnection(conn);
        sentMessages.Clear();

        string helloJson = ProtocolSerializer.Serialize("client.hello", new ClientHelloPayload(
            Client: "MUjs",
            Version: "1.2.0",
            Capabilities: new[] { "html5", "editor", "docking", "audio" }
        ), "handshake-1");

        await server.HandleInputAsync(conn, helloJson);

        Assert.Equal(ClientProtocolMode.MujsWebSocket, conn.ProtocolMode);
        Assert.NotNull(conn.SessionToken);
        Assert.Contains("editor", conn.ClientCapabilities);

        Assert.Single(sentMessages);
        string replyJson = sentMessages[0];

        Assert.True(ProtocolSerializer.TryParseType(replyJson, out var replyType, out var replyId));
        Assert.Equal("server.hello", replyType);
        Assert.Equal("handshake-1", replyId);

        var serverHello = ProtocolSerializer.DeserializePayload<ServerHelloPayload>(replyJson);
        Assert.NotNull(serverHello);
        Assert.Equal("pennmush-dotnet", serverHello.Server);
        Assert.NotEmpty(serverHello.SessionToken);
        Assert.Contains("mushcode", serverHello.Features.SupportedLanguages);
    }

    [Fact]
    public async Task EditorRpc_OpenAndSave_UpdatesObjectAttribute()
    {
        var server = new MUSHServer();
        var sentMessages = new List<string>();

        var conn = new MUSHConnection(msg =>
        {
            sentMessages.Add(msg);
            return Task.CompletedTask;
        });

        server.RegisterConnection(conn);

        // Perform client hello
        string helloJson = ProtocolSerializer.Serialize("client.hello", new ClientHelloPayload("MUjs", "1.2.0", new[] { "editor" }));
        await server.HandleInputAsync(conn, helloJson);

        // Log in as God (player #1)
        var god = server.Database.GetObject(new Dbref(1));
        Assert.NotNull(god);
        conn.PlayerId = god.Id;
        sentMessages.Clear();

        // 1. Trigger @client/edit me/DESCRIBE
        string editCmd = ProtocolSerializer.Serialize("terminal.input", new TerminalInputPayload("@client/edit me/DESCRIBE"));
        await server.HandleInputAsync(conn, editCmd);

        Assert.NotEmpty(sentMessages);
        string openMsg = sentMessages.Last();
        Assert.True(ProtocolSerializer.TryParseType(openMsg, out var openType, out _));
        Assert.Equal("editor.open", openType);

        var openPayload = ProtocolSerializer.DeserializePayload<EditorOpenPayload>(openMsg);
        Assert.NotNull(openPayload);
        Assert.Equal("mushcode", openPayload.Language);
        Assert.Equal("DESCRIBE", openPayload.Target.Member);
        string sessionId = openPayload.SessionId;

        // 2. Perform editor.save
        sentMessages.Clear();
        string newCode = "You see Number One. {Refurbished with MUjs editor!}";
        string saveJson = ProtocolSerializer.Serialize("editor.save", new EditorSavePayload(sessionId, newCode), "save-1");
        await server.HandleInputAsync(conn, saveJson);

        Assert.Single(sentMessages);
        string resultMsg = sentMessages[0];
        Assert.True(ProtocolSerializer.TryParseType(resultMsg, out var resultType, out var resultId));
        Assert.Equal("editor.result", resultType);
        Assert.Equal("save-1", resultId);

        var resultPayload = ProtocolSerializer.DeserializePayload<EditorResultPayload>(resultMsg);
        Assert.NotNull(resultPayload);
        Assert.True(resultPayload.Success);
        Assert.Empty(resultPayload.Diagnostics);

        // Verify attribute was updated in database
        Assert.Equal(newCode, god.GetAttribute("DESCRIBE")?.Value);
    }

    [Fact]
    public async Task EditorRpc_SaveWithUnbalancedBraces_ReturnsDiagnostics()
    {
        var server = new MUSHServer();
        var sentMessages = new List<string>();

        var conn = new MUSHConnection(msg =>
        {
            sentMessages.Add(msg);
            return Task.CompletedTask;
        });

        server.RegisterConnection(conn);

        string helloJson = ProtocolSerializer.Serialize("client.hello", new ClientHelloPayload("MUjs", "1.2.0", new[] { "editor" }));
        await server.HandleInputAsync(conn, helloJson);

        var god = server.Database.GetObject(new Dbref(1))!;
        conn.PlayerId = god.Id;

        // Open editor session directly
        var openPayload = server.EditorSessions.StartAttributeSession(god.Id, god.Id, "TEST_ATTR", server.Database);
        Assert.NotNull(openPayload);

        // Save with unclosed brace
        string brokenCode = "think {Unbalanced brace without closing";
        string saveJson = ProtocolSerializer.Serialize("editor.save", new EditorSavePayload(openPayload.SessionId, brokenCode));
        sentMessages.Clear();
        await server.HandleInputAsync(conn, saveJson);

        Assert.Single(sentMessages);
        var resultPayload = ProtocolSerializer.DeserializePayload<EditorResultPayload>(sentMessages[0]);
        Assert.NotNull(resultPayload);
        Assert.True(resultPayload.Success); // Saved, but diagnostic warning returned
        Assert.NotEmpty(resultPayload.Diagnostics);
        Assert.Contains(resultPayload.Diagnostics, d => d.Message.Contains("brace"));
    }

    [Fact]
    public async Task ClientCommands_DispatchWindowStyleAudioEnvelopes()
    {
        var server = new MUSHServer();
        var sentMessages = new List<string>();

        var conn = new MUSHConnection(msg =>
        {
            sentMessages.Add(msg);
            return Task.CompletedTask;
        });

        server.RegisterConnection(conn);

        // Negotiate MUjs
        await server.HandleInputAsync(conn, ProtocolSerializer.Serialize("client.hello", new ClientHelloPayload("MUjs", "1.2.0", new[] { "docking", "styles", "audio" })));
        var god = server.Database.GetObject(new Dbref(1))!;
        conn.PlayerId = god.Id;
        sentMessages.Clear();

        // 1. @client/window
        await server.HandleInputAsync(conn, ProtocolSerializer.Serialize("terminal.input", new TerminalInputPayload("@client/window me=automap:dock-right:replace:<canvas id='m'></canvas>:/css/map.css")));
        Assert.NotEmpty(sentMessages);
        var windowMsg = sentMessages.Last();
        Assert.True(ProtocolSerializer.TryParseType(windowMsg, out var wType, out _));
        Assert.Equal("window.update", wType);
        var windowPayload = ProtocolSerializer.DeserializePayload<WindowUpdatePayload>(windowMsg);
        Assert.NotNull(windowPayload);
        Assert.Equal("automap", windowPayload.WindowId);
        Assert.Equal("dock-right", windowPayload.Target);
        Assert.Equal("replace", windowPayload.Action);

        // 2. @client/style
        sentMessages.Clear();
        await server.HandleInputAsync(conn, ProtocolSerializer.Serialize("terminal.input", new TerminalInputPayload("@client/style me=/css/themes/cyber.css")));
        Assert.NotEmpty(sentMessages);
        var styleMsg = sentMessages.Last();
        Assert.True(ProtocolSerializer.TryParseType(styleMsg, out var sType, out _));
        Assert.Equal("style.load", sType);
        var stylePayload = ProtocolSerializer.DeserializePayload<StyleLoadPayload>(styleMsg);
        Assert.NotNull(stylePayload);
        Assert.Equal("/css/themes/cyber.css", stylePayload.Url);

        // 3. @client/audio
        sentMessages.Clear();
        await server.HandleInputAsync(conn, ProtocolSerializer.Serialize("terminal.input", new TerminalInputPayload("@client/audio me=/audio/sfx/alarm.wav")));
        Assert.NotEmpty(sentMessages);
        var audioMsg = sentMessages.Last();
        Assert.True(ProtocolSerializer.TryParseType(audioMsg, out var aType, out _));
        Assert.Equal("audio.play", aType);
        var audioPayload = ProtocolSerializer.DeserializePayload<AudioPlayPayload>(audioMsg);
        Assert.NotNull(audioPayload);
        Assert.Equal("/audio/sfx/alarm.wav", audioPayload.Url);
    }

    [Fact]
    public async Task SoftcodeFunctions_DispatchProtocolEnvelopes()
    {
        var server = new MUSHServer();
        var sentMessages = new List<string>();

        var conn = new MUSHConnection(msg =>
        {
            sentMessages.Add(msg);
            return Task.CompletedTask;
        });

        server.RegisterConnection(conn);
        await server.HandleInputAsync(conn, ProtocolSerializer.Serialize("client.hello", new ClientHelloPayload("MUjs", "1.2.0", new[] { "docking", "styles" })));
        var god = server.Database.GetObject(new Dbref(1))!;
        conn.PlayerId = god.Id;
        sentMessages.Clear();

        // Evaluate [cwindow(#1, hud, dock-left, replace, <div>HP: 100</div>)]
        string res1 = server.Evaluator.Evaluate("[cwindow(#1, hud, dock-left, replace, <div>HP: 100</div>)]", new PennMUSH.Core.MUSHcode.EvaluationContext(god.Id, god.Id));
        Assert.Equal("1", res1);
        Assert.NotEmpty(sentMessages);
        Assert.Contains(sentMessages, m => m.Contains("\"type\":\"window.update\"") && m.Contains("hud"));

        // Evaluate [cstyle(#1, /css/neon.css)]
        sentMessages.Clear();
        string res2 = server.Evaluator.Evaluate("[cstyle(#1, /css/neon.css)]", new PennMUSH.Core.MUSHcode.EvaluationContext(god.Id, god.Id));
        Assert.Equal("1", res2);
        Assert.NotEmpty(sentMessages);
        Assert.Contains(sentMessages, m => m.Contains("\"type\":\"style.load\"") && m.Contains("/css/neon.css"));

        // Evaluate [caudio(#1, /sfx/jump.wav, 0.75, 0)]
        sentMessages.Clear();
        string res3 = server.Evaluator.Evaluate("[caudio(#1, /sfx/jump.wav, 0.75, 0)]", new PennMUSH.Core.MUSHcode.EvaluationContext(god.Id, god.Id));
        Assert.Equal("1", res3);
        Assert.NotEmpty(sentMessages);
        Assert.Contains(sentMessages, m => m.Contains("\"type\":\"audio.play\"") && m.Contains("/sfx/jump.wav"));
    }

    [Fact]
    public async Task LegacyTelnetConnection_ReceivesPlainTextFallbacks()
    {
        var server = new MUSHServer();
        var sentMessages = new List<string>();

        // Default telnet connection (not sending client.hello)
        var conn = new MUSHConnection(msg =>
        {
            sentMessages.Add(msg);
            return Task.CompletedTask;
        });

        server.RegisterConnection(conn);
        var god = server.Database.GetObject(new Dbref(1))!;
        conn.PlayerId = god.Id;
        sentMessages.Clear();

        // Send a window update to standard telnet player
        string res = server.Evaluator.Evaluate("[cwindow(#1, hud, dock-left, replace, <div>HP: 100</div>)]", new PennMUSH.Core.MUSHcode.EvaluationContext(god.Id, god.Id));
        Assert.Equal("1", res);

        Assert.NotEmpty(sentMessages);
        string text = sentMessages[0];
        // Must NOT be a raw JSON envelope
        Assert.False(text.StartsWith('{'));
        Assert.Contains("[Client Window: hud", text);
    }

    [Fact]
    public async Task HtmlMujsLine_WrapsBreaksInADivWithoutATrailingBreak()
    {
        var sent = new List<string>();
        var conn = new MUSHConnection(msg =>
        {
            sent.Add(msg);
            return Task.CompletedTask;
        });
        conn.Capabilities.SupportsHtml = true;
        conn.ProtocolMode = ClientProtocolMode.MujsWebSocket;

        await conn.SendLineAsync("Alpha\r\nBeta\nGamma");

        var payload = ProtocolSerializer.DeserializePayload<TerminalOutputPayload>(sent[0]);
        Assert.NotNull(payload);
        Assert.Equal("<div>Alpha<br/>Beta<br/>Gamma</div>", payload.Html);
    }

    [Fact]
    public async Task HtmlStreamLine_EndsWithAWireNewline()
    {
        var sent = new List<string>();
        var conn = new MUSHConnection(msg =>
        {
            sent.Add(msg);
            return Task.CompletedTask;
        });
        conn.Capabilities.SupportsHtml = true;

        await conn.SendLineAsync("Alpha\nBeta");

        Assert.Equal("<div>Alpha<br/>Beta</div>\r\n", sent[0]);
    }

    [Fact]
    public async Task PlainLine_KeepsTheWireNewline()
    {
        var sent = new List<string>();
        var conn = new MUSHConnection(msg =>
        {
            sent.Add(msg);
            return Task.CompletedTask;
        });

        await conn.SendLineAsync("Alpha\nBeta");

        Assert.Equal("Alpha\nBeta\r\n", sent[0]);
    }

    [Fact]
    public async Task HtmlMujsLine_LeavesProtocolJsonUnwrapped()
    {
        var sent = new List<string>();
        var conn = new MUSHConnection(msg =>
        {
            sent.Add(msg);
            return Task.CompletedTask;
        });
        conn.Capabilities.SupportsHtml = true;
        conn.ProtocolMode = ClientProtocolMode.MujsWebSocket;

        string json = "{\"type\":\"space.telemetry\",\"payload\":{\"speed\":1}}";
        await conn.SendLineAsync(json);

        var payload = ProtocolSerializer.DeserializePayload<TerminalOutputPayload>(sent[0]);
        Assert.NotNull(payload);
        Assert.Equal(json + "\r\n", payload.Html);
    }

    [Fact]
    public async Task HtmlMujsRawText_TurnsNewlinesIntoBreaks()
    {
        var sent = new List<string>();
        var conn = new MUSHConnection(msg =>
        {
            sent.Add(msg);
            return Task.CompletedTask;
        });
        conn.Capabilities.SupportsHtml = true;
        conn.ProtocolMode = ClientProtocolMode.MujsWebSocket;

        await conn.SendRawAsync("Line one\r\nLine two\n");

        var payload = ProtocolSerializer.DeserializePayload<TerminalOutputPayload>(sent[0]);
        Assert.NotNull(payload);
        Assert.Equal("Line one<br/>Line two<br/>", payload.Html);
    }
}
