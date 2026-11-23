namespace PennMUSH.Core.Tests.Database;

using PennMUSH.Core.Database;
using PennMUSH.Core.Types;

public class ObjectIndexTests
{
    [Fact]
    public void PlayerAndNameLookupsFollowRenameAndDestroy()
    {
        var db = new ObjectDatabase();
        var room = db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var god = db.CreateObject("One", ObjectType.Player, new Dbref(1));
        var hero = db.CreateObject("Hero", ObjectType.Player, god.Id);
        hero.Owner = hero.Id;

        Assert.Equal(god.Id, db.FindPlayer("one")!.Id);
        Assert.Equal(hero.Id, db.FindObject("hero")!.Id);
        Assert.Equal(room.Id, db.FindObject("limbo")!.Id);

        hero.Name = "Ada";
        Assert.Null(db.FindPlayer("Hero"));
        Assert.Equal(hero.Id, db.FindPlayer("ada")!.Id);

        hero.Type = ObjectType.Garbage;
        hero.Name = "Garbage";
        Assert.Null(db.FindPlayer("Ada"));
        Assert.DoesNotContain(db.ByType(ObjectType.Player), player => player.Id == hero.Id);
        Assert.Contains(db.ByType(ObjectType.Garbage), obj => obj.Id == hero.Id);
    }

    [Fact]
    public void OwnerZoneAndParentIndexesTrackMembership()
    {
        var db = new ObjectDatabase();
        var god = db.CreateObject("One", ObjectType.Player, new Dbref(1));
        var zone = db.CreateObject("Land", ObjectType.Thing, god.Id);
        var thing = db.CreateObject("Rock", ObjectType.Thing, god.Id);
        thing.AddParent(zone.Id);
        thing.AddZone(zone.Id);

        Assert.Equal(new[] { thing.Id }, db.ChildrenOf(zone.Id).Select(obj => obj.Id));
        Assert.Contains(db.ByZone(zone.Id), obj => obj.Id == thing.Id);
        Assert.Contains(db.ByOwner(god.Id), obj => obj.Id == thing.Id);

        thing.Owner = zone.Id;
        Assert.DoesNotContain(db.ByOwner(god.Id), obj => obj.Id == thing.Id);
        Assert.Contains(db.ByOwner(zone.Id), obj => obj.Id == thing.Id);

        thing.RemoveParent(zone.Id);
        thing.RemoveZone(zone.Id);
        Assert.Empty(db.ChildrenOf(zone.Id));
        Assert.Empty(db.ByZone(zone.Id));
    }

    [Fact]
    public void ExitAliasesResolveFromTheRoomMap()
    {
        var db = new ObjectDatabase();
        var room = db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var other = db.CreateObject("Yard", ObjectType.Room, new Dbref(1));
        var north = db.CreateObject("North;n;out", ObjectType.Exit, new Dbref(1));
        var south = db.CreateObject("South;s", ObjectType.Exit, new Dbref(1));
        db.AttachExit(room.Id, north.Id);
        db.AttachExit(room.Id, south.Id);
        db.LinkExit(north.Id, other.Id);

        Assert.Equal(north.Id, db.FindExit(room.Id, "n")!.Id);
        Assert.Equal(north.Id, db.FindExit(room.Id, "OUT")!.Id);
        Assert.Equal(south.Id, db.FindExit(room.Id, "south")!.Id);
        Assert.Null(db.FindExit(other.Id, "n"));

        north.Name = "East;e";
        Assert.Null(db.FindExit(room.Id, "n"));
        Assert.Equal(north.Id, db.FindExit(room.Id, "e")!.Id);

        db.RemoveExit(room.Id, north.Id);
        Assert.Null(db.FindExit(room.Id, "e"));
        Assert.Equal(south.Id, db.FindExit(room.Id, "s")!.Id);
    }

    [Fact]
    public void LoadedWorldKeepsTheIndexes()
    {
        string path = Path.Combine(Path.GetTempPath(), "index-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var original = new ObjectDatabase();
            var god = WorldBootstrapper.GenerateBaseWorld(original);
            var hero = original.CreateObject("Hero", ObjectType.Player, god.Id);
            hero.Owner = hero.Id;
            var rock = original.CreateObject("Rock", ObjectType.Thing, hero.Id);
            rock.AddParent(new Dbref(2));

            using (var store = new SqliteStore($"Data Source={path}"))
                store.Save(original);

            var loaded = new ObjectDatabase();
            using (var store = new SqliteStore($"Data Source={path}"))
                Assert.True(store.Load(loaded));

            Assert.Equal(hero.Id, loaded.FindPlayer("hero")!.Id);
            Assert.Contains(loaded.ChildrenOf(new Dbref(2)), obj => obj.Id == rock.Id);
            Assert.Contains(loaded.ByOwner(hero.Id), obj => obj.Id == rock.Id);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(path))
            {
                try { File.Delete(path); } catch { }
            }
        }
    }
}
