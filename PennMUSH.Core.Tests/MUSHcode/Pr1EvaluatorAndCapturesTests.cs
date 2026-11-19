namespace PennMUSH.Core.Tests.MUSHcode;

using Xunit;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class Pr1EvaluatorAndCapturesTests
{
    private readonly ObjectDatabase _db;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly EvaluationContext _wizCtx;
    private readonly EvaluationContext _mortalCtx;
    private readonly MUSHObject _room;
    private readonly MUSHObject _wiz;
    private readonly MUSHObject _mortal;
    private readonly MUSHObject _thing;

    public Pr1EvaluatorAndCapturesTests()
    {
        _db = new ObjectDatabase();
        _room = _db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);             // #0
        _wiz = _db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);          // #1
        _wiz.Owner = _wiz.Id;
        _wiz.AddFlag("WIZARD");
        _db.MoveObject(_wiz.Id, _room.Id);

        _mortal = _db.CreateObject("Mortal", ObjectType.Player, Dbref.Nothing);       // #2
        _mortal.Owner = _mortal.Id;
        _db.MoveObject(_mortal.Id, _room.Id);

        _thing = _db.CreateObject("Box", ObjectType.Thing, _wiz.Id);                  // #3
        _thing.SetAttribute("PLAIN", "hello", _wiz.Id);
        _thing.SetAttribute("EVALME", "[name(me)]-%!", _wiz.Id);
        _db.MoveObject(_thing.Id, _room.Id);

        _registry = new FunctionRegistry(_db);
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;

        _wizCtx = new EvaluationContext(_wiz.Id, _wiz.Id, _wiz.Id, _room.Id, new[] { "orig0" });
        _mortalCtx = new EvaluationContext(_mortal.Id, _mortal.Id, _mortal.Id, _room.Id, new[] { "m0" });
    }

    [Fact]
    public void Null_EvaluatesArguments_WhileDoubleAtDoesNot()
    {
        Assert.Equal("ran", _evaluator.Evaluate("[null(setr(a,ran))][r(a)]", _wizCtx));
        Assert.Equal(string.Empty, _evaluator.Evaluate("[@@(setr(b,skipped))][r(b)]", _wizCtx));
    }

    [Fact]
    public void SwitchAndReswitch_PopulateWildcardAndRegexCaptures()
    {
        Assert.Equal("foo|bar|orig0", _evaluator.Evaluate("[switch(foo:bar,*:*,%0|%1)]|%0", _wizCtx));
        Assert.Equal("abc123|abc|123|$|$100", _evaluator.Evaluate("[reswitch(abc123def,{([a-z]+)(?<num>[0-9]+)},$0|$1|$<num>|$$)]|$100", _wizCtx));
        Assert.Equal("first", _evaluator.Evaluate("[switch(a,a,first,b,second)]", _wizCtx));
        Assert.Contains("TOO FEW ARGUMENTS", _evaluator.Evaluate("[switch(a,a)]", _wizCtx));
        Assert.Contains("TOO FEW ARGUMENTS", _evaluator.Evaluate("[reswitch(a,a)]", _wizCtx));
    }

    [Fact]
    public void RegMatchAndRegEdit_MatchOriginalSemantics()
    {
        Assert.Equal("1", _evaluator.Evaluate("[regmatch(abc123,{([a-z]+)([0-9]+)},0:full - 2:num)]", _wizCtx));
        Assert.Equal("abc123|123", _evaluator.Evaluate("[r(full)]|[r(num)]", _wizCtx));

        // Failed match clears listed registers
        Assert.Equal("0", _evaluator.Evaluate("[regmatch(12345,{([a-z]+)},0:full)]", _wizCtx));
        Assert.Equal(string.Empty, _evaluator.Evaluate("[r(full)]", _wizCtx));

        // Invalid regex returns REGEXP ERROR
        Assert.StartsWith("#-1 REGEXP ERROR:", _evaluator.Evaluate("[regmatch(abc,\\[)]", _wizCtx));

        // Regedit evaluates replacement per match with $0..$n
        Assert.Equal("A:1 b2", _evaluator.Evaluate("[regedit(a1 b2,{([a-z])([0-9])},[ucstr($1)]:$2)]", _wizCtx));
        Assert.Equal("A:1 B:2", _evaluator.Evaluate("[regeditall(a1 b2,{([a-z])([0-9])},[ucstr($1)]:$2)]", _wizCtx));
    }

    [Fact]
    public void RegistersAndStackFunctions_ValidateAndInspectTypes()
    {
        Assert.Equal("#-1 INVALID Q-REGISTER NAME", _evaluator.Evaluate("[setq(bad name,1)]", _wizCtx));
        Assert.Equal("#-1 FUNCTION (SETQ) EXPECTS AN EVEN NUMBER OF ARGUMENTS", _evaluator.Evaluate("[setq(a,1,b)]", _wizCtx));
        Assert.Equal("#-1 FUNCTION (LETQ) EXPECTS AN ODD NUMBER OF ARGUMENTS", _evaluator.Evaluate("[letq(a,1,b,2)]", _wizCtx));

        Assert.Equal("orig0", _evaluator.Evaluate("[r(0,args)]", _wizCtx));
        Assert.Equal("top|inner", _evaluator.Evaluate("[switch(top,*,[switch(inner,*,[stext(l)]|[stext(0)])])]", _wizCtx));
        Assert.Contains("Qa", _evaluator.Evaluate("[setq(a,1)][registers(*,qregisters)]", _wizCtx));
        Assert.Contains("A0", _evaluator.Evaluate("[registers(*,args)]", _wizCtx));
    }

    [Fact]
    public void Fn_Ulambda_Default_EDefault_Objeval_And_V_WorkCorrectly()
    {
        Assert.Equal("3", _evaluator.Evaluate("[fn(add,1,2)]", _wizCtx));
        Assert.Equal("6", _evaluator.Evaluate("[ulambda([lit(#lambda/)][lit(add(%0,1))],5)]", _wizCtx));

        // Default evaluates fallback only if missing
        Assert.Equal("hello", _evaluator.Evaluate($"[default({_thing.Id}/MISSING,{_thing.Id}/PLAIN,setr(d1,skipped))][r(d1)]", _wizCtx));
        Assert.Equal("ran", _evaluator.Evaluate($"[default({_thing.Id}/MISSING,setr(d2,ran))]", _wizCtx));

        // EDefault evaluates found attribute as target object
        Assert.Equal($"Box-{_thing.Id}", _evaluator.Evaluate($"[edefault({_thing.Id}/EVALME,fallback)]", _wizCtx));

        // Objeval checks controls() and is noParse
        Assert.Equal(_thing.Id.ToString(), _evaluator.Evaluate($"[objeval({_thing.Id},%!)]", _wizCtx));
        Assert.Equal(_mortal.Id.ToString(), _evaluator.Evaluate($"[objeval({_thing.Id},%!)]", _mortalCtx));

        // V() handles built-in substitutions and stack args
        Assert.Equal($"orig0|{_wiz.Id}|Wizard|{_wizCtx.Location}", _evaluator.Evaluate("[v(0)]|[v(#)]|[v(n)]|[v(l)]", _wizCtx));
    }
}
