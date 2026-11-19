namespace PennMUSH.Core.Tests.Presentation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

public class Backend7ColorsMonikerHtmlTests
{
    [Fact]
    public void ColorsFunction_AndAnsiFunction_SupportFullX11TableAndConversions()
    {
        var db = new ObjectDatabase();
        var room = db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);
        var wiz = db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);
        wiz.Owner = wiz.Id;
        wiz.AddFlag("WIZARD");
        db.MoveObject(wiz.Id, room.Id);

        var registry = new FunctionRegistry(db);
        var evaluator = new ExpressionEvaluator(registry.Dispatch, db);
        registry.Evaluator = evaluator;
        var ctx = new EvaluationContext(wiz.Id, wiz.Id, wiz.Id, room.Id);

        // Full X11/CSS named_colors table (excludes xterm0..255 by default)
        string[] allNamed = evaluator.Evaluate("[colors()]", ctx).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(allNamed.Length >= 600);
        Assert.Contains("cornflowerblue", allNamed);
        Assert.Contains("dodgerblue", allNamed);
        Assert.Contains("aliceblue", allNamed);
        Assert.DoesNotContain("xterm196", allNamed);

        // Pattern listing includes xterm* when matched explicitly
        string[] xterms = evaluator.Evaluate("[colors(xterm19*)]", ctx).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains("xterm196", xterms);
        Assert.Contains("xterm199", xterms);

        // Format conversions matching PennMUSH colors()
        Assert.Equal("#6495ed", evaluator.Evaluate("[colors(cornflowerblue,hex)]", ctx));
        Assert.Equal("100 149 237", evaluator.Evaluate("[colors(cornflowerblue,rgb)]", ctx));
        Assert.Equal("69", evaluator.Evaluate("[colors(cornflowerblue,256color)]", ctx));
        Assert.Equal("45", evaluator.Evaluate("[colors(cornflowerblue,xterm256x)]", ctx));
        Assert.Equal("bh", evaluator.Evaluate("[colors(cornflowerblue,16color)]", ctx));
        Assert.Equal("cornflowerblue", evaluator.Evaluate("[colors(cornflowerblue,name)]", ctx));
        Assert.Equal("red red1", evaluator.Evaluate("[colors(#ff0000,name)]", ctx));
        Assert.Equal("#ff0000/#0000ee", evaluator.Evaluate("[colors(hr/b,hex)]", ctx));
        Assert.Equal("u#ff0000", evaluator.Evaluate("[colors(uh r,hex styles)]", ctx));

        Assert.Equal("#-1 INVALID COLOR", evaluator.Evaluate("[colors(notarealcolor,hex)]", ctx));
        Assert.Equal("#-1 COLORS() REQUIRES AT LEAST ONE COLOR", evaluator.Evaluate("[colors(h,hex)]", ctx));
        Assert.Equal("#-1 INVALID ARGUMENT", evaluator.Evaluate("[colors(red,bogusfmt)]", ctx));

        // ansi() with full named colors, bare xterm numbers, RGB triples, and fg/bg pairs
        Assert.Equal("\x1B[38;2;100;149;237msky\x1B[0m", evaluator.Evaluate("[ansi(cornflowerblue,sky)]", ctx));
        Assert.Equal("\x1B[38;2;255;215;0;48;2;0;0;128mbadge\x1B[0m", evaluator.Evaluate("[ansi(+gold/+navy,badge)]", ctx));
        Assert.Equal("\x1B[38;5;196mhot\x1B[0m", evaluator.Evaluate("[ansi(196,hot)]", ctx));
        Assert.Equal("\x1B[38;2;255;0;128mpink\x1B[0m", evaluator.Evaluate("[ansi(<255 0 128>,pink)]", ctx));
        Assert.Equal("\x1B[38;2;100;149;237mcorn\x1B[0m", evaluator.Evaluate("[ansi(<#6495ed>,corn)]", ctx));
        Assert.Equal("\x1B[1;31;44malert\x1B[0m", evaluator.Evaluate("[ansi(hr/b,alert)]", ctx));

        // valid() colorname and ansicolor categories
        Assert.Equal("1", evaluator.Evaluate("[valid(colorname,cornflowerblue)]", ctx));
        Assert.Equal("0", evaluator.Evaluate("[valid(colorname,notacolor)]", ctx));
        Assert.Equal("1", evaluator.Evaluate("[valid(ansicolor,+gold/navy)]", ctx));
        Assert.Equal("0", evaluator.Evaluate("[valid(ansicolor,???)]", ctx));
    }

    [Fact]
    public void PercentKSubstitution_AndMonikerValidation_WorkCorrectly()
    {
        var db = new ObjectDatabase();
        var room = db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);
        var alice = db.CreateObject("alice", ObjectType.Player, Dbref.Nothing);
        alice.Owner = alice.Id;
        alice.AddFlag("WIZARD");
        db.MoveObject(alice.Id, room.Id);

        var bob = db.CreateObject("bob", ObjectType.Player, alice.Id);
        bob.Owner = bob.Id;
        db.MoveObject(bob.Id, room.Id);

        var registry = new FunctionRegistry(db);
        var evaluator = new ExpressionEvaluator(registry.Dispatch, db);
        registry.Evaluator = evaluator;
        var dispatcher = new CommandDispatcher(evaluator);
        dispatcher.Register(new MonikerCommandHandler(evaluator), "@moniker");

        var aliceCtx = new EvaluationContext(alice.Id, alice.Id, alice.Id, room.Id);
        var bobCtx = new EvaluationContext(bob.Id, bob.Id, bob.Id, room.Id);
        var badCtx = new EvaluationContext(alice.Id, Dbref.Nothing, alice.Id, room.Id);

        // Unset moniker falls back to name; invalid enactor yields #-1
        Assert.Equal("bob", evaluator.Evaluate("%k", bobCtx));
        Assert.Equal("Bob", evaluator.Evaluate("%K", bobCtx));
        Assert.Equal("#-1", evaluator.Evaluate("%k", badCtx));

        // Plain text and underline/flash-only monikers are rejected
        var plainRes = dispatcher.Dispatch(alice.Id, $"@moniker #{alice.Id.Value}=plain", db);
        Assert.Contains("You need to specify a moniker with some ANSI.", plainRes.Notifications);
        Assert.Null(alice.GetAttribute("MONIKER"));

        var ufRes = dispatcher.Dispatch(alice.Id, $"@moniker #{alice.Id.Value}=[ansi(uf,alice)]", db);
        Assert.Contains("You need to specify a moniker with some ANSI.", ufRes.Notifications);
        Assert.Null(alice.GetAttribute("MONIKER"));

        // Valid color + underline strips underline (4) while preserving bold red (1;31)
        var okRes = dispatcher.Dispatch(alice.Id, $"@moniker #{alice.Id.Value}=[ansi(hr u,alice)]", db);
        Assert.Contains("Moniker set.", okRes.Notifications);
        Assert.Equal("\x1B[1;31malice\x1B[0m", alice.GetAttribute("MONIKER")!.Value);

        // %k returns ANSI moniker; %K capitalizes first visible letter after CSI prefix
        Assert.Equal("\x1B[1;31malice\x1B[0m", evaluator.Evaluate("%k", aliceCtx));
        Assert.Equal("\x1B[1;31mAlice\x1B[0m", evaluator.Evaluate("%K", aliceCtx));

        // Clearing removes MONIKER attribute
        var clearRes = dispatcher.Dispatch(alice.Id, $"@moniker #{alice.Id.Value}=", db);
        Assert.Contains("Moniker cleared.", clearRes.Notifications);
        Assert.Null(alice.GetAttribute("MONIKER"));
    }

    [Fact]
    public void AlignAndLAlign_SupportColumnAnsiAndAnsiAwareWrapping()
    {
        var db = new ObjectDatabase();
        var room = db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);
        var player = db.CreateObject("One", ObjectType.Player, Dbref.Nothing);
        player.Owner = player.Id;
        db.MoveObject(player.Id, room.Id);

        var registry = new FunctionRegistry(db);
        var evaluator = new ExpressionEvaluator(registry.Dispatch, db);
        registry.Evaluator = evaluator;
        var ctx = new EvaluationContext(player.Id, player.Id, player.Id, room.Id);

        // Column (ansi) wraps cell and padding
        Assert.Equal("\x1B[1;31mhi   \x1B[0m", evaluator.Evaluate("[align(5(hr),hi)]", ctx));
        Assert.Equal(
            "\x1B[38;2;100;149;237ma   \x1B[0m \x1B[38;2;255;136;0;48;2;0;0;128m  b\x1B[0m",
            evaluator.Evaluate("[align(4(+cornflowerblue) >3(#ff8800/navy),a,b)]", ctx));

        // Empty filler row in second column inherits column color
        Assert.Equal(
            "\x1B[31ma   \x1B[0m \x1B[34mc   \x1B[0m|\x1B[31mb   \x1B[0m \x1B[34m    \x1B[0m",
            evaluator.Evaluate("[align(4(r) 4(b),a%rb,c, ,%b,|)]", ctx));

        // Nested ansi() inside colored column resumes column color after inner \x1B[0m
        Assert.Equal(
            "\x1B[1;31m\x1B[32mhi\x1B[0m\x1B[1;31m yo   \x1B[0m",
            evaluator.Evaluate("[align(8(hr),[ansi(g,hi)] yo)]", ctx));

        // ANSI-aware wrapping does not count escape sequences against column width
        Assert.Equal(
            "\x1B[32mHello\x1B[0m|\x1B[32mworld\x1B[0m",
            evaluator.Evaluate("[align(5,[ansi(g,Hello world)], , ,|)]", ctx));

        // 0-width column and LALIGN (ansi)
        Assert.Equal("kept", evaluator.Evaluate("[align(0 4,skip,kept)]", ctx));
        Assert.Equal("\x1B[32mhi  \x1B[0m \x1B[34m  7\x1B[0m", evaluator.Evaluate("[lalign(4(g) >3(b),hi|7,|)]", ctx));
    }

    [Fact]
    public void AnsiToHtmlTranslator_AndRender_ConvertAnsiAndRespectColorDepth()
    {
        var db = new ObjectDatabase();
        var room = db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);
        var wiz = db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);
        wiz.Owner = wiz.Id;
        wiz.AddFlag("WIZARD");
        db.MoveObject(wiz.Id, room.Id);

        var registry = new FunctionRegistry(db);
        var evaluator = new ExpressionEvaluator(registry.Dispatch, db);
        registry.Evaluator = evaluator;
        var ctx = new EvaluationContext(wiz.Id, wiz.Id, wiz.Id, room.Id);

        // 16-color, bold-bright upgrade, xterm256, 24-bit RGB, and text styles
        Assert.Equal("<span style=\"color:#cd0000\">red</span>", AnsiToHtmlTranslator.Translate("\x1B[31mred\x1B[0m"));
        Assert.Equal(
            "<span style=\"color:#ff0000;font-weight:bold\">hi</span>",
            AnsiToHtmlTranslator.Translate("\x1B[1;31mhi\x1B[0m"));
        Assert.Equal(
            "<span style=\"color:#ff0000\">x</span>",
            AnsiToHtmlTranslator.Translate("\x1B[38;5;196mx\x1B[0m"));
        Assert.Equal(
            "<span style=\"color:#6495ed;background-color:#000080\">x</span>",
            AnsiToHtmlTranslator.Translate("\x1B[38;2;100;149;237;48;2;0;0;128mx\x1B[0m"));
        Assert.Equal(
            "<span class=\"ansi-blink\" style=\"color:#cdcd00;font-style:italic;text-decoration:underline line-through\">s</span>",
            AnsiToHtmlTranslator.Translate("\x1B[3;4;5;9;33ms\x1B[0m"));
        Assert.Equal(
            "<span style=\"color:#000000;background-color:#cd0000\">inv</span>",
            AnsiToHtmlTranslator.Translate("\x1B[7;31minv\x1B[0m"));

        // Escaping literal HTML inside ANSI
        Assert.Equal(
            "<span style=\"color:#cd0000\">&lt;script&gt;</span>",
            AnsiToHtmlTranslator.Translate("\x1B[31m<script>\x1B[0m", escapeHtml: true));

        // ColorDepth downsampling (Color256, Standard16, None)
        Assert.Equal(
            "<span style=\"color:#6495ed\">true</span>",
            AnsiToHtmlTranslator.Translate("\x1B[38;2;100;149;237mtrue\x1B[0m", colorMode: ColorDepth.TrueColor));
        Assert.Equal(
            "<span style=\"color:#5f87ff\">x256</span>",
            AnsiToHtmlTranslator.Translate("\x1B[38;2;100;149;237mx256\x1B[0m", colorMode: ColorDepth.Color256));
        Assert.Equal(
            "<span style=\"color:#5c5cff\">x16</span>",
            AnsiToHtmlTranslator.Translate("\x1B[38;2;100;149;237mx16\x1B[0m", colorMode: ColorDepth.Standard16));
        Assert.Equal(
            "&lt;b&gt;plain&lt;/b&gt;",
            AnsiToHtmlTranslator.Translate("\x1B[31m<b>plain</b>\x1B[0m", escapeHtml: true, colorMode: ColorDepth.None));

        // ConnectionSnapshot.ToClientCapabilities + OutputFormatter + render(str, ansi html)
        var snap = new ConnectionSnapshot(
            wiz.Id, 1, "localhost", "127.0.0.1", "MUjs", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            SupportsHtml: true, SupportsRgb24: true);
        Assert.Equal(
            "<span style=\"color:#ff0000\">R</span>",
            OutputFormatter.FormatOutput("\x1B[38;2;255;0;0mR\x1B[0m", snap.ToClientCapabilities()));
        Assert.Equal(
            "<span style=\"color:#cd0000\">&lt;b&gt;Red&lt;/b&gt;</span>",
            evaluator.Evaluate("[render(ansi(r,<b>Red</b>),ansi html)]", ctx));
    }

    [Fact]
    public async Task ServerPipeline_ConvertsAnsiForHtmlClients_StripsWhenPlain_AndKeepsTelnetAnsi()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"backend7_{Guid.NewGuid():N}.db");
        var server = new MUSHServer(dbPath);
        try
        {
            var god = server.Database.GetObject(new Dbref(1))!;
            var room = server.Database.GetObject(new Dbref(0))!;
            var bob = server.Database.CreateObject("bob", ObjectType.Player, god.Id);
            bob.Owner = bob.Id;
            bob.AddFlag("WIZARD");
            server.Database.MoveObject(bob.Id, room.Id);

            var telnetOut = new List<string>();
            var telnetConn = new MUSHConnection(msg =>
            {
                telnetOut.Add(msg);
                return Task.CompletedTask;
            })
            {
                PlayerId = bob.Id
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
            wsConn.Capabilities.ColorMode = ColorDepth.TrueColor;

            server.RegisterConnection(telnetConn, isReconnect: true);
            server.RegisterConnection(wsConn, isReconnect: true);
            telnetOut.Clear();
            wsOut.Clear();

            // @moniker + %k/%K end-to-end
            await server.HandleInputAsync(telnetConn, "@moniker me=[ansi(hr u,bob)]");
            telnetOut.Clear();
            await server.HandleInputAsync(telnetConn, "think %k|%K");
            Assert.Equal("\x1B[1;31mbob\x1B[0m|\x1B[1;31mBob\x1B[0m\r\n", Assert.Single(telnetOut));

            // Broadcast ANSI to both HTML WebSocket client and Telnet client
            telnetOut.Clear();
            wsOut.Clear();
            await server.HandleInputAsync(wsConn, "say [ansi(cornflowerblue,Sky)]");

            Assert.Equal("One says, \"\x1B[38;2;100;149;237mSky\x1B[0m\"\r\n", Assert.Single(telnetOut));
            var sayPayload = ProtocolSerializer.DeserializePayload<TerminalOutputPayload>(Assert.Single(wsOut));
            Assert.NotNull(sayPayload);
            Assert.Equal("<div>You say, \"<span style=\"color:#6495ed\">Sky</span>\"</div>", sayPayload.Html);
            Assert.Equal("You say, \"Sky\"", sayPayload.Plain);

            // Mixed wshtml() + ansi() + escapeHtml=true without double escaping
            wsOut.Clear();
            string mixed = server.Evaluator.Evaluate("[wshtml(<b>badge</b>,badge)] [ansi(r,<script>)]", new EvaluationContext(god.Id, god.Id, god.Id, room.Id));
            await wsConn.SendLineAsync(mixed, escapeHtml: true);
            var mixedPayload = ProtocolSerializer.DeserializePayload<TerminalOutputPayload>(Assert.Single(wsOut));
            Assert.NotNull(mixedPayload);
            Assert.Equal("<div><b>badge</b> <span style=\"color:#cd0000\">&lt;script&gt;</span></div>", mixedPayload.Html);

            // @sockset =colorstyle plain strips ANSI on BOTH HTML and Telnet clients
            await server.HandleInputAsync(wsConn, "@sockset =colorstyle plain");
            await server.HandleInputAsync(telnetConn, "@sockset =colorstyle plain");
            telnetOut.Clear();
            wsOut.Clear();

            await server.HandleInputAsync(wsConn, "say [ansi(r,PlainNow)]");
            Assert.Equal("One says, \"PlainNow\"\r\n", Assert.Single(telnetOut));
            var plainPayload = ProtocolSerializer.DeserializePayload<TerminalOutputPayload>(Assert.Single(wsOut));
            Assert.NotNull(plainPayload);
            Assert.Equal("<div>You say, \"PlainNow\"</div>", plainPayload.Html);
            Assert.DoesNotContain("<span", plainPayload.Html);
        }
        finally
        {
            server.Stop();
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
}
