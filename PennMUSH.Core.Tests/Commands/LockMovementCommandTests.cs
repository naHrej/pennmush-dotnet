namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;
using Xunit;

public class LockMovementCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher = new();
    private readonly ActionQueue _queue = new();
    private readonly Dbref _room;
    private readonly Dbref _owner;

    public LockMovementCommandTests()
    {
        _room = _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1)).Id;
        _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        _owner = _db.CreateObject("Owner", ObjectType.Player, new Dbref(1)).Id;
        _db.MoveObject(_owner, _room);
        _dispatcher.Register(new GoCommandHandler(), "go");
        _dispatcher.Register(new GetCommandHandler(), "get");
        _dispatcher.Register(new DropCommandHandler(), "drop");
        _dispatcher.Register(new EnterCommandHandler(), "enter");
        _dispatcher.Register(new LeaveCommandHandler(), "leave");
        _dispatcher.Register(new GiveCommandHandler(), "give");
        _dispatcher.Register(new PageCommandHandler(), "page");
        _dispatcher.Register(new UseCommandHandler(_queue), "use");
        _dispatcher.Register(new EmptyCommandHandler(), "empty");
        _dispatcher.Register(new TeachCommandHandler(_queue), "teach");
        _dispatcher.Register(new ThinkCommandHandler(), "think");
    }

    [Fact]
    public void Go_HonorsLeaveLockAndStoredExitLock()
    {
        var castle = _db.CreateObject("Castle", ObjectType.Room, _owner);
        var exit = _db.CreateObject("North", ObjectType.Exit, _owner);
        _db.AttachExit(_room, exit.Id);
        _db.LinkExit(exit.Id, castle.Id);
        var room = _db.GetObject(_room)!;
        room.SetLock(LockType.Leave, new ConstantLockNode(false), _owner);

        var blocked = _dispatcher.Dispatch(_owner, "go North", _db);

        Assert.Equal(_room, _db.GetObject(_owner)!.Location);
        Assert.Contains("You can't go that way.", blocked.Notifications);
        room.ClearLock(LockType.Leave);
        exit.SetLock(LockType.Basic, new ConstantLockNode(false), _owner);
        exit.SetAttribute("FAIL", "The gate is shut.", _owner);

        var locked = _dispatcher.Dispatch(_owner, "go North", _db);

        Assert.Equal(_room, _db.GetObject(_owner)!.Location);
        Assert.Contains("The gate is shut.", locked.Notifications);
        exit.ClearLock(LockType.Basic);

        var open = _dispatcher.Dispatch(_owner, "go North", _db);

        Assert.Equal(castle.Id, _db.GetObject(_owner)!.Location);
        Assert.Contains("Castle", open.Notifications);
    }

    [Fact]
    public void GetAndDrop_HonorBasicAndDropLocks()
    {
        var coin = _db.CreateObject("Coin", ObjectType.Thing, _owner);
        _db.MoveObject(coin.Id, _room);
        coin.SetLock(LockType.Basic, new ConstantLockNode(false), _owner);

        var stuck = _dispatcher.Dispatch(_owner, "get Coin", _db);

        Assert.Equal(_room, coin.Location);
        Assert.Contains("You can't pick that up.", stuck.Notifications);
        coin.ClearLock(LockType.Basic);
        _dispatcher.Dispatch(_owner, "get Coin", _db);
        Assert.Equal(_owner, coin.Location);

        coin.SetLock(LockType.Drop, new ConstantLockNode(false), _owner);
        var held = _dispatcher.Dispatch(_owner, "drop Coin", _db);
        Assert.Equal(_owner, coin.Location);
        Assert.Contains("You can't seem to get rid of that.", held.Notifications);

        coin.ClearLock(LockType.Drop);
        _db.GetObject(_room)!.SetLock(LockType.Drop, new ConstantLockNode(false), _owner);
        var roomLocked = _dispatcher.Dispatch(_owner, "drop Coin", _db);
        Assert.Equal(_owner, coin.Location);
        Assert.Contains("You can't seem to drop things here.", roomLocked.Notifications);

        _db.GetObject(_room)!.ClearLock(LockType.Drop);
        var dropped = _dispatcher.Dispatch(_owner, "drop Coin", _db);
        Assert.Equal(_room, coin.Location);
        Assert.Contains("Dropped.", dropped.Notifications);
    }

    [Fact]
    public void EnterAndLeave_HonorTheirLocks()
    {
        var box = _db.CreateObject("Box", ObjectType.Thing, _owner);
        _db.MoveObject(box.Id, _room);
        box.SetLock(LockType.Enter, new ConstantLockNode(false), _owner);

        var denied = _dispatcher.Dispatch(_owner, "enter Box", _db);

        Assert.Equal(_room, _db.GetObject(_owner)!.Location);
        Assert.Contains("Permission denied.", denied.Notifications);
        box.ClearLock(LockType.Enter);
        _dispatcher.Dispatch(_owner, "enter Box", _db);
        Assert.Equal(box.Id, _db.GetObject(_owner)!.Location);

        box.SetLock(LockType.Leave, new ConstantLockNode(false), _owner);
        var trapped = _dispatcher.Dispatch(_owner, "leave", _db);
        Assert.Equal(box.Id, _db.GetObject(_owner)!.Location);
        Assert.Contains("You can't leave.", trapped.Notifications);

        box.ClearLock(LockType.Leave);
        _dispatcher.Dispatch(_owner, "leave", _db);
        Assert.Equal(_room, _db.GetObject(_owner)!.Location);
    }

    [Fact]
    public void GiveAndPage_HonorTheirLocks()
    {
        var friend = _db.CreateObject("Friend", ObjectType.Player, _owner);
        _db.MoveObject(friend.Id, _room);
        var gift = _db.CreateObject("Gift", ObjectType.Thing, _owner);
        _db.MoveObject(gift.Id, _owner);
        gift.SetLock(LockType.Give, new ConstantLockNode(false), _owner);

        var refused = _dispatcher.Dispatch(_owner, "give Friend=Gift", _db);

        Assert.Equal(_owner, gift.Location);
        Assert.Contains("You can't give that away.", refused.Notifications);
        gift.ClearLock(LockType.Give);
        var given = _dispatcher.Dispatch(_owner, "give Friend=Gift", _db);
        Assert.Equal(friend.Id, gift.Location);
        Assert.Contains("You give Gift to Friend.", given.Notifications);

        friend.SetLock(LockType.Page, new ConstantLockNode(false), _owner);
        var silent = _dispatcher.Dispatch(_owner, "page Friend=hello", _db);
        Assert.Contains("Permission denied.", silent.Notifications);
        Assert.DoesNotContain(silent.Notifications, note => note.Contains("pages"));
        friend.ClearLock(LockType.Page);
        var paged = _dispatcher.Dispatch(_owner, "page Friend=hello", _db);
        Assert.Contains("You page Friend with 'hello'.", paged.Notifications);
    }

    [Fact]
    public void Use_RunsTheUseMessageAndQueuesTheAction()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        _db.MoveObject(lamp.Id, _room);
        lamp.SetAttribute("USE", "The lamp clicks.", _owner);
        lamp.SetAttribute("AUSE", "think clicked", _owner);

        var used = _dispatcher.Dispatch(_owner, "use Lamp", _db);
        var ran = _queue.ProcessReady(DateTimeOffset.UtcNow.AddSeconds(1), _dispatcher, _db);

        Assert.Contains("The lamp clicks.", used.Notifications);
        Assert.Contains("clicked", ran.SelectMany(ctx => ctx.Notifications));

        lamp.SetLock(LockType.Use, new ConstantLockNode(false), _owner);
        int before = _queue.Count;
        var denied = _dispatcher.Dispatch(_owner, "use Lamp", _db);
        Assert.Contains("Permission denied.", denied.Notifications);
        Assert.Equal(before, _queue.Count);
    }

    [Fact]
    public void Use_QueuesRunoutWhenThereIsNoAction()
    {
        var torch = _db.CreateObject("Torch", ObjectType.Thing, _owner);
        _db.MoveObject(torch.Id, _room);
        torch.SetAttribute("RUNOUT", "think dry", _owner);

        _dispatcher.Dispatch(_owner, "use Torch", _db);
        var ran = _queue.ProcessReady(DateTimeOffset.UtcNow.AddSeconds(1), _dispatcher, _db);

        Assert.Contains("Used.", _dispatcher.Dispatch(_owner, "use Torch", _db).Notifications);
        Assert.Contains("dry", ran.SelectMany(ctx => ctx.Notifications));
    }

    [Fact]
    public void Empty_MovesWhatTheLocksAllow()
    {
        var bag = _db.CreateObject("Bag", ObjectType.Thing, _owner);
        var coin = _db.CreateObject("Coin", ObjectType.Thing, _owner);
        var gem = _db.CreateObject("Gem", ObjectType.Thing, _owner);
        _db.MoveObject(bag.Id, _owner);
        _db.MoveObject(coin.Id, bag.Id);
        _db.MoveObject(gem.Id, bag.Id);
        gem.SetLock(LockType.Basic, new ConstantLockNode(false), _owner);

        var emptied = _dispatcher.Dispatch(_owner, "empty Bag", _db);

        Assert.Equal(_owner, coin.Location);
        Assert.Equal(bag.Id, gem.Location);
        Assert.Contains("You remove 1 object from Bag.", emptied.Notifications);

        _db.MoveObject(bag.Id, _room);
        var dropped = _dispatcher.Dispatch(_owner, "empty me", _db);
        Assert.Equal(_room, coin.Location);
        Assert.Contains("You remove 1 object from Owner.", dropped.Notifications);

        var foreign = _db.CreateObject("Crate", ObjectType.Thing, new Dbref(1));
        var cargo = _db.CreateObject("Cargo", ObjectType.Thing, new Dbref(1));
        _db.MoveObject(foreign.Id, _room);
        _db.MoveObject(cargo.Id, foreign.Id);
        foreign.SetLock(LockType.Enter, new ConstantLockNode(false), new Dbref(1));
        var denied = _dispatcher.Dispatch(_owner, "empty Crate", _db);
        Assert.Equal(foreign.Id, cargo.Location);
        Assert.Contains("Permission denied.", denied.Notifications);
    }

    [Fact]
    public void Teach_AnnouncesTheCommandAndQueuesIt()
    {
        var taught = _dispatcher.Dispatch(_owner, "teach think lesson", _db);
        Assert.Contains("Owner types --> think lesson", taught.Notifications);

        var ran = _queue.ProcessReady(DateTimeOffset.UtcNow.AddSeconds(1), _dispatcher, _db);
        Assert.Contains("lesson", ran.SelectMany(ctx => ctx.Notifications));

        _db.GetObject(_room)!.SetLock(LockType.Speech, new ConstantLockNode(false), _owner);
        int before = _queue.Count;
        var denied = _dispatcher.Dispatch(_owner, "teach think again", _db);
        Assert.Contains("Permission denied.", denied.Notifications);
        Assert.Equal(before, _queue.Count);
    }
}
