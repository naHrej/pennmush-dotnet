namespace PennMUSH.Core.Tests.MUSHcode;

using System;
using System.Collections.Generic;
using Xunit;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.MUSHcode.Functions;
using PennMUSH.Core.Types;

public class Pr4AnsiStringsTextFormatTests
{
    private readonly ObjectDatabase _db;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly EvaluationContext _wizCtx;
    private readonly EvaluationContext _mortalCtx;
    private readonly MUSHObject _room;
    private readonly MUSHObject _wiz;
    private readonly MUSHObject _alice;

    public Pr4AnsiStringsTextFormatTests()
    {
        _db = new ObjectDatabase();
        _room = _db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);             // #0
        _wiz = _db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);          // #1
        _wiz.Owner = _wiz.Id;
        _wiz.AddFlag("WIZARD");
        _db.MoveObject(_wiz.Id, _room.Id);

        _alice = _db.CreateObject("Alice", ObjectType.Player, _wiz.Id);               // #2
        _alice.Owner = _alice.Id;
        _db.MoveObject(_alice.Id, _room.Id);

        var now = DateTimeOffset.UtcNow;
        var snaps = new List<ConnectionSnapshot>
        {
            new(_wiz.Id, 10, "localhost", "127.0.0.1", "xterm-256color", now, now),
            new(_alice.Id, 11, "alice", "10.0.0.2", "pueblo", now, now)
        };

        _registry = new FunctionRegistry(_db)
        {
            NotificationDispatcher = _ => { },
            ConnectionsProvider = () => snaps
        };
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;

        _wizCtx = new EvaluationContext(_wiz.Id, _wiz.Id, _wiz.Id, _room.Id);
        _mortalCtx = new EvaluationContext(_alice.Id, _alice.Id, _alice.Id, _room.Id);
    }

    [Fact]
    public void AnsiAwareStringFunctions_PreserveMarkupAndVisibleLengths()
    {
        string colored = _evaluator.Evaluate("[ansi(r,hello)]", _wizCtx);
        Assert.Equal("5", _evaluator.Evaluate("[strlen(ansi(r,hello))]", _wizCtx));
        Assert.Equal("\x1B[31mel\x1B[0m", _evaluator.Evaluate("[mid(ansi(r,hello),1,2)]", _wizCtx));
        Assert.Equal("\x1B[31mll\x1B[0m", _evaluator.Evaluate("[mid(ansi(r,hello),3,-2)]", _wizCtx));
        Assert.Equal(colored + "   ", _evaluator.Evaluate("[ljust(ansi(r,hello),8)]", _wizCtx));

        // LJUST/RJUST do not truncate unless 4th arg is truthy; CENTER never truncates
        Assert.Equal("abcdef", _evaluator.Evaluate("[ljust(abcdef,3)]", _wizCtx));
        Assert.Equal("abc", _evaluator.Evaluate("[ljust(abcdef,3, ,1)]", _wizCtx));
        Assert.Equal("abcdef", _evaluator.Evaluate("[center(abcdef,3)]", _wizCtx));
        Assert.Equal("..hi==", _evaluator.Evaluate("[center(hi,6,.,=)]", _wizCtx));
    }

    [Fact]
    public void StringFunctions_MatchOriginalSemantics()
    {
        // Case-sensitive BEFORE/AFTER & default space
        Assert.Equal("Foo:bar", _evaluator.Evaluate("[before(Foo:bar,B)]", _wizCtx));
        Assert.Equal("foo", _evaluator.Evaluate("[before(foo bar)]", _wizCtx));
        Assert.Equal("bar", _evaluator.Evaluate("[after(foo bar)]", _wizCtx));

        // 0-based LPOS
        Assert.Equal("0 2 4", _evaluator.Evaluate("[lpos(ababab,a)]", _wizCtx));

        // COMP types
        Assert.Equal("-1", _evaluator.Evaluate("[comp(2,10,N)]", _wizCtx));
        Assert.Equal("0", _evaluator.Evaluate("[comp(Abc,abc,I)]", _wizCtx));

        // EDIT ^ and $
        Assert.Equal("pre-mid-post", _evaluator.Evaluate("[edit(mid,^,pre-,$, -post)]", _wizCtx).Replace(" ", ""));

        // TABLE with alignment and osep
        Assert.Equal("  a|  b", _evaluator.Evaluate("[table(a b,>3,20, ,|)]", _wizCtx));

        // WRAP with width1st and linesep
        Assert.Equal("one|two|three", _evaluator.Evaluate("[wrap(one two three,5,5,|)]", _wizCtx));

        // SECURE & ESCAPE
        Assert.Equal("a b c", _evaluator.Evaluate("[secure({a[b]c})]", _wizCtx));
        Assert.Equal(@"\a\[b\]", _evaluator.Evaluate("[escape({a[b]})]", _wizCtx));
    }

    [Fact]
    public void TextFormatFunctions_MatchOriginalSemantics()
    {
        // 256-color and hex RGB ANSI
        Assert.Equal("\x1B[38;5;196mhi\x1B[0m", _evaluator.Evaluate("[ansi(xterm196,hi)]", _wizCtx));
        Assert.Equal("\x1B[38;2;255;0;128mhi\x1B[0m", _evaluator.Evaluate("[ansi(#ff0080,hi)]", _wizCtx));
        Assert.Equal("#-1 INVALID ANSI DEFINITION", _evaluator.Evaluate("[ansi(???,hi)]", _wizCtx));
        Assert.Equal(string.Empty, _evaluator.Evaluate("[ansi(r,)]", _wizCtx));

        // COLORS (legacy 16-color vs full X11/CSS color table)
        Assert.Equal("#cd0000", _evaluator.Evaluate("[colors(r,hex)]", _wizCtx));
        Assert.Equal("205 0 0", _evaluator.Evaluate("[colors(r,rgb)]", _wizCtx));
        Assert.Equal("#ff0000", _evaluator.Evaluate("[colors(red,hex)]", _wizCtx));
        Assert.Equal("255 0 0", _evaluator.Evaluate("[colors(red,rgb)]", _wizCtx));

        // ACCENT equal length error
        Assert.Equal("#-1 STRING LENGTHS MUST BE EQUAL", _evaluator.Evaluate("[accent(ab,')]", _wizCtx));

        // RENDER flags & permissions
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate("[render(hi,ansi)]", _mortalCtx));
        Assert.Equal("#-1 INVALID SECOND ARGUMENT", _evaluator.Evaluate("[render(hi,bogus)]", _wizCtx));

        // PUEBLO & OOB
        Assert.Equal("0", _evaluator.Evaluate($"[pueblo({_wiz.Id})]", _wizCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[pueblo({_alice.Id})]", _wizCtx));
        Assert.Equal("#-1 INVALID JSON", _evaluator.Evaluate($"[oob({_alice.Id},GMCP,notjson)]", _wizCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[oob({_alice.Id},GMCP,[json(object,ok,1)])]", _wizCtx));
    }
}
