namespace PennMUSH.Core.Tests.MUSHcode;

using System;
using System.Collections.Generic;
using Xunit;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.MUSHcode.Functions;
using PennMUSH.Core.Types;

public class Pr5ListMathTimeCryptoJsonTests
{
    private readonly ObjectDatabase _db;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly EvaluationContext _ctx;
    private readonly MUSHObject _wiz;

    public Pr5ListMathTimeCryptoJsonTests()
    {
        _db = new ObjectDatabase();
        var room = _db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);         // #0
        _wiz = _db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);          // #1
        _wiz.Owner = _wiz.Id;
        _wiz.AddFlag("WIZARD");
        _db.MoveObject(_wiz.Id, room.Id);

        _wiz.SetAttribute("IS_EVEN", "[eq(mod(%0,%1),0)]", _wiz.Id);
        _wiz.SetAttribute("MAP_POS", "%0:%1", _wiz.Id);
        _wiz.SetAttribute("JMAP", "%0:%2=%1", _wiz.Id);

        var now = DateTimeOffset.UtcNow;
        var snaps = new List<ConnectionSnapshot>
        {
            new(_wiz.Id, 10, "localhost", "127.0.0.1", "ssl-xterm", now, now)
        };

        _registry = new FunctionRegistry(_db)
        {
            ConnectionsProvider = () => snaps
        };
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;

        _ctx = new EvaluationContext(_wiz.Id, _wiz.Id, _wiz.Id, room.Id);
    }

    [Fact]
    public void ListFunctions_MatchOriginalSemantics()
    {
        // Case-sensitive MEMBER & single-element check
        Assert.Equal("0", _evaluator.Evaluate("[member(Alpha Beta,alpha)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[member(Alpha Beta,Alpha)]", _ctx));
        Assert.Equal("#-1 CAN ONLY TEST ONE ELEMENT", _evaluator.Evaluate("[member(a b,a b)]", _ctx));

        // ITEMS without collapsing spaces
        Assert.Equal("1", _evaluator.Evaluate("[items(, )]", _ctx));
        Assert.Equal("3", _evaluator.Evaluate("[items(a  b, )]", _ctx));

        // REMOVE word list case-sensitive
        Assert.Equal("A c", _evaluator.Evaluate("[remove(A b c d,b d)]", _ctx));

        // SORT auto-detect & osep
        Assert.Equal("2|10|100", _evaluator.Evaluate("[sort(100 2 10,,,|)]", _ctx));
        Assert.Equal("#1|#2|#10", _evaluator.Evaluate("[sort(#10 #2 #1,,,|)]", _ctx));

        // SET* sorted output
        Assert.Equal("1 2 3 10", _evaluator.Evaluate("[setunion(10 2,1 3 2)]", _ctx));
        Assert.Equal("2 10", _evaluator.Evaluate("[setinter(10 2 5,2 10)]", _ctx));
        Assert.Equal("1 5", _evaluator.Evaluate("[setdiff(5 1 2,2)]", _ctx));

        // MAP %1 and FILTER exact "1" + extra args
        Assert.Equal("a:1 b:2", _evaluator.Evaluate("[map(me/MAP_POS,a b)]", _ctx));
        Assert.Equal("2 4", _evaluator.Evaluate("[filter(me/IS_EVEN,1 2 3 4,%b,%b,2)]", _ctx));
    }

    [Fact]
    public void MathFunctions_MatchOriginalSemantics()
    {
        Assert.Equal("4", _evaluator.Evaluate("[bnand(5,1)]", _ctx));
        Assert.Equal(ulong.MaxValue.ToString(), _evaluator.Evaluate("[bnot(0)]", _ctx));
        Assert.Equal("#-1 ARGUMENTS MUST BE UNSIGNED INTEGERS", _evaluator.Evaluate("[band(-1,2)]", _ctx));

        Assert.Equal("foo11", _evaluator.Evaluate("[inc(foo10)]", _ctx));
        Assert.Equal("foo9", _evaluator.Evaluate("[dec(foo10)]", _ctx));
        Assert.Equal("#-1 ARGUMENT MUST END IN AN INTEGER", _evaluator.Evaluate("[inc(abc)]", _ctx));

        Assert.Equal("5", _evaluator.Evaluate("[bound(2,5)]", _ctx));
        Assert.Equal("0 0 0", _evaluator.Evaluate("[vunit(0 0 0)]", _ctx));

        Assert.Equal("#-1 ARGUMENTS MUST BE INTEGERS", _evaluator.Evaluate("[remainder(5.5,2)]", _ctx));
        Assert.Equal("#-1 ARGUMENTS MUST BE INTEGERS", _evaluator.Evaluate("[div(5.5,2)]", _ctx));

        Assert.Equal("1.20", _evaluator.Evaluate("[round(1.2,2,1)]", _ctx));
        Assert.Equal("101", _evaluator.Evaluate("[trunc(101dalmations)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[log(2.718281828459045,e)]", _ctx).Substring(0, 1));
        Assert.Equal("#-1 BASE OUT OF RANGE", _evaluator.Evaluate("[log(10,1)]", _ctx));
    }

    [Fact]
    public void TimeCryptoAndJsonFunctions_MatchOriginalSemantics()
    {
        Assert.Equal("1y  1w  1d  1h  1m  1s", _evaluator.Evaluate("[etime(32230861)]", _ctx));
        Assert.Equal("1y  1w", _evaluator.Evaluate("[etime(32230861,6)]", _ctx));

        Assert.Equal("1", _evaluator.Evaluate("[ssl()]", _ctx));
        Assert.Contains("sha256", _evaluator.Evaluate("[digest(list)]", _ctx));
        Assert.Equal("#-1 UNSUPPORTED DIGEST TYPE", _evaluator.Evaluate("[digest(bogus,hi)]", _ctx));
        Assert.Equal("a+b c", _evaluator.Evaluate("[urldecode({a+b%20c})]", _ctx));

        Assert.Equal("#-1 INVALID VALUE", _evaluator.Evaluate("[json(array,notjson)]", _ctx));
        Assert.Equal("#-1 INVALID TYPE", _evaluator.Evaluate("[json(bogus,1)]", _ctx));
        Assert.Equal("string", _evaluator.Evaluate("[json_query(\"hi\")]", _ctx));
        Assert.Equal("\"Alice\"", _evaluator.Evaluate("[json_query({{\"name\":\"Alice\"}},get,name)]", _ctx));
        Assert.Equal("string:a=Alice", _evaluator.Evaluate("[json_map(me/JMAP,{{\"a\":\"Alice\"}})]", _ctx));
    }
}
