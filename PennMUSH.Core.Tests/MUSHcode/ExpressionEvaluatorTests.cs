namespace PennMUSH.Core.Tests.MUSHcode;

using System;
using Xunit;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class ExpressionEvaluatorTests
{
    private readonly EvaluationContext _defaultContext;
    private readonly ExpressionEvaluator _evaluator;

    public ExpressionEvaluatorTests()
    {
        _defaultContext = new EvaluationContext(
            executor: new Dbref(2),
            enactor: new Dbref(1),
            caller: new Dbref(3),
            location: new Dbref(4)
        );

        _evaluator = new ExpressionEvaluator(MockDispatcher);
    }

    private static string MockDispatcher(string functionName, EvaluationContext context, string[] args)
    {
        if (string.Equals(functionName, "ADD", StringComparison.OrdinalIgnoreCase))
        {
            int sum = 0;
            foreach (var a in args)
            {
                if (int.TryParse(a.Trim(), out int val))
                    sum += val;
            }
            return sum.ToString();
        }

        if (string.Equals(functionName, "CONCAT", StringComparison.OrdinalIgnoreCase))
        {
            return string.Concat(args);
        }

        return $"#-1 FUNCTION ({functionName.ToUpperInvariant()}) NOT FOUND";
    }

    [Fact]
    public void Evaluate_PlainText_ReturnsUnchanged()
    {
        var result = _evaluator.Evaluate("Hello World 123!", _defaultContext);
        Assert.Equal("Hello World 123!", result);
    }

    [Fact]
    public void Evaluate_LiteralBraces_StripsOuterBracesAndPreservesContent()
    {
        var result = _evaluator.Evaluate("{Hello [ADD(1, 2)] %r}", _defaultContext);
        Assert.Equal("Hello [ADD(1, 2)] %r", result);
    }

    [Fact]
    public void Evaluate_BackslashEscapes_PreservesEscapedCharacters()
    {
        var result = _evaluator.Evaluate("\\[Hello\\] \\%r", _defaultContext);
        Assert.Equal("[Hello] %r", result);
    }

    [Fact]
    public void Evaluate_StandardSubstitutions_FormatsCorrectly()
    {
        var resultDbrefs = _evaluator.Evaluate("%# %! %@ %l", _defaultContext);
        Assert.Equal("#1 #2 #3 #4", resultDbrefs);

        var resultChars = _evaluator.Evaluate("Line1%rLine2%tTabbed%bSpaced%%End", _defaultContext);
        Assert.Equal("Line1\nLine2\tTabbed Spaced%End", resultChars);
    }

    [Fact]
    public void Evaluate_PositionalArguments_ReturnsCorrectArgs()
    {
        var ctx = new EvaluationContext(
            executor: new Dbref(1),
            enactor: new Dbref(1),
            args: new[] { "first", "second" }
        );

        var result = _evaluator.Evaluate("%0 %1 %2", ctx);
        Assert.Equal("first second ", result);
    }

    [Fact]
    public void Evaluate_Registers_StoresAndRetrieves()
    {
        _defaultContext.SetRegister("0", "registerZero");
        _defaultContext.SetRegister("myVar", "namedRegister");

        var result = _evaluator.Evaluate("%q0 and %q<myVar>", _defaultContext);
        Assert.Equal("registerZero and namedRegister", result);
    }

    [Fact]
    public void Evaluate_FunctionCalls_DispatchesAndSupportsNesting()
    {
        var simple = _evaluator.Evaluate("[ADD(2, 3)]", _defaultContext);
        Assert.Equal("5", simple);

        var nested = _evaluator.Evaluate("[ADD([ADD(1, 2)], 4)]", _defaultContext);
        Assert.Equal("7", nested);

        var concat = _evaluator.Evaluate("[CONCAT(foo, bar, baz)]", _defaultContext);
        Assert.Equal("foobarbaz", concat);

        var unknown = _evaluator.Evaluate("[FOOBAR(1, 2)]", _defaultContext);
        Assert.Equal("#-1 FUNCTION (FOOBAR) NOT FOUND", unknown);
    }

    [Fact]
    public void Evaluate_DepthAndSafetyLimits_EnforcesBounds()
    {
        var deepCtx = new EvaluationContext(new Dbref(1), new Dbref(1))
        {
            RecursionDepth = 51,
            MaxRecursionDepth = 50
        };
        var recursionResult = _evaluator.Evaluate("test", deepCtx);
        Assert.Equal("#-1 FUNCTION RECURSION LIMIT EXCEEDED", recursionResult);

        var busyCtx = new EvaluationContext(new Dbref(1), new Dbref(1))
        {
            InvocationCount = 5001,
            MaxInvocations = 5000
        };
        var invocationResult = _evaluator.Evaluate("test", busyCtx);
        Assert.Equal("#-1 FUNCTION INVOCATION LIMIT EXCEEDED", invocationResult);
    }
}

