namespace PennMUSH.Server.Session;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Session;

public sealed class ConnectionDirectory : ISessionDirectory
{
    private readonly ConcurrentDictionary<string, MUSHConnection> _connections;
    private readonly Action<MUSHConnection> _drop;
    private readonly HashSet<int> _pending = new();

    public ConnectionDirectory(ConcurrentDictionary<string, MUSHConnection> connections, Action<MUSHConnection> drop)
    {
        _connections = connections;
        _drop = drop;
    }

    public int? SourceDescriptor { get; set; }
    public string Poll { get; set; } = "Doing";

    public IReadOnlyList<SessionInfo> List()
    {
        return _connections.Values
            .Where(conn => !_pending.Contains(conn.Descriptor))
            .OrderBy(conn => conn.Descriptor)
            .Select(conn => new SessionInfo
            {
                Descriptor = conn.Descriptor,
                PlayerId = conn.PlayerId,
                Hidden = conn.Hidden,
                Host = conn.Host,
                ConnectedAt = conn.ConnectedAt,
                LastActivity = conn.LastActivity,
                Commands = conn.Commands,
                BytesSent = conn.BytesSent,
                BytesReceived = conn.BytesReceived
            })
            .ToList();
    }

    public void SetHidden(int descriptor, bool hidden)
    {
        var conn = Find(descriptor);
        if (conn != null)
            conn.Hidden = hidden;
    }

    public void Disconnect(int descriptor)
    {
        if (Find(descriptor) != null)
            _pending.Add(descriptor);
    }

    public void FlushDisconnects()
    {
        foreach (int descriptor in _pending.ToArray())
        {
            _pending.Remove(descriptor);
            var conn = Find(descriptor);
            if (conn != null)
                _drop(conn);
        }
    }

    private MUSHConnection? Find(int descriptor)
    {
        return _connections.Values.FirstOrDefault(conn => conn.Descriptor == descriptor);
    }
}
