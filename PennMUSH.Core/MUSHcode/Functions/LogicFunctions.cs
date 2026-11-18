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
    }

    public static bool IsTruthy(string? s) => MushBoolean.IsTrue(s);

    private static string Eval(FunctionRegistry registry, EvaluationContext ctx, string raw)
        => registry.Evaluator != null ? registry.Evaluator.Evaluate(raw, ctx) : raw;

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

    private static bool WildOrRangeMatch(string pat, string target)
    {
        if (string.IsNullOrEmpty(pat) && string.IsNullOrEmpty(target))
            return true;

        if (pat.Length > 1 && (pat[0] == '<' || pat[0] == '>')
            && double.TryParse(pat.AsSpan(1), NumberStyles.Float, CultureInfo.InvariantCulture, out double pNum)
            && double.TryParse(target, NumberStyles.Float, CultureInfo.InvariantCulture, out double tNum))
        {
            return pat[0] == '<' ? tNum < pNum : tNum > pNum;
        }

        return PatternMatcher.Match(pat, target, out _);
    }

    private static string DoSwitch(FunctionRegistry registry, EvaluationContext ctx, string[] args, bool exact, bool findAll)
    {
        if (args.Length < 2) return string.Empty;

        string target = Eval(registry, ctx, args[0]);
        var sb = new StringBuilder();
        bool found = false;

        for (int j = 1; j < args.Length - 1; j += 2)
        {
            string pat = Eval(registry, ctx, args[j]);
            bool matched = exact
                ? string.Equals(pat, target, StringComparison.Ordinal)
                : WildOrRangeMatch(pat, target);

            if (matched)
            {
                string action = exact ? args[j + 1] : args[j + 1].Replace("#$", target);
                sb.Append(Eval(registry, ctx, action));
                found = true;
                if (!findAll) return sb.ToString();
            }
        }

        if (!found && (args.Length % 2 == 0))
        {
            string defAction = exact ? args[^1] : args[^1].Replace("#$", target);
            sb.Append(Eval(registry, ctx, defAction));
        }

        return sb.ToString();
    }

    private static string DoReSwitch(FunctionRegistry registry, EvaluationContext ctx, string[] args, bool ignoreCase, bool findAll)
    {
        if (args.Length < 2) return string.Empty;

        string target = Eval(registry, ctx, args[0]);
        var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
        var sb = new StringBuilder();
        bool found = false;

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
                sb.Append(Eval(registry, ctx, action));
                found = true;
                if (!findAll) return sb.ToString();
            }
        }

        if (!found && (args.Length % 2 == 0))
        {
            string defAction = args[^1].Replace("#$", target);
            sb.Append(Eval(registry, ctx, defAction));
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

    public static string Setq(EvaluationContext ctx, string[] args)
    {
        for (int i = 0; i + 1 < args.Length; i += 2)
        {
            ctx.SetRegister(args[i].Trim(), args[i + 1]);
        }
        return string.Empty;
    }

    public static string Setr(EvaluationContext ctx, string[] args)
    {
        Setq(ctx, args);
        return args.Length >= 2 ? args[1] : string.Empty;
    }

    public static string R(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        return ctx.GetRegister(args[0].Trim());
    }
}
