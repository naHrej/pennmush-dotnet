namespace PennMUSH.Core.Tests.Spatial;

using System;
using Xunit;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class SpatialAndTraversalTests
{
    private readonly ObjectDatabase _db;
    private readonly CommandDispatcher _dispatcher;
    private readonly ExpressionEvaluator _evaluator;
    private readonly LookCommandHandler _lookHandler;
    private readonly MUSHObject _roomZero;
    private readonly MUSHObject _god;
    private readonly MUSHObject _player;
    private readonly MUSHObject _startRoom;

    public SpatialAndTraversalTests()
    {
        _db = new ObjectDatabase();
        var registry = new FunctionRegistry(_db);
        _evaluator = new ExpressionEvaluator(registry.Dispatch, registry.Database);
        registry.Evaluator = _evaluator;

        _lookHandler = new LookCommandHandler(_evaluator);

        // #0 = Room Zero
        _roomZero = _db.CreateObject("Room Zero", ObjectType.Room, new Dbref(1));
        // #1 = God
        _god = _db.CreateObject("God", ObjectType.Player, new Dbref(1));
        // #2 = Player
        _player = _db.CreateObject("Adventurer", ObjectType.Player, _god.Id);
        // #3 = Start Room
        _startRoom = _db.CreateObject("Town Square", ObjectType.Room, _god.Id);

        _db.MoveObject(_player.Id, _startRoom.Id);

        _dispatcher = new CommandDispatcher();
        _dispatcher.Register(new DigCommandHandler(_lookHandler));
        _dispatcher.Register(new OpenCommandHandler());
        _dispatcher.Register(new LinkCommandHandler());
        _dispatcher.Register(new UnlinkCommandHandler());
        var goHandler = new GoCommandHandler(_lookHandler, _evaluator);
        _dispatcher.Register(goHandler, "goto", "move");
        _dispatcher.Register(_lookHandler, "l");
        _dispatcher.Register(new GetCommandHandler(), "take");
        _dispatcher.Register(new DropCommandHandler());
        _dispatcher.Register(new InventoryCommandHandler(), "i");
    }

    [Fact]
    public void DigCommand_CreatesRoomWithTwoWayExits()
    {
        var ctx = _dispatcher.Dispatch(_player.Id, "@dig Tavern=North;n,South;s", _db);

        Assert.Contains(ctx.Notifications, n => n.Contains("Tavern created"));
        Assert.Contains(ctx.Notifications, n => n.Contains("Opening exit North;n"));
        Assert.Contains(ctx.Notifications, n => n.Contains("Opening exit South;s"));

        // Verify exit in start room
        var northExit = _db.FindExit(_startRoom.Id, "n");
        Assert.NotNull(northExit);
        Assert.Equal(ObjectType.Exit, northExit.Type);
        Assert.True(northExit.Location.IsValid);

        // Verify exit in Tavern
        var tavern = _db.GetObject(northExit.Location);
        Assert.NotNull(tavern);
        Assert.Equal("Tavern", tavern.Name);

        var southExit = _db.FindExit(tavern.Id, "s");
        Assert.NotNull(southExit);
        Assert.Equal(_startRoom.Id, southExit.Location);
    }

    [Fact]
    public void DigTeleport_MovesPlayerToNewRoom()
    {
        var ctx = _dispatcher.Dispatch(_player.Id, "@dig/teleport Dungeon", _db);

        Assert.Contains(ctx.Notifications, n => n.Contains("Teleported to Dungeon."));
        var newRoom = _db.GetObject(_player.Location);
        Assert.NotNull(newRoom);
        Assert.Equal("Dungeon", newRoom.Name);
    }

    [Fact]
    public void OpenAndLink_CreatesLinkedExit()
    {
        var destRoom = _db.CreateObject("Marketplace", ObjectType.Room, _god.Id);
        var ctx = _dispatcher.Dispatch(_player.Id, $"@open East;e={destRoom.Id}", _db);

        Assert.Contains(ctx.Notifications, n => n.Contains("Opened and linked to Marketplace"));

        var exit = _db.FindExit(_startRoom.Id, "East");
        Assert.NotNull(exit);
        Assert.Equal(destRoom.Id, exit.Location);
    }

    [Fact]
    public void GoCommand_TraversesExitAndInvokesLook()
    {
        var castle = _db.CreateObject("Castle", ObjectType.Room, _god.Id);
        castle.SetAttribute("DESCRIBE", "A majestic castle with towering spires.", _god.Id);

        var exit = _db.CreateObject("North;n;enter castle", ObjectType.Exit, _god.Id);
        exit.SetAttribute("SUCC", "You pass through the iron gates.", _god.Id);
        _db.AttachExit(_startRoom.Id, exit.Id);
        _db.LinkExit(exit.Id, castle.Id);

        var ctx = _dispatcher.Dispatch(_player.Id, "go North", _db);

        Assert.Equal(castle.Id, _player.Location);
        Assert.Contains("You pass through the iron gates.", ctx.Notifications);
        Assert.Contains("Castle", ctx.Notifications);
        Assert.Contains("A majestic castle with towering spires.", ctx.Notifications);
    }

    [Fact]
    public void ImplicitExitMatching_TraversesViaAlias()
    {
        var forest = _db.CreateObject("Enchanted Forest", ObjectType.Room, _god.Id);
        var exit = _db.CreateObject("North;n;forest;trail", ObjectType.Exit, _god.Id);
        _db.AttachExit(_startRoom.Id, exit.Id);
        _db.LinkExit(exit.Id, forest.Id);

        // Player just types 'trail'
        var ctx = _dispatcher.Dispatch(_player.Id, "trail", _db);

        Assert.Equal(forest.Id, _player.Location);
        Assert.Contains("Enchanted Forest", ctx.Notifications);
    }

    [Fact]
    public void ExitLock_PreventsEntryWhenLocked()
    {
        var vault = _db.CreateObject("Vault", ObjectType.Room, _god.Id);
        var exit = _db.CreateObject("Vault;v", ObjectType.Exit, _god.Id);
        exit.SetAttribute("LOCK", $"#{_god.Id.Value}", _god.Id); // Only God can pass
        exit.SetAttribute("FAIL", "The heavy stone door refuses to budge.", _god.Id);
        _db.AttachExit(_startRoom.Id, exit.Id);
        _db.LinkExit(exit.Id, vault.Id);

        // Player tries to enter
        var failCtx = _dispatcher.Dispatch(_player.Id, "v", _db);
        Assert.Equal(_startRoom.Id, _player.Location);
        Assert.Contains("The heavy stone door refuses to budge.", failCtx.Notifications);

        // God tries to enter
        _db.MoveObject(_god.Id, _startRoom.Id);
        var passCtx = _dispatcher.Dispatch(_god.Id, "v", _db);
        Assert.Equal(vault.Id, _god.Location);
    }

    [Fact]
    public void InventoryOperations_GetDropAndListItems()
    {
        var sword = _db.CreateObject("Silver Sword", ObjectType.Thing, _player.Id);
        _db.MoveObject(sword.Id, _startRoom.Id);

        // Initial inventory empty
        var invEmpty = _dispatcher.Dispatch(_player.Id, "i", _db);
        Assert.Contains("You are empty-handed.", invEmpty.Notifications);

        // Look in room sees sword in contents
        var lookCtx = _dispatcher.Dispatch(_player.Id, "look", _db);
        Assert.Contains("Contents:", lookCtx.Notifications);
        Assert.Contains("  Silver Sword", lookCtx.Notifications);

        // Take sword
        var getCtx = _dispatcher.Dispatch(_player.Id, "get Silver Sword", _db);
        Assert.Contains("Taken.", getCtx.Notifications);
        Assert.Equal(_player.Id, sword.Location);

        // Check inventory has sword
        var invWithSword = _dispatcher.Dispatch(_player.Id, "inventory", _db);
        Assert.Contains("You are carrying:", invWithSword.Notifications);
        Assert.Contains("  Silver Sword", invWithSword.Notifications);

        // Drop sword
        var dropCtx = _dispatcher.Dispatch(_player.Id, "drop Silver Sword", _db);
        Assert.Contains("Dropped.", dropCtx.Notifications);
        Assert.Equal(_startRoom.Id, sword.Location);
    }

    [Fact]
    public void MUSHcode_SpatialFunctions_EvaluateProperly()
    {
        var ctx = new EvaluationContext(_player.Id, _player.Id);

        var room = _db.CreateObject("Observatory", ObjectType.Room, _god.Id);
        var exit1 = _db.CreateObject("East;e", ObjectType.Exit, _god.Id);
        var exit2 = _db.CreateObject("West;w", ObjectType.Exit, _god.Id);
        var telescope = _db.CreateObject("Brass Telescope", ObjectType.Thing, _god.Id);

        _db.AttachExit(room.Id, exit1.Id);
        _db.AttachExit(room.Id, exit2.Id);
        _db.MoveObject(telescope.Id, room.Id);

        // con
        Assert.Equal(telescope.Id.ToString(), _evaluator.Evaluate($"[con({room.Id})]", ctx));

        // exits / lexits
        var exitsResult = _evaluator.Evaluate($"[exits({room.Id})]", ctx);
        Assert.Contains(exit1.Id.ToString(), exitsResult);
        Assert.Contains(exit2.Id.ToString(), exitsResult);

        var lexitsResult = _evaluator.Evaluate($"[lexits({room.Id})]", ctx);
        Assert.Equal(exitsResult, lexitsResult);
    }
}
