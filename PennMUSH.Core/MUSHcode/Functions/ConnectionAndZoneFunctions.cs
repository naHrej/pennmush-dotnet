namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Commands;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Presentation;
using PennMUSH.Core.Types;

public sealed record ConnectionSnapshot(
    Dbref PlayerId,
    int Descriptor,
    string Host,
    string IpAddress,
    string TerminalType,
    DateTimeOffset ConnectedAt,
    DateTimeOffset LastActivityAt,
    bool IsHidden = false,
    long BytesSent = 0,
    long BytesReceived = 0,
    int CommandCount = 0,
    bool SupportsTelnet = false,
    bool SupportsGmcp = false,
    bool IsSsl = false,
    bool IsWebSocket = false,
    bool SupportsHtml = false,
    bool SupportsPueblo = false,
    bool StripAccents = false,
    bool PromptNewlines = false,
    bool SupportsXterm256 = false,
    bool SupportsRgb24 = false,
    int Width = 80,
    int Height = 24,
    string? ColorStyle = null)
{
    public ClientCapabilities ToClientCapabilities()
    {
        ColorDepth depth = ColorStyle?.ToLowerInvariant() switch
        {
            "plain" => ColorDepth.None,
            "16color" or "hilite" => ColorDepth.Standard16,
            "xterm256" => ColorDepth.Color256,
            "rgb24" or "truecolor" => ColorDepth.TrueColor,
            _ => SupportsRgb24
                ? ColorDepth.TrueColor
                : SupportsXterm256
                    ? ColorDepth.Color256
                    : ColorDepth.Standard16
        };

        return new ClientCapabilities
        {
            SupportsHtml = SupportsHtml,
            SupportsPueblo = SupportsPueblo,
            SupportsGmcp = SupportsGmcp,
            IsSsl = IsSsl,
            IsWebSocket = IsWebSocket,
            StripAccents = StripAccents,
            PromptNewlines = PromptNewlines,
            ColorMode = depth,
            TerminalWidth = Width,
            TerminalHeight = Height,
            TerminalType = TerminalType
        };
    }
}

