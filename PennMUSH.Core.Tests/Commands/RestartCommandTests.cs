namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;
using Xunit;

public class RestartCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly ActionQueue _queue = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _god;
    private readonly Dbref _ada;
    private readonly Dbref _lamp;

    public RestartCommandTests()
    {
        var room = _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.Owner = god.Id;
        god.AddFlag("WIZARD");
        _god = god.Id;
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        ada.Owner = ada.Id;
        _ada = ada.Id;
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, ada.Id);
        lamp.SetAttribute("STARTUP", "@emit awake;@emit two", ada.Id);
        _lamp = lamp.Id;
        _db.MoveObject(god.Id, room.Id);
        _db.MoveObject(ada.Id, room.Id);
        _db.MoveObject(lamp.Id, room.Id);
        var evaluator = new ExpressionEvaluator(database: _db);
        _dispatcher = new CommandDispatcher(evaluator);
        _dispatcher.Register(new RestartCommandHandler(_queue), "@restart");
        _dispatcher.AddRestriction("@restart", "nogagged", null);
    }

    [Fact]
    public void ABareRestartHaltsYouAndQueuesStartup()
    {
        _db.GetObject(_ada)!.SetAttribute("STARTUP", "@emit ready", _ada);
        _queue.Enqueue(_lamp, _ada, "@emit old");

        var result = _dispatcher.Dispatch(_ada, "@restart", _db);

        Assert.Equal(["Halted: Ada(#2)"], result.Notifications);
        Assert.DoesNotContain(_queue.Snapshot(), entry => entry.Command == "@emit old");
        Assert.Equal(["@emit ready", "@emit awake", "@emit two"], _queue.Snapshot().Select(entry => entry.Command));
    }

    [Fact]
    public void NamingYourselfAnnouncesTheRestart()
    {
        var result = _dispatcher.Dispatch(_ada, "@restart me", _db);
        Assert.Equal(
            ["All of your objects are being restarted.", "Halted: Ada(#2)"],
            result.Notifications);
        Assert.Equal(["@emit awake", "@emit two"], _queue.Snapshot().Select(entry => entry.Command));
    }

    [Fact]
    public void RestartingOneObjectLeavesTheOthersAlone()
    {
        _db.GetObject(_ada)!.SetAttribute("STARTUP", "@emit ready", _ada);
        var result = _dispatcher.Dispatch(_ada, "@restart Lamp", _db);
        Assert.Equal(
            ["Restarting: Lamp(#3)", "Halted: Lamp(#3)"],
            result.Notifications);
        Assert.Equal(["@emit awake", "@emit two"], _queue.Snapshot().Select(entry => entry.Command));
        Assert.All(_queue.Snapshot(), entry => Assert.Equal(_lamp, entry.Executor));
    }

    [Fact]
    public void AHaltedObjectAndAParentStartupAreSkipped()
    {
        var parent = _db.CreateObject("Parent", ObjectType.Thing, _ada);
        parent.SetAttribute("STARTUP", "@emit parent", _ada);
        var child = _db.CreateObject("Child", ObjectType.Thing, _ada);
        child.Parent = parent.Id;
        var stopped = _db.CreateObject("Stopped", ObjectType.Thing, _ada);
        stopped.AddFlag("HALT");
        stopped.SetAttribute("STARTUP", "@emit stopped", _ada);

        _dispatcher.Dispatch(_ada, $"@restart #{child.Id.Value}", _db);
        _dispatcher.Dispatch(_ada, $"@restart #{stopped.Id.Value}", _db);

        Assert.Empty(_queue.Snapshot());
    }

    [Fact]
    public void RestartingSomeoneElseNeedsTheHaltPower()
    {
        var denied = _dispatcher.Dispatch(_ada, "@restart One", _db);
        Assert.Contains("Permission denied.", denied.Notifications);
        Assert.Empty(_queue.Snapshot());

        var lamp = _db.GetObject(_lamp)!;
        var restarted = _dispatcher.Dispatch(_god, "@restart Lamp", _db);
        Assert.Contains("Restarting: Ada's Lamp(#3)", restarted.Notifications);
        Assert.Contains(restarted.TargetedNotifications, note => note.Target == _ada && note.Message == "Restarting: Lamp(#3), by One");
        Assert.Equal(["@emit awake", "@emit two"], _queue.Snapshot().Select(entry => entry.Command));
        Assert.Equal(_ada, lamp.Owner);
    }

    [Fact]
    public void RestartAllTellsEveryPlayerAndSkipsAHaltedObject()
    {
        var stopped = _db.CreateObject("Stopped", ObjectType.Thing, _god);
        stopped.AddFlag("HALT");
        stopped.SetAttribute("STARTUP", "@emit stopped", _god);
        var mortal = _dispatcher.Dispatch(_ada, "@restart/all", _db);
        Assert.Contains("You do not have the power to restart the world.", mortal.Notifications);

        var world = _dispatcher.Dispatch(_god, "@restart /all", _db);
        Assert.Equal(
            [
                "Your objects have been globally halted by One",
                "Halted: One(#1)",
                "Your objects are being globally restarted by One"
            ],
            world.Notifications);
        Assert.Contains(world.TargetedNotifications, note => note.Target == _ada && note.Message == "Your objects have been globally halted by One");
        Assert.Contains(world.TargetedNotifications, note => note.Target == _ada && note.Message == "Your objects are being globally restarted by One");
        Assert.Equal(["@emit awake", "@emit two"], _queue.Snapshot().Select(entry => entry.Command));
    }

    [Fact]
    public void AMissingObjectAndAGaggedPlayerAreRefused()
    {
        var missing = _dispatcher.Dispatch(_ada, "@restart nobody", _db);
        Assert.Contains("I can't see that here.", missing.Notifications);

        _db.GetObject(_ada)!.AddFlag("GAGGED");
        var gagged = _dispatcher.Dispatch(_ada, "@restart", _db);
        Assert.Contains("You cannot do that while gagged.", gagged.Notifications);
        Assert.Empty(_queue.Snapshot());
    }
}
