namespace PennMUSH.Core.Tests.MUSHcode;

using System;
using Xunit;
using PennMUSH.Core.Types;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;

public class FunctionRegistryTests
{
    private readonly ObjectDatabase _database;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly EvaluationContext _context;
    private readonly MUSHObject _testThing;

    public FunctionRegistryTests()
    {
        _database = new ObjectDatabase();
        _registry = new FunctionRegistry(_database);
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;

        var owner = _database.CreateObject("Wizard", ObjectType.Player, new Dbref(1));
        var room = _database.CreateObject("Room Zero", ObjectType.Room, owner.Id);
        _testThing = _database.CreateObject("Magic Wand", ObjectType.Thing, owner.Id);
        _testThing.Location = room.Id;

        _testThing.SetAttribute("DESCRIBE", "A glowing wand.", owner.Id);
        _testThing.SetAttribute("ADD_ARGS", "[add(%0, %1)]", owner.Id);

        _context = new EvaluationContext(
            executor: owner.Id,
            enactor: owner.Id,
            caller: owner.Id,
            location: room.Id
        );
    }

    [Fact]
    public void MathFunctions_EvaluateCorrectly()
    {
        Assert.Equal("6", _evaluator.Evaluate("[add(1, 2, 3)]", _context));
        Assert.Equal("7", _evaluator.Evaluate("[sub(10, 3)]", _context));
        Assert.Equal("24", _evaluator.Evaluate("[mul(2, 3, 4)]", _context));
        Assert.Equal("5", _evaluator.Evaluate("[div(10, 2)]", _context));
        Assert.Equal("#-1 DIVISION BY ZERO", _evaluator.Evaluate("[div(10, 0)]", _context));
        Assert.Equal("1", _evaluator.Evaluate("[mod(10, 3)]", _context));
        Assert.Equal("#-1 DIVISION BY ZERO", _evaluator.Evaluate("[mod(10, 0)]", _context));
        Assert.Equal("15", _evaluator.Evaluate("[abs(-15)]", _context));
        Assert.Equal("2", _evaluator.Evaluate("[min(5, 2, 8)]", _context));
        Assert.Equal("8", _evaluator.Evaluate("[max(5, 2, 8)]", _context));
    }

    [Fact]
    public void StringFunctions_EvaluateCorrectly()
    {
        Assert.Equal("one two three", _evaluator.Evaluate("[cat(one, two, three)]", _context));
        Assert.Equal("onetwothree", _evaluator.Evaluate("[strcat(one, two, three)]", _context));
        Assert.Equal("5", _evaluator.Evaluate("[strlen(hello)]", _context));
        Assert.Equal("oob", _evaluator.Evaluate("[mid(foobar, 1, 3)]", _context));
        Assert.Equal("hello", _evaluator.Evaluate("[trim({  hello  })]", _context));
        Assert.Equal("HELLO", _evaluator.Evaluate("[ucstr(hello)]", _context));
        Assert.Equal("hello", _evaluator.Evaluate("[lcstr(HELLO)]", _context));
        Assert.Equal("hahaha", _evaluator.Evaluate("[repeat(ha, 3)]", _context));
        Assert.Equal("0", _evaluator.Evaluate("[comp(abc, abc)]", _context));
        Assert.Equal("-1", _evaluator.Evaluate("[comp(abc, abd)]", _context));
        Assert.Equal("1", _evaluator.Evaluate("[comp(abd, abc)]", _context));
    }

    [Fact]
    public void LogicFunctions_EvaluateCorrectly()
    {
        Assert.Equal("1", _evaluator.Evaluate("[t(1)]", _context));
        Assert.Equal("0", _evaluator.Evaluate("[t(0)]", _context));
        Assert.Equal("0", _evaluator.Evaluate("[t(#-1 NOT FOUND)]", _context));
        Assert.Equal("yes", _evaluator.Evaluate("[if(1, yes, no)]", _context));
        Assert.Equal("no", _evaluator.Evaluate("[if(0, yes, no)]", _context));
        Assert.Equal("equal", _evaluator.Evaluate("[ifeq(abc, ABC, equal, notEqual)]", _context));
        Assert.Equal("notEqual", _evaluator.Evaluate("[ifeq(abc, xyz, equal, notEqual)]", _context));
        Assert.Equal("1", _evaluator.Evaluate("[cand(1, hello, 2)]", _context));
        Assert.Equal("0", _evaluator.Evaluate("[cand(1, 0, 2)]", _context));
        Assert.Equal("1", _evaluator.Evaluate("[cor(0, 0, 1)]", _context));
        Assert.Equal("0", _evaluator.Evaluate("[cor(0, 0, 0)]", _context));

        _evaluator.Evaluate("[setq(myreg, myval)]", _context);
        Assert.Equal("myval", _evaluator.Evaluate("[r(myreg)]", _context));
    }

    [Fact]
    public void DbFunctions_EvaluateCorrectly()
    {
        string thingRef = _testThing.Id.ToString();

        Assert.Equal("Magic Wand", _evaluator.Evaluate($"[name({thingRef})]", _context));
        Assert.Equal("A glowing wand.", _evaluator.Evaluate($"[get({thingRef}/DESCRIBE)]", _context));
        Assert.Equal("1", _evaluator.Evaluate($"[hasattr({thingRef}, DESCRIBE)]", _context));
        Assert.Equal("0", _evaluator.Evaluate($"[hasattr({thingRef}, NONEXISTENT)]", _context));
        Assert.Equal(_testThing.Location.ToString(), _evaluator.Evaluate($"[loc({thingRef})]", _context));
        Assert.Equal(_testThing.Owner.ToString(), _evaluator.Evaluate($"[owner({thingRef})]", _context));
        Assert.Equal("THING", _evaluator.Evaluate($"[type({thingRef})]", _context));

        Assert.Equal("40", _evaluator.Evaluate($"[u({thingRef}/ADD_ARGS, 15, 25)]", _context));
    }
}