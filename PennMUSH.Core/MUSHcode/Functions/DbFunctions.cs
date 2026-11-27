namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Flags;
using PennMUSH.Core.Locks;
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
        registry.Register("NAMELIST", (ctx, args) => NameListFunc(registry, ctx, args));
        registry.Register("NAMEGRAB", (ctx, args) => NameGrabFunc(registry, ctx, args, all: false));
        registry.Register("NAMEGRABALL", (ctx, args) => NameGrabFunc(registry, ctx, args, all: true));
        registry.Register("FUNCTIONS", (ctx, args) => FunctionsFunc(registry, ctx, args));
        registry.Register("LIST", (ctx, args) => ListFunc(registry, ctx, args));
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
        registry.Register("NUM", (ctx, args) => NumFunc(registry, ctx, args));
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
        registry.Register("DEFAULT", (ctx, args) => DefaultFunc(registry, ctx, args), noParse: true);
        registry.Register("EDEFAULT", (ctx, args) => EDefaultFunc(registry, ctx, args), noParse: true);
        registry.Register("EVAL", (ctx, args) => EvalFunc(registry, ctx, args));
        registry.Register("OBJEVAL", (ctx, args) => ObjEvalFunc(registry, ctx, args), noParse: true);
        registry.Register("V", (ctx, args) => VFunc(registry, ctx, args));
        registry.Register("S", (ctx, args) => SFunc(registry, ctx, args));
        registry.Register("LATTR", (ctx, args) => LattrFunc(registry, ctx, args, includeParents: false));
        registry.Register("LATTRP", (ctx, args) => LattrFunc(registry, ctx, args, includeParents: true));
        registry.Register("NATTR", (ctx, args) => NattrFunc(registry, ctx, args, includeParents: false));
        registry.Register("NATTRP", (ctx, args) => NattrFunc(registry, ctx, args, includeParents: true));
        registry.Register("ATTRCNT", (ctx, args) => NattrFunc(registry, ctx, args, includeParents: false));
        registry.Register("XATTR", (ctx, args) => XattrFunc(registry, ctx, args, includeParents: false));
        registry.Register("XATTRP", (ctx, args) => XattrFunc(registry, ctx, args, includeParents: true));
        registry.Register("HASATTRVAL", (ctx, args) => HasAttrCore(registry, ctx, args, includeParents: false, requireValue: true));
        registry.Register("HASATTRP", (ctx, args) => HasAttrCore(registry, ctx, args, includeParents: true, requireValue: false));
        registry.Register("HASATTRPVAL", (ctx, args) => HasAttrCore(registry, ctx, args, includeParents: true, requireValue: true));
        registry.Register("OBJID", (ctx, args) => ObjIdFunc(registry, ctx, args));
        registry.Register("TESTLOCK", (ctx, args) => TestLockFunc(registry, ctx, args));
        registry.Register("PCREATE", (ctx, args) => Pcreate(registry, ctx, args));

        // Section 5: Stored object values, locks, flags/powers, walkers, stats
        registry.Register("MONEY", (ctx, args) => MoneyFunc(registry, ctx, args));
        registry.Register("MONIKER", (ctx, args) => MonikerFunc(registry, ctx, args));
        registry.Register("CNAME", (ctx, args) => MonikerFunc(registry, ctx, args));
        registry.Register("DOING", (ctx, args) => DoingFunc(registry, ctx, args));
        registry.Register("ALIAS", (ctx, args) => AliasFunc(registry, ctx, args));
        registry.Register("FULLALIAS", (ctx, args) => FullAliasFunc(registry, ctx, args));
        registry.Register("HEIGHT", (ctx, args) => ScreenDimFunc(registry, ctx, args, "SCREENHEIGHT", "24"));
        registry.Register("WIDTH", (ctx, args) => ScreenDimFunc(registry, ctx, args, "SCREENWIDTH", "78"));

        registry.Register("LOCKS", (ctx, args) => LocksFunc(registry, ctx, args, listAllIfEmpty: false));
        registry.Register("LLOCKS", (ctx, args) => LocksFunc(registry, ctx, args, listAllIfEmpty: true));
        registry.Register("LOCKFLAGS", (ctx, args) => LockFlagsFunc(registry, ctx, args, longNames: false));
        registry.Register("LLOCKFLAGS", (ctx, args) => LockFlagsFunc(registry, ctx, args, longNames: true));
        registry.Register("LOCKOWNER", (ctx, args) => LockOwnerFunc(registry, ctx, args));
        registry.Register("LOCK", (ctx, args) => LockFunc(registry, ctx, args));
        registry.Register("ELOCK", (ctx, args) => ELockFunc(registry, ctx, args));
        registry.Register("ATRLOCK", (ctx, args) => AtrLockFunc(registry, ctx, args));

        registry.Register("ANDFLAGS", (ctx, args) => CheckLetterFlagsFunc(registry, ctx, args, requireAll: true));
        registry.Register("ORFLAGS", (ctx, args) => CheckLetterFlagsFunc(registry, ctx, args, requireAll: false));
        registry.Register("ANDLFLAGS", (ctx, args) => CheckLongFlagsFunc(registry, ctx, args, requireAll: true));
        registry.Register("ORLFLAGS", (ctx, args) => CheckLongFlagsFunc(registry, ctx, args, requireAll: false));
        registry.Register("ANDLPOWERS", (ctx, args) => CheckPowersFunc(registry, ctx, args, requireAll: true));
        registry.Register("ORLPOWERS", (ctx, args) => CheckPowersFunc(registry, ctx, args, requireAll: false));

        registry.Register("CHILDREN", (ctx, args) => ChildrenFunc(registry, ctx, args, countOnly: false));
        registry.Register("NCHILDREN", (ctx, args) => ChildrenFunc(registry, ctx, args, countOnly: true));
        registry.Register("ENTRANCES", (ctx, args) => EntrancesFunc(registry, ctx, args));
        registry.Register("LOCATE", (ctx, args) => LocateFunc(registry, ctx, args));
        registry.Register("PMATCH", (ctx, args) => PMatchFunc(registry, ctx, args));
        registry.Register("PLAYER", (ctx, args) => PlayerFunc(registry, ctx, args));
        registry.Register("ROOM", (ctx, args) => RoomFunc(registry, ctx, args));
        registry.Register("EXIT", (ctx, args) => ExitFunc(registry, ctx, args));
        registry.Register("NEXT", (ctx, args) => NextFunc(registry, ctx, args));
        registry.Register("NEXTDBREF", (ctx, args) => NextDbrefFunc(registry));

        registry.Register("LCON", (ctx, args) => LconFunc(registry, ctx, args));
        registry.Register("LPLAYERS", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Player, visibleOnly: false, countOnly: false, slice: false));
        registry.Register("LTHINGS", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Thing, visibleOnly: false, countOnly: false, slice: false));
        registry.Register("NCON", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Player | ObjectType.Thing, visibleOnly: false, countOnly: true, slice: false));
        registry.Register("NEXITS", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Exit, visibleOnly: false, countOnly: true, slice: false));
        registry.Register("NPLAYERS", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Player, visibleOnly: false, countOnly: true, slice: false));
        registry.Register("NTHINGS", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Thing, visibleOnly: false, countOnly: true, slice: false));
        registry.Register("LVCON", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Player | ObjectType.Thing, visibleOnly: true, countOnly: false, slice: false));
        registry.Register("LVEXITS", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Exit, visibleOnly: true, countOnly: false, slice: false));
        registry.Register("LVPLAYERS", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Player, visibleOnly: true, countOnly: false, slice: false));
        registry.Register("LVTHINGS", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Thing, visibleOnly: true, countOnly: false, slice: false));
        registry.Register("NVCON", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Player | ObjectType.Thing, visibleOnly: true, countOnly: true, slice: false));
        registry.Register("NVEXITS", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Exit, visibleOnly: true, countOnly: true, slice: false));
        registry.Register("NVPLAYERS", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Player, visibleOnly: true, countOnly: true, slice: false));
        registry.Register("NVTHINGS", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Thing, visibleOnly: true, countOnly: true, slice: false));
        registry.Register("XCON", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Player | ObjectType.Thing, visibleOnly: false, countOnly: false, slice: true));
        registry.Register("XEXITS", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Exit, visibleOnly: false, countOnly: false, slice: true));
        registry.Register("XPLAYERS", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Player, visibleOnly: false, countOnly: false, slice: true));
        registry.Register("XTHINGS", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Thing, visibleOnly: false, countOnly: false, slice: true));
        registry.Register("XVCON", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Player | ObjectType.Thing, visibleOnly: true, countOnly: false, slice: true));
        registry.Register("XVEXITS", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Exit, visibleOnly: true, countOnly: false, slice: true));
        registry.Register("XVPLAYERS", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Player, visibleOnly: true, countOnly: false, slice: true));
        registry.Register("XVTHINGS", (ctx, args) => DbWalkFunc(registry, ctx, args, ObjectType.Thing, visibleOnly: true, countOnly: false, slice: true));

        registry.Register("FINDABLE", (ctx, args) => FindableFunc(registry, ctx, args));
        registry.Register("HIDDEN", (ctx, args) => HiddenFunc(registry, ctx, args));
        registry.Register("OBJMEM", (ctx, args) => ObjMemFunc(registry, ctx, args));
        registry.Register("PLAYERMEM", (ctx, args) => PlayerMemFunc(registry, ctx, args));
        registry.Register("LSTATS", (ctx, args) => LStatsFunc(registry, ctx, args));

        // Section 6: Attribute search and database search
        registry.Register("GREP", (ctx, args) => GrepFunc(registry, ctx, args, includeParents: false, ignoreCase: false, wildcard: false));
        registry.Register("PGREP", (ctx, args) => GrepFunc(registry, ctx, args, includeParents: true, ignoreCase: false, wildcard: false));
        registry.Register("GREPI", (ctx, args) => GrepFunc(registry, ctx, args, includeParents: false, ignoreCase: true, wildcard: false));
        registry.Register("WILDGREP", (ctx, args) => GrepFunc(registry, ctx, args, includeParents: false, ignoreCase: false, wildcard: true));
        registry.Register("WILDGREPI", (ctx, args) => GrepFunc(registry, ctx, args, includeParents: false, ignoreCase: true, wildcard: true));

        registry.Register("REGLATTR", (ctx, args) => RegLattrFunc(registry, ctx, args, includeParents: false));
        registry.Register("REGLATTRP", (ctx, args) => RegLattrFunc(registry, ctx, args, includeParents: true));
        registry.Register("REGNATTR", (ctx, args) => RegNattrFunc(registry, ctx, args, includeParents: false));
        registry.Register("REGNATTRP", (ctx, args) => RegNattrFunc(registry, ctx, args, includeParents: true));
        registry.Register("REGXATTR", (ctx, args) => RegXattrFunc(registry, ctx, args, includeParents: false));
        registry.Register("REGXATTRP", (ctx, args) => RegXattrFunc(registry, ctx, args, includeParents: true));

        registry.Register("LSEARCH", (ctx, args) => LSearchFunc(registry, ctx, args, reverse: false, countOnly: false));
        registry.Register("LSEARCHR", (ctx, args) => LSearchFunc(registry, ctx, args, reverse: true, countOnly: false));
        registry.Register("NLSEARCH", (ctx, args) => LSearchFunc(registry, ctx, args, reverse: false, countOnly: true));
        registry.Register("NSEARCH", (ctx, args) => LSearchFunc(registry, ctx, args, reverse: false, countOnly: true));
        registry.Register("LOCKFILTER", (ctx, args) => LockFilterFunc(registry, ctx, args));

        // Section 7: Type tests and server metadata
        registry.Register("VALID", (ctx, args) => ValidFunc(registry, ctx, args));
        registry.Register("ISDBREF", (ctx, args) => IsDbrefFunc(registry, args));
        registry.Register("ISINT", (ctx, args) => IsIntFunc(args));
        registry.Register("ISNUM", (ctx, args) => IsNumFunc(args));
        registry.Register("ISOBJID", (ctx, args) => IsObjIdFunc(registry, args));
        registry.Register("ISWORD", (ctx, args) => IsWordFunc(args));
        registry.Register("VERSION", (ctx, args) => "PennMUSH.NET 1.8.8p0");
        registry.Register("NUMVERSION", (ctx, args) => "1080800");
        registry.Register("MUDNAME", (ctx, args) => registry.MudName);
        registry.Register("MUDURL", (ctx, args) => registry.MudUrl);
        registry.Register("CMDS", (ctx, args) => CmdsFunc(registry, ctx, args));

        // Section 11: Ufun/eval variants
        registry.Register("ULOCAL", (ctx, args) => ULocalFunc(registry, ctx, args));
        registry.Register("UDEFAULT", (ctx, args) => UDefaultFunc(registry, ctx, args, localize: false), noParse: true);
        registry.Register("ULDEFAULT", (ctx, args) => UDefaultFunc(registry, ctx, args, localize: true), noParse: true);
        registry.Register("ULAMBDA", (ctx, args) => ULambdaFunc(registry, ctx, args));
        registry.Register("GET_EVAL", (ctx, args) => GetEvalFunc(registry, ctx, args));
        registry.Register("ZFUN", (ctx, args) => ZFunFunc(registry, ctx, args));
        registry.Register("PFUN", (ctx, args) => PFunFunc(registry, ctx, args));
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
            registry.EffectiveDefaultHome,
            registry.EffectivePlayerStart,
            registry.EffectiveStartingMoney,
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
        var obj = ResolveObj(reg, ctx, args[0]);
        return obj != null ? ShortName(obj) : MushError.NoMatch.Text();
    }

    private static string ShortName(MUSHObject obj)
    {
        if (obj.Type != ObjectType.Exit)
            return obj.Name;
        int semi = obj.Name.IndexOf(';');
        return semi >= 0 ? obj.Name.Substring(0, semi) : obj.Name;
    }

    public static string NameFormat(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        string plain = Name(reg, ctx, args);
        if (args.Length == 0 || reg.Database == null || plain == MushError.NoMatch.Text())
            return plain;

        var obj = ResolveObj(reg, ctx, args[0]);
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
        MUSHObject? obj;
        string attrName;
        if (slash == -1)
        {
            obj = reg.Database.GetObject(ctx.Executor);
            attrName = args[0].Trim();
            if (attrName.Length == 0) return MushError.NoMatch.Text();
        }
        else if (slash <= 0 || slash >= args[0].Length - 1)
        {
            return MushError.NoMatch.Text();
        }
        else
        {
            obj = ResolveObj(reg, ctx, args[0].Substring(0, slash));
            attrName = args[0].Substring(slash + 1).Trim();
        }

        if (obj == null) return MushError.NoMatch.Text();

        var looker = reg.Database.GetObject(ctx.Executor);
        var attr = looker != null
            ? AttributePermissions.GetReadableAttribute(looker, obj, attrName, reg.Database, includeParents: true)
            : obj.GetAttribute(attrName, reg.Database);
        return attr != null ? attr.Value : string.Empty;
    }

    public static string HasAttr(FunctionRegistry reg, EvaluationContext ctx, string[] args)
        => HasAttrCore(reg, ctx, args, includeParents: false, requireValue: false);

    public static string HasAttrCore(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool includeParents, bool requireValue)
    {
        if (args.Length == 0 || reg.Database == null) return "0";
        MUSHObject? obj;
        string attrName;
        if (args.Length == 1)
        {
            int slash = args[0].IndexOf('/');
            if (slash <= 0 || slash >= args[0].Length - 1) return "0";
            obj = ResolveObj(reg, ctx, args[0].Substring(0, slash));
            attrName = args[0].Substring(slash + 1).Trim();
        }
        else
        {
            obj = ResolveObj(reg, ctx, args[0]);
            attrName = args[1].Trim();
        }

        if (obj == null) return "0";

        var looker = reg.Database.GetObject(ctx.Executor);
        var attr = looker != null
            ? AttributePermissions.GetReadableAttribute(looker, obj, attrName, reg.Database, includeParents)
            : (includeParents ? obj.GetAttribute(attrName, reg.Database) : obj.GetAttribute(attrName));
        if (attr == null) return "0";
        return !requireValue || !string.IsNullOrEmpty(attr.Value) ? "1" : "0";
    }

    public static string Loc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);
        if (!AttributePermissions.CanLocate(looker, obj)) return MushError.Nothing.Text();
        return obj.Location.ToString();
    }

    public static string Owner(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        return obj != null ? obj.Owner.ToString() : MushError.NoMatch.Text();
    }

    public static string Type(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        return obj != null ? obj.Type.ToString().ToUpperInvariant() : MushError.NoMatch.Text();
    }

    public static string Ufun(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        string trimmedSpec = args[0].TrimStart();
        if (trimmedSpec.StartsWith("#lambda/", StringComparison.OrdinalIgnoreCase))
        {
            string body = trimmedSpec.Substring("#lambda/".Length);
            if (reg.Evaluator == null) return body;
            string[] lambdaArgs = args.Length > 1 ? args[1..] : Array.Empty<string>();
            var childCtx = ctx.CreateChildContext(ctx.Executor, ctx.Executor, lambdaArgs);
            return reg.Evaluator.Evaluate(body, childCtx);
        }

        int slash = args[0].IndexOf('/');
        MUSHObject? obj;
        string attrName;
        if (slash == -1)
        {
            obj = reg.Database.GetObject(ctx.Executor);
            attrName = args[0].Trim();
            if (attrName.Length == 0) return MushError.NoMatch.Text();
        }
        else if (slash <= 0 || slash >= args[0].Length - 1)
        {
            return MushError.NoMatch.Text();
        }
        else
        {
            obj = ResolveObj(reg, ctx, args[0].Substring(0, slash));
            attrName = args[0].Substring(slash + 1).Trim();
        }

        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);

        var attr = looker != null
            ? AttributePermissions.GetReadableAttribute(looker, obj, attrName, reg.Database, includeParents: true)
            : obj.GetAttribute(attrName, reg.Database);
        if (attr == null) return string.Empty;
        if (IsBlockedBySaferUfun(reg, ctx, obj, attr)) return MushError.PermissionDenied.Text();

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
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);

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
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);

        string powerQuery = args[1].Trim();
        var def = FlagTable.FindPower(powerQuery);
        return (def != null && obj.HasPower(def.Name)) ? "1" : "0";
    }

    public static string FlagsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        string raw = args[0].Trim();
        int slash = raw.IndexOf('/');
        if (slash > 0)
        {
            var target = ResolveObj(reg, ctx, raw[..slash].Trim());
            if (target == null) return MushError.NoMatch.Text();
            var attr = target.GetAttribute(raw[(slash + 1)..].Trim(), reg.Database);
            if (attr == null) return MushError.NoMatch.Text();
            return PennMUSH.Core.Commands.Handlers.ExamineCommandHandler.AttrFlagLetters(attr);
        }

        var obj = ResolveObj(reg, ctx, raw);
        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);

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

        foreach (var flagName in obj.Flags.Select(FlagTable.DisplayName).OrderBy(f => f))
        {
            var def = FlagTable.FindFlag(flagName) ?? FlagTable.FindFlagByInternalName(flagName);
            if (def != null && def.Letter != '\0')
                sb.Append(def.Letter);
        }

        return sb.ToString();
    }

    public static string LflagsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);

        return string.Join(" ", obj.Flags.Select(FlagTable.DisplayName).OrderBy(f => f));
    }

    public static string PowersFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);

        return string.Join(" ", obj.Powers.OrderBy(p => p));
    }

    public static string ConFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);

        return obj.Contents.ToString();
    }

    public static string ExitsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);

        return string.Join(" ", reg.Database.GetExits(obj.Id).Select(e => e.Id.ToString()));
    }

    public static string ParentFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);

        return obj.Parent.ToString();
    }

    public static string LparentFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);

        return string.Join(" ", obj.Parents.Select(p => p.ToString()));
    }

    public static string ZoneFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);

        return obj.Zone.ToString();
    }

    public static string LzoneFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);

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

    public static string NumFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return MushError.NoMatch.Text();
        var dbref = ParseDbref(args[0], ctx);
        if (!dbref.IsValid && reg.Database != null)
        {
            var matched = reg.Database.MatchObject(ctx.Executor, args[0].Trim());
            if (matched != null) dbref = matched.Id;
        }
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
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);

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

    public static string RoomFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);

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

    private static Dbref FindExitSourceRoom(ObjectDatabase db, Dbref exitId)
    {
        foreach (var room in db.ByType(ObjectType.Room))
        {
            if (!room.Exits.IsValid)
                continue;
            var curr = room.Exits;
            var seen = new HashSet<Dbref>();
            while (curr.IsValid && seen.Add(curr))
            {
                if (curr == exitId) return room.Id;
                curr = db.GetObject(curr)?.Next ?? Dbref.Nothing;
            }
        }
        return Dbref.Nothing;
    }

    public static string WhereFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);

        Dbref loc;
        if (obj.Type == ObjectType.Exit)
        {
            loc = obj.Home.IsValid ? obj.Home : FindExitSourceRoom(reg.Database, obj.Id);
        }
        else
        {
            loc = obj.Location;
        }
        return loc.ToString();
    }

    public static string HomeFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);

        return obj.Home.IsValid ? obj.Home.ToString() : "#0";
    }

    public static string HasTypeFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return "0";
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return "#-1 NO SUCH OBJECT VISIBLE";

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

        var looker = reg.Database.GetObject(ctx.Executor);
        var attr = looker != null
            ? AttributePermissions.GetReadableAttribute(looker, obj, attrName, reg.Database, includeParents: true)
            : obj.GetAttribute(attrName, reg.Database);
        return attr != null ? attr.Value : string.Empty;
    }

    private static (MUSHObject? Obj, AttributeEntry? Attr) ResolveObjAttr(FunctionRegistry reg, EvaluationContext ctx, string spec)
    {
        if (reg.Database == null) return (null, null);
        int slash = spec.IndexOf('/');
        MUSHObject? obj;
        string attrName;
        if (slash > 0 && slash < spec.Length - 1)
        {
            string objSpec = spec.Substring(0, slash).Trim();
            obj = reg.Database.GetObject(ParseDbref(objSpec, ctx))
                ?? reg.Database.MatchObject(ctx.Executor, objSpec);
            attrName = spec.Substring(slash + 1).Trim();
        }
        else
        {
            obj = reg.Database.GetObject(ctx.Executor);
            attrName = spec.Trim();
        }

        if (obj == null) return (null, null);
        var looker = reg.Database.GetObject(ctx.Executor);
        var attr = looker != null
            ? AttributePermissions.GetReadableAttribute(looker, obj, attrName, reg.Database, includeParents: true)
            : obj.GetAttribute(attrName, reg.Database);
        return (obj, attr);
    }

    public static string DefaultFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        string eval(string raw) => reg.Evaluator != null ? reg.Evaluator.Evaluate(raw, ctx) : raw;

        for (int i = 0; i < args.Length - 1; i++)
        {
            string spec = eval(args[i]);
            var (_, attr) = ResolveObjAttr(reg, ctx, spec);
            if (attr != null)
                return attr.Value;
        }
        return eval(args[^1]);
    }

    public static string EDefaultFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        string eval(string raw) => reg.Evaluator != null ? reg.Evaluator.Evaluate(raw, ctx) : raw;

        string spec = eval(args[0]);
        var (obj, attr) = ResolveObjAttr(reg, ctx, spec);
        if (obj != null && attr != null)
        {
            if (IsBlockedBySaferUfun(reg, ctx, obj, attr)) return MushError.PermissionDenied.Text();
            if (reg.Evaluator == null) return attr.Value;
            var childCtx = ctx.CreateChildContext(obj.Id, ctx.Executor, Array.Empty<string>());
            return reg.Evaluator.Evaluate(attr.Value, childCtx);
        }

        return eval(args[1]);
    }

    public static string EvalFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Evaluator == null) return string.Empty;
        var dbref = ParseDbref(args[0], ctx);
        string attrName = args[1].Trim();

        var obj = reg.Database?.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        var looker = reg.Database?.GetObject(ctx.Executor);
        var attr = looker != null
            ? AttributePermissions.GetReadableAttribute(looker, obj, attrName, reg.Database, includeParents: true)
            : obj.GetAttribute(attrName, reg.Database);
        if (attr == null) return string.Empty;
        if (IsBlockedBySaferUfun(reg, ctx, obj, attr)) return MushError.PermissionDenied.Text();

        var childCtx = ctx.CreateChildContext(obj.Id, ctx.Executor, Array.Empty<string>());
        return reg.Evaluator.Evaluate(attr.Value, childCtx);
    }

    private static bool IsBlockedBySaferUfun(FunctionRegistry reg, EvaluationContext ctx, MUSHObject obj, AttributeEntry attr)
    {
        if (!reg.IsSaferUfunEnabled || reg.Database == null)
            return false;
        var looker = reg.Database.GetObject(ctx.Executor);
        if (looker == null || obj.Id == looker.Id)
            return false;
        if (attr.Flags.HasFlag(AttributeFlags.Public))
            return false;
        int targetRank = obj.HasFlag("TRUST")
            ? Math.Max(AttributePermissions.PrivilegeRank(obj), AttributePermissions.PrivilegeRank(reg.Database.GetObject(obj.Owner)))
            : AttributePermissions.PrivilegeRank(obj);
        return targetRank > AttributePermissions.PrivilegeRank(looker);
    }

    public static string ObjEvalFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Evaluator == null) return string.Empty;
        string targetSpec = reg.Evaluator.Evaluate(args[0], ctx);
        var dbref = ParseDbref(targetSpec, ctx);
        var target = reg.Database?.GetObject(dbref) ?? reg.Database?.MatchObject(ctx.Executor, targetSpec);
        var actor = reg.Database?.GetObject(ctx.Executor);

        bool canControl = target != null && actor != null && AttributePermissions.Controls(actor, target);
        if (canControl && !reg.IsUnsafeEvalEnabled && AttributePermissions.PrivilegeRank(target) > AttributePermissions.PrivilegeRank(actor))
            return MushError.PermissionDenied.Text();

        Dbref evalAs = canControl ? target!.Id : ctx.Executor;

        var childCtx = ctx.CreateChildContext(evalAs, ctx.Executor, ctx.Arguments);
        return reg.Evaluator.Evaluate(args[1], childCtx);
    }

    public static string VFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string name = args[0].Trim();
        if (name.Length == 1)
        {
            switch (char.ToLowerInvariant(name[0]))
            {
                case '#': return ctx.Enactor.ToString();
                case '@': return ctx.Caller.ToString();
                case '!': return ctx.Executor.ToString();
                case 'l': return ctx.Location.ToString();
                case 'n': return reg.Database?.GetObject(ctx.Enactor)?.Name ?? string.Empty;
            }
        }
        if (int.TryParse(name, out int argIdx) && argIdx >= 0)
            return ctx.GetArgument(argIdx);

        if (reg.Database == null) return string.Empty;
        var obj = reg.Database.GetObject(ctx.Executor);
        if (obj == null) return string.Empty;

        var attr = obj.GetAttribute(name, reg.Database);
        return attr != null ? attr.Value : string.Empty;
    }

    public static string SFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (!reg.IsUnsafeEvalEnabled) return MushError.FunctionDisabled.Text();
        if (args.Length == 0 || reg.Evaluator == null) return string.Empty;
        return reg.Evaluator.Evaluate(args[0], ctx);
    }

    private static IEnumerable<string> GetMatchingReadableAttrNames(FunctionRegistry reg, EvaluationContext ctx, MUSHObject obj, string pattern, bool includeParents)
    {
        var looker = reg.Database?.GetObject(ctx.Executor);
        return AttributePermissions.EnumerateAttributes(obj, reg.Database, includeParents)
            .Where(pair => (looker == null || AttributePermissions.CanRead(looker, obj, pair.Entry, pair.Source))
                && PennMUSH.Core.Commands.Handlers.ExamineCommandHandler.MatchesAttributePattern(pair.Entry.Name, pattern))
            .Select(pair => pair.Entry.Name)
            .OrderBy(n => n, StringComparer.Ordinal);
    }

    public static string LattrFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool includeParents = false)
    {
        if (args.Length == 0 || reg.Database == null) return string.Empty;
        string raw = args[0].Trim();
        string? inlinePattern = null;
        int slash = raw.IndexOf('/');
        if (slash > 0)
        {
            inlinePattern = raw[(slash + 1)..].Trim();
            raw = raw[..slash].Trim();
        }
        var obj = ResolveObj(reg, ctx, raw);
        if (obj == null) return string.Empty;

        string pattern = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1])
            ? args[1].Trim()
            : (!string.IsNullOrEmpty(inlinePattern) ? inlinePattern : "*");
        return string.Join(" ", GetMatchingReadableAttrNames(reg, ctx, obj, pattern, includeParents));
    }

    public static string NattrFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool includeParents = false)
    {
        if (args.Length == 0 || reg.Database == null) return "0";
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return "0";
        return GetMatchingReadableAttrNames(reg, ctx, obj, "*", includeParents).Count().ToString();
    }

    public static string XattrFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool includeParents)
    {
        if (args.Length < 4 || reg.Database == null) return string.Empty;
        var dbref = ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return string.Empty;

        if (!int.TryParse(args[2].Trim(), out int start) || !int.TryParse(args[3].Trim(), out int count))
            return MushError.ArgumentMustBeInteger.Text();
        if (start < 1 || count < 1)
            return MushError.OutOfRange.Text();

        string pattern = string.IsNullOrWhiteSpace(args[1]) ? "*" : args[1].Trim();
        return string.Join(" ", GetMatchingReadableAttrNames(reg, ctx, obj, pattern, includeParents)
            .Skip(start - 1)
            .Take(count));
    }

    public static string ObjIdFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();
        var looker = reg.Database.GetObject(ctx.Executor);

        return $"{obj.Id}:{obj.CreationTime.ToUnixTimeSeconds()}";
    }

    public static string TestLockFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return "0";
        var whoRef = ParseDbref(args[1], ctx);
        var whoObj = reg.Database.GetObject(whoRef);
        if (whoObj == null) return "0";

        var key = LockParser.Parse(args[0]);
        if (key == null) return "0";
        return key.Evaluate(whoRef, ctx.Executor, reg.Database) ? "1" : "0";
    }

    public static string NameListFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return string.Empty;

        MUSHObject? errObj = null;
        string? errAttrName = null;
        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
        {
            string spec = args[1].Trim();
            int slash = spec.IndexOf('/');
            if (slash >= 0)
            {
                string objSpec = spec.Substring(0, slash).Trim();
                errAttrName = spec.Substring(slash + 1).Trim();
                errObj = reg.Database.GetObject(ParseDbref(objSpec, ctx))
                    ?? reg.Database.MatchObject(ctx.Executor, objSpec);
            }
            else
            {
                errObj = reg.Database.GetObject(ctx.Executor);
                errAttrName = spec;
            }

            if (errObj == null) return MushError.NoMatch.Text();
            if (string.IsNullOrEmpty(errAttrName) || errObj.GetAttribute(errAttrName, reg.Database) == null)
                return MushError.NoSuchAttribute.Text();
        }

        string[] tokens = args[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var results = new List<string>(tokens.Length);
        foreach (string raw in tokens)
        {
            string lookup = raw.StartsWith('*') ? raw.Substring(1) : raw;
            MUSHObject? player = lookup.Equals("me", StringComparison.OrdinalIgnoreCase)
                ? reg.Database.GetObject(ctx.Executor) is { Type: ObjectType.Player } meObj ? meObj : null
                : reg.Database.FindPlayer(lookup);

            if (player != null)
            {
                results.Add(player.Id.ToString());
            }
            else if (errObj != null && errAttrName != null)
            {
                string fallback = Ufun(reg, ctx, [$"{errObj.Id}/{errAttrName}", raw, MushError.Nothing.Text()]);
                if (!string.IsNullOrEmpty(fallback))
                    results.Add(fallback);
            }
            else
            {
                results.Add(MushError.Nothing.Text());
            }
        }

        return string.Join(" ", results);
    }

    public static string NameGrabFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool all)
    {
        if (args.Length < 2 || reg.Database == null) return string.Empty;
        if (args.Length > 2 && args[2].Length > 1)
            return MushError.SeparatorMustBeOneCharacter.Text();

        string delim = args.Length > 2 && args[2].Length == 1 ? args[2] : " ";
        string target = args[1].Trim();
        var tokens = ListFunctions.SplitList(args[0], delim);

        if (all)
        {
            var matched = new List<string>();
            foreach (string token in tokens)
            {
                var obj = reg.Database.GetObject(ParseDbref(token, ctx));
                if (obj == null) continue;

                if (string.Equals(token.Trim(), target, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(obj.Id.ToString(), target, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(obj.Name, target, StringComparison.OrdinalIgnoreCase) ||
                    MatchesWordPrefix(obj.Name, target))
                {
                    matched.Add(token);
                }
            }
            return string.Join(delim, matched);
        }

        string? exactMatch = null;
        string? prefixMatch = null;
        foreach (string token in tokens)
        {
            var obj = reg.Database.GetObject(ParseDbref(token, ctx));
            if (obj == null) continue;

            if (string.Equals(token.Trim(), target, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(obj.Id.ToString(), target, StringComparison.OrdinalIgnoreCase))
            {
                return token;
            }

            if (exactMatch == null && string.Equals(obj.Name, target, StringComparison.OrdinalIgnoreCase))
                exactMatch = token;
            else if (prefixMatch == null && MatchesWordPrefix(obj.Name, target))
                prefixMatch = token;
        }

        return exactMatch ?? prefixMatch ?? string.Empty;
    }

    private static bool MatchesWordPrefix(string objName, string target)
    {
        if (string.IsNullOrEmpty(target)) return false;
        if (objName.StartsWith(target, StringComparison.OrdinalIgnoreCase))
            return true;

        var words = objName.Split([' ', ';'], StringSplitOptions.RemoveEmptyEntries);
        foreach (string word in words)
        {
            if (word.StartsWith(target, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public static string FunctionsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        string which = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
            ? args[0].Trim().ToLowerInvariant()
            : "all";

        if (which == "builtin")
        {
            return string.Join(" ", reg.Names
                .Select(n => n.ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
        }

        if (which == "local")
        {
            return string.Join(" ", reg.ListSoft()
                .Select(f => f.Name.ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
        }

        if (which == "all")
        {
            return string.Join(" ", reg.Names
                .Concat(reg.ListSoft().Select(f => f.Name))
                .Select(n => n.ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
        }

        return MushError.Nothing.Text();
    }

    public static string ListFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            return MushError.Nothing.Text();

        string type = args[0].Trim().ToLowerInvariant();
        string which = args.Length > 1 ? args[1].Trim().ToLowerInvariant() : string.Empty;

        if (args.Length == 1 && type is "builtin" or "local" or "all")
            return FunctionsFunc(reg, ctx, [type]);

        if (type.StartsWith('@') && "@functions".StartsWith(type, StringComparison.Ordinal))
            return FunctionsFunc(reg, ctx, ["local"]);

        if ("functions".StartsWith(type, StringComparison.Ordinal))
            return FunctionsFunc(reg, ctx, [string.IsNullOrEmpty(which) ? "all" : which]);

        if ("attribs".StartsWith(type, StringComparison.Ordinal) ||
            "attributes".StartsWith(type, StringComparison.Ordinal))
        {
            return string.Join(" ", StandardAttributes.CanonicalNames()
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
        }

        if ("flags".StartsWith(type, StringComparison.Ordinal))
        {
            return string.Join(" ", FlagTable.Definitions
                .Select(d => d.Name)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
        }

        if ("powers".StartsWith(type, StringComparison.Ordinal))
        {
            return string.Join(" ", FlagTable.PowerNames
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
        }

        if ("locks".StartsWith(type, StringComparison.Ordinal))
        {
            return string.Join(" ", LockType.Names);
        }

        return MushError.Nothing.Text();
    }

    private static MUSHObject? ResolveObj(FunctionRegistry reg, EvaluationContext ctx, string spec)
    {
        if (reg.Database == null || string.IsNullOrWhiteSpace(spec)) return null;
        return reg.Database.GetObject(ParseDbref(spec, ctx))
            ?? reg.Database.MatchObject(ctx.Executor, spec);
    }

    private static (MUSHObject? Obj, string LockTypeName) ResolveLockSpec(FunctionRegistry reg, EvaluationContext ctx, string spec)
    {
        int slash = spec.IndexOf('/');
        string objSpec = slash >= 0 ? spec.Substring(0, slash).Trim() : spec.Trim();
        string lockName = slash >= 0 && slash < spec.Length - 1 ? spec.Substring(slash + 1).Trim() : LockType.Basic;
        return (ResolveObj(reg, ctx, objSpec), lockName);
    }

    private static bool CanReadLock(MUSHObject? looker, MUSHObject target, LockEntry entry)
    {
        if (looker == null) return true;
        if (AttributePermissions.CanSeeAll(looker) || AttributePermissions.Controls(looker, target))
            return true;
        if (entry.Flags.HasFlag(LockFlags.Private) || entry.Flags.HasFlag(LockFlags.Wizard))
            return false;
        return entry.Flags.HasFlag(LockFlags.Visual) || target.HasFlag("VISUAL");
    }

    public static string MoneyFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var obj = args.Length > 0 ? ResolveObj(reg, ctx, args[0]) : null;
        return obj != null ? obj.Pennies.ToString() : MushError.NoMatch.Text();
    }

    public static string MonikerFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var obj = args.Length > 0 ? ResolveObj(reg, ctx, args[0]) : null;
        if (obj == null) return MushError.NoMatch.Text();
        var attr = obj.GetAttribute("MONIKER");
        return attr != null && !string.IsNullOrEmpty(attr.Value) ? attr.Value : obj.Name;
    }

    public static string DoingFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var obj = args.Length > 0 ? ResolveObj(reg, ctx, args[0]) : null;
        if (obj == null) return MushError.NoMatch.Text();
        var attr = obj.GetAttribute("DOING");
        return attr != null ? attr.Value : string.Empty;
    }

    public static string AliasFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();

        if (args.Length >= 2)
        {
            var actor = reg.Database.GetObject(ctx.Executor);
            if (actor == null || !AttributePermissions.Controls(actor, obj))
                return MushError.PermissionDenied.Text();
            var res = AttributeWriter.Apply(actor, obj, "ALIAS", args[1]);
            return res.Ok ? string.Empty : MushError.PermissionDenied.Text();
        }

        if (obj.Type == ObjectType.Exit)
        {
            int semi = obj.Name.IndexOf(';');
            if (semi < 0 || semi >= obj.Name.Length - 1) return string.Empty;
            int nextSemi = obj.Name.IndexOf(';', semi + 1);
            return nextSemi > semi ? obj.Name.Substring(semi + 1, nextSemi - semi - 1) : obj.Name.Substring(semi + 1);
        }

        var attr = obj.GetAttribute("ALIAS");
        return attr != null ? attr.Value : string.Empty;
    }

    public static string FullAliasFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();

        if (obj.Type == ObjectType.Exit)
        {
            int semi = obj.Name.IndexOf(';');
            return semi >= 0 && semi < obj.Name.Length - 1 ? obj.Name.Substring(semi + 1) : string.Empty;
        }

        var attr = obj.GetAttribute("ALIAS");
        return attr != null ? attr.Value : string.Empty;
    }

    public static string ScreenDimFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, string attrName, string defaultValue)
    {
        if (args.Length == 0 || reg.Database == null) return defaultValue;
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj != null)
        {
            var attr = obj.GetAttribute(attrName, reg.Database);
            if (attr != null && int.TryParse(attr.Value.Trim(), out int val) && val > 0)
                return val.ToString();
        }
        return args.Length > 1 && int.TryParse(args[1].Trim(), out int fb) && fb > 0 ? fb.ToString() : defaultValue;
    }

    public static string LocksFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool listAllIfEmpty)
    {
        if (args.Length == 0)
        {
            return listAllIfEmpty
                ? "Basic Enter Use Zone Page Teleport Speech Listen Command Parent Link Leave Drop Give From Receive Pay Chzone Zone Follow"
                : MushError.NoMatch.Text();
        }

        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();

        return string.Join(" ", obj.Locks.Select(l => l.Type).OrderBy(t => t, StringComparer.OrdinalIgnoreCase));
    }

    public static string LockFlagsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool longNames)
    {
        if (args.Length == 0)
            return longNames ? "owner visual private wizard locked noclone" : "ovpwln";

        if (reg.Database == null) return MushError.NoSuchLock.Text();
        var (obj, lockType) = ResolveLockSpec(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();

        var entry = obj.GetLock(lockType);
        var looker = reg.Database.GetObject(ctx.Executor);
        if (entry == null || !CanReadLock(looker, obj, entry))
            return MushError.NoSuchLock.Text();

        if (!longNames)
        {
            var sb = new StringBuilder();
            if (entry.Flags.HasFlag(LockFlags.Owner)) sb.Append('o');
            if (entry.Flags.HasFlag(LockFlags.Visual)) sb.Append('v');
            if (entry.Flags.HasFlag(LockFlags.Private)) sb.Append('p');
            if (entry.Flags.HasFlag(LockFlags.Wizard)) sb.Append('w');
            if (entry.Flags.HasFlag(LockFlags.Locked)) sb.Append('l');
            if (entry.Flags.HasFlag(LockFlags.NoClone)) sb.Append('n');
            return sb.ToString();
        }

        var names = new List<string>();
        if (entry.Flags.HasFlag(LockFlags.Owner)) names.Add("owner");
        if (entry.Flags.HasFlag(LockFlags.Visual)) names.Add("visual");
        if (entry.Flags.HasFlag(LockFlags.Private)) names.Add("private");
        if (entry.Flags.HasFlag(LockFlags.Wizard)) names.Add("wizard");
        if (entry.Flags.HasFlag(LockFlags.Locked)) names.Add("locked");
        if (entry.Flags.HasFlag(LockFlags.NoClone)) names.Add("noclone");
        return string.Join(" ", names);
    }

    public static string LockOwnerFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoSuchLock.Text();
        var (obj, lockType) = ResolveLockSpec(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();

        var entry = obj.GetLock(lockType);
        var looker = reg.Database.GetObject(ctx.Executor);
        if (entry == null || !CanReadLock(looker, obj, entry))
            return MushError.NoSuchLock.Text();
        return entry.Creator.ToString();
    }

    public static string LockFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.Nothing.Text();
        var (obj, lockType) = ResolveLockSpec(reg, ctx, args[0]);
        if (obj == null) return "0";

        var looker = reg.Database.GetObject(ctx.Executor);
        if (args.Length >= 2)
        {
            if (looker == null || !AttributePermissions.Controls(looker, obj))
                return MushError.PermissionDenied.Text();
            if (string.IsNullOrWhiteSpace(args[1]))
                obj.ClearLock(lockType);
            else
                obj.SetLock(lockType, LockParser.Parse(args[1]), looker.Id);
        }

        var entry = obj.GetLock(lockType);
        if (entry == null) return string.Empty;
        if (!CanReadLock(looker, obj, entry)) return MushError.Nothing.Text();
        return entry.Key.Unparse();
    }

    public static string ELockFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return MushError.Nothing.Text();
        var (obj, lockType) = ResolveLockSpec(reg, ctx, args[0]);
        var victim = ResolveObj(reg, ctx, args[1]);
        if (obj == null || victim == null) return MushError.Nothing.Text();

        var entry = obj.GetLock(lockType);
        var looker = reg.Database.GetObject(ctx.Executor);
        if (entry != null && !CanReadLock(looker, obj, entry))
            return MushError.Nothing.Text();

        return obj.EvalLock(lockType, victim.Id, reg.Database) ? "1" : "0";
    }

    public static string AtrLockFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.Nothing.Text();
        int slash = args[0].IndexOf('/');
        if (slash <= 0 || slash >= args[0].Length - 1) return MushError.Nothing.Text();

        var obj = ResolveObj(reg, ctx, args[0].Substring(0, slash));
        string attrName = args[0].Substring(slash + 1).Trim();
        if (obj == null) return "0";

        var looker = reg.Database.GetObject(ctx.Executor);
        var attr = looker != null
            ? AttributePermissions.GetReadableAttribute(looker, obj, attrName, reg.Database, includeParents: false)
            : obj.GetAttribute(attrName);
        if (attr == null) return MushError.Nothing.Text();

        if (args.Length == 1)
            return attr.Flags.HasFlag(AttributeFlags.Locked) ? "1" : "0";

        if (looker == null || !AttributePermissions.Controls(looker, obj))
            return MushError.PermissionDenied.Text();

        string state = args[1].Trim().ToLowerInvariant();
        if (state == "on")
        {
            obj.AddAttributeFlags(attr.Name, AttributeFlags.Locked);
            obj.SetAttributeCreator(attr.Name, looker.Owner.IsValid ? looker.Owner : looker.Id);
            return string.Empty;
        }
        if (state == "off")
        {
            obj.RemoveAttributeFlags(attr.Name, AttributeFlags.Locked);
            return string.Empty;
        }
        return MushError.Nothing.Text();
    }

    private static bool MatchTypeOrFlagLetter(MUSHObject obj, char letter)
    {
        return letter switch
        {
            'P' when obj.Type == ObjectType.Player => true,
            'R' when obj.Type == ObjectType.Room => true,
            'T' when obj.Type == ObjectType.Thing => true,
            'E' when obj.Type == ObjectType.Exit => true,
            _ => FlagTable.FindFlagByLetter(letter, obj.Type) is { } def && obj.HasFlag(def.Name)
        };
    }

    public static string CheckLetterFlagsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool requireAll)
    {
        if (args.Length < 2 || reg.Database == null) return "0";
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return "0";

        string spec = args[1].Trim();
        if (string.IsNullOrEmpty(spec)) return "0";
        if (spec.EndsWith('!')) return "#-1 INVALID FLAG";

        bool negateNext = false;
        bool anyChecked = false;
        foreach (char c in spec)
        {
            if (c == '!')
            {
                negateNext = !negateNext;
                continue;
            }
            anyChecked = true;
            bool has = c == 'c'
                ? reg.Database.IsConnected(obj.Id)
                : MatchTypeOrFlagLetter(obj, c);
            bool ok = negateNext ? !has : has;
            negateNext = false;

            if (requireAll && !ok) return "0";
            if (!requireAll && ok) return "1";
        }
        return requireAll && anyChecked ? "1" : "0";
    }

    public static string CheckLongFlagsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool requireAll)
    {
        if (args.Length < 2 || reg.Database == null) return "0";
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return "0";

        var tokens = args[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return "0";

        foreach (string token in tokens)
        {
            bool neg = token.StartsWith('!');
            string name = neg ? token.Substring(1) : token;
            if (string.IsNullOrEmpty(name))
                return "#-1 INVALID FLAG";
        }

        foreach (string token in tokens)
        {
            bool neg = token.StartsWith('!');
            string name = neg ? token.Substring(1) : token;
            bool has = name.ToUpperInvariant() switch
            {
                "PLAYER" => obj.Type == ObjectType.Player,
                "ROOM" => obj.Type == ObjectType.Room,
                "THING" => obj.Type == ObjectType.Thing,
                "EXIT" => obj.Type == ObjectType.Exit,
                "CONNECTED" => reg.Database.IsConnected(obj.Id),
                _ => obj.HasFlag(name)
            };
            bool ok = neg ? !has : has;
            if (requireAll && !ok) return "0";
            if (!requireAll && ok) return "1";
        }
        return requireAll ? "1" : "0";
    }

    public static string CheckPowersFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool requireAll)
    {
        if (args.Length < 2 || reg.Database == null) return "0";
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return "0";

        var tokens = args[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return "0";

        foreach (string token in tokens)
        {
            bool neg = token.StartsWith('!');
            string name = neg ? token.Substring(1) : token;
            bool has = obj.HasPower(name);
            bool ok = neg ? !has : has;
            if (requireAll && !ok) return "0";
            if (!requireAll && ok) return "1";
        }
        return requireAll ? "1" : "0";
    }

    public static string ChildrenFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool countOnly)
    {
        if (args.Length == 0 || reg.Database == null) return countOnly ? "0" : string.Empty;
        var parentObj = ResolveObj(reg, ctx, args[0]);
        if (parentObj == null) return countOnly ? "0" : MushError.NoMatch.Text();

        var children = reg.Database.ChildrenOf(parentObj.Id).ToList();

        return countOnly
            ? children.Count.ToString()
            : string.Join(" ", children.Select(c => c.Id.ToString()));
    }

    public static string EntrancesFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.Database == null) return string.Empty;
        var target = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
            ? ResolveObj(reg, ctx, args[0])
            : reg.Database.GetObject(ctx.Location);
        if (target == null) return MushError.NoMatch.Text();

        string typeSpec = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]) ? args[1].Trim().ToUpperInvariant() : "A";
        ObjectType filter = 0;
        foreach (char ch in typeSpec)
        {
            filter |= ch switch
            {
                'E' => ObjectType.Exit,
                'P' => ObjectType.Player,
                'R' => ObjectType.Room,
                'T' => ObjectType.Thing,
                'A' => ObjectType.Player | ObjectType.Room | ObjectType.Thing | ObjectType.Exit,
                _ => (ObjectType)0
            };
        }
        if (filter == 0)
            filter = ObjectType.Player | ObjectType.Room | ObjectType.Thing | ObjectType.Exit;

        int low = args.Length > 2 && int.TryParse(args[2].Trim(), out int l) && l >= 0 ? l : 0;
        int high = args.Length > 3 && int.TryParse(args[3].Trim(), out int h) && h >= 0 ? h : int.MaxValue;

        var matches = reg.Database.GetAllObjects()
            .Where(o => o.Type != ObjectType.Garbage
                && o.Id.Value >= low && o.Id.Value <= high
                && (filter & o.Type) != 0
                && (o.Type == ObjectType.Exit ? o.Location == target.Id : o.Home == target.Id))
            .OrderBy(o => o.Id.Value)
            .Select(o => o.Id.ToString());

        return string.Join(" ", matches);
    }

    public static string LocateFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3 || reg.Database == null) return MushError.Nothing.Text();
        var looker = ResolveObj(reg, ctx, args[0]);
        if (looker == null) return MushError.Nothing.Text();

        string what = args[1].Trim();
        string flags = args[2];
        if (string.IsNullOrEmpty(what)) return MushError.Nothing.Text();

        ObjectType typeFilter = ObjectType.None;
        bool checkAll = flags.Contains('*');
        bool allowAbs = checkAll || flags.Contains('a');
        bool allowMe = checkAll || flags.Contains('m');
        bool allowHere = checkAll || flags.Contains('h');
        bool allowInv = checkAll || flags.Contains('i');
        bool allowNeighbor = checkAll || flags.Contains('n');
        bool allowExit = checkAll || flags.Contains('e') || flags.Contains('c');
        bool allowPlayer = checkAll || flags.Contains('p') || flags.Contains('y');
        bool exact = flags.Contains('x');
        bool ambigOk = flags.Contains('X');

        if (flags.Contains('P')) typeFilter |= ObjectType.Player;
        if (flags.Contains('R')) typeFilter |= ObjectType.Room;
        if (flags.Contains('T')) typeFilter |= ObjectType.Thing;
        if (flags.Contains('E')) typeFilter |= ObjectType.Exit;
        if (typeFilter == ObjectType.None || flags.Contains('N'))
            typeFilter = ObjectType.Player | ObjectType.Room | ObjectType.Thing | ObjectType.Exit;

        bool MatchesType(MUSHObject o) => o.Type != ObjectType.Garbage && (typeFilter & o.Type) != 0;

        if (allowMe && what.Equals("me", StringComparison.OrdinalIgnoreCase))
            return MatchesType(looker) ? looker.Id.ToString() : MushError.Nothing.Text();

        if (allowHere && what.Equals("here", StringComparison.OrdinalIgnoreCase))
        {
            var hereObj = reg.Database.GetObject(looker.Location);
            return hereObj != null && MatchesType(hereObj) ? hereObj.Id.ToString() : MushError.Nothing.Text();
        }

        if (allowAbs && what.StartsWith('#'))
        {
            var absObj = reg.Database.GetObject(ParseDbref(what, ctx));
            if (absObj != null && MatchesType(absObj))
                return absObj.Id.ToString();
        }

        var candidates = new List<MUSHObject>();
        void AddRange(IEnumerable<MUSHObject> objs)
        {
            foreach (var o in objs)
            {
                if (MatchesType(o) && !candidates.Any(c => c.Id == o.Id))
                    candidates.Add(o);
            }
        }

        if (allowInv) AddRange(reg.Database.GetContents(looker.Id));
        if (allowNeighbor && looker.Location.IsValid) AddRange(reg.Database.GetContents(looker.Location));
        if (allowExit && looker.Location.IsValid) AddRange(reg.Database.GetExits(looker.Location));
        if (allowPlayer)
        {
            string pSearch = what.StartsWith('*') ? what.Substring(1) : what;
            var p = reg.Database.FindPlayer(pSearch);
            if (p != null && MatchesType(p) && !candidates.Any(c => c.Id == p.Id))
                candidates.Add(p);
        }

        var exactMatches = new List<MUSHObject>();
        var prefixMatches = new List<MUSHObject>();
        foreach (var cand in candidates)
        {
            if (cand.Type == ObjectType.Exit)
            {
                var parts = cand.Name.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Any(p => p.Equals(what, StringComparison.OrdinalIgnoreCase)))
                    exactMatches.Add(cand);
                else if (!exact && parts.Any(p => p.StartsWith(what, StringComparison.OrdinalIgnoreCase)))
                    prefixMatches.Add(cand);
            }
            else
            {
                if (cand.Name.Equals(what, StringComparison.OrdinalIgnoreCase))
                    exactMatches.Add(cand);
                else if (!exact && cand.Name.StartsWith(what, StringComparison.OrdinalIgnoreCase))
                    prefixMatches.Add(cand);
            }
        }

        var hits = exactMatches.Count > 0 ? exactMatches : prefixMatches;
        if (hits.Count == 0) return MushError.Nothing.Text();
        if (hits.Count > 1 && !ambigOk) return "#-2";
        return ambigOk ? hits[^1].Id.ToString() : hits[0].Id.ToString();
    }

    public static string PMatchFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null || string.IsNullOrWhiteSpace(args[0]))
            return MushError.Nothing.Text();

        string q = args[0].Trim();
        if (q.StartsWith('*')) q = q.Substring(1);
        if (q.Equals("me", StringComparison.OrdinalIgnoreCase))
        {
            var me = reg.Database.GetObject(ctx.Executor);
            return me != null && me.Type == ObjectType.Player ? me.Id.ToString() : MushError.Nothing.Text();
        }
        if (q.StartsWith('#'))
        {
            var p = reg.Database.GetObject(ParseDbref(q, ctx));
            return p != null && p.Type == ObjectType.Player ? p.Id.ToString() : MushError.Nothing.Text();
        }

        var exact = reg.Database.FindPlayer(q);
        if (exact != null) return exact.Id.ToString();

        var prefixes = reg.Database.GetAllObjects()
            .Where(o => o.Type == ObjectType.Player && o.Name.StartsWith(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (prefixes.Count == 1) return prefixes[0].Id.ToString();
        if (prefixes.Count > 1) return "#-2";
        return MushError.Nothing.Text();
    }

    public static string PlayerFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.ConnectionsProvider == null) return MushError.Nothing.Text();
        if (!int.TryParse(args[0].Trim(), out int port)) return MushError.ArgumentMustBeInteger.Text();
        var snap = reg.ConnectionsProvider().FirstOrDefault(s => s.Descriptor == port);
        return snap != null ? snap.PlayerId.ToString() : MushError.Nothing.Text();
    }

    public static string ExitFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.Nothing.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null || !obj.Exits.IsValid) return MushError.Nothing.Text();
        return obj.Exits.ToString();
    }

    public static string NextFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.Nothing.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null || !obj.Next.IsValid) return MushError.Nothing.Text();
        return obj.Next.ToString();
    }

    public static string NextDbrefFunc(FunctionRegistry reg)
        => reg.Database != null ? reg.Database.Top.ToString() : MushError.Nothing.Text();

    public static string LconFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return string.Empty;
        if (args.Length == 1)
            return DbWalkFunc(reg, ctx, args, ObjectType.Player | ObjectType.Thing, visibleOnly: false, countOnly: false, slice: false);

        var loc = ResolveObj(reg, ctx, args[0]);
        if (loc == null) return string.Empty;

        string mode = args[1].Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(mode))
            return DbWalkFunc(reg, ctx, [args[0]], ObjectType.Player | ObjectType.Thing, visibleOnly: false, countOnly: false, slice: false);

        IEnumerable<MUSHObject> contents = reg.Database.GetContents(loc.Id);
        if ("player".StartsWith(mode, StringComparison.Ordinal))
            contents = contents.Where(o => o.Type == ObjectType.Player);
        else if ("object".StartsWith(mode, StringComparison.Ordinal) || "thing".StartsWith(mode, StringComparison.Ordinal))
            contents = contents.Where(o => o.Type == ObjectType.Thing);
        else if ("connect".StartsWith(mode, StringComparison.Ordinal))
            contents = contents.Where(o => o.Type == ObjectType.Player && reg.Database.IsConnected(o.Id));
        else if ("puppet".StartsWith(mode, StringComparison.Ordinal))
            contents = contents.Where(o => o.Type == ObjectType.Thing && o.HasFlag("PUPPET"));
        else if ("listen".StartsWith(mode, StringComparison.Ordinal))
            contents = contents.Where(o => (o.Type == ObjectType.Thing || o.Type == ObjectType.Player)
                && (o.HasFlag("MONITOR") || o.HasAttribute("LISTEN")));
        else
            return MushError.Nothing.Text();

        return string.Join(" ", contents.Select(o => o.Id.ToString()));
    }

    public static string DbWalkFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, ObjectType typeMask, bool visibleOnly, bool countOnly, bool slice)
    {
        if (args.Length == 0 || reg.Database == null) return countOnly ? "0" : string.Empty;
        int start = 1, count = int.MaxValue;
        if (slice)
        {
            if (args.Length < 3 || !int.TryParse(args[1].Trim(), out start) || !int.TryParse(args[2].Trim(), out count))
                return MushError.ArgumentMustBeInteger.Text();
            if (start < 1 || count < 1)
                return MushError.OutOfRange.Text();
        }

        var loc = ResolveObj(reg, ctx, args[0]);
        if (loc == null) return countOnly ? "0" : string.Empty;

        var looker = reg.Database.GetObject(ctx.Executor);
        if (looker != null && !AttributePermissions.CanExamine(looker, loc) && looker.Location != loc.Id && ctx.Enactor != loc.Id)
            return countOnly ? "0" : MushError.Nothing.Text();
        IEnumerable<MUSHObject> items = typeMask == ObjectType.Exit
            ? reg.Database.GetExits(loc.Id)
            : reg.Database.GetContents(loc.Id).Where(o => (typeMask & o.Type) != 0);

        if (visibleOnly && looker != null && !AttributePermissions.CanSeeAll(looker))
            items = items.Where(o => !o.HasFlag("DARK") || AttributePermissions.Controls(looker, o));

        if (countOnly)
            return items.Count().ToString();

        if (slice)
            items = items.Skip(start - 1).Take(count);

        return string.Join(" ", items.Select(o => o.Id.ToString()));
    }

    public static string FindableFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return "0";
        var looker = ResolveObj(reg, ctx, args[0]);
        var victim = ResolveObj(reg, ctx, args[1]);
        if (looker == null || victim == null) return "0";

        if (AttributePermissions.CanSeeAll(looker) || AttributePermissions.Controls(looker, victim))
            return "1";
        if (victim.HasFlag("UNFINDABLE"))
            return "0";
        var loc = reg.Database.GetObject(victim.Location);
        if (loc != null && loc.HasFlag("UNFINDABLE"))
            return "0";
        return "1";
    }

    public static string HiddenFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.Nothing.Text();
        var target = ResolveObj(reg, ctx, args[0]);
        var looker = reg.Database.GetObject(ctx.Executor);
        if (target == null || target.Type != ObjectType.Player) return MushError.Nothing.Text();

        if (looker == null || !AttributePermissions.CanSeeAll(looker))
            return MushError.Nothing.Text();
        return target.HasFlag("DARK") ? "1" : "0";
    }

    private static int EstimateObjectBytes(MUSHObject obj)
    {
        int size = 64 + obj.Name.Length;
        foreach (var attr in obj.Attributes)
            size += 32 + attr.Name.Length + attr.Value.Length;
        return size;
    }

    public static string ObjMemFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.Nothing.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        return obj != null ? EstimateObjectBytes(obj).ToString() : MushError.NoMatch.Text();
    }

    public static string PlayerMemFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.Nothing.Text();
        var player = ResolveObj(reg, ctx, args[0]);
        if (player == null) return MushError.NoMatch.Text();

        long total = reg.Database.ByOwner(player.Id).Sum(o => (long)EstimateObjectBytes(o));
        return total.ToString();
    }

    public static string LStatsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.Database == null) return "0 0 0 0 0";
        string whoArg = args.Length > 0 ? args[0].Trim() : "all";
        bool all = string.IsNullOrEmpty(whoArg) || whoArg.Equals("all", StringComparison.OrdinalIgnoreCase);

        Dbref ownerFilter = Dbref.Nothing;
        if (!all)
        {
            var target = ResolveObj(reg, ctx, whoArg);
            if (target == null) return MushError.NoMatch.Text();
            ownerFilter = target.Id;
        }

        int rooms = 0, exits = 0, things = 0, players = 0, garbage = 0;
        var counted = all
            ? Enum.GetValues<ObjectType>().Where(type => type is ObjectType.Room or ObjectType.Exit or ObjectType.Thing or ObjectType.Player or ObjectType.Garbage)
                .SelectMany(type => reg.Database.ByType(type))
            : reg.Database.ByOwner(ownerFilter);
        foreach (var obj in counted)
        {
            switch (obj.Type)
            {
                case ObjectType.Room: rooms++; break;
                case ObjectType.Exit: exits++; break;
                case ObjectType.Thing: things++; break;
                case ObjectType.Player: players++; break;
                case ObjectType.Garbage: garbage++; break;
            }
        }

        int total = rooms + exits + things + players + (all ? garbage : 0);
        return all
            ? $"{total} {rooms} {exits} {things} {players} {garbage}"
            : $"{total} {rooms} {exits} {things} {players}";
    }

    public static string GrepFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool includeParents, bool ignoreCase, bool wildcard)
    {
        if (args.Length < 3 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = ResolveObj(reg, ctx, args[0]);
        if (obj == null) return MushError.NoMatch.Text();

        string attrPattern = args[1].Trim();
        if (string.IsNullOrEmpty(attrPattern)) return MushError.NoSuchAttribute.Text();
        string needle = args[2];
        if (string.IsNullOrEmpty(needle)) return string.Empty;

        var looker = reg.Database.GetObject(ctx.Executor);
        Regex? wildRegex = null;
        if (wildcard)
        {
            string rePat = "^" + Regex.Escape(needle).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
            wildRegex = new Regex(rePat, (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None) | RegexOptions.Singleline, RegexFunctions.DefaultRegexTimeout);
        }

        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var matches = AttributePermissions.EnumerateAttributes(obj, reg.Database, includeParents)
            .Where(pair => (looker == null || AttributePermissions.CanRead(looker, obj, pair.Entry, pair.Source))
                && PatternMatcher.Match(attrPattern, pair.Entry.Name, out _)
                && (wildRegex != null ? wildRegex.IsMatch(pair.Entry.Value) : pair.Entry.Value.Contains(needle, comparison)))
            .Select(pair => pair.Entry.Name)
            .OrderBy(n => n, StringComparer.Ordinal);

        return string.Join(" ", matches);
    }

    private static List<string>? GetRegexMatchingReadableAttrNames(FunctionRegistry reg, EvaluationContext ctx, string objAndPattern, bool includeParents, out string? error)
    {
        error = null;
        if (reg.Database == null) return new List<string>();

        int slash = objAndPattern.IndexOf('/');
        string objSpec = slash >= 0 ? objAndPattern.Substring(0, slash).Trim() : objAndPattern.Trim();
        string pattern = slash >= 0 ? objAndPattern.Substring(slash + 1) : ".*";
        if (string.IsNullOrEmpty(pattern)) pattern = ".*";

        var obj = ResolveObj(reg, ctx, objSpec);
        if (obj == null) return new List<string>();

        Regex regex;
        try
        {
            regex = new Regex(pattern, RegexOptions.IgnoreCase, RegexFunctions.DefaultRegexTimeout);
        }
        catch (RegexMatchTimeoutException)
        {
            error = MushError.RegexTimeout.Text();
            return null;
        }
        catch
        {
            return new List<string>();
        }

        var looker = reg.Database.GetObject(ctx.Executor);
        return AttributePermissions.EnumerateAttributes(obj, reg.Database, includeParents)
            .Where(pair => (looker == null || AttributePermissions.CanRead(looker, obj, pair.Entry, pair.Source))
                && regex.IsMatch(pair.Entry.Name))
            .Select(pair => pair.Entry.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
    }

    public static string RegLattrFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool includeParents)
    {
        if (args.Length == 0 || reg.Database == null) return string.Empty;
        if (args.Length > 1 && args[1].Length > 1)
            return MushError.SeparatorMustBeOneCharacter.Text();
        string osep = args.Length > 1 && args[1].Length == 1 ? args[1] : " ";

        var names = GetRegexMatchingReadableAttrNames(reg, ctx, args[0], includeParents, out var error);
        if (names == null) return error ?? string.Empty;
        return string.Join(osep, names);
    }

    public static string RegNattrFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool includeParents)
    {
        if (args.Length == 0 || reg.Database == null) return "0";
        var names = GetRegexMatchingReadableAttrNames(reg, ctx, args[0], includeParents, out var error);
        if (names == null) return error ?? "0";
        return names.Count.ToString();
    }

    public static string RegXattrFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool includeParents)
    {
        if (args.Length < 3 || reg.Database == null) return string.Empty;
        if (!int.TryParse(args[1].Trim(), out int start) || !int.TryParse(args[2].Trim(), out int count))
            return MushError.ArgumentMustBeInteger.Text();
        if (start < 1 || count < 1)
            return MushError.OutOfRange.Text();
        if (args.Length > 3 && args[3].Length > 1)
            return MushError.SeparatorMustBeOneCharacter.Text();
        string osep = args.Length > 3 && args[3].Length == 1 ? args[3] : " ";

        var names = GetRegexMatchingReadableAttrNames(reg, ctx, args[0], includeParents, out var error);
        if (names == null) return error ?? string.Empty;
        return string.Join(osep, names.Skip(start - 1).Take(count));
    }

    public static string LSearchFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool reverse, bool countOnly)
    {
        if (args.Length == 0 || reg.Database == null) return countOnly ? "0" : string.Empty;

        var actor = reg.Database.GetObject(ctx.Executor);
        if (actor == null) return countOnly ? "0" : MushError.Nothing.Text();

        string ownerToken = args[0].Trim();
        bool allOwners = string.IsNullOrEmpty(ownerToken) || ownerToken.Equals("all", StringComparison.OrdinalIgnoreCase);
        MUSHObject? owner = null;
        if (!allOwners)
        {
            owner = ownerToken.Equals("me", StringComparison.OrdinalIgnoreCase)
                ? actor
                : ResolveObj(reg, ctx, ownerToken);
            if (owner == null || owner.Type != ObjectType.Player)
                return countOnly ? "0" : MushError.Nothing.Text();
        }

        var stdCriteria = new List<(string Key, string Value)>();
        var evalCriteria = new List<(ObjectType? TypeFilter, string Expr, bool IsLock)>();
        int? minDb = null;
        int? maxDb = null;
        int? start = null;
        int? count = null;

        for (int i = 1; i + 1 < args.Length; i += 2)
        {
            string cls = args[i].Trim().ToLowerInvariant();
            string val = args[i + 1];
            if (cls == "none") continue;

            if (cls == "mindb" && int.TryParse(val.Trim(), out int mn)) { minDb = mn; continue; }
            if (cls == "maxdb" && int.TryParse(val.Trim(), out int mx)) { maxDb = mx; continue; }
            if (cls == "start" && int.TryParse(val.Trim(), out int st)) { start = st; continue; }
            if (cls == "count" && int.TryParse(val.Trim(), out int ct)) { count = ct; continue; }

            if (cls == "eval") { evalCriteria.Add((null, val, false)); continue; }
            if (cls == "eplayer") { evalCriteria.Add((ObjectType.Player, val, false)); continue; }
            if (cls == "eroom") { evalCriteria.Add((ObjectType.Room, val, false)); continue; }
            if (cls == "eexit") { evalCriteria.Add((ObjectType.Exit, val, false)); continue; }
            if (cls == "ething" || cls == "eobject") { evalCriteria.Add((ObjectType.Thing, val, false)); continue; }
            if (cls == "elock") { evalCriteria.Add((null, val, true)); continue; }

            stdCriteria.Add((cls, val));
        }

        bool seeAll = AttributePermissions.CanSeeAll(actor) || actor.HasPower("Search");
        bool playersArePublic = stdCriteria.Any(c =>
            (c.Key == "type" && c.Value.Equals("player", StringComparison.OrdinalIgnoreCase)) || c.Key == "players");

        var hits = new List<MUSHObject>();
        foreach (var obj in reg.Database.GetAllObjects().OrderBy(o => o.Id.Value))
        {
            if (obj.Type == ObjectType.Garbage) continue;
            if (minDb.HasValue && obj.Id.Value < minDb.Value) continue;
            if (maxDb.HasValue && obj.Id.Value > maxDb.Value) continue;
            if (owner != null && obj.Owner != owner.Id) continue;
            if (!seeAll && !(playersArePublic && obj.Type == ObjectType.Player) && !ListenerSearch.CanExamine(actor, obj))
                continue;
            if (!SearchCommandHandler.Matches(obj, stdCriteria))
                continue;

            bool evalOk = true;
            foreach (var (typeFilter, expr, isLock) in evalCriteria)
            {
                if (typeFilter.HasValue && obj.Type != typeFilter.Value) { evalOk = false; break; }
                if (isLock)
                {
                    var key = LockParser.Parse(expr);
                    if (!key.Evaluate(obj.Id, ctx.Executor, reg.Database)) { evalOk = false; break; }
                }
                else if (reg.Evaluator != null)
                {
                    string body = expr.Replace("##", obj.Id.ToString());
                    if (!MushBoolean.IsTrue(reg.Evaluator.Evaluate(body, ctx))) { evalOk = false; break; }
                }
            }
            if (evalOk) hits.Add(obj);
        }

        if (reverse) hits.Reverse();
        if (start.HasValue || count.HasValue)
        {
            int s = Math.Max(1, start ?? 1);
            int c = Math.Max(0, count ?? hits.Count);
            hits = hits.Skip(s - 1).Take(c).ToList();
        }

        if (countOnly) return hits.Count.ToString();
        return string.Join(" ", hits.Select(o => o.Id.ToString()));
    }

    public static string LockFilterFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return string.Empty;
        if (args.Length > 2 && args[2].Length > 1)
            return MushError.SeparatorMustBeOneCharacter.Text();
        string delim = args.Length > 2 && args[2].Length == 1 ? args[2] : " ";

        var key = LockParser.Parse(args[0]);
        var tokens = ListFunctions.SplitList(args[1], delim);
        var kept = new List<string>();
        foreach (string token in tokens)
        {
            var obj = reg.Database.GetObject(ParseDbref(token, ctx));
            if (obj != null && key.Evaluate(obj.Id, ctx.Executor, reg.Database))
                kept.Add(token);
        }

        return string.Join(delim, kept);
    }

    public static string ValidFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return "0";
        string cat = args[0].Trim().ToLowerInvariant();
        string val = args[1];

        return cat switch
        {
            "name" => !string.IsNullOrWhiteSpace(val) && !val.Contains('=') && !val.Contains('\r') && !val.Contains('\n') ? "1" : "0",
            "attrname" => AttributeEntry.IsValidName(val.Trim()) ? "1" : "0",
            "playername" => !string.IsNullOrWhiteSpace(val) && !val.Any(char.IsWhiteSpace) && !val.Contains('=') ? "1" : "0",
            "flag" => FlagTable.FindFlag(val.Trim()) != null ? "1" : "0",
            "power" => FlagTable.FindPower(val.Trim()) != null ? "1" : "0",
            "function" => !string.IsNullOrWhiteSpace(val) && val.Trim().All(c => char.IsLetterOrDigit(c) || c == '_') ? "1" : "0",
            "locktype" => !string.IsNullOrWhiteSpace(val) ? "1" : "0",
            "passwd" => !string.IsNullOrEmpty(val) ? "1" : "0",
            "qreg" => !string.IsNullOrWhiteSpace(val) && val.Trim().All(c => char.IsLetterOrDigit(c) || c == '_') ? "1" : "0",
            "colorname" => Presentation.AnsiColorTable.TryGetNamedColor(val.Trim().TrimStart('+'), out _) ? "1" : "0",
            "ansicolor" => !string.IsNullOrWhiteSpace(val) && Presentation.AnsiColorTable.TryParseAnsiSpec(val.Trim(), out _) ? "1" : "0",
            _ => "0"
        };
    }

    public static string IsDbrefFunc(FunctionRegistry reg, string[] args)
    {
        if (args.Length == 0 || !Dbref.TryParse(args[0].Trim(), out var d) || !d.IsValid)
            return "0";
        if (reg.Database == null) return "1";
        var obj = reg.Database.GetObject(d);
        return obj != null && obj.Type != ObjectType.Garbage ? "1" : "0";
    }

    public static string IsIntFunc(string[] args)
        => args.Length > 0 && long.TryParse(args[0].Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out _) ? "1" : "0";

    public static string IsNumFunc(string[] args)
        => args.Length > 0
           && double.TryParse(args[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d)
           && !double.IsNaN(d) && !double.IsInfinity(d)
            ? "1" : "0";

    public static string IsObjIdFunc(FunctionRegistry reg, string[] args)
    {
        if (args.Length == 0) return "0";
        string raw = args[0].Trim();
        int colon = raw.IndexOf(':');
        if (colon <= 0 || colon >= raw.Length - 1) return "0";
        if (!Dbref.TryParse(raw.Substring(0, colon), out var d) || !d.IsValid) return "0";
        if (!long.TryParse(raw.Substring(colon + 1), out long ts)) return "0";
        if (reg.Database == null) return "1";
        var obj = reg.Database.GetObject(d);
        return obj != null && obj.Type != ObjectType.Garbage && obj.CreationTime.ToUnixTimeSeconds() == ts ? "1" : "0";
    }

    public static string IsWordFunc(string[] args)
        => args.Length > 0 && args[0].Length > 0 && args[0].All(char.IsLetter) ? "1" : "0";

    public static string CmdsFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return MushError.TooFewArguments.Text();
        if (reg.ConnectionsProvider == null) return "-1";
        string target = args[0].Trim();
        var snaps = reg.ConnectionsProvider();
        ConnectionSnapshot? snap = int.TryParse(target, out int port)
            ? snaps.FirstOrDefault(s => s.Descriptor == port)
            : null;
        if (snap == null && reg.Database != null)
        {
            var p = reg.Database.GetObject(ParseDbref(target, ctx)) ?? reg.Database.FindPlayer(target.TrimStart('*'));
            if (p != null) snap = snaps.FirstOrDefault(s => s.PlayerId == p.Id);
        }
        return snap != null ? snap.CommandCount.ToString() : "-1";
    }

    public static string ULocalFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var saved = ctx.CloneRegisters();
        try
        {
            return Ufun(reg, ctx, args);
        }
        finally
        {
            ctx.RestoreRegisters(saved);
        }
    }

    public static string UDefaultFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, bool localize)
    {
        if (args.Length < 2 || reg.Database == null) return string.Empty;

        string eval(string raw) => reg.Evaluator != null ? reg.Evaluator.Evaluate(raw, ctx) : raw;

        string spec = eval(args[0]);
        int slash = spec.IndexOf('/');
        MUSHObject? obj = null;
        string attrName = spec.Trim();
        if (slash > 0 && slash < spec.Length - 1)
        {
            obj = reg.Database.GetObject(ParseDbref(spec.Substring(0, slash), ctx));
            attrName = spec.Substring(slash + 1).Trim();
        }
        else
        {
            obj = reg.Database.GetObject(ctx.Executor);
        }

        var looker = reg.Database.GetObject(ctx.Executor);
        var attr = obj != null
            ? (looker != null
                ? AttributePermissions.GetReadableAttribute(looker, obj, attrName, reg.Database, includeParents: true)
                : obj.GetAttribute(attrName, reg.Database))
            : null;

        if (attr == null)
            return eval(args[1]);

        var uargs = new string[Math.Max(1, args.Length - 1)];
        uargs[0] = $"{obj!.Id}/{attrName}";
        for (int i = 2; i < args.Length; i++)
            uargs[i - 1] = eval(args[i]);

        if (!localize)
            return Ufun(reg, ctx, uargs);

        var saved = ctx.CloneRegisters();
        try
        {
            return Ufun(reg, ctx, uargs);
        }
        finally
        {
            ctx.RestoreRegisters(saved);
        }
    }

    public static string ULambdaFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string first = args[0].TrimStart();
        var evalArgs = args.Skip(1).ToArray();

        if (first.StartsWith("#lambda/", StringComparison.OrdinalIgnoreCase))
        {
            string body = first.Substring("#lambda/".Length);
            if (reg.Evaluator == null) return body;
            var childCtx = ctx.CreateChildContext(ctx.Executor, ctx.Executor, evalArgs);
            return reg.Evaluator.Evaluate(body, childCtx);
        }

        return Ufun(reg, ctx, args);
    }

    public static string GetEvalFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return string.Empty;
        var (obj, attr) = ResolveObjAttr(reg, ctx, args[0]);
        if (obj == null || attr == null) return string.Empty;
        if (IsBlockedBySaferUfun(reg, ctx, obj, attr)) return MushError.PermissionDenied.Text();
        if (reg.Evaluator == null) return attr.Value;
        var childCtx = ctx.CreateChildContext(obj.Id, ctx.Executor, Array.Empty<string>());
        return reg.Evaluator.Evaluate(attr.Value, childCtx);
    }

    public static string ZFunFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return string.Empty;
        var execObj = reg.Database.GetObject(ctx.Executor);
        if (execObj == null || !execObj.Zone.IsValid) return string.Empty;

        var uargs = new string[args.Length];
        uargs[0] = $"{execObj.Zone}/{args[0].Trim()}";
        for (int i = 1; i < args.Length; i++)
            uargs[i] = args[i];
        return Ufun(reg, ctx, uargs);
    }

    public static string PFunFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return string.Empty;
        var execObj = reg.Database.GetObject(ctx.Executor);
        if (execObj == null || !execObj.Parent.IsValid) return string.Empty;

        var uargs = new string[args.Length];
        uargs[0] = $"{execObj.Parent}/{args[0].Trim()}";
        for (int i = 1; i < args.Length; i++)
            uargs[i] = args[i];
        return Ufun(reg, ctx, uargs);
    }
}
