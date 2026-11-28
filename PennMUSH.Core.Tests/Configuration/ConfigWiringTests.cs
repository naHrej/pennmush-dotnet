namespace PennMUSH.Core.Tests.Configuration;

using System.IO;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Configuration;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;
using Xunit;

public class ConfigWiringTests
{
    [Fact]
    public void AllKnownConfigDirectivesAreWiredAcrossParserCatalogAndSwitch()
    {
        var tmpPath = Path.GetTempFileName();
        MushConfiguration cfg;
        try
        {
            File.WriteAllText(tmpPath, "tiny_trim_fun yes\ndict_file txt/dict.txt\nregister_create_file txt/reg.txt\n");
            cfg = MushConfigParser.ParseFile(tmpPath);
        }
        finally
        {
            File.Delete(tmpPath);
        }

        Assert.True(cfg.TinyTrimFun);
        Assert.Equal("txt/dict.txt", cfg.DictFile);
        Assert.Equal("txt/reg.txt", cfg.RegisterFile);

        Assert.NotNull(ConfigCatalog.Find("tiny_trim_fun"));
        Assert.NotNull(ConfigCatalog.Find("dict_file"));
        Assert.NotNull(ConfigCatalog.Find("register_create_file"));

        Assert.True(cfg.TrySetSwitch("tiny_trim_fun", false, out _));
        Assert.False(cfg.TinyTrimFun);
    }

    [Fact]
    public void ConfigSetSyncsSpecialDbrefsAndProvidersDynamically()
    {
        var db = new ObjectDatabase();
        var room0 = db.CreateObject("Room Zero", ObjectType.Room, new Dbref(1));
        var god = db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.AddFlag("WIZARD");
        god.AddFlag("GOD");
        var customMaster = db.CreateObject("Custom Master", ObjectType.Room, god.Id);
        var customHome = db.CreateObject("Custom Home", ObjectType.Room, god.Id);
        var exitInCustomMaster = db.CreateObject("warp", ObjectType.Exit, god.Id);
        exitInCustomMaster.Location = customHome.Id;
        exitInCustomMaster.Exits = customMaster.Id;
        customMaster.Exits = exitInCustomMaster.Id;
        db.MoveObject(god.Id, room0.Id);

        var cfg = new MushConfiguration();
        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new ConfigCommandHandler(cfg), "@config");
        dispatcher.Register(new GoCommandHandler(), "go");
        dispatcher.Register(new HomeCommandHandler(), "home");
        dispatcher.Register(new PcreateCommandHandler(
            null,
            () => cfg.DefaultHome,
            () => cfg.PlayerStart,
            () => cfg.StartingMoney), "@pcreate");

        // Set master_room, default_home, player_start, starting_money via @config/set
        Assert.Contains("Option set.", dispatcher.Dispatch(god.Id, $"@config/set master_room=#{customMaster.Id.Value}", db).Notifications);
        Assert.Contains("Option set.", dispatcher.Dispatch(god.Id, $"@config/set default_home=#{customHome.Id.Value}", db).Notifications);
        Assert.Contains("Option set.", dispatcher.Dispatch(god.Id, $"@config/set player_start=#{customHome.Id.Value}", db).Notifications);
        Assert.Contains("Option set.", dispatcher.Dispatch(god.Id, "@config/set starting_money=250", db).Notifications);

        Assert.Equal(customMaster.Id, db.MasterRoomId);
        Assert.Equal(customHome.Id, db.DefaultHomeId);
        Assert.Equal(customHome.Id, db.PlayerStartId);

        // `go warp` should now resolve via the reconfigured master_room
        dispatcher.Dispatch(god.Id, "go warp", db);
        Assert.Equal(customHome.Id, god.Location);

        // `home` with invalid Home should fall back to db.DefaultHomeId (customHome)
        god.Home = Dbref.Nothing;
        db.MoveObject(god.Id, room0.Id);
        dispatcher.Dispatch(god.Id, "home", db);
        Assert.Equal(customHome.Id, god.Location);