public static class ConnectionAndZoneFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        // Section 10: Zone broadcast & nospoof
        registry.Register("ZEMIT", (ctx, args) => ZEmit(registry, ctx, args, spoofable: true), sideEffect: true);
        registry.Register("NSZEMIT", (ctx, args) => ZEmit(registry, ctx, args, spoofable: false), sideEffect: true);
        registry.Register("NSCEMIT", (ctx, args) => NsCemit(registry, ctx, args), sideEffect: true);
        registry.Register("NSEMIT", (ctx, args) => Section9And18Functions.Emit(registry, ctx, args), sideEffect: true);
        registry.Register("NSLEMIT", (ctx, args) => Section9And18Functions.Lemit(registry, ctx, args), sideEffect: true);
        registry.Register("NSOEMIT", (ctx, args) => Section9And18Functions.Oemit(registry, ctx, args), sideEffect: true);
        registry.Register("NSPEMIT", (ctx, args) => Section9And18Functions.Pemit(registry, ctx, args, prompt: false), sideEffect: true);
        registry.Register("NSPROMPT", (ctx, args) => Section9And18Functions.Pemit(registry, ctx, args, prompt: true), sideEffect: true);
        registry.Register("NSREMIT", (ctx, args) => Section9And18Functions.Remit(registry, ctx, args), sideEffect: true);

        // Section 12: Connected descriptors / WHO
        registry.Register("CONN", (ctx, args) => ConnSecs(registry, ctx, args));
        registry.Register("IDLE", (ctx, args) => IdleSecs(registry, ctx, args));
        registry.Register("IDLESECS", (ctx, args) => IdleSecs(registry, ctx, args));
        registry.Register("HOST", (ctx, args) => HostInfo(registry, ctx, args, ipOnly: false));
        registry.Register("HOSTNAME", (ctx, args) => HostInfo(registry, ctx, args, ipOnly: false));
        registry.Register("IPADDR", (ctx, args) => HostInfo(registry, ctx, args, ipOnly: true));
        registry.Register("TERMINFO", (ctx, args) => TermInfo(registry, ctx, args));
        registry.Register("PORTS", (ctx, args) => PortsList(registry, ctx, args));
        registry.Register("LPORTS", (ctx, args) => LPortsFunc(registry, ctx, args));
        registry.Register("SENT", (ctx, args) => SentRecvFunc(registry, ctx, args, sent: true));
        registry.Register("RECV", (ctx, args) => SentRecvFunc(registry, ctx, args, sent: false));

        registry.Register("LWHO", (ctx, args) => WhoList(registry, ctx, args, useObjId: false, countOnly: false, slice: false, mode: WhoMode.All));
        registry.Register("LWHOID", (ctx, args) => WhoList(registry, ctx, args, useObjId: true, countOnly: false, slice: false, mode: WhoMode.All));
        registry.Register("NWHO", (ctx, args) => WhoList(registry, ctx, args, useObjId: false, countOnly: true, slice: false, mode: WhoMode.All));
        registry.Register("XWHO", (ctx, args) => WhoList(registry, ctx, args, useObjId: false, countOnly: false, slice: true, mode: WhoMode.All));
        registry.Register("XWHOID", (ctx, args) => WhoList(registry, ctx, args, useObjId: true, countOnly: false, slice: true, mode: WhoMode.All));

        registry.Register("MWHO", (ctx, args) => WhoList(registry, ctx, args, useObjId: false, countOnly: false, slice: false, mode: WhoMode.Mortal));
        registry.Register("MWHOID", (ctx, args) => WhoList(registry, ctx, args, useObjId: true, countOnly: false, slice: false, mode: WhoMode.Mortal));
        registry.Register("NMWHO", (ctx, args) => WhoList(registry, ctx, args, useObjId: false, countOnly: true, slice: false, mode: WhoMode.Mortal));
        registry.Register("XMWHO", (ctx, args) => WhoList(registry, ctx, args, useObjId: false, countOnly: false, slice: true, mode: WhoMode.Mortal));
        registry.Register("XMWHOID", (ctx, args) => WhoList(registry, ctx, args, useObjId: true, countOnly: false, slice: true, mode: WhoMode.Mortal));

        registry.Register("ZWHO", (ctx, args) => WhoList(registry, ctx, args, useObjId: false, countOnly: false, slice: false, mode: WhoMode.Zone));
        registry.Register("ZMWHO", (ctx, args) => WhoList(registry, ctx, args, useObjId: false, countOnly: false, slice: false, mode: WhoMode.ZoneMortal));
    }

    private enum WhoMode { All, Mortal, Zone, ZoneMortal }

    public static string ZEmit(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool spoofable)
    {
        if (args.Length < 2 || reg.Database == null || reg.NotificationDispatcher == null)
            return string.Empty;

        var zone = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx))
            ?? reg.Database.MatchObject(ctx.Executor, args[0]);
        if (zone == null) return MushError.NoMatch.Text();

        foreach (var room in reg.Database.GetAllObjects().Where(o => o.Type == ObjectType.Room && o.InZone(zone.Id)))
        {
            reg.NotificationDispatcher.Invoke(new Notification(
                NotificationScope.Room,
                args[1],
                Target: room.Id,
                Speaker: ctx.Executor,
                Spoofable: spoofable));
        }
        return string.Empty;
    }

    public static string NsCemit(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Channels == null) return string.Empty;
        string sender = reg.Database?.GetObject(ctx.Executor)?.Name ?? ctx.Executor.ToString();
        var res = reg.Channels.Broadcast(args[0], ctx.Executor, sender, args[1], isEmit: true);
        if (res != null && reg.NotificationDispatcher != null)
        {
            foreach (var r in res.Recipients)
                reg.NotificationDispatcher.Invoke(new Notification(NotificationScope.Target, res.FormattedMessage, Target: r, Speaker: ctx.Executor, Spoofable: false));
        }
        return string.Empty;
    }

    private static IReadOnlyList<ConnectionSnapshot> GetSnapshots(FunctionRegistry reg)
    {
        if (reg.ConnectionsProvider != null)
            return reg.ConnectionsProvider();

        if (reg.Database == null)
            return Array.Empty<ConnectionSnapshot>();

        var now = DateTimeOffset.UtcNow;
        int desc = 1;
        return reg.Database.GetAllObjects()
            .Where(o => o.Type == ObjectType.Player && reg.Database.IsConnected(o.Id))
            .OrderBy(o => o.Id.Value)
            .Select(o => new ConnectionSnapshot(o.Id, desc++, "localhost", "127.0.0.1", "ANSI", now, now, o.HasFlag("DARK")))
            .ToList();
    }

    private static ConnectionSnapshot? FindSnapshot(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return null;
        var looker = reg.Database.GetObject(ctx.Executor);
        bool seeHidden = AttributePermissions.CanSeeAll(looker);
        var snapshots = GetSnapshots(reg);
        string raw = args[0].Trim();
        ConnectionSnapshot? snap = null;
        if (int.TryParse(raw, out int port))
        {
            snap = snapshots.FirstOrDefault(s => s.Descriptor == port);
        }
        if (snap == null)
        {
            var p = reg.Database.GetObject(DbFunctions.ParseDbref(raw, ctx)) ?? reg.Database.FindPlayer(raw.TrimStart('*'));
            if (p != null) snap = snapshots.FirstOrDefault(s => s.PlayerId == p.Id);
        }
        if (snap != null && snap.IsHidden && !seeHidden && snap.PlayerId != ctx.Executor)
            return null;
        return snap;
    }

    public static string ConnSecs(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var snap = FindSnapshot(reg, ctx, args);
        return snap != null ? Math.Max(0, (long)(DateTimeOffset.UtcNow - snap.ConnectedAt).TotalSeconds).ToString() : "-1";
    }

    public static string IdleSecs(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var snap = FindSnapshot(reg, ctx, args);
        return snap != null ? Math.Max(0, (long)(DateTimeOffset.UtcNow - snap.LastActivityAt).TotalSeconds).ToString() : "-1";
    }

    public static string HostInfo(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool ipOnly)
    {
        var snap = FindSnapshot(reg, ctx, args);
        if (snap == null) return MushError.Nothing.Text();
        var looker = reg.Database?.GetObject(ctx.Executor);
        if (snap.PlayerId != ctx.Executor && !AttributePermissions.CanSeeAll(looker))
            return MushError.Nothing.Text();
        return ipOnly ? snap.IpAddress : snap.Host;
    }

    public static string TermInfo(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var snap = FindSnapshot(reg, ctx, args);
        if (snap == null) return MushError.Nothing.Text();
        var looker = reg.Database?.GetObject(ctx.Executor);
        if (snap.PlayerId != ctx.Executor && !AttributePermissions.CanSeeAll(looker))
            return MushError.Nothing.Text();

        var tokens = new List<string>
        {
            string.IsNullOrWhiteSpace(snap.TerminalType) ? "unknown" : snap.TerminalType
        };

        if (snap.SupportsPueblo) tokens.Add("pueblo");
        if (snap.SupportsHtml) tokens.Add("html");
        if (snap.IsWebSocket) tokens.Add("websocket");
        if (snap.SupportsTelnet) tokens.Add("telnet");
        if (snap.SupportsGmcp) tokens.Add("gmcp");
        if (snap.IsSsl) tokens.Add("ssl");
        if (snap.PromptNewlines) tokens.Add("prompt_newlines");
        if (snap.StripAccents) tokens.Add("stripaccents");
        if (snap.SupportsXterm256) tokens.Add("xterm256");
        if (snap.SupportsRgb24) tokens.Add("rgb24");
        if (!string.IsNullOrWhiteSpace(snap.ColorStyle)
            && !tokens.Contains(snap.ColorStyle, StringComparer.OrdinalIgnoreCase))
        {
            tokens.Add(snap.ColorStyle);
        }

        return string.Join(" ", tokens);
    }

    public static string PortsList(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return string.Empty;
        var p = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx)) ?? reg.Database.FindPlayer(args[0].Trim().TrimStart('*'));
        if (p == null) return string.Empty;
        var looker = reg.Database.GetObject(ctx.Executor);
        if (p.Id != ctx.Executor && !AttributePermissions.CanSeeAll(looker))
            return string.Empty;
        var snaps = GetSnapshots(reg).Where(s => s.PlayerId == p.Id);
        return string.Join(" ", snaps.Select(s => s.Descriptor.ToString()));
    }

    public static string LPortsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.Database == null) return string.Empty;
        var looker = reg.Database.GetObject(ctx.Executor);
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            var viewer = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx)) ?? reg.Database.FindPlayer(args[0].Trim().TrimStart('*'));
            if (viewer != null) looker = viewer;
        }
        bool seeHidden = AttributePermissions.CanSeeAll(looker);
        var snaps = GetSnapshots(reg).AsEnumerable();
        if (!seeHidden)
            snaps = snaps.Where(s => !s.IsHidden);
        return string.Join(" ", snaps.Select(s => s.Descriptor.ToString()));
    }

    public static string SentRecvFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool sent)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.TooFewArguments.Text();
        string target = args[0].Trim();
        var snaps = GetSnapshots(reg);
        ConnectionSnapshot? snap = null;

        if (int.TryParse(target, out int port))
        {
            snap = snaps.FirstOrDefault(s => s.Descriptor == port);
        }
        else
        {
            var p = reg.Database.GetObject(DbFunctions.ParseDbref(target, ctx)) ?? reg.Database.FindPlayer(target.TrimStart('*'));
            if (p != null) snap = snaps.FirstOrDefault(s => s.PlayerId == p.Id);
        }

        if (snap == null) return "-1";
        var actor = reg.Database.GetObject(ctx.Executor);
        if (snap.PlayerId != ctx.Executor && !AttributePermissions.CanSeeAll(actor))
            return "-1";

        return (sent ? snap.BytesSent : snap.BytesReceived).ToString();
    }

    private static string WhoList(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool useObjId, bool countOnly, bool slice, WhoMode mode)
    {
        if (reg.Database == null) return countOnly ? "0" : string.Empty;
        var looker = reg.Database.GetObject(ctx.Executor);
        bool seeHidden = AttributePermissions.CanSeeAll(looker);

        var snaps = GetSnapshots(reg).AsEnumerable();
        if (!seeHidden || mode is WhoMode.Mortal or WhoMode.ZoneMortal)
            snaps = snaps.Where(s => !s.IsHidden);

        if (mode is WhoMode.Zone or WhoMode.ZoneMortal)
        {
            if (args.Length == 0) return string.Empty;
            var zone = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx));
            if (zone == null) return string.Empty;
            if (zone.Type == ObjectType.Player && !zone.HasFlag("SHARED"))
                return MushError.InvalidZone.Text();
            if (mode == WhoMode.Zone && !seeHidden && zone.GetLock(PennMUSH.Core.Locks.LockType.Zone) != null && !zone.EvalLock(PennMUSH.Core.Locks.LockType.Zone, ctx.Executor, reg.Database))
                return MushError.PermissionDenied.Text();
            snaps = snaps.Where(s =>
            {
                var pObj = reg.Database.GetObject(s.PlayerId);
                if (pObj == null) return false;
                var loc = reg.Database.GetObject(pObj.Location);
                return pObj.InZone(zone.Id) || (loc != null && loc.InZone(zone.Id));
            });
        }

        var playerIds = snaps.Select(s => s.PlayerId).Distinct().ToList();
        if (countOnly) return playerIds.Count.ToString();

        if (slice)
        {
            if (args.Length < 2 || !int.TryParse(args[0].Trim(), out int start) || !int.TryParse(args[1].Trim(), out int count))
                return MushError.ArgumentMustBeInteger.Text();
            if (start < 1 || count < 1) return MushError.OutOfRange.Text();
            playerIds = playerIds.Skip(start - 1).Take(count).ToList();
        }

        return string.Join(" ", playerIds.Select(id =>
        {
            if (!useObjId) return id.ToString();
            var obj = reg.Database.GetObject(id);
            return obj != null ? $"{obj.Id}:{obj.CreationTime.ToUnixTimeSeconds()}" : id.ToString();
        }));
    }
}
