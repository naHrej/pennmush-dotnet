namespace PennMUSH.Core.Tests.Database;

using System;
using System.IO;
using System.Linq;
using Xunit;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class PersistenceAndBootstrapTests
{
    [Fact]
    public void WorldBootstrapper_GeneratesExactPennMUSHPristineWorld()
    {
        var db = new ObjectDatabase();
        var god = WorldBootstrapper.GenerateBaseWorld(db);

        Assert.Equal(3, db.Count);

        // #0: Room Zero
        var roomZero = db.GetObject(new Dbref(0));
        Assert.NotNull(roomZero);
        Assert.Equal("Room Zero", roomZero.Name);
        Assert.Equal(ObjectType.Room, roomZero.Type);
        Assert.Equal(new Dbref(1), roomZero.Owner);
        Assert.Equal("You are in Room Zero.", roomZero.GetAttribute("DESCRIBE")?.Value);

        // #1: One (God)
        Assert.NotNull(god);
        Assert.Equal(new Dbref(1), god.Id);
        Assert.Equal("One", god.Name);
        Assert.Equal(ObjectType.Player, god.Type);
        Assert.Equal(roomZero.Id, god.Location);
        Assert.Equal(roomZero.Id, god.Home);
        Assert.True(god.IsGod);
        Assert.True(god.HasFlag("WIZARD"));
        Assert.Equal(1000, god.Pennies);

        // #2: Master Room
        var masterRoom = db.GetObject(new Dbref(2));
        Assert.NotNull(masterRoom);
        Assert.Equal("Master Room", masterRoom.Name);
        Assert.Equal(ObjectType.Room, masterRoom.Type);
        Assert.Equal(god.Id, masterRoom.Owner);
    }

    [Fact]
    public void FlatfileStore_SaveAndLoad_RoundtripsSuccessfully()
    {
        var originalDb = new ObjectDatabase();
        var god = WorldBootstrapper.GenerateBaseWorld(originalDb);

        var player = originalDb.CreateObject("Adventurer", ObjectType.Player, god.Id);
        player.Location = new Dbref(0);
        player.Pennies = 250;
        player.AddFlag("ANSI");
        player.AddPower("BUILDER");
        player.SetAttribute("TITLE", "The Bold", god.Id);

        // Add parent and zone
        player.AddParent(new Dbref(2));
        player.AddZone(new Dbref(0));

        var sw = new StringWriter();
        FlatfileStore.Save(originalDb, sw);
        string dumpContent = sw.ToString();

        Assert.Contains("+V5", dumpContent);
        Assert.Contains("***END OF DUMP***", dumpContent);
        Assert.Contains("Adventurer", dumpContent);

        // Load into fresh database
        var loadedDb = new ObjectDatabase();
        using var sr = new StringReader(dumpContent);
        FlatfileStore.Load(loadedDb, sr);

        Assert.Equal(originalDb.Count, loadedDb.Count);

        var loadedPlayer = loadedDb.GetObject(player.Id);
        Assert.NotNull(loadedPlayer);
        Assert.Equal("Adventurer", loadedPlayer.Name);
        Assert.Equal(ObjectType.Player, loadedPlayer.Type);
        Assert.Equal(new Dbref(0), loadedPlayer.Location);
        Assert.Equal(250, loadedPlayer.Pennies);
        Assert.True(loadedPlayer.HasFlag("ANSI"));
        Assert.True(loadedPlayer.HasPower("BUILDER"));
        Assert.Equal("The Bold", loadedPlayer.GetAttribute("TITLE")?.Value);
        Assert.Contains(new Dbref(2), loadedPlayer.Parents);
        Assert.Contains(new Dbref(0), loadedPlayer.Zones);
    }

    [Fact]
    public void SqliteStore_SaveAndLoad_RoundtripsSuccessfully()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"mush_test_{Guid.NewGuid():N}.db");
        string connStr = $"Data Source={dbPath}";

        try
        {
            using var sqlite = new SqliteStore(connStr);
            var originalDb = new ObjectDatabase();
            var god = WorldBootstrapper.GenerateBaseWorld(originalDb);

            var sword = originalDb.CreateObject("Broadsword", ObjectType.Thing, god.Id);
            sword.Location = new Dbref(0);
            sword.Pennies = 50;
            sword.SetAttribute("DAMAGE", "2d6+2", god.Id);
            sword.AddFlag("DARK");
            sword.AddParent(new Dbref(2));
            sword.AddZone(new Dbref(0));

            // Save to SQLite
            sqlite.Save(originalDb);

            // Load into fresh database
            var loadedDb = new ObjectDatabase();
            bool loaded = sqlite.Load(loadedDb);

            Assert.True(loaded);
            Assert.Equal(originalDb.Count, loadedDb.Count);

            var loadedSword = loadedDb.GetObject(sword.Id);
            Assert.NotNull(loadedSword);
            Assert.Equal("Broadsword", loadedSword.Name);
            Assert.Equal(ObjectType.Thing, loadedSword.Type);
            Assert.Equal(new Dbref(0), loadedSword.Location);
            Assert.Equal(50, loadedSword.Pennies);
            Assert.True(loadedSword.HasFlag("DARK"));
            Assert.Equal("2d6+2", loadedSword.GetAttribute("DAMAGE")?.Value);
            Assert.Contains(new Dbref(2), loadedSword.Parents);
            Assert.Contains(new Dbref(0), loadedSword.Zones);
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }
}
