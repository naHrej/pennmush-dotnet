namespace PennMUSH.Core.Tests.Database;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Configuration;
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

        // Verify mush.cnf and @config/set system_object wiring
        var tmpPath = Path.GetTempFileName();
        MushConfiguration cfg;
        try
        {
            File.WriteAllText(tmpPath, "system_object #4\n");
            cfg = MushConfigParser.ParseFile(tmpPath);
        }
        finally
        {
            File.Delete(tmpPath);
        }
        Assert.Equal(new Dbref(4), cfg.SystemObject);

        var altSys = db.CreateObject("AltSys", ObjectType.Thing, god.Id);
        altSys.SetAttribute("ALT", combat.Id.ToString(), god.Id);
        dispatcher.Register(new ConfigCommandHandler(cfg), "@config");
        var setCfg = dispatcher.Dispatch(god.Id, $"@config/set system_object={altSys.Id}", db);
        Assert.Contains("Option set.", setCfg.Notifications);
        Assert.Equal(altSys.Id, db.SystemObjectId);
        Assert.Equal(combat.Id, db.MatchObject(god.Id, "$alt")!.Id);
    }
}
