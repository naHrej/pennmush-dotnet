namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Database;
using PennMUSH.Core.Locks;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;
using Xunit;

public class CommandBatchTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly ActionQueue _queue = new();
    private readonly ChannelRegistry _channels = new();
    private readonly Dbref _god;
    private readonly Dbref _ada;
    private readonly Dbref _room;

    public CommandBatchTests()
    {
        var room = _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        _room = room.Id;
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.Owner = god.Id;
        god.AddFlag("WIZARD");
        god.Warnings = WarningBits.Normal;
        _god = god.Id;
        var ada = _db.CreateObject("Ada", ObjectType.Player, god.Id);
        ada.Owner = ada.Id;
        ada.Pennies = 50;
        ada.Warnings = WarningBits.Normal;
        _ada = ada.Id;
        _db.MoveObject(god.Id, room.Id);
        _db.MoveObject(ada.Id, room.Id);
        _db.SetConnected(ada.Id, true);
        _db.SetConnected(god.Id, true);
        var evaluator = new ExpressionEvaluator(database: _db);
        _dispatcher = new CommandDispatcher(evaluator);
        _dispatcher.Register(new CommandCommandHandler(_dispatcher), "@command");
        _dispatcher.Register(new HookCommandHandler(_dispatcher), "@hook");
        _dispatcher.Register(new WcheckCommandHandler(), "@wcheck");
        _dispatcher.Register(new BuyCommandHandler(evaluator, _queue), "buy");
        _dispatcher.Register(new DbckCommandHandler(), "@dbck");
        _dispatcher.Register(new ClockCommandHandler(_channels), "@clock");
        _dispatcher.Register(new ChannelCommandHandler(_channels), "@channel");
        _dispatcher.Register(new ChatCommandHandler(_channels, evaluator), "@chat");
        _dispatcher.Register(new PingCommandHandler("ping", "pong"), "ping");
        _dispatcher.AddRestriction("buy", "nogagged", null);
    }

    [Fact]
    public void AWizardAddsACommandAndGodDeletesIt()
    {
        var added = _dispatcher.Dispatch(_god, "@command/add frob", _db);
        Assert.Equal("Command FROB added.", added.Notifications[^1]);

        var ran = _dispatcher.Dispatch(_ada, "frob", _db);
        Assert.Equal("This command has not been implemented.", ran.Notifications[0]);

        var mortal = _dispatcher.Dispatch(_ada, "@command/add nope", _db);
        Assert.Equal("Permission denied.", mortal.Notifications[0]);

        var refused = _dispatcher.Dispatch(_ada, "@command/delete frob", _db);
        Assert.Equal("Permission denied.", refused.Notifications[0]);

        var removed = _dispatcher.Dispatch(_god, "@command/delete frob", _db);
        Assert.Equal("Removed FROB from command table.", removed.Notifications[0]);
    }

    [Fact]
    public void DisablingACommandLetsItFallThrough()
    {
        _dispatcher.Dispatch(_god, "@command/disable ping", _db);
        var result = _dispatcher.Dispatch(_ada, "ping", _db);
        Assert.Equal("Huh?  (Type \"help\" for help.)", result.Notifications[0]);

        var always = _dispatcher.Dispatch(_god, "@command/disable @command", _db);
        Assert.Contains("@command is ALWAYS enabled.", always.Notifications);
    }

    [Fact]
    public void AnIgnoreHookSkipsTheCommandAndAnOverrideReplacesIt()
    {
        var ada = _db.GetObject(_ada)!;
        ada.SetAttribute("BLOCK", "0", _ada);
        ada.SetAttribute("ALLOW", "1", _ada);
        ada.SetAttribute("SWAP", "$ping:ping2", _ada);
        _dispatcher.Register(new PingCommandHandler("ping2", "from hook"));

        var blocked = _dispatcher.Dispatch(_god, "@hook/ignore ping=#" + _ada.Value + "/BLOCK", _db);
        Assert.Equal("Hook set for PING.", blocked.Notifications[0]);
        var silent = _dispatcher.Dispatch(_ada, "ping", _db);
        Assert.Empty(silent.Notifications);

        _dispatcher.Dispatch(_god, "@hook/ignore ping", _db);
        _dispatcher.Dispatch(_god, "@hook/before ping=#" + _ada.Value + "/ALLOW", _db);
        var before = _dispatcher.Dispatch(_ada, "ping", _db);
        Assert.Equal("pong", before.Notifications[0]);

        _dispatcher.Dispatch(_god, "@hook/override ping=#" + _ada.Value + "/SWAP", _db);
        var replaced = _dispatcher.Dispatch(_ada, "ping", _db);
        Assert.Equal("from hook", replaced.Notifications[0]);

        var list = _dispatcher.Dispatch(_god, "@hook/list ping", _db);
        Assert.Contains(list.Notifications, line => line.StartsWith("@hook/before:"));
        Assert.Contains(list.Notifications, line => line.StartsWith("@hook/override:"));
    }

    [Fact]
    public void WcheckWarnsAboutARoomWithoutADescription()
    {
        var room = _db.GetObject(_room)!;
        room.Owner = _ada;
        var result = _dispatcher.Dispatch(_ada, "@wcheck Limbo", _db);
        Assert.Equal(
            new[] { "Warning 'room-desc' for Limbo(#0):", "room has no description", "@wcheck complete." },
            result.Notifications);

        var mortal = _dispatcher.Dispatch(_ada, "@wcheck/all", _db);
        Assert.Equal("You'd better check your wizbit first.", mortal.Notifications[0]);
    }

    [Fact]
    public void BuyPaysTheVendorAndAPayLockCanRefuse()
    {
        var vendor = _db.CreateObject("Shop", ObjectType.Thing, _ada);
        vendor.SetAttribute("PRICELIST", "WIDGET:10 RED_HAT:5-15", _ada);
        _db.MoveObject(vendor.Id, _room);

        var bought = _dispatcher.Dispatch(_ada, "buy widget", _db);
        Assert.Equal("You buy a widget from Shop.", bought.Notifications[0]);
        Assert.Equal(40, _db.GetObject(_ada)!.Pennies);
        Assert.Equal(10, vendor.Pennies);

        vendor.SetLock(LockType.Pay, LockParser.Parse("#99"), _ada);
        var refused = _dispatcher.Dispatch(_ada, "buy red hat", _db);
        Assert.Equal("Shop doesn't want your money.", refused.Notifications[0]);
        Assert.Equal(40, _db.GetObject(_ada)!.Pennies);
    }

    [Fact]
    public void AbuyQueuesTheVendorsCommandListForTheBuyer()
    {
        var vendor = _db.CreateObject("Shop", ObjectType.Thing, _ada);
        vendor.SetAttribute("PRICELIST", "WIDGET:10", _ada);
        vendor.SetAttribute("ABUY", "@create %0\n@create spare", _ada);
        _db.MoveObject(vendor.Id, _room);
        _dispatcher.Register(new CreateCommandHandler(), "@create");

        var bought = _dispatcher.Dispatch(_ada, "buy widget", _db);
        Assert.Equal("You buy a widget from Shop.", bought.Notifications[0]);
        Assert.Equal(2, _queue.Count);
        Assert.DoesNotContain(_db.GetAllObjects(), obj => obj.Name == "widget");

        var ran = _queue.ProcessReady(DateTimeOffset.UtcNow.AddSeconds(1), _dispatcher, _db);
        Assert.Equal(0, _queue.Count);
        var created = _db.GetAllObjects().Where(obj => obj.Name == "widget").ToList();
        Assert.Single(created);
        Assert.Equal(vendor.Id, created[0].Owner);
        Assert.Contains(_db.GetAllObjects(), obj => obj.Name == "spare" && obj.Owner == vendor.Id);
        Assert.Contains(ran, ctx => ctx.Notifications.Any(note => note.StartsWith("Created: widget")));
    }

    [Fact]
    public void DbckRepairsAnInvalidOwnerAndWarnsAboutADisconnectedRoom()
    {
        var mortal = _dispatcher.Dispatch(_ada, "@dbck", _db);
        Assert.Equal("Silly mortal, chicks are for kids!", mortal.Notifications[0]);

        var orphan = _db.CreateObject("Orphan", ObjectType.Thing, _room);
        var spare = _db.CreateObject("Spare", ObjectType.Room, _ada);
        var result = _dispatcher.Dispatch(_god, "@dbck", _db);
        Assert.Equal(
            new[] { "GAME: Performing database consistency check.", "GAME: Database consistency check complete." },
            result.Notifications);
        Assert.Equal(_god, orphan.Owner);
        Assert.Contains(result.TargetedNotifications, note => note.Target == _ada && note.Message.Contains("disconnected room"));
        Assert.Equal(_ada, spare.Owner);
    }

    [Fact]
    public void AJoinLockRefusesAPlayerAndLetsAWizardThrough()
    {
        _channels.CreateChannel("Public", _god);
        var set = _dispatcher.Dispatch(_god, "@clock/join Public=#99", _db);
        Assert.Equal("CHAT: Joinlock on <Public> set.", set.Notifications[0]);

        var denied = _dispatcher.Dispatch(_ada, "@channel/join Public", _db);
        Assert.Equal("Permission to join denied.", denied.Notifications[0]);
        Assert.False(_channels.GetChannel("Public")!.HasMember(_ada));

        var joined = _dispatcher.Dispatch(_god, "@channel/join Public", _db);
        Assert.Contains("CHAT: Warning: You don't meet channel join permissions! (joining anyway)", joined.Notifications);
        Assert.True(_channels.GetChannel("Public")!.HasMember(_god));

        _channels.GetChannel("Public")!.AddMember(_ada);
        _dispatcher.Dispatch(_god, "@clock/speak Public=#99", _db);
        var quiet = _dispatcher.Dispatch(_ada, "@chat Public=hello", _db);
        Assert.Equal("Sorry, you're not allowed to speak on channel <Public>.", quiet.Notifications[0]);
    }

    private sealed class PingCommandHandler : ICommandHandler
    {
        private readonly string _text;
        public PingCommandHandler(string name, string text)
        {
            Name = name;
            _text = text;
        }
        public string Name { get; }
        public bool Execute(CommandContext context, ObjectDatabase db)
        {
            context.Notify(_text);
            return true;
        }
    }
}
