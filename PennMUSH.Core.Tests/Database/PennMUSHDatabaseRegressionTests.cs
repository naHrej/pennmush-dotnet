namespace PennMUSH.Core.Tests.Database;

using System;
using System.IO;
using System.Linq;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Database;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Types;
using Xunit;

public class PennMUSHDatabaseRegressionTests
{
    private static string GetTestDataPath(string filename)
    {
        // Try output directory first, then fallback to source directory
        string localPath = Path.Combine(AppContext.BaseDirectory, "TestData", filename);
        if (File.Exists(localPath)) return localPath;

        string sourcePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", filename);
        if (File.Exists(sourcePath)) return Path.GetFullPath(sourcePath);

        throw new FileNotFoundException($"Cannot locate test file {filename} at {localPath} or {sourcePath}");
    }

    [Fact]
    public void LoadPennMUSHBaseDump_SucceedsWithExactObjectsAndAttributes()
    {
        string path = GetTestDataPath("pennmush_base.dump");
        var db = new ObjectDatabase();

        using (var reader = new StreamReader(path))
        {
            FlatfileStore.Load(db, reader);
        }

        Assert.Equal(3, db.Count);

        // Object #0: Room Zero
        var roomZero = db.GetObject(new Dbref(0));
        Assert.NotNull(roomZero);
        Assert.Equal("Room Zero", roomZero.Name);
        Assert.Equal(ObjectType.Room, roomZero.Type);
        Assert.Equal(new Dbref(1), roomZero.Owner);
        Assert.Equal(new Dbref(1), roomZero.Contents);
        Assert.Contains("ACCESSED", roomZero.Flags);
        Assert.Equal("You are in Room Zero.  It's very dark here.", roomZero.GetAttribute("DESCRIBE")?.Value);
        Assert.Equal("is briefly visible through the mist.", roomZero.GetAttribute("OSUCCESS")?.Value);

        // Object #1: One (God / Wizard player)
        var one = db.GetObject(new Dbref(1));
        Assert.NotNull(one);
        Assert.Equal("One", one.Name);
        Assert.Equal(ObjectType.Player, one.Type);
        Assert.Equal(new Dbref(1), one.Owner);
        Assert.Equal(new Dbref(0), one.Location);
        Assert.Contains("WIZARD", one.Flags);
        Assert.Contains("CONNECT", one.Flags);
        Assert.Equal("You see Number One.", one.GetAttribute("DESCRIBE")?.Value);
        Assert.Equal("Sun Jan 17 18:50:49 1993", one.GetAttribute("LAST")?.Value);
        Assert.Equal("disco.seas.upenn.edu", one.GetAttribute("LASTSITE")?.Value);

        // Object #2: Master Room
        var masterRoom = db.GetObject(new Dbref(2));
        Assert.NotNull(masterRoom);
        Assert.Equal("Master Room", masterRoom.Name);
        Assert.Equal(ObjectType.Room, masterRoom.Type);
        Assert.Equal(new Dbref(1), masterRoom.Owner);
        Assert.Contains("FLOATING", masterRoom.Flags);
        Assert.StartsWith("This is the master room", masterRoom.GetAttribute("DESCRIBE")?.Value);
    }

