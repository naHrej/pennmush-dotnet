namespace PennMUSH.Core.MUSHcode;

using System;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public delegate string MUSHFunction(EvaluationContext context, string[] args);

public class FunctionRegistry
{
    private readonly Dictionary<string, MUSHFunction> _functions = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _noParseFunctions = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _sideEffectFunctions = new(StringComparer.OrdinalIgnoreCase);

    public ObjectDatabase? Database { get; }
    private ExpressionEvaluator? _evaluator;
    public ExpressionEvaluator? Evaluator
    {
        get => _evaluator;
        set
        {
            _evaluator = value;
            if (_evaluator != null)
            {
                _evaluator.IsKnownFunction = HasFunction;
                _evaluator.IsNoParseFunction = IsNoParse;
            }
        }
    }
    public Action<PennMUSH.Core.Commands.Notification>? NotificationDispatcher { get; set; }
    public PennMUSH.Core.Protocol.EditorSessionManager? EditorSessions { get; set; }
    public PennMUSH.Core.Scheduling.CronScheduler? Scheduler { get; set; }
    public PennMUSH.Core.Queue.ActionQueue? Queue { get; set; }
    public PennMUSH.Core.Communication.Channels.ChannelRegistry? Channels { get; set; }
    public PennMUSH.Core.Communication.Mail.MailStore? Mail { get; set; }
    public PennMUSH.Core.Communication.Mail.MailAliasTable? MailAliases { get; set; }
    public IPlayerPasswords? Passwords { get; set; }
    public Dbref PlayerStart { get; set; } = new(0);
    public Dbref DefaultHome { get; set; } = new(0);
    public int StartingMoney { get; set; } = 100;
    public string MudName { get; set; } = "PennMUSH";
    public string MudUrl { get; set; } = string.Empty;
    public Func<IEnumerable<string>>? CommandNamesProvider { get; set; }
    public DateTimeOffset ServerStartTime { get; set; } = DateTimeOffset.UtcNow;
    public int RestartCount { get; set; } = 0;
    public string PollQuestion { get; set; } = string.Empty;
    public Func<IReadOnlyList<Functions.ConnectionSnapshot>>? ConnectionsProvider { get; set; }
    public Func<string, string?>? ConfigProvider { get; set; }
    public Func<Dbref, string, bool>? PasswordVerifier { get; set; }
    public Func<string, IEnumerable<string>>? TextEntriesProvider { get; set; }
    public Func<string, string, string?>? TextFileEntryProvider { get; set; }
    public SqlDatabase? SqlDatabase { get; set; }
    public bool SideEffectsEnabled { get; set; } = true;

    public FunctionRegistry(ObjectDatabase? database = null)
    {
        Database = database;
        RegisterBuiltins();
    }

    public void Register(string name, MUSHFunction function, bool noParse = false, bool sideEffect = false)
    {
        _functions[name] = function;
        if (noParse)
            _noParseFunctions.Add(name);
        if (sideEffect)
            _sideEffectFunctions.Add(name);
    }

    public bool Unregister(string name)
    {
        _noParseFunctions.Remove(name);
        return _functions.Remove(name);
    }

    public bool AddAlias(string original, string alias)
    {
        if (_functions.TryGetValue(original, out var func))
        {
            _functions[alias] = func;
            if (_noParseFunctions.Contains(original))
                _noParseFunctions.Add(alias);
            return true;
        }
        return false;
    }

    private readonly Dictionary<string, SoftFunction> _soft = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _softAliases = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FunctionGate> _builtinGates = new(StringComparer.OrdinalIgnoreCase);

    public bool HasFunction(string name) => _functions.ContainsKey(name) || _soft.ContainsKey(name) || _softAliases.ContainsKey(name);
    public bool HasBuiltin(string name) => _functions.ContainsKey(name);
    public bool IsNoParse(string functionName)
    {
        if (_soft.TryGetValue(functionName, out var soft))
            return soft.Gate.Restrictions.Contains("NoParse");
        string canonical = _softAliases.TryGetValue(functionName, out var aliased) ? aliased : functionName;
        return _noParseFunctions.Contains(canonical)
            || (_builtinGates.TryGetValue(canonical, out var gate) && gate.Restrictions.Contains("NoParse"));
    }
    public IEnumerable<string> Names => _functions.Keys;

