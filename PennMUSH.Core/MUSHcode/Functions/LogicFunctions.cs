namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Queue;

public static class LogicFunctions
{
    private const double FloatEpsilon = 1e-9;

    public static void Register(FunctionRegistry registry)
    {
        registry.Register("T", T);
        registry.Register("IF", (ctx, args) => DoCond(registry, ctx, args, findTrue: true, findAll: false), noParse: true);
        registry.Register("IFELSE", (ctx, args) => DoCond(registry, ctx, args, findTrue: true, findAll: false), noParse: true);
        registry.Register("COND", (ctx, args) => DoCond(registry, ctx, args, findTrue: true, findAll: false), noParse: true);
        registry.Register("CONDALL", (ctx, args) => DoCond(registry, ctx, args, findTrue: true, findAll: true), noParse: true);
        registry.Register("NCOND", (ctx, args) => DoCond(registry, ctx, args, findTrue: false, findAll: false), noParse: true);
        registry.Register("NCONDALL", (ctx, args) => DoCond(registry, ctx, args, findTrue: false, findAll: true), noParse: true);
        registry.Register("IFEQ", Ifeq);

        registry.Register("CAND", (ctx, args) => EvalCand(registry, ctx, args, negate: false), noParse: true);
        registry.Register("NCAND", (ctx, args) => EvalCand(registry, ctx, args, negate: true), noParse: true);
        registry.Register("COR", (ctx, args) => EvalCor(registry, ctx, args, negate: false), noParse: true);
        registry.Register("NCOR", (ctx, args) => EvalCor(registry, ctx, args, negate: true), noParse: true);

        registry.Register("AND", And);
        registry.Register("OR", Or);
        registry.Register("NOT", Not);
        registry.Register("XOR", Xor);
        registry.Register("NAND", Nand);
        registry.Register("NOR", Nor);

        registry.Register("EQ", Eq);
        registry.Register("NEQ", Neq);
        registry.Register("GT", Gt);
        registry.Register("GTE", Gte);
        registry.Register("LT", Lt);
        registry.Register("LTE", Lte);

        registry.Register("SWITCH", (ctx, args) => DoSwitch(registry, ctx, args, exact: false, findAll: false), noParse: true);
        registry.Register("SWITCHALL", (ctx, args) => DoSwitch(registry, ctx, args, exact: false, findAll: true), noParse: true);
        registry.Register("CASE", (ctx, args) => DoSwitch(registry, ctx, args, exact: true, findAll: false), noParse: true);
        registry.Register("CASEALL", (ctx, args) => DoSwitch(registry, ctx, args, exact: true, findAll: true), noParse: true);
        registry.Register("RESWITCH", (ctx, args) => DoReSwitch(registry, ctx, args, ignoreCase: false, findAll: false), noParse: true);
        registry.Register("RESWITCHALL", (ctx, args) => DoReSwitch(registry, ctx, args, ignoreCase: false, findAll: true), noParse: true);
        registry.Register("RESWITCHI", (ctx, args) => DoReSwitch(registry, ctx, args, ignoreCase: true, findAll: false), noParse: true);
        registry.Register("RESWITCHALLI", (ctx, args) => DoReSwitch(registry, ctx, args, ignoreCase: true, findAll: true), noParse: true);

        registry.Register("FIRSTOF", (ctx, args) => DoFirstOf(registry, ctx, args, isBool: true), noParse: true);
        registry.Register("ALLOF", (ctx, args) => DoAllOf(registry, ctx, args, isBool: true), noParse: true);
        registry.Register("STRFIRSTOF", (ctx, args) => DoFirstOf(registry, ctx, args, isBool: false), noParse: true);
        registry.Register("STRALLOF", (ctx, args) => DoAllOf(registry, ctx, args, isBool: false), noParse: true);

        registry.Register("SETQ", Setq);
        registry.Register("SETR", Setr);
        registry.Register("R", R);

        // Section 11: Evaluation frame, registers, switch stack
        registry.Register("NULL", (ctx, args) => string.Empty);
        registry.Register("@@", (ctx, args) => string.Empty, noParse: true);
        registry.Register("LIT", Lit, noParse: true);
        registry.Register("LETQ", (ctx, args) => Letq(registry, ctx, args), noParse: true);
        registry.Register("LOCALIZE", (ctx, args) => Localize(registry, ctx, args), noParse: true);
        registry.Register("LISTQ", Listq);
        registry.Register("REGISTERS", Registers);
        registry.Register("UNSETQ", Unsetq);
        registry.Register("SLEV", Slev);
        registry.Register("STEXT", Stext);
        registry.Register("FN", (ctx, args) => Fn(registry, ctx, args), noParse: true);
    }