    [Fact]
    public void LoadPennMUSH214Dump_SucceedsWithFullWorldAndSoftcode()
    {
        string path = GetTestDataPath("pennmush_214.dump");
        var db = new ObjectDatabase();

        using (var reader = new StreamReader(path))
        {
            FlatfileStore.Load(db, reader);
        }

        Assert.Equal(214, db.Count);

        // #0: Room Zero
        var r0 = db.GetObject(new Dbref(0));
        Assert.NotNull(r0);
        Assert.Equal("Room Zero", r0.Name);
        Assert.Equal(ObjectType.Room, r0.Type);
        Assert.Equal(new Dbref(5), r0.Contents);

        // #1: One (Wizard)
        var one = db.GetObject(new Dbref(1));
        Assert.NotNull(one);
        Assert.Equal("One", one.Name);
        Assert.Equal(ObjectType.Player, one.Type);
        Assert.Contains("WIZARD", one.Flags);
        Assert.Contains("ANSI", one.Flags);
        Assert.Contains("COLOR", one.Flags);
        Assert.Equal("127.0.0.1", one.GetAttribute("LASTIP")?.Value);
        Assert.Equal("AMZN219417B.ant.amazon.com", one.GetAttribute("LASTSITE")?.Value);

        // #2: Master Room
        var mRoom = db.GetObject(new Dbref(2));
        Assert.NotNull(mRoom);
        Assert.Equal("Master Room", mRoom.Name);
        Assert.Contains("FLOATING", mRoom.Flags);

        // #3: Wizard3
        var wiz3 = db.GetObject(new Dbref(3));
        Assert.NotNull(wiz3);
        Assert.Equal("Wizard3", wiz3.Name);
        Assert.Equal(ObjectType.Player, wiz3.Type);
        Assert.Equal(150, wiz3.Pennies);
        Assert.Contains("WIZARD", wiz3.Flags);
        Assert.Contains("SAFE", wiz3.Flags);
        Assert.Equal("Y", wiz3.GetAttribute("ALIAS")?.Value);
        Assert.NotNull(wiz3.GetLock("Use"));
        Assert.NotNull(wiz3.GetLock("Enter"));
        Assert.NotNull(wiz3.GetLock("Basic"));

        // #29: JHV Mordak (Thing with eval lock on Zone)
        var obj29 = db.GetObject(new Dbref(29));
        Assert.NotNull(obj29);
        Assert.Equal("JHV Mordak", obj29.Name);
        Assert.Equal(ObjectType.Thing, obj29.Type);
        Assert.NotNull(obj29.GetLock("Zone"));

        // #212 was garbage in PennMUSH dump and automatically filled
        var obj212 = db.GetObject(new Dbref(212));
        Assert.NotNull(obj212);
        Assert.Equal(ObjectType.Garbage, obj212.Type);

        // #213: Last object in dump
        var obj213 = db.GetObject(new Dbref(213));
        Assert.NotNull(obj213);

        // Verify softcode attribute $WIPESHIP
        var shipWithWipe = db.GetAllObjects().FirstOrDefault(o => o.GetAttribute("WIPE_SHIP") != null);
        Assert.NotNull(shipWithWipe);
        Assert.StartsWith("$WIPESHIP:", shipWithWipe.GetAttribute("WIPE_SHIP")?.Value);
    }

    [Fact]
    public void FlatfileStore_SaveAndReloadPennMUSH214_RoundtripsSuccessfully()
    {
        string path = GetTestDataPath("pennmush_214.dump");
        var originalDb = new ObjectDatabase();

        using (var reader = new StreamReader(path))
        {
            FlatfileStore.Load(originalDb, reader);
        }

        var sw = new StringWriter();
        FlatfileStore.Save(originalDb, sw);
        string dump = sw.ToString();

        var reloadedDb = new ObjectDatabase();
        using (var reader = new StringReader(dump))
        {
            FlatfileStore.Load(reloadedDb, reader);
        }

        Assert.Equal(originalDb.Count, reloadedDb.Count);

        for (int i = 0; i < originalDb.Count; i++)
        {
            var orig = originalDb.GetObject(new Dbref(i));
            var reloaded = reloadedDb.GetObject(new Dbref(i));

            Assert.NotNull(orig);
            Assert.NotNull(reloaded);
            Assert.Equal(orig.Name, reloaded.Name);
            Assert.Equal(orig.Type, reloaded.Type);
            Assert.Equal(orig.Location, reloaded.Location);
            Assert.Equal(orig.Owner, reloaded.Owner);
            Assert.Equal(orig.Attributes.Count, reloaded.Attributes.Count);
        }
    }