    public SoftFunction? FindSoft(string name)
        => _soft.TryGetValue(name, out var function) ? function : null;

    public IReadOnlyList<SoftFunction> ListSoft()
        => _soft.Values.OrderBy(function => function.Thing.Value).ThenBy(function => function.Name, StringComparer.Ordinal).ToList();

    public void SaveSoft(SoftFunction function) => _soft[function.Name] = function;

    public bool RemoveSoft(string name) => _soft.Remove(name);

    public bool TryAlias(string name, out string canonical)
        => _softAliases.TryGetValue(name, out canonical!);

    public void SaveAlias(string alias, string canonical) => _softAliases[alias] = canonical;

    public bool RemoveAlias(string alias) => _softAliases.Remove(alias);

    public FunctionGate BuiltinGate(string name)
    {
        if (!_builtinGates.TryGetValue(name, out var gate))
        {
            gate = new FunctionGate();
            _builtinGates[name] = gate;
        }
        return gate;
    }

    public string Dispatch(string functionName, EvaluationContext context, string[] args)
    {
        if (_soft.TryGetValue(functionName, out var soft))
            return InvokeSoft(soft, context, args);

        string canonical = functionName;
        if (_softAliases.TryGetValue(functionName, out var aliased))
            canonical = aliased;

        return DispatchBuiltin(canonical, context, args);
    }

    public string DispatchBuiltin(string functionName, EvaluationContext context, string[] args)
    {
        if (_sideEffectFunctions.Contains(functionName) && (!SideEffectsEnabled || context.NoSideFX))
            return MushError.PermissionDenied.Text();

        if (_builtinGates.TryGetValue(functionName, out var gate))
        {
            string denied = gate.Deny(Database, context);
            if (denied.Length > 0)
                return denied;
        }

        if (_functions.TryGetValue(functionName, out var func))
            return func(context, args);

        return MushError.FunctionNotFound.Text("function", functionName.ToUpperInvariant());
    }

    public bool IsBuiltinNoParse(string name)
        => _noParseFunctions.Contains(name);

    public string InvokeSoft(SoftFunction function, EvaluationContext context, string[] args)
    {
        string denied = function.Gate.Deny(Database, context);
        if (denied.Length > 0)
            return denied;

        int count = args.Length;
        if (function.MinArgs == 0 && count == 1 && string.IsNullOrEmpty(args[0]))
            count = 0;
        if (count < function.MinArgs || count > function.MaxArgs)
            return FunctionRestrictions.Expects(function.Name, function.MinArgs, function.MaxArgs, count);

        if (Database == null)
            return MushError.NoSuchAttribute.Text();
        var obj = Database.GetObject(function.Thing);
        var attr = obj?.GetAttribute(function.Attribute, Database);
        if (obj == null || attr == null)
            return MushError.NoSuchAttribute.Text();

        if (Evaluator == null)
            return attr.Value;

        var child = context.CreateChildContext(obj.Id, context.Executor, args);
        if (!child.PushCall(obj.Id, function.Attribute))
            return MushError.RecursionCycleDetected.Text();
        try
        {
            return Evaluator.Evaluate(attr.Value, child);
        }
        finally
        {
            child.PopCall(obj.Id, function.Attribute);
        }
    }

    private void RegisterBuiltins()
    {
        Functions.MathFunctions.Register(this);
        Functions.StringFunctions.Register(this);
        Functions.ListFunctions.Register(this);
        Functions.RegexFunctions.Register(this);
        Functions.TimeFunctions.Register(this);
        Functions.LogicFunctions.Register(this);
        Functions.DbFunctions.Register(this);
        Functions.AsyncFunctions.Register(this);
        Functions.CronFunctions.Register(this);
        Functions.ClientFunctions.Register(this);
        Functions.JsonFunctions.Register(this);
        Functions.CryptoFunctions.Register(this);
        Functions.ChannelFunctions.Register(this);
        Functions.MailFunctions.Register(this);
        Functions.Section9And18Functions.Register(this);
        Functions.ConnectionAndZoneFunctions.Register(this);
        Functions.TextFormatFunctions.Register(this);
        Functions.LateSubsystemFunctions.Register(this);
    }
}

