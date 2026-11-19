namespace PennMUSH.Core.Communication.Mail;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PennMUSH.Core.Types;

public record MailStats(int TotalCount, int UnreadCount, int ClearedCount);

public class MailStore
{
    private static int _nextMailId = 1;
    private readonly ConcurrentDictionary<Dbref, List<MailMessage>> _mailboxes = new();
    private readonly ConcurrentDictionary<Dbref, int> _activeFolders = new();
    private readonly object _lock = new();

    public MailMessage SendMail(Dbref sender, Dbref recipient, string subject, string body, bool urgent = false)
    {
        var msg = new MailMessage
        {
            Id = Interlocked.Increment(ref _nextMailId),
            Sender = sender,
            Recipient = recipient,
            Subject = string.IsNullOrWhiteSpace(subject) ? "(No Subject)" : subject.Trim(),
            Body = body,
            Timestamp = DateTimeOffset.UtcNow,
            Folder = 0,
            Flags = urgent ? MailFlags.Urgent : MailFlags.None
        };

        var box = _mailboxes.GetOrAdd(recipient, _ => new List<MailMessage>());
        lock (box)
        {
            box.Add(msg);
        }

        return msg;
    }

    public int GetCurrentFolder(Dbref player)
    {
        return _activeFolders.TryGetValue(player, out int folder) ? folder : 0;
    }

    public void SetCurrentFolder(Dbref player, int folder)
    {
        _activeFolders[player] = Math.Clamp(folder, 0, 15);
    }

    public IReadOnlyList<MailMessage> ExportAllMessages()
    {
        var all = new List<MailMessage>();
        foreach (var box in _mailboxes.Values)
        {
            lock (box) all.AddRange(box);
        }
        return all;
    }

    public IReadOnlyDictionary<Dbref, int> ExportActiveFolders()
        => new Dictionary<Dbref, int>(_activeFolders);

    public void ClearAndLoad(IEnumerable<MailMessage> messages, IEnumerable<KeyValuePair<Dbref, int>> activeFolders)
    {
        _mailboxes.Clear();
        _activeFolders.Clear();
        int maxId = 0;
        foreach (var msg in messages)
        {
            if (msg.Id > maxId) maxId = msg.Id;
            var box = _mailboxes.GetOrAdd(msg.Recipient, _ => new List<MailMessage>());
            lock (box) box.Add(msg);
        }
        if (maxId >= _nextMailId)
            _nextMailId = maxId + 1;
        foreach (var kv in activeFolders)
            _activeFolders[kv.Key] = Math.Clamp(kv.Value, 0, 15);
    }

    public IReadOnlyList<MailMessage> GetMessages(Dbref player, int? folder = null, bool includeCleared = false)
    {
        int targetFolder = folder ?? GetCurrentFolder(player);
        if (!_mailboxes.TryGetValue(player, out var box))
        {
            return Array.Empty<MailMessage>();
        }

        lock (box)
        {
            return box
                .Where(m => m.Folder == targetFolder && (includeCleared || !m.IsCleared))
                .OrderBy(m => m.Timestamp)
                .ToList();
        }
    }

    public MailMessage? GetMessageByNumber(Dbref player, int number, int? folder = null)
    {
        var list = GetMessages(player, folder, includeCleared: true);
        if (number < 1 || number > list.Count) return null;
        return list[number - 1];
    }

    public bool MarkRead(Dbref player, int number)
    {
        var msg = GetMessageByNumber(player, number);
        if (msg == null) return false;
        msg.Flags |= MailFlags.Read;
        return true;
    }

    public bool MarkCleared(Dbref player, int number, bool cleared = true)
    {
        var msg = GetMessageByNumber(player, number);
        if (msg == null) return false;
        if (cleared)
            msg.Flags |= MailFlags.Cleared;
        else
            msg.Flags &= ~MailFlags.Cleared;
        return true;
    }

    public int Purge(Dbref player)
    {
        if (!_mailboxes.TryGetValue(player, out var box)) return 0;

        lock (box)
        {
            int removed = box.RemoveAll(m => m.IsCleared);
            return removed;
        }
    }

    public MailStats GetMailStats(Dbref player)
    {
        if (!_mailboxes.TryGetValue(player, out var box))
            return new MailStats(0, 0, 0);

        lock (box)
        {
            int total = box.Count;
            int unread = box.Count(m => !m.IsRead);
            int cleared = box.Count(m => m.IsCleared);
            return new MailStats(total, unread, cleared);
        }
    }
}
