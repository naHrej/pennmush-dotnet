namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;
using Xunit;

public class ActionListCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly ActionQueue _queue = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _ada;
    private readonly Dbref _bea;
    private readonly Dbref _widget;

    public ActionListCommandTests()
    {
        var room = _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        ada.Owner = ada.Id;
        _ada = ada.Id;
        var bea = _db.CreateObject("Bea", ObjectType.Player, new Dbref(1));
        bea.Owner = bea.Id;
        _bea = bea.Id;
        var widget = _db.CreateObject("Widget", ObjectType.Thing, ada.Id);
        widget.SetAttribute("SUCCESS", "You ring it.", ada.Id);
        widget.SetAttribute("OS", "rings it.", ada.Id);
        widget.SetAttribute("ASUCCESS", $"@pemit #{ada.Id.Value}=acted %0", ada.Id);
        _widget = widget.Id;
        _db.MoveObject(ada.Id, room.Id);
        _db.MoveObject(bea.Id, room.Id);
        _db.MoveObject(widget.Id, room.Id);
        var evaluator = new ExpressionEvaluator(database: _db);
        _dispatcher = new CommandDispatcher(evaluator);
        _dispatcher.Register(new IncludeCommandHandler(_dispatcher, evaluator), "@include");
        _dispatcher.Register(new ActionListCommandHandler(_dispatcher, "@assert", evaluator, _queue), "@assert");
        _dispatcher.Register(new ActionListCommandHandler(_dispatcher, "@break", evaluator, _queue), "@break");
        _dispatcher.Register(new ActionListCommandHandler(_dispatcher, "@retry", evaluator, _queue), "@retry");
        _dispatcher.Register(new VerbCommandHandler(_dispatcher, evaluator), "@verb");
        _dispatcher.Register(new PemitCommandHandler(), "@pemit");
        _dispatcher.AddRestriction("@retry", "nogagged", null);
    }

    [Fact]
    public void AssertAndBreakStopTheRestOfTheList()
    {
        Set("STOP", "@pemit me=one;@assert 0;@pemit me=two");
        Assert.Equal(["one"], Heard("@include Ada/STOP"));

        Set("GO", "@pemit me=one;@assert 1;@pemit me=two");
        Assert.Equal(["one", "two"], Heard("@include Ada/GO"));

        Set("BREAK", "@pemit me=one;@break 1;@pemit me=two");
        Assert.Equal(["one"], Heard("@include Ada/BREAK"));

        Set("PASS", "@pemit me=one;@break 0;@pemit me=two");
        Assert.Equal(["one", "two"], Heard("@include Ada/PASS"));

        Set("FAIL", "@assert 0=@pemit me=failed;@pemit me=after");
        Assert.Equal(["failed"], Heard("@include Ada/FAIL"));
    }

    [Fact]
    public void PennBooleansMatchTheOriginalRules()
    {
        Assert.False(MushBoolean.IsTrue(""));
        Assert.False(MushBoolean.IsTrue("   "));
        Assert.False(MushBoolean.IsTrue("0"));
        Assert.False(MushBoolean.IsTrue("  0"));
        Assert.False(MushBoolean.IsTrue("#-1"));
        Assert.True(MushBoolean.IsTrue("1"));
        Assert.True(MushBoolean.IsTrue("hello"));
        Assert.True(MushBoolean.IsTrue("#1"));
        Assert.True(MushBoolean.IsTrue("0 "));

        Set("ERR", "@assert #-1;@pemit me=after");
        Assert.Empty(Heard("@include Ada/ERR"));
        Set("WORD", "@assert hello;@pemit me=after");
        Assert.Equal(["after"], Heard("@include Ada/WORD"));
    }

    [Fact]
    public void RetryRerunsTheListWithNewArguments()
    {
        Set("AGAIN", "@pemit me=%0;@assert %0;@retry 1=0");
        Assert.Equal(["1", "0"], Heard("@include Ada/AGAIN=1"));
    }

    [Fact]
    public void NoBreakKeepsTheCallerGoing()
    {
        Set("INNER", "@pemit me=inside;@break 1;@pemit me=hidden");
        Set("OUTER", "@include Ada/INNER;@pemit me=after");
        Assert.Equal(["inside"], Heard("@include Ada/OUTER"));

        Set("SAFE", "@include/nobreak Ada/INNER;@pemit me=after");
        Assert.Equal(["inside", "after"], Heard("@include Ada/SAFE"));
    }

    [Fact]
    public void QueuedAssertWaitsAndAGaggedRetryIsRefused()
    {
        Set("LATER", "@assert/queued 0=@pemit me=later;@pemit me=after");
        Assert.Empty(Heard("@include Ada/LATER"));
        Assert.Equal(["@pemit me=later"], _queue.Snapshot().Select(entry => entry.Command));

        _db.GetObject(_ada)!.AddFlag("GAGGED");
        Set("GAG", "@retry 1");
        var gagged = _dispatcher.Dispatch(_ada, "@include Ada/GAG", _db);
        Assert.Contains("You cannot do that while gagged.", gagged.Notifications);
    }

    [Fact]
    public void VerbSendsMessagesAndRunsTheAction()
    {
        var result = _dispatcher.Dispatch(_ada, "@verb Widget=Ada,SUCCESS,nope,OS,does nothing,ASUCCESS,clang", _db);
        Assert.Contains("You ring it.", result.Notifications);
        Assert.Contains(result.TargetedNotifications, note => note.Target == _ada && note.Message == "acted clang");
        Assert.Contains(result.TargetedNotifications, note => note.Target == _bea && note.Message == "rings it.");
        Assert.DoesNotContain(result.Notifications, note => note.Contains("nope") || note.Contains("does nothing"));
    }

    [Fact]
    public void VerbRefusesAMissingObjectOrSomeoneElsesWidget()
    {
        var missing = _dispatcher.Dispatch(_ada, "@verb Nobody=Ada,SUCCESS", _db);
        Assert.Contains("What was the victim of the verb?", missing.Notifications);

        var noActor = _dispatcher.Dispatch(_ada, "@verb Widget", _db);
        Assert.Contains("What do you want to do with the verb?", noActor.Notifications);

        var unknown = _dispatcher.Dispatch(_ada, "@verb Widget=Nobody,SUCCESS", _db);
        Assert.Contains("What do you want to do the verb?", unknown.Notifications);

        var denied = _dispatcher.Dispatch(_bea, "@verb Widget=Ada,SUCCESS", _db);
        Assert.Contains("Permission denied.", denied.Notifications);
    }

    private void Set(string attribute, string value)
        => _db.GetObject(_ada)!.SetAttribute(attribute, value, _ada);

    private string[] Heard(string command)
    {
        var result = _dispatcher.Dispatch(_ada, command, _db);
        return result.TargetedNotifications
            .Where(note => note.Scope == NotificationScope.Target && note.Target == _ada)
            .Select(note => note.Message)
            .ToArray();
    }
}
