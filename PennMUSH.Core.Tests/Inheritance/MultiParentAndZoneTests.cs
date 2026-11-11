namespace PennMUSH.Core.Tests.Inheritance;

using System;
using Xunit;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class MultiParentAndZoneTests
{
    private readonly ObjectDatabase _db;
    private readonly CommandDispatcher _dispatcher;
    private readonly ExpressionEvaluator _evaluator;
    private readonly MUSHObject _roomZero;
    private readonly MUSHObject _god;
    private readonly MUSHObject _player;
    private readonly MUSHObject _target;

    public MultiParentAndZoneTests()
    {
        _db = new ObjectDatabase();
        var registry = new FunctionRegistry(_db);
        _evaluator = new ExpressionEvaluator(registry.Dispatch, registry.Database);
        registry.Evaluator = _evaluator;

        _roomZero = _db.CreateObject("Room Zero", ObjectType.Room, new Dbref(1));
        _god = _db.CreateObject("God", ObjectType.Player, new Dbref(1));
        _player = _db.CreateObject("Wizard", ObjectType.Player, _god.Id);
        _target = _db.CreateObject("Hero", ObjectType.Player, _god.Id);

        _dispatcher = new CommandDispatcher();
        _dispatcher.Register(new ParentCommandHandler());
        _dispatcher.Register(new ZoneCommandHandler());
    }

    [Fact]
    public void MultiParent_InheritsAttributesFromMultipleParents()
    {
        var archetypeA = _db.CreateObject("Combatant Archetype", ObjectType.Thing, _god.Id);
        archetypeA.SetAttribute("ATTACK", "Sword Strike", _god.Id);
        archetypeA.SetAttribute("ROLE", "Warrior", _god.Id);

        var archetypeB = _db.CreateObject("Scholar Archetype", ObjectType.Thing, _god.Id);
        archetypeB.SetAttribute("DEFENSE", "Arcane Shield", _god.Id);
        archetypeB.SetAttribute("ROLE", "Mage", _god.Id); // Overridden by A because A is first

        var ctx = _dispatcher.Dispatch(_god.Id, $"@parent {_target.Id}={archetypeA.Id} {archetypeB.Id}", _db);
        Assert.Contains("Parent set.", ctx.Notifications);

        Assert.Equal(2, _target.Parents.Count);
        Assert.Equal(archetypeA.Id, _target.Parents[0]);
        Assert.Equal(archetypeB.Id, _target.Parents[1]);

        // Inherited from A
        Assert.Equal("Sword Strike", _target.GetAttribute("ATTACK", _db)?.Value);
        // Inherited from B
        Assert.Equal("Arcane Shield", _target.GetAttribute("DEFENSE", _db)?.Value);
        // Precedence: A is first in list, so Warrior wins over Mage
        Assert.Equal("Warrior", _target.GetAttribute("ROLE", _db)?.Value);
    }

    [Fact]
    public void MultiParent_LocalAttributeOverridesInherited()
    {
        var parent = _db.CreateObject("Generic NPC", ObjectType.Thing, _god.Id);
        parent.SetAttribute("GREETING", "Hello stranger.", _god.Id);

        _target.SetParents(new[] { parent.Id }, _db);
        Assert.Equal("Hello stranger.", _target.GetAttribute("GREETING", _db)?.Value);

        // Local override
        _target.SetAttribute("GREETING", "Greetings, noble champion!", _god.Id);
        Assert.Equal("Greetings, noble champion!", _target.GetAttribute("GREETING", _db)?.Value);
    }

    [Fact]
    public void TransitiveInheritance_ResolvesAncestors()
    {
        var grandparent = _db.CreateObject("Base Entity", ObjectType.Thing, _god.Id);
        grandparent.SetAttribute("CORE_VERSION", "1.0", _god.Id);

        var parent = _db.CreateObject("Derived Entity", ObjectType.Thing, _god.Id);
        parent.SetParents(new[] { grandparent.Id }, _db);

        _target.SetParents(new[] { parent.Id }, _db);

        Assert.Equal("1.0", _target.GetAttribute("CORE_VERSION", _db)?.Value);
    }

    [Fact]
    public void ParentCycleDetection_PreventsLoops()
    {
        var objA = _db.CreateObject("Object A", ObjectType.Thing, _god.Id);
        var objB = _db.CreateObject("Object B", ObjectType.Thing, _god.Id);
        var objC = _db.CreateObject("Object C", ObjectType.Thing, _god.Id);

        objA.SetParents(new[] { objB.Id }, _db);
        objB.SetParents(new[] { objC.Id }, _db);

        // Self parent
        var selfCtx = _dispatcher.Dispatch(_god.Id, $"@parent {objA.Id}={objA.Id}", _db);
        Assert.Contains("You would create a loop in the parent tree.", selfCtx.Notifications);

        // Transitive cycle: C tries to parent to A (since A -> B -> C)
        var cycleCtx = _dispatcher.Dispatch(_god.Id, $"@parent {objC.Id}={objA.Id}", _db);
        Assert.Contains("You would create a loop in the parent tree.", cycleCtx.Notifications);
        Assert.Empty(objC.Parents);
    }

    [Fact]
    public void ParentCommand_ClearParents()
    {
        var parent = _db.CreateObject("Template", ObjectType.Thing, _god.Id);
        _target.SetParents(new[] { parent.Id }, _db);
        Assert.Single(_target.Parents);

        var ctx = _dispatcher.Dispatch(_god.Id, $"@parent {_target.Id}=none", _db);
        Assert.Contains("Parent cleared.", ctx.Notifications);
        Assert.Empty(_target.Parents);
        Assert.Equal(Dbref.Nothing, _target.Parent);
    }

    [Fact]
    public void MultiZone_AssignsMultipleZonesAndChecksMembership()
    {
        var zoneCity = _db.CreateObject("City Zone", ObjectType.Thing, _god.Id);
        var zoneWeather = _db.CreateObject("Rainy Weather Zone", ObjectType.Thing, _god.Id);

        var ctx = _dispatcher.Dispatch(_god.Id, $"@zone {_target.Id}={zoneCity.Id} {zoneWeather.Id}", _db);
        Assert.Contains("Zone set.", ctx.Notifications);

        Assert.Equal(2, _target.Zones.Count);
        Assert.True(_target.InZone(zoneCity.Id));
        Assert.True(_target.InZone(zoneWeather.Id));

        // Clear zones
        var clearCtx = _dispatcher.Dispatch(_god.Id, $"@zone {_target.Id}=none", _db);
        Assert.Contains("Zone cleared.", clearCtx.Notifications);
        Assert.Empty(_target.Zones);
        Assert.False(_target.InZone(zoneCity.Id));
    }

    [Fact]
    public void MUSHcode_ParentAndZoneFunctions_EvaluateProperly()
    {
        var ctx = new EvaluationContext(_god.Id, _god.Id);

        var p1 = _db.CreateObject("Parent One", ObjectType.Thing, _god.Id);
        var p2 = _db.CreateObject("Parent Two", ObjectType.Thing, _god.Id);
        var z1 = _db.CreateObject("Zone One", ObjectType.Thing, _god.Id);
        var z2 = _db.CreateObject("Zone Two", ObjectType.Thing, _god.Id);

        p1.SetAttribute("SPELL", "Fireball", _god.Id);
        p1.SetAttribute("CAST_SPELL", "Casting [get(me/SPELL)]!", _god.Id);

        _target.SetParents(new[] { p1.Id, p2.Id }, _db);
        _target.SetZones(new[] { z1.Id, z2.Id });

        // parent & lparent
        Assert.Equal(p1.Id.ToString(), _evaluator.Evaluate($"[parent({_target.Id})]", ctx));
        Assert.Equal($"{p1.Id} {p2.Id}", _evaluator.Evaluate($"[lparent({_target.Id})]", ctx));

        // zone & lzone
        Assert.StartsWith("#", _evaluator.Evaluate($"[zone({_target.Id})]", ctx));
        var lzoneResult = _evaluator.Evaluate($"[lzone({_target.Id})]", ctx);
        Assert.Contains(z1.Id.ToString(), lzoneResult);
        Assert.Contains(z2.Id.ToString(), lzoneResult);

        // inzone
        Assert.Equal("1", _evaluator.Evaluate($"[inzone({_target.Id}, {z1.Id})]", ctx));
        Assert.Equal("0", _evaluator.Evaluate($"[inzone({_target.Id}, {_god.Id})]", ctx));

        // Inherited get()
        Assert.Equal("Fireball", _evaluator.Evaluate($"[get({_target.Id}/SPELL)]", ctx));

        // Inherited ufun()
        Assert.Equal("Casting Fireball!", _evaluator.Evaluate($"[u({_target.Id}/CAST_SPELL)]", ctx));
    }
}
