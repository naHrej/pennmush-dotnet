namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;
using Xunit;

public class QueueReportCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly ActionQueue _queue = new();
    private readonly CommandDispatcher _dispatcher = new();
    private readonly Dbref _god;
    private readonly Dbref _owner;
    private readonly Dbref _other;

    public QueueReportCommandTests()
    {
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        _god = _db.CreateObject("One", ObjectType.Player, new Dbref(1)).Id;
        _owner = _db.CreateObject("Owner", ObjectType.Player, _god).Id;
        _other = _db.CreateObject("Other", ObjectType.Player, _god).Id;
        _dispatcher.Register(new WaitCommandHandler(_queue), "@wait");
        _dispatcher.Register(new ThinkCommandHandler(), "think");
        _dispatcher.Register(new PsCommandHandler(_queue), "@ps");
        _dispatcher.Register(new KickCommandHandler(_queue, _dispatcher), "@kick");
    }

    [Fact]
    public void Ps_ListsThePlayersQueueAndOneEntry()
    {
        var waited = _dispatcher.Dispatch(_owner, "@wait 30=think later", _db);
        var held = _dispatcher.Dispatch(_owner, "@wait gate=think held", _db);
        int waitedPid = Pid(waited);
        int heldPid = Pid(held);

        var report = _dispatcher.Dispatch(_owner, "@ps", _db);

        Assert.Contains("Queue for Owner:", report.Notifications);
        Assert.Contains(report.Notifications, line => line.Contains($"#{waitedPid}") && line.Contains("think later"));
        Assert.Contains(report.Notifications, line => line.Contains($"#{heldPid}") && line.Contains("semaphore:gate") && line.Contains("think held"));
        Assert.Contains("Command: 0  Wait: 1  Semaphore: 1  Total: 2", report.Notifications);

        var one = _dispatcher.Dispatch(_owner, $"@ps {waitedPid}", _db);
        Assert.Contains($"#{waitedPid}", one.Notifications[0]);
        Assert.DoesNotContain(one.Notifications, line => line.Contains("think held"));

        var count = _dispatcher.Dispatch(_owner, "@ps/count", _db);
        Assert.Equal("Queue entries: 2", count.Notifications[0]);

        var summary = _dispatcher.Dispatch(_owner, "@ps/summary", _db);
        Assert.Contains("Command: 0  Wait: 1  Semaphore: 1  Total: 2", summary.Notifications);

        var denied = _dispatcher.Dispatch(_owner, "@ps/all", _db);
        Assert.Contains("Permission denied.", denied.Notifications);

        var all = _dispatcher.Dispatch(_god, "@ps/all", _db);
        Assert.Contains("Queue for all:", all.Notifications);
        Assert.Contains(all.Notifications, line => line.Contains("think later"));

        var hidden = _dispatcher.Dispatch(_other, $"@ps {heldPid}", _db);
        Assert.Contains("Permission denied.", hidden.Notifications);

        var missing = _dispatcher.Dispatch(_owner, "@ps 9999", _db);
        Assert.Contains("That is not a valid pid!", missing.Notifications);
    }

    [Fact]
    public void Kick_RunsTheNextWaitingCommand()
    {
        _dispatcher.Dispatch(_owner, "@wait 30=think later", _db);
        _dispatcher.Dispatch(_owner, "@wait gate=think held", _db);

        var denied = _dispatcher.Dispatch(_owner, "@kick", _db);
        Assert.Contains("Permission denied.", denied.Notifications);
        Assert.Equal(2, _queue.Count);

        var kicked = _dispatcher.Dispatch(_god, "@kick 1", _db);

        Assert.Contains("Kicked 1 command.", kicked.Notifications);
        Assert.Contains("later", kicked.Notifications);
        Assert.Equal(1, _queue.Count);
        Assert.Contains(_queue.Snapshot(), entry => entry.Semaphore == "gate");

        var empty = _dispatcher.Dispatch(_god, "@kick", _db);
        Assert.Contains("Nothing to kick.", empty.Notifications);
        Assert.Equal(1, _queue.Count);

        var bad = _dispatcher.Dispatch(_god, "@kick nope", _db);
        Assert.Contains("Usage: @kick <count>", bad.Notifications);
    }

    private static int Pid(CommandContext context)
    {
        string line = context.Notifications[0];
        int start = line.LastIndexOf(' ') + 1;
        string number = line.TrimEnd('.');
        number = number.Substring(start);
        return int.Parse(number);
    }
}