    public static bool IsTruthy(string? s) => MushBoolean.IsTrue(s);

    private static string Eval(FunctionRegistry registry, EvaluationContext ctx, string raw)
        => registry.Evaluator != null ? registry.Evaluator.Evaluate(raw, ctx) : raw;

    private static string EvalAction(FunctionRegistry registry, EvaluationContext ctx, string raw)
    {
        string body = raw.Length >= 2 && raw[0] == '{' && raw[^1] == '}' ? raw[1..^1] : raw;
        return registry.Evaluator != null ? registry.Evaluator.Evaluate(body, ctx) : body;
    }

    public static string T(EvaluationContext ctx, string[] args)
        => args.Length > 0 && IsTruthy(args[0]) ? "1" : "0";

    public static string If(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        return IsTruthy(args[0])
            ? (args.Length > 1 ? args[1] : string.Empty)
            : (args.Length > 2 ? args[2] : string.Empty);
    }

    public static string Ifeq(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        return string.Equals(args[0], args[1], StringComparison.OrdinalIgnoreCase)
            ? (args.Length > 2 ? args[2] : string.Empty)
            : (args.Length > 3 ? args[3] : string.Empty);
    }

    public static string Cand(EvaluationContext ctx, string[] args)
        => args.All(IsTruthy) ? "1" : "0";

    public static string Cor(EvaluationContext ctx, string[] args)
        => args.Any(IsTruthy) ? "1" : "0";

    private static string EvalCand(FunctionRegistry registry, EvaluationContext ctx, string[] args, bool negate)
    {
        foreach (string raw in args)
        {
            if (!IsTruthy(Eval(registry, ctx, raw)))
                return negate ? "1" : "0";
        }
        return negate ? "0" : "1";
    }

    private static string EvalCor(FunctionRegistry registry, EvaluationContext ctx, string[] args, bool negate)
    {
        foreach (string raw in args)
        {
            if (IsTruthy(Eval(registry, ctx, raw)))
                return negate ? "0" : "1";
        }
        return negate ? "1" : "0";
    }

    public static string And(EvaluationContext ctx, string[] args)
        => args.Length > 0 && args.All(IsTruthy) ? "1" : "0";

    public static string Or(EvaluationContext ctx, string[] args)
        => args.Any(IsTruthy) ? "1" : "0";

    public static string Not(EvaluationContext ctx, string[] args)
        => args.Length > 0 && IsTruthy(args[0]) ? "0" : "1";

    public static string Xor(EvaluationContext ctx, string[] args)
        => args.Count(IsTruthy) == 1 ? "1" : "0";

    public static string Nand(EvaluationContext ctx, string[] args)
        => args.Length == 0 || !args.All(IsTruthy) ? "1" : "0";

    public static string Nor(EvaluationContext ctx, string[] args)
        => !args.Any(IsTruthy) ? "1" : "0";

