namespace PennMUSH.Server.Engine;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PennMUSH.Core.Access;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Database;
using PennMUSH.Server.Session;

public class MUSHServerHost : IDisposable
{
    private readonly object _rebootLock = new();
    private readonly ConcurrentDictionary<string, MUSHConnection> _activeConnections = new();
    private readonly string _dbPath;
    private readonly string? _configPath;
    private readonly SiteLockTable _siteLocks = new();
    private string? _importFlatfilePath;
    private MUSHServer _server;

    public MUSHServer Server => _server;
    public ObjectDatabase Database => _server.Database;
    public ChannelRegistry Channels => _server.Channels;
    public IReadOnlyCollection<MUSHConnection> Connections => _activeConnections.Values.ToList();

    public MUSHServerHost(string dbPath = "mush.db", string? configPath = null, string? importFlatfilePath = null)
    {
        _dbPath = dbPath;
        _configPath = configPath;
        _importFlatfilePath = importFlatfilePath;
        _server = CreateServer();
    }

    private MUSHServer CreateServer()
    {
        string? importPath = _importFlatfilePath;
        _importFlatfilePath = null; // Never re-import over live state on subsequent hot-reboots.
        var server = new MUSHServer(_dbPath, _configPath, _siteLocks, importPath);
        server.OnRebootRequested += () => Reboot();
        server.OnQuietRebootRequested += () => Reboot(announce: false);
        server.OnShutdownRequested += () => Shutdown();
        return server;
    }

    public void RegisterConnection(MUSHConnection conn)
    {
        _activeConnections[conn.Id] = conn;
        _server.RegisterConnection(conn);
    }

    public void UnregisterConnection(MUSHConnection conn)
    {
        _activeConnections.TryRemove(conn.Id, out _);
        _server.UnregisterConnection(conn);
    }

    public Task HandleInputAsync(MUSHConnection conn, string message)
    {
        return _server.HandleInputAsync(conn, message);
    }

    /// <summary>
    /// Executes a hot-reboot: saves the SQLite database, cleanly unloads extensions (persisting
    /// starship states and systems), instantiates a fresh MUSHServer engine, and re-binds all
    /// active client sockets and sessions with zero disconnects.
    /// </summary>
    public Action? ProcessExit { get; set; }

    public void Reboot(bool announce = true)
    {
        _server.Run(() =>
        {
            lock (_rebootLock)
                RebootCore(announce);
        });
    }

    private void RebootCore(bool announce)
    {
        Console.WriteLine("[MUSHServerHost] Executing hot-reboot of game engine...");

            if (announce)
            {
                foreach (var conn in _activeConnections.Values)
                {
                    _ = conn.SendLineAsync("<div class=\"system-msg\">*** Hot-reboot: Persisting universe and reloading engine (sockets preserved)... ***</div>");
                }
            }

            // 2. Prepare old server for reboot (persists database, shuts down extensions cleanly)
            _server.PrepareForReboot();

            // 3. Re-instantiate new MUSHServer instance (re-loads database, cron, extensions)
            _server = CreateServer();

            // 4. Re-attach every active connection to the new server instance!
            foreach (var conn in _activeConnections.Values)
            {
                _server.RegisterConnection(conn, isReconnect: true);
                _ = conn.SendLineAsync(announce
                    ? "<div class=\"system-msg\">*** Hot-reboot complete. Engine reloaded with 0 disconnects. ***</div>"
                    : "GAME: Reboot finished.");
            }

        Console.WriteLine($"[MUSHServerHost] Hot-reboot completed successfully. {_activeConnections.Count} player connections preserved.");
    }

    public void Shutdown()
    {
        _server.Run(() =>
        {
            lock (_rebootLock)
            {
                _server.Stop();
                foreach (var conn in _activeConnections.Values)
                    conn.Close();
                ProcessExit?.Invoke();
            }
        });
    }

    public void Dispose()
    {
        _server.Stop();
        foreach (var conn in _activeConnections.Values)
        {
            conn.Close();
        }
        _activeConnections.Clear();
    }
}
