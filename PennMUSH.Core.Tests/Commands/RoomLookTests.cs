namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Locks;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class RoomLookTests
{
    private readonly ObjectDatabase _db = new();
    private readonly LookCommandHandler _look;
    private readonly CommandDispatcher _dispatcher;

    public RoomLookTests()
    {
        var registry = new FunctionRegistry(_db);
        var evaluator = new ExpressionEvaluator(registry.Dispatch, registry.Database);
        registry.Evaluator = evaluator;
        _look = new LookCommandHandler(evaluator);
        _dispatcher = new CommandDispatcher();
        _dispatcher.Register(_look, "look");
        _dispatcher.Register(new GoCommandHandler(_look, evaluator), "go");
        _dispatcher.Register(new EnterCommandHandler(_look), "enter");
        _dispatcher.Register(new LeaveCommandHandler(_look), "leave");
        _dispatcher.Register(new TeleportCommandHandler(_look), "@teleport");
    }

    [Fact]
    public void ExplicitLook_ShowsRoomTextWithoutEmptyFiller()
    {
        var room = _db.CreateObject("Hall", ObjectType.Room, new Dbref(1));
        room.SetAttribute("DESCRIBE", "Stone and torchlight.", new Dbref(1));
        room.SetAttribute("SUCCESS", "The air is cold.", new Dbref(1));
        var player = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        var lamp = _db.CreateObject("Lamp;lantern", ObjectType.Thing, new Dbref(1));
        var north = _db.CreateObject("North;n", ObjectType.Exit, new Dbref(1));
        _db.MoveObject(player.Id, room.Id);
        _db.MoveObject(lamp.Id, room.Id);
        _db.AttachExit(room.Id, north.Id);

        var ctx = _dispatcher.Dispatch(player.Id, "look", _db);

        Assert.Contains("Hall", ctx.Notifications);
        Assert.Contains("Stone and torchlight.", ctx.Notifications);
        Assert.Contains("The air is cold.", ctx.Notifications);
        Assert.Contains("Contents:", ctx.Notifications);
        Assert.Contains("  Lamp", ctx.Notifications);
        Assert.Contains("Obvious exits:", ctx.Notifications);
        Assert.Contains("  North", ctx.Notifications);
        Assert.DoesNotContain("You see nothing special.", ctx.Notifications);
    }

    [Fact]
    public void AutomaticTerseLook_SkipsDescriptionAndSuccess()
    {
        var room = _db.CreateObject("Hall", ObjectType.Room, new Dbref(1));
        room.SetAttribute("DESCRIBE", "Stone and torchlight.", new Dbref(1));
        room.SetAttribute("SUCCESS", "The air is cold.", new Dbref(1));
        room.SetAttribute("OSUCCESS", "Ada glances around.", new Dbref(1));
        var player = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        player.AddFlag("TERSE");
        var other = _db.CreateObject("Side Room", ObjectType.Room, new Dbref(1));
        var exit = _db.CreateObject("North;n", ObjectType.Exit, new Dbref(1));
        _db.MoveObject(player.Id, other.Id);
        _db.AttachExit(other.Id, exit.Id);
        _db.LinkExit(exit.Id, room.Id);

        var ctx = _dispatcher.Dispatch(player.Id, "go north", _db);

        Assert.Contains("Hall", ctx.Notifications);
        Assert.DoesNotContain("Stone and torchlight.", ctx.Notifications);
        Assert.DoesNotContain("The air is cold.", ctx.Notifications);
        Assert.Contains(ctx.TargetedNotifications, note =>
            note.Scope == NotificationScope.RoomExcept && note.Message == "Ada glances around.");
    }

    [Fact]
    public void LockedRoom_ShowsFailureInsteadOfSuccess()
    {
        var room = _db.CreateObject("Vault", ObjectType.Room, new Dbref(1));
        room.SetLock(LockType.Basic, new ConstantLockNode(false), new Dbref(1));
        room.SetAttribute("FAILURE", "The vault is sealed.", new Dbref(1));
        room.SetAttribute("SUCCESS", "You should not see this.", new Dbref(1));
        var player = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        _db.MoveObject(player.Id, room.Id);

        var ctx = _dispatcher.Dispatch(player.Id, "look", _db);

        Assert.Contains("The vault is sealed.", ctx.Notifications);
        Assert.DoesNotContain("You should not see this.", ctx.Notifications);
    }

    [Fact]
    public void EnterAndLeave_ShowThePlaceJustEntered()
    {
        var room = _db.CreateObject("Hall", ObjectType.Room, new Dbref(1));
        room.SetAttribute("DESCRIBE", "Back in the hall.", new Dbref(1));
        var box = _db.CreateObject("Cabinet", ObjectType.Thing, new Dbref(1));
        box.SetAttribute("IDESCRIBE", "Shelves of bottles.", new Dbref(1));
        var player = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        _db.MoveObject(player.Id, room.Id);
        _db.MoveObject(box.Id, room.Id);

        var entered = _dispatcher.Dispatch(player.Id, "enter Cabinet", _db);

        Assert.Equal(box.Id, player.Location);
        Assert.Contains("You enter Cabinet.", entered.Notifications);
        Assert.Contains("Cabinet", entered.Notifications);
        Assert.Contains("Shelves of bottles.", entered.Notifications);

        var left = _dispatcher.Dispatch(player.Id, "leave", _db);

        Assert.Equal(room.Id, player.Location);
        Assert.Contains("Hall", left.Notifications);
        Assert.Contains("Back in the hall.", left.Notifications);
    }

    [Fact]
    public void Teleport_ShowsTheDestinationToThePlayerWhoMoved()
    {
        var start = _db.CreateObject("Start", ObjectType.Room, new Dbref(1));
        var dest = _db.CreateObject("Observatory", ObjectType.Room, new Dbref(1));
        dest.SetAttribute("DESCRIBE", "The dome is open.", new Dbref(1));
        var wizard = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        var guest = _db.CreateObject("Bea", ObjectType.Player, new Dbref(1));
        _db.MoveObject(wizard.Id, start.Id);
        _db.MoveObject(guest.Id, start.Id);

        var self = _dispatcher.Dispatch(wizard.Id, $"@teleport {dest.Id}", _db);
        Assert.Contains("Teleported.", self.Notifications);
        Assert.Contains("Observatory", self.Notifications);
        Assert.Contains("The dome is open.", self.Notifications);

        var other = _dispatcher.Dispatch(wizard.Id, $"@teleport {guest.Id}={dest.Id}", _db);
        Assert.Contains("Teleported.", other.Notifications);
        Assert.DoesNotContain("The dome is open.", other.Notifications);
        Assert.Contains(other.TargetedNotifications, note =>
            note.Scope == NotificationScope.Target && note.Target == guest.Id && note.Message == "The dome is open.");
    }
}
