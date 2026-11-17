namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Linq;
using PennMUSH.Core.Communication.Mail;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode.Functions;
using PennMUSH.Core.Types;

public class MailCommandHandler : ICommandHandler
{
    private readonly MailStore _mail;
    private readonly MailAliasTable? _aliases;

    public string Name => "@mail";

    public MailCommandHandler(MailStore mail, MailAliasTable? aliases = null)
    {
        _mail = mail;
        _aliases = aliases;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var executor = db.GetObject(context.Executor);
        if (executor == null) return false;

        // 1. @mail/read <num>
        if (context.Switches.Contains("read"))
        {
            if (!int.TryParse(context.Arguments.Trim(), out int num) || num <= 0)
            {
                context.Notify("Usage: @mail/read <message-number>");
                return false;
            }

            var msg = _mail.GetMessageByNumber(executor.Id, num);
            if (msg == null)
            {
                context.Notify($"Message {num} not found.");
                return false;
            }

            _mail.MarkRead(executor.Id, num);
            var senderObj = db.GetObject(msg.Sender);
            string senderName = senderObj != null ? senderObj.Name : msg.Sender.ToString();

            context.Notify("----------------------------------------------------------------");
            context.Notify($"Message: {num,-5} From: {senderName,-20} Date: {msg.Timestamp:ddd MMM d HH:mm}");
            context.Notify($"Subject: {msg.Subject}");
            context.Notify("----------------------------------------------------------------");
            context.Notify(msg.Body);
            context.Notify("----------------------------------------------------------------");
            return true;
        }

        // 2. @mail/clear <num>
        if (context.Switches.Contains("clear"))
        {
            if (!int.TryParse(context.Arguments.Trim(), out int num) || num <= 0)
            {
                context.Notify("Usage: @mail/clear <message-number>");
                return false;
            }

            if (_mail.MarkCleared(executor.Id, num, cleared: true))
            {
                context.Notify($"Message {num} marked as cleared.");
                return true;
            }
            context.Notify($"Message {num} not found.");
            return false;
        }

        // 3. @mail/unclear <num>
        if (context.Switches.Contains("unclear"))
        {
            if (!int.TryParse(context.Arguments.Trim(), out int num) || num <= 0)
            {
                context.Notify("Usage: @mail/unclear <message-number>");
                return false;
            }

            if (_mail.MarkCleared(executor.Id, num, cleared: false))
            {
                context.Notify($"Message {num} unmarked.");
                return true;
            }
            context.Notify($"Message {num} not found.");
            return false;
        }

        // 4. @mail/purge
        if (context.Switches.Contains("purge"))
        {
            int purged = _mail.Purge(executor.Id);
            context.Notify($"{purged} cleared message(s) purged.");
            return true;
        }

        // 5. @mail/folder [<num>]
        if (context.Switches.Contains("folder"))
        {
            if (string.IsNullOrWhiteSpace(context.Arguments))
            {
                int curr = _mail.GetCurrentFolder(executor.Id);
                context.Notify($"Current mail folder is: {curr}");
                return true;
            }

            if (int.TryParse(context.Arguments.Trim(), out int f) && f >= 0 && f <= 15)
            {
                _mail.SetCurrentFolder(executor.Id, f);
                context.Notify($"Switched to mail folder {f}.");
                return true;
            }
            context.Notify("Folder must be between 0 and 15.");
            return false;
        }

        // 6. @mail/status
        if (context.Switches.Contains("status"))
        {
            var stats = _mail.GetMailStats(executor.Id);
            context.Notify($"Mail status: {stats.TotalCount} message(s), {stats.UnreadCount} unread, {stats.ClearedCount} cleared.");
            return true;
        }

        // 7. Sending mail: @mail <target>=<subject>/<body|message>
        if (!string.IsNullOrEmpty(context.LeftArg) && !string.IsNullOrEmpty(context.RightArg))
        {
            var targetRef = DbFunctions.ParseDbref(context.LeftArg, null);
            MUSHObject? targetObj = null;

            if (targetRef.IsValid)
            {
                targetObj = db.GetObject(targetRef);
            }
            else
            {
                // Find player by name
                for (int i = 0; i < db.Count; i++)
                {
                    var o = db.GetObject(new Dbref(i));
                    if (o != null && o.Type == ObjectType.Player && string.Equals(o.Name, context.LeftArg.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        targetObj = o;
                        break;
                    }
                }
            }

            if ((targetObj == null || targetObj.Type != ObjectType.Player) && TrySendAlias(context, db, executor))
                return true;

            if (targetObj == null || targetObj.Type != ObjectType.Player)
            {
                context.Notify($"Player '{context.LeftArg}' not found.");
                return false;
            }

            string subject = "(No Subject)";
            string body = context.RightArg;
            int slash = context.RightArg.IndexOf('/');
            if (slash > 0)
            {
                subject = context.RightArg.Substring(0, slash).Trim();
                body = context.RightArg.Substring(slash + 1).Trim();
            }

            bool urgent = context.Switches.Contains("urgent");
            var sent = _mail.SendMail(executor.Id, targetObj.Id, subject, body, urgent);
            context.Notify($"Mail sent to {targetObj.Name} (Subject: {sent.Subject}).");
            return true;
        }

        // 8. Default: list messages in current folder
        int activeFolder = _mail.GetCurrentFolder(executor.Id);
        var messages = _mail.GetMessages(executor.Id, activeFolder, includeCleared: true);

        if (messages.Count == 0)
        {
            context.Notify($"Folder {activeFolder} is empty.");
            return true;
        }

        context.Notify($"--- Mail folder {activeFolder} ({messages.Count} messages) ---");
        context.Notify("Num Status From                 Date             Subject");
        context.Notify("----------------------------------------------------------------");
        for (int i = 0; i < messages.Count; i++)
        {
            var msg = messages[i];
            string status = (msg.IsCleared ? "C" : (msg.IsRead ? " " : "U")) + (msg.IsUrgent ? "!" : " ");
            var sObj = db.GetObject(msg.Sender);
            string sName = sObj != null ? sObj.Name : msg.Sender.ToString();
            string dateStr = msg.Timestamp.ToString("MMM dd HH:mm");
            context.Notify($"{i + 1,-3} [{status}]  {sName,-20} {dateStr,-16} {msg.Subject}");
        }
        context.Notify("----------------------------------------------------------------");

        return true;
    }

    private bool TrySendAlias(CommandContext context, ObjectDatabase db, MUSHObject executor)
    {
        if (_aliases == null)
            return false;
        string name = context.LeftArg.Trim();
        if (name.Length == 0 || name[0] != MailAliasTable.Token)
            return false;
        var alias = _aliases.Find(executor, name);
        if (alias == null)
            return false;

        string subject = "(No Subject)";
        string body = context.RightArg;
        int slash = context.RightArg.IndexOf('/');
        if (slash > 0)
        {
            subject = context.RightArg[..slash].Trim();
            body = context.RightArg[(slash + 1)..].Trim();
        }
        bool urgent = context.Switches.Contains("urgent");
        bool showMembers = MailAliasTable.CanSeeMembers(executor, alias);
        foreach (var id in alias.Members)
        {
            var sent = _mail.SendMail(executor.Id, id, subject, body, urgent);
            if (!showMembers)
                continue;
            var recipient = db.GetObject(id);
            string who = recipient == null ? id.ToString() : recipient.Name;
            context.Notify($"Mail sent to {who} (Subject: {sent.Subject}).");
        }
        if (!showMembers)
            context.Notify($"You sent your message to the '{alias.Name}' alias");
        return true;
    }
}