    private static bool TryParseNumbers(string[] args, out double[] nums, out string error)
    {
        if (args.Length < 2)
        {
            nums = Array.Empty<double>();
            error = MushError.ComparisonRequiresTwoOrMoreNumbers.Text();
            return false;
        }

        nums = new double[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            if (!double.TryParse(args[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out nums[i]))
            {
                error = MushError.ArgumentsMustBeNumbers.Text();
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool NearEq(double a, double b) => Math.Abs(a - b) < FloatEpsilon;

    public static string Eq(EvaluationContext ctx, string[] args)
    {
        if (!TryParseNumbers(args, out var nums, out var error)) return error;
        for (int i = 1; i < nums.Length; i++)
        {
            if (!NearEq(nums[i - 1], nums[i])) return "0";
        }
        return "1";
    }

    public static string Neq(EvaluationContext ctx, string[] args)
    {
        if (!TryParseNumbers(args, out var nums, out var error)) return error;
        for (int i = 1; i < nums.Length; i++)
        {
            if (!NearEq(nums[i - 1], nums[i])) return "1";
        }
        return "0";
    }

    public static string Gt(EvaluationContext ctx, string[] args) => ComparePairs(args, (a, b) => !NearEq(a, b) && a > b);
    public static string Gte(EvaluationContext ctx, string[] args) => ComparePairs(args, (a, b) => NearEq(a, b) || a > b);
    public static string Lt(EvaluationContext ctx, string[] args) => ComparePairs(args, (a, b) => !NearEq(a, b) && a < b);
    public static string Lte(EvaluationContext ctx, string[] args) => ComparePairs(args, (a, b) => NearEq(a, b) || a < b);

    private static string ComparePairs(string[] args, Func<double, double, bool> predicate)
    {
        if (!TryParseNumbers(args, out var nums, out var error)) return error;
        for (int i = 1; i < nums.Length; i++)
        {
            if (!predicate(nums[i - 1], nums[i])) return "0";
        }
        return "1";
    }

    private static string DoCond(FunctionRegistry registry, EvaluationContext ctx, string[] args, bool findTrue, bool findAll)
    {
        var sb = new StringBuilder();
        bool found = false;

        for (int i = 0; i < args.Length - 1; i += 2)
        {
            string condVal = Eval(registry, ctx, args[i]);
            if (IsTruthy(condVal) == findTrue)
            {
                sb.Append(Eval(registry, ctx, args[i + 1]));
                if (!findAll) return sb.ToString();
                found = true;
            }
        }

        if (!found && (args.Length % 2 == 1))
            sb.Append(Eval(registry, ctx, args[^1]));

        return sb.ToString();
    }

    private static bool WildOrRangeMatch(string pat, string target, out string[] captures)
    {
        captures = Array.Empty<string>();
        if (string.IsNullOrEmpty(pat) && string.IsNullOrEmpty(target))
            return true;

        if (pat.Length > 1 && (pat[0] == '<' || pat[0] == '>')
            && double.TryParse(pat.AsSpan(1), NumberStyles.Float, CultureInfo.InvariantCulture, out double pNum)
            && double.TryParse(target, NumberStyles.Float, CultureInfo.InvariantCulture, out double tNum))
        {
            return pat[0] == '<' ? tNum < pNum : tNum > pNum;
        }

        return PatternMatcher.Match(pat, target, out captures);
    }

    private static string DoSwitch(FunctionRegistry registry, EvaluationContext ctx, string[] args, bool exact, bool findAll)
    {
        if (args.Length < 3) return MushError.TooFewArguments.Text();

        string target = Eval(registry, ctx, args[0]);
        var sb = new StringBuilder();
        bool found = false;

        ctx.SwitchStack.Add(target);
        try
        {
            for (int j = 1; j < args.Length - 1; j += 2)
            {
                string pat = Eval(registry, ctx, args[j]);
                string[] captures = Array.Empty<string>();
                bool matched = exact
                    ? string.Equals(pat, target, StringComparison.Ordinal)
                    : WildOrRangeMatch(pat, target, out captures);

                if (matched)
                {
                    string action = exact ? args[j + 1] : args[j + 1].Replace("#$", target);
                    var savedArgs = ctx.Arguments;
                    if (!exact) ctx.Arguments = captures;
                    try
                    {
                        sb.Append(EvalAction(registry, ctx, action));
                    }
                    finally
                    {
                        ctx.Arguments = savedArgs;
                    }
                    found = true;
                    if (!findAll) return sb.ToString();
                }
            }

            if (!found && (args.Length % 2 == 0))
            {
                string defAction = exact ? args[^1] : args[^1].Replace("#$", target);
                sb.Append(Eval(registry, ctx, defAction));
            }
        }
        finally
        {
            ctx.SwitchStack.RemoveAt(ctx.SwitchStack.Count - 1);
        }

        return sb.ToString();
    }

    private static string DoReSwitch(FunctionRegistry registry, EvaluationContext ctx, string[] args, bool ignoreCase, bool findAll)
    {
        if (args.Length < 3) return MushError.TooFewArguments.Text();

        string target = Eval(registry, ctx, args[0]);
        var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
        var sb = new StringBuilder();
        bool found = false;

        ctx.SwitchStack.Add(target);
        try
        {
            for (int j = 1; j < args.Length - 1; j += 2)
            {
                string pat = Eval(registry, ctx, args[j]);
                Match match;
                try
                {
                    match = Regex.Match(target, pat, options, RegexFunctions.DefaultRegexTimeout);
                }
                catch
                {
                    continue;
                }

                if (match.Success)
                {
                    string action = args[j + 1].Replace("#$", target);
                    var savedRx = ctx.RegexCaptures;
                    ctx.RegexCaptures = EvaluationContext.BuildRegexCaptures(match);
                    try
                    {
                        sb.Append(EvalAction(registry, ctx, action));
                    }
                    finally
                    {
                        ctx.RegexCaptures = savedRx;
                    }
                    found = true;
                    if (!findAll) return sb.ToString();
                }
            }

            if (!found && (args.Length % 2 == 0))
            {
                string defAction = args[^1].Replace("#$", target);
                sb.Append(Eval(registry, ctx, defAction));
            }
        }
        finally
        {
            ctx.SwitchStack.RemoveAt(ctx.SwitchStack.Count - 1);
        }

        return sb.ToString();
    }

    private static string DoFirstOf(FunctionRegistry registry, EvaluationContext ctx, string[] args, bool isBool)
    {
        string last = string.Empty;
        for (int i = 0; i < args.Length; i++)
        {
            last = Eval(registry, ctx, args[i]);
            if ((isBool && IsTruthy(last)) || (!isBool && last.Length > 0))
                return last;
        }
        return last;
    }

    private static string DoAllOf(FunctionRegistry registry, EvaluationContext ctx, string[] args, bool isBool)
    {
        if (args.Length == 0) return string.Empty;
        if (args.Length == 1)
        {
            string single = Eval(registry, ctx, args[0]);
            return ((isBool && IsTruthy(single)) || (!isBool && single.Length > 0)) ? single : string.Empty;
        }

        string sep = Eval(registry, ctx, args[^1]);
        var results = new List<string>();
        for (int i = 0; i < args.Length - 1; i++)
        {
            string val = Eval(registry, ctx, args[i]);
            if ((isBool && IsTruthy(val)) || (!isBool && val.Length > 0))
                results.Add(val);
        }
        return string.Join(sep, results);
    }

    public static bool IsValidQregName(string name)
        => !string.IsNullOrEmpty(name) && name.Length <= 64 &&
           !name.Any(c => char.IsWhiteSpace(c) || char.IsControl(c) || c is '<' or '>' or '=');

    private static string SetqCore(EvaluationContext ctx, string[] args, string funcName)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        if (args.Length % 2 != 0) return MushError.FunctionExpectsEvenArguments.Text("function", funcName);

        for (int i = 0; i + 1 < args.Length; i += 2)
        {
            string regName = args[i].Trim();
            if (!IsValidQregName(regName))
                return MushError.InvalidQRegisterName.Text();
            ctx.SetRegister(regName, args[i + 1]);
        }
        return string.Empty;
    }

    public static string Setq(EvaluationContext ctx, string[] args) => SetqCore(ctx, args, "SETQ");

    public static string Setr(EvaluationContext ctx, string[] args)
    {
        string err = SetqCore(ctx, args, "SETR");
        if (err.Length > 0) return err;
        return args.Length >= 2 ? args[1] : string.Empty;
    }

    public static string R(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return MushError.TooFewArguments.Text();
        string name = args[0].Trim();
        string type = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1])
            ? args[1].Trim().ToLowerInvariant()
            : "qregisters";

        return type switch
        {
            "qregisters" => IsValidQregName(name) ? ctx.GetRegister(name) : MushError.InvalidQRegisterName.Text(),
            "regexp" => ctx.GetRegexCapture(name),
            "switch" => int.TryParse(name, out int swIdx) && swIdx >= 0 && swIdx < ctx.SwitchStack.Count
                ? ctx.SwitchStack[^(swIdx + 1)]
                : string.Empty,
            "iter" => int.TryParse(name, out int itIdx) && itIdx >= 0 && itIdx < ctx.IterStack.Count
                ? ctx.IterStack[^(itIdx + 1)].Text
                : string.Empty,
            "args" or "stack" => int.TryParse(name, out int argIdx) ? ctx.GetArgument(argIdx) : string.Empty,
            _ => MushError.Nothing.Text()
        };
    }

    public static string Lit(EvaluationContext ctx, string[] args)
        => args.Length > 0 ? args[0] : string.Empty;

    public static string Localize(FunctionRegistry registry, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        var saved = ctx.CloneRegisters();
        try
        {
            return Eval(registry, ctx, args[0]);
        }
        finally
        {
            ctx.RestoreRegisters(saved);
        }
    }

    public static string Letq(FunctionRegistry registry, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return MushError.TooFewArguments.Text();
        if (args.Length % 2 == 0) return MushError.FunctionExpectsOddArguments.Text("function", "LETQ");
        if (args.Length == 1) return Eval(registry, ctx, args[0]);

        var saved = ctx.CloneRegisters();
        try
        {
            for (int i = 0; i + 1 < args.Length - 1; i += 2)
            {
                string regName = Eval(registry, ctx, args[i]).Trim();
                if (!IsValidQregName(regName))
                    return MushError.InvalidQRegisterName.Text();
                string regVal = Eval(registry, ctx, args[i + 1]);
                ctx.SetRegister(regName, regVal);
            }
            return Eval(registry, ctx, args[^1]);
        }
        finally
        {
            ctx.RestoreRegisters(saved);
        }
    }

    public static string Listq(EvaluationContext ctx, string[] args)
    {
        string pattern = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) ? args[0].Trim() : "*";
        return string.Join(" ", ctx.Registers
            .Where(kv => !string.IsNullOrEmpty(kv.Value) && PatternMatcher.Match(pattern, kv.Key, out _))
            .Select(kv => kv.Key)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase));
    }

    public static string Registers(EvaluationContext ctx, string[] args)
    {
        string pattern = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) ? args[0].Trim() : "*";
        string typesArg = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1])
            ? args[1].Trim().ToLowerInvariant()
            : "qregisters regexp switch iter args";
        string osep = args.Length > 2 ? args[2] : " ";

        var types = new HashSet<string>(typesArg.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        var results = new List<string>();

        if (types.Contains("qregisters"))
        {
            foreach (var kv in ctx.Registers.Where(kv => !string.IsNullOrEmpty(kv.Value)))
            {
                string key = "Q" + kv.Key;
                if (PatternMatcher.Match(pattern, key, out _)) results.Add(key);
            }
        }
        if (types.Contains("regexp") && ctx.RegexCaptures != null)
        {
            foreach (var kv in ctx.RegexCaptures.Where(kv => !string.IsNullOrEmpty(kv.Value)))
            {
                string key = "R" + kv.Key;
                if (PatternMatcher.Match(pattern, key, out _)) results.Add(key);
            }
        }
        if (types.Contains("switch"))
        {
            for (int i = 0; i < ctx.SwitchStack.Count; i++)
            {
                string key = "S" + i;
                if (PatternMatcher.Match(pattern, key, out _)) results.Add(key);
            }
        }
        if (types.Contains("iter"))
        {
            for (int i = 0; i < ctx.IterStack.Count; i++)
            {
                string key = "T" + i;
                if (PatternMatcher.Match(pattern, key, out _)) results.Add(key);
            }
        }
        if (types.Contains("args") || types.Contains("stack"))
        {
            for (int i = 0; i < ctx.Arguments.Length; i++)
            {
                if (string.IsNullOrEmpty(ctx.Arguments[i])) continue;
                string key = "A" + i;
                if (PatternMatcher.Match(pattern, key, out _)) results.Add(key);
            }
        }

        return string.Join(osep, results.OrderBy(k => k, StringComparer.OrdinalIgnoreCase));
    }

    public static string Unsetq(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            ctx.Registers.Clear();
            return string.Empty;
        }

        var patterns = args[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (string key in ctx.Registers.Keys.ToList())
        {
            if (patterns.Any(p => PatternMatcher.Match(p, key, out _)))
                ctx.Registers.Remove(key);
        }
        return string.Empty;
    }

    public static string Slev(EvaluationContext ctx, string[] args)
        => (ctx.SwitchStack.Count - 1).ToString();

    public static string Stext(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return MushError.TooFewArguments.Text();
        string arg = args[0].Trim();
        int level;
        if (arg.Equals("l", StringComparison.OrdinalIgnoreCase))
        {
            level = ctx.SwitchStack.Count - 1;
        }
        else if (!int.TryParse(arg, out level))
        {
            return MushError.ArgumentMustBeInteger.Text();
        }
        if (level < 0 || level >= ctx.SwitchStack.Count)
            return MushError.OutOfRange.Text();
        return ctx.SwitchStack[^(level + 1)];
    }

    public static string Fn(FunctionRegistry registry, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return MushError.TooFewArguments.Text();
        string rawSpec = Eval(registry, ctx, args[0]).Trim();
        if (string.IsNullOrEmpty(rawSpec)) return MushError.FunctionNotFound.Text("function", string.Empty);

        var targetCtx = ctx;
        string fnName = rawSpec;
        int slash = rawSpec.IndexOf('/');
        if (slash > 0 && slash < rawSpec.Length - 1 && registry.Database != null)
        {
            string objSpec = rawSpec.Substring(0, slash).Trim();
            fnName = rawSpec.Substring(slash + 1).Trim();
            var obj = registry.Database.GetObject(DbFunctions.ParseDbref(objSpec, ctx))
                ?? registry.Database.MatchObject(ctx.Executor, objSpec);
            var actor = registry.Database.GetObject(ctx.Executor);
            if (obj == null) return MushError.NoMatch.Text();
            if (actor == null || (!PennMUSH.Core.Attributes.AttributePermissions.Controls(actor, obj) && !PennMUSH.Core.Attributes.AttributePermissions.CanSeeAll(actor)))
                return MushError.PermissionDenied.Text();
            targetCtx = ctx.CreateChildContext(obj.Id, ctx.Executor, ctx.Arguments);
        }

        bool noParse = registry.IsBuiltinNoParse(fnName);
        var callArgs = new string[args.Length - 1];
        for (int i = 1; i < args.Length; i++)
            callArgs[i - 1] = noParse ? args[i] : Eval(registry, ctx, args[i]);

        return registry.DispatchBuiltin(fnName, targetCtx, callArgs);
    }
}
