namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PennMUSH.Core.Communication.Mail;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public static class MailFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Register("MAIL", (ctx, args) => MailFunc(registry, ctx, args));
        registry.Register("MAILLIST", (ctx, args) => MailList(registry, ctx, args));
        registry.Register("MAILFROM", (ctx, args) => MailField(registry, ctx, args, m => m.Sender.ToString()));
        registry.Register("MAILSUBJECT", (ctx, args) => MailField(registry, ctx, args, m => m.Subject));
        registry.Register("MAILTIME", (ctx, args) => MailField(registry, ctx, args, m => m.Timestamp.ToLocalTime().ToString("ddd MMM d HH:mm:ss yyyy", CultureInfo.InvariantCulture)));
        registry.Register("MAILSTATUS", (ctx, args) => MailField(registry, ctx, args, FormatStatus));
        registry.Register("MAILSEND", (ctx, args) => MailSend(registry, ctx, args));
        registry.Register("MAILSTATS", (ctx, args) => MailStatsFunc(registry, ctx, args, detailed: false));
        registry.Register("MAILDSTATS", (ctx, args) => MailStatsFunc(registry, ctx, args, detailed: true));
        registry.Register("MAILFSTATS", (ctx, args) => MailFullStatsFunc(registry, ctx, args));
        registry.Register("FOLDERSTATS", (ctx, args) => MailFStats(registry, ctx, args));
        registry.Register("MALIAS", (ctx, args) => MAlias(registry, ctx, args));
    }

    private static (Dbref Target, int? Folder, int? Number) ParseMailRef(FunctionRegistry reg, EvaluationContext ctx, string[] args, int refIdx)
    {
        Dbref target = ctx.Executor;
        int? folder = null;
        int? num = null;

        if (args.Length > refIdx && !string.IsNullOrWhiteSpace(args[refIdx]))
        {
            string raw = args[refIdx].Trim();
            int colon = raw.IndexOf(':');
            if (colon >= 0)
            {
                if (int.TryParse(raw.Substring(0, colon), out int f)) folder = f;
                if (int.TryParse(raw.Substring(colon + 1), out int n)) num = n;
            }
            else if (int.TryParse(raw, out int n))
            {
                num = n;
            }
            else if (reg.Database != null)
            {
                var p = reg.Database.GetObject(DbFunctions.ParseDbref(raw, ctx)) ?? reg.Database.FindPlayer(raw);
                if (p != null) target = p.Id;
            }
        }

        return (target, folder, num);
    }

    public static string MailFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.Mail == null) return "0 0 0";
        if (args.Length == 0 || (args.Length == 1 && !int.TryParse(args[0].Trim(), out _) && !args[0].Contains(':') && reg.Database != null && (reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx)) != null || reg.Database.FindPlayer(args[0].Trim()) != null)))
        {
            Dbref statsTarget = args.Length > 0
                ? (reg.Database?.GetObject(DbFunctions.ParseDbref(args[0], ctx))?.Id ?? reg.Database?.FindPlayer(args[0].Trim())?.Id ?? ctx.Executor)
                : ctx.Executor;
            var stats = reg.Mail.GetMailStats(statsTarget);
            return $"{stats.TotalCount} {stats.UnreadCount} {stats.ClearedCount}";
        }

        var (target, folder, num) = ParseMailRef(reg, ctx, args, 0);
        if (args.Length >= 2 && int.TryParse(args[1].Trim(), out int msgNum))
        {
            target = reg.Database?.GetObject(DbFunctions.ParseDbref(args[0], ctx))?.Id ?? reg.Database?.FindPlayer(args[0].Trim())?.Id ?? ctx.Executor;
            num = msgNum;
        }
        if (!num.HasValue) return string.Empty;

        var msg = reg.Mail.GetMessageByNumber(target, num.Value, folder);
        return msg?.Body ?? MushError.OutOfRange.Text();
    }

    public static string MailList(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.Mail == null) return string.Empty;
        var (target, folder, _) = ParseMailRef(reg, ctx, args, 0);
        int f = folder ?? reg.Mail.GetCurrentFolder(target);
        var msgs = reg.Mail.GetMessages(target, f);
        return string.Join(" ", Enumerable.Range(1, msgs.Count).Select(i => $"{f}:{i}"));
    }

    private static string MailField(FunctionRegistry reg, EvaluationContext ctx, string[] args, Func<MailMessage, string> selector)
    {
        if (reg.Mail == null || args.Length == 0) return MushError.TooFewArguments.Text();
        var (target, folder, num) = ParseMailRef(reg, ctx, args, 0);
        if (args.Length >= 2 && int.TryParse(args[1].Trim(), out int msgNum))
        {
            target = reg.Database?.GetObject(DbFunctions.ParseDbref(args[0], ctx))?.Id ?? reg.Database?.FindPlayer(args[0].Trim())?.Id ?? ctx.Executor;
            num = msgNum;
        }
        if (!num.HasValue) return MushError.ArgumentMustBeInteger.Text();

        var msg = reg.Mail.GetMessageByNumber(target, num.Value, folder);
        return msg != null ? selector(msg) : MushError.OutOfRange.Text();
    }

    private static string FormatStatus(MailMessage m)
    {
        var sb = new StringBuilder();
        sb.Append(m.IsRead ? '-' : 'N');
        sb.Append(m.IsCleared ? 'C' : '-');
        sb.Append(m.IsUrgent ? 'U' : '-');
        sb.Append(m.IsTagged ? 'T' : '-');
        return sb.ToString();
    }

    public static string MailSend(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Mail == null || reg.Database == null)
            return MushError.TooFewArguments.Text();

        string recipsRaw = args[0];
        string body = args[1];
        string subject = "(No Subject)";
        int slash = body.IndexOf('/');
        if (slash > 0 && slash < body.Length - 1)
        {
            subject = body.Substring(0, slash);
            body = body.Substring(slash + 1);
        }

        int sent = 0;
        foreach (string tok in recipsRaw.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var p = reg.Database.GetObject(DbFunctions.ParseDbref(tok, ctx)) ?? reg.Database.FindPlayer(tok.TrimStart('*'));
            if (p != null && p.Type == ObjectType.Player)
            {
                reg.Mail.SendMail(ctx.Executor, p.Id, subject, body);
                sent++;
            }
        }
        return sent > 0 ? string.Empty : MushError.NoMatch.Text();
    }

    public static string MailStatsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool detailed)
    {
        if (reg.Mail == null) return "0";
        Dbref target = ctx.Executor;
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) && reg.Database != null)
        {
            var p = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx)) ?? reg.Database.FindPlayer(args[0].Trim());
            if (p == null) return MushError.NoMatch.Text();
            target = p.Id;
        }

        var stats = reg.Mail.GetMailStats(target);
        if (!detailed)
            return stats.TotalCount.ToString();
        return $"{stats.TotalCount} {stats.UnreadCount} {stats.ClearedCount}";
    }

    public static string MailFStats(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.Mail == null) return "0 0 0";
        int folder = args.Length > 0 && int.TryParse(args[0].Trim(), out int f)
            ? f
            : reg.Mail.GetCurrentFolder(ctx.Executor);

        var msgs = reg.Mail.GetMessages(ctx.Executor, folder, includeCleared: true);
        int total = msgs.Count;
        int unread = msgs.Count(m => !m.IsRead);
        int cleared = msgs.Count(m => m.IsCleared);
        return $"{total} {unread} {cleared}";
    }

    public static string MAlias(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.MailAliases == null || reg.Database == null) return string.Empty;
        var viewer = reg.Database.GetObject(ctx.Executor);
        if (viewer == null) return string.Empty;

        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            return string.Join(" ", reg.MailAliases.Visible(viewer).Select(a => "+" + a.Name));

        var alias = reg.MailAliases.Find(viewer, args[0].Trim());
        if (alias == null) return MushError.NoMatch.Text();
        return string.Join(" ", alias.Members.Select(m => m.ToString()));
    }

    public static string MailFullStatsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.Mail == null) return "0 0 0 0 0 0 0 0";
        Dbref target = ctx.Executor;
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) && reg.Database != null)
        {
            var p = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx)) ?? reg.Database.FindPlayer(args[0].Trim().TrimStart('*'));
            if (p == null) return MushError.NoMatch.Text();
            target = p.Id;
        }

        int recTotal = 0, recUnread = 0, recCleared = 0, recBytes = 0;
        for (int f = 0; f <= 15; f++)
        {
            foreach (var m in reg.Mail.GetMessages(target, f, includeCleared: true))
            {
                recTotal++;
                if (!m.IsRead) recUnread++;
                if (m.IsCleared) recCleared++;
                recBytes += (m.Subject?.Length ?? 0) + (m.Body?.Length ?? 0);
            }
        }

        int sentTotal = 0, sentUnread = 0, sentCleared = 0, sentBytes = 0;
        if (reg.Database != null)
        {
            foreach (var obj in reg.Database.GetAllObjects().Where(o => o.Type == ObjectType.Player))
            {
                for (int f = 0; f <= 15; f++)
                {
                    foreach (var m in reg.Mail.GetMessages(obj.Id, f, includeCleared: true).Where(m => m.Sender == target))
                    {
                        sentTotal++;
                        if (!m.IsRead) sentUnread++;
                        if (m.IsCleared) sentCleared++;
                        sentBytes += (m.Subject?.Length ?? 0) + (m.Body?.Length ?? 0);
                    }
                }
            }
        }

        return $"{sentTotal} {sentUnread} {sentCleared} {sentBytes} {recTotal} {recUnread} {recCleared} {recBytes}";
    }
}
