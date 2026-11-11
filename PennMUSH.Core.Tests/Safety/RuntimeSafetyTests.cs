namespace PennMUSH.Core.Tests.Safety;

using System;
using System.Threading;
using Xunit;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class RuntimeSafetyTests
{
    private readonly ObjectDatabase _db;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly MUSHObject _objA;
    private readonly MUSHObject _objB;

    public RuntimeSafetyTests()
    {
        _db = new ObjectDatabase();
        var room = _db.CreateObject("Test Room", ObjectType.Room, Dbref.Nothing);
        var god = _db.CreateObject("God", ObjectType.Player, Dbref.Nothing);
        god.Location = room.Id;

        _objA = _db.CreateObject("ObjectA", ObjectType.Thing, god.Id);
        _objB = _db.CreateObject("ObjectB", ObjectType.Thing, god.Id);

        _registry = new FunctionRegistry(_db);
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;
    }

    [Fact]
    public void RecursionCycle_DirectAndIndirect_DetectedAndPrevented()
    {
        // A calls B, B calls A -> infinite recursion loop
        _objA.SetAttribute("LOOP_ATTR", $"[u(#{_objB.Id.Value}/LOOP_ATTR)]", _objA.Owner);
        _objB.SetAttribute("LOOP_ATTR", $"[u(#{_objA.Id.Value}/LOOP_ATTR)]", _objB.Owner);

        var ctx = new EvaluationContext(_objA.Id, _objA.Id);
        string result = _evaluator.Evaluate($"[u(#{_objA.Id.Value}/LOOP_ATTR)]", ctx);

        // Instead of stack overflow crash, terminates cleanly with cycle detection
        Assert.Contains("#-1 RECURSION CYCLE DETECTED", result);
    }

    [Fact]
    public void ExecutionTimeout_AbortsRunawayExecution()
    {
        var ctx = new EvaluationContext(_objA.Id, _objA.Id)
        {
            ExecutionDeadline = DateTimeOffset.UtcNow.AddMilliseconds(-10) // Already expired
        };

        string result = _evaluator.Evaluate("[add(1,2)]", ctx);
        Assert.Equal("#-1 TIMEOUT (RUNAWAY LOOP DETECTED)", result);
    }

    [Fact]
    public void CancellationToken_CancelsExecutionCleanly()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-canceled

        var ctx = new EvaluationContext(_objA.Id, _objA.Id)
        {
            CancellationToken = cts.Token
        };

        string result = _evaluator.Evaluate("[add(1,2)]", ctx);
        Assert.Equal("#-1 TIMEOUT (RUNAWAY LOOP DETECTED)", result);
    }

    [Fact]
    public void RegexTimeout_NeutralizesReDoS()
    {
        var ctx = new EvaluationContext(_objA.Id, _objA.Id);

        // Pathological catastrophic backtracking regex: (a+)+$ against a long string ending in X
        // In unshielded engines this locks a CPU core for minutes/hours.
        string pattern = "{(a+)+$}";
        string input = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaX";

        string result = _evaluator.Evaluate($"[regmatch({input},{pattern})]", ctx);
        Assert.Equal("#-1 REGEX TIMEOUT", result);
    }

    [Fact]
    public void QueueFloodProtection_ThrottlesPlayerQueue()
    {
        var queue = new ActionQueue { MaxQueueEntriesPerPlayer = 5 };

        // Enqueue 5 allowed commands
        for (int i = 0; i < 5; i++)
        {
            var entry = queue.Enqueue(_objA.Id, _objA.Id, $"say command {i}");
            Assert.NotNull(entry);
        }

        // 6th command exceeds player queue quota and is rejected
        var overflowEntry = queue.Enqueue(_objA.Id, _objA.Id, "say overflow");
        Assert.Null(overflowEntry);
    }
}
