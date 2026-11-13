namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;
using Xunit;

public class PurgeCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher = new();
    private readonly Dbref _god;
    private readonly Dbref _owner;
    private readonly Dbref _hall;

    public PurgeCommandTests()
    {
        _hall = _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1)).Id;
        _god = _db.CreateObject("One", ObjectType.Player, new Dbref(1)).Id;
        _owner = _db.CreateObject("Owner", ObjectType.Player, _god).Id;
        _db.MoveObject(_owner, _hall);
        _db.MoveObject(_god, _hall);
        _dispatcher.Register(new DestroyCommandHandler(), "@destroy", "@recycle", "@nuke");
        _dispatcher.Register(new UndestroyCommandHandler(), "@undestroy");
        _dispatcher.Register(new PurgeCommandHandler(), "@purge");
    }

    [Fact]
    public void Nuke_FreesAnObjectThatIsAlreadyGoing()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        _db.MoveObject(lamp.Id, _hall);

        var marked = _dispatcher.Dispatch(_owner, $"@nuke #{lamp.Id.Value}", _db);

        Assert.Contains($"The wrecking ball comes for Lamp ({lamp.Id}).", marked.Notifications);
        Assert.True(lamp.HasFlag("GOING"));
        Assert.Equal(ObjectType.Thing, lamp.Type);
        Assert.Contains(lamp, _db.GetContents(_hall));

        var freed = _dispatcher.Dispatch(_owner, $"@destroy #{lamp.Id.Value}", _db);

        Assert.Contains($"Lamp ({lamp.Id}) has been destroyed.", freed.Notifications);
        Assert.Equal(ObjectType.Garbage, lamp.Type);
        Assert.Equal("Garbage", lamp.Name);
        Assert.DoesNotContain(lamp, _db.GetContents(_hall));
        Assert.False(lamp.HasFlag("GOING"));

        var spared = _dispatcher.Dispatch(_owner, $"@undestroy #{lamp.Id.Value}", _db);
        Assert.Contains("That can't be undestroyed.", spared.Notifications);
        Assert.Equal(ObjectType.Garbage, lamp.Type);
    }

    [Fact]
    public void Nuke_CanMarkASafeObjectThatDestroyRefuses()
    {
        var statue = _db.CreateObject("Statue", ObjectType.Thing, _owner);
        statue.AddFlag("SAFE");

        var denied = _dispatcher.Dispatch(_owner, $"@destroy #{statue.Id.Value}", _db);

        Assert.Contains("That object is set SAFE.", denied.Notifications);
        Assert.False(statue.HasFlag("GOING"));

        var marked = _dispatcher.Dispatch(_owner, $"@nuke #{statue.Id.Value}", _db);

        Assert.True(statue.HasFlag("GOING"));
        Assert.Equal(ObjectType.Thing, statue.Type);

        var freed = _dispatcher.Dispatch(_god, $"@nuke #{statue.Id.Value}", _db);

        Assert.Equal(ObjectType.Garbage, statue.Type);
        Assert.Contains($"Statue ({statue.Id}) has been destroyed.", freed.Notifications);
    }

    [Fact]
    public void Purge_FreesEveryGoingObject()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        var book = _db.CreateObject("Book", ObjectType.Thing, _owner);
        var door = _db.CreateObject("Door", ObjectType.Exit, _god);
        _db.MoveObject(lamp.Id, _hall);
        _db.AttachExit(_hall, door.Id);
        lamp.AddFlag("GOING");
        door.AddFlag("GOING");

        var denied = _dispatcher.Dispatch(_owner, "@purge", _db);
        Assert.Contains("Permission denied.", denied.Notifications);
        Assert.Equal(ObjectType.Thing, lamp.Type);

        var purged = _dispatcher.Dispatch(_god, "@purge", _db);

        Assert.Contains("Purged 2 objects.", purged.Notifications);
        Assert.Equal(ObjectType.Garbage, lamp.Type);
        Assert.Equal(ObjectType.Garbage, door.Type);
        Assert.Equal(ObjectType.Thing, book.Type);
        Assert.DoesNotContain(lamp, _db.GetContents(_hall));
        Assert.DoesNotContain(door, _db.GetExits(_hall));

        var empty = _dispatcher.Dispatch(_god, "@purge", _db);
        Assert.Contains("Nothing to purge.", empty.Notifications);
    }

    [Fact]
    public void Nuke_RefusesFundamentalObjects()
    {
        var result = _dispatcher.Dispatch(_god, "@nuke #0", _db);
        Assert.Contains("You cannot destroy fundamental system objects (#0, #1, #2).", result.Notifications);
        Assert.Equal(ObjectType.Room, _db.GetObject(_hall)!.Type);
    }
}
