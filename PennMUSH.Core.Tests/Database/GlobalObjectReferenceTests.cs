namespace PennMUSH.Core.Tests.Database;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;
using Xunit;

public class GlobalObjectReferenceTests
{
    [Fact]
    public void WorldBootstrapper_CreatesSystemObject_And_MatchObjectResolvesDollarRefs()
    {
        var db = new ObjectDatabase();
        var god = WorldBootstrapper.GenerateBaseWorld(db);

        var sysObj = db.GetObject(new Dbref(3));
        Assert.NotNull(sysObj);
        Assert.Equal("System Object", sysObj!.Name);
        Assert.Equal(ObjectType.Thing, sysObj.Type);
        Assert.True(sysObj.HasFlag("SAFE"));
        Assert.True(sysObj.HasFlag("VISUAL"));

        var combat = db.CreateObject("CombatSystem", ObjectType.Thing, god.Id);
        sysObj.SetAttribute("COMBAT", combat.Id.ToString(), god.Id);

        var resolved = db.MatchObject(god.Id, "$combat");
        Assert.NotNull(resolved);
        Assert.Equal(combat.Id, resolved!.Id);
        Assert.Null(db.MatchObject(god.Id, "$nonexistent"));

        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new ExamineCommandHandler(), "examine", "ex");
        var ex = dispatcher.Dispatch(god.Id, "examine $combat", db);
        Assert.Contains(ex.Notifications, n => n.Contains("CombatSystem", System.StringComparison.Ordinal));
    }
}