public class EnactorPercentSubstitutionTests
{
    [Fact]
    public void Evaluate_EnactorNameAndPronouns_FollowSexAttribute()
    {
        var db = new ObjectDatabase();
        var ada = db.CreateObject("ada", ObjectType.Player, new Dbref(1));
        ada.SetAttribute("SEX", "female", ada.Id);
        ada.SetAttribute("VA", "bright star", ada.Id);
        var ctx = new EvaluationContext(executor: ada.Id, enactor: ada.Id);
        var evaluator = new ExpressionEvaluator(database: db);

        Assert.Equal("ada", evaluator.Evaluate("%n", ctx));
        Assert.Equal("Ada", evaluator.Evaluate("%N", ctx));
        Assert.Equal("she", evaluator.Evaluate("%s", ctx));
        Assert.Equal("She", evaluator.Evaluate("%S", ctx));
        Assert.Equal("her", evaluator.Evaluate("%o", ctx));
        Assert.Equal("her", evaluator.Evaluate("%p", ctx));
        Assert.Equal("hers", evaluator.Evaluate("%a", ctx));
        Assert.Equal("Hers", evaluator.Evaluate("%A", new EvaluationContext(ada.Id, ada.Id)));
    }

    [Fact]
    public void Evaluate_MaleAndPluralPronouns()
    {
        var db = new ObjectDatabase();
        var owen = db.CreateObject("owen", ObjectType.Player, new Dbref(1));
        owen.SetAttribute("SEX", "male", owen.Id);
        var crew = db.CreateObject("crew", ObjectType.Thing, owen.Id);
        crew.SetAttribute("SEX", "plural", crew.Id);
        var evaluator = new ExpressionEvaluator(database: db);

        Assert.Equal("him", evaluator.Evaluate("%o", new EvaluationContext(owen.Id, owen.Id)));
        Assert.Equal("they", evaluator.Evaluate("%s", new EvaluationContext(crew.Id, crew.Id)));
        Assert.Equal("theirs", evaluator.Evaluate("%a", new EvaluationContext(crew.Id, crew.Id)));
    }

    [Fact]
    public void Evaluate_ObjectIdArgumentCountAndBudget()
    {
        var db = new ObjectDatabase();
        var ada = db.CreateObject("ada", ObjectType.Player, new Dbref(1));
        var ctx = new EvaluationContext(
            executor: ada.Id,
            enactor: ada.Id,
            args: new[] { "one", "two" });
        var evaluator = new ExpressionEvaluator(database: db);

        Assert.Equal($"{ada.Id}:{ada.CreationTime.ToUnixTimeSeconds()}", evaluator.Evaluate("%:", ctx));
        Assert.Equal("2", evaluator.Evaluate("%+", ctx));
        var budget = new EvaluationContext(executor: ada.Id, enactor: ada.Id);
        Assert.Equal("1 0", evaluator.Evaluate("%?", budget));
    }

    [Fact]
    public void Evaluate_AttributePercentCodes_ReadExecutorAttributes()
    {
        var db = new ObjectDatabase();
        var ada = db.CreateObject("ada", ObjectType.Player, new Dbref(1));
        var room = db.CreateObject("Hall", ObjectType.Room, ada.Id);
        ada.SetAttribute("VA", "bright star", ada.Id);
        ada.SetAttribute("WB", "quiet", ada.Id);
        room.SetAttribute("XA", "hall light", room.Id);
        var ctx = new EvaluationContext(executor: ada.Id, enactor: room.Id);
        var evaluator = new ExpressionEvaluator(database: db);

        Assert.Equal("bright star", evaluator.Evaluate("%va", ctx));
        Assert.Equal("Bright star", evaluator.Evaluate("%VA", ctx));
        Assert.Equal("quiet", evaluator.Evaluate("%wb", ctx));
        Assert.Equal(string.Empty, evaluator.Evaluate("%vz", ctx));
        Assert.Equal("hall light", evaluator.Evaluate("%xa", new EvaluationContext(room.Id, ada.Id)));
    }

    [Fact]
    public void Evaluate_MissingEnactor_ReturnsNoMatchMarker()
    {
        var db = new ObjectDatabase();
        var evaluator = new ExpressionEvaluator(database: db);
        var ctx = new EvaluationContext(executor: new Dbref(4), enactor: new Dbref(9));

        Assert.Equal("#-1", evaluator.Evaluate("%n", ctx));
        Assert.Equal("#-1", evaluator.Evaluate("%s", ctx));
        Assert.Equal("#-1", evaluator.Evaluate("%:", ctx));
    }
}
