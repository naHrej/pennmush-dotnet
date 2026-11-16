namespace PennMUSH.Core.Tests.MUSHcode;

using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class NameFormatTests
{
    [Fact]
    public void NameFormat_UsesTheAttributeAndFallsBackToName()
    {
        var db = new ObjectDatabase();
        var owner = db.CreateObject("QueueTester;qt", ObjectType.Thing, new Dbref(1));
        var registry = new FunctionRegistry(db);
        var evaluator = new ExpressionEvaluator(registry.Dispatch, db);
        registry.Evaluator = evaluator;
        var ctx = new EvaluationContext(executor: owner.Id, enactor: owner.Id);
        string call = "[nameformat(" + owner.Id + ")]";

        Assert.Equal("QueueTester;qt", evaluator.Evaluate(call, ctx));

        owner.SetAttribute("NAMEFORMAT", "   ", owner.Id);
        Assert.Equal("QueueTester;qt", evaluator.Evaluate(call, ctx));

        owner.SetAttribute("NAMEFORMAT", "*%1", owner.Id);
        Assert.Equal("*QueueTester;qt", evaluator.Evaluate(call, ctx));

        owner.SetAttribute("NAMEFORMAT", "[name(%0)]", owner.Id);
        Assert.Equal("QueueTester;qt", evaluator.Evaluate(call, ctx));

        var viewer = db.CreateObject("Ada", ObjectType.Player, owner.Id);
        var room = db.CreateObject("Hall", ObjectType.Room, owner.Id);
        var viewerCtx = new EvaluationContext(executor: viewer.Id, enactor: viewer.Id);
        owner.SetAttribute("NAMEFORMAT", "%!", owner.Id);
        Assert.Equal(owner.Id.ToString(), evaluator.Evaluate(call, viewerCtx));

        room.SetAttribute("NAMEFORMAT", "[name(%!)]", room.Id);
        Assert.Equal(
            "Hall",
            FormatAttributes.Evaluate(db, evaluator, room, viewer.Id, viewer.Id, "NAMEFORMAT"));

        Assert.Equal("#-1 NO MATCH", evaluator.Evaluate("[nameformat(#99)]", ctx));
    }
}
