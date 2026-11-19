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
using PennMUSH.Core.Types;

public class Pr6LateSubsystemsTests
{
    private readonly ObjectDatabase _db;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly EvaluationContext _wizCtx;
    private readonly EvaluationContext _aliceCtx;
    private readonly List<Notification> _notifications = new();
    private readonly MUSHObject _wiz;
    private readonly MUSHObject _alice;

    public Pr6LateSubsystemsTests()
    {
        _db = new ObjectDatabase();
        var room = _db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);         // #0
        _wiz = _db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);          // #1
        _wiz.Owner = _wiz.Id;
        _wiz.AddFlag("WIZARD");
        _db.MoveObject(_wiz.Id, room.Id);

        _alice = _db.CreateObject("Alice", ObjectType.Player, _wiz.Id);               // #2
        _alice.Owner = _alice.Id;
        _alice.SetAttribute("RQUOTA", "10", _wiz.Id);
        _db.MoveObject(_alice.Id, room.Id);

        var channels = new ChannelRegistry();
        channels.CreateChannel("Public", _wiz.Id, ChannelFlags.Public | ChannelFlags.Player, "General");
        var pub = channels.GetChannel("Public")!;
        pub.AddMember(_wiz.Id, "Admin");
        pub.AddMember(_alice.Id, "Member");
        pub.GetMember(_alice.Id)!.IsMuted = true;
        pub.AddMessage("Wizard", _wiz.Id, "First");
        pub.AddMessage("Wizard", _wiz.Id, "Second");

        var mail = new MailStore();
        mail.SendMail(_alice.Id, _wiz.Id, "Subj1", "Body1");
        mail.SendMail(_alice.Id, _wiz.Id, "Subj2", "Body2");
        mail.MarkRead(_wiz.Id, 1);

        var textEntries = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["help"] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["WHO"] = "Lists connected players.",
                ["QUIT"] = "Disconnects."
            }
        };

        _registry = new FunctionRegistry(_db)
        {
            NotificationDispatcher = n => _notifications.Add(n),
            Channels = channels,
            Mail = mail,
            TextEntriesProvider = f => textEntries.TryGetValue(f, out var d) ? d.Keys : Array.Empty<string>(),
            TextFileEntryProvider = (f, t) => textEntries.TryGetValue(f, out var d) && d.TryGetValue(t, out var b) ? b : null
        };
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;

        _wizCtx = new EvaluationContext(_wiz.Id, _wiz.Id, _wiz.Id, room.Id);
        _aliceCtx = new EvaluationContext(_alice.Id, _alice.Id, _alice.Id, room.Id);
    }

    [Fact]
    public void ChannelFunctions_MatchOriginalSemantics()
    {
        Assert.Equal("First|Second", _evaluator.Evaluate("[crecall(Public,2,1,|)]", _wizCtx));
        Assert.Equal("First Second", _evaluator.Evaluate("[crecall(Public,0)]", _wizCtx));
        Assert.Equal("#-1 ARGUMENT MUST BE A POSITIVE INTEGER", _evaluator.Evaluate("[crecall(Public,-1)]", _wizCtx));

        Assert.Equal("G", _evaluator.Evaluate($"[cflags(Public,{_alice.Id})]", _wizCtx));
        Assert.Equal("Gag", _evaluator.Evaluate($"[clflags(Public,{_alice.Id})]", _wizCtx));
        Assert.Equal("#-1 NOT ON CHANNEL", _evaluator.Evaluate("cflags(Public,#0)", _wizCtx));

        Assert.Equal("#-1 NO SUCH LOCK TYPE", _evaluator.Evaluate("[clock(Public/bogus)]", _wizCtx));
        Assert.Equal(string.Empty, _evaluator.Evaluate($"[clock(Public/join,{_wiz.Id})]", _wizCtx));
        Assert.Equal(_wiz.Id.ToString(), _evaluator.Evaluate("[clock(Public/join)]", _wizCtx));

        Assert.Equal("#-1 NO CHANNEL GIVEN", _evaluator.Evaluate("[cusers()]", _wizCtx));
        Assert.Equal(_wiz.Id.ToString(), _evaluator.Evaluate("[cwho(Public,on,1)]", _wizCtx));
        Assert.Equal("#-1 INVALID ARGUMENT", _evaluator.Evaluate("[cwho(Public,bogus)]", _wizCtx));
    }

    [Fact]
    public void MailFunctions_MatchOriginalSemantics()
    {
        Assert.Equal("2", _evaluator.Evaluate("[mail()]", _wizCtx));
        Assert.Equal("2 1 0", _evaluator.Evaluate("[mail(me)]", _wizCtx));

        Assert.Equal("0:2", _evaluator.Evaluate("[maillist(me,unread)]", _wizCtx));
        Assert.Equal("0:1", _evaluator.Evaluate("[maillist(me,read)]", _wizCtx));

        Assert.Equal("0 2", _evaluator.Evaluate($"[mailstats({_wiz.Id})]", _wizCtx));
        Assert.Equal("2 0", _evaluator.Evaluate($"[mailstats({_alice.Id})]", _aliceCtx));
        Assert.Equal("2 1 0 0 0 0", _evaluator.Evaluate($"[maildstats({_alice.Id})]", _aliceCtx));
        Assert.Equal("2 1 0", _evaluator.Evaluate($"[folderstats({_wiz.Id},0)]", _wizCtx));
    }

    [Fact]
    public void LateSubsystemFunctions_MatchOriginalSemantics()
    {
        Assert.Equal("99999", _evaluator.Evaluate($"[quota({_wiz.Id})]", _wizCtx));
        Assert.Equal("11", _evaluator.Evaluate($"[quota({_alice.Id})]", _wizCtx));

        Assert.Equal("#-1 ARGUMENT MUST BE AN INTEGER", _evaluator.Evaluate("[beep(abc)]", _wizCtx));
        Assert.Equal("#-1 OUT OF RANGE", _evaluator.Evaluate("[beep(9)]", _wizCtx));

        _notifications.Clear();
        Assert.Equal("3", _evaluator.Evaluate($"[benchmark(add(1,2),5,{_wiz.Id})]", _wizCtx));
        Assert.Contains(_notifications, n => n.Target == _wiz.Id && n.Message.Contains("Average:"));

        Assert.Equal("LWHO|CWHO", _evaluator.Evaluate("[suggest(functions,lwh,|,2)]", _wizCtx));
        Assert.Equal("WHO|QUIT", _evaluator.Evaluate("[textentries(help,*,|)]", _wizCtx));
        Assert.Equal("WHO", _evaluator.Evaluate("[textsearch(help,*connected*)]", _wizCtx));

        Assert.Equal("#-1 NO SUCH PLAYER", _evaluator.Evaluate("[checkpass(#0,pw)]", _wizCtx));
        Assert.Equal("#-1 IMAGINARY NUMBER", _evaluator.Evaluate("[root(-16,2)]", _wizCtx));
        Assert.Equal("#-1 ROOT OUT OF RANGE", _evaluator.Evaluate("[root(16,-2)]", _wizCtx));
    }
}