    [Fact]
    public void LoadPennMUSH188Outdb_SucceedsWithLabeledObjectsLocksAndMultilineAttributes()
    {
        string path = GetTestDataPath("pennmush-1.8.8p0.outdb");
        var db = new ObjectDatabase();

        using (var reader = new StreamReader(path))
        {
            FlatfileStore.Load(db, reader);
        }

        Assert.Equal(10, db.Count);

        var r0 = db.GetObject(new Dbref(0));
        Assert.NotNull(r0);
        Assert.Equal("Room Zero", r0.Name);
        Assert.Equal(ObjectType.Room, r0.Type);
        Assert.Contains("LINK_OK", r0.Flags);
        var r0Desc = r0.GetAttribute("DESCRIBE");
        Assert.NotNull(r0Desc);
        Assert.Equal("You are in Room Zero.", r0Desc.Value);
        Assert.True(r0Desc.Flags.HasFlag(AttributeFlags.NoProg));
        Assert.True(r0Desc.Flags.HasFlag(AttributeFlags.Visual));
        Assert.True(r0Desc.Flags.HasFlag(AttributeFlags.Public));

        var one = db.GetObject(new Dbref(1));
        Assert.NotNull(one);
        Assert.Equal("One", one.Name);
        Assert.Equal(ObjectType.Player, one.Type);
        Assert.Equal(150, one.Pennies);
        Assert.Contains("WIZARD", one.Flags);
        Assert.Contains("CONNECTED", one.Flags);
        var basicLock = one.GetLock("Basic");
        Assert.NotNull(basicLock);
        Assert.Equal(LockFlags.Private, basicLock.Flags);
        Assert.Equal(9, one.Attributes.Count);
        Assert.Equal("localhost", one.GetAttribute("LASTSITE")?.Value);

        var north = db.GetObject(new Dbref(6));
        Assert.NotNull(north);
        Assert.Equal("North", north.Name);
        Assert.Equal(ObjectType.Exit, north.Type);
        Assert.Equal(new Dbref(5), north.Location);
        Assert.Equal("n", north.GetAttribute("ALIAS")?.Value);

        var widget = db.GetObject(new Dbref(8));
        Assert.NotNull(widget);
        Assert.Equal("Widget", widget.Name);
        Assert.Equal(ObjectType.Thing, widget.Type);
        Assert.Contains(new Dbref(9), widget.Parents);
        Assert.Contains(new Dbref(9), widget.Zones);
        Assert.NotEqual(0u, widget.Warnings);
        Assert.NotNull(widget.GetLock("Basic"));
        Assert.NotNull(widget.GetLock("Use"));

        string? multi = widget.GetAttribute("MULTI")?.Value.Replace("\r\n", "\n");
        Assert.Equal("first line\nsecond line", multi);
        Assert.Equal("He said \"hi\" \\ back", widget.GetAttribute("NOTE")?.Value);
    }

    [Fact]
    public void LoadPennMUSH188OutdbWithHoles_FillsGarbageSlots()
    {
        string path = GetTestDataPath("pennmush-1.8.8p0-holes.outdb");
        var db = new ObjectDatabase();

        using (var reader = new StreamReader(path))
        {
            FlatfileStore.Load(db, reader);
        }

        Assert.Equal(15, db.Count);
        Assert.Equal(ObjectType.Garbage, db.GetObject(new Dbref(7))?.Type);
        Assert.Equal(ObjectType.Garbage, db.GetObject(new Dbref(8))?.Type);
        Assert.Equal(ObjectType.Garbage, db.GetObject(new Dbref(10))?.Type);
        Assert.NotEqual(ObjectType.Garbage, db.GetObject(new Dbref(9))?.Type);
        Assert.Equal("North", db.GetObject(new Dbref(14))?.Name);
    }

    [Fact]
    public void LoadPennMUSH188MinimalPositiveHeader_SucceedsWithDbVersion6()
    {
        const string dump = """
            +V1282
            dbversion 6
            savedtime "Fri Jan 01 00:00:00 2026"
            +FLAGS LIST
            flagcount 0
            flagaliascount 0
            +POWER LIST
            flagcount 0
            flagaliascount 0
            +ATTRIBUTES LIST
            attrcount 0
            attraliascount 0
            ~3
            !0
            name "Room Zero"
            location #-1
            contents #-1
            exits #-1
            next #-1
            parent #-1
            lockcount 0
            owner #1
            zone #-1
            pennies 0
            type 1
            flags ""
            powers ""
            warnings ""
            created 100
            modified 101
            attrcount 0
            !2
            name "Player Two"
            location #0
            contents #-1
            exits #-1
            next #-1
            parent #-1
            lockcount 1
             type "Basic"
              creator #2
              flags "visual"
              derefs 0
              key "#2"
            owner #2
            zone #-1
            pennies 10
            type 8
            flags "CONNECTED"
            powers "BOOT"
            warnings "exit-unlinked"
            created 200
            modified 201
            attrcount 1
             name "DESCRIBE"
              owner #2
              flags "wizard"
              derefs 1
              value "A player"
            ***END OF DUMP***
            """;

        var db = new ObjectDatabase();
        using var reader = new StringReader(dump);
        FlatfileStore.Load(db, reader);

        Assert.Equal(3, db.Count);
        Assert.Equal(ObjectType.Garbage, db.GetObject(new Dbref(1))?.Type);

        var p2 = db.GetObject(new Dbref(2));
        Assert.NotNull(p2);
        Assert.Equal("Player Two", p2.Name);
        Assert.Equal(ObjectType.Player, p2.Type);
        Assert.Contains("CONNECTED", p2.Flags);
        Assert.Contains("BOOT", p2.Powers);
        Assert.NotEqual(0u, p2.Warnings);
        Assert.Equal(LockFlags.Visual, p2.GetLock("Basic")?.Flags);
        Assert.Equal("A player", p2.GetAttribute("DESCRIBE")?.Value);
    }
}
