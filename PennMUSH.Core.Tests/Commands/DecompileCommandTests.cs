namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Attributes;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Types;
using Xunit;

public class DecompileCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher = new();
    private readonly Dbref _owner;

    public DecompileCommandTests()
    {
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.AddFlag("WIZARD");
        _owner = _db.CreateObject("Ada", ObjectType.Player, god.Id).Id;
        _dispatcher.Register(new DecompileCommandHandler(), "@decompile");
    }

    [Fact]
    public void EmptyRequest_AsksWhatToDecompile()
    {
        var result = _dispatcher.Dispatch(_owner, "@decompile", _db);

        Assert.Equal("What do you want to @decompile?", result.Notifications.Single());
    }

    [Fact]
    public void Thing_PrintsCreateLinkFlagsAndAttributes()
    {
        var room = _db.CreateObject("Hall", ObjectType.Room, _owner);
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        lamp.Home = room.Id;
        lamp.Parent = room.Id;
        lamp.Zone = room.Id;
        lamp.AddFlag("SAFE");
        lamp.AddPower("Builder");
        lamp.SetAttribute("DESC", "glass", _owner);
        lamp.SetAttribute("NOTE", " line\nnext", _owner, AttributeFlags.Visual);
        lamp.SetLock(LockType.Basic, new DbrefLockNode(_owner), _owner, LockFlags.Private);

        var lines = _dispatcher.Dispatch(_owner, " @decompile Lamp", _db).Notifications;

        Assert.Contains($"@@ Lamp ({lamp.Id})", lines);
        Assert.Contains("@create Lamp", lines);
        Assert.Contains($"@link Lamp = {room.Id}", lines);
        Assert.Contains($"@chzone Lamp = {room.Id}", lines);
        Assert.Contains($"@parent Lamp={room.Id}", lines);
        Assert.Contains("@lock/Basic Lamp=me", lines);
        Assert.Contains("@lset Lamp/Basic=no_inherit", lines);
        Assert.Contains("@set Lamp = SAFE", lines);
        Assert.Contains("@power Lamp = Builder", lines);
        Assert.Contains("&DESC Lamp=glass", lines);
        Assert.Contains("@set Lamp=NOTE:%bline%rnext", lines);
        Assert.Contains("@set Lamp/NOTE=visual", lines);
    }

    [Fact]
    public void DbAndName_ChooseTheObjectWord()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        lamp.SetAttribute("DESC", "glass", _owner);
        var byDb = _dispatcher.Dispatch(_owner, $"@decompile/db #{lamp.Id.Value}", _db).Notifications;
        var byName = _dispatcher.Dispatch(_owner, $"@decompile/db/name #{lamp.Id.Value}", _db).Notifications;
        var me = _dispatcher.Dispatch(_owner, "@decompile me", _db).Notifications;

        Assert.Contains($"&DESC {lamp.Id}=glass", byDb);
        Assert.Contains("@create Lamp", byDb);
        Assert.Contains("&DESC Lamp=glass", byName);
        Assert.DoesNotContain(byName, line => line.StartsWith("&DESC #"));
        Assert.Contains($"@@ Ada ({_owner})", me);
        Assert.Contains("@@ No attributes match '**'. @@", me);
    }

    [Fact]
    public void PrefixAndTinyFugue_LeadEveryLine()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        lamp.SetAttribute("DESC", "glass", _owner, AttributeFlags.Visual);
        var ada = _db.GetObject(_owner)!;
        ada.SetAttribute("TFPREFIX", "grab ", _owner);

        var prefixed = _dispatcher.Dispatch(_owner, "@decompile/prefix Lamp=>> ", _db).Notifications;
        var fugue = _dispatcher.Dispatch(_owner, "@decompile/tf Lamp", _db).Notifications;

        Assert.All(prefixed, line => Assert.StartsWith(">> ", line));
        Assert.Contains(">> @create Lamp", prefixed);
        Assert.All(fugue, line => Assert.StartsWith("grab ", line));
        Assert.Contains($"grab &DESC {lamp.Id}=glass", fugue);
        Assert.DoesNotContain(fugue, line => line.Contains("@set "));
    }

    [Fact]
    public void FlagsAttribsAndSkipDefaults_SelectTheSections()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        lamp.AddFlag("SAFE");
        lamp.SetAttribute("DESC", "glass", _owner, StandardAttributes.FlagsOf("DESC"));
        lamp.SetAttribute("NOTE", "kept", _owner, AttributeFlags.Visual);

        var flags = _dispatcher.Dispatch(_owner, "@decompile/flags Lamp", _db).Notifications;
        var attribs = _dispatcher.Dispatch(_owner, "@decompile/attribs Lamp", _db).Notifications;
        var skipped = _dispatcher.Dispatch(_owner, "@decompile/skipdefaults Lamp", _db).Notifications;
        var one = _dispatcher.Dispatch(_owner, "@decompile Lamp/NOTE", _db).Notifications;

        Assert.Contains("@create Lamp", flags);
        Assert.Contains("@set Lamp = SAFE", flags);
        Assert.DoesNotContain(flags, line => line.StartsWith("&") || line.Contains("=DESC") || line.Contains("=NOTE"));
        Assert.Contains("&DESC Lamp=glass", attribs);
        Assert.DoesNotContain(attribs, line => line.StartsWith("@create") || line.StartsWith("@@"));
        Assert.DoesNotContain(skipped, line => line.Contains("/DESC="));
        Assert.Contains("@set Lamp/NOTE=visual", skipped);
        Assert.Equal(new[] { "&NOTE Lamp=kept", "@set Lamp/NOTE=visual" }, one);
    }

    [Fact]
    public void MissingPattern_ReportsThatNothingMatched()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);

        var result = _dispatcher.Dispatch(_owner, "@decompile Lamp/MISSING", _db);

        Assert.Equal("@@ No attributes match 'MISSING'. @@", result.Notifications.Single());
    }

    [Fact]
    public void Exit_NamesItsSourceAndUsesTheShortName()
    {
        var hall = _db.CreateObject("Hall", ObjectType.Room, _owner);
        var exit = _db.CreateObject("North;n", ObjectType.Exit, _owner);
        _db.AttachExit(hall.Id, exit.Id);
        _db.LinkExit(exit.Id, hall.Id);

        var lines = _dispatcher.Dispatch(_owner, $"@decompile #{exit.Id.Value}", _db).Notifications;

        Assert.Contains($"@@ North ({exit.Id}), in Hall ({hall.Id})", lines);
        Assert.Contains("@open North;n", lines);
        Assert.Contains($"@link North = {hall.Id}", lines);
    }

    [Fact]
    public void Stranger_IsRefusedTheFullListing()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        var stranger = _db.CreateObject("Bea", ObjectType.Player, _owner).Id;

        var result = _dispatcher.Dispatch(stranger, $"@decompile #{lamp.Id.Value}", _db);

        Assert.Equal("Permission denied.", result.Notifications.Single());
    }

    [Fact]
    public void NoDumpAndInternalAttributes_StayOut()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        lamp.SetAttribute("QUEUE", "1", _owner, AttributeFlags.NoDump);
        lamp.SetAttribute("XYXXY", "secret", _owner, AttributeFlags.Internal);

        var lines = _dispatcher.Dispatch(_owner, "@decompile/attribs Lamp", _db).Notifications;

        Assert.Equal("@@ No attributes match '**'. @@", lines.Single());
    }
}
