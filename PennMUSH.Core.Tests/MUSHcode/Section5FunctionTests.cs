namespace PennMUSH.Core.Tests.MUSHcode;

using System;
using System.Collections.Generic;
using Xunit;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Locks;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.MUSHcode.Functions;
using PennMUSH.Core.Types;

public class Section5FunctionTests
{
    private readonly ObjectDatabase _db;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly EvaluationContext _wizCtx;
    private readonly EvaluationContext _aliceCtx;
    private readonly EvaluationContext _bobCtx;
    private readonly MUSHObject _room;
    private readonly MUSHObject _room2;
    private readonly MUSHObject _wiz;
    private readonly MUSHObject _alice;
    private readonly MUSHObject _bob;
    private readonly MUSHObject _parentThing;
    private readonly MUSHObject _childThing;
    private readonly MUSHObject _darkThing;
    private readonly MUSHObject _exitNorth;
    private readonly MUSHObject _darkExit;

    public Section5FunctionTests()
    {
        _db = new ObjectDatabase();
        _room = _db.CreateObject("Start Room", ObjectType.Room, Dbref.Nothing);       // #0
        _wiz = _db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);          // #1
        _wiz.Owner = _wiz.Id;
        _wiz.AddFlag("WIZARD");
        _db.MoveObject(_wiz.Id, _room.Id);

        _room2 = _db.CreateObject("Other Room", ObjectType.Room, _wiz.Id);            // #2
        _alice = _db.CreateObject("Alice", ObjectType.Player, _wiz.Id);               // #3
        _alice.Owner = _alice.Id;
        _alice.Pennies = 250;
        _alice.SetAttribute("SEX", "female", _alice.Id);
        _alice.SetAttribute("MONIKER", "Lady Alice", _alice.Id);
        _alice.SetAttribute("DOING", "Testing softcode", _alice.Id);
        _alice.SetAttribute("ALIAS", "Ali", _alice.Id);
        _alice.SetAttribute("SCREENHEIGHT", "40", _alice.Id);
        _alice.SetAttribute("SCREENWIDTH", "120", _alice.Id);
        _db.MoveObject(_alice.Id, _room.Id);
        _db.SetConnected(_alice.Id, true);

        _bob = _db.CreateObject("Bob", ObjectType.Player, _wiz.Id);                   // #4
        _bob.Owner = _bob.Id;
        _bob.AddFlag("DARK");
        _bob.AddFlag("UNFINDABLE");
        _db.MoveObject(_bob.Id, _room.Id);

        _parentThing = _db.CreateObject("ParentBox", ObjectType.Thing, _alice.Id);    // #5
        _parentThing.SetAttribute("INHERITED", "from-parent", _alice.Id, AttributeFlags.Visual);
        _parentThing.SetAttribute("SECRET_P", "hidden-parent", _alice.Id, AttributeFlags.Private | AttributeFlags.Visual);
        _parentThing.SetAttribute("EMPTYATTR", "", _alice.Id, AttributeFlags.Visual);

        _childThing = _db.CreateObject("ChildBox", ObjectType.Thing, _alice.Id);      // #6
        _childThing.AddParent(_parentThing.Id, _db);
        _childThing.AddFlag("PUPPET");
        _childThing.SetAttribute("OWN", "own-value", _alice.Id, AttributeFlags.Visual);
        _childThing.SetAttribute("EMPTYOWN", "", _alice.Id, AttributeFlags.Visual);
        _db.MoveObject(_childThing.Id, _room.Id);

        _darkThing = _db.CreateObject("Shadow Orb", ObjectType.Thing, _wiz.Id);       // #7
        _darkThing.AddFlag("DARK");
        _darkThing.AddPower("Builder");
        _db.MoveObject(_darkThing.Id, _room.Id);

        _exitNorth = _db.CreateObject("North;n;out", ObjectType.Exit, _wiz.Id);       // #8
        _db.AttachExit(_room.Id, _exitNorth.Id);
        _db.LinkExit(_exitNorth.Id, _room2.Id);

        _darkExit = _db.CreateObject("Secret;sec", ObjectType.Exit, _wiz.Id);         // #9
        _darkExit.AddFlag("DARK");
        _db.AttachExit(_room.Id, _darkExit.Id);
        _db.LinkExit(_darkExit.Id, _room2.Id);

