namespace PennMUSH.Core.Database;

using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using PennMUSH.Core.Types;

public sealed record ConnLogRecord(
    long Id,
    int Dbref,
    string Name,
    string IpAddress,
    string Hostname,
    long ConnectedAt,
    long DisconnectedAt,
    string Reason,
    bool Ssl,
    bool WebSocket);

/// <summary>
/// Persistent SQLite connection log database powering connlog(), connrecord(), addrlog(), and @sitelock/player.
/// </summary>
public sealed class ConnectionLogStore : IDisposable
{
    private SqliteConnection? _connection;

    public bool IsOpen => _connection != null;

    public void OpenFile(string path)
    {
        Close();
        var conn = new SqliteConnection($"Data Source={path}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS connlog (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    dbref INTEGER NOT NULL,
    name TEXT,
    ipaddr TEXT NOT NULL,
    hostname TEXT NOT NULL,
    conn INTEGER NOT NULL,
    disconn INTEGER NOT NULL,
    reason TEXT,
    ssl INTEGER NOT NULL DEFAULT 0,
    websocket INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX IF NOT EXISTS idx_connlog_dbref ON connlog(dbref);
CREATE INDEX IF NOT EXISTS idx_connlog_conn ON connlog(conn);";
        cmd.ExecuteNonQuery();
        _connection = conn;
    }

    public long RecordConnect(Dbref player, string? name, string ipaddr, string hostname, long connectedAtUnix, bool ssl = false, bool websocket = false)
    {
        if (_connection == null) return 0;
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
INSERT INTO connlog (dbref, name, ipaddr, hostname, conn, disconn, reason, ssl, websocket)
VALUES ($dbref, $name, $ip, $host, $conn, -1, '-', $ssl, $ws);
SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$dbref", player.Value);
        cmd.Parameters.AddWithValue("$name", (object?)name ?? "-");
        cmd.Parameters.AddWithValue("$ip", ipaddr);
        cmd.Parameters.AddWithValue("$host", hostname);
        cmd.Parameters.AddWithValue("$conn", connectedAtUnix);
        cmd.Parameters.AddWithValue("$ssl", ssl ? 1 : 0);
        cmd.Parameters.AddWithValue("$ws", websocket ? 1 : 0);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    public void UpdatePlayer(long recordId, Dbref player, string name)
    {
        if (_connection == null || recordId <= 0) return;
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE connlog SET dbref = $dbref, name = $name WHERE id = $id;";
        cmd.Parameters.AddWithValue("$dbref", player.Value);
        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$id", recordId);
        cmd.ExecuteNonQuery();
    }

    public void RecordDisconnect(long recordId, long disconnectedAtUnix, string? reason = null)
    {
        if (_connection == null || recordId <= 0) return;
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE connlog SET disconn = $disconn, reason = $reason WHERE id = $id;";
        cmd.Parameters.AddWithValue("$disconn", disconnectedAtUnix);
        cmd.Parameters.AddWithValue("$reason", reason ?? "quit");
        cmd.Parameters.AddWithValue("$id", recordId);
        cmd.ExecuteNonQuery();
    }

    public string? GetLastIpForPlayer(Dbref player)
    {
        if (_connection == null) return null;
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT ipaddr FROM connlog WHERE dbref = $dbref ORDER BY id DESC LIMIT 1;";
        cmd.Parameters.AddWithValue("$dbref", player.Value);
        return cmd.ExecuteScalar() as string;
    }

    public ConnLogRecord? GetRecord(long id)
    {
        if (_connection == null) return null;
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
SELECT id, dbref, ifnull(name, '-'), ipaddr, hostname, conn, disconn, ifnull(reason, '-'), ssl, websocket
FROM connlog WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new ConnLogRecord(
            r.GetInt64(0),
            r.GetInt32(1),
            r.GetString(2),
            r.GetString(3),
            r.GetString(4),
            r.GetInt64(5),
            r.GetInt64(6),
            r.GetString(7),
            r.GetInt32(8) != 0,
            r.GetInt32(9) != 0);
    }

    public IReadOnlyList<long> QueryConnLogIds(string mode, long? afterUnix = null, long? beforeUnix = null, string? spec = null)
    {
        var ids = new List<long>();
        if (_connection == null) return ids;

        using var cmd = _connection.CreateCommand();
        var clauses = new List<string>();
        if (afterUnix.HasValue)
        {
            clauses.Add("conn >= $after");
            cmd.Parameters.AddWithValue("$after", afterUnix.Value);
        }
        if (beforeUnix.HasValue)
        {
            clauses.Add("conn <= $before");
            cmd.Parameters.AddWithValue("$before", beforeUnix.Value);
        }

        switch (mode.ToLowerInvariant())
        {
            case "logged-in" or "online":
                clauses.Add("disconn = -1");
                break;
            case "player":
                if (Dbref.TryParse(spec ?? string.Empty, out var d) && d.IsValid)
                {
                    clauses.Add("dbref = $dbref");
                    cmd.Parameters.AddWithValue("$dbref", d.Value);
                }
                else
                {
                    clauses.Add("lower(name) = lower($pname)");
                    cmd.Parameters.AddWithValue("$pname", spec ?? string.Empty);
                }
                break;
            case "ip":
                clauses.Add("ipaddr LIKE $pat ESCAPE '\\'");
                cmd.Parameters.AddWithValue("$pat", WildcardToLike(spec ?? "*"));
                break;
            case "hostname":
                clauses.Add("lower(hostname) LIKE lower($pat) ESCAPE '\\'");
                cmd.Parameters.AddWithValue("$pat", WildcardToLike(spec ?? "*"));
                break;
        }

        string where = clauses.Count > 0 ? " WHERE " + string.Join(" AND ", clauses) : string.Empty;
        cmd.CommandText = $"SELECT id FROM connlog{where} ORDER BY id ASC;";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            ids.Add(r.GetInt64(0));
        return ids;
    }

    public IReadOnlyList<(string Ip, string Host)> QueryAddrLog(string mode, string pattern)
    {
        var results = new List<(string, string)>();
        if (_connection == null) return results;

        using var cmd = _connection.CreateCommand();
        string col = mode.Equals("hostname", StringComparison.OrdinalIgnoreCase) ? "lower(hostname)" : "ipaddr";
        string val = mode.Equals("hostname", StringComparison.OrdinalIgnoreCase) ? WildcardToLike(pattern).ToLowerInvariant() : WildcardToLike(pattern);
        cmd.CommandText = $"SELECT DISTINCT ipaddr, hostname FROM connlog WHERE {col} LIKE $pat ESCAPE '\\' ORDER BY id ASC;";
        cmd.Parameters.AddWithValue("$pat", val);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            results.Add((r.GetString(0), r.GetString(1)));
        return results;
    }

    private static string WildcardToLike(string wild)
    {
        return wild
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_")
            .Replace("*", "%")
            .Replace("?", "_");
    }

    public void Close()
    {
        if (_connection != null)
        {
            SqliteConnection.ClearPool(_connection);
            _connection.Dispose();
            _connection = null;
        }
    }

    public void Dispose() => Close();
}
