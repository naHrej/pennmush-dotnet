namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Attributes;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;
using Xunit;

public class ObjectReadWriteCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher = new();
    private readonly ActionQueue _queue = new();
    private readonly Dbref _god;
    private readonly Dbref _owner;
    private int _saves;

    public ObjectReadWriteCommandTests()
    {
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        _god = _db.CreateObject("One", ObjectType.Player, new Dbref(1)).Id;
        _owner = _db.CreateObject("Owner", ObjectType.Player, _god).Id;
        _dispatcher.Register(new CopyAttributeCommandHandler(), "@cpattr", "@mvattr");
        _dispatcher.Register(new EditAttributeCommandHandler(), "@edit");
        _dispatcher.Register(new GrepCommandHandler(), "@grep");
        _dispatcher.Register(new AttributeLockCommandHandler(), "@atrlock");
        _dispatcher.Register(new AttributeChownCommandHandler(), "@atrchown");
        _dispatcher.Register(new LockFlagCommandHandler(), "@lset");
        _dispatcher.Register(new DecompileCommandHandler(), "@decompile");
        _dispatcher.Register(new MonikerCommandHandler(), "@moniker");
        _dispatcher.Register(new MessageCommandHandler(), "@message");
        _dispatcher.Register(new FindCommandHandler(), "@find");
        _dispatcher.Register(new WhereIsCommandHandler(), "@whereis");
        _dispatcher.Register(new EntrancesCommandHandler(), "@entrances");
        _dispatcher.Register(new FirstExitCommandHandler(), "@firstexit");
        _dispatcher.Register(new UptimeCommandHandler(), "@uptime");
        _dispatcher.Register(new PoorCommandHandler(), "@poor");
        _dispatcher.Register(new ListCommandHandler(_dispatcher), "@list");
        _dispatcher.Register(new DumpCommandHandler(() => _saves++), "@dump");
        _dispatcher.Register(new SetCommandHandler(), "@set");
        _dispatcher.Register(new LockCommandHandler(), "@lock");
        _dispatcher.Register(new ExamineCommandHandler(), "examine", "brief");
        _dispatcher.Register(new PemitCommandHandler(), "@pemit", "@prompt");
        _dispatcher.Register(new EmitCommandHandler(), "@emit", "@lemit");
        _dispatcher.Register(new WaitCommandHandler(_queue), "@wait", "@notify", "@drain");
    }

    [Fact]
    public void CopyAndMove_TransferTheAttributeAndItsFlags()
    {
        var source = _db.CreateObject("Source", ObjectType.Thing, _owner);
        var dest = _db.CreateObject("Dest", ObjectType.Thing, _owner);
        source.SetAttribute("DESC", "bright", _owner, AttributeFlags.Visual);

        var copied = _dispatcher.Dispatch(_owner, $"@cpattr #{source.Id.Value}/DESC=#{dest.Id.Value}/LOOK", _db);
        var moved = _dispatcher.Dispatch(_owner, $"@mvattr #{source.Id.Value}/DESC=#{dest.Id.Value}", _db);

        Assert.Equal("Copied.", copied.Notifications.Single());
        Assert.Equal("bright", dest.GetAttribute("LOOK")!.Value);
        Assert.Equal(AttributeFlags.Visual, dest.GetAttribute("LOOK")!.Flags);
        Assert.Equal("Moved.", moved.Notifications.Single());
        Assert.Null(source.GetAttribute("DESC"));
        Assert.Equal("bright", dest.GetAttribute("DESC")!.Value);
    }

    [Fact]
    public void EditAndGrep_ChangeAndFindAttributeText()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        lamp.SetAttribute("DESC", "a bright lamp", _owner);

        var edited = _dispatcher.Dispatch(_owner, $"@edit #{lamp.Id.Value}/DESC=bright,dim", _db);
        var found = _dispatcher.Dispatch(_owner, $"@grep #{lamp.Id.Value}=dim", _db);

        Assert.Equal("Edited.", edited.Notifications.Single());
        Assert.Equal("a dim lamp", lamp.GetAttribute("DESC")!.Value);
        Assert.Contains("DESC: a dim lamp", found.Notifications.Single());
    }

    [Fact]
    public void AttributeLock_BlocksLaterSetsUntilItIsCleared()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        lamp.SetAttribute("DESC", "fixed", _owner);

        _dispatcher.Dispatch(_owner, $"@atrlock #{lamp.Id.Value}/DESC=on", _db);
        var denied = _dispatcher.Dispatch(_owner, $"@set #{lamp.Id.Value}=DESC:changed", _db);

        Assert.Contains("Locked", denied.Notifications.Single());
        Assert.Equal("fixed", lamp.GetAttribute("DESC")!.Value);

        _dispatcher.Dispatch(_owner, $"@atrlock #{lamp.Id.Value}/DESC=off", _db);
        var allowed = _dispatcher.Dispatch(_owner, $"@set #{lamp.Id.Value}=DESC:changed", _db);

        Assert.Equal("Set.", allowed.Notifications.Single());
        Assert.Equal("changed", lamp.GetAttribute("DESC")!.Value);
    }

    [Fact]
    public void AttributeChownAndLockFlags_UpdateTheStoredOwnerAndFlags()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        var heir = _db.CreateObject("Heir", ObjectType.Player, _owner);
        lamp.SetAttribute("DESC", "lamp", _owner);
        lamp.SetLock(LockType.Basic, new ConstantLockNode(true), _owner);

        _dispatcher.Dispatch(_owner, $"@atrchown #{lamp.Id.Value}/DESC=#{heir.Id.Value}", _db);
        var listed = _dispatcher.Dispatch(_owner, $"@lset #{lamp.Id.Value}/Basic=visual", _db);

        Assert.Equal(heir.Id, lamp.GetAttribute("DESC")!.Creator);
        Assert.True(lamp.GetLock(LockType.Basic)!.Flags.HasFlag(LockFlags.Visual));
        Assert.Contains("Visual", listed.Notifications.Single());
    }

    [Fact]
    public void DecompileMonikerAndMessage_ReportStoredText()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        lamp.SetAttribute("DESC", "glass", _owner);
        var player = _db.GetObject(_owner)!;

        var code = _dispatcher.Dispatch(_owner, $"@decompile #{lamp.Id.Value}", _db);
        _dispatcher.Dispatch(_owner, $"@moniker #{lamp.Id.Value}=Glass Lamp", _db);
        var sent = _dispatcher.Dispatch(_owner, $"@message #{player.Id.Value}=#{lamp.Id.Value}/DESC", _db);

        Assert.Contains($"@set {lamp.Id}=DESC:glass", code.Notifications.Single());
        Assert.Equal("Glass Lamp", lamp.GetAttribute("MONIKER")!.Value);
        Assert.Equal("glass", sent.TargetedNotifications.Single(n => n.Scope == NotificationScope.Target).Message);
    }

    [Fact]
    public void Prompt_MarksTheTargetNotification()
    {
        var player = _db.GetObject(_owner)!;
        var result = _dispatcher.Dispatch(_owner, $"@prompt #{player.Id.Value}=Ready?", _db);

        var prompt = result.TargetedNotifications.Single(n => n.Scope == NotificationScope.Target);
        Assert.True(prompt.Prompt);
        Assert.Equal("[Owner:] Ready?", prompt.Message);
    }

    [Fact]
    public void Lemit_SendsTheTextToThePlayersLocation()
    {
        var room = _db.CreateObject("Hall", ObjectType.Room, _owner);
        _db.MoveObject(_owner, room.Id);

        var result = _dispatcher.Dispatch(_owner, "@lemit The lamps flicker.", _db);

        Assert.Equal("[Owner:] The lamps flicker.", result.Notifications.Single());
        Assert.Equal(NotificationScope.Room, result.TargetedNotifications.Single().Scope);
    }

    [Fact]
    public void FindAndWhereIs_ReportControlledObjectsAndLocation()
    {
        var room = _db.CreateObject("Hall", ObjectType.Room, _owner);
        var lamp = _db.CreateObject("Brass Lamp", ObjectType.Thing, _owner);
        var other = _db.CreateObject("Other", ObjectType.Player, _owner);
        _db.MoveObject(other.Id, room.Id);

        var found = _dispatcher.Dispatch(_owner, "@find brass", _db);
        var where = _dispatcher.Dispatch(_owner, $"@whereis #{other.Id.Value}", _db);

        Assert.Contains($"Brass Lamp ({lamp.Id})", found.Notifications.Single());
        Assert.Contains("Hall", where.Notifications.Single());
        Assert.Contains("looking for you", where.TargetedNotifications.Single(n => n.Target == other.Id).Message);
    }

    [Fact]
    public void Brief_OmitsTheAttributeDump()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _owner);
        lamp.SetAttribute("DESC", "secret text", _owner);

        var full = _dispatcher.Dispatch(_owner, $"examine #{lamp.Id.Value}", _db);
        var brief = _dispatcher.Dispatch(_owner, $"brief #{lamp.Id.Value}", _db);

        Assert.Contains("secret text", full.Notifications.Single());
        Assert.DoesNotContain("secret text", brief.Notifications.Single());
        Assert.Contains("Lamp", brief.Notifications.Single());
    }

    [Fact]
    public void Drain_RemovesSemaphoreWaiters()
    {
        _dispatcher.Dispatch(_owner, "@wait gate=say go", _db);
        Assert.Equal(1, _queue.Count);

        var drained = _dispatcher.Dispatch(_owner, "@drain gate", _db);

        Assert.Equal(0, _queue.Count);
        Assert.Contains("Drained 1", drained.Notifications.Single());
    }

    [Fact]
    public void DumpUptimePoorAndList_UseExistingServerState()
    {
        var mortal = _db.CreateObject("Mortal", ObjectType.Player, _owner);
        mortal.Pennies = 40;

        var dumped = _dispatcher.Dispatch(_god, "@dump", _db);
        var up = _dispatcher.Dispatch(mortal.Id, "@uptime", _db);
        var denied = _dispatcher.Dispatch(mortal.Id, "@poor 3", _db);
        var poor = _dispatcher.Dispatch(_god, "@poor 3", _db);
        var commands = _dispatcher.Dispatch(_god, "@list commands", _db);

        Assert.Equal(1, _saves);
        Assert.Equal("Database saved.", dumped.Notifications.Single());
        Assert.StartsWith("Up since", up.Notifications.Single());
        Assert.Equal("Permission denied.", denied.Notifications.Single());
        Assert.Equal(3, mortal.Pennies);
        Assert.Contains("Set", poor.Notifications.Single());
        Assert.Contains("@dump", commands.Notifications.Single());
    }

    [Fact]
    public void EntrancesAndFirstExit_FollowTheExitChain()
    {
        var room = _db.CreateObject("Hall", ObjectType.Room, _owner);
        var north = _db.CreateObject("North", ObjectType.Exit, _owner);
        var south = _db.CreateObject("South", ObjectType.Exit, _owner);
        var guest = _db.CreateObject("Guest", ObjectType.Thing, _owner);
        guest.Home = room.Id;
        _db.AttachExit(room.Id, north.Id);
        _db.AttachExit(room.Id, south.Id);
        _db.LinkExit(north.Id, room.Id);

        var entrances = _dispatcher.Dispatch(_owner, $"@entrances #{room.Id.Value}", _db);
        _dispatcher.Dispatch(_owner, $"@firstexit #{north.Id.Value}", _db);

        Assert.Contains("North", entrances.Notifications.Single());
        Assert.Contains("Guest", entrances.Notifications.Single());
        Assert.Equal(north.Id, room.Exits);
    }
}