        // `@pcreate` should use the updated provider values
        dispatcher.Dispatch(god.Id, "@pcreate TestPlayer=secret", db);
        var created = db.FindPlayer("TestPlayer");
        Assert.NotNull(created);
        Assert.Equal(customHome.Id, created!.Home);
        Assert.Equal(customHome.Id, created.Location);
        Assert.Equal(250, created.Pennies);
    }

    [Fact]
    public void EnsureDefaultConfigFilesCopiesMissingCnfFromDstWithoutOverwritingExisting()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "pennmush-cfg-test-" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "mushcnf.dst"), "mud_name FromDst\n");
            File.WriteAllText(Path.Combine(tempDir, "aliascnf.dst"), "# alias dst\n");
            File.WriteAllText(Path.Combine(tempDir, "namescnf.dst"), "# names dst\n");
            File.WriteAllText(Path.Combine(tempDir, "restrictcnf.dst"), "# restrict dst\n");
            File.WriteAllText(Path.Combine(tempDir, "alias.cnf"), "# existing custom alias\n");

            PennMUSH.Server.Engine.MUSHServer.EnsureDefaultConfigFiles(tempDir);

            Assert.Equal("mud_name FromDst\n", File.ReadAllText(Path.Combine(tempDir, "mush.cnf")));
            Assert.Equal("# existing custom alias\n", File.ReadAllText(Path.Combine(tempDir, "alias.cnf")));
            Assert.Equal("# names dst\n", File.ReadAllText(Path.Combine(tempDir, "names.cnf")));
            Assert.Equal("# restrict dst\n", File.ReadAllText(Path.Combine(tempDir, "restrict.cnf")));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ConfigSave_IsGatedByConfigSaveDirective_AndPersistsWhenEnabled()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "# Header comment\nconfig_save yes\nmud_name OldMUSH\n");
            var cfg = MushConfigParser.ParseFile(tempFile);
            cfg.ConfigFilePath = tempFile;

            Assert.True(cfg.ConfigSave);
            // config_save cannot be changed via @config/set, @config/save, or TrySetSwitch
            Assert.Null(ConfigCatalog.Find("config_save"));
            Assert.False(cfg.TrySetSwitch("config_save", false, out _));

            var db = new ObjectDatabase();
            db.CreateObject("Room Zero", ObjectType.Room, new Dbref(1));
            var god = db.CreateObject("One", ObjectType.Player, new Dbref(1));
            god.AddFlag("WIZARD");
            god.AddFlag("GOD");

            var dispatcher = new CommandDispatcher();
            dispatcher.Register(new ConfigCommandHandler(cfg), "@config");

            var blocked = dispatcher.Dispatch(god.Id, "@config/set config_save=no", db);
            Assert.DoesNotContain("Option set.", blocked.Notifications);
            Assert.True(cfg.ConfigSave);

            var blockedSave = dispatcher.Dispatch(god.Id, "@config/save config_save=no", db);
            Assert.DoesNotContain("Option set and saved.", blockedSave.Notifications);
            Assert.True(cfg.ConfigSave);

            // Update existing directive and append a new one
            var savedExisting = dispatcher.Dispatch(god.Id, "@config/save mud_name=NewMUSH", db);
            Assert.Contains("Option set and saved.", savedExisting.Notifications);

            var savedNew = dispatcher.Dispatch(god.Id, "@config/save starting_money=500", db);
            Assert.Contains("Option set and saved.", savedNew.Notifications);

            string contents = File.ReadAllText(tempFile);
            Assert.Contains("# Header comment", contents);
            Assert.Contains("mud_name NewMUSH", contents);
            Assert.DoesNotContain("mud_name OldMUSH", contents);
            Assert.Contains("starting_money 500", contents);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void UnsafeEvalFunctions_DisabledByDefault_CapsObjEvalAndFn_AndAllowsWhenEnabled()
    {
        var db = new ObjectDatabase();
        var god = WorldBootstrapper.GenerateBaseWorld(db);
        var wiz = db.CreateObject("WizardBob", ObjectType.Player, new Dbref(1));
        wiz.AddFlag("WIZARD");
        var wizThing = db.CreateObject("WizThing", ObjectType.Thing, wiz.Id);

        var cfg = new MushConfiguration();
        Assert.False(cfg.UnsafeEvalFunctions);

        var functions = new PennMUSH.Core.MUSHcode.FunctionRegistry(db)
        {
            UnsafeEvalFunctionsProvider = () => cfg.UnsafeEvalFunctions
        };
        var evaluator = new PennMUSH.Core.MUSHcode.ExpressionEvaluator(functions.Dispatch, db);
        functions.Evaluator = evaluator;

        var wizCtx = new PennMUSH.Core.MUSHcode.EvaluationContext(wiz.Id, wiz.Id, wiz.Id, wiz.Location);

        // s() is disabled by default
        Assert.Equal("#-1 FUNCTION DISABLED", evaluator.Evaluate("[s([add(1,2)])]", wizCtx));

        // objeval() works on equal-or-lower privilege objects (wiz -> wizThing)
        Assert.Equal(wizThing.Id.ToString(), evaluator.Evaluate($"[objeval({wizThing.Id},%!)]", wizCtx));

        // objeval() and fn() cannot escalate above caller's privilege tier (wiz -> god #1)
        Assert.Equal("#-1 PERMISSION DENIED", evaluator.Evaluate($"[objeval({god.Id},%!)]", wizCtx));
        Assert.Equal("#-1 PERMISSION DENIED", evaluator.Evaluate($"[fn({god.Id}/add,1,2)]", wizCtx));

        // Enabling unsafe_eval_functions via TrySetSwitch (or @config/set) restores s() and unrestricted objeval/fn
        Assert.True(cfg.TrySetSwitch("unsafe_eval_functions", true, out _));
        Assert.Equal("3", evaluator.Evaluate("[s([add(1,2)])]", wizCtx));
        Assert.Equal(god.Id.ToString(), evaluator.Evaluate($"[objeval({god.Id},%!)]", wizCtx));
        Assert.Equal("3", evaluator.Evaluate($"[fn({god.Id}/add,1,2)]", wizCtx));

        // restrict_function via TryRestrict works as well
        Assert.True(functions.TryRestrict("s", "nobody"));
        Assert.Equal("#-1 FUNCTION DISABLED", evaluator.Evaluate("[s([add(1,2)])]", wizCtx));
    }

    [Fact]
    public void Phase1_Currency_ConfigList_And_BaseRoomDbck_WorkDynamically()
    {
        var db = new ObjectDatabase();
        var god = WorldBootstrapper.GenerateBaseWorld(db);
        var altBase = db.CreateObject("AltBase", ObjectType.Room, god.Id);
        var orphan = db.CreateObject("Orphan", ObjectType.Thing, god.Id);
        orphan.Home = new Dbref(999);
        orphan.Location = new Dbref(999);

        var cfg = new MushConfiguration();
        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new ConfigCommandHandler(cfg), "@config");
        dispatcher.Register(new ScoreCommandHandler(), "score");
        dispatcher.Register(new PoorCommandHandler(), "@poor");
        dispatcher.Register(new DbckCommandHandler(), "@dbck");

        var functions = new PennMUSH.Core.MUSHcode.FunctionRegistry(db)
        {
            ConfigProvider = key => ConfigCatalog.Find(key)?.Read(cfg)
        };
        var evaluator = new PennMUSH.Core.MUSHcode.ExpressionEvaluator(functions.Dispatch, db);
        var godCtx = new PennMUSH.Core.MUSHcode.EvaluationContext(god.Id, god.Id, god.Id, god.Location);

        // config() with no args lists ConfigCatalog options
        string allOpts = evaluator.Evaluate("[config()]", godCtx);
        Assert.Contains("system_object", allOpts);
        Assert.Contains("unsafe_eval_functions", allOpts);
        Assert.Contains("money_singular", allOpts);

        // money_singular / money_plural update score and @poor dynamically
        dispatcher.Dispatch(god.Id, "@config/set money_singular=Credit", db);
        dispatcher.Dispatch(god.Id, "@config/set money_plural=Credits", db);
        Assert.Contains("You have 1000 credits.", dispatcher.Dispatch(god.Id, "score", db).Notifications);
        Assert.Contains("Set 1 player(s) to 1 credit.", dispatcher.Dispatch(god.Id, "@poor 1", db).Notifications);
        Assert.Contains("You have 1 credit.", dispatcher.Dispatch(god.Id, "score", db).Notifications);

        // base_room fallback in @dbck when default_home is invalid
        dispatcher.Dispatch(god.Id, "@config/set default_home=#999", db);
        dispatcher.Dispatch(god.Id, $"@config/set base_room=#{altBase.Id.Value}", db);
        dispatcher.Dispatch(god.Id, "@dbck", db);
        Assert.Equal(altBase.Id, orphan.Home);
        Assert.Equal(altBase.Id, orphan.Location);
    }

    [Fact]
    public async System.Threading.Tasks.Task Phase1_Server_GuestsAndPaycheck_RespectConfig()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"phase1_srv_{System.Guid.NewGuid():N}.db");
        try
        {
            var server = new PennMUSH.Server.Engine.MUSHServer(dbPath);
            server.Config.Paycheck = 75;
            server.Config.Guests = true;
            server.Config.MaxGuests = 1;

            var alice = server.Database.CreateObject("Alice", ObjectType.Player, new Dbref(1));
            alice.Owner = alice.Id;
            alice.Pennies = 10;
            PennMUSH.Core.Database.PasswordHash.Store(alice, "pass");
            server.Database.MoveObject(alice.Id, new Dbref(0));

            var conn1Out = new System.Collections.Generic.List<string>();
            var conn1 = new PennMUSH.Server.Session.MUSHConnection(line => { conn1Out.Add(line); return System.Threading.Tasks.Task.CompletedTask; });
            server.RegisterConnection(conn1);
            await server.HandleInputAsync(conn1, "connect Alice pass");
            Assert.Equal(85, alice.Pennies);
            Assert.Contains(conn1Out, l => l.Contains("daily paycheck of 75 pennies"));

            // Second login same day does not award paycheck again
            var conn1b = new PennMUSH.Server.Session.MUSHConnection(_ => System.Threading.Tasks.Task.CompletedTask);
            server.RegisterConnection(conn1b);
            await server.HandleInputAsync(conn1b, "connect Alice pass");
            Assert.Equal(85, alice.Pennies);

            // Guest login works up to MaxGuests = 1
            var g1Out = new System.Collections.Generic.List<string>();
            var g1 = new PennMUSH.Server.Session.MUSHConnection(line => { g1Out.Add(line); return System.Threading.Tasks.Task.CompletedTask; });
            server.RegisterConnection(g1);
            await server.HandleInputAsync(g1, "connect guest");
            Assert.True(g1.PlayerId.IsValid);

            var g2Out = new System.Collections.Generic.List<string>();
            var g2 = new PennMUSH.Server.Session.MUSHConnection(line => { g2Out.Add(line); return System.Threading.Tasks.Task.CompletedTask; });
            server.RegisterConnection(g2);
            await server.HandleInputAsync(g2, "connect guest");
            Assert.False(g2.PlayerId.IsValid);
            Assert.Contains(g2Out, l => l.Contains("all guest slots are currently in use"));

            server.Stop();
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
                try { File.Delete(dbPath); } catch { }
        }
    }

    [Fact]
    public void Phase2_SaferUfun_SideEffects_Limits_And_PlayerNaming_Work()
    {
        var tmpPath = Path.GetTempFileName();
        MushConfiguration cfg;
        try
        {
            File.WriteAllText(tmpPath, "safer_ufun yes\nidle_timeout 2m\nunconnected_idle_timeout 5m\nplayer_name_len 10\nlogins no\n");
            cfg = MushConfigParser.ParseFile(tmpPath);
        }
        finally
        {
            File.Delete(tmpPath);
        }

        Assert.Equal(120, cfg.IdleTimeout);
        Assert.Equal(300, cfg.UnconnectedIdleTimeout);
        Assert.Equal(10, cfg.PlayerNameLen);
        Assert.False(cfg.Logins);

        var db = new ObjectDatabase
        {
            EmptyAttrs = false,
            MaxAttrsPerObj = 1,
            MaxParents = 1,
            PlayerNameLen = 10,
            PlayerNameSpaces = false,
            OnlyAsciiInNames = true
        };
        var god = WorldBootstrapper.GenerateBaseWorld(db);
        var wiz = db.CreateObject("WizardBob", ObjectType.Player, new Dbref(1));
        wiz.AddFlag("WIZARD");
        var mortal = db.CreateObject("Alice", ObjectType.Player, new Dbref(1));
        var mortalThing = db.CreateObject("Box", ObjectType.Thing, mortal.Id);
        var wizThing = db.CreateObject("WizBox", ObjectType.Thing, wiz.Id);
        wizThing.AddFlag("VISUAL");
        wizThing.AddFlag("TRUST");
        wizThing.SetAttribute("SECRET", "hi", wiz.Id);

        var functions = new PennMUSH.Core.MUSHcode.FunctionRegistry(db)
        {
            SaferUfunProvider = () => cfg.SaferUfun,
            SideEffectsEnabledProvider = () => cfg.FunctionSideEffects
        };
        var evaluator = new PennMUSH.Core.MUSHcode.ExpressionEvaluator(functions.Dispatch, db);
        functions.Evaluator = evaluator;
        var mortalCtx = new PennMUSH.Core.MUSHcode.EvaluationContext(mortal.Id, mortal.Id, mortal.Id, mortal.Location);

        // safer_ufun blocks mortal calling u() on higher-privilege object's attribute
        Assert.Equal("#-1 PERMISSION DENIED", evaluator.Evaluate($"[u({wizThing.Id}/SECRET)]", mortalCtx));
        cfg.SaferUfun = false;
        Assert.Equal("hi", evaluator.Evaluate($"[u({wizThing.Id}/SECRET)]", mortalCtx));

        // function_side_effects=no disables side-effect functions like set()
        cfg.FunctionSideEffects = false;
        Assert.Equal("#-1 PERMISSION DENIED", evaluator.Evaluate($"[set({mortalThing.Id},FOO:bar)]", mortalCtx));

        // max_attrs_per_obj and empty_attrs
        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new SetCommandHandler(), "@set");
        dispatcher.Register(new ParentCommandHandler(), "@parent");
        dispatcher.Dispatch(mortal.Id, $"@set #{mortalThing.Id.Value}=A:1", db);
        Assert.Contains("Permission denied.", dispatcher.Dispatch(mortal.Id, $"@set #{mortalThing.Id.Value}=B:2", db).Notifications);
        dispatcher.Dispatch(mortal.Id, $"@set #{mortalThing.Id.Value}=A:", db);
        Assert.Null(mortalThing.GetAttribute("A"));

        // player_name_len / player_name_spaces / only_ascii_in_names
        Assert.False(PlayerCreator.OkPlayerName("TooLongPlayerName", db));
        Assert.False(PlayerCreator.OkPlayerName("Two Words", db));
        Assert.True(PlayerCreator.OkPlayerName("ShortName", db));
    }

    [Fact]
    public void Phase3_Ancestors_LinkToObject_And_RepeatedFlags_Work()
    {
        var tmpPath = Path.GetTempFileName();
        MushConfiguration cfg;
        try
        {
            File.WriteAllText(tmpPath, "player_flags enter_ok\nplayer_flags ansi\nancestor_thing #2\nfloat_precision 4\nlink_to_object no\n");
            cfg = MushConfigParser.ParseFile(tmpPath);
        }
        finally
        {
            File.Delete(tmpPath);
        }

        Assert.Contains("enter_ok", cfg.PlayerFlags);
        Assert.Contains("ansi", cfg.PlayerFlags);
        Assert.Equal(new Dbref(2), cfg.AncestorThing);
        Assert.Equal(4, cfg.FloatPrecision);
        Assert.False(cfg.LinkToObject);
        Assert.True(cfg.TrySetSwitch("use_muxcomm", true, out _));
        Assert.True(cfg.UseMuxcomm);

        var db = new ObjectDatabase();
        var god = WorldBootstrapper.GenerateBaseWorld(db);
        var ancestor = db.CreateObject("ThingAncestor", ObjectType.Thing, god.Id);
        ancestor.SetAttribute("GREETING", "Hello from ancestor", god.Id);
        var child = db.CreateObject("PlainThing", ObjectType.Thing, god.Id);
        var exit = db.CreateObject("out", ObjectType.Exit, god.Id);

        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new ConfigCommandHandler(cfg), "@config");
        dispatcher.Register(new LinkCommandHandler(), "@link");

        // Configure ancestor_thing and link_to_object via @config/set
        dispatcher.Dispatch(god.Id, $"@config/set ancestor_thing=#{ancestor.Id.Value}", db);
        Assert.Equal("Hello from ancestor", child.GetAttribute("GREETING", db)?.Value);

        dispatcher.Dispatch(god.Id, "@config/set link_to_object=no", db);
        Assert.Contains("Permission denied.", dispatcher.Dispatch(god.Id, $"@link #{exit.Id.Value}=#{child.Id.Value}", db).Notifications);
        dispatcher.Dispatch(god.Id, "@config/set link_to_object=yes", db);
        Assert.Contains("Linked.", dispatcher.Dispatch(god.Id, $"@link #{exit.Id.Value}=#{child.Id.Value}", db).Notifications);
    }

    [Fact]
    public void AncestorOptionsListInTheirOwnGroupAndApplyImmediately()
    {
        foreach (var name in new[] { "ancestor_room", "ancestor_thing", "ancestor_exit", "ancestor_player" })
            Assert.Equal("ancestors", ConfigCatalog.Find(name)!.Group);

        var cfg = new MushConfiguration();
        var db = new ObjectDatabase();
        var god = WorldBootstrapper.GenerateBaseWorld(db);
        var roomAncestor = db.CreateObject("RoomAncestor", ObjectType.Room, god.Id);
        roomAncestor.SetAttribute("GREETING", "from room ancestor", god.Id);
        var room = db.CreateObject("PlainRoom", ObjectType.Room, god.Id);
        Assert.Null(room.GetAttribute("GREETING", db));

        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new ConfigCommandHandler(cfg), "@config");

        var listed = dispatcher.Dispatch(god.Id, "@config ancestors", db);
        Assert.Contains("Default ancestor objects", listed.Notifications);
        Assert.Contains(listed.Notifications, line => line.Contains("ancestor_room"));
        Assert.Contains(listed.Notifications, line => line.Contains("ancestor_thing"));
        Assert.Contains(listed.Notifications, line => line.Contains("ancestor_exit"));
        Assert.Contains(listed.Notifications, line => line.Contains("ancestor_player"));

        var database = dispatcher.Dispatch(god.Id, "@config db", db);
        Assert.DoesNotContain(database.Notifications, line => line.Contains("ancestor_"));

        dispatcher.Dispatch(god.Id, $"@config/set ancestor_room=#{roomAncestor.Id.Value}", db);
        Assert.Equal(roomAncestor.Id, cfg.AncestorRoom);
        Assert.Equal(roomAncestor.Id, db.AncestorRoom);
        Assert.Equal("from room ancestor", room.GetAttribute("GREETING", db)?.Value);

        dispatcher.Dispatch(god.Id, "@config/set ancestor_room=#-1", db);
        Assert.Equal(Dbref.Nothing, db.AncestorRoom);
        Assert.Null(room.GetAttribute("GREETING", db));
    }

    [Fact]
    public void Phase4_Intervals_Mssp_HtmlScreens_And_Connlog_ParseAndResolve()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "penn-p4-" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tmpDir, "txt"));
        try
        {
            string cnf = Path.Combine(tmpDir, "mush.cnf");
            File.WriteAllText(cnf,
                "dump_interval 30m\ndbck_interval 10m\npurge_interval 10m1s\nwarn_interval 1h\n" +
                "http_per_second 5\nevent_handler #4\nuse_connlog yes\nconnlog_db log/custom_conn.db\n" +
                "connect_html_file txt/custom_connect.html\nmssp ansi/1\nmssp XTERM 256 COLORS/1\n");
            File.WriteAllText(Path.Combine(tmpDir, "txt", "custom_connect.html"), "<p>Hi</p>");

            var cfg = MushConfigParser.ParseFile(cnf);
            Assert.Equal(1800, cfg.DumpInterval);
            Assert.Equal(600, cfg.DbckInterval);
            Assert.Equal(601, cfg.PurgeInterval);
            Assert.Equal(3600, cfg.WarnInterval);
            Assert.Equal(5, cfg.HttpPerSecond);
            Assert.Equal(new Dbref(4), cfg.EventHandler);
            Assert.Equal("log/custom_conn.db", cfg.ConnlogDb);
            Assert.Equal("1", cfg.MsspFields["ansi"]);
            Assert.Equal("1", cfg.MsspFields["XTERM 256 COLORS"]);

            string? resolvedHtml = GameScreenFiles.Resolve(tmpDir, cfg.ConnectFile, webSocket: true, cfg.ConnectHtmlFile);
            Assert.NotNull(resolvedHtml);
            Assert.EndsWith("custom_connect.html", resolvedHtml!);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public void SpeechMogrifier_FormatsSayPoseAndSemiposeGlobally()
    {
        var db = new ObjectDatabase();
        var god = WorldBootstrapper.GenerateBaseWorld(db);
        var mog = db.CreateObject("SpeechMog", ObjectType.Thing, god.Id);
        mog.SetAttribute("MOGRIFYSAY", "\\[%1\\] %2", god.Id);
        mog.SetAttribute("MOGRIFYPOSE", "* %1 %2", god.Id);
        mog.SetAttribute("MOGRIFYSEMIPOSE", "* %1%2", god.Id);

        var cfg = new MushConfiguration();
        var functions = new PennMUSH.Core.MUSHcode.FunctionRegistry(db);
        var evaluator = new PennMUSH.Core.MUSHcode.ExpressionEvaluator(functions.Dispatch, db);
        var dispatcher = new CommandDispatcher(evaluator);
        dispatcher.Register(new ConfigCommandHandler(cfg), "@config");
        dispatcher.Register(new SayCommandHandler(evaluator), "say", "\"");
        dispatcher.Register(new PoseCommandHandler(evaluator), "pose", ":", ";", "semipose");

        // Configure speech_mogrifier via @config/set
        dispatcher.Dispatch(god.Id, $"@config/set speech_mogrifier=#{mog.Id.Value}", db);
        Assert.Equal(mog.Id, db.SpeechMogrifierId);

        var sayRes = dispatcher.Dispatch(god.Id, "say hello", db);
        Assert.Contains("[One] hello", sayRes.Notifications);

        var poseRes = dispatcher.Dispatch(god.Id, "pose waves.", db);
        Assert.Contains("* One waves.", poseRes.Notifications);

        var semiRes = dispatcher.Dispatch(god.Id, ";'s eyes narrow.", db);
        Assert.Contains("* One's eyes narrow.", semiRes.Notifications);
    }

    [Fact]
    public void WhoMogrifier_ConfigSetCopiesTheDbref()
    {
        var db = new ObjectDatabase();
        var god = WorldBootstrapper.GenerateBaseWorld(db);
        var mog = db.CreateObject("WhoMog", ObjectType.Thing, god.Id);
        var cfg = new MushConfiguration();
        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new ConfigCommandHandler(cfg), "@config");

        Assert.Contains("Option set.", dispatcher.Dispatch(god.Id, $"@config/set who_mogrifier=#{mog.Id.Value}", db).Notifications);
        Assert.Equal(mog.Id, cfg.WhoMogrifier);
        Assert.Equal(mog.Id, db.WhoMogrifierId);

        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, $"who_mogrifier #{mog.Id.Value}\n");
            Assert.Equal(mog.Id, MushConfigParser.ParseFile(path).WhoMogrifier);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
