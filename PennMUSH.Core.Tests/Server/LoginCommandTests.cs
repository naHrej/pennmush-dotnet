namespace PennMUSH.Core.Tests.Server;

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PennMUSH.Server.Engine;
using PennMUSH.Server.Session;

public class LoginCommandTests
{
    [Fact]
    public async Task ShortConnect_LogsInThePlayer()
    {
        var (server, path) = Start();
        try
        {
            var conn = new MUSHConnection(_ => Task.CompletedTask);
            server.RegisterConnection(conn);

            await server.HandleInputAsync(conn, "co One");

            Assert.True(conn.PlayerId.IsValid);
        }
        finally
        {
            Stop(server, path);
        }
    }

    [Fact]
    public async Task ShortCreate_CreatesThePlayer()
    {
        var (server, path) = Start();
        try
        {
            var conn = new MUSHConnection(_ => Task.CompletedTask);
            server.RegisterConnection(conn);

            await server.HandleInputAsync(conn, "cr Tester secret");

            Assert.True(conn.PlayerId.IsValid);
            Assert.Equal("Tester", server.Database.GetObject(conn.PlayerId)!.Name);
        }
        finally
        {
            Stop(server, path);
        }
    }

    [Fact]
    public async Task UnknownShortForm_StaysAtLogin()
    {
        var (server, path) = Start();
        try
        {
            var messages = new List<string>();
            var conn = new MUSHConnection(message =>
            {
                messages.Add(message);
                return Task.CompletedTask;
            });
            server.RegisterConnection(conn);
            messages.Clear();

            await server.HandleInputAsync(conn, "con One");

            Assert.False(conn.PlayerId.IsValid);
            Assert.Contains(messages, message => message.Contains("Valid login commands"));
        }
        finally
        {
            Stop(server, path);
        }
    }

    private static (MUSHServer Server, string Path) Start()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "login-" + Guid.NewGuid().ToString("N") + ".db");
        return (new MUSHServer(path) { AutosaveEnabled = false }, path);
    }

    private static void Stop(MUSHServer server, string path)
    {
        server.Stop();
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
