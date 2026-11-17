namespace PennMUSH.Core.Communication.Channels;

using System;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Database;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Types;

public record ChannelMessage(DateTimeOffset Timestamp, string SenderName, Dbref SenderId, string Text);

public class Channel
{
    private readonly Dictionary<Dbref, ChannelMember> _members = new();
    private readonly Dictionary<string, (string Text, LockNode Key)> _locks = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ChannelMessage> _buffer = new();
    private readonly object _lock = new();

    public string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dbref Owner { get; set; }
    public ChannelFlags Flags { get; set; } = ChannelFlags.Public | ChannelFlags.Player;
    public int BufferCapacity { get; set; } = 50;
    public int MessageCount { get; private set; }

    public IReadOnlyCollection<ChannelMember> Members
    {
        get
        {
            lock (_lock) return _members.Values.ToList();
        }
    }

    public Channel(string name, Dbref owner, ChannelFlags flags = ChannelFlags.Public | ChannelFlags.Player)
    {
        Name = name;
        Owner = owner;
        Flags = flags;
    }

    public bool AddMember(Dbref player, string title = "")
    {
        lock (_lock)
        {
            if (_members.ContainsKey(player)) return false;
            _members[player] = new ChannelMember(player, title);
            return true;
        }
    }

    public bool RemoveMember(Dbref player)
    {
        lock (_lock)
        {
            return _members.Remove(player);
        }
    }

    public bool HasLock(string kind)
    {
        lock (_lock) return _locks.ContainsKey(kind);
    }

    public void SetLock(string kind, string text, LockNode key)
    {
        lock (_lock) _locks[kind] = (text, key);
    }

    public void ClearLock(string kind)
    {
        lock (_lock) _locks.Remove(kind);
    }

    public bool Passes(string kind, Dbref who, ObjectDatabase db)
    {
        lock (_lock)
        {
            if (!_locks.TryGetValue(kind, out var entry))
                return true;
            return entry.Key.Evaluate(who, Owner, db);
        }
    }

    public bool HasMember(Dbref player)
    {
        lock (_lock)
        {
            return _members.ContainsKey(player);
        }
    }

    public ChannelMember? GetMember(Dbref player)
    {
        lock (_lock)
        {
            return _members.TryGetValue(player, out var member) ? member : null;
        }
    }

    public void AddMessage(string senderName, Dbref senderId, string text)
    {
        lock (_lock)
        {
            var msg = new ChannelMessage(DateTimeOffset.UtcNow, senderName, senderId, text);
            _buffer.Add(msg);
            MessageCount++;
            if (_buffer.Count > BufferCapacity)
            {
                _buffer.RemoveAt(0);
            }
        }
    }

    public IReadOnlyList<ChannelMessage> GetRecentMessages(int count = 20)
    {
        lock (_lock)
        {
            return _buffer.TakeLast(count).ToList();
        }
    }
}
