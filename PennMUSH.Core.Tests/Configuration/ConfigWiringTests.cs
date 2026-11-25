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
}
