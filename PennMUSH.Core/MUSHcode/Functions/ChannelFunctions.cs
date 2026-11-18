namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.MUSHcode;

public static class ChannelFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Register("CBUFFER", (ctx, args) => CBuffer(registry, ctx, args));
        registry.Register("CBUFFERADD", (ctx, args) => CBufferAdd(registry, ctx, args));
        registry.Register("CDESC", (ctx, args) => CDesc(registry, ctx, args));
        registry.Register("CFLAGS", (ctx, args) => CFlags(registry, ctx, args, longNames: false));
        registry.Register("CLFLAGS", (ctx, args) => CFlags(registry, ctx, args, longNames: true));
        registry.Register("CHANNELS", (ctx, args) => ChannelsList(registry, ctx, args));
        registry.Register("CLOCK", (ctx, args) => CLock(registry, ctx, args));
        registry.Register("CMOGRIFIER", (ctx, args) => CMogrifier(registry, ctx, args));
        registry.Register("CMSGS", (ctx, args) => CMsgs(registry, ctx, args));
        registry.Register("COWNER", (ctx, args) => COwner(registry, ctx, args));
        registry.Register("CRECALL", (ctx, args) => CBuffer(registry, ctx, args));
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
        if (ch == null) return MushError.NoMatch.Text();

        int count = args.Length > 1 && int.TryParse(args[1].Trim(), out int c) && c > 0 ? c : 20;
        var msgs = ch.GetRecentMessages(count);
        return string.Join("\n", msgs.Select(m => m.Text));
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

        var dbref = DbFunctions.ParseDbref(args[0], ctx);
        if (!dbref.IsValid) return MushError.NoMatch.Text();
        return string.Join(sep, reg.Channels.GetChannelsForPlayer(dbref).Select(c => c.Name));
    }

    public static string CLock(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Channels == null) return MushError.Nothing.Text();
        int slash = args[0].IndexOf('/');
        string chName = slash >= 0 ? args[0].Substring(0, slash).Trim() : args[0].Trim();
        string lockKind = slash >= 0 && slash < args[0].Length - 1 ? args[0].Substring(slash + 1).Trim() : "join";

        var ch = FindChannel(reg, chName);
        if (ch == null) return MushError.NoMatch.Text();
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
        return member.IsMuted ? "Gagged" : "On";
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
        if (args.Length == 0 || reg.Channels == null) return "0";
        var ch = FindChannel(reg, args[0]);
        return ch != null ? ch.Members.Count.ToString() : MushError.NoMatch.Text();
    }

    public static string CWho(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Channels == null) return string.Empty;
        var ch = FindChannel(reg, args[0]);
        if (ch == null) return MushError.NoMatch.Text();

        string filter = args.Length > 1 ? args[1].Trim().ToLowerInvariant() : "on";
        var members = ch.Members.AsEnumerable();
        if (filter == "on")
            members = members.Where(m => !m.IsHidden);

        return string.Join(" ", members.Select(m => m.PlayerId.ToString()));
    }
}