        _registry = new FunctionRegistry(_db);
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;

        _wizCtx = new EvaluationContext(_wiz.Id, _wiz.Id, _wiz.Id, _room.Id);
        _aliceCtx = new EvaluationContext(_alice.Id, _alice.Id, _alice.Id, _room.Id);
        _bobCtx = new EvaluationContext(_bob.Id, _bob.Id, _bob.Id, _room.Id);
    }

    [Fact]
    public void Section5_PronounEconomyIdentity_WorkCorrectly()
    {
        Assert.Equal("hers", _evaluator.Evaluate($"[aposs({_alice.Id})]", _wizCtx));
        Assert.Equal("his", _evaluator.Evaluate("[aposs(male)]", _wizCtx));
        Assert.Equal("250", _evaluator.Evaluate($"[money({_alice.Id})]", _wizCtx));
        Assert.Equal("Lady Alice", _evaluator.Evaluate($"[moniker({_alice.Id})]", _wizCtx));
        Assert.Equal("Lady Alice", _evaluator.Evaluate($"[cname({_alice.Id})]", _wizCtx));
        Assert.Equal("Wizard", _evaluator.Evaluate($"[moniker({_wiz.Id})]", _wizCtx));
        Assert.Equal("Testing softcode", _evaluator.Evaluate($"[doing({_alice.Id})]", _wizCtx));
        Assert.Equal("Ali", _evaluator.Evaluate($"[alias({_alice.Id})]", _wizCtx));
        Assert.Equal("Ali", _evaluator.Evaluate($"[fullalias({_alice.Id})]", _wizCtx));
        Assert.Equal("n", _evaluator.Evaluate($"[alias({_exitNorth.Id})]", _wizCtx));
        Assert.Equal("n;out", _evaluator.Evaluate($"[fullalias({_exitNorth.Id})]", _wizCtx));
        Assert.Equal("40", _evaluator.Evaluate($"[height({_alice.Id})]", _wizCtx));
        Assert.Equal("120", _evaluator.Evaluate($"[width({_alice.Id})]", _wizCtx));
        Assert.Equal("24", _evaluator.Evaluate($"[height({_bob.Id})]", _wizCtx));
        Assert.Equal("78", _evaluator.Evaluate($"[width({_bob.Id})]", _wizCtx));
    }

    [Fact]
    public void Section5_LockFunctions_WorkCorrectly()
    {
        _childThing.SetLock(LockType.Basic, new DbrefLockNode(_alice.Id), _alice.Id, LockFlags.Visual | LockFlags.Locked);
        _childThing.SetLock(LockType.Enter, new ConstantLockNode(true), _alice.Id, LockFlags.Private);

        Assert.Contains("Basic", _evaluator.Evaluate($"[locks({_childThing.Id})]", _aliceCtx));
        Assert.Contains("Teleport", _evaluator.Evaluate("[llocks()]", _aliceCtx));
        Assert.Equal("vl", _evaluator.Evaluate($"[lockflags({_childThing.Id}/Basic)]", _aliceCtx));
        Assert.Equal("visual locked", _evaluator.Evaluate($"[llockflags({_childThing.Id}/Basic)]", _aliceCtx));
        Assert.Equal("ovpwln", _evaluator.Evaluate("[lockflags()]", _aliceCtx));
        Assert.Equal("owner visual private wizard locked noclone", _evaluator.Evaluate("[llockflags()]", _aliceCtx));
        Assert.Equal(_alice.Id.ToString(), _evaluator.Evaluate($"[lockowner({_childThing.Id}/Basic)]", _aliceCtx));
        Assert.Equal(_alice.Id.ToString(), _evaluator.Evaluate($"[lock({_childThing.Id}/Basic)]", _aliceCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[elock({_childThing.Id}/Basic,{_alice.Id})]", _aliceCtx));
        Assert.Equal("0", _evaluator.Evaluate($"[elock({_childThing.Id}/Basic,{_bob.Id})]", _aliceCtx));

        // Private lock is hidden from Bob (non-owner, non-wizard)
        Assert.Equal("#-1", _evaluator.Evaluate($"[elock({_childThing.Id}/Enter,{_bob.Id})]", _bobCtx));
        Assert.Equal("#-1 NO SUCH LOCK", _evaluator.Evaluate($"[lockflags({_childThing.Id}/Enter)]", _bobCtx));

        // Lock mutation via lock() and atrlock()
        Assert.Equal(_bob.Id.ToString(), _evaluator.Evaluate($"[lock({_childThing.Id}/Use,{_bob.Id})]", _aliceCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[elock({_childThing.Id}/Use,{_bob.Id})]", _aliceCtx));
        Assert.Equal("0", _evaluator.Evaluate($"[atrlock({_childThing.Id}/OWN)]", _aliceCtx));
        Assert.Equal(string.Empty, _evaluator.Evaluate($"[atrlock({_childThing.Id}/OWN,on)]", _aliceCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[atrlock({_childThing.Id}/OWN)]", _aliceCtx));
    }

    [Fact]
    public void Section5_AttributeAndBranchPermissions_WorkCorrectly()
    {
        // Own vs parent attributes
        Assert.Equal("1", _evaluator.Evaluate($"[hasattr({_childThing.Id},OWN)]", _bobCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[hasattrval({_childThing.Id},OWN)]", _bobCtx));
        Assert.Equal("0", _evaluator.Evaluate($"[hasattrval({_childThing.Id},EMPTYOWN)]", _bobCtx));
        Assert.Equal("0", _evaluator.Evaluate($"[hasattr({_childThing.Id},INHERITED)]", _bobCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[hasattrp({_childThing.Id},INHERITED)]", _bobCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[hasattrpval({_childThing.Id},INHERITED)]", _bobCtx));
        Assert.Equal("0", _evaluator.Evaluate($"[hasattrpval({_childThing.Id},EMPTYATTR)]", _bobCtx));
        Assert.Equal("0", _evaluator.Evaluate($"[hasattrp({_childThing.Id},SECRET_P)]", _bobCtx));

        Assert.Contains("INHERITED", _evaluator.Evaluate($"[lattrp({_childThing.Id})]", _bobCtx));
        Assert.DoesNotContain("INHERITED", _evaluator.Evaluate($"[lattr({_childThing.Id})]", _bobCtx));
        Assert.True(int.Parse(_evaluator.Evaluate($"[nattrp({_childThing.Id})]", _bobCtx)) >
                    int.Parse(_evaluator.Evaluate($"[nattr({_childThing.Id})]", _bobCtx)));
        Assert.Equal("EMPTYOWN OWN", _evaluator.Evaluate($"[xattr({_childThing.Id},*,1,2)]", _bobCtx));
        Assert.Contains("INHERITED", _evaluator.Evaluate($"[xattrp({_childThing.Id},INHERITED,1,1)]", _bobCtx));

        // Branch restrictions:
        // 1) MDark on branch hides Visual child from Bob, but Wizard still sees it
        _childThing.SetAttribute("SECBRANCH", " ", _alice.Id, AttributeFlags.Root | AttributeFlags.MDark | AttributeFlags.Visual);
        _childThing.SetAttribute("SECBRANCH`LEAF", "secret-val", _alice.Id, AttributeFlags.Visual);

        Assert.Equal(string.Empty, _evaluator.Evaluate($"[get({_childThing.Id}/SECBRANCH`LEAF)]", _bobCtx));
        Assert.Equal(string.Empty, _evaluator.Evaluate($"[xget({_childThing.Id},SECBRANCH`LEAF)]", _bobCtx));
        Assert.Equal("0", _evaluator.Evaluate($"[hasattr({_childThing.Id},SECBRANCH`LEAF)]", _bobCtx));
        Assert.DoesNotContain("SECBRANCH`LEAF", _evaluator.Evaluate($"[lattr({_childThing.Id},SECBRANCH*)]", _bobCtx));

        Assert.Equal("secret-val", _evaluator.Evaluate($"[get({_childThing.Id}/SECBRANCH`LEAF)]", _wizCtx));

        // 2) Visual on branch does NOT grant access to non-Visual child
        _childThing.SetAttribute("PUBBRANCH", " ", _alice.Id, AttributeFlags.Root | AttributeFlags.Visual);
        _childThing.SetAttribute("PUBBRANCH`PRIVLEAF", "priv-val", _alice.Id, AttributeFlags.None);

        Assert.Equal(string.Empty, _evaluator.Evaluate($"[get({_childThing.Id}/PUBBRANCH`PRIVLEAF)]", _bobCtx));
        Assert.Equal("0", _evaluator.Evaluate($"[hasattr({_childThing.Id},PUBBRANCH`PRIVLEAF)]", _bobCtx));
        Assert.DoesNotContain("PUBBRANCH`PRIVLEAF", _evaluator.Evaluate($"[lattr({_childThing.Id},PUBBRANCH*)]", _bobCtx));
        Assert.Contains("PUBBRANCH", _evaluator.Evaluate($"[lattr({_childThing.Id},PUBBRANCH*)]", _bobCtx));

        // 3) examine respects branch restrictions for Bob
        var examine = new ExamineCommandHandler();
        var cmdCtx = new CommandContext
        {
            Executor = _bob.Id,
            Enactor = _bob.Id,
            CommandName = "examine",
            Arguments = _childThing.Id.ToString()
        };
        Assert.True(examine.Execute(cmdCtx, _db));
        string examined = string.Join("\n", cmdCtx.Notifications);
        Assert.DoesNotContain("SECBRANCH`LEAF", examined);
        Assert.DoesNotContain("PUBBRANCH`PRIVLEAF", examined);
        Assert.Contains("OWN", examined);
    }

    [Fact]
    public void Section5_FlagsPowersGraphAndLocate_WorkCorrectly()
    {
        Assert.Equal("1", _evaluator.Evaluate($"[andflags({_darkThing.Id},TD!P)]", _wizCtx));
        Assert.Equal("0", _evaluator.Evaluate($"[andflags({_darkThing.Id},TP)]", _wizCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[orflags({_darkThing.Id},PW)]", _wizCtx) == "0" ? "1" : "0");
        Assert.Equal("1", _evaluator.Evaluate($"[orflags({_darkThing.Id},PD)]", _wizCtx));

        Assert.Equal("1", _evaluator.Evaluate($"[andlflags({_darkThing.Id},THING DARK !PLAYER)]", _wizCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[orlflags({_darkThing.Id},WIZARD DARK)]", _wizCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[andlpowers({_darkThing.Id},Builder !Boot)]", _wizCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[orlpowers({_darkThing.Id},Boot Builder)]", _wizCtx));

        Assert.Equal(_childThing.Id.ToString(), _evaluator.Evaluate($"[children({_parentThing.Id})]", _wizCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[nchildren({_parentThing.Id})]", _wizCtx));
        Assert.Contains(_exitNorth.Id.ToString(), _evaluator.Evaluate($"[entrances({_room2.Id},E)]", _wizCtx));

        Assert.Equal(_alice.Id.ToString(), _evaluator.Evaluate($"[locate({_wiz.Id},Alice,*)]", _wizCtx));
        Assert.Equal(_exitNorth.Id.ToString(), _evaluator.Evaluate($"[locate({_wiz.Id},n,eE)]", _wizCtx));
        Assert.Equal(_alice.Id.ToString(), _evaluator.Evaluate("[pmatch(Ali)]", _wizCtx));
        Assert.Equal(_alice.Id.ToString(), _evaluator.Evaluate($"[player({_alice.Id})]", _wizCtx));
        Assert.Equal(_room.Id.ToString(), _evaluator.Evaluate($"[room({_childThing.Id})]", _wizCtx));
        Assert.True(DbFunctions.ParseDbref(_evaluator.Evaluate($"[exit({_room.Id})]", _wizCtx)).IsValid);
        Assert.True(DbFunctions.ParseDbref(_evaluator.Evaluate($"[next({_childThing.Id})]", _wizCtx)).IsValid);
        Assert.Equal(_db.Top.ToString(), _evaluator.Evaluate("[nextdbref()]", _wizCtx));
    }

    [Fact]
    public void Section5_DbWalkersVisibilityMemoryAndStats_WorkCorrectly()
    {
        Assert.Contains(_alice.Id.ToString(), _evaluator.Evaluate($"[lcon({_room.Id})]", _wizCtx));
        Assert.Equal(_alice.Id.ToString(), _evaluator.Evaluate($"[lcon({_room.Id},connect)]", _wizCtx));
        Assert.Equal(_childThing.Id.ToString(), _evaluator.Evaluate($"[lcon({_room.Id},puppet)]", _wizCtx));
        Assert.Contains(_bob.Id.ToString(), _evaluator.Evaluate($"[lplayers({_room.Id})]", _wizCtx));
        Assert.Contains(_darkThing.Id.ToString(), _evaluator.Evaluate($"[lthings({_room.Id})]", _wizCtx));

        Assert.Equal("5", _evaluator.Evaluate($"[ncon({_room.Id})]", _wizCtx));
        Assert.Equal("2", _evaluator.Evaluate($"[nexits({_room.Id})]", _wizCtx));
        Assert.Equal("3", _evaluator.Evaluate($"[nplayers({_room.Id})]", _wizCtx));
        Assert.Equal("2", _evaluator.Evaluate($"[nthings({_room.Id})]", _wizCtx));

        // Visible walkers for Alice exclude DARK objects (Bob, Shadow Orb, Secret exit)
        Assert.DoesNotContain(_bob.Id.ToString(), _evaluator.Evaluate($"[lvplayers({_room.Id})]", _aliceCtx));
        Assert.DoesNotContain(_darkThing.Id.ToString(), _evaluator.Evaluate($"[lvthings({_room.Id})]", _aliceCtx));
        Assert.DoesNotContain(_darkThing.Id.ToString(), _evaluator.Evaluate($"[lvcon({_room.Id})]", _aliceCtx));
        Assert.Equal(_exitNorth.Id.ToString(), _evaluator.Evaluate($"[lvexits({_room.Id})]", _aliceCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[nvexits({_room.Id})]", _aliceCtx));
        Assert.Equal("2", _evaluator.Evaluate($"[nvplayers({_room.Id})]", _aliceCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[nvthings({_room.Id})]", _aliceCtx));
        Assert.Equal("3", _evaluator.Evaluate($"[nvcon({_room.Id})]", _aliceCtx));

        // Slicing walkers x* and xv*
        Assert.Equal("2", _evaluator.Evaluate($"[words([xcon({_room.Id},1,2)])]", _wizCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[words([xexits({_room.Id},1,1)])]", _wizCtx));
        Assert.Equal("2", _evaluator.Evaluate($"[words([xplayers({_room.Id},1,2)])]", _wizCtx));
        Assert.Equal("2", _evaluator.Evaluate($"[words([xthings({_room.Id},1,2)])]", _wizCtx));
        Assert.Equal("2", _evaluator.Evaluate($"[words([xvcon({_room.Id},1,2)])]", _aliceCtx));
        Assert.Equal(_exitNorth.Id.ToString(), _evaluator.Evaluate($"[xvexits({_room.Id},1,1)]", _aliceCtx));
        Assert.Equal("2", _evaluator.Evaluate($"[words([xvplayers({_room.Id},1,2)])]", _aliceCtx));
        Assert.Equal(_childThing.Id.ToString(), _evaluator.Evaluate($"[xvthings({_room.Id},1,1)]", _aliceCtx));

        // Visibility, memory, stats
        Assert.Equal("0", _evaluator.Evaluate($"[findable({_alice.Id},{_bob.Id})]", _aliceCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[findable({_wiz.Id},{_bob.Id})]", _wizCtx));
        Assert.Equal("1", _evaluator.Evaluate($"[hidden({_bob.Id})]", _wizCtx));
        Assert.Equal("#-1", _evaluator.Evaluate($"[hidden({_bob.Id})]", _aliceCtx));

        Assert.True(int.Parse(_evaluator.Evaluate($"[objmem({_alice.Id})]", _wizCtx)) > 64);
        Assert.True(int.Parse(_evaluator.Evaluate($"[playermem({_alice.Id})]", _wizCtx)) >=
                    int.Parse(_evaluator.Evaluate($"[objmem({_alice.Id})]", _wizCtx)));
        Assert.Equal("10 2 2 3 3 0", _evaluator.Evaluate("[lstats(all)]", _wizCtx));
    }
}
