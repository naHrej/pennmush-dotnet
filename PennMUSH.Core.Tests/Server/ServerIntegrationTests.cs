namespace PennMUSH.Core.Tests.Server;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;
using PennMUSH.Server.Engine;
using PennMUSH.Server.Session;

public class ServerIntegrationTests
{
    [Fact]
    public void MUSHServer_StartsAndAutoBootstrapsPristineWorld()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"server_boot_{Guid.NewGuid():N}.db");
        try
        {
            var server = new MUSHServer(dbPath);
            Assert.True(server.Database.Count >= 3);

            var roomZero = server.Database.GetObject(new Dbref(0));
            Assert.NotNull(roomZero);
            Assert.Equal("Room Zero", roomZero.Name);

            var god = server.Database.GetObject(new Dbref(1));
            Assert.NotNull(god);
            Assert.Equal("One", god.Name);
            Assert.True(god.IsGod);

            var master = server.Database.GetObject(new Dbref(2));
            Assert.NotNull(master);
            Assert.Equal("Master Room", master.Name);

            Assert.NotNull(server.Channels.GetChannel("Public"));
            server.Stop();
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }

    [Fact]
    public async Task MUSHServer_CharacterCreation_LoginAndCommandFlow()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"server_char_{Guid.NewGuid():N}.db");
        try
        {
            var server = new MUSHServer(dbPath);
            var outputs = new List<string>();

            var conn = new MUSHConnection(line =>
            {
                outputs.Add(line);
                return Task.CompletedTask;
            });

            server.RegisterConnection(conn);

            // 1. Create character
            await server.HandleInputAsync(conn, "create Hero secret123");
            Assert.Contains(outputs, o => o.Contains("Character Hero created!"));
            Assert.True(conn.PlayerId.IsValid);

            outputs.Clear();

            // 2. In-game speech
            await server.HandleInputAsync(conn, "say Hello world!");
            Assert.Contains(outputs, o => o.Contains("You say, \"Hello world!\""));

            outputs.Clear();

            // 3. Channel chat on Public
            await server.HandleInputAsync(conn, "+c Public=Testing the public channel");
            Assert.Contains(outputs, o => o.Contains("[Public] Hero: Testing the public channel"));

            outputs.Clear();

            // 4. In-game mail status
            await server.HandleInputAsync(conn, "@mail/status");
            Assert.Contains(outputs, o => o.Contains("Mail status: 0 message(s)"));

            // Disconnect
            server.UnregisterConnection(conn);
            server.Stop();
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }

    [Fact]
    public async Task MUSHServer_MultiSession_RoomBroadcastWorksCorrectly()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"server_multi_{Guid.NewGuid():N}.db");
        try
        {
            var server = new MUSHServer(dbPath);
            var aliceOutputs = new List<string>();
            var bobOutputs = new List<string>();

            var aliceConn = new MUSHConnection(line =>
            {
                aliceOutputs.Add(line);
                return Task.CompletedTask;
            });

            var bobConn = new MUSHConnection(line =>
            {
                bobOutputs.Add(line);
                return Task.CompletedTask;
            });

            server.RegisterConnection(aliceConn);
            server.RegisterConnection(bobConn);

            await server.HandleInputAsync(aliceConn, "create Alice pass1");
            await server.HandleInputAsync(bobConn, "create Bob pass2");

            bobOutputs.Clear();

            // Alice says something in the room
            server.BroadcastToRoom(new Dbref(0), "Alice waves to the room.", aliceConn.PlayerId);

            // Bob should receive it
            Assert.Contains(bobOutputs, o => o.Contains("Alice waves to the room."));

            server.UnregisterConnection(aliceConn);
            server.UnregisterConnection(bobConn);
            server.Stop();
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }

    [Fact]
    public async Task MUSHServer_DeliversConnectScreenAndMotd()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"server_connect_{Guid.NewGuid():N}.db");
        try
        {
            var server = new MUSHServer(dbPath);
            var outputs = new List<string>();

            var conn = new MUSHConnection(line =>
            {
                outputs.Add(line);
                return Task.CompletedTask;
            });

            // On connection, connect screen should be delivered
            server.RegisterConnection(conn);
            Assert.NotEmpty(outputs);
            Assert.Contains(outputs, o => o.Contains("connect") || o.Contains("PennMUSH"));

            outputs.Clear();

            // Connect as God (#1 "One", default empty or "one")
            await server.HandleInputAsync(conn, "connect One one");
            Assert.Contains(outputs, o => o.Contains("Welcome back to PennMUSH"));

            outputs.Clear();

            // Help
            outputs.Clear();
            await server.HandleInputAsync(conn, "help look");
            Assert.NotEmpty(outputs);
            Assert.Contains(outputs, o => o.Contains("LOOK") || o.Contains("look"));

            // Quit
            outputs.Clear();
            await server.HandleInputAsync(conn, "QUIT");
            Assert.Contains(outputs, o => o.Contains("Thank you for visiting") || o.Contains("Goodbye"));

            server.Stop();
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }

    [Fact]
    public async Task MUSHServerHost_HotReboots_WithoutDisconnectingPlayerConnections()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"server_reboot_{Guid.NewGuid():N}.db");
        try
        {
            var host = new MUSHServerHost(dbPath);
            var connOutputs = new List<string>();

            var conn = new MUSHConnection(line =>
            {
                connOutputs.Add(line);
                return Task.CompletedTask;
            });

            // 1. Connect player and authenticate
            host.RegisterConnection(conn);
            await host.HandleInputAsync(conn, "connect One one");
            Assert.True(conn.PlayerId.IsValid);
            Assert.Equal(1, conn.PlayerId.Value);

            // 2. Perform in-world modifications
            await host.HandleInputAsync(conn, "@dig Starship Deck");

            connOutputs.Clear();

            // 3. Trigger hot-reboot via @reboot command
            await host.HandleInputAsync(conn, "@reboot");

            // Allow async task to complete reboot
            await Task.Delay(400);

            // 4. Verify sockets were NEVER disconnected and player remained authenticated
            Assert.True(conn.IsConnected);
            Assert.Equal(1, host.Connections.Count);
            Assert.Equal(conn.Id, host.Connections.First().Id);
            Assert.Equal(1, host.Connections.First().PlayerId.Value);

            // Verify hot-reboot broadcast was received by client
            Assert.Contains(connOutputs, o => o.Contains("Hot-reboot") || o.Contains("reloaded with 0 disconnects"));

            // 5. Verify commands still execute seamlessly on reloaded engine
            connOutputs.Clear();
            await host.HandleInputAsync(conn, "think [add(20,22)]");
            Assert.Contains(connOutputs, o => o.Contains("42"));

            host.Dispose();
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }

    [Fact]
    public async Task ShutdownAnnouncesThenStops()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"server_shutdown_{Guid.NewGuid():N}.db");
        try
        {
            var host = new MUSHServerHost(dbPath);
            var stopped = false;
            host.ProcessExit = () => stopped = true;
            var connOutputs = new List<string>();
            var conn = new MUSHConnection(line =>
            {
                connOutputs.Add(line);
                return Task.CompletedTask;
            });
            host.RegisterConnection(conn);
            await host.HandleInputAsync(conn, "connect One one");
            connOutputs.Clear();

            await host.HandleInputAsync(conn, "@shutdown");

            Assert.Contains(connOutputs, line => line.Contains("GAME: Shutdown by One"));
            Assert.True(stopped);
            host.Dispose();
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }

    [Fact]
    public async Task ShutdownRebootKeepsTheConnection()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"server_shutdown_reboot_{Guid.NewGuid():N}.db");
        try
        {
            var host = new MUSHServerHost(dbPath);
            var connOutputs = new List<string>();
            var conn = new MUSHConnection(line =>
            {
                connOutputs.Add(line);
                return Task.CompletedTask;
            });
            host.RegisterConnection(conn);
            await host.HandleInputAsync(conn, "connect One one");
            connOutputs.Clear();

            await host.HandleInputAsync(conn, "@shutdown/reboot");

            Assert.Contains(connOutputs, line => line.Contains("GAME: Reboot w/o disconnect by One, please wait."));
            Assert.Contains(connOutputs, line => line.Contains("GAME: Reboot finished."));
            Assert.True(conn.IsConnected);
            Assert.Equal(1, conn.PlayerId.Value);

            connOutputs.Clear();
            await host.HandleInputAsync(conn, "think [add(20,22)]");
            Assert.Contains(connOutputs, line => line.Contains("42"));
            host.Dispose();
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }

    [Fact]
    public async Task BannedHostCannotConnect()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"server_sitelock_{Guid.NewGuid():N}.db");
        try
        {
            var host = new MUSHServerHost(dbPath);
            var wizard = new MUSHConnection(line => Task.CompletedTask);
            wizard.Host = "192.0.2.1";
            host.RegisterConnection(wizard);
            await host.HandleInputAsync(wizard, "connect One one");

            await host.HandleInputAsync(wizard, "@sitelock/ban 192.0.2.*");

            var outputs = new List<string>();
            var banned = new MUSHConnection(line =>
            {
                outputs.Add(line);
                return Task.CompletedTask;
            });
            banned.Host = "192.0.2.8";
            host.RegisterConnection(banned);
            await host.HandleInputAsync(banned, "connect One one");

            Assert.Contains(outputs, line => line.Contains("Player connections not allowed."));
            Assert.False(banned.PlayerId.IsValid);
            host.Dispose();
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }
}
