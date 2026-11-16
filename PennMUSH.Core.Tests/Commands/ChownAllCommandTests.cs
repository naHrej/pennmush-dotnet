namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;
using Xunit;

public class ChownAllCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _ada;
    private readonly Dbref _bea;

    public ChownAllCommandTests()
    {
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.Owner = god.Id;
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        ada.Owner = ada.Id;
        ada.AddFlag("WIZARD");
        _ada = ada.Id;
        var bea = _db.CreateObject("Bea", ObjectType.Player, new Dbref(1));
        bea.Owner = bea.Id;
        _bea = bea.Id;
        _dispatcher = new CommandDispatcher();
        _dispatcher.Register(new ChownAllCommandHandler(), "@chownall");
    }

    [Fact]
    public void ChownAll_MovesObjectsAndQuota()
    {
        var denied = _dispatcher.Dispatch(_bea, "@chownall Ada=Bea", _db);
        Assert.Contains("Try asking them first!", denied.Notifications);

        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _ada);
        var hall = _db.CreateObject("Hall", ObjectType.Room, _ada);
        lamp.AddFlag("WIZARD");
        lamp.AddFlag("GOING");

        var moved = _dispatcher.Dispatch(_ada, "@chownall Ada=Bea", _db);
        Assert.Contains("Ownership changed for 2 objects.", moved.Notifications);
        Assert.Equal(_bea, lamp.Owner);
        Assert.Equal(_bea, hall.Owner);
        Assert.False(lamp.HasFlag("WIZARD"));
        Assert.False(lamp.HasFlag("GOING"));
        Assert.True(lamp.HasFlag("HALT"));
        Assert.Equal("22", _db.GetObject(_ada)!.GetAttribute("RQUOTA")!.Value);
        Assert.Equal("16", _db.GetObject(_bea)!.GetAttribute("RQUOTA")!.Value);

        var missing = _dispatcher.Dispatch(_ada, "@chownall Nobody=Bea", _db);
        Assert.Contains("I can't see that here.", missing.Notifications);
    }

    [Fact]
    public void Switches_NarrowTheTypesAndPreserveCanKeepPrivileges()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _ada);
        var hall = _db.CreateObject("Hall", ObjectType.Room, _ada);
        lamp.AddFlag("WIZARD");
        lamp.AddPower("SEE_ALL");

        var things = _dispatcher.Dispatch(_ada, "@chownall/things Ada=*Bea", _db);
        Assert.Contains("Ownership changed for 1 objects.", things.Notifications);
        Assert.Equal(_bea, lamp.Owner);
        Assert.Equal(_ada, hall.Owner);

        var kept = _db.CreateObject("Wand", ObjectType.Thing, _ada);
        kept.AddFlag("WIZARD");
        var preserve = _dispatcher.Dispatch(_ada, "@chownall/preserve me", _db);
        Assert.Contains("Ownership changed for 2 objects.", preserve.Notifications);
        Assert.Equal(_ada, kept.Owner);
        Assert.Equal(_ada, hall.Owner);
        Assert.True(kept.HasFlag("WIZARD"));
        Assert.Contains($"Warning: @CHOWN/PRESERVE on an object (#{kept.Id.Value}) with WIZ, ROY, INHERIT, or @power privileges.", preserve.Notifications);

        var given = _dispatcher.Dispatch(_ada, "@chownall/preserve Ada=Bea", _db);
        Assert.Contains($"Warning: WIZ flag reset on #{kept.Id.Value} because @CHOWN/PRESERVE is to a non-WIZ flagged third-party.", given.Notifications);
        Assert.False(kept.HasFlag("WIZARD"));
        Assert.Equal(_bea, kept.Owner);
    }
}
