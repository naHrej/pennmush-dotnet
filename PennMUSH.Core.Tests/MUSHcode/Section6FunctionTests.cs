namespace PennMUSH.Core.Tests.MUSHcode;

using Xunit;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class Section6FunctionTests
{
    private readonly ObjectDatabase _db;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly EvaluationContext _wizCtx;
    private readonly MUSHObject _room;
    private readonly MUSHObject _wiz;
    private readonly MUSHObject _alice;
    private readonly MUSHObject _parent;
    private readonly MUSHObject _child;

    public Section6FunctionTests()
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

        _parent = _db.CreateObject("ParentObj", ObjectType.Thing, _wiz.Id);           // #3
        _parent.SetAttribute("P_ALPHA", "Hello World", _wiz.Id, AttributeFlags.Visual);
        _parent.SetAttribute("P_SECRET", "Hello Private", _wiz.Id, AttributeFlags.Private | AttributeFlags.Visual);

        _child = _db.CreateObject("ChildObj", ObjectType.Thing, _wiz.Id);             // #4
        _child.AddParent(_parent.Id, _db);
        _child.AddFlag("DARK");
        _child.SetAttribute("A_ONE", "Magic Spell", _wiz.Id, AttributeFlags.Visual);
        _child.SetAttribute("A_TWO", "magic potion", _wiz.Id, AttributeFlags.Visual);
        _child.SetAttribute("B_NUM", "12345", _wiz.Id, AttributeFlags.Visual);
        _db.MoveObject(_child.Id, _room.Id);

        _wiz.SetAttribute("IS_TRUTHY", "[%0]", _wiz.Id);
        _wiz.SetAttribute("IS_EVEN", "[eq(mod(%0,2),0)]", _wiz.Id);
        _wiz.SetAttribute("CHAR_STEP", "[%0:%1]", _wiz.Id);
        _wiz.SetAttribute("ADD_ONE", "[add(%0,1)]", _wiz.Id);

        _registry = new FunctionRegistry(_db);
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;

        _wizCtx = new EvaluationContext(_wiz.Id, _wiz.Id, _wiz.Id, _room.Id);
    }

    [Fact]
    public void Section6_GrepFamily_WorksCorrectly()
    {
        Assert.Equal("A_ONE", _evaluator.Evaluate($"[grep({_child.Id},*,Magic)]", _wizCtx));
        Assert.Equal("A_ONE A_TWO", _evaluator.Evaluate($"[grepi({_child.Id},*,magic)]", _wizCtx));
        Assert.Equal("P_ALPHA", _evaluator.Evaluate($"[pgrep({_child.Id},*,Hello)]", _wizCtx));
        Assert.Equal("A_ONE", _evaluator.Evaluate($"[wildgrep({_child.Id},*,*Spell)]", _wizCtx));
        Assert.Equal("A_ONE A_TWO", _evaluator.Evaluate($"[wildgrepi({_child.Id},*,*MAGIC*)]", _wizCtx));
    }

    [Fact]
    public void Section6_RegLattrFamily_WorksCorrectly()
    {
        Assert.Equal("A_ONE A_TWO", _evaluator.Evaluate($"[reglattr({_child.Id}/^A_)]", _wizCtx));
        Assert.Equal("A_ONE|A_TWO", _evaluator.Evaluate($"[reglattr({_child.Id}/^A_,|)]", _wizCtx));
        Assert.Equal("A_ONE A_TWO P_ALPHA", _evaluator.Evaluate($"[reglattrp({_child.Id}/^(A_|P_ALPHA))]", _wizCtx));
        Assert.Equal("2", _evaluator.Evaluate($"[regnattr({_child.Id}/^A_)]", _wizCtx));
        Assert.Equal("3", _evaluator.Evaluate($"[regnattrp({_child.Id}/^(A_|P_ALPHA))]", _wizCtx));
        Assert.Equal("A_TWO B_NUM", _evaluator.Evaluate($"[regxattr({_child.Id}/.,2,2)]", _wizCtx));
        Assert.Equal("P_ALPHA", _evaluator.Evaluate($"[regxattrp({_child.Id}/^P_,1,1)]", _wizCtx));
    }

    [Fact]
    public void Section6_LSearchLockFilterFilterBoolForEach_WorkCorrectly()
    {
        // lsearch / lsearchr / nlsearch / nsearch
        Assert.Equal($"{_parent.Id} {_child.Id}", _evaluator.Evaluate("[lsearch(all,type,thing)]", _wizCtx));
        Assert.Equal($"{_child.Id} {_parent.Id}", _evaluator.Evaluate("[lsearchr(all,type,thing)]", _wizCtx));
        Assert.Equal("2", _evaluator.Evaluate("[nlsearch(all,type,thing)]", _wizCtx));
        Assert.Equal("2", _evaluator.Evaluate("[nsearch(all,type,thing)]", _wizCtx));
        Assert.Equal(_child.Id.ToString(), _evaluator.Evaluate("[lsearch(all,flags,D,type,thing)]", _wizCtx));
        Assert.Equal(_child.Id.ToString(), _evaluator.Evaluate("[lsearch(all,eval,{[strmatch(name(##),Child*)]})]", _wizCtx));

        // lockfilter
        string refs = $"{_wiz.Id} {_alice.Id} {_child.Id}";
        Assert.Equal(_alice.Id.ToString(), _evaluator.Evaluate($"[lockfilter({_alice.Id},{refs})]", _wizCtx));
        Assert.Equal($"{_wiz.Id}|{_child.Id}", _evaluator.Evaluate($"[lockfilter(!{_alice.Id},{refs.Replace(' ', '|')},|)]", _wizCtx));

        // filterbool
        Assert.Equal("yes 2", _evaluator.Evaluate("[filterbool(me/IS_TRUTHY,yes 0 {} 2)]", _wizCtx));
        Assert.Equal("2|4", _evaluator.Evaluate("[filterbool(me/IS_EVEN,1 2 3 4, ,|)]", _wizCtx));

        // foreach
        Assert.Equal("65432", _evaluator.Evaluate("[foreach(me/ADD_ONE,54321)]", _wizCtx));
        Assert.Equal("a:0b:1c:2", _evaluator.Evaluate("[foreach(me/CHAR_STEP,abc)]", _wizCtx));
        Assert.Equal("Pre-23-Post", _evaluator.Evaluate("[foreach(me/ADD_ONE,Pre-#12#-Post,#,#)]", _wizCtx));
    }
}
