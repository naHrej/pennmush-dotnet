namespace PennMUSH.Core.Tests.Protocol;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.MUSHcode.Functions;
using PennMUSH.Core.Presentation;
using PennMUSH.Core.Protocol;
using PennMUSH.Core.Types;
using PennMUSH.Server.Engine;
using PennMUSH.Server.Session;
using Xunit;

public class Backend6CapabilitiesAndWebSocketTests
{
    [Fact]
    public async Task TelnetNegotiator_AndConnections_ExposeStructuredCapabilityFlagsInSessionInfoAndTermInfo()
    {
        var dbPath = TempDbPath();
        var server = new MUSHServer(dbPath);
        try
        {
            var god = server.Database.GetObject(new Dbref(1))!;
            var room = server.Database.GetObject(new Dbref(0))!;
            var alice = server.Database.CreateObject("Alice", ObjectType.Player, god.Id);
            alice.Owner = alice.Id;
            server.Database.MoveObject(alice.Id, room.Id);

            var telnetOut = new List<string>();
            var telnetConn = new MUSHConnection(msg =>
            {
                telnetOut.Add(msg);
                return Task.CompletedTask;
            })
            {
                PlayerId = god.Id,
                IsSsl = true
            };
            // Negotiate TTYPE xterm-256color + GMCP
            byte[] gmcpWill = new[] { TelnetConstants.IAC, TelnetConstants.WILL, TelnetConstants.TELOPT_GMCP };
            byte[] ttypePacket = new byte[] { TelnetConstants.IAC, TelnetConstants.SB, TelnetConstants.TELOPT_TTYPE, 0 }
                .Concat(Encoding.ASCII.GetBytes("xterm-256color"))
                .Concat(new[] { TelnetConstants.IAC, TelnetConstants.SE })
                .ToArray();
            telnetConn.Negotiator.ProcessInput(gmcpWill);
            telnetConn.Negotiator.ProcessInput(ttypePacket);
            Assert.True(telnetConn.Capabilities.SupportsGmcp);
            Assert.True(telnetConn.Capabilities.SupportsXterm256);

            var wsOut = new List<string>();
            var wsConn = new MUSHConnection(msg =>
            {
                wsOut.Add(msg);
                return Task.CompletedTask;
            })
            {
                PlayerId = alice.Id
            };
            wsConn.TelnetEnabled = false;

            server.RegisterConnection(telnetConn, isReconnect: true);
            server.RegisterConnection(wsConn, isReconnect: true);
            telnetOut.Clear();
            wsOut.Clear();

            // Complete MUjs WebSocket handshake with rgb24 and gmcp capabilities
            string helloJson = ProtocolSerializer.Serialize(
                "client.hello",
                new ClientHelloPayload("MUjsWeb", "2.0.0", new[] { "html5", "rgb24", "gmcp" }));
            await server.HandleInputAsync(wsConn, helloJson);

            // @sockset toggles stripaccents, prompt_newlines, colorstyle on God's telnet descriptor
            await server.HandleInputAsync(telnetConn, "@sockset =stripaccents yes prompt_newlines yes colorstyle xterm256");

            var snapshots = server.Functions.ConnectionsProvider!();
            var godSnap = Assert.Single(snapshots, s => s.PlayerId == god.Id);
            Assert.True(godSnap.SupportsTelnet);
            Assert.True(godSnap.SupportsGmcp);
            Assert.True(godSnap.IsSsl);
            Assert.False(godSnap.IsWebSocket);
            Assert.True(godSnap.StripAccents);
            Assert.True(godSnap.PromptNewlines);
            Assert.True(godSnap.SupportsXterm256);
            Assert.False(godSnap.SupportsRgb24);

            var aliceSnap = Assert.Single(snapshots, s => s.PlayerId == alice.Id);
            Assert.True(aliceSnap.IsWebSocket);
            Assert.True(aliceSnap.SupportsHtml);
            Assert.True(aliceSnap.SupportsRgb24);
            Assert.True(aliceSnap.SupportsXterm256);
            Assert.True(aliceSnap.SupportsGmcp);

            var godCtx = new EvaluationContext(god.Id, god.Id, god.Id, room.Id);
            var aliceCtx = new EvaluationContext(alice.Id, alice.Id, alice.Id, room.Id);

            string godTerm = server.Evaluator.Evaluate("[terminfo(me)]", godCtx);
            Assert.Contains("xterm-256color", godTerm);
            Assert.Contains("telnet", godTerm);
            Assert.Contains("gmcp", godTerm);
            Assert.Contains("ssl", godTerm);
            Assert.Contains("prompt_newlines", godTerm);
            Assert.Contains("stripaccents", godTerm);
            Assert.Contains("xterm256", godTerm);
            Assert.Equal("1", server.Evaluator.Evaluate("[ssl(me)]", godCtx));
            Assert.Equal("0", server.Evaluator.Evaluate("[pueblo(me)]", godCtx));

            string aliceTerm = server.Evaluator.Evaluate("[terminfo(me)]", aliceCtx);
            Assert.Contains("MUjsWeb", aliceTerm);
            Assert.Contains("html", aliceTerm);
            Assert.Contains("websocket", aliceTerm);
            Assert.Contains("gmcp", aliceTerm);
            Assert.Contains("rgb24", aliceTerm);
            Assert.Equal("1", server.Evaluator.Evaluate("[pueblo(me)]", aliceCtx));

            // SessionInfo via ConnectionDirectory also carries capabilities
            var dirField = typeof(MUSHServer).GetField("_sessions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dir = (PennMUSH.Core.Session.ISessionDirectory)dirField!.GetValue(server)!;
            var wsSession = Assert.Single(dir.List(), s => s.PlayerId == alice.Id);
            Assert.True(wsSession.IsWebSocket);
            Assert.True(wsSession.SupportsHtml);
            Assert.True(wsSession.SupportsRgb24);
        }
        finally
        {
            server.Stop();
            CleanupDb(dbPath);
        }
    }

    [Fact]
    public async Task Sockset_PrefixSuffixStripAccentsAndPromptNewlines_TransformServerOutput()
    {
        var dbPath = TempDbPath();
        var server = new MUSHServer(dbPath);
        try
        {
            var god = server.Database.GetObject(new Dbref(1))!;
            var sent = new List<string>();
            var conn = new MUSHConnection(msg =>
            {
                sent.Add(msg);
                return Task.CompletedTask;
            })
            {
                PlayerId = god.Id
            };
            server.RegisterConnection(conn, isReconnect: true);
            sent.Clear();

            // Enable OUTPUTPREFIX, OUTPUTSUFFIX, and STRIPACCENTS
            await server.HandleInputAsync(conn, "@sockset =outputprefix >>> outputsuffix <<< stripaccents yes");
            sent.Clear();

            await server.HandleInputAsync(conn, "think Café naïve résumé");
            Assert.Equal(
                new[] { ">>>\r\n", "Cafe naive resume\r\n", "<<<\r\n" },
                sent);

            // Clear prefix/suffix/stripaccents and verify preserved accents without wrapping
            await server.HandleInputAsync(conn, "@sockset =outputprefix  outputsuffix  stripaccents no");
            conn.OutputPrefix = "";
            conn.OutputSuffix = "";
            sent.Clear();

            await server.HandleInputAsync(conn, "think Café");
            Assert.Equal(new[] { "Café\r\n" }, sent);

            // PROMPT_NEWLINES = No (default) omits trailing \r\n on @prompt
            sent.Clear();
            await server.HandleInputAsync(conn, "@prompt/silent me=HP>");
            Assert.Equal(new[] { "HP>" }, sent);

            // PROMPT_NEWLINES = Yes appends \r\n on @prompt
            await server.HandleInputAsync(conn, "@sockset =prompt_newlines yes");
            sent.Clear();
            await server.HandleInputAsync(conn, "@prompt/silent me=HP>");
            Assert.Equal(new[] { "HP>\r\n" }, sent);
        }
        finally
        {
            server.Stop();
            CleanupDb(dbPath);
        }
    }

    [Fact]
    public async Task WsHtmlAndWsJson_FrameForWebSocketDescriptorsAndFallbackOtherwise()
    {
        var dbPath = TempDbPath();
        var server = new MUSHServer(dbPath);
        try
        {
            var god = server.Database.GetObject(new Dbref(1))!;
            var room = server.Database.CreateObject("Hall", ObjectType.Room, god.Id);
            var mortal = server.Database.CreateObject("Bob", ObjectType.Player, god.Id);
            mortal.Owner = mortal.Id;
            server.Database.MoveObject(god.Id, room.Id);
            server.Database.MoveObject(mortal.Id, room.Id);

            var telnetOut = new List<string>();
            var telnetConn = new MUSHConnection(msg =>
            {
                telnetOut.Add(msg);
                return Task.CompletedTask;
            })
            {
                PlayerId = mortal.Id
            };

            var wsOut = new List<string>();
            var wsConn = new MUSHConnection(msg =>
            {
                wsOut.Add(msg);
                return Task.CompletedTask;
            })
            {
                PlayerId = god.Id,
                IsWebSocket = true,
                ProtocolMode = ClientProtocolMode.MujsWebSocket
            };
            wsConn.Capabilities.SupportsHtml = true;

            server.RegisterConnection(telnetConn, isReconnect: true);
            server.RegisterConnection(wsConn, isReconnect: true);

            // Permissions: mortal lacks Pueblo_Send until granted; invalid JSON rejected
            var mortalCtx = new EvaluationContext(mortal.Id, mortal.Id, mortal.Id, room.Id);
            var godCtx = new EvaluationContext(god.Id, god.Id, god.Id, room.Id);
            Assert.Equal("#-1 PERMISSION DENIED", server.Evaluator.Evaluate("[wshtml(<b>hi</b>,hi)]", mortalCtx));
            Assert.Equal("#-1 PERMISSION DENIED", server.Evaluator.Evaluate("[wsjson({{\"a\":1}},a)]", mortalCtx));
            Assert.Equal("#-1 INVALID JSON", server.Evaluator.Evaluate("[wsjson(not-json,fallback)]", godCtx));

            await server.HandleInputAsync(wsConn, $"@power #{mortal.Id.Value}=Pueblo_Send");
            Assert.NotEqual("#-1 PERMISSION DENIED", server.Evaluator.Evaluate("[wshtml(<b>hi</b>,hi)]", mortalCtx));

            // Room broadcast with wshtml: WebSocket gets HTML, Telnet gets default text
            telnetOut.Clear();
            wsOut.Clear();
            await server.HandleInputAsync(wsConn, "say [wshtml(<a href=\"https://pennmush.org\">PennMUSH</a>,Go to https://pennmush.org)]");

            Assert.Single(telnetOut);
            Assert.Equal("One says, \"Go to https://pennmush.org\"\r\n", telnetOut[0]);

            Assert.Single(wsOut);
            var htmlEnvelope = ProtocolSerializer.DeserializePayload<TerminalOutputPayload>(wsOut[0]);
            Assert.NotNull(htmlEnvelope);
            Assert.Equal("<div>You say, \"<a href=\"https://pennmush.org\">PennMUSH</a>\"</div>", htmlEnvelope.Html);

            // Room broadcast with wsjson + fallback: WebSocket gets ws.json envelope, Telnet gets fallback
            telnetOut.Clear();
            wsOut.Clear();
            server.Evaluator.Evaluate("[emit([wsjson({{\"hp\":100}},HP: 100)])]", godCtx);
            await Task.Delay(20);

            Assert.Equal(new[] { "HP: 100\r\n" }, telnetOut);
            Assert.Single(wsOut);
            Assert.True(ProtocolSerializer.TryParseType(wsOut[0], out string? jsonType, out _));
            Assert.Equal("ws.json", jsonType);
            var jsonPayload = ProtocolSerializer.DeserializePayload<JsonElement>(wsOut[0]);
            Assert.Equal(100, jsonPayload.GetProperty("hp").GetInt32());

            // wsjson without fallback emits nothing to plain Telnet clients
            telnetOut.Clear();
            wsOut.Clear();
            server.Evaluator.Evaluate("[emit([wsjson({{\"tick\":1}})])]", godCtx);
            await Task.Delay(20);

            Assert.Empty(telnetOut);
            Assert.Single(wsOut);
            Assert.True(ProtocolSerializer.TryParseType(wsOut[0], out string? tickType, out _));
            Assert.Equal("ws.json", tickType);

            // oob() delivers JSON envelope to WebSocket, does not emit blank line to plain Telnet
            telnetOut.Clear();
            wsOut.Clear();
            await server.HandleInputAsync(wsConn, "think [oob(#" + god.Id.Value + " #" + mortal.Id.Value + ",Char.Vitals,{{\"mp\":50}})]");
            Assert.Empty(telnetOut);
            Assert.Contains(wsOut, msg =>
                ProtocolSerializer.TryParseType(msg, out var t, out _)
                && t == "Char.Vitals"
                && ProtocolSerializer.DeserializePayload<JsonElement>(msg).GetProperty("mp").GetInt32() == 50);
        }
        finally
        {
            server.Stop();
            CleanupDb(dbPath);
        }
    }

    private static string TempDbPath()
        => Path.Combine(Path.GetTempPath(), $"backend6_{Guid.NewGuid():N}.db");

    private static void CleanupDb(string dbPath)
    {
        foreach (string suffix in new[] { "", "-shm", "-wal" })
        {
            string candidate = dbPath + suffix;
            if (File.Exists(candidate))
            {
                try { File.Delete(candidate); } catch { }
            }
        }
    }
}
