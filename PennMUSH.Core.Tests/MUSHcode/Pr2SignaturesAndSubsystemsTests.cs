namespace PennMUSH.Core.Tests.MUSHcode;

using System;
using System.Collections.Generic;
using Xunit;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Communication.Mail;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.MUSHcode.Functions;
using PennMUSH.Core.Types;

public class Pr2SignaturesAndSubsystemsTests
{
    private readonly ObjectDatabase _db;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly EvaluationContext _wizCtx;
    private readonly EvaluationContext _mortalCtx;
    private readonly List<Notification> _notifications = new();
    private readonly MUSHObject _room1;
    private readonly MUSHObject _room2;
    private readonly MUSHObject _wiz;
    private readonly MUSHObject _alice;
    private readonly MUSHObject _bob;
    private readonly MUSHObject _bag;
    private readonly MUSHObject _gem;
    private readonly MUSHObject _exit;

    public Pr2SignaturesAndSubsystemsTests()
    {
        _db = new ObjectDatabase();
        _room1 = _db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);            // #0
        _wiz = _db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);          // #1
        _wiz.Owner = _wiz.Id;
        _wiz.AddFlag("WIZARD");
        _wiz.SetAttribute("SEX", "male", _wiz.Id);
        _db.MoveObject(_wiz.Id, _room1.Id);

        _room2 = _db.CreateObject("Vault", ObjectType.Room, _wiz.Id);                 // #2
        _alice = _db.CreateObject("Alice", ObjectType.Player, _wiz.Id);               // #3
        _alice.Owner = _alice.Id;
        _alice.SetAttribute("SEX", "female", _wiz.Id);
        _db.MoveObject(_alice.Id, _room2.Id);

        _bob = _db.CreateObject("Bob", ObjectType.Player, _wiz.Id);                   // #4
        _bob.Owner = _bob.Id;
        _db.MoveObject(_bob.Id, _room2.Id);

        _bag = _db.CreateObject("Bag", ObjectType.Thing, _wiz.Id);                    // #5
        _bag.Home = _room1.Id;
        _db.MoveObject(_bag.Id, _wiz.Id);

        _gem = _db.CreateObject("Gem", ObjectType.Thing, _wiz.Id);                    // #6
        _gem.Home = _room2.Id;
        _db.MoveObject(_gem.Id, _bag.Id);

        _exit = _db.CreateObject("North;n", ObjectType.Exit, _wiz.Id);                // #7
        _db.AttachExit(_room1.Id, _exit.Id);
        _db.LinkExit(_exit.Id, _room2.Id);

        _wiz.SetAttribute("MUNGE_SORT", "[revwords(%0,%1)]", _wiz.Id);
        _wiz.SetAttribute("STEP_PAIR", "%0-%1", _wiz.Id);

        var channels = new ChannelRegistry();
        channels.CreateChannel("Public", _wiz.Id, ChannelFlags.Public | ChannelFlags.Player, "General");
        var pub = channels.GetChannel("Public")!;
        pub.BufferCapacity = 42;
        pub.AddMember(_wiz.Id, "Admin");
        pub.AddMember(_alice.Id, "Member");

        var mail = new MailStore();
        mail.SendMail(_alice.Id, _wiz.Id, "Subj1", "Body12345");

        var now = DateTimeOffset.UtcNow;
        var snaps = new List<ConnectionSnapshot>
        {
            new(_wiz.Id, 10, "localhost", "127.0.0.1", "xterm", now.AddSeconds(-60), now.AddSeconds(-1), IsHidden: false, BytesSent: 111, BytesReceived: 222, CommandCount: 7),
            new(_alice.Id, 11, "alice.host", "10.0.0.2", "mushclient", now.AddSeconds(-30), now.AddSeconds(-2), IsHidden: false, BytesSent: 333, BytesReceived: 444, CommandCount: 3),
            new(_bob.Id, 12, "bob.host", "10.0.0.3", "tintin", now.AddSeconds(-10), now.AddSeconds(-3), IsHidden: true, BytesSent: 555, BytesReceived: 666, CommandCount: 9)
        };

        _registry = new FunctionRegistry(_db)
        {
            NotificationDispatcher = n => _notifications.Add(n),
            Channels = channels,
            Mail = mail,
            ConnectionsProvider = () => snaps
        };
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;

        _wizCtx = new EvaluationContext(_wiz.Id, _wiz.Id, _wiz.Id, _room1.Id);
        _mortalCtx = new EvaluationContext(_alice.Id, _alice.Id, _alice.Id, _room2.Id);
    }

    [Fact]
    public void SentRecvPlayerCmdsLportsAndMwho_MatchOriginalSemantics()
    {
        Assert.Equal("111", _evaluator.Evaluate("[sent(10)]", _wizCtx));
        Assert.Equal("444", _evaluator.Evaluate($"[recv({_alice.Id})]", _wizCtx));
        Assert.Equal("-1", _evaluator.Evaluate($"[sent({_wiz.Id})]", _mortalCtx));

        Assert.Equal(_alice.Id.ToString(), _evaluator.Evaluate("[player(11)]", _wizCtx));
        Assert.Equal("7", _evaluator.Evaluate("[cmds(10)]", _wizCtx));

        Assert.Equal("10 11 12", _evaluator.Evaluate("[lports()]", _wizCtx));
        Assert.Equal("10 11", _evaluator.Evaluate("[lports()]", _mortalCtx));

        // MWHO is Mortal WHO (all non-hidden players across all rooms, never hidden even for wizards)
        Assert.Equal($"{_wiz.Id} {_alice.Id}", _evaluator.Evaluate("[mwho()]", _wizCtx));
        Assert.Equal("2", _evaluator.Evaluate("[nmwho()]", _wizCtx));
        Assert.Equal(_alice.Id.ToString(), _evaluator.Evaluate("[xmwho(2,1)]", _wizCtx));
    }

    [Fact]
    public void CemitCbufferAndMailfstats_MatchOriginalSemantics()
    {
        Assert.Equal("42", _evaluator.Evaluate("[cbuffer(Public)]", _wizCtx));

        _notifications.Clear();
        Assert.Equal(string.Empty, _evaluator.Evaluate("[cemit(Public,Hello channel)]", _wizCtx));
        Assert.Contains(_notifications, n => n.Message.Contains("Hello channel"));

        // sent(0 0 0 0) + rec(1 1 0 14) for Wizard
        Assert.Equal("0 0 0 0 1 1 0 14", _evaluator.Evaluate($"[mailfstats({_wiz.Id})]", _wizCtx));
        // sent(1 1 0 14) + rec(0 0 0 0) for Alice
        Assert.Equal("1 1 0 14 0 0 0 0", _evaluator.Evaluate($"[mailfstats({_alice.Id})]", _wizCtx));
    }

    [Fact]
    public void WhereRoomEntrancesAndTestlock_MatchOriginalSemantics()
    {
        // Gem is inside Bag (which is carried by Wizard in Hall #0)
        Assert.Equal(_bag.Id.ToString(), _evaluator.Evaluate($"[where({_gem.Id})]", _wizCtx));
        Assert.Equal(_room1.Id.ToString(), _evaluator.Evaluate($"[room({_gem.Id})]", _wizCtx));
        // Exit's where() is its source room (#0)
        Assert.Equal(_room1.Id.ToString(), _evaluator.Evaluate($"[where({_exit.Id})]", _wizCtx));

        // Entrances finds linked objects (Exit -> _room2, Thing.Home -> _room2), NOT contents (_alice/_bob)
        string ent = _evaluator.Evaluate($"[entrances({_room2.Id},ET)]", _wizCtx);
        Assert.Equal($"{_gem.Id} {_exit.Id}", ent);

        Assert.Equal("1", _evaluator.Evaluate($"[testlock({_wiz.Id}|{_alice.Id},{_alice.Id})]", _wizCtx));
        Assert.Equal("0", _evaluator.Evaluate($"[testlock({_wiz.Id},{_alice.Id})]", _wizCtx));
    }

    [Fact]
    public void ListStringAndTagwrapSignatures_MatchOriginalSemantics()
    {
        Assert.Equal("a-b c-d e-", _evaluator.Evaluate("[step(me/STEP_PAIR,a b c d e,2)]", _wizCtx));
        Assert.Equal("three two one", _evaluator.Evaluate("[munge(me/MUNGE_SORT,1 2 3,one two three)]", _wizCtx));
        Assert.Equal("a X c Z", _evaluator.Evaluate("[splice(a b c b,W X Y Z,b)]", _wizCtx));
        Assert.Equal("#-1 NUMBER OF WORDS MUST BE EQUAL", _evaluator.Evaluate("[splice(a b,X,b)]", _wizCtx));

        Assert.Equal(string.Empty, _evaluator.Evaluate("[lnum(0)]", _wizCtx));
        Assert.Equal("#-1 OUT OF RANGE", _evaluator.Evaluate("[lnum(-1)]", _wizCtx));
        Assert.Equal("10|12|14", _evaluator.Evaluate("[lnum(10,14,|,2)]", _wizCtx));
        Assert.Equal("0 0.5 1", _evaluator.Evaluate("[lnum(0,1,%b,0.5)]", _wizCtx));

        // unique removes adjacent duplicates, case-sensitive by default
        Assert.Equal("a A b a", _evaluator.Evaluate("[unique(a a A b b a)]", _wizCtx));
        Assert.Equal("a b", _evaluator.Evaluate("[unique(a A b,i)]", _wizCtx));

        Assert.Equal("<div class=\"x\">hello</div>", _evaluator.Evaluate("[tagwrap(div,class=\"x\",hello)]", _wizCtx));
        Assert.Equal("a", _evaluator.Evaluate("[art(cat)]", _wizCtx));
        Assert.Equal("an", _evaluator.Evaluate("[art(owl)]", _wizCtx));
        Assert.Equal("he", _evaluator.Evaluate($"[subj({_wiz.Id})]", _wizCtx));
        Assert.Equal("her", _evaluator.Evaluate($"[poss({_alice.Id})]", _wizCtx));
        Assert.Equal("her", _evaluator.Evaluate($"[obj({_alice.Id})]", _wizCtx));
        Assert.Equal("hers", _evaluator.Evaluate($"[aposs({_alice.Id})]", _wizCtx));
        Assert.Equal("#-1 NO MATCH", _evaluator.Evaluate("[subj(nobody)]", _wizCtx));
    }
}
