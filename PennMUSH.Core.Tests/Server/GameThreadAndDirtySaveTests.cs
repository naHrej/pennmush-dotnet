namespace PennMUSH.Core.Tests.Server;

using System.Threading;
using Microsoft.Data.Sqlite;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;
using PennMUSH.Server.Engine;
using PennMUSH.Server.Session;

public class GameThreadAndDirtySaveTests
{
    [Fact]
    public async Task GameMailbox_RunsWorldWorkOneAtATime()
    {
        using var mailbox = new GameMailbox();
        int depth = 0;
        int max = 0;

        var tasks = Enumerable.Range(0, 8).Select(_ => mailbox.RunAsync(() =>
        {
            int now = Interlocked.Increment(ref depth);
            int seen = Volatile.Read(ref max);
            while (seen < now && Interlocked.CompareExchange(ref max, now, seen) != seen)
                seen = Volatile.Read(ref max);
            Thread.Sleep(20);
            Interlocked.Decrement(ref depth);
        })).ToArray();

        await Task.WhenAll(tasks);
        Assert.Equal(1, max);
    }

    [Fact]
    public void GameMailbox_RunReturnsTheValue()
    {
        using var mailbox = new GameMailbox();
        Assert.Equal(7, mailbox.Run(() => 7));
    }

    [Fact]
    public async Task TwoSessions_RunCommandsWithoutLosingEitherEdit()
    {
        string path = Path.Combine(Path.GetTempPath(), "game-thread-" + Guid.NewGuid().ToString("N") + ".db");
        var server = new MUSHServer(path) { AutosaveEnabled = false };
        try
        {
            var one = new MUSHConnection(_ => Task.CompletedTask);
            var heroConn = new MUSHConnection(_ => Task.CompletedTask);
            server.RegisterConnection(one);
            server.RegisterConnection(heroConn);
            await server.HandleInputAsync(one, "connect One one");
            await server.HandleInputAsync(heroConn, "create Hero secret");

            await Task.WhenAll(
                server.HandleInputAsync(one, "@set me=ALPHA:one"),
                server.HandleInputAsync(heroConn, "@set me=BETA:hero"));

            var god = server.Database.GetObject(new Dbref(1));
            var hero = server.Database.GetAllObjects().First(o => o.Name == "Hero");
            Assert.Equal("one", god!.GetAttribute("ALPHA")?.Value);
            Assert.Equal("hero", hero.GetAttribute("BETA")?.Value);
        }
        finally
        {
            server.Stop();
            TryDelete(path);
        }
    }

    [Fact]
    public async Task DirtyFlush_KeepsAnAttributeEditAndAChannelAcrossRestart()
    {
        string path = Path.Combine(Path.GetTempPath(), "dirty-save-" + Guid.NewGuid().ToString("N") + ".db");
        var server = new MUSHServer(path) { AutosaveEnabled = false };
        try
        {
            var conn = new MUSHConnection(_ => Task.CompletedTask);
            server.RegisterConnection(conn);
            await server.HandleInputAsync(conn, "connect One one");
            await server.HandleInputAsync(conn, "@set me=NOTE:kept");
            await server.HandleInputAsync(conn, "@channel/add Bulletin");
            await server.HandleInputAsync(conn, "@sitelock/ban 203.0.113.5");

            AssertMissing(path);

            server.SaveAll();

            using (var store = new SqliteStore($"Data Source={path}"))
            {
                var db = new ObjectDatabase();
                Assert.True(store.Load(db));
                Assert.Equal("kept", db.GetObject(new Dbref(1))!.GetAttribute("NOTE")?.Value);
            }

            var channels = new ChannelRegistry();
            var locks = new PennMUSH.Core.Access.SiteLockTable();
            new RuntimeStateStore($"Data Source={path}").Load(channels, siteLocks: locks);
            Assert.NotNull(channels.GetChannel("Bulletin"));
            Assert.Contains(locks.Rules, rule => rule.Host == "203.0.113.5");
        }
        finally
        {
            server.Stop();
            TryDelete(path);
        }
    }

    private static void AssertMissing(string path)
    {
        using var store = new SqliteStore($"Data Source={path}");
        var db = new ObjectDatabase();
        store.Load(db);
        Assert.Null(db.GetObject(new Dbref(1))!.GetAttribute("NOTE"));

        var channels = new ChannelRegistry();
        var locks = new PennMUSH.Core.Access.SiteLockTable();
        new RuntimeStateStore($"Data Source={path}").Load(channels, siteLocks: locks);
        Assert.Null(channels.GetChannel("Bulletin"));
        Assert.DoesNotContain(locks.Rules, rule => rule.Host == "203.0.113.5");
    }

    private static void TryDelete(string path)
    {
        SqliteConnection.ClearAllPools();
        foreach (string suffix in new[] { "", "-wal", "-shm" })
        {
            string file = path + suffix;
            if (!File.Exists(file))
                continue;
            try { File.Delete(file); } catch { }
        }
    }
}
