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
}
