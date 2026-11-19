namespace PennMUSH.Core.Tests.MUSHcode;

using System;
using System.Collections.Generic;
using Xunit;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.MUSHcode.Functions;
using PennMUSH.Core.Types;

public class Sections10_12_14_19FunctionTests
{
    private readonly ObjectDatabase _db;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly EvaluationContext _ctx;
    private readonly EvaluationContext _mortalCtx;
    private readonly List<Notification> _notifications = new();
    private readonly MUSHObject _room;
    private readonly MUSHObject _room2;
    private readonly MUSHObject _zone;
    private readonly MUSHObject _wiz;
    private readonly MUSHObject _alice;
    private readonly MUSHObject _hiddenBob;

    public Sections10_12_14_19FunctionTests()
    {
        _db = new ObjectDatabase();
        _room = _db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);             // #0
        _wiz = _db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);          // #1
        _wiz.Owner = _wiz.Id;
        _wiz.AddFlag("WIZARD");
        _db.MoveObject(_wiz.Id, _room.Id);

        _zone = _db.CreateObject("ZoneMaster", ObjectType.Thing, _wiz.Id);            // #2
        _room.Zone = _zone.Id;

        _room2 = _db.CreateObject("Annex", ObjectType.Room, _wiz.Id);                 // #3
        _room2.Zone = _zone.Id;

        _alice = _db.CreateObject("Alice", ObjectType.Player, _wiz.Id);               // #4
        _alice.Owner = _alice.Id;
        _alice.SetAttribute("NAMEACCENT", "..'..", _wiz.Id);
        _alice.SetAttribute("RQUOTA", "42", _wiz.Id);
        _alice.SetAttribute("FOLLOWERS", "#1", _wiz.Id);
        _alice.SetAttribute("FOLLOWING", "#1", _wiz.Id);
        _db.MoveObject(_alice.Id, _room.Id);

        _hiddenBob = _db.CreateObject("Bob", ObjectType.Player, _wiz.Id);             // #5
        _hiddenBob.Owner = _hiddenBob.Id;
        _hiddenBob.AddFlag("DARK");
        _db.MoveObject(_hiddenBob.Id, _room2.Id);

        var channels = new ChannelRegistry();
        channels.CreateChannel("Public", _wiz.Id, ChannelFlags.Public | ChannelFlags.Player, "General chat");
        var pub = channels.GetChannel("Public")!;
        pub.AddMember(_wiz.Id, "Admin");
        pub.AddMember(_alice.Id, "Guest");

        var now = DateTimeOffset.UtcNow;
        var connections = new List<ConnectionSnapshot>
        {
            new(_wiz.Id, 10, "localhost", "127.0.0.1", "xterm-256color", now.AddSeconds(-120), now.AddSeconds(-5), IsHidden: false),
            new(_alice.Id, 11, "alice.example.com", "10.0.0.2", "mushclient", now.AddSeconds(-60), now.AddSeconds(-15), IsHidden: false),
            new(_hiddenBob.Id, 12, "bob.example.com", "10.0.0.3", "tintin++", now.AddSeconds(-300), now.AddSeconds(-20), IsHidden: true)
        };

        var textEntries = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["help"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["WHO"] = "Lists connected players.",
                ["QUIT"] = "Disconnects from the MUSH."
            }
        };

        _registry = new FunctionRegistry(_db)
        {
            NotificationDispatcher = n => _notifications.Add(n),
            Channels = channels,
            PollQuestion = "Tabs or spaces?",
            ConnectionsProvider = () => connections,
            ConfigProvider = key => key == "custom_opt" ? "custom_val" : null,
            PasswordVerifier = (id, pw) => id == _alice.Id && pw == "secret",
            TextEntriesProvider = file => textEntries.TryGetValue(file, out var dict) ? dict.Keys : Array.Empty<string>(),
            TextFileEntryProvider = (file, topic) => textEntries.TryGetValue(file, out var dict) && dict.TryGetValue(topic, out var body) ? body : null
        };
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;

        _ctx = new EvaluationContext(_wiz.Id, _wiz.Id, _wiz.Id, _room.Id);
        _mortalCtx = new EvaluationContext(_alice.Id, _alice.Id, _alice.Id, _room.Id);
    }

    [Fact]
    public void Section10_ZoneAndNoSpoofEmits_DispatchNotifications()
    {
        _notifications.Clear();
        _evaluator.Evaluate($"[zemit({_zone.Id},Zone msg)][nszemit({_zone.Id},NS Zone msg)]", _ctx);
        Assert.Contains(_notifications, n => n.Scope == NotificationScope.Room && n.Target == _room.Id && n.Message == "Zone msg" && n.Spoofable);
        Assert.Contains(_notifications, n => n.Scope == NotificationScope.Room && n.Target == _room2.Id && n.Message == "NS Zone msg" && !n.Spoofable);

        _notifications.Clear();
        _evaluator.Evaluate($"[nscemit(Public,Chan msg)][nsemit(NS Emit)][nslemit(NS Lemit)][nsoemit({_alice.Id},NS Oemit)][nspemit({_alice.Id},NS Pemit)][nsprompt({_alice.Id},NS Prompt)][nsremit({_room.Id},NS Remit)]", _ctx);
        Assert.Contains(_notifications, n => n.Message.Contains("Chan msg") && !n.Spoofable);
        Assert.Contains(_notifications, n => n.Scope == NotificationScope.Room && n.Message == "NS Emit");
        Assert.Contains(_notifications, n => n.Scope == NotificationScope.Room && n.Message == "NS Lemit");
        Assert.Contains(_notifications, n => n.Target == _wiz.Id && n.Message == "NS Oemit");
        Assert.Contains(_notifications, n => n.Target == _alice.Id && n.Message == "NS Pemit");
        Assert.Contains(_notifications, n => n.Target == _alice.Id && n.Prompt && n.Message == "NS Prompt");
        Assert.Contains(_notifications, n => n.Message == "NS Remit");
    }

    [Fact]
    public void Section12_WhoAndConnectionFunctions_WorkCorrectly()
    {
        Assert.Equal("3", _evaluator.Evaluate("[nwho()]", _ctx));
        Assert.Equal("2", _evaluator.Evaluate("[nwho()]", _mortalCtx));

        string wizObjId = $"{_wiz.Id}:{_wiz.CreationTime.ToUnixTimeSeconds()}";
        string aliceObjId = $"{_alice.Id}:{_alice.CreationTime.ToUnixTimeSeconds()}";

        Assert.Equal($"{_wiz.Id} {_alice.Id} {_hiddenBob.Id}", _evaluator.Evaluate("[lwho()]", _ctx));
        Assert.Equal($"{_wiz.Id} {_alice.Id}", _evaluator.Evaluate("[lwho()]", _mortalCtx));
        Assert.Equal($"{wizObjId} {aliceObjId}", _evaluator.Evaluate("[lwhoid()]", _mortalCtx));

        Assert.Equal($"{_alice.Id}", _evaluator.Evaluate("[xwho(2,1)]", _mortalCtx));
        Assert.Equal($"{aliceObjId}", _evaluator.Evaluate("[xwhoid(2,1)]", _mortalCtx));

        Assert.Equal("2", _evaluator.Evaluate("[nmwho()]", _ctx));
        Assert.Equal($"{_wiz.Id} {_alice.Id}", _evaluator.Evaluate("[mwho()]", _ctx));
        Assert.Equal($"{wizObjId} {aliceObjId}", _evaluator.Evaluate("[mwhoid()]", _ctx));
        Assert.Equal($"{_alice.Id}", _evaluator.Evaluate("[xmwho(2,1)]", _ctx));
        Assert.Equal($"{aliceObjId}", _evaluator.Evaluate("[xmwhoid(2,1)]", _ctx));

        Assert.Equal($"{_wiz.Id} {_alice.Id} {_hiddenBob.Id}", _evaluator.Evaluate($"[zwho({_zone.Id})]", _ctx));
        Assert.Equal($"{_wiz.Id} {_alice.Id}", _evaluator.Evaluate($"[zmwho({_zone.Id})]", _ctx));

        Assert.True(int.Parse(_evaluator.Evaluate($"[conn({_alice.Id})]", _ctx)) >= 55);
        Assert.True(int.Parse(_evaluator.Evaluate($"[idle({_alice.Id})]", _ctx)) >= 10);
        Assert.True(int.Parse(_evaluator.Evaluate($"[idlesecs({_alice.Id})]", _ctx)) >= 10);

        Assert.Equal("alice.example.com", _evaluator.Evaluate($"[host({_alice.Id})]", _ctx));
        Assert.Equal("alice.example.com", _evaluator.Evaluate($"[hostname({_alice.Id})]", _ctx));
        Assert.Equal("10.0.0.2", _evaluator.Evaluate($"[ipaddr({_alice.Id})]", _ctx));
        Assert.Equal("mushclient", _evaluator.Evaluate($"[terminfo({_alice.Id})]", _ctx));
        Assert.Equal("11", _evaluator.Evaluate($"[ports({_alice.Id})]", _ctx));
        Assert.Equal("10 11 12", _evaluator.Evaluate("[lports()]", _ctx));
    }

    [Fact]
    public void Section14_AnsiAccentHtmlAndProtocolFunctions_WorkCorrectly()
    {
        string colored = _evaluator.Evaluate("[ansi(r,Red)]", _ctx);
        Assert.Equal("\x1B[31mRed\x1B[0m", colored);
        Assert.Equal("Red", _evaluator.Evaluate("[stripansi(ansi(r,Red))]", _ctx));
        Assert.Contains("red", _evaluator.Evaluate("[colors()]", _ctx));

        Assert.Equal("Álice", _evaluator.Evaluate("[accent(Alice,'    )]", _ctx));
        Assert.Equal("Alíce", _evaluator.Evaluate($"[accname({_alice.Id})]", _ctx));

        Assert.Equal("Red", _evaluator.Evaluate("[render(ansi(r,Red),ascii)]", _ctx));
        Assert.Equal("&lt;b&gt;", _evaluator.Evaluate("[render(<b>,html)]", _ctx));

        Assert.Equal("<b>hi</b>", _evaluator.Evaluate("[html(<b>hi</b>)]", _ctx));
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate("[html(<b>hi</b>)]", _mortalCtx));
        Assert.Equal("<a href=\"x\">", _evaluator.Evaluate("[tag(a,href=\"x\")]", _ctx));
        Assert.Equal("</a>", _evaluator.Evaluate("[endtag(a)]", _ctx));
        Assert.Equal("<b id=\"1\">hi</b>", _evaluator.Evaluate("[tagwrap(b,id=\"1\",hi)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[pueblo()]", _ctx));

        _notifications.Clear();
        Assert.Equal("1", _evaluator.Evaluate($"[oob({_alice.Id},GMCP.Char,\"hello\")]", _ctx));
        Assert.Contains(_notifications, n => n.Target == _alice.Id && n.ProtocolType == "GMCP.Char" && (string?)n.ProtocolPayload == "\"hello\"");
        Assert.Equal("<span>ok</span>", _evaluator.Evaluate($"[wshtml(<span>ok</span>,{_alice.Id})]", _ctx));
        Assert.Contains(_notifications, n => n.Target == _alice.Id && n.ProtocolType == "wshtml");
    }

    [Fact]
    public void Section19_LateSubsystemFunctions_WorkCorrectly()
    {
        Assert.Equal("43", _evaluator.Evaluate($"[quota({_alice.Id})]", _ctx));
        Assert.Equal("#1", _evaluator.Evaluate($"[followers({_alice.Id})]", _ctx));
        Assert.Equal("#1", _evaluator.Evaluate($"[following({_alice.Id})]", _ctx));

        Assert.Equal("custom_val", _evaluator.Evaluate("[config(custom_opt)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate($"[checkpass({_alice.Id},secret)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate($"[checkpass({_alice.Id},wrong)]", _ctx));
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate($"[checkpass({_alice.Id},secret)]", _mortalCtx));

        Assert.Equal("#-1 SQL IS NOT ENABLED", _evaluator.Evaluate("[sql(SELECT 1)]", _ctx));
        Assert.Equal("#-1 SQL IS NOT ENABLED", _evaluator.Evaluate("[mapsql(ME/FOO,SELECT 1)]", _ctx));

        Assert.Equal(string.Empty, _evaluator.Evaluate("[addrlog(127.0.0.1)]", _ctx));
        Assert.Equal(string.Empty, _evaluator.Evaluate("[connlog(all)]", _ctx));
        Assert.Equal(string.Empty, _evaluator.Evaluate($"[connrecord({_alice.Id})]", _ctx));
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate("[addrlog(127.0.0.1)]", _mortalCtx));

        Assert.Equal("\a\a", _evaluator.Evaluate("[beep(2)]", _ctx));
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate("[beep(1)]", _mortalCtx));

        Assert.StartsWith("Average:", _evaluator.Evaluate("[benchmark(add(1,2),5)]", _ctx));
        Assert.Equal("Tabs or spaces?", _evaluator.Evaluate("[poll()]", _ctx));
        Assert.Contains("LWHO", _evaluator.Evaluate("[suggest(functions,lwh)]", _ctx));

        Assert.Equal("WHO QUIT", _evaluator.Evaluate("[textentries(help,*)]", _ctx));
        Assert.Equal("Lists connected players.", _evaluator.Evaluate("[textfile(help,WHO)]", _ctx));
        Assert.Equal("WHO", _evaluator.Evaluate("[textsearch(help,connected)]", _ctx));

        Assert.Equal("3", _evaluator.Evaluate("[root(27,3)]", _ctx));
        Assert.Equal("-2", _evaluator.Evaluate("[root(-8,3)]", _ctx));
    }
}
