namespace PennMUSH.Core.Tests.MUSHcode;

using System.Collections.Generic;
using Xunit;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Communication.Mail;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Locks;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class Sections9_13_17_18FunctionTests
{
    private readonly ObjectDatabase _db;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly EvaluationContext _ctx;
    private readonly List<Notification> _notifications = new();
    private readonly MUSHObject _room;
    private readonly MUSHObject _room2;
    private readonly MUSHObject _wiz;
    private readonly MUSHObject _alice;
    private readonly MUSHObject _thing;

    public Sections9_13_17_18FunctionTests()
    {
        _db = new ObjectDatabase();
        _room = _db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);             // #0
        _wiz = _db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);          // #1
        _wiz.Owner = _wiz.Id;
        _wiz.AddFlag("WIZARD");
        _db.MoveObject(_wiz.Id, _room.Id);

        _room2 = _db.CreateObject("Annex", ObjectType.Room, _wiz.Id);                 // #2
        _alice = _db.CreateObject("Alice", ObjectType.Player, _wiz.Id);               // #3
        _alice.Owner = _alice.Id;
        _db.MoveObject(_alice.Id, _room.Id);

        _thing = _db.CreateObject("Widget", ObjectType.Thing, _wiz.Id);               // #4
        _thing.SetAttribute("CMD_TEST", "$hello *:@emit Hi %0", _wiz.Id);
        _thing.SetLock(LockType.Basic, new DbrefLockNode(_wiz.Id), _wiz.Id);
        _db.MoveObject(_thing.Id, _room.Id);

        var channels = new ChannelRegistry();
        channels.CreateChannel("Public", _wiz.Id, ChannelFlags.Public | ChannelFlags.Player, "General chat");
        var pub = channels.GetChannel("Public")!;
        pub.AddMember(_wiz.Id, "Admin");
        pub.AddMember(_alice.Id, "Guest");
        pub.SetLock("join", "#TRUE", new ConstantLockNode(true));
        channels.Broadcast("Public", _wiz.Id, "Wizard", "Welcome!");

        var mail = new MailStore();
        mail.SendMail(_alice.Id, _wiz.Id, "Greetings", "Hello Wizard!");
        var aliases = new MailAliasTable();
        var ma = new MailAlias { Name = "staff", Owner = _wiz.Id };
        ma.Members.Add(_wiz.Id);
        ma.Members.Add(_alice.Id);
        aliases.Aliases.ToString();
        // Add alias cleanly
        var addField = typeof(MailAliasTable).GetField("_aliases", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        ((List<MailAlias>)addField!.GetValue(aliases)!).Add(ma);

        var queue = new ActionQueue();
        queue.Enqueue(_wiz.Id, _wiz.Id, "@emit queued!", System.TimeSpan.FromSeconds(10), "sem1");

        _registry = new FunctionRegistry(_db)
        {
            NotificationDispatcher = n => _notifications.Add(n),
            Channels = channels,
            Mail = mail,
            MailAliases = aliases,
            Queue = queue
        };
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;
        _ctx = new EvaluationContext(_wiz.Id, _wiz.Id, _wiz.Id, _room.Id);
    }

    [Fact]
    public void Section9_SideEffectsAndSpeak_WorkCorrectly()
    {
        _notifications.Clear();
        _evaluator.Evaluate("[emit(Room msg)][pemit(#3,Private msg)][prompt(#3,Prompt>)][remit(#0,Remit msg)][oemit(#3,Others msg)][lemit(Outer msg)]", _ctx);
        Assert.Contains(_notifications, n => n.Scope == NotificationScope.Room && n.Message == "Room msg");
        Assert.Contains(_notifications, n => n.Scope == NotificationScope.Target && n.Target == _alice.Id && n.Message == "Private msg");
        Assert.Contains(_notifications, n => n.Prompt && n.Message == "Prompt>");
        Assert.Contains(_notifications, n => n.Target == _wiz.Id && n.Message == "Others msg");

        Assert.Equal(string.Empty, _evaluator.Evaluate($"[attrib_set({_thing.Id}/TEST_A,val1)]", _ctx));
        Assert.Equal("val1", _evaluator.Evaluate($"[get({_thing.Id}/TEST_A)]", _ctx));
        Assert.Equal(string.Empty, _evaluator.Evaluate($"[set({_thing.Id},TEST_B:val2)]", _ctx));
        Assert.Equal("val2", _evaluator.Evaluate($"[get({_thing.Id}/TEST_B)]", _ctx));
        Assert.Equal(string.Empty, _evaluator.Evaluate($"[wipe({_thing.Id}/TEST_*)]", _ctx));
        Assert.Equal(string.Empty, _evaluator.Evaluate($"[get({_thing.Id}/TEST_A)]", _ctx));
        Assert.Equal(string.Empty, _evaluator.Evaluate($"[lset({_thing.Id}/Basic,visual)]", _ctx));
        Assert.Contains("v", _evaluator.Evaluate($"[lockflags({_thing.Id}/Basic)]", _ctx));

        string created = _evaluator.Evaluate("[create(NewBox)]", _ctx);
        Assert.True(Dbref.TryParse(created, out var cRef) && cRef.IsValid);
        string cloned = _evaluator.Evaluate($"[clone({created},ClonedBox)]", _ctx);
        Assert.True(Dbref.TryParse(cloned, out var clRef) && clRef.IsValid);
        string dug = _evaluator.Evaluate("[dig(Vault,in,out)]", _ctx);
        Assert.True(Dbref.TryParse(dug, out var dRef) && dRef.IsValid);
        string opened = _evaluator.Evaluate($"[open(Door,{_room2.Id},{_room.Id})]", _ctx);
        Assert.True(Dbref.TryParse(opened, out var oRef) && oRef.IsValid);
        Assert.Equal(string.Empty, _evaluator.Evaluate($"[link({opened},{dug})]", _ctx));
        Assert.Equal(string.Empty, _evaluator.Evaluate($"[tel({created},{dug})]", _ctx));
        Assert.Equal(dug, _evaluator.Evaluate($"[loc({created})]", _ctx));

        Assert.Equal("Wizard says, \"Hello!\"", _evaluator.Evaluate("[speak(#1,Hello!)]", _ctx));
        Assert.Equal("Wizard waves.", _evaluator.Evaluate("[speak(#1,:waves.)]", _ctx));
    }

    [Fact]
    public void Section13_ChannelFunctions_WorkCorrectly()
    {
        Assert.Equal("50", _evaluator.Evaluate("[cbuffer(Public)]", _ctx));
        Assert.Equal(string.Empty, _evaluator.Evaluate("[cbufferadd(Public,Extra line)]", _ctx));
        Assert.Contains("Extra line", _evaluator.Evaluate("[crecall(Public)]", _ctx));
        Assert.Equal("General chat", _evaluator.Evaluate("[cdesc(Public)]", _ctx));
        Assert.Contains("O", _evaluator.Evaluate("[cflags(Public)]", _ctx));
        Assert.Contains("Open", _evaluator.Evaluate("[clflags(Public)]", _ctx));
        Assert.Equal("Public", _evaluator.Evaluate("[channels()]", _ctx));
        Assert.Equal("Public", _evaluator.Evaluate($"[channels({_alice.Id})]", _ctx));
        Assert.Equal("#TRUE", _evaluator.Evaluate("[clock(Public/join)]", _ctx));
        Assert.Equal("2", _evaluator.Evaluate("[cmsgs(Public)]", _ctx));
        Assert.Equal(_wiz.Id.ToString(), _evaluator.Evaluate("[cowner(Public)]", _ctx));
        Assert.Equal("On", _evaluator.Evaluate($"[cstatus(Public,{_wiz.Id})]", _ctx));
        Assert.Equal("Admin", _evaluator.Evaluate($"[ctitle(Public,{_wiz.Id})]", _ctx));
        Assert.Equal("2", _evaluator.Evaluate("[cusers(Public)]", _ctx));
        Assert.Contains(_alice.Id.ToString(), _evaluator.Evaluate("[cwho(Public)]", _ctx));
        Assert.Equal("#-1", _evaluator.Evaluate("[cmogrifier(Public)]", _ctx));
    }

    [Fact]
    public void Section17_MailFunctions_WorkCorrectly()
    {
        Assert.Equal("1", _evaluator.Evaluate("[mail()]", _ctx));
        Assert.Equal("1 1 0", _evaluator.Evaluate("[mail(me)]", _ctx));
        Assert.Equal("0:1", _evaluator.Evaluate("[maillist()]", _ctx));
        Assert.Equal("Hello Wizard!", _evaluator.Evaluate("[mail(1)]", _ctx));
        Assert.Equal(_alice.Id.ToString(), _evaluator.Evaluate("[mailfrom(1)]", _ctx));
        Assert.Equal("Greetings", _evaluator.Evaluate("[mailsubject(1)]", _ctx));
        Assert.False(string.IsNullOrWhiteSpace(_evaluator.Evaluate("[mailtime(1)]", _ctx)));
        Assert.Equal("N---", _evaluator.Evaluate("[mailstatus(1)]", _ctx));

        Assert.Equal(string.Empty, _evaluator.Evaluate($"[mailsend({_alice.Id},Hi/Body text)]", _ctx));
        Assert.Equal("-1", _evaluator.Evaluate($"[sent({_wiz.Id})]", _ctx));
        Assert.Equal("-1", _evaluator.Evaluate($"[recv({_wiz.Id})]", _ctx));
        Assert.Equal("1 1", _evaluator.Evaluate("[mailstats()]", _ctx));
        Assert.Equal("1 1 0 1 1 0", _evaluator.Evaluate("[maildstats()]", _ctx));
        Assert.Equal("1 1 0 11 1 1 0 22", _evaluator.Evaluate("[mailfstats()]", _ctx));
        Assert.Equal("1 1 0", _evaluator.Evaluate("[folderstats(0)]", _ctx));
        Assert.Equal("+staff", _evaluator.Evaluate("[malias()]", _ctx));
        Assert.Equal($"{_wiz.Id} {_alice.Id}", _evaluator.Evaluate("[malias(+staff)]", _ctx));
    }

    [Fact]
    public void Section18_QueueAndScanFunctions_WorkCorrectly()
    {
        Assert.Equal("1", _evaluator.Evaluate("[getpids()]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[lpids(#1)]", _ctx));
        Assert.Equal("#1", _evaluator.Evaluate("[pidinfo(1,executor)]", _ctx));
        Assert.Equal("@emit queued!", _evaluator.Evaluate("[pidinfo(1,command)]", _ctx));
        Assert.Equal($"{_thing.Id}/CMD_TEST", _evaluator.Evaluate("[scan(#1,hello world)]", _ctx));
    }
}
