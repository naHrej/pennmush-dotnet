namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.MUSHcode;

public static class ChannelFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Register("CBUFFER", (ctx, args) => CBuffer(registry, ctx, args));
        registry.Register("CBUFFERADD", (ctx, args) => CBufferAdd(registry, ctx, args), sideEffect: true);
        registry.Register("CDESC", (ctx, args) => CDesc(registry, ctx, args));
        registry.Register("CEMIT", (ctx, args) => CEmit(registry, ctx, args), sideEffect: true);
        registry.Register("CFLAGS", (ctx, args) => CFlags(registry, ctx, args, longNames: false));
        registry.Register("CLFLAGS", (ctx, args) => CFlags(registry, ctx, args, longNames: true));
        registry.Register("CHANNELS", (ctx, args) => ChannelsList(registry, ctx, args));
        registry.Register("CLOCK", (ctx, args) => CLock(registry, ctx, args));
        registry.Register("CMOGRIFIER", (ctx, args) => CMogrifier(registry, ctx, args));
        registry.Register("CMSGS", (ctx, args) => CMsgs(registry, ctx, args));
        registry.Register("COWNER", (ctx, args) => COwner(registry, ctx, args));
        registry.Register("CRECALL", (ctx, args) => CRecall(registry, ctx, args));
        registry.Register("CSTATUS", (ctx, args) => CStatus(registry, ctx, args));
        registry.Register("CTITLE", (ctx, args) => CTitle(registry, ctx, args));
        registry.Register("CUSERS", (ctx, args) => CUsers(registry, ctx, args));
        registry.Register("CWHO", (ctx, args) => CWho(registry, ctx, args));
    }

    private static Channel? FindChannel(FunctionRegistry reg, string name)
        => reg.Channels?.MatchChannel(name, out _);

    public static string CBuffer(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Channels == null) return MushError.Nothing.Text();
        var ch = FindChannel(reg, args[0]);
        return ch != null ? ch.BufferCapacity.ToString() : MushError.NoMatch.Text();
    }

    public static string CRecall(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Channels == null) return MushError.Nothing.Text();
        var ch = FindChannel(reg, args[0]);
        if (ch == null) return MushError.NoMatch.Text();

        int count = 10;
        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
        {
            if (!int.TryParse(args[1].Trim(), out int parsed))
                return MushError.ArgumentMustBePositiveInteger.Text();
            if (parsed < 0) return MushError.ArgumentMustBePositiveInteger.Text();
            count = parsed == 0 ? int.MaxValue : parsed;
        }
        int start = 1;
        if (args.Length > 2 && !string.IsNullOrWhiteSpace(args[2]) && (!int.TryParse(args[2].Trim(), out start) || start < 1))
            return MushError.OutOfRange.Text();
        string delim = args.Length > 3 && !string.IsNullOrEmpty(args[3]) ? args[3] : " ";
        bool showStamp = args.Length > 4 && LogicFunctions.IsTruthy(args[4]);

        var allMsgs = ch.GetRecentMessages(ch.BufferCapacity);
        var slice = allMsgs.Skip(start - 1).Take(count);
        return string.Join(delim, slice.Select(m => showStamp ? $"[{m.Timestamp:yyyy-MM-dd HH:mm:ss}] {m.Text}" : m.Text));
    }

    public static string CEmit(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Channels == null) return MushError.TooFewArguments.Text();
        string sender = reg.Database?.GetObject(ctx.Executor)?.Name ?? ctx.Executor.ToString();
        var res = reg.Channels.Broadcast(args[0], ctx.Executor, sender, args[1], isEmit: true);
        if (res != null && reg.NotificationDispatcher != null)
        {
            foreach (var r in res.Recipients)
                reg.NotificationDispatcher.Invoke(new Notification(NotificationScope.Target, res.FormattedMessage, Target: r, Speaker: ctx.Executor, Spoofable: true));
        }
        return string.Empty;
    }

    public static string CBufferAdd(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Channels == null) return MushError.TooFewArguments.Text();
        var ch = FindChannel(reg, args[0]);
        if (ch == null) return MushError.NoMatch.Text();

        string senderName = reg.Database?.GetObject(ctx.Executor)?.Name ?? ctx.Executor.ToString();
        ch.AddMessage(senderName, ctx.Executor, args[1]);
        return string.Empty;
    }

    public static string CDesc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Channels == null) return MushError.Nothing.Text();
        var ch = FindChannel(reg, args[0]);
        return ch != null ? ch.Description : MushError.NoMatch.Text();
    }

    public static string CFlags(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool longNames)
    {
        if (args.Length == 0 || reg.Channels == null) return MushError.Nothing.Text();
        var ch = FindChannel(reg, args[0]);
        if (ch == null) return MushError.NoMatch.Text();

        if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]) && reg.Database != null)
        {
            var target = reg.Database.GetObject(DbFunctions.ParseDbref(args[1], ctx))
                ?? reg.Database.MatchObject(ctx.Executor, args[1]);
            if (target == null) return MushError.NoMatch.Text();
            var member = ch.GetMember(target.Id);
            if (member == null) return "#-1 NOT ON CHANNEL";
            if (!longNames)
            {
                var usb = new StringBuilder();
                if (member.IsMuted) usb.Append('G');
                if (member.IsHidden) usb.Append('H');
                return usb.ToString();
            }
            var ulist = new List<string>();
            if (member.IsMuted) ulist.Add("Gag");
            if (member.IsHidden) ulist.Add("Hide");
            return string.Join(" ", ulist);
        }

        if (!longNames)
        {
            var sb = new StringBuilder();
            if (ch.Flags.HasFlag(ChannelFlags.Admin)) sb.Append('A');
            if (ch.Flags.HasFlag(ChannelFlags.Disabled)) sb.Append('D');
            if (ch.Flags.HasFlag(ChannelFlags.Public)) sb.Append('O');
            if (ch.Flags.HasFlag(ChannelFlags.Player)) sb.Append('P');
            if (ch.Flags.HasFlag(ChannelFlags.Quiet)) sb.Append('Q');
            return sb.ToString();
        }

        var names = new List<string>();
        if (ch.Flags.HasFlag(ChannelFlags.Admin)) names.Add("Admin");
        if (ch.Flags.HasFlag(ChannelFlags.Disabled)) names.Add("Disabled");
        if (ch.Flags.HasFlag(ChannelFlags.Public)) names.Add("Open");
        if (ch.Flags.HasFlag(ChannelFlags.Player)) names.Add("Player");
        if (ch.Flags.HasFlag(ChannelFlags.Quiet)) names.Add("Quiet");
        return string.Join(" ", names);
    }

    public static string ChannelsList(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.Channels == null) return string.Empty;
        string sep = " ";
        if (args.Length >= 2)
        {
            if (args[1].Length > 1) return MushError.SeparatorMustBeOneCharacter.Text();
            if (args[1].Length == 1) sep = args[1];
        }

        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            return string.Join(sep, reg.Channels.GetAllChannels().Select(c => c.Name));

        var obj = reg.Database?.GetObject(DbFunctions.ParseDbref(args[0], ctx))
            ?? reg.Database?.MatchObject(ctx.Executor, args[0]);
        if (obj == null && args.Length == 1 && args[0].Length == 1)
            return string.Join(args[0], reg.Channels.GetAllChannels().Select(c => c.Name));
        if (obj == null) return MushError.NoMatch.Text();
        return string.Join(sep, reg.Channels.GetChannelsForPlayer(obj.Id).Select(c => c.Name));
    }

    public static string CLock(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Channels == null) return MushError.Nothing.Text();
        int slash = args[0].IndexOf('/');
        string chName = slash >= 0 ? args[0].Substring(0, slash).Trim() : args[0].Trim();
        string lockKind = slash >= 0 && slash < args[0].Length - 1 ? args[0].Substring(slash + 1).Trim().ToLowerInvariant() : "join";

        if (lockKind is not ("join" or "speak" or "mod" or "see" or "hide"))
            return "#-1 NO SUCH LOCK TYPE";

        var ch = FindChannel(reg, chName);
        if (ch == null) return MushError.NoMatch.Text();

        if (args.Length >= 2)
        {
            if (!reg.SideEffectsEnabled || ctx.NoSideFX)
                return MushError.PermissionDenied.Text();
            var actor = reg.Database?.GetObject(ctx.Executor);
            if (actor == null || (!actor.IsWizard && ch.Owner != actor.Id))
                return MushError.PermissionDenied.Text();
            var key = string.IsNullOrWhiteSpace(args[1]) ? null : PennMUSH.Core.Locks.LockParser.Parse(args[1]);
            ch.SetLock(lockKind, args[1], key!);
            return string.Empty;
        }

        return ch.GetLockText(lockKind);
    }

    public static string CMogrifier(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Channels == null) return MushError.Nothing.Text();
        var ch = FindChannel(reg, args[0]);
        if (ch == null) return MushError.NoMatch.Text();
        return ch.Mogrifier.IsValid ? ch.Mogrifier.ToString() : MushError.Nothing.Text();
    }

    public static string CMsgs(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Channels == null) return MushError.Nothing.Text();
        var ch = FindChannel(reg, args[0]);
        return ch != null ? ch.MessageCount.ToString() : MushError.NoMatch.Text();
    }

    public static string COwner(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Channels == null) return MushError.Nothing.Text();
        var ch = FindChannel(reg, args[0]);
        return ch != null ? ch.Owner.ToString() : MushError.NoMatch.Text();
    }

    public static string CStatus(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Channels == null) return MushError.Nothing.Text();
        var ch = FindChannel(reg, args[0]);
        var dbref = DbFunctions.ParseDbref(args[1], ctx);
        if (ch == null || !dbref.IsValid) return MushError.Nothing.Text();

        var member = ch.GetMember(dbref);
        if (member == null) return "Off";
        return member.IsMuted ? "Gag" : "On";
    }

    public static string CTitle(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Channels == null) return string.Empty;
        var ch = FindChannel(reg, args[0]);
        var dbref = DbFunctions.ParseDbref(args[1], ctx);
        if (ch == null || !dbref.IsValid) return MushError.Nothing.Text();

        var member = ch.GetMember(dbref);
        return member?.Title ?? string.Empty;
    }

    public static string CUsers(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]) || reg.Channels == null) return "#-1 NO CHANNEL GIVEN";
        var ch = FindChannel(reg, args[0]);
        return ch != null ? ch.Members.Count.ToString() : MushError.NoMatch.Text();
    }

    public static string CWho(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Channels == null) return string.Empty;
        var ch = FindChannel(reg, args[0]);
        if (ch == null) return MushError.NoMatch.Text();

        string filter = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]) ? args[1].Trim().ToLowerInvariant() : "on";
        if (filter is not ("on" or "off" or "all"))
            return "#-1 INVALID ARGUMENT";
        bool skipGagged = args.Length > 2 && LogicFunctions.IsTruthy(args[2]);

        var looker = reg.Database?.GetObject(ctx.Executor);
        bool privWho = PennMUSH.Core.Attributes.AttributePermissions.CanPrivWho(looker);
        var members = ch.Members.AsEnumerable();
        if (skipGagged)
            members = members.Where(m => !m.IsMuted);
        if (filter == "on" && !privWho)
            members = members.Where(m => !m.IsHidden);
        else if (filter == "off")
            members = members.Where(m => m.IsHidden);

        return string.Join(" ", members.Select(m => m.PlayerId.ToString()));
    }
}
