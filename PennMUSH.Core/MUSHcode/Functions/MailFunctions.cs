namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PennMUSH.Core.Commands;
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
        registry.Register("MAILSEND", (ctx, args) => MailSend(registry, ctx, args), sideEffect: true);
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

    private static bool CanAccessMail(FunctionRegistry reg, EvaluationContext ctx, Dbref target)
    {
        if (target == ctx.Executor || reg.Database == null) return true;
        var actor = reg.Database.GetObject(ctx.Executor);
        var targetObj = reg.Database.GetObject(target);
        return PennMUSH.Core.Attributes.AttributePermissions.Controls(actor, targetObj);
    }

    public static string MailFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.Mail == null) return "0";
        if (args.Length == 0)
            return reg.Mail.GetMailStats(ctx.Executor).TotalCount.ToString();

        if (args.Length == 1 && !int.TryParse(args[0].Trim(), out _) && !args[0].Contains(':') && reg.Database != null &&
            (reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx)) != null || reg.Database.FindPlayer(args[0].Trim()) != null))
        {
            Dbref statsTarget = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx))?.Id
                ?? reg.Database.FindPlayer(args[0].Trim())?.Id ?? ctx.Executor;
            if (!CanAccessMail(reg, ctx, statsTarget))
                return MushError.PermissionDenied.Text();
            var stats = reg.Mail.GetMailStats(statsTarget);
            return $"{stats.TotalCount} {stats.UnreadCount} {stats.ClearedCount}";
        }

        var (target, folder, num) = ParseMailRef(reg, ctx, args, 0);
        if (args.Length >= 2 && int.TryParse(args[1].Trim(), out int msgNum))
        {
            target = reg.Database?.GetObject(DbFunctions.ParseDbref(args[0], ctx))?.Id ?? reg.Database?.FindPlayer(args[0].Trim())?.Id ?? ctx.Executor;
            num = msgNum;
        }
        if (!CanAccessMail(reg, ctx, target))
            return MushError.PermissionDenied.Text();
        if (!num.HasValue) return string.Empty;

        var msg = reg.Mail.GetMessageByNumber(target, num.Value, folder);
        return msg?.Body ?? MushError.OutOfRange.Text();
    }

    public static string MailList(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.Mail == null) return string.Empty;
        Dbref target = ctx.Executor;
        string spec = args.Length > 0 ? args[0].Trim() : string.Empty;
        if (args.Length >= 2 && reg.Database != null)
        {
            var p = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx)) ?? reg.Database.FindPlayer(args[0].Trim().TrimStart('*'));
            if (p == null) return MushError.NoMatch.Text();
            target = p.Id;
            spec = args[1].Trim();
        }
        else if (args.Length == 1 && reg.Database != null)
        {
            var p = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx)) ?? reg.Database.FindPlayer(args[0].Trim().TrimStart('*'));
            if (p != null)
            {
                target = p.Id;
                spec = string.Empty;
            }
        }
        if (!CanAccessMail(reg, ctx, target))
            return MushError.PermissionDenied.Text();

        int folder = reg.Mail.GetCurrentFolder(target);
        int colon = spec.IndexOf(':');
        if (colon >= 0)
        {
            if (int.TryParse(spec.Substring(0, colon), out int pf)) folder = pf;
            spec = spec.Substring(colon + 1).Trim();
        }

        var msgs = reg.Mail.GetMessages(target, folder, includeCleared: true);
        var indices = new List<string>();
        for (int i = 0; i < msgs.Count; i++)
        {
            var m = msgs[i];
            int num = i + 1;
            bool match = spec.ToLowerInvariant() switch
            {
                "" or "all" => !m.IsCleared,
                "unread" => !m.IsRead && !m.IsCleared,
                "read" => m.IsRead && !m.IsCleared,
                "cleared" => m.IsCleared,
                "tagged" => m.IsTagged && !m.IsCleared,
                "urgent" => m.IsUrgent && !m.IsCleared,
                _ => MatchNumberRange(spec, num) && !m.IsCleared
            };
            if (match) indices.Add($"{folder}:{num}");
        }
        return string.Join(" ", indices);
    }

    private static bool MatchNumberRange(string spec, int num)
    {
        if (int.TryParse(spec, out int exact)) return num == exact;
        int dash = spec.IndexOf('-');
        if (dash > 0 && dash < spec.Length - 1 &&
            int.TryParse(spec.Substring(0, dash), out int lo) &&
            int.TryParse(spec.Substring(dash + 1), out int hi))
            return num >= lo && num <= hi;
        return false;
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
        string subject = Feedback.MailNoSubject.Text();
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
        if (reg.Mail == null) return detailed ? "0 0 0 0 0 0" : "0 0";
        Dbref target = ctx.Executor;
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) && reg.Database != null)
        {
            var p = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx)) ?? reg.Database.FindPlayer(args[0].Trim().TrimStart('*'));
            if (p == null) return MushError.NoMatch.Text();
            target = p.Id;
        }
        if (!CanAccessMail(reg, ctx, target))
            return MushError.PermissionDenied.Text();

        var rec = reg.Mail.GetMailStats(target);
        int sentTotal = 0, sentUnread = 0, sentCleared = 0;
        if (reg.Database != null)
        {
            foreach (var obj in reg.Database.ByType(ObjectType.Player))
            {
                for (int f = 0; f <= 15; f++)
                {
                    foreach (var m in reg.Mail.GetMessages(obj.Id, f, includeCleared: true).Where(m => m.Sender == target))
                    {
                        sentTotal++;
                        if (!m.IsRead) sentUnread++;
                        if (m.IsCleared) sentCleared++;
                    }
                }
            }
        }

        if (!detailed)
            return $"{sentTotal} {rec.TotalCount}";
        return $"{sentTotal} {sentUnread} {sentCleared} {rec.TotalCount} {rec.UnreadCount} {rec.ClearedCount}";
    }

    public static string MailFStats(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.Mail == null) return "0 0 0";
        Dbref target = ctx.Executor;
        int folder = reg.Mail.GetCurrentFolder(target);

        if (args.Length == 1 && !string.IsNullOrWhiteSpace(args[0]))
        {
            if (int.TryParse(args[0].Trim(), out int f))
                folder = f;
            else if (reg.Database != null)
            {
                var p = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx)) ?? reg.Database.FindPlayer(args[0].Trim().TrimStart('*'));
                if (p == null) return MushError.NoMatch.Text();
                target = p.Id;
                folder = reg.Mail.GetCurrentFolder(target);
            }
        }
        else if (args.Length >= 2)
        {
            if (reg.Database != null)
            {
                var p = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx)) ?? reg.Database.FindPlayer(args[0].Trim().TrimStart('*'));
                if (p == null) return MushError.NoMatch.Text();
                target = p.Id;
            }
            if (!int.TryParse(args[1].Trim(), out folder))
                return MushError.FolderMustBeInteger.Text();
        }
        if (!CanAccessMail(reg, ctx, target))
            return MushError.PermissionDenied.Text();

        var msgs = reg.Mail.GetMessages(target, folder, includeCleared: true);
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

        string sep = args.Length > 1 && !string.IsNullOrEmpty(args[1]) ? args[1] : " ";
        var alias = reg.MailAliases.Find(viewer, args[0].Trim());
        if (alias == null)
        {
            if (args.Length == 1 && args[0].Length == 1)
                return string.Join(args[0], reg.MailAliases.Visible(viewer).Select(a => "+" + a.Name));
            return MushError.NoMatch.Text();
        }
        return string.Join(sep, alias.Members.Select(m => m.ToString()));
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
            foreach (var obj in reg.Database.ByType(ObjectType.Player))
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
