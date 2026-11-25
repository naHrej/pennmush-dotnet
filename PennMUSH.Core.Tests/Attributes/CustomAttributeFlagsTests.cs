namespace PennMUSH.Core.Tests.Attributes;

using System;
using System.IO;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class CustomAttributeFlagsTests
{
    [Fact]
    public void RegisterCustomFlag_ValidatesAndRejectsBuiltIns()
    {
        Assert.False(StandardAttributes.RegisterCustomFlag(""));
        Assert.False(StandardAttributes.RegisterCustomFlag("none"));
        Assert.False(StandardAttributes.RegisterCustomFlag("visual"));
        Assert.False(StandardAttributes.RegisterCustomFlag("bad flag"));

        string flag = "test_type_int";
        try
        {
            Assert.True(StandardAttributes.RegisterCustomFlag(flag));
            Assert.True(StandardAttributes.IsCustomFlag("TEST_TYPE_INT"));
            Assert.Contains(flag, StandardAttributes.GetCustomFlags());
        }
        finally
        {
            StandardAttributes.UnregisterCustomFlag(flag);
        }

        Assert.False(StandardAttributes.IsCustomFlag(flag));
    }

    [Fact]
    public void SetCommand_TogglesCustomAttributeFlagsAndDecompiles()
    {
        const string flag = "script_int";
        StandardAttributes.RegisterCustomFlag(flag);
        try
        {
            var db = new ObjectDatabase();
            var god = db.CreateObject("One", ObjectType.Player, new Dbref(1));
            god.Owner = god.Id;
            god.SetAttribute("HP", "100", god.Id);

            var dispatcher = new CommandDispatcher(new ExpressionEvaluator());
            dispatcher.Register(new SetCommandHandler(), "@set");
            dispatcher.Register(new DecompileCommandHandler(), "@decompile");

            var setCtx = dispatcher.Dispatch(god.Id, "@set me/HP=script_int", db);
            Assert.Contains("Set.", setCtx.Notifications);
            Assert.True(god.GetAttribute("HP")!.HasCustomFlag("SCRIPT_INT"));
            Assert.Contains("script_int", StandardAttributes.DescribeFlags(god.GetAttribute("HP")!.Flags, god.GetAttribute("HP")!.CustomFlags));

            var decCtx = dispatcher.Dispatch(god.Id, "@decompile me/HP", db);
            Assert.Contains(decCtx.Notifications, line => line.Contains("@set me/HP=script_int", StringComparison.Ordinal));

            var clearCtx = dispatcher.Dispatch(god.Id, "@set me/HP=!script_int", db);
            Assert.Contains("Set.", clearCtx.Notifications);
            Assert.False(god.GetAttribute("HP")!.HasCustomFlag(flag));
        }
        finally
        {
            StandardAttributes.UnregisterCustomFlag(flag);
        }
    }

    [Fact]
    public void SqliteStore_And_FlatfileStore_RoundTripCustomFlags()
    {
        const string flagA = "script_list";
        const string flagB = "script_map";
        StandardAttributes.RegisterCustomFlag(flagA);
        StandardAttributes.RegisterCustomFlag(flagB);

        string tempDir = Path.Combine(Path.GetTempPath(), $"pennmush_custom_flags_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        string sqlitePath = Path.Combine(tempDir, "test.db");

        string connStr = $"Data Source={sqlitePath};Pooling=False";

        try
        {
            var db = new ObjectDatabase();
            var obj = db.CreateObject("Thing", ObjectType.Thing, new Dbref(0));
            obj.SetAttribute("PLAIN", "abc", obj.Id, AttributeFlags.Visual);
            obj.SetAttribute("TYPED", "[1,2,3]", obj.Id, AttributeFlags.Visual, customFlags: new[] { flagA, flagB });

            using (var store = new SqliteStore(connStr))
            {
                store.InitializeSchema();
                store.Save(db);
            }

            var loadedSqlite = new ObjectDatabase();
            using (var store = new SqliteStore(connStr))
            {
                store.InitializeSchema();
                store.Load(loadedSqlite);
            }

            var sqliteTyped = loadedSqlite.GetObject(obj.Id)!.GetAttribute("TYPED")!;
            Assert.True(sqliteTyped.Flags.HasFlag(AttributeFlags.Visual));
            Assert.True(sqliteTyped.HasCustomFlag(flagA));
            Assert.True(sqliteTyped.HasCustomFlag(flagB));
            Assert.Empty(loadedSqlite.GetObject(obj.Id)!.GetAttribute("PLAIN")!.CustomFlags);

            using var sw = new StringWriter();
            FlatfileStore.Save(db, sw);
            string dump = sw.ToString();
            Assert.DoesNotContain("]PLAIN^0^512^", dump, StringComparison.Ordinal);

            var loadedFlat = new ObjectDatabase();
            using var sr = new StringReader(dump);
            FlatfileStore.Load(loadedFlat, sr);

            var flatTyped = loadedFlat.GetObject(obj.Id)!.GetAttribute("TYPED")!;
            Assert.True(flatTyped.Flags.HasFlag(AttributeFlags.Visual));
            Assert.True(flatTyped.HasCustomFlag(flagA));
            Assert.True(flatTyped.HasCustomFlag(flagB));
        }
        finally
        {
            StandardAttributes.UnregisterCustomFlag(flagA);
            StandardAttributes.UnregisterCustomFlag(flagB);
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Examine_DisplaysCustomAttributeFlags_And_PlayerDollarCommandsMatch()
    {
        const string flag = "custom_tag";
        StandardAttributes.RegisterCustomFlag(flag);
        try
        {
            var db = new ObjectDatabase();
            var room = db.CreateObject("Room Zero", ObjectType.Room, new Dbref(1));
            var god = db.CreateObject("One", ObjectType.Player, new Dbref(1));
            god.Owner = god.Id;
            god.Location = room.Id; // Intentionally not linked in room.Contents (matches WorldBootstrapper)
            god.SetAttribute("MYATTR", "hello", god.Id, AttributeFlags.Visual, customFlags: new[] { flag });
            god.SetAttribute("CMD_TEST", "$+ping *:@pemit %#=Pong:%0", god.Id);

            var dispatcher = new CommandDispatcher(new ExpressionEvaluator());
            dispatcher.Register(new ExamineCommandHandler(), "examine", "ex");
            dispatcher.Register(new PemitCommandHandler(), "@pemit");

            var exObj = dispatcher.Dispatch(god.Id, "examine me", db);
            Assert.Contains(exObj.Notifications, n => n.Contains("MYATTR [#1v custom_tag]: hello", StringComparison.Ordinal));

            var exAttr = dispatcher.Dispatch(god.Id, "examine me/MYATTR", db);
            Assert.Contains(exAttr.Notifications, n => n.Contains("MYATTR [#1v custom_tag]: hello", StringComparison.Ordinal));

            var cmdCtx = dispatcher.Dispatch(god.Id, "+ping world", db);
            Assert.Single(cmdCtx.TargetedNotifications);
            Assert.Equal("Pong:world", cmdCtx.TargetedNotifications[0].Message);

            // Also verify no double-execution when player IS in room.Contents
            db.MoveObject(god.Id, room.Id);
            var cmdCtx2 = dispatcher.Dispatch(god.Id, "+ping again", db);
            Assert.Single(cmdCtx2.TargetedNotifications);
            Assert.Equal("Pong:again", cmdCtx2.TargetedNotifications[0].Message);
        }
        finally
        {
            StandardAttributes.UnregisterCustomFlag(flag);
        }
    }
}
