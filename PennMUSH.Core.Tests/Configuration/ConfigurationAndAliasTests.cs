namespace PennMUSH.Core.Tests.Configuration;

using System;
using System.IO;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Configuration;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class ConfigurationAndAliasTests
{
    [Fact]
    public void MushConfigParser_ParsesGameMushCnfWithIncludes()
    {
        // Locate game/mush.cnf
        string basePath = AppContext.BaseDirectory;
        string cnfPath = Path.Combine(basePath, "game", "mush.cnf");
        if (!File.Exists(cnfPath))
        {
            // Fallback to solution root game/mush.cnf
            cnfPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "game", "mush.cnf"));
        }

        Assert.True(File.Exists(cnfPath), $"mush.cnf should exist at {cnfPath}");

        var config = MushConfigParser.ParseFile(cnfPath);

        // Verify essentials
        Assert.Equal("PennMUSH", config.MudName);
        Assert.Equal(4201, config.Port);
        Assert.True(config.UseDns);

        // Verify command aliases loaded from alias.cnf via include
        Assert.True(config.CommandAliases.ContainsKey("l"));
        Assert.Equal("look", config.CommandAliases["l"]);
        Assert.True(config.CommandAliases.ContainsKey("i"));
        Assert.Equal("inventory", config.CommandAliases["i"]);
        Assert.True(config.CommandAliases.ContainsKey("@sw"));
        Assert.Equal("@switch", config.CommandAliases["@sw"]);

        // Verify function aliases loaded from alias.cnf
        Assert.True(config.FunctionAliases.ContainsKey("parse"));
        Assert.Equal("iter", config.FunctionAliases["parse"]);
        Assert.True(config.FunctionAliases.ContainsKey("mod"));
        Assert.Equal("modulo", config.FunctionAliases["mod"]);

        // Verify reserved aliases
        Assert.Contains("dummy_exit_alias", config.ReservedAliases);

        // Verify restrictions loaded from restrict.cnf via include
        Assert.True(config.CommandRestrictions.ContainsKey("@set"));
        Assert.Contains(config.CommandRestrictions["@set"], r => r.Restriction.Equals("noguest", StringComparison.OrdinalIgnoreCase));
        Assert.True(config.CommandRestrictions.ContainsKey("home"));
        Assert.Contains(config.CommandRestrictions["home"], r => r.Restriction.Equals("nofixed", StringComparison.OrdinalIgnoreCase) && r.ErrorMessage != null && r.ErrorMessage.Contains("You can't do that IC!"));
    }

    [Fact]
    public void CommandDispatcher_EnforcesRestrictions()
    {
        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new TestCommand("home"));
        dispatcher.AddRestriction("home", "nofixed", "You can't do that IC!");

        var db = new ObjectDatabase();
        var player = db.CreateObject("FixedPlayer", ObjectType.Player, Dbref.Nothing);
        player.AddFlag("FIXED");

        var ctx = dispatcher.Dispatch(player.Id, "home", db);
        Assert.Contains("You can't do that IC!", ctx.Notifications);
        Assert.DoesNotContain("Executed home", ctx.Notifications);
    }

    [Fact]
    public void CommandTable_ExactAndAliasMatching()
    {
        var table = new CommandTable();
        var lookHandler = new LookCommandHandler();
        table.Register(lookHandler);
        table.AddAlias("look", "l");

        var exactResult = table.Lookup("look");
        Assert.Equal(CommandLookupStatus.Found, exactResult.Status);
        Assert.Same(lookHandler, exactResult.Handler);

        var aliasResult = table.Lookup("l");
        Assert.Equal(CommandLookupStatus.Found, aliasResult.Status);
        Assert.Same(lookHandler, aliasResult.Handler);
    }

    [Fact]
    public void CommandTable_PrefixAutoaliasing()
    {
        var table = new CommandTable();
        table.Register(new InventoryCommandHandler());

        // "inve" should prefix match "inventory"
        var prefixResult = table.Lookup("inve");
        Assert.Equal(CommandLookupStatus.Found, prefixResult.Status);
        Assert.Equal("inventory", prefixResult.CanonicalName);
    }

    [Fact]
    public void CommandTable_AmbiguousPrefix_ReportsAmbiguous()
    {
        var table = new CommandTable();
        table.Register(new DigCommandHandler(), "@dig");
        table.Register(new DropCommandHandler(), "drop");

        // Register dummy commands that share prefix
        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new TestCommand("teleport"));
        dispatcher.Register(new TestCommand("telephone"));

        var result = dispatcher.Table.Lookup("tele");
        Assert.Equal(CommandLookupStatus.Ambiguous, result.Status);

        var db = new ObjectDatabase();
        var ctx = dispatcher.Dispatch(new Dbref(1), "tele", db);
        Assert.Contains("Which of those commands do you mean?", ctx.Notifications);
    }

    [Fact]
    public void CommandTable_ReservedAlias_DoesNotAutoalias()
    {
        var dispatcher = new CommandDispatcher();
        var newsHandler = new TestCommand("news");
        dispatcher.Register(newsHandler);
        dispatcher.Register(new GoCommandHandler(), "go");

        // Reserve "n" so it doesn't autoalias to "news"
        dispatcher.ReserveAlias("n");

        var db = new ObjectDatabase();
        var room = db.CreateObject("Test Room", ObjectType.Room, Dbref.Nothing);
        var player = db.CreateObject("Player", ObjectType.Player, Dbref.Nothing);
        player.Location = room.Id;

        // Create an exit "n;north"
        var dest = db.CreateObject("North Room", ObjectType.Room, Dbref.Nothing);
        var exit = db.CreateObject("n;north", ObjectType.Exit, Dbref.Nothing);
        db.AttachExit(room.Id, exit.Id);
        db.LinkExit(exit.Id, dest.Id);

        // Player types "n" -> should traverse exit, NOT run "news"
        var ctx = dispatcher.Dispatch(player.Id, "n", db);

        Assert.Equal(dest.Id, player.Location);
        Assert.DoesNotContain("Which of those commands do you mean?", ctx.Notifications);
    }

    [Fact]
    public void FunctionRegistry_FunctionAliasesWork()
    {
        var db = new ObjectDatabase();
        var registry = new FunctionRegistry(db);
        var evaluator = new ExpressionEvaluator(registry.Dispatch, registry.Database);
        registry.Evaluator = evaluator;

        // Add alias parse -> iter, mod -> modulo
        registry.AddAlias("iter", "parse");
        registry.AddAlias("modulo", "mod");

        var ctx = new EvaluationContext(new Dbref(1), new Dbref(1));

        string parseResult = evaluator.Evaluate("[parse(1 2 3,##)]", ctx);
        Assert.Equal("1 2 3", parseResult);

        string modResult = evaluator.Evaluate("[mod(17,5)]", ctx);
        Assert.Equal("2", modResult);
    }

    [Fact]
    public void CommandTable_AliasPrefixMatching_MatchesCanonicalHandler()
    {
        var table = new CommandTable();
        table.Register(new GetCommandHandler(), "get", "take");

        // "tak" should prefix match the alias "take", resolving to canonical "get"
        var prefixResult = table.Lookup("tak");
        Assert.Equal(CommandLookupStatus.Found, prefixResult.Status);
        Assert.Equal("get", prefixResult.CanonicalName);
    }

    [Fact]
    public void LookCommand_LookAtPlayerByName_Succeeds()
    {
        var db = new ObjectDatabase();
        var room = db.CreateObject("Room Zero", ObjectType.Room, Dbref.Nothing);
        var wizard = db.CreateObject("One", ObjectType.Player, Dbref.Nothing);
        wizard.Location = room.Id;
        var user = db.CreateObject("Krenath", ObjectType.Player, Dbref.Nothing);
        user.Location = room.Id;

        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new LookCommandHandler(), "look", "l");

        // Test "l Krenath" and "look Krenath"
        var ctx1 = dispatcher.Dispatch(wizard.Id, "l Krenath", db);
        Assert.Contains("Krenath", ctx1.Notifications);
        Assert.Contains("You see nothing special.", ctx1.Notifications);
        Assert.DoesNotContain("I can't see that here.", ctx1.Notifications);

        var ctx2 = dispatcher.Dispatch(wizard.Id, "look Krenath", db);
        Assert.Contains("Krenath", ctx2.Notifications);
        Assert.Contains("You see nothing special.", ctx2.Notifications);
        Assert.DoesNotContain("I can't see that here.", ctx2.Notifications);
    }

    [Fact]
    public void ExamineCommand_AliasedAsX_Succeeds()
    {
        var db = new ObjectDatabase();
        var room = db.CreateObject("Room Zero", ObjectType.Room, Dbref.Nothing);
        var wizard = db.CreateObject("One", ObjectType.Player, Dbref.Nothing);
        wizard.Location = room.Id;
        var user = db.CreateObject("Krenath", ObjectType.Player, Dbref.Nothing);
        user.Location = room.Id;

        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new ExamineCommandHandler(), "examine", "ex", "x");

        var ctx = dispatcher.Dispatch(wizard.Id, "x Krenath", db);
        Assert.Contains(ctx.Notifications, n => n.Contains("Krenath (#4)") || n.Contains("Krenath"));
        Assert.Contains(ctx.Notifications, n => n.Contains("Type: Player"));
    }

    [Fact]
    public void SetCommand_ByName_Succeeds()
    {
        var db = new ObjectDatabase();
        var room = db.CreateObject("Room Zero", ObjectType.Room, Dbref.Nothing);
        var wizard = db.CreateObject("One", ObjectType.Player, Dbref.Nothing);
        wizard.Location = room.Id;
        wizard.AddFlag("WIZARD");
        var user = db.CreateObject("Krenath", ObjectType.Player, Dbref.Nothing);
        user.Location = room.Id;

        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new SetCommandHandler(), "@set");

        var ctx = dispatcher.Dispatch(wizard.Id, "@set Krenath=WIZARD", db);
        Assert.Contains("Set.", ctx.Notifications);
        Assert.True(user.HasFlag("WIZARD"));
    }

    private class TestCommand : ICommandHandler
    {
        public string Name { get; }
        public TestCommand(string name) => Name = name;
        public bool Execute(CommandContext context, ObjectDatabase db)
        {
            context.Notify($"Executed {Name}");
            return true;
        }
    }
}
