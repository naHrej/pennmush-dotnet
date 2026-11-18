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
                context.Notify(Feedback.UsageMailReadMessageNumber);
                return false;
            }

            var msg = _mail.GetMessageByNumber(executor.Id, num);
            if (msg == null)
            {
                context.Notify(Feedback.MessageNotFound, ("message", $"{num}"));
                return false;
            }

            _mail.MarkRead(executor.Id, num);
            var senderObj = db.GetObject(msg.Sender);
            string senderName = senderObj != null ? senderObj.Name : msg.Sender.ToString();

            context.Notify(Feedback.Dashes64);
            context.Notify($"Message: {num,-5} From: {senderName,-20} Date: {msg.Timestamp:ddd MMM d HH:mm}");
            context.Notify(Feedback.SubjectValue, ("value", $"{msg.Subject}"));
            context.Notify(Feedback.Dashes64);
            context.Notify(msg.Body);
            context.Notify(Feedback.Dashes64);
            return true;
        }

        // 2. @mail/clear <num>
        if (context.Switches.Contains("clear"))
        {
            if (!int.TryParse(context.Arguments.Trim(), out int num) || num <= 0)
            {
                context.Notify(Feedback.UsageMailClearMessageNumber);
                return false;
            }

            if (_mail.MarkCleared(executor.Id, num, cleared: true))
            {
                context.Notify(Feedback.MessageMarkedAsCleared, ("message", $"{num}"));
                return true;
            }
            context.Notify(Feedback.MessageNotFound, ("message", $"{num}"));
            return false;
        }

        // 3. @mail/unclear <num>
        if (context.Switches.Contains("unclear"))
        {
            if (!int.TryParse(context.Arguments.Trim(), out int num) || num <= 0)
            {
                context.Notify(Feedback.UsageMailUnclearMessageNumber);
                return false;
            }

            if (_mail.MarkCleared(executor.Id, num, cleared: false))
            {
                context.Notify(Feedback.MessageUnmarked, ("message", $"{num}"));
                return true;
            }
            context.Notify(Feedback.MessageNotFound, ("message", $"{num}"));
            return false;
        }

        // 4. @mail/purge
        if (context.Switches.Contains("purge"))
        {
            int purged = _mail.Purge(executor.Id);
            context.Notify(Feedback.ClearedMessageSPurged, ("value", $"{purged}"));
            return true;
        }

        // 5. @mail/folder [<num>]
        if (context.Switches.Contains("folder"))
        {
            if (string.IsNullOrWhiteSpace(context.Arguments))
            {
                int curr = _mail.GetCurrentFolder(executor.Id);
                context.Notify(Feedback.CurrentMailFolderIs, ("value", $"{curr}"));
                return true;
            }

            if (int.TryParse(context.Arguments.Trim(), out int f) && f >= 0 && f <= 15)
            {
                _mail.SetCurrentFolder(executor.Id, f);
                context.Notify(Feedback.SwitchedToMailFolder, ("value", $"{f}"));
                return true;
            }
            context.Notify(Feedback.FolderMustBeBetween0And15);
            return false;
        }

        // 6. @mail/status
        if (context.Switches.Contains("status"))
        {
            var stats = _mail.GetMailStats(executor.Id);
            context.Notify(Feedback.MailStatusMessageSUnreadCleared, ("value", $"{stats.TotalCount}"), ("value2", $"{stats.UnreadCount}"), ("value3", $"{stats.ClearedCount}"));
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
                context.Notify(Feedback.PlayerNotFound, ("player", $"{context.LeftArg}"));
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
            context.Notify(Feedback.MailSentToSubject, ("value", $"{targetObj.Name}"), ("value2", $"{sent.Subject}"));
            return true;
        }

        // 8. Default: list messages in current folder
        int activeFolder = _mail.GetCurrentFolder(executor.Id);
        var messages = _mail.GetMessages(executor.Id, activeFolder, includeCleared: true);

        if (messages.Count == 0)
        {
            context.Notify(Feedback.FolderIsEmpty, ("value", $"{activeFolder}"));
            return true;
        }

        context.Notify(Feedback.MailFolderMessages, ("value", $"{activeFolder}"), ("value2", $"{messages.Count}"));
        context.Notify(Feedback.NumStatusFromDateSubject);
        context.Notify(Feedback.Dashes64);
        for (int i = 0; i < messages.Count; i++)
        {
            var msg = messages[i];
            string status = (msg.IsCleared ? "C" : (msg.IsRead ? " " : "U")) + (msg.IsUrgent ? "!" : " ");
            var sObj = db.GetObject(msg.Sender);
            string sName = sObj != null ? sObj.Name : msg.Sender.ToString();
            string dateStr = msg.Timestamp.ToString("MMM dd HH:mm");
            context.Notify($"{i + 1,-3} [{status}]  {sName,-20} {dateStr,-16} {msg.Subject}");
        }
        context.Notify(Feedback.Dashes64);

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
            context.Notify(Feedback.MailSentToSubject, ("value", $"{who}"), ("value2", $"{sent.Subject}"));
        }
        if (!showMembers)
            context.Notify(Feedback.YouSentYourMessageToTheAlias, ("value", $"{alias.Name}"));
        return true;
    }
}
