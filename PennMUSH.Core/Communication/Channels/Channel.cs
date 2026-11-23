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

    private string _name;
    private string _description = string.Empty;
    private Dbref _owner;
    private Dbref _mogrifier = Dbref.Nothing;
    private ChannelFlags _flags = ChannelFlags.Public | ChannelFlags.Player;
    private int _bufferCapacity = 50;

    public Action? Changed { get; set; }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
                return;
            _name = value;
            Touch();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (_description == value)
                return;
            _description = value ?? string.Empty;
            Touch();
        }
    }

    public Dbref Owner
    {
        get => _owner;
        set
        {
            if (_owner == value)
                return;
            _owner = value;
            Touch();
        }
    }

    public Dbref Mogrifier
    {
        get => _mogrifier;
        set
        {
            if (_mogrifier == value)
                return;
            _mogrifier = value;
            Touch();
        }
    }

    public ChannelFlags Flags
    {
        get => _flags;
        set
        {
            if (_flags == value)
                return;
            _flags = value;
            Touch();
        }
    }

    public int BufferCapacity
    {
        get => _bufferCapacity;
        set
        {
            if (_bufferCapacity == value)
                return;
            _bufferCapacity = value;
            Touch();
        }
    }

    public int MessageCount { get; private set; }

    private void Touch() => Changed?.Invoke();

    public IReadOnlyCollection<ChannelMember> Members
    {
        get
        {
            lock (_lock) return _members.Values.ToList();
        }
    }

    public IReadOnlyDictionary<string, string> GetAllLockTexts()
    {
        lock (_lock) return _locks.ToDictionary(kv => kv.Key, kv => kv.Value.Text, StringComparer.OrdinalIgnoreCase);
    }

    public Channel(string name, Dbref owner, ChannelFlags flags = ChannelFlags.Public | ChannelFlags.Player)
    {
        _name = name;
        _owner = owner;
        _flags = flags;
    }

    public bool AddMember(Dbref player, string title = "")
    {
        lock (_lock)
        {
            if (_members.ContainsKey(player)) return false;
            var member = new ChannelMember(player, title) { Changed = Touch };
            _members[player] = member;
            Touch();
            return true;
        }
    }

    public bool RemoveMember(Dbref player)
    {
        lock (_lock)
        {
            bool removed = _members.Remove(player);
            if (removed)
                Touch();
            return removed;
        }
    }

    public bool HasLock(string kind)
    {
        lock (_lock) return _locks.ContainsKey(kind);
    }

    public string GetLockText(string kind)
    {
        lock (_lock) return _locks.TryGetValue(kind, out var entry) ? entry.Text : string.Empty;
    }

    public void SetLock(string kind, string text, LockNode key)
    {
        lock (_lock) _locks[kind] = (text, key);
        Touch();
    }

    public void ClearLock(string kind)
    {
        bool removed;
        lock (_lock) removed = _locks.Remove(kind);
        if (removed)
            Touch();
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
            if (_buffer.Count > _bufferCapacity)
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
