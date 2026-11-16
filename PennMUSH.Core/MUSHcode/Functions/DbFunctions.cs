namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Linq;
using System.Text;
using PennMUSH.Core.Flags;
using PennMUSH.Core.Types;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Database;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Queue;
using PennMUSH.Core.MUSHcode;

public static class DbFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Register("NAME", (ctx, args) => Name(registry, ctx, args));
        registry.Register("NAMEFORMAT", (ctx, args) => NameFormat(registry, ctx, args));
        registry.Register("GET", (ctx, args) => Get(registry, ctx, args));
        registry.Register("HASATTR", (ctx, args) => HasAttr(registry, ctx, args));
        registry.Register("LOC", (ctx, args) => Loc(registry, ctx, args));
        registry.Register("OWNER", (ctx, args) => Owner(registry, ctx, args));
        registry.Register("TYPE", (ctx, args) => Type(registry, ctx, args));
        registry.Register("U", (ctx, args) => Ufun(registry, ctx, args));
        registry.Register("UFUN", (ctx, args) => Ufun(registry, ctx, args));
        registry.Register("HASFLAG", (ctx, args) => HasFlagFunc(registry, ctx, args));
        registry.Register("HASPOWER", (ctx, args) => HasPowerFunc(registry, ctx, args));
        registry.Register("FLAGS", (ctx, args) => FlagsFunc(registry, ctx, args));
        registry.Register("LFLAGS", (ctx, args) => LflagsFunc(registry, ctx, args));
        registry.Register("POWERS", (ctx, args) => PowersFunc(registry, ctx, args));
        registry.Register("CON", (ctx, args) => ConFunc(registry, ctx, args));
        registry.Register("EXITS", (ctx, args) => ExitsFunc(registry, ctx, args));
        registry.Register("LEXITS", (ctx, args) => ExitsFunc(registry, ctx, args));
        registry.Register("PARENT", (ctx, args) => ParentFunc(registry, ctx, args));
        registry.Register("LPARENT", (ctx, args) => LparentFunc(registry, ctx, args));
        registry.Register("ZONE", (ctx, args) => ZoneFunc(registry, ctx, args));
        registry.Register("LZONE", (ctx, args) => LzoneFunc(registry, ctx, args));
        registry.Register("INZONE", (ctx, args) => InzoneFunc(registry, ctx, args));
        registry.Register("NUM", (ctx, args) => NumFunc(ctx, args));
        registry.Register("RNUM", (ctx, args) => RnumFunc(ctx, args));
        registry.Register("FULLNAME", (ctx, args) => FullNameFunc(registry, ctx, args));
        registry.Register("RLOC", (ctx, args) => RlocFunc(registry, ctx, args));
        registry.Register("WHERE", (ctx, args) => WhereFunc(registry, ctx, args));
        registry.Register("HOME", (ctx, args) => HomeFunc(registry, ctx, args));
        registry.Register("HASTYPE", (ctx, args) => HasTypeFunc(registry, ctx, args));
        registry.Register("CONTROLS", (ctx, args) => ControlsFunc(registry, ctx, args));
        registry.Register("NEARBY", (ctx, args) => NearbyFunc(registry, ctx, args));
        registry.Register("VISIBLE", (ctx, args) => VisibleFunc(registry, ctx, args));
        registry.Register("XGET", (ctx, args) => XgetFunc(registry, ctx, args));
        registry.Register("DEFAULT", (ctx, args) => DefaultFunc(registry, ctx, args));
        registry.Register("EDEFAULT", (ctx, args) => EDefaultFunc(registry, ctx, args));
        registry.Register("EVAL", (ctx, args) => EvalFunc(registry, ctx, args));
        registry.Register("OBJEVAL", (ctx, args) => ObjEvalFunc(registry, ctx, args));
        registry.Register("V", (ctx, args) => VFunc(registry, ctx, args));
        registry.Register("S", (ctx, args) => SFunc(registry, ctx, args));
        registry.Register("LATTR", (ctx, args) => LattrFunc(registry, ctx, args));
        registry.Register("NATTR", (ctx, args) => NattrFunc(registry, ctx, args));
        registry.Register("ATTRCNT", (ctx, args) => NattrFunc(registry, ctx, args));
        registry.Register("OBJID", (ctx, args) => ObjIdFunc(registry, ctx, args));
        registry.Register("TESTLOCK", (ctx, args) => TestLockFunc(registry, ctx, args));
        registry.Register("PCREATE", (ctx, args) => Pcreate(registry, ctx, args));
    }

    private static string Pcreate(FunctionRegistry registry, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || registry.Database == null)
            return MushError.Nothing.Text();
        var actor = registry.Database.GetObject(ctx.Executor);
        if (actor == null)
            return MushError.Nothing.Text();
        var created = PlayerCreator.Create(
            registry.Database,
            actor,
            args[0],
            args[1],
            args.Length > 2 ? args[2] : null,
            registry.Passwords,
            registry.DefaultHome,
            registry.PlayerStart,
            registry.StartingMoney,
            message => registry.NotificationDispatcher?.Invoke(
                new Notification(NotificationScope.Self, message, ctx.Executor)));
        return created.IsValid ? created.ToString() : MushError.Nothing.Text();
    }

    public static Dbref ParseDbref(string s, EvaluationContext? ctx = null)
    {
        if (string.IsNullOrWhiteSpace(s)) return Dbref.Nothing;
        s = s.Trim();
        if (s.Equals("me", StringComparison.OrdinalIgnoreCase))
            return ctx != null ? ctx.Executor : Dbref.Nothing;
        if (s.Equals("here", StringComparison.OrdinalIgnoreCase))
            return ctx != null ? ctx.Location : Dbref.Nothing;
        if (s.StartsWith('#')) s = s.Substring(1);
        return int.TryParse(s, out int val) ? new Dbref(val) : Dbref.Nothing;
    }

    public static string Name(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        return obj != null ? obj.Name : MushError.NoMatch.Text();
    }

    public static string NameFormat(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        string plain = Name(reg, ctx, args);
        if (args.Length == 0 || reg.Database == null || plain == MushError.NoMatch.Text())
            return plain;

        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null)
            return plain;

        string? formatted = FormatAttributes.Evaluate(
            reg.Database,
            reg.Evaluator,
            obj,
            ctx.Executor,
            ctx.Executor,
            "NAMEFORMAT",
            obj.Id.ToString(),
            plain);
        return string.IsNullOrWhiteSpace(formatted) ? plain : formatted;
    }

    public static string Get(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        int slash = args[0].IndexOf('/');
        if (slash <= 0 || slash >= args[0].Length - 1) return MushError.NoMatch.Text();

        var dbref = ParseDbref(args[0].Substring(0, slash), ctx);
        string attrName = args[0].Substring(slash + 1).Trim();

        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        var attr = obj.GetAttribute(attrName, reg.Database);
        return attr != null ? attr.Value : string.Empty;
    }

    public static string HasAttr(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return "0";
        var dbref = ParseDbref(args[0], ctx);
        string attrName = args[1].Trim();

        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return "0";

        return obj.HasAttribute(attrName, reg.Database) ? "1" : "0";
    }

    public static string Loc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        return obj != null ? obj.Location.ToString() : MushError.NoMatch.Text();
    }

    public static string Owner(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        return obj != null ? obj.Owner.ToString() : MushError.NoMatch.Text();
    }

    public static string Type(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        return obj != null ? obj.Type.ToString().ToUpperInvariant() : MushError.NoMatch.Text();
    }

    public static string Ufun(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        int slash = args[0].IndexOf('/');
        if (slash <= 0 || slash >= args[0].Length - 1) return MushError.NoMatch.Text();

        var dbref = ParseDbref(args[0].Substring(0, slash), ctx);
        string attrName = args[0].Substring(slash + 1).Trim();

        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        var attr = obj.GetAttribute(attrName, reg.Database);
        if (attr == null) return string.Empty;

        string[] ufunArgs = args.Length > 1 ? args[1..] : Array.Empty<string>();

        if (reg.Evaluator != null)
        {
            var childCtx = ctx.CreateChildContext(obj.Id, ctx.Executor, ufunArgs);
            if (!childCtx.PushCall(obj.Id, attrName))
            {
                return MushError.RecursionCycleDetected.Text();
            }
            try
            {
                return reg.Evaluator.Evaluate(attr.Value, childCtx);
            }
            finally
            {
                childCtx.PopCall(obj.Id, attrName);
            }
        }

        return attr.Value;
    }

    public static string HasFlagFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        string flagQuery = args[1].Trim();
        if (flagQuery.Length == 1)
        {
            char letter = flagQuery[0];
            // Check type letter shortcuts
            if (letter == 'P' && obj.Type == ObjectType.Player) return "1";
            if (letter == 'R' && obj.Type == ObjectType.Room) return "1";
            if (letter == 'T' && obj.Type == ObjectType.Thing) return "1";
            if (letter == 'E' && obj.Type == ObjectType.Exit) return "1";

            var def = FlagTable.FindFlagByLetter(letter, obj.Type);
            if (def != null && obj.HasFlag(def.Name)) return "1";
        }
        else
        {
            var def = FlagTable.FindFlag(flagQuery);
            if (def != null && obj.HasFlag(def.Name)) return "1";
        }

        return "0";
    }

    public static string HasPowerFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        string powerQuery = args[1].Trim();
        var def = FlagTable.FindPower(powerQuery);
        return (def != null && obj.HasPower(def.Name)) ? "1" : "0";
    }

    public static string FlagsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        var sb = new StringBuilder();
        // Type letter first
        sb.Append(obj.Type switch
        {
            ObjectType.Player => 'P',
            ObjectType.Room => 'R',
            ObjectType.Thing => 'T',
            ObjectType.Exit => 'E',
            ObjectType.Garbage => 'g',
            _ => '?'
        });

        foreach (var flagName in obj.Flags.OrderBy(f => f))
        {
            var def = FlagTable.FindFlag(flagName);
            if (def != null && def.Letter != '\0')
                sb.Append(def.Letter);
        }

        return sb.ToString();
    }

    public static string LflagsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        return string.Join(" ", obj.Flags.OrderBy(f => f));
    }

    public static string PowersFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        return string.Join(" ", obj.Powers.OrderBy(p => p));
    }

    public static string ConFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        return obj.Contents.ToString();
    }

    public static string ExitsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        return string.Join(" ", reg.Database.GetExits(obj.Id).Select(e => e.Id.ToString()));
    }

    public static string ParentFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        return obj.Parent.ToString();
    }

    public static string LparentFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        return string.Join(" ", obj.Parents.Select(p => p.ToString()));
    }

    public static string ZoneFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        return obj.Zone.ToString();
    }

    public static string LzoneFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        return string.Join(" ", obj.Zones.Select(z => z.ToString()));
    }

    public static string InzoneFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return "0";
        var targetRef = ParseDbref(args[0], ctx);
        var zoneRef = ParseDbref(args[1], ctx);

        var obj = reg.Database.GetObject(targetRef);
        if (obj == null) return "0";

        return obj.InZone(zoneRef) ? "1" : "0";
    }

    public static string NumFunc(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        return dbref.IsValid ? dbref.ToString() : MushError.NoMatch.Text();
    }

    public static string RnumFunc(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "-1";
        var dbref = ParseDbref(args[0], ctx);
        return dbref.IsValid ? dbref.Value.ToString() : "-1";
    }

    public static string FullNameFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        return obj != null ? obj.Name : MushError.NoMatch.Text();
    }

    public static string RlocFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        int levels = 1;
        if (args.Length > 1 && int.TryParse(args[1].Trim(), out int lvl) && lvl > 0)
            levels = lvl;

        var curr = obj.Location;
        for (int i = 1; i < levels && curr.IsValid; i++)
        {
            var parentObj = reg.Database.GetObject(curr);
            if (parentObj == null || !parentObj.Location.IsValid) break;
            curr = parentObj.Location;
        }

        return curr.ToString();
    }

    public static string WhereFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        var curr = obj;
        var seen = new HashSet<Dbref>();
        while (curr != null && curr.Type != ObjectType.Room && curr.Location.IsValid && seen.Add(curr.Id))
        {
            var next = reg.Database.GetObject(curr.Location);
            if (next == null) break;
            curr = next;
        }

        return curr != null ? curr.Id.ToString() : MushError.NoMatch.Text();
    }

    public static string HomeFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        return obj.Home.IsValid ? obj.Home.ToString() : "#0";
    }

    public static string HasTypeFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return "0";
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return "0";

        string targetType = args[1].Trim();
        return string.Equals(obj.Type.ToString(), targetType, StringComparison.OrdinalIgnoreCase) ? "1" : "0";
    }

    public static string ControlsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return "0";
        var actorRef = ParseDbref(args[0], ctx);
        var targetRef = ParseDbref(args[1], ctx);

        var actor = reg.Database.GetObject(actorRef);
        var target = reg.Database.GetObject(targetRef);
        if (actor == null || target == null) return "0";

        if (actor.IsGod || actor.IsWizard) return "1";
        if (target.Owner == actor.Id || target.Owner == actor.Owner) return "1";

        return "0";
    }

    public static string NearbyFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return "0";
        var o1 = reg.Database.GetObject(ParseDbref(args[0], ctx));
        var o2 = reg.Database.GetObject(ParseDbref(args[1], ctx));
        if (o1 == null || o2 == null) return "0";

        if (o1.Id == o2.Id) return "1";
        if (o1.Location == o2.Id || o2.Location == o1.Id) return "1";
        if (o1.Location.IsValid && o1.Location == o2.Location) return "1";

        return "0";
    }

    public static string VisibleFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return "0";
        var looker = reg.Database.GetObject(ParseDbref(args[0], ctx));
        var target = reg.Database.GetObject(ParseDbref(args[1], ctx));
        if (looker == null || target == null) return "0";

        if (looker.IsGod || looker.IsWizard) return "1";
        if (target.HasFlag("DARK")) return "0";

        return "1";
    }

    public static string XgetFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        string attrName = args[1].Trim();

        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        var attr = obj.GetAttribute(attrName, reg.Database);
        return attr != null ? attr.Value : string.Empty;
    }

    public static string DefaultFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string val = Get(reg, ctx, [args[0]]);
        return (!string.IsNullOrEmpty(val) && !val.StartsWith("#-")) ? val : args[1];
    }

    public static string EDefaultFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string val = Get(reg, ctx, [args[0]]);
        if (!string.IsNullOrEmpty(val) && !val.StartsWith("#-")) return val;

        if (reg.Evaluator != null)
        {
            return reg.Evaluator.Evaluate(args[1], ctx);
        }
        return args[1];
    }

    public static string EvalFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Evaluator == null) return string.Empty;
        var dbref = ParseDbref(args[0], ctx);
        string attrName = args[1].Trim();

        var obj = reg.Database?.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        var attr = obj.GetAttribute(attrName, reg.Database);
        if (attr == null) return string.Empty;

        return reg.Evaluator.Evaluate(attr.Value, ctx);
    }

    public static string ObjEvalFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Evaluator == null) return string.Empty;
        var dbref = ParseDbref(args[0], ctx);

        var childCtx = ctx.CreateChildContext(dbref, ctx.Enactor, ctx.Arguments);
        return reg.Evaluator.Evaluate(args[1], childCtx);
    }

    public static string VFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return string.Empty;
        var obj = reg.Database.GetObject(ctx.Executor);
        if (obj == null) return string.Empty;

        var attr = obj.GetAttribute(args[0].Trim(), reg.Database);
        return attr != null ? attr.Value : string.Empty;
    }

    public static string SFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Evaluator == null) return string.Empty;
        return reg.Evaluator.Evaluate(args[0], ctx);
    }

    public static string LattrFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return string.Empty;
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return string.Empty;

        string pattern = args.Length > 1 ? args[1].Trim() : "*";
        var names = obj.Attributes
            .Where(a => PatternMatcher.Match(pattern, a.Name, out _))
            .Select(a => a.Name)
            .OrderBy(n => n);

        return string.Join(" ", names);
    }

    public static string NattrFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return "0";
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        return (obj != null ? obj.Attributes.Count : 0).ToString();
    }

    public static string ObjIdFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        return $"{obj.Id}:{obj.CreationTime.ToUnixTimeSeconds()}";
    }

    public static string TestLockFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return "0";
        var targetRef = ParseDbref(args[0], ctx);
        string lockName = args[1].Trim();
        var whoRef = args.Length > 2 ? ParseDbref(args[2], ctx) : ctx.Enactor;

        var targetObj = reg.Database.GetObject(targetRef);
        if (targetObj == null) return "0";

        return targetObj.EvalLock(lockName, whoRef, reg.Database) ? "1" : "0";
    }
}
