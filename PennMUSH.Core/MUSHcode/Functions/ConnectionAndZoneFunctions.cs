namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Commands;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public sealed record ConnectionSnapshot(
    Dbref PlayerId,
    int Descriptor,
    string Host,
    string IpAddress,
    string TerminalType,
    DateTimeOffset ConnectedAt,
    DateTimeOffset LastActivityAt,
    bool IsHidden = false);

public static class ConnectionAndZoneFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        // Section 10: Zone broadcast & nospoof
        registry.Register("ZEMIT", (ctx, args) => ZEmit(registry, ctx, args, spoofable: true));
        registry.Register("NSZEMIT", (ctx, args) => ZEmit(registry, ctx, args, spoofable: false));
        registry.Register("NSCEMIT", (ctx, args) => NsCemit(registry, ctx, args));
        registry.Register("NSEMIT", (ctx, args) => Section9And18Functions.Emit(registry, ctx, args));
        registry.Register("NSLEMIT", (ctx, args) => Section9And18Functions.Lemit(registry, ctx, args));
        registry.Register("NSOEMIT", (ctx, args) => Section9And18Functions.Oemit(registry, ctx, args));
        registry.Register("NSPEMIT", (ctx, args) => Section9And18Functions.Pemit(registry, ctx, args, prompt: false));
        registry.Register("NSPROMPT", (ctx, args) => Section9And18Functions.Pemit(registry, ctx, args, prompt: true));
        registry.Register("NSREMIT", (ctx, args) => Section9And18Functions.Remit(registry, ctx, args));

        // Section 12: Connected descriptors / WHO
        registry.Register("CONN", (ctx, args) => ConnSecs(registry, ctx, args));
        registry.Register("IDLE", (ctx, args) => IdleSecs(registry, ctx, args));
        registry.Register("IDLESECS", (ctx, args) => IdleSecs(registry, ctx, args));
        registry.Register("HOST", (ctx, args) => HostInfo(registry, ctx, args, ipOnly: false));
        registry.Register("HOSTNAME", (ctx, args) => HostInfo(registry, ctx, args, ipOnly: false));
        registry.Register("IPADDR", (ctx, args) => HostInfo(registry, ctx, args, ipOnly: true));
        registry.Register("TERMINFO", (ctx, args) => TermInfo(registry, ctx, args));
        registry.Register("PORTS", (ctx, args) => PortsList(registry, ctx, args, withType: false));
        registry.Register("LPORTS", (ctx, args) => PortsList(registry, ctx, args, withType: true));

        registry.Register("LWHO", (ctx, args) => WhoList(registry, ctx, args, useObjId: false, countOnly: false, slice: false, mode: WhoMode.All));
        registry.Register("LWHOID", (ctx, args) => WhoList(registry, ctx, args, useObjId: true, countOnly: false, slice: false, mode: WhoMode.All));
        registry.Register("NWHO", (ctx, args) => WhoList(registry, ctx, args, useObjId: false, countOnly: true, slice: false, mode: WhoMode.All));
        registry.Register("XWHO", (ctx, args) => WhoList(registry, ctx, args, useObjId: false, countOnly: false, slice: true, mode: WhoMode.All));
        registry.Register("XWHOID", (ctx, args) => WhoList(registry, ctx, args, useObjId: true, countOnly: false, slice: true, mode: WhoMode.All));

        registry.Register("MWHO", (ctx, args) => WhoList(registry, ctx, args, useObjId: false, countOnly: false, slice: false, mode: WhoMode.Room));
        registry.Register("MWHOID", (ctx, args) => WhoList(registry, ctx, args, useObjId: true, countOnly: false, slice: false, mode: WhoMode.Room));
        registry.Register("NMWHO", (ctx, args) => WhoList(registry, ctx, args, useObjId: false, countOnly: true, slice: false, mode: WhoMode.Room));
        registry.Register("XMWHO", (ctx, args) => WhoList(registry, ctx, args, useObjId: false, countOnly: false, slice: true, mode: WhoMode.Room));
        registry.Register("XMWHOID", (ctx, args) => WhoList(registry, ctx, args, useObjId: true, countOnly: false, slice: true, mode: WhoMode.Room));

        registry.Register("ZWHO", (ctx, args) => WhoList(registry, ctx, args, useObjId: false, countOnly: false, slice: false, mode: WhoMode.Zone));
        registry.Register("ZMWHO", (ctx, args) => WhoList(registry, ctx, args, useObjId: false, countOnly: false, slice: false, mode: WhoMode.ZoneMortal));
    }

    private enum WhoMode { All, Room, Zone, ZoneMortal }

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
        var snapshots = GetSnapshots(reg);
        string raw = args[0].Trim();
        if (int.TryParse(raw, out int port))
        {
            var byPort = snapshots.FirstOrDefault(s => s.Descriptor == port);
            if (byPort != null) return byPort;
        }
        var p = reg.Database.GetObject(DbFunctions.ParseDbref(raw, ctx)) ?? reg.Database.FindPlayer(raw.TrimStart('*'));
        return p != null ? snapshots.FirstOrDefault(s => s.PlayerId == p.Id) : null;
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
        return ipOnly ? snap.IpAddress : snap.Host;
    }

    public static string TermInfo(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var snap = FindSnapshot(reg, ctx, args);
        return snap != null ? snap.TerminalType : MushError.Nothing.Text();
    }

    public static string PortsList(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool withType)
    {
        if (args.Length == 0 || reg.Database == null) return string.Empty;
        var p = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx)) ?? reg.Database.FindPlayer(args[0].Trim().TrimStart('*'));
        if (p == null) return string.Empty;
        var snaps = GetSnapshots(reg).Where(s => s.PlayerId == p.Id);
        return string.Join(" ", snaps.Select(s => withType ? $"{s.Descriptor}:telnet" : s.Descriptor.ToString()));
    }

    private static string WhoList(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool useObjId, bool countOnly, bool slice, WhoMode mode)
    {
        if (reg.Database == null) return countOnly ? "0" : string.Empty;
        var looker = reg.Database.GetObject(ctx.Executor);
        bool seeHidden = AttributePermissions.CanSeeAll(looker);

        var snaps = GetSnapshots(reg).AsEnumerable();
        if (!seeHidden || mode == WhoMode.ZoneMortal)
            snaps = snaps.Where(s => !s.IsHidden);

        if (mode == WhoMode.Room)
        {
            Dbref roomRef = looker?.Location ?? ctx.Location;
            if (args.Length > 0 && !slice && !string.IsNullOrWhiteSpace(args[0]))
            {
                var argObj = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx));
                if (argObj != null) roomRef = argObj.Type == ObjectType.Room ? argObj.Id : argObj.Location;
            }
            snaps = snaps.Where(s => reg.Database.GetObject(s.PlayerId)?.Location == roomRef);
        }
        else if (mode is WhoMode.Zone or WhoMode.ZoneMortal)
        {
            if (args.Length == 0) return string.Empty;
            var zone = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx));
            if (zone == null) return string.Empty;
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
            int idx1 = mode == WhoMode.Room && args.Length >= 3 ? 1 : 0;
            if (args.Length < idx1 + 2 || !int.TryParse(args[idx1].Trim(), out int start) || !int.TryParse(args[idx1 + 1].Trim(), out int count))
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
