namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Attributes;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class CommandPriorityTests
{
    private readonly ObjectDatabase _db = new();
    private readonly ExpressionEvaluator _evaluator = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _room0;
    private readonly Dbref _masterRoom;
    private readonly Dbref _player;

    public CommandPriorityTests()
    {
        var r0 = _db.CreateObject("Room Zero", ObjectType.Room, Dbref.Nothing); // #0
        _room0 = r0.Id;
        var p = _db.CreateObject("Player", ObjectType.Player, Dbref.Nothing);   // #1
        _player = p.Id;
        p.Owner = _player;
        _db.MoveObject(_player, _room0);

        var mr = _db.CreateObject("Master Room", ObjectType.Room, _player);     // #2
        _masterRoom = mr.Id;

        _dispatcher = new CommandDispatcher(_evaluator);
        _dispatcher.Register(new CreateCommandHandler());
        _dispatcher.Register(new SetCommandHandler());
        _dispatcher.Register(new GoCommandHandler(), "go");
        _dispatcher.Register(new PemitCommandHandler(), "@pemit");
        _dispatcher.Register(new ChatCommandHandler(new ChannelRegistry(), _evaluator), "@chat");
    }

    [Fact]
    public void Create_SetsNoCommandFlagByDefault()
    {
        var ctx = _dispatcher.Dispatch(_player, "@create Widget", _db);
        var widget = _db.GetAllObjects().First(o => o.Name == "Widget");
        Assert.True(widget.HasFlag("NO_COMMAND"));
    }

    [Fact]
    public void SetAttributeFlag_Regexp_RebuildsListenersAndMatches()
    {
        var widget = _db.CreateObject("RegexWidget", ObjectType.Thing, _player);
        _db.MoveObject(widget.Id, _room0);
        widget.SetAttribute("CMD_RE", @"$^\+re (\d+)$:@pemit %#=num:%1", _player);

        // Before regexp flag, +re 42 should NOT match
        var before = _dispatcher.Dispatch(_player, "+re 42", _db);
        Assert.Contains("Huh?  (Type \"help\" for help.)", before.Notifications);

        var setCtx = _dispatcher.Dispatch(_player, "@set RegexWidget/CMD_RE=regexp", _db);
        Assert.Contains("Set.", setCtx.Notifications);
        Assert.True(widget.GetAttribute("CMD_RE")!.Flags.HasFlag(AttributeFlags.Regexp));

        var after = _dispatcher.Dispatch(_player, "+re 42", _db);
        Assert.Contains(after.TargetedNotifications, n => n.Target == _player && n.Message == "num:42");
    }

    [Fact]
    public void PlusWord_WithoutMatchingChannel_FallsThroughToHuh()
    {
        var ctx = _dispatcher.Dispatch(_player, "+nonexistent hello", _db);
        Assert.Contains("Huh?  (Type \"help\" for help.)", ctx.Notifications);
        Assert.DoesNotContain(ctx.Notifications, n => n.Contains("CHAT: No such channel"));
    }

    [Fact]
    public void CommandPriority_LocalTierBreaksBeforeZoneAndMasterRoom()
    {
        var localObj = _db.CreateObject("LocalObj", ObjectType.Thing, _player);
        _db.MoveObject(localObj.Id, _room0);
        localObj.SetAttribute("CMD1", "$+test:@pemit %#=local1", _player);

        var localObj2 = _db.CreateObject("LocalObj2", ObjectType.Thing, _player);
        _db.MoveObject(localObj2.Id, _room0);
        localObj2.SetAttribute("CMD2", "$+test:@pemit %#=local2", _player);

        var zoneObj = _db.CreateObject("ZoneObj", ObjectType.Thing, _player);
        _db.GetObject(_room0)!.Zone = zoneObj.Id;
        zoneObj.SetAttribute("CMDZ", "$+test:@pemit %#=zone", _player);
        zoneObj.SetAttribute("CMDZ_ONLY", "$+zonly:@pemit %#=zone-only", _player);

        var globalObj = _db.CreateObject("GlobalObj", ObjectType.Thing, _player);
        _db.MoveObject(globalObj.Id, _masterRoom);
        globalObj.SetAttribute("CMDG", "$+test:@pemit %#=global", _player);
        globalObj.SetAttribute("CMDG_ONLY", "$+gonly:@pemit %#=global-only", _player);

        // +test matches both local objects and breaks before zone/global
        var localRes = _dispatcher.Dispatch(_player, "+test", _db);
        Assert.Contains(localRes.TargetedNotifications, n => n.Target == _player && n.Message == "local1");
        Assert.Contains(localRes.TargetedNotifications, n => n.Target == _player && n.Message == "local2");
        Assert.DoesNotContain(localRes.TargetedNotifications, n => n.Message == "zone");
        Assert.DoesNotContain(localRes.TargetedNotifications, n => n.Message == "global");

        // +zonly skips local (no match), matches zone, and breaks before global
        var zoneRes = _dispatcher.Dispatch(_player, "+zonly", _db);
        Assert.Contains(zoneRes.TargetedNotifications, n => n.Target == _player && n.Message == "zone-only");
        Assert.DoesNotContain(zoneRes.TargetedNotifications, n => n.Message == "global-only");

        // +gonly falls through local and zone and matches Master Room #2
        var globalRes = _dispatcher.Dispatch(_player, "+gonly", _db);
        Assert.Contains(globalRes.TargetedNotifications, n => n.Target == _player && n.Message == "global-only");
    }
}
