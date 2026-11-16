namespace PennMUSH.Core.Tests.MUSHcode;

using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class BareFunctionTests
{
    [Fact]
    public void FirstParenthesis_CallsAFunctionWhenTheTextSoFarIsItsName()
    {
        var db = new ObjectDatabase();
        var player = db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        var registry = new FunctionRegistry(db);
        var evaluator = new ExpressionEvaluator(registry.Dispatch, db);
        registry.Evaluator = evaluator;
        var ctx = new EvaluationContext(executor: player.Id, enactor: player.Id, args: new[] { "name" });
        string id = player.Id.ToString();

        Assert.Equal("Ada", evaluator.Evaluate("name(me)", ctx));
        Assert.Equal("Ada", evaluator.Evaluate("[name(me)]", ctx));
        Assert.Equal("Ada", evaluator.Evaluate("%0(me)", ctx));
        Assert.Equal("Ada name(me)", evaluator.Evaluate("name(me) name(me)", ctx));
        Assert.Equal("hello(name(me))", evaluator.Evaluate("hello(name(me))", ctx));
        Assert.Equal("6", evaluator.Evaluate("add(add(1,2),3)", ctx));
        Assert.Equal("a b", evaluator.Evaluate("iter(a b,##)", ctx));

        string chained = "AdaAda";
        Assert.Equal(chained, evaluator.Evaluate("[name(me)][name(" + id + ")]", ctx));
        Assert.Equal(chained, evaluator.Evaluate("name(me)[name(" + id + ")]", ctx));
        Assert.Equal("Adaname(" + id + ")", evaluator.Evaluate("name(me)name(" + id + ")", ctx));
    }
}