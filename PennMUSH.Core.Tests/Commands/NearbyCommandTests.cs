namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;
using Xunit;

public class NearbyCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher = new();
    private readonly Dbref _god;
    private readonly Dbref _owner;
    private readonly Dbref _hall;
    private readonly Dbref _master;

    public NearbyCommandTests()
    {
        _hall = _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1)).Id;
        _god = _db.CreateObject("One", ObjectType.Player, new Dbref(1)).Id;
        _owner = _db.CreateObject("Owner", ObjectType.Player, _god).Id;
        _master = _db.CreateObject("Master", ObjectType.Room, _god).Id;
        _db.MoveObject(_owner, _hall);
        _db.MoveObject(_god, _hall);
        _dispatcher.Register(new ScanCommandHandler(_master.Value), "@scan");
        _dispatcher.Register(new SweepCommandHandler(), "@sweep");
        _dispatcher.Register(new WithCommandHandler(_dispatcher, null, _master.Value), "with");
        _dispatcher.Register(new SearchCommandHandler(), "@search");
    }

    [Fact]
    public void Scan_ReportsVisibleListenersInTheChosenSet()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        var secret = _db.CreateObject("Secret", ObjectType.Thing, _god);
        var carried = _db.CreateObject("Widget", ObjectType.Thing, _owner);
        var zone = _db.CreateObject("District", ObjectType.Thing, _god);
        var globe = _db.CreateObject("Globe", ObjectType.Thing, _god);
        _db.MoveObject(lamp.Id, _hall);
        _db.MoveObject(secret.Id, _hall);
        _db.MoveObject(carried.Id, _owner);
        _db.MoveObject(globe.Id, _master);
        var hall = _db.GetObject(_hall)!;
        hall.Zone = zone.Id;
        lamp.SetAttribute("FOO", "$foo *:ping", _owner);
        secret.SetAttribute("HIDDEN", "$foo *:hidden", _god);
        carried.SetAttribute("BAR", "$bar:widget", _owner);
        zone.SetAttribute("ZONE", "$ping:zone", _god);
        zone.AddFlag("VISUAL");
        globe.SetAttribute("GLOBAL", "$ping:global", _god);
        globe.AddFlag("VISUAL");

        var missing = _dispatcher.Dispatch(_owner, "@scan", _db);
        Assert.Contains("What command do you want to scan for?", missing.Notifications);

        var room = _dispatcher.Dispatch(_owner, "@scan foo hi", _db);
        Assert.Contains($"Lamp(#{lamp.Id.Value})  [1: FOO]", room.Notifications);
        Assert.DoesNotContain(room.Notifications, line => line.Contains("Secret"));
        Assert.DoesNotContain(room.Notifications, line => line.Contains("Widget"));

        secret.AddFlag("VISUAL");
        var visible = _dispatcher.Dispatch(_owner, "@scan/room foo hi", _db);
        Assert.Contains($"Secret(#{secret.Id.Value})  [1: HIDDEN]", visible.Notifications);
        Assert.DoesNotContain(visible.Notifications, line => line.Contains("Widget"));

        var self = _dispatcher.Dispatch(_owner, "@scan/self bar", _db);
        Assert.Contains($"Widget(#{carried.Id.Value})  [1: BAR]", self.Notifications);
        Assert.DoesNotContain(self.Notifications, line => line.Contains("Lamp"));

        var zoned = _dispatcher.Dispatch(_owner, "@scan/zone ping", _db);
        Assert.Contains($"Matched zone of location: District(#{zone.Id.Value})  [1: ZONE]", zoned.Notifications);

        var globals = _dispatcher.Dispatch(_owner, "@scan/globals ping", _db);
        Assert.Contains($"Globe(#{globe.Id.Value})  [1: GLOBAL]", globals.Notifications);
    }

    [Fact]
    public void Sweep_ReportsListenersPuppetsAndConnectedPlayers()
    {
        var guest = _db.CreateObject("Guest", ObjectType.Player, _god);
        var puppet = _db.CreateObject("Puppet", ObjectType.Thing, guest.Id);
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        var bag = _db.CreateObject("Bag", ObjectType.Thing, _owner);
        var door = _db.CreateObject("Door", ObjectType.Exit, _god);
        _db.MoveObject(guest.Id, _hall);
        _db.MoveObject(puppet.Id, _hall);
        _db.MoveObject(lamp.Id, _hall);
        _db.MoveObject(bag.Id, _owner);
        _db.AttachExit(_hall, door.Id);
        puppet.AddFlag("PUPPET");
        lamp.SetAttribute("FOO", "$hi:ping", _owner);
        bag.SetAttribute("FOO", "$hi:bag", _owner);
        var hall = _db.GetObject(_hall)!;
        hall.AddFlag("AUDIBLE");
        door.AddFlag("AUDIBLE");
        _db.SetConnected(guest.Id, true);

        var sweep = _dispatcher.Dispatch(_owner, "@sweep", _db);
        Assert.Contains("Listening in ROOM:", sweep.Notifications);
        Assert.Contains("Guest [speech]. (connected)", sweep.Notifications);
        Assert.Contains("Puppet [speech].", sweep.Notifications);
        Assert.Contains("Lamp [commands].", sweep.Notifications);
        Assert.Contains("Listening EXITS:", sweep.Notifications);
        Assert.Contains("Door [broadcasting].", sweep.Notifications);
        Assert.Contains("Listening in your INVENTORY:", sweep.Notifications);
        Assert.Contains("Bag [commands].", sweep.Notifications);

        var inventory = _dispatcher.Dispatch(_owner, "@sweep/inventory", _db);
        Assert.Contains("Bag [commands].", inventory.Notifications);
        Assert.DoesNotContain(inventory.Notifications, line => line.Contains("Listening in ROOM:"));

        var connected = _dispatcher.Dispatch(_owner, "@sweep connected", _db);
        Assert.Contains("Guest is listening.", connected.Notifications);
        Assert.Contains("Puppet [owner: Guest] is listening.", connected.Notifications);
        Assert.DoesNotContain(connected.Notifications, line => line.Contains("Lamp"));
        Assert.DoesNotContain(connected.Notifications, line => line.Contains("Listening EXITS:"));
    }

    [Fact]
    public void With_RunsANearbyListenerAndARoomOfListeners()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        var globe = _db.CreateObject("Globe", ObjectType.Thing, _god);
        var far = _db.CreateObject("Far", ObjectType.Room, _god);
        var distant = _db.CreateObject("Distant", ObjectType.Thing, _god);
        _db.MoveObject(lamp.Id, _hall);
        _db.MoveObject(globe.Id, _master);
        _db.MoveObject(distant.Id, far.Id);
        lamp.SetAttribute("HI", "$hi:Hello there", _owner);
        globe.SetAttribute("HI", "$hi:From the master room", _god);
        distant.SetAttribute("HI", "$hi:Far away", _god);

        var spoken = _dispatcher.Dispatch(_owner, "with lamp=hi", _db);
        Assert.Contains("Hello there", spoken.Notifications);

        var denied = _dispatcher.Dispatch(_owner, $"with #{distant.Id.Value}=hi", _db);
        Assert.Contains("I don't see that here.", denied.Notifications);

        var room = _dispatcher.Dispatch(_owner, $"with/room #{_master.Value}=hi", _db);
        Assert.Contains("From the master room", room.Notifications);

        var none = _dispatcher.Dispatch(_owner, "with lamp=nope", _db);
        Assert.Contains("No matching command.", none.Notifications);
    }

    [Fact]
    public void Search_FiltersTheDatabaseByOwnerTypeNameAndFlags()
    {
        var study = _db.CreateObject("Study", ObjectType.Room, _owner);
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        var statue = _db.CreateObject("Statue", ObjectType.Thing, _god);
        lamp.AddFlag("VISUAL");

        var mine = _dispatcher.Dispatch(_owner, "@search type=thing", _db);
        Assert.Contains($"Lamp (#{lamp.Id.Value})", mine.Notifications);
        Assert.DoesNotContain(mine.Notifications, line => line.Contains("Statue"));

        var all = _dispatcher.Dispatch(_owner, "@search all type=thing", _db);
        Assert.Contains($"Lamp (#{lamp.Id.Value})", all.Notifications);
        Assert.DoesNotContain(all.Notifications, line => line.Contains("Statue"));

        var wizard = _dispatcher.Dispatch(_god, "@search all type=thing", _db);
        Assert.Contains($"Statue (#{statue.Id.Value})", wizard.Notifications);

        var players = _dispatcher.Dispatch(_owner, "@search all type=player", _db);
        Assert.Contains("Owner (#2)", players.Notifications);
        Assert.Contains("One (#1)", players.Notifications);

        var named = _dispatcher.Dispatch(_owner, "@search name=La", _db);
        Assert.Contains($"Lamp (#{lamp.Id.Value})", named.Notifications);
        Assert.DoesNotContain(named.Notifications, line => line.Contains("Study"));

        var flagged = _dispatcher.Dispatch(_owner, "@search lflags=VISUAL", _db);
        Assert.Contains($"Lamp (#{lamp.Id.Value})", flagged.Notifications);

        var rooms = _dispatcher.Dispatch(_owner, "@search type=room", _db);
        Assert.Contains($"Study (#{study.Id.Value})", rooms.Notifications);
        Assert.DoesNotContain(rooms.Notifications, line => line.Contains("Limbo"));

        var unavailable = _dispatcher.Dispatch(_owner, "@search eval=1", _db);
        Assert.Contains("That search is not available.", unavailable.Notifications);
    }
}
