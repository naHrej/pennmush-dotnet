namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Flags;
using PennMUSH.Core.Locks;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public static class Section9And18Functions
{
    public static void Register(FunctionRegistry registry)
    {
        // Section 9: Side-effect commands & speak (pcreate is in DbFunctions)
        registry.Register("EMIT", (ctx, args) => Emit(registry, ctx, args), sideEffect: true);
        registry.Register("PEMIT", (ctx, args) => Pemit(registry, ctx, args, prompt: false), sideEffect: true);
        registry.Register("PROMPT", (ctx, args) => Pemit(registry, ctx, args, prompt: true), sideEffect: true);
        registry.Register("REMIT", (ctx, args) => Remit(registry, ctx, args), sideEffect: true);
        registry.Register("OEMIT", (ctx, args) => Oemit(registry, ctx, args), sideEffect: true);
        registry.Register("LEMIT", (ctx, args) => Lemit(registry, ctx, args), sideEffect: true);
        registry.Register("MESSAGE", (ctx, args) => MessageFunc(registry, ctx, args), sideEffect: true);
        registry.Register("SET", (ctx, args) => SetFunc(registry, ctx, args), sideEffect: true);
        registry.Register("WIPE", (ctx, args) => WipeFunc(registry, ctx, args), sideEffect: true);
        registry.Register("ATTRIB_SET", (ctx, args) => AttribSetFunc(registry, ctx, args), sideEffect: true);
        registry.Register("LSET", (ctx, args) => LSetFunc(registry, ctx, args), sideEffect: true);
        registry.Register("CREATE", (ctx, args) => CreateFunc(registry, ctx, args), sideEffect: true);
        registry.Register("OPEN", (ctx, args) => OpenFunc(registry, ctx, args), sideEffect: true);
        registry.Register("LINK", (ctx, args) => LinkFunc(registry, ctx, args), sideEffect: true);
        registry.Register("TEL", (ctx, args) => TelFunc(registry, ctx, args), sideEffect: true);
        registry.Register("DIG", (ctx, args) => DigFunc(registry, ctx, args), sideEffect: true);
        registry.Register("CLONE", (ctx, args) => CloneFunc(registry, ctx, args), sideEffect: true);
        registry.Register("SPEAK", (ctx, args) => SpeakFunc(registry, ctx, args));

        // Section 18: Queue inspection and @scan
        registry.Register("PIDINFO", (ctx, args) => PidInfoFunc(registry, ctx, args));
        registry.Register("GETPIDS", (ctx, args) => GetPidsFunc(registry, ctx, args));
        registry.Register("LPIDS", (ctx, args) => LPidsFunc(registry, ctx, args));
        registry.Register("SCAN", (ctx, args) => ScanFunc(registry, ctx, args));
    }

    private static void DispatchNote(FunctionRegistry reg, NotificationScope scope, string message, Dbref target, Dbref location = default, Dbref speaker = default, bool prompt = false)
        => reg.NotificationDispatcher?.Invoke(new Notification(scope, message, Target: scope == NotificationScope.Room && location != default ? location : target, Prompt: prompt, Speaker: speaker));

    private static MUSHObject? ResolveObj(FunctionRegistry reg, EvaluationContext ctx, string spec)
        => reg.Database == null ? null : (reg.Database.GetObject(DbFunctions.ParseDbref(spec, ctx)) ?? reg.Database.MatchObject(ctx.Executor, spec));

    public static string Emit(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return string.Empty;
        var exec = reg.Database.GetObject(ctx.Executor);
        var loc = exec?.Location ?? ctx.Location;
        DispatchNote(reg, NotificationScope.Room, args[0], ctx.Executor, loc, ctx.Executor);
        return string.Empty;
    }

    public static string Pemit(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool prompt)
    {
        if (args.Length < 2 || reg.Database == null) return string.Empty;
        foreach (string tok in args[0].Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var target = ResolveObj(reg, ctx, tok);
            if (target != null)
                DispatchNote(reg, NotificationScope.Target, args[1], target.Id, target.Location, ctx.Executor, prompt);
        }
        return string.Empty;
    }

    public static string Remit(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return string.Empty;
        var room = ResolveObj(reg, ctx, args[0]);
        if (room != null)
            DispatchNote(reg, NotificationScope.Room, args[1], ctx.Executor, room.Id, ctx.Executor);
        return string.Empty;
    }

    public static string Oemit(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return string.Empty;
        var excluded = new HashSet<Dbref>();
        Dbref loc = ctx.Location;
        foreach (string tok in args[0].Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var obj = ResolveObj(reg, ctx, tok);
            if (obj != null)
            {
                excluded.Add(obj.Id);
                if (obj.Location.IsValid) loc = obj.Location;
            }
        }
        foreach (var item in reg.Database.GetContents(loc))
        {
            if (!excluded.Contains(item.Id))
                DispatchNote(reg, NotificationScope.Target, args[1], item.Id, loc, ctx.Executor);
        }
        return string.Empty;
    }

    public static string Lemit(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return string.Empty;
        var exec = reg.Database.GetObject(ctx.Executor);
        var loc = exec != null ? reg.Database.GetObject(DbFunctions.ParseDbref(DbFunctions.WhereFunc(reg, ctx, [exec.Id.ToString()]), ctx)) : null;
        if (loc != null)
            DispatchNote(reg, NotificationScope.Room, args[0], ctx.Executor, loc.Id, ctx.Executor);
        return string.Empty;
    }

    public static string MessageFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3 || reg.Database == null) return string.Empty;
        var target = ResolveObj(reg, ctx, args[0]);
        if (target == null) return MushError.NoMatch.Text();

        string msg = args[2];
        var attrObj = ResolveObj(reg, ctx, args[1].Contains('/') ? args[1].Split('/')[0] : ctx.Executor.ToString());
        string attrName = args[1].Contains('/') ? args[1].Split('/', 2)[1] : args[1];
        if (attrObj != null && attrObj.GetAttribute(attrName, reg.Database) != null && reg.Evaluator != null)
        {
            var uargs = new string[Math.Max(1, args.Length - 2)];
            uargs[0] = args[2];
            for (int i = 3; i < args.Length; i++) uargs[i - 2] = args[i];
            msg = DbFunctions.Ufun(reg, ctx, new[] { $"{attrObj.Id}/{attrName}" }.Concat(uargs).ToArray());
        }
        DispatchNote(reg, NotificationScope.Target, msg, target.Id, target.Location, ctx.Executor);
        return string.Empty;
    }

    public static string SetFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return MushError.TooFewArguments.Text();
        var actor = reg.Database.GetObject(ctx.Executor);
        var target = ResolveObj(reg, ctx, args[0]);
        if (actor == null || target == null) return MushError.NoMatch.Text();
        if (!AttributePermissions.Controls(actor, target)) return MushError.PermissionDenied.Text();

        string val = args[1].Trim();
        int colon = val.IndexOf(':');
        if (colon > 0)
        {
            var res = AttributeWriter.Apply(actor, target, val.Substring(0, colon), val.Substring(colon + 1));
            return res.Ok ? string.Empty : MushError.PermissionDenied.Text();
        }

        bool clear = val.StartsWith('!');
        string flagName = clear ? val.Substring(1) : val;
        var def = FlagTable.FindFlag(flagName);
        if (def == null) return MushError.Nothing.Text();
        if (!FlagTable.CanSetFlag(actor, target, def, clear, out _))
            return MushError.PermissionDenied.Text();
        if (clear) target.RemoveFlag(def.Name);
        else target.AddFlag(def.Name);
        return string.Empty;
    }

    public static string WipeFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.TooFewArguments.Text();
        var actor = reg.Database.GetObject(ctx.Executor);
        int slash = args[0].IndexOf('/');
        var target = ResolveObj(reg, ctx, slash >= 0 ? args[0].Substring(0, slash) : args[0]);
        string pattern = slash >= 0 ? args[0].Substring(slash + 1).Trim() : "*";
        if (actor == null || target == null) return MushError.NoMatch.Text();
        if (!AttributePermissions.Controls(actor, target)) return MushError.PermissionDenied.Text();

        foreach (var attr in target.Attributes.Where(a => PatternMatcher.Match(pattern, a.Name, out _)).ToList())
            AttributeWriter.Apply(actor, target, attr.Name, null);
        return string.Empty;
    }

    public static string AttribSetFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.TooFewArguments.Text();
        int slash = args[0].IndexOf('/');
        if (slash <= 0 || slash >= args[0].Length - 1) return MushError.Nothing.Text();

        var actor = reg.Database.GetObject(ctx.Executor);
        var target = ResolveObj(reg, ctx, args[0].Substring(0, slash));
        string attrName = args[0].Substring(slash + 1).Trim();
        if (actor == null || target == null) return MushError.NoMatch.Text();
        if (!AttributePermissions.Controls(actor, target)) return MushError.PermissionDenied.Text();

        var res = AttributeWriter.Apply(actor, target, attrName, args.Length > 1 ? args[1] : string.Empty);
        return res.Ok ? string.Empty : MushError.PermissionDenied.Text();
    }

    public static string LSetFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return MushError.TooFewArguments.Text();
        int slash = args[0].IndexOf('/');
        var actor = reg.Database.GetObject(ctx.Executor);
        var target = ResolveObj(reg, ctx, slash >= 0 ? args[0].Substring(0, slash) : args[0]);
        string lockType = slash >= 0 ? args[0].Substring(slash + 1).Trim() : LockType.Basic;
        if (actor == null || target == null) return MushError.NoMatch.Text();
        if (!AttributePermissions.Controls(actor, target)) return MushError.PermissionDenied.Text();

        var entry = target.GetLock(lockType);
        if (entry == null) return MushError.NoSuchLock.Text();

        var flags = entry.Flags;
        foreach (string tok in args[1].Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            bool clear = tok.StartsWith('!');
            string name = (clear ? tok.Substring(1) : tok).ToLowerInvariant();
            LockFlags lf = name switch
            {
                "visual" => LockFlags.Visual,
                "private" => LockFlags.Private,
                "wizard" => LockFlags.Wizard,
                "locked" => LockFlags.Locked,
                "noclone" => LockFlags.NoClone,
                "owner" => LockFlags.Owner,
                _ => LockFlags.None
            };
            if (lf == LockFlags.None) return MushError.Nothing.Text();
            flags = clear ? flags & ~lf : flags | lf;
        }
        target.SetLock(entry.Type, entry.Key, entry.Creator, flags);
        return string.Empty;
    }

    public static string CreateFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null || string.IsNullOrWhiteSpace(args[0]))
            return MushError.Nothing.Text();
        var thing = reg.Database.CreateObject(args[0].Trim(), ObjectType.Thing, ctx.Executor);
        reg.Database.MoveObject(thing.Id, ctx.Executor);
        return thing.Id.ToString();
    }

    public static string OpenFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null || string.IsNullOrWhiteSpace(args[0]))
            return MushError.Nothing.Text();
        var exec = reg.Database.GetObject(ctx.Executor);
        Dbref srcRoom = args.Length > 2 && !string.IsNullOrWhiteSpace(args[2])
            ? (ResolveObj(reg, ctx, args[2])?.Id ?? ctx.Location)
            : (exec?.Location.IsValid == true ? exec.Location : ctx.Location);

        var exit = reg.Database.CreateObject(args[0].Trim(), ObjectType.Exit, ctx.Executor);
        if (srcRoom.IsValid) reg.Database.AttachExit(srcRoom, exit.Id);
        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
        {
            var dest = ResolveObj(reg, ctx, args[1]);
            if (dest != null) reg.Database.LinkExit(exit.Id, dest.Id);
        }
        return exit.Id.ToString();
    }

    public static string LinkFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return MushError.TooFewArguments.Text();
        var actor = reg.Database.GetObject(ctx.Executor);
        var obj = ResolveObj(reg, ctx, args[0]);
        var dest = ResolveObj(reg, ctx, args[1]);
        if (actor == null || obj == null || dest == null) return MushError.NoMatch.Text();
        if (!AttributePermissions.Controls(actor, obj)) return MushError.PermissionDenied.Text();

        if (obj.Type == ObjectType.Exit) reg.Database.LinkExit(obj.Id, dest.Id);
        else obj.Home = dest.Id;
        return string.Empty;
    }

    public static string TelFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return MushError.TooFewArguments.Text();
        var actor = reg.Database.GetObject(ctx.Executor);
        var victim = ResolveObj(reg, ctx, args[0]);
        var dest = ResolveObj(reg, ctx, args[1]);
        if (actor == null || victim == null || dest == null) return MushError.NoMatch.Text();
        if (victim.Type == ObjectType.Room) return MushError.PermissionDenied.Text();
        if (!AttributePermissions.Controls(actor, victim) && !actor.HasPower("Tel_Anything"))
            return MushError.PermissionDenied.Text();
        if (!AttributePermissions.Controls(actor, dest) && !dest.HasFlag("JUMP_OK") && !actor.HasPower("Tel_Anywhere"))
            return MushError.PermissionDenied.Text();
        reg.Database.MoveObject(victim.Id, dest.Id);
        return string.Empty;
    }

    public static string DigFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null || string.IsNullOrWhiteSpace(args[0]))
            return MushError.Nothing.Text();
        var room = reg.Database.CreateObject(args[0].Trim(), ObjectType.Room, ctx.Executor);
        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
        {
            var exec = reg.Database.GetObject(ctx.Executor);
            if (exec != null && exec.Location.IsValid)
            {
                var ex = reg.Database.CreateObject(args[1].Trim(), ObjectType.Exit, ctx.Executor);
                reg.Database.AttachExit(exec.Location, ex.Id);
                reg.Database.LinkExit(ex.Id, room.Id);
            }
        }
        if (args.Length > 2 && !string.IsNullOrWhiteSpace(args[2]))
        {
            var exec = reg.Database.GetObject(ctx.Executor);
            if (exec != null && exec.Location.IsValid)
            {
                var back = reg.Database.CreateObject(args[2].Trim(), ObjectType.Exit, ctx.Executor);
                reg.Database.AttachExit(room.Id, back.Id);
                reg.Database.LinkExit(back.Id, exec.Location);
            }
        }
        return room.Id.ToString();
    }

    public static string CloneFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.Nothing.Text();
        var actor = reg.Database.GetObject(ctx.Executor);
        var src = ResolveObj(reg, ctx, args[0]);
        if (src == null) return MushError.NoMatch.Text();
        if (!AttributePermissions.CanExamine(actor, src))
            return MushError.PermissionDenied.Text();

        string newName = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]) ? args[1].Trim() : src.Name;
        var clone = reg.Database.CreateObject(newName, src.Type, ctx.Executor);
        foreach (string f in src.Flags) clone.AddFlag(f);
        foreach (var a in src.Attributes)
        {
            if (!a.Flags.HasFlag(AttributeFlags.NoCopy))
                clone.SetAttribute(a.Name, a.Value, ctx.Executor, a.Flags);
        }
        if (src.Location.IsValid) reg.Database.MoveObject(clone.Id, src.Location);
        return clone.Id.ToString();
    }

    public static string SpeakFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return string.Empty;
        var speaker = ResolveObj(reg, ctx, args[0]);
        string speakerName = speaker?.Name ?? args[0];
        string text = args[1];
        string sayStr = args.Length > 2 && !string.IsNullOrEmpty(args[2]) ? args[2] : "says,";

        if (text.StartsWith(':'))
            return $"{speakerName} {text.Substring(1).TrimStart()}";
        if (text.StartsWith(';'))
            return $"{speakerName}{text.Substring(1)}";
        return $"{speakerName} {sayStr} \"{text}\"";
    }

    public static string PidInfoFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Queue == null) return MushError.Nothing.Text();
        if (!int.TryParse(args[0].Trim(), out int pid)) return MushError.ArgumentMustBeInteger.Text();

        var entry = reg.Queue.Snapshot().FirstOrDefault(e => e.Pid == pid);
        if (entry == null) return MushError.NoMatch.Text();

        string field = args.Length > 1 ? args[1].Trim().ToLowerInvariant() : "executor";
        string sep = args.Length > 2 ? args[2] : " ";
        var fields = field.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var values = new List<string>(fields.Length);
        foreach (string f in fields)
        {
            values.Add(f switch
            {
                "executor" => entry.Executor.ToString(),
                "enactor" => entry.Enactor.ToString(),
                "command" => entry.Command,
                "semaphore" => entry.Semaphore ?? string.Empty,
                "time" => Math.Max(0, (long)(entry.ScheduledTime - DateTimeOffset.UtcNow).TotalSeconds).ToString(),
                _ => string.Empty
            });
        }
        return string.Join(sep, values);
    }

    public static string GetPidsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.Queue == null) return string.Empty;
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            return string.Join(" ", reg.Queue.Snapshot().Select(e => e.Pid));

        var target = ResolveObj(reg, ctx, args[0]);
        if (target == null) return string.Empty;
        return string.Join(" ", reg.Queue.Snapshot().Where(e => e.Executor == target.Id).Select(e => e.Pid));
    }

    public static string LPidsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.Queue == null) return string.Empty;
        var entries = reg.Queue.Snapshot().AsEnumerable();
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            var target = ResolveObj(reg, ctx, args[0]);
            if (target != null)
                entries = entries.Where(e => e.Executor == target.Id);
            else
                return string.Empty;
        }
        return string.Join(" ", entries.Select(e => e.Pid));
    }

    public static string ScanFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return string.Empty;
        Dbref lookerRef = ctx.Executor;
        string commandStr;
        if (args.Length >= 2)
        {
            lookerRef = ResolveObj(reg, ctx, args[0])?.Id ?? ctx.Executor;
            commandStr = args[1];
        }
        else
        {
            commandStr = args[0];
        }

        var looker = reg.Database.GetObject(lookerRef);
        if (looker == null) return string.Empty;

        var hits = new List<string>();
        var candidates = reg.Database.GetAllObjects().Where(o =>
            o.Type != ObjectType.Garbage &&
            (o.Id == looker.Id || o.Location == looker.Id || o.Location == looker.Location || o.Id == looker.Location));

        foreach (var obj in candidates.OrderBy(o => o.Id.Value))
        {
            foreach (var attr in obj.Attributes)
            {
                if (!attr.Value.StartsWith('$')) continue;
                int colon = attr.Value.IndexOf(':');
                if (colon <= 1) continue;
                string pat = attr.Value.Substring(1, colon - 1);
                if (PatternMatcher.Match(pat, commandStr, out _))
                    hits.Add($"{obj.Id}/{attr.Name}");
            }
        }
        return string.Join(" ", hits);
    }
}
