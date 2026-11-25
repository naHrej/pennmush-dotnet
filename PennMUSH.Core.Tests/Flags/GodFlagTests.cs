namespace PennMUSH.Core.Tests.Flags;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Flags;
using PennMUSH.Core.Types;
using Xunit;

public class GodFlagTests
{
    [Fact]
    public void WorldBootstrapper_SetsGodFlagOnOne()
    {
        var db = new ObjectDatabase();
        var god = WorldBootstrapper.GenerateBaseWorld(db);

        Assert.True(god.HasFlag("GOD"));
        Assert.True(god.IsGod);
        Assert.True(god.IsWizard);
    }

    [Fact]
    public void OnlyGodCanSetOrClearGodFlag_AndOnlyOnPlayers()
    {
        var db = new ObjectDatabase();
        var god = WorldBootstrapper.GenerateBaseWorld(db);

        var wiz = db.CreateObject("WizardBob", ObjectType.Player, new Dbref(1));
        wiz.AddFlag("WIZARD");
        var mortal = db.CreateObject("Alice", ObjectType.Player, new Dbref(1));
        var thing = db.CreateObject("Box", ObjectType.Thing, god.Id);

        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new SetCommandHandler(), "@set");
        dispatcher.Register(new PoorCommandHandler(), "@poor");

        // Non-God wizard and mortal cannot set GOD
        var wizAttempt = dispatcher.Dispatch(wiz.Id, $"@set #{mortal.Id.Value}=GOD", db);
        Assert.Contains("Permission denied.", wizAttempt.Notifications);
        Assert.False(mortal.IsGod);

        var mortalAttempt = dispatcher.Dispatch(mortal.Id, $"@set #{mortal.Id.Value}=GOD", db);
        Assert.Contains("Permission denied.", mortalAttempt.Notifications);
        Assert.False(mortal.IsGod);

        // Cannot set GOD on non-player
        var thingAttempt = dispatcher.Dispatch(god.Id, $"@set #{thing.Id.Value}=GOD", db);
        Assert.Contains("That flag cannot be set on that type of object.", thingAttempt.Notifications);
        Assert.False(thing.IsGod);

        // God can grant GOD to Alice, giving Alice God privileges
        var grant = dispatcher.Dispatch(god.Id, $"@set #{mortal.Id.Value}=GOD", db);
        Assert.Contains("Set.", grant.Notifications);
        Assert.True(mortal.HasFlag("GOD"));
        Assert.True(mortal.IsGod);
        Assert.True(mortal.IsWizard);

        // Alice can now execute God-only commands (@poor) and grant GOD to WizardBob
        var poor = dispatcher.Dispatch(mortal.Id, "@poor 100", db);
        Assert.DoesNotContain("You can't remake the world in your image.", poor.Notifications);

        var grantWiz = dispatcher.Dispatch(mortal.Id, $"@set #{wiz.Id.Value}=GOD", db);
        Assert.Contains("Set.", grantWiz.Notifications);
        Assert.True(wiz.IsGod);

        // God can clear GOD from Alice
        var clear = dispatcher.Dispatch(god.Id, $"@set #{mortal.Id.Value}=!GOD", db);
        Assert.Contains("Set.", clear.Notifications);
        Assert.False(mortal.HasFlag("GOD"));
        Assert.False(mortal.IsGod);
    }
}