public sealed class SoftFunction
{
    public string Name { get; set; } = "";
    public Dbref Thing { get; set; }
    public string Attribute { get; set; } = "";
    public int MinArgs { get; set; }
    public int MaxArgs { get; set; } = 10;
    public FunctionGate Gate { get; set; } = new();
}

public sealed class FunctionGate
{
    public bool Disabled { get; set; }
    public HashSet<string> Restrictions { get; } = new(StringComparer.OrdinalIgnoreCase);

    public FunctionGate Copy()
    {
        var copy = new FunctionGate { Disabled = Disabled };
        foreach (string restriction in Restrictions)
            copy.Restrictions.Add(restriction);
        return copy;
    }

    public bool Matches(FunctionGate other)
        => Disabled == other.Disabled && Restrictions.SetEquals(other.Restrictions);

    public string Deny(ObjectDatabase? db, EvaluationContext context)
    {
        if (Disabled)
            return MushError.FunctionDisabled.Text();

        MUSHObject? player = db?.GetObject(context.Executor);
        bool wizard = player != null && player.IsWizard;
        bool god = player != null && player.IsGod;
        bool privileged = player != null && player.IsRoyalty;
        if (Restrictions.Contains("God") && !god)
            return MushError.PermissionDenied.Text();
        if (Restrictions.Contains("Wizard") && !wizard)
            return MushError.PermissionDenied.Text();
        if (Restrictions.Contains("Admin") && !privileged)
            return MushError.PermissionDenied.Text();
        if (Restrictions.Contains("NoGagged") && player != null && player.HasFlag("GAGGED"))
            return MushError.PermissionDenied.Text();
        if (Restrictions.Contains("NoFixed") && player != null && player.HasFlag("FIXED"))
            return MushError.PermissionDenied.Text();
        if (Restrictions.Contains("NoGuest") && player != null && player.HasFlag("GUEST"))
            return MushError.PermissionDenied.Text();
        if (Restrictions.Contains("NoSideFX"))
            return MushError.PermissionDenied.Text();
        return "";
    }
}

public static class FunctionRestrictions
{
    public static readonly string[] ReportOrder =
    {
        "NoGagged", "NoFixed", "NoGuest", "Admin", "Wizard", "God",
        "NoSideFX", "LogArgs", "LogName", "NoParse", "Localize", "Userfn",
        "StripAnsi", "Literal", "Deprecated"
    };

    private static readonly HashSet<string> Known = new(ReportOrder, StringComparer.OrdinalIgnoreCase) { "Nobody" };

    public static bool Apply(FunctionGate gate, string text)
    {
        FunctionGate before = gate.Copy();
        foreach (string raw in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            bool clear = raw.StartsWith('!');
            string name = clear ? raw[1..] : raw;
            if (!Known.Contains(name))
                continue;
            if (name.Equals("Nobody", StringComparison.OrdinalIgnoreCase))
            {
                gate.Disabled = !clear;
                continue;
            }

            string canonical = ReportOrder.First(item => item.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (clear)
                gate.Restrictions.Remove(canonical);
            else
                gate.Restrictions.Add(canonical);
        }
        return !gate.Matches(before);
    }

    public static string Flags(FunctionGate gate)
    {
        var names = ReportOrder.Where(gate.Restrictions.Contains);
        return string.Join(", ", names);
    }

    public static string Expects(string name, int min, int max, int got)
    {
        string range = min == max
            ? min.ToString()
            : min + 1 == max
                ? $"{min} OR {max}"
                : $"BETWEEN {min} AND {max}";
        return MushError.FunctionExpects.Text(("function", name), ("range", range), ("got", got.ToString()));
    }
}
