namespace PennMUSH.Core.Tests.Queue;

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.MUSHcode.Functions;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class ActionQueueAndAsyncTests
{
    private readonly ObjectDatabase _db;
    private readonly ActionQueue _queue;
    private readonly CommandDispatcher _dispatcher;
    private readonly ExpressionEvaluator _evaluator;
    private readonly MUSHObject _roomZero;
    private readonly MUSHObject _god;
    private readonly MUSHObject _player;
    private readonly MUSHObject _room;

    public ActionQueueAndAsyncTests()
    {
        _db = new ObjectDatabase();
        _queue = new ActionQueue();
        AsyncFunctions.Initialize(_queue);

        var registry = new FunctionRegistry(_db);
        _evaluator = new ExpressionEvaluator(registry.Dispatch, registry.Database);
        registry.Evaluator = _evaluator;
        AsyncFunctions.Register(registry, _queue);

        _roomZero = _db.CreateObject("Room Zero", ObjectType.Room, new Dbref(1));
        _god = _db.CreateObject("God", ObjectType.Player, new Dbref(1));
        _player = _db.CreateObject("Mortal", ObjectType.Player, _god.Id);
        _room = _db.CreateObject("Testing Lab", ObjectType.Room, _god.Id);

        _db.MoveObject(_player.Id, _room.Id);

        _dispatcher = new CommandDispatcher(_evaluator);
        _dispatcher.Register(new SayCommandHandler(), "\"");
        _dispatcher.Register(new WaitCommandHandler(_queue), "@notify");
        _dispatcher.Register(new HaltCommandHandler(_queue));
        _dispatcher.Register(new AllHaltCommandHandler(_queue));
    }

    [Fact]
    public void TimedQueue_ExecutesOnlyWhenReady()
    {
        var entry = _queue.Enqueue(_player.Id, _player.Id, "say Action Fired!", TimeSpan.FromSeconds(5));
        Assert.Equal(1, _queue.Count);

        // Before time elapsed
        var now = DateTimeOffset.UtcNow;
        var processedEarly = _queue.ProcessReady(now, _dispatcher, _db);
        Assert.Empty(processedEarly);
        Assert.Equal(1, _queue.Count);

        // After time elapsed
        var processedLate = _queue.ProcessReady(now.AddSeconds(6), _dispatcher, _db);
        Assert.Single(processedLate);
        Assert.Equal(0, _queue.Count);
        Assert.Contains("You say, \"Action Fired!\"", processedLate[0].Notifications);
    }

    [Fact]
    public void Semaphore_WaitsUntilNotified()
    {
        var waitCtx = _dispatcher.Dispatch(_player.Id, "@wait signal1=say Signal Received!", _db);
        Assert.Contains(waitCtx.Notifications, n => n.Contains("Queued on semaphore 'signal1'"));
        Assert.Equal(1, _queue.Count);

        // Processing before notify produces nothing
        var early = _queue.ProcessReady(DateTimeOffset.UtcNow.AddMinutes(5), _dispatcher, _db);
        Assert.Empty(early);

        // Notify semaphore
        var notifyCtx = _dispatcher.Dispatch(_player.Id, "@notify signal1", _db);
        Assert.Contains("Notified 1 process(es) on semaphore 'signal1'.", notifyCtx.Notifications);

        // Now it's ready
        var ready = _queue.ProcessReady(DateTimeOffset.UtcNow, _dispatcher, _db);
        Assert.Single(ready);
        Assert.Contains("You say, \"Signal Received!\"", ready[0].Notifications);
    }

    [Fact]
    public void HaltCommand_ClearsTargetEntries()
    {
        _queue.Enqueue(_player.Id, _player.Id, "say One", TimeSpan.FromSeconds(10));
        _queue.Enqueue(_player.Id, _player.Id, "say Two", TimeSpan.FromSeconds(20));
        _queue.Enqueue(_god.Id, _god.Id, "say God Message", TimeSpan.FromSeconds(30));
        Assert.Equal(3, _queue.Count);

        // Halt player
        var haltCtx = _dispatcher.Dispatch(_player.Id, $"@halt {_player.Id}", _db);
        Assert.Contains("Halted 2 process(es).", haltCtx.Notifications);
        Assert.Equal(1, _queue.Count);

        // Allhalt by Wizard
        var allHaltCtx = _dispatcher.Dispatch(_god.Id, "@allhalt", _db);
        Assert.Contains("Halted all 1 process(es)", allHaltCtx.Notifications[0]);
        Assert.Equal(0, _queue.Count);
    }

    [Fact]
    public void CommandListener_MatchesWildcardPatternAndCapturesArguments()
    {
        var vendor = _db.CreateObject("Automated Vendor", ObjectType.Thing, _god.Id);
        _db.MoveObject(vendor.Id, _room.Id);

        // Setup listener attribute on vendor: $buy *:You purchased %0 from vendor!
        vendor.SetAttribute("CMD_BUY", "$buy *:You purchased %0 from vendor!", _god.Id);

        var ctx = _dispatcher.Dispatch(_player.Id, "buy healing potion", _db);
        Assert.Contains("You purchased healing potion from vendor!", ctx.Notifications);
    }

    [Fact]
    public void ParallelMap_ExecutesConcurrentlyAcrossThreadsPreservingOrder()
    {
        var helper = _db.CreateObject("Helper", ObjectType.Thing, _god.Id);
        helper.SetAttribute("DOUBLE", "[add(%0, %0)]", _god.Id);

        var evalCtx = new EvaluationContext(_player.Id, _player.Id);
        string result = _evaluator.Evaluate($"[parallel_map(1 2 3 4 5 6 7 8 9 10,  , {helper.Id}/DOUBLE)]", evalCtx);

        Assert.Equal("2 4 6 8 10 12 14 16 18 20", result);
    }

    [Fact]
    public async Task AsyncFunction_RunsInBackgroundWorkerAndQueuesCallback()
    {
        var evalCtx = new EvaluationContext(_player.Id, _player.Id);

        // async({[mul(7, 6)]}, say Answer is %0)
        string taskId = _evaluator.Evaluate("[async({[mul(7, 6)]}, say Answer is %0)]", evalCtx);
        Assert.StartsWith("TASK#", taskId);

        // Wait up to 500ms for threadpool worker to complete
        bool found = false;
        for (int i = 0; i < 50; i++)
        {
            if (_queue.Count > 0)
            {
                found = true;
                break;
            }
            await Task.Delay(10);
        }

        Assert.True(found, "Async background task did not enqueue completion callback.");
        var ready = _queue.ProcessReady(DateTimeOffset.UtcNow, _dispatcher, _db);
        Assert.Single(ready);
        Assert.Contains("You say, \"Answer is 42\"", ready[0].Notifications);
    }
}
