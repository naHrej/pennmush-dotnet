namespace PennMUSH.Core.Tests.Commands;

using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Types;
using Xunit;

public class FollowerCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher = new();
    private readonly HashSet<int> _online = new();
    private readonly Dbref _room;
    private readonly Dbref _ada;
    private readonly Dbref _bea;

    public FollowerCommandTests()
    {
        _room = _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1)).Id;
        _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        _ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1)).Id;
        _bea = _db.CreateObject("Bea", ObjectType.Player, new Dbref(1)).Id;
        _db.MoveObject(_ada, _room);
        _db.MoveObject(_bea, _room);
        _online.Add(_ada.Value);
        _online.Add(_bea.Value);
        _dispatcher.Register(new FollowCommandHandler(id => _online.Contains(id.Value)), "follow");
        _dispatcher.Register(new UnfollowCommandHandler(), "unfollow");
        _dispatcher.Register(new DismissCommandHandler(), "dismiss");
        _dispatcher.Register(new DesertCommandHandler(), "desert");
        _dispatcher.Register(new GoCommandHandler(null, null, _dispatcher, id => _online.Contains(id.Value)), "go");
        _dispatcher.Register(new EnterCommandHandler(null, _dispatcher, id => _online.Contains(id.Value)), "enter");
        _dispatcher.Register(new LeaveCommandHandler(null, _dispatcher, id => _online.Contains(id.Value)), "leave");
    }

    [Fact]
    public void Follow_ListsAddsAndRefuses()
    {
        var empty = Run(_ada, "follow");
        Assert.Equal(new[] { "You are following: ", "You are followed by: " }, empty.Notifications);

        var started = Run(_bea, "follow Ada");
        Assert.Contains("You begin following Ada.", started.Notifications);
        Assert.Contains("Bea begins following you.", started.TargetedNotifications.Select(note => note.Message));
        Assert.Equal("#" + _bea.Value, _db.GetObject(_ada)!.GetAttribute("FOLLOWERS")!.Value);
        Assert.Equal("#" + _ada.Value, _db.GetObject(_bea)!.GetAttribute("FOLLOWING")!.Value);
        Assert.Equal("You're already following Ada.", Single(Run(_bea, "follow Ada")));
        Assert.Equal("You chase your tail for a while and feel silly.", Single(Run(_ada, "follow me")));
        Assert.Equal("You can only follow players and things.", Single(Run(_ada, "follow here")));
        Assert.Equal("You don't see that here.", Single(Run(_ada, "follow Nobody")));

        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _ada);
        var lance = _db.CreateObject("Lance", ObjectType.Thing, _ada);
        _db.MoveObject(lamp.Id, _room);
        _db.MoveObject(lance.Id, _room);
        Assert.Equal("I can't tell which one to follow.", Single(Run(_ada, "follow L")));

        var hidden = _db.CreateObject("Crate", ObjectType.Thing, _ada);
        hidden.AddFlag("DARK");
        _db.MoveObject(hidden.Id, _room);
        Assert.Equal("You don't see that here.", Single(Run(_bea, "follow Crate")));

        var cam = _db.CreateObject("Cam", ObjectType.Player, new Dbref(1));
        _db.MoveObject(cam.Id, _room);
        Assert.Equal("You don't see that here.", Single(Run(_ada, "follow Cam")));

        var dee = _db.CreateObject("Dee", ObjectType.Player, new Dbref(1));
        _db.MoveObject(dee.Id, _room);
        _online.Add(dee.Id.Value);
        var leader = _db.GetObject(_ada)!;
        leader.SetLock(LockType.Follow, new ConstantLockNode(false), _ada);
        Assert.Equal("You're not allowed to follow.", Single(Run(dee.Id, "follow Ada")));
        leader.SetAttribute("FOLLOW_LOCK`FAILURE", "Ada refuses.", _ada);
        Assert.Equal("Ada refuses.", Single(Run(dee.Id, "follow Ada")));
    }

    [Fact]
    public void UnfollowDismissAndDesert_UpdateBothLists()
    {
        Run(_bea, "follow Ada");
        var stopped = Run(_bea, "unfollow Ada");
        Assert.Contains("You stop following Ada.", stopped.Notifications);
        Assert.Contains("Bea stops following you.", stopped.TargetedNotifications.Select(note => note.Message));
        Assert.Equal("You're not following Ada.", Single(Run(_bea, "unfollow Ada")));
        Assert.Equal("You stop following anyone.", Single(Run(_bea, "unfollow")));

        Run(_bea, "follow Ada");
        var dismissed = Run(_ada, "dismiss Bea");
        Assert.Contains("Bea stops following you.", dismissed.Notifications);
        Assert.Equal("Bea isn't following you.", Single(Run(_ada, "dismiss Bea")));
        Assert.Equal("You dismiss all your followers.", Single(Run(_ada, "dismiss")));

        Run(_bea, "follow Ada");
        var deserted = Run(_bea, "desert Ada");
        Assert.Contains("You stop following Ada.", deserted.Notifications);
        Assert.Contains("Ada stops following you.", deserted.Notifications);
        Assert.Equal("Ada isn't following you, nor vice versa.", Single(Run(_bea, "desert Ada")));
        Assert.Equal("You desert everyone you're leading or following.", Single(Run(_ada, "desert")));
    }

    [Fact]
    public void Movement_BringsFollowersWhoArePresent()
    {
        var hall = _db.CreateObject("Hall", ObjectType.Room, _ada);
        var north = _db.CreateObject("North", ObjectType.Exit, _ada);
        _db.AttachExit(_room, north.Id);
        _db.LinkExit(north.Id, hall.Id);
        var box = _db.CreateObject("Box", ObjectType.Thing, _ada);
        var pup = _db.CreateObject("Pup", ObjectType.Thing, _ada);
        var cam = _db.CreateObject("Cam", ObjectType.Player, new Dbref(1));
        _db.MoveObject(box.Id, _room);
        _db.MoveObject(pup.Id, _room);
        _db.MoveObject(cam.Id, _room);
        _online.Add(cam.Id.Value);
        Run(_bea, "follow Ada");
        Run(pup.Id, "follow Ada");
        Run(cam.Id, "follow Ada");
        _online.Remove(cam.Id.Value);

        var moved = Run(_ada, "go North");
        Assert.Equal(hall.Id, _db.GetObject(_ada)!.Location);
        Assert.Equal(hall.Id, _db.GetObject(_bea)!.Location);
        Assert.Equal(hall.Id, _db.GetObject(pup.Id)!.Location);
        Assert.Equal(_room, _db.GetObject(cam.Id)!.Location);
        Assert.Contains("You follow Ada.", moved.TargetedNotifications.Select(note => note.Message));

        _db.MoveObject(box.Id, hall.Id);
        Run(_ada, "enter Box");
        Assert.Equal(box.Id, _db.GetObject(_ada)!.Location);
        Assert.Equal(box.Id, _db.GetObject(_bea)!.Location);
        Run(_ada, "leave");
        Assert.Equal(hall.Id, _db.GetObject(_ada)!.Location);
        Assert.Equal(hall.Id, _db.GetObject(_bea)!.Location);
    }

    private CommandContext Run(Dbref who, string command) => _dispatcher.Dispatch(who, command, _db);

    private static string Single(CommandContext ctx) => Assert.Single(ctx.Notifications);
}
