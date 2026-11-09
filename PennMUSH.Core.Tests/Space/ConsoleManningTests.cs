namespace PennMUSH.Core.Tests.Space;

using PennMUSH.Space;

public class ConsoleManningTests
{
    [Fact]
    public void Console_BelongsToTheShipAndItsRoom()
    {
        var manager = new SpaceManager();
        var ship = manager.CreateObject(10);
        ship.AddRoom(4);
        var console = manager.AddConsole(10, 20, 4, "helm", "tactical");

        Assert.Equal(ship, console.Ship);
        Assert.Equal(4, console.RoomId);
        Assert.Contains(console, ship.Consoles);
        Assert.Equal(20, manager.FindConsole(20)!.Id);
    }

    [Fact]
    public void ManConsole_RequiresThePlayersRoomAndOneConsole()
    {
        var manager = new SpaceManager();
        var ship = manager.CreateObject(10);
        ship.AddRoom(4);
        ship.AddRoom(5);
        manager.AddConsole(10, 20, 4, "helm");
        manager.AddConsole(10, 21, 5, "tactical");

        Assert.False(manager.ManConsole(20, playerId: 7, playerRoomId: 5));
        Assert.True(manager.ManConsole(20, playerId: 7, playerRoomId: 4));
        Assert.False(manager.ManConsole(21, playerId: 7, playerRoomId: 5));
        Assert.False(manager.ManConsole(20, playerId: 8, playerRoomId: 4));

        Assert.True(manager.SelectMode(20, 7, "Helm"));
        Assert.Equal("helm", manager.FindConsole(20)!.ActiveMode);
        Assert.False(manager.SelectMode(20, 7, "tactical"));

        Assert.True(manager.UnmanConsole(20, 7));
        Assert.Null(manager.FindConsole(20)!.OperatorId);
        Assert.Null(manager.FindConsole(20)!.ActiveMode);
        Assert.True(manager.ManConsole(21, playerId: 7, playerRoomId: 5));
    }

    [Fact]
    public void AddConsole_RejectsARoomThatIsNotAboard()
    {
        var ship = new SpaceManager().CreateObject(10);
        Assert.Throws<InvalidOperationException>(() => ship.AddConsole(20, 4, "helm"));
    }
}
