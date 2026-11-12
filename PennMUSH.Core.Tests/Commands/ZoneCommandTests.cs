namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Types;
using Xunit;

public class ZoneCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher = new();
    private readonly Dbref _god;
    private readonly Dbref _owner;
    private readonly Dbref _hall;

    public ZoneCommandTests()
    {
        _hall = _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1)).Id;
        _god = _db.CreateObject("One", ObjectType.Player, new Dbref(1)).Id;
        _owner = _db.CreateObject("Owner", ObjectType.Player, _god).Id;
        _db.MoveObject(_owner, _hall);
        _db.MoveObject(_god, _hall);
        _dispatcher.Register(new ChzoneCommandHandler(), "@chzone");
        _dispatcher.Register(new ChzoneAllCommandHandler(), "@chzoneall");
        _dispatcher.Register(new ZemitCommandHandler(), "@zemit", "@nszemit");
    }

    [Fact]
    public void Chzone_RequiresTheChzoneLockAndStripsPrivileges()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        var zone = _db.CreateObject("District", ObjectType.Thing, _god);
        zone.SetLock(LockType.Chzone, new ConstantLockNode(false), _god);
        lamp.AddFlag("WIZARD");
        lamp.AddPower("Can_spoof");

        var denied = _dispatcher.Dispatch(_owner, $"@chzone #{lamp.Id.Value}=#{zone.Id.Value}", _db);

        Assert.Contains("Permission denied.", denied.Notifications);
        Assert.False(lamp.InZone(zone.Id));
        Assert.Null(zone.GetLock(LockType.Zone));

        zone.ClearLock(LockType.Chzone);
        var moved = _dispatcher.Dispatch(_owner, $"@chzone #{lamp.Id.Value}=#{zone.Id.Value}", _db);

        Assert.Contains("Zone set.", moved.Notifications);
        Assert.Equal(zone.Id, lamp.Zone);
        Assert.False(lamp.HasFlag("WIZARD"));
        Assert.False(lamp.HasPower("Can_spoof"));
        Assert.NotNull(zone.GetLock(LockType.Zone));

        lamp.AddFlag("WIZARD");
        lamp.AddPower("Can_spoof");
        var kept = _dispatcher.Dispatch(_god, $"@chzone/preserve #{lamp.Id.Value}=#{zone.Id.Value}", _db);
        Assert.Contains("Zone set.", kept.Notifications);
        Assert.True(lamp.HasFlag("WIZARD"));
        Assert.True(lamp.HasPower("Can_spoof"));

        var loop = _dispatcher.Dispatch(_god, $"@chzone #{zone.Id.Value}=#{lamp.Id.Value}", _db);
        Assert.Contains("That would cause a zone loop.", loop.Notifications);

        var cleared = _dispatcher.Dispatch(_owner, $"@chzone #{lamp.Id.Value}=none", _db);
        Assert.Contains("Zone cleared.", cleared.Notifications);
        Assert.False(lamp.Zone.IsValid);
    }

    [Fact]
    public void ChzoneAll_UpdatesEveryObjectAPlayerOwns()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        var book = _db.CreateObject("Book", ObjectType.Thing, _owner);
        var other = _db.CreateObject("Statue", ObjectType.Thing, _god);
        var zone = _db.CreateObject("District", ObjectType.Thing, _god);

        var denied = _dispatcher.Dispatch(_owner, $"@chzoneall #{_owner.Value}=#{zone.Id.Value}", _db);
        Assert.Contains("Permission denied.", denied.Notifications);
        Assert.False(lamp.InZone(zone.Id));

        var updated = _dispatcher.Dispatch(_god, $"@chzoneall #{_owner.Value}=#{zone.Id.Value}", _db);
        Assert.Contains("Zone updated on 2 objects.", updated.Notifications);
        Assert.Equal(zone.Id, lamp.Zone);
        Assert.Equal(zone.Id, book.Zone);
        Assert.False(other.InZone(zone.Id));
    }

    [Fact]
    public void Zemit_ReachesRoomsInTheZoneAndKeepsTheSpeakerName()
    {
        var zone = _db.CreateObject("District", ObjectType.Thing, _god);
        var inside = _db.CreateObject("Market", ObjectType.Room, _god);
        var outside = _db.CreateObject("Field", ObjectType.Room, _god);
        inside.Zone = zone.Id;

        var denied = _dispatcher.Dispatch(_owner, $"@zemit #{zone.Id.Value}=Hello", _db);
        Assert.Contains("Permission denied.", denied.Notifications);

        var sent = _dispatcher.Dispatch(_god, $"@zemit #{zone.Id.Value}=Hello", _db);
        Assert.Contains("[One:] Hello", sent.TargetedNotifications.Where(note => note.Scope == NotificationScope.Room && note.Target == inside.Id).Select(note => note.Message));
        Assert.DoesNotContain(sent.TargetedNotifications, note => note.Scope == NotificationScope.Room && note.Target == outside.Id);
        Assert.Contains("You zemit to District.", sent.Notifications);

        _db.GetObject(_god)!.AddPower("Can_spoof");
        var plain = _dispatcher.Dispatch(_god, $"@zemit #{zone.Id.Value}=Hello", _db);
        var forced = _dispatcher.Dispatch(_god, $"@nszemit #{zone.Id.Value}=Hello", _db);
        Assert.Contains("Hello", plain.TargetedNotifications.Where(note => note.Target == inside.Id).Select(note => note.Message));
        Assert.Contains("[One:] Hello", forced.TargetedNotifications.Where(note => note.Target == inside.Id).Select(note => note.Message));
    }
}
