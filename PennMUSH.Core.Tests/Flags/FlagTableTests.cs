namespace PennMUSH.Core.Tests.Flags;

using System;
using Xunit;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Flags;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class FlagTableTests
{
    private readonly ObjectDatabase _db;
    private readonly CommandDispatcher _dispatcher;
    private readonly MUSHObject _roomZero;
    private readonly MUSHObject _god;
    private readonly MUSHObject _mortal;
    private readonly MUSHObject _thing;
    private readonly MUSHObject _room;

    public FlagTableTests()
    {
        _db = new ObjectDatabase();
        // #0 = Room Zero
        _roomZero = _db.CreateObject("Room Zero", ObjectType.Room, new Dbref(1));
        // #1 = God
        _god = _db.CreateObject("God", ObjectType.Player, new Dbref(1));
        // #2 = Mortal
        _mortal = _db.CreateObject("Mortal", ObjectType.Player, _god.Id);
        // #3 = Box
        _thing = _db.CreateObject("Box", ObjectType.Thing, _mortal.Id);
        // #4 = Room
        _room = _db.CreateObject("Great Hall", ObjectType.Room, _god.Id);

        _dispatcher = new CommandDispatcher();
        _dispatcher.Register(new SetCommandHandler());
        _dispatcher.Register(new PowerCommandHandler());
    }

    [Fact]
    public void CanonicalFlags_AreLoadedFromTable()
    {
        var wizardFlag = FlagTable.FindFlag("WIZARD");
        Assert.NotNull(wizardFlag);
        Assert.Equal('W', wizardFlag.Letter);
        Assert.True(wizardFlag.Permissions.HasFlag(FlagPrivileges.Wizard));

        var darkFlag = FlagTable.FindFlag("DARK");
        Assert.NotNull(darkFlag);
        Assert.Equal('D', darkFlag.Letter);

        var havenFlag = FlagTable.FindFlag("HAVEN");
        Assert.NotNull(havenFlag);
        Assert.Equal(ObjectType.Player, havenFlag.TargetType);
    }

    [Fact]
    public void FlagAliases_ResolveToCanonicalNames()
    {
        var inheritFlag = FlagTable.FindFlag("INHERIT");
        Assert.NotNull(inheritFlag);
        Assert.Equal("TRUST", inheritFlag.Name);

        var traceFlag = FlagTable.FindFlag("TRACE");
        Assert.NotNull(traceFlag);
        Assert.Equal("DEBUG", traceFlag.Name);

        var colourFlag = FlagTable.FindFlag("COLOUR");
        Assert.NotNull(colourFlag);
        Assert.Equal("COLOR", colourFlag.Name);

        var zoneFlag = FlagTable.FindFlag("ZONE");
        Assert.NotNull(zoneFlag);
        Assert.Equal("SHARED", zoneFlag.Name);
    }

    [Fact]
    public void CanonicalPowers_AreLoadedWithAliases()
    {
        var announcePower = FlagTable.FindPower("Announce");
        Assert.NotNull(announcePower);
        Assert.True(announcePower.IsPower);

        var wallAlias = FlagTable.FindPower("@wall");
        Assert.NotNull(wallAlias);
        Assert.Equal("Announce", wallAlias.Name);

        var spoofAlias = FlagTable.FindPower("Can_nspemit");
        Assert.NotNull(spoofAlias);
        Assert.Equal("Can_spoof", spoofAlias.Name);
    }

    [Fact]
    public void InternalFlags_CannotBeSetByAnyone()
    {
        var goingFlag = FlagTable.FindFlag("GOING");
        Assert.NotNull(goingFlag);

        bool allowed = FlagTable.CanSetFlag(_god, _thing, goingFlag, false, out string err);
        Assert.False(allowed);
        Assert.Contains("Only the game can set that flag", err);
    }

    [Fact]
    public void TargetTypeMismatch_ReturnsError()
    {
        var havenFlag = FlagTable.FindFlag("HAVEN"); // Player only
        Assert.NotNull(havenFlag);

        bool allowed = FlagTable.CanSetFlag(_god, _room, havenFlag, false, out string err);
        Assert.False(allowed);
        Assert.Contains("That flag cannot be set on that type of object", err);
    }

    [Fact]
    public void PermissionChecks_EnforceWizardHierarchy()
    {
        var wizardFlag = FlagTable.FindFlag("WIZARD");
        Assert.NotNull(wizardFlag);

        // Mortal cannot set Wizard
        bool mortalCanSet = FlagTable.CanSetFlag(_mortal, _mortal, wizardFlag, false, out string mortalErr);
        Assert.False(mortalCanSet);
        Assert.Equal("Permission denied.", mortalErr);

        // God can set Wizard
        bool godCanSet = FlagTable.CanSetFlag(_god, _mortal, wizardFlag, false, out string godErr);
        Assert.True(godCanSet);
        Assert.Empty(godErr);
    }

    [Fact]
    public void SetCommand_TogglesFlagsOnObject()
    {
        // God sets WIZARD on Mortal
        var setCtx = _dispatcher.Dispatch(_god.Id, $"@set {_mortal.Id}=WIZARD", _db);
        Assert.Contains("Set.", setCtx.Notifications);
        Assert.True(_mortal.HasFlag("WIZARD"));
        Assert.True(_mortal.IsWizard);

        // God clears WIZARD on Mortal
        var clearCtx = _dispatcher.Dispatch(_god.Id, $"@set {_mortal.Id}=!WIZARD", _db);
        Assert.Contains("Set.", clearCtx.Notifications);
        Assert.False(_mortal.HasFlag("WIZARD"));
        Assert.False(_mortal.IsWizard);
    }

    [Fact]
    public void PowerCommand_AllowsWizardToSetPowers()
    {
        // Mortal fails
        var failCtx = _dispatcher.Dispatch(_mortal.Id, $"@power {_thing.Id}=Builder", _db);
        Assert.Contains("Permission denied.", failCtx.Notifications);
        Assert.False(_thing.HasPower("Builder"));

        // God succeeds
        var setCtx = _dispatcher.Dispatch(_god.Id, $"@power {_thing.Id}=Builder", _db);
        Assert.Contains("Set.", setCtx.Notifications);
        Assert.True(_thing.HasPower("Builder"));

        // God removes power
        var clearCtx = _dispatcher.Dispatch(_god.Id, $"@power {_thing.Id}=!Builder", _db);
        Assert.Contains("Set.", clearCtx.Notifications);
        Assert.False(_thing.HasPower("Builder"));
    }

    [Fact]
    public void MUSHcode_FlagAndPowerFunctions_EvaluateProperly()
    {
        var registry = new FunctionRegistry(_db);
        var evaluator = new ExpressionEvaluator(registry.Dispatch, registry.Database);
        var ctx = new EvaluationContext(_god.Id, _god.Id);

        _thing.AddFlag("SAFE");
        _thing.AddFlag("STICKY");
        _thing.AddPower("Builder");

        // hasflag
        Assert.Equal("1", evaluator.Evaluate($"[hasflag({_thing.Id}, SAFE)]", ctx));
        Assert.Equal("1", evaluator.Evaluate($"[hasflag({_thing.Id}, S)]", ctx)); // Letter 'S' is STICKY
        Assert.Equal("0", evaluator.Evaluate($"[hasflag({_thing.Id}, WIZARD)]", ctx));

        // haspower
        Assert.Equal("1", evaluator.Evaluate($"[haspower({_thing.Id}, Builder)]", ctx));
        Assert.Equal("0", evaluator.Evaluate($"[haspower({_thing.Id}, Announce)]", ctx));

        // flags
        var flagsResult = evaluator.Evaluate($"[flags({_thing.Id})]", ctx);
        Assert.StartsWith("T", flagsResult); // Thing type letter
        Assert.Contains("S", flagsResult);  // Sticky
        Assert.Contains("X", flagsResult);  // Safe

        // lflags
        var lflagsResult = evaluator.Evaluate($"[lflags({_thing.Id})]", ctx);
        Assert.Contains("SAFE", lflagsResult);
        Assert.Contains("STICKY", lflagsResult);

        // powers
        var powersResult = evaluator.Evaluate($"[powers({_thing.Id})]", ctx);
        Assert.Equal("Builder", powersResult);
    }
}
