namespace PennMUSH.Core.Tests.MUSHcode;

using System;
using System.Collections.Generic;
using Xunit;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Communication.Mail;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Locks;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.MUSHcode.Functions;
using PennMUSH.Core.Types;

public class Pr3PermissionsAndGatingTests
{
    private readonly ObjectDatabase _db;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly EvaluationContext _wizCtx;
    private readonly EvaluationContext _aliceCtx;
    private readonly EvaluationContext _royalCtx;
    private readonly MUSHObject _room1;
    private readonly MUSHObject _room2;
    private readonly MUSHObject _zone;
    private readonly MUSHObject _wiz;
    private readonly MUSHObject _alice;
    private readonly MUSHObject _bob;
    private readonly MUSHObject _royal;
    private readonly MUSHObject _wizBox;

    public Pr3PermissionsAndGatingTests()
    {
        _db = new ObjectDatabase();
        _room1 = _db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);            // #0
        _wiz = _db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);          // #1
        _wiz.Owner = _wiz.Id;
        _wiz.AddFlag("WIZARD");
        _db.MoveObject(_wiz.Id, _room1.Id);

        _room2 = _db.CreateObject("Vault", ObjectType.Room, _wiz.Id);                 // #2
        _zone = _db.CreateObject("ZoneObj", ObjectType.Thing, _wiz.Id);               // #3
        _zone.SetLock(LockType.Zone, new DbrefLockNode(_wiz.Id), _wiz.Id);
        _room2.Zone = _zone.Id;

        _alice = _db.CreateObject("Alice", ObjectType.Player, _wiz.Id);               // #4
        _alice.Owner = _alice.Id;
        _db.MoveObject(_alice.Id, _room1.Id);

        _bob = _db.CreateObject("Bob", ObjectType.Player, _wiz.Id);                   // #5
        _bob.Owner = _bob.Id;
        _bob.AddFlag("UNFINDABLE");
        _bob.AddFlag("DARK");
        _db.MoveObject(_bob.Id, _room2.Id);

        _royal = _db.CreateObject("Royalty", ObjectType.Player, _wiz.Id);             // #6
        _royal.Owner = _royal.Id;
        _royal.AddFlag("ROYALTY");
        _db.MoveObject(_royal.Id, _room1.Id);

        _wizBox = _db.CreateObject("WizBox", ObjectType.Thing, _wiz.Id);              // #7
        _wizBox.SetAttribute("SECRET", "topsecret", _wiz.Id, AttributeFlags.MDark);
        _wizBox.SetAttribute("VIS_EVAL", "[name(me)]", _wiz.Id, AttributeFlags.Visual);
        _wizBox.SetAttribute("FOLLOWERS", "#1", _wiz.Id);
        _db.MoveObject(_wizBox.Id, _room2.Id);

        var channels = new ChannelRegistry();
        channels.CreateChannel("Public", _wiz.Id, ChannelFlags.Public | ChannelFlags.Player, "General");
        var pub = channels.GetChannel("Public")!;
        pub.AddMember(_wiz.Id, "Admin");
        pub.AddMember(_alice.Id, "Member");
        pub.GetMember(_alice.Id)!.IsMuted = true;

        var mail = new MailStore();
        mail.SendMail(_wiz.Id, _bob.Id, "Subj", "Secret mail");

        var now = DateTimeOffset.UtcNow;
        var snaps = new List<ConnectionSnapshot>
        {
            new(_alice.Id, 10, "alice.host", "10.0.0.1", "xterm", now.AddSeconds(-30), now.AddSeconds(-2), IsHidden: false),
            new(_bob.Id, 11, "bob.host", "10.0.0.2", "mushclient", now.AddSeconds(-30), now.AddSeconds(-2), IsHidden: true)
        };

        _registry = new FunctionRegistry(_db)
        {
            NotificationDispatcher = _ => { },
            Channels = channels,
            Mail = mail,
            ConnectionsProvider = () => snaps
        };
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;

        _wizCtx = new EvaluationContext(_wiz.Id, _wiz.Id, _wiz.Id, _room1.Id);
        _aliceCtx = new EvaluationContext(_alice.Id, _alice.Id, _alice.Id, _room1.Id);
        _royalCtx = new EvaluationContext(_royal.Id, _royal.Id, _royal.Id, _room1.Id);
    }

    [Fact]
    public void SideEffectGating_BlocksWhenDisabledOrNoSideFX()
    {
        _registry.SideEffectsEnabled = false;
        try
        {
            Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate($"[set({_alice.Id},TEST:1)]", _wizCtx));
            Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate("[create(Widget)]", _wizCtx));
            Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate("[cemit(Public,Hi)]", _wizCtx));
            Assert.Equal("3", _evaluator.Evaluate("[add(1,2)]", _wizCtx));
        }
        finally
        {
            _registry.SideEffectsEnabled = true;
        }

        var noSideCtx = _wizCtx with { NoSideFX = true };
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate($"[tel({_alice.Id},{_room2.Id})]", noSideCtx));
    }

    [Fact]
    public void DbAndSideEffectFunctions_EnforcePermissions()
    {
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate($"[tel({_bob.Id},{_room1.Id})]", _aliceCtx));
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate($"[clone({_wizBox.Id})]", _aliceCtx));

        Assert.Equal("#-1", _evaluator.Evaluate($"[loc({_bob.Id})]", _aliceCtx));
        Assert.Equal(_room2.Id.ToString(), _evaluator.Evaluate($"[loc({_bob.Id})]", _wizCtx));

        Assert.Equal("#-1", _evaluator.Evaluate($"[lcon({_room2.Id})]", _aliceCtx));
        Assert.Equal("#-1", _evaluator.Evaluate($"[hidden({_alice.Id})]", _aliceCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[hidden({_bob.Id})]", _wizCtx));

        Assert.Equal(string.Empty, _evaluator.Evaluate($"[eval({_wizBox.Id},SECRET)]", _aliceCtx));
        Assert.Equal("WizBox", _evaluator.Evaluate($"[get_eval({_wizBox.Id}/VIS_EVAL)]", _aliceCtx));
    }

    [Fact]
    public void ConnectionChannelMailAndLateFunctions_EnforcePermissions()
    {
        Assert.Equal("#-1", _evaluator.Evaluate($"[host({_bob.Id})]", _aliceCtx));
        Assert.Equal("#-1", _evaluator.Evaluate($"[ipaddr({_bob.Id})]", _aliceCtx));
        Assert.Equal("#-1", _evaluator.Evaluate($"[terminfo({_bob.Id})]", _aliceCtx));
        Assert.Equal("-1", _evaluator.Evaluate($"[idle({_bob.Id})]", _aliceCtx));
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate($"[zwho({_zone.Id})]", _aliceCtx));

        Assert.Equal("Gag", _evaluator.Evaluate($"[cstatus(Public,{_alice.Id})]", _wizCtx));

        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate($"[mail({_bob.Id})]", _aliceCtx));
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate($"[followers({_wizBox.Id})]", _aliceCtx));
        Assert.Equal("\a", _evaluator.Evaluate("[beep(1)]", _royalCtx));
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate($"[oob({_bob.Id},GMCP,{{}})]", _aliceCtx));
    }
}
