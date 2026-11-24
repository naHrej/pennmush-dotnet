namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;

public static class RegexFunctions
{
    public static readonly TimeSpan DefaultRegexTimeout = TimeSpan.FromMilliseconds(100);

    public static void Register(FunctionRegistry registry)
    {
        registry.Register("REGMATCH", RegMatch);
        registry.Register("REGMATCHI", RegMatchI);
        registry.Register("REGEDIT", (ctx, args) => RegEditCore(registry, ctx, args, RegexOptions.None, replaceAll: false), noParse: true);
        registry.Register("REGEDITI", (ctx, args) => RegEditCore(registry, ctx, args, RegexOptions.IgnoreCase, replaceAll: false), noParse: true);
        registry.Register("REGEDITALL", (ctx, args) => RegEditCore(registry, ctx, args, RegexOptions.None, replaceAll: true), noParse: true);
        registry.Register("REGEDITALLI", (ctx, args) => RegEditCore(registry, ctx, args, RegexOptions.IgnoreCase, replaceAll: true), noParse: true);
        registry.Register("REGSUB", (ctx, args) => RegEditCore(registry, ctx, args, RegexOptions.None, replaceAll: false), noParse: true);
        registry.Register("REGRAB", RegGrab);
        registry.Register("REGRABI", RegGrabI);
        registry.Register("REGRABALL", RegGrabAll);
        registry.Register("REGRABALLI", RegGrabAllI);
        registry.Register("REGREP", (ctx, args) => RegRepImpl(registry, ctx, args, RegexOptions.None));
        registry.Register("REGREPI", (ctx, args) => RegRepImpl(registry, ctx, args, RegexOptions.IgnoreCase));
        registry.Register("REGLMATCH", RegLMatch);
        registry.Register("REGLMATCHI", RegLMatchI);
        registry.Register("REGLMATCHALL", RegLMatchAll);
        registry.Register("REGLMATCHALLI", RegLMatchAllI);
        registry.Register("ISREGEXP", IsRegexp);
    }

    public static string RegMatch(EvaluationContext ctx, string[] args)
    {
        return DoRegMatch(ctx, args, RegexOptions.None);
    }

    public static string RegMatchI(EvaluationContext ctx, string[] args)
    {
        return DoRegMatch(ctx, args, RegexOptions.IgnoreCase);
    }

    private static string DoRegMatch(EvaluationContext ctx, string[] args, RegexOptions options)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        string input = args[0];
        string pattern = args[1];

        var specs = new List<(string? GroupKey, int PositionalIdx, string? RegName)>();
        if (args.Length >= 3)
        {
            var tokens = string.Join(" ", args.Skip(2))
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tokens.Length; i++)
            {
                string tok = tokens[i];
                if (tok == "-")
                {
                    specs.Add((null, i, null));
                    continue;
                }
                int colon = tok.IndexOf(':');
                if (colon > 0 && colon < tok.Length - 1)
                {
                    string key = tok.Substring(0, colon);
                    string reg = tok.Substring(colon + 1).TrimStart('%', 'q', 'Q');
                    specs.Add((key, i, reg));
                }
                else
                {
                    string reg = tok.TrimStart('%', 'q', 'Q');
                    specs.Add((null, i, reg));
                }
            }
        }

        // Initialize all listed registers to "" even if match fails
        foreach (var spec in specs)
        {
            if (spec.RegName != null)
                ctx.SetRegister(spec.RegName, string.Empty);
        }

        try
        {
            var match = Regex.Match(input, pattern, options, DefaultRegexTimeout);
            if (!match.Success) return "0";

            foreach (var spec in specs)
            {
                if (spec.RegName == null) continue;
                string value = string.Empty;
                if (spec.GroupKey != null)
                {
                    if (int.TryParse(spec.GroupKey, out int gIdx))
                    {
                        if (gIdx >= 0 && gIdx < match.Groups.Count)
                            value = match.Groups[gIdx].Value;
                    }
                    else if (match.Groups[spec.GroupKey].Success)
                    {
                        value = match.Groups[spec.GroupKey].Value;
                    }
                }
                else if (spec.PositionalIdx < match.Groups.Count)
                {
                    value = match.Groups[spec.PositionalIdx].Value;
                }
                ctx.SetRegister(spec.RegName, value);
            }

            return "1";
        }
        catch (RegexMatchTimeoutException)
        {
            return MushError.RegexTimeout.Text();
        }
        catch (ArgumentException ex)
        {
            return MushError.RegexpError.Text("error", ex.Message);
        }
    }

    public static string RegEdit(EvaluationContext ctx, string[] args)
        => DoRegEdit(null, ctx, args, RegexOptions.None, replaceAll: false, evalArgs: false);

    public static string RegEditI(EvaluationContext ctx, string[] args)
        => DoRegEdit(null, ctx, args, RegexOptions.IgnoreCase, replaceAll: false, evalArgs: false);

    public static string RegEditAll(EvaluationContext ctx, string[] args)
        => DoRegEdit(null, ctx, args, RegexOptions.None, replaceAll: true, evalArgs: false);

    public static string RegEditAllI(EvaluationContext ctx, string[] args)
        => DoRegEdit(null, ctx, args, RegexOptions.IgnoreCase, replaceAll: true, evalArgs: false);

    public static string RegEditCore(FunctionRegistry reg, EvaluationContext ctx, string[] args, RegexOptions options, bool replaceAll)
        => DoRegEdit(reg, ctx, args, options, replaceAll, evalArgs: true);

    private static string DoRegEdit(FunctionRegistry? reg, EvaluationContext ctx, string[] args, RegexOptions options, bool replaceAll, bool evalArgs)
    {
        if (args.Length < 3) return args.Length > 0 ? (evalArgs && reg?.Evaluator != null ? reg.Evaluator.Evaluate(args[0], ctx) : args[0]) : string.Empty;
        string current = evalArgs && reg?.Evaluator != null ? reg.Evaluator.Evaluate(args[0], ctx) : args[0];

        try
        {
            for (int i = 1; i + 1 < args.Length; i += 2)
            {
                string pat = evalArgs && reg?.Evaluator != null ? reg.Evaluator.Evaluate(args[i], ctx) : args[i];
                string replExpr = args[i + 1];
                var regex = new Regex(pat, options, DefaultRegexTimeout);

                string EvalMatch(Match m)
                {
                    if (!evalArgs || reg?.Evaluator == null)
                        return replExpr;
                    var savedRx = ctx.RegexCaptures;
                    ctx.RegexCaptures = EvaluationContext.BuildRegexCaptures(m);
                    try
                    {
                        return reg.Evaluator.Evaluate(replExpr, ctx);
                    }
                    finally
                    {
                        ctx.RegexCaptures = savedRx;
                    }
                }

                current = replaceAll
                    ? regex.Replace(current, EvalMatch)
                    : regex.Replace(current, EvalMatch, 1);
            }
            return current;
        }
        catch (RegexMatchTimeoutException)
        {
            return MushError.RegexTimeout.Text();
        }
        catch (ArgumentException ex)
        {
            return MushError.RegexpError.Text("error", ex.Message);
        }
    }

    public static string RegGrab(EvaluationContext ctx, string[] args)
        => DoRegrab(args, RegexOptions.None, findAll: false, returnIndices: false);

    public static string RegGrabI(EvaluationContext ctx, string[] args)
        => DoRegrab(args, RegexOptions.IgnoreCase, findAll: false, returnIndices: false);

    public static string RegGrabAll(EvaluationContext ctx, string[] args)
        => DoRegrab(args, RegexOptions.None, findAll: true, returnIndices: false);

    public static string RegGrabAllI(EvaluationContext ctx, string[] args)
        => DoRegrab(args, RegexOptions.IgnoreCase, findAll: true, returnIndices: false);

    public static string RegLMatch(EvaluationContext ctx, string[] args)
        => DoRegrab(args, RegexOptions.None, findAll: false, returnIndices: true);

    public static string RegLMatchI(EvaluationContext ctx, string[] args)
        => DoRegrab(args, RegexOptions.IgnoreCase, findAll: false, returnIndices: true);

    public static string RegLMatchAll(EvaluationContext ctx, string[] args)
        => DoRegrab(args, RegexOptions.None, findAll: true, returnIndices: true);

    public static string RegLMatchAllI(EvaluationContext ctx, string[] args)
        => DoRegrab(args, RegexOptions.IgnoreCase, findAll: true, returnIndices: true);

    private static string DoRegrab(string[] args, RegexOptions options, bool findAll, bool returnIndices)
    {
        if (args.Length < 2) return string.Empty;
        if (args.Length > 2 && args[2].Length > 1)
            return MushError.SeparatorMustBeOneCharacter.Text();

        string delim = args.Length > 2 && args[2].Length == 1 ? args[2] : " ";
        string osep = delim;
        if (findAll && args.Length > 3)
        {
            if (args[3].Length > 1)
                return MushError.SeparatorMustBeOneCharacter.Text();
            if (args[3].Length == 1)
                osep = args[3];
        }

        var list = ListFunctions.SplitList(args[0], delim);
        string pattern = args[1];

        try
        {
            var regex = new Regex(pattern, options, DefaultRegexTimeout);
            var matches = new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                if (!regex.IsMatch(list[i]))
                    continue;

                string value = returnIndices ? (i + 1).ToString() : list[i];
                if (!findAll)
                    return value;
                matches.Add(value);
            }
            return string.Join(osep, matches);
        }
        catch (RegexMatchTimeoutException)
        {
            return MushError.RegexTimeout.Text();
        }
        catch (ArgumentException ex)
        {
            return $"#-1 REGEXP ERROR: {ex.Message}";
        }
    }

    public static string RegRep(FunctionRegistry reg, EvaluationContext ctx, string[] args)
        => RegRepImpl(reg, ctx, args, RegexOptions.None);

    public static string RegRepI(FunctionRegistry reg, EvaluationContext ctx, string[] args)
        => RegRepImpl(reg, ctx, args, RegexOptions.IgnoreCase);

    private static string RegRepImpl(FunctionRegistry reg, EvaluationContext ctx, string[] args, RegexOptions options)
    {
        if (args.Length < 3 || reg.Database == null)
            return MushError.NoMatch.Text();

        var obj = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx))
            ?? reg.Database.MatchObject(ctx.Executor, args[0]);
        if (obj == null)
            return MushError.NoMatch.Text();

        string attrPattern = args[1].Trim();
        if (string.IsNullOrEmpty(attrPattern))
            return MushError.NoSuchAttribute.Text();

        string regexPattern = args[2];
        if (string.IsNullOrEmpty(regexPattern))
            return string.Empty;

        try
        {
            var regex = new Regex(regexPattern, options, DefaultRegexTimeout);
            var matches = obj.Attributes
                .Where(a => PatternMatcher.Match(attrPattern, a.Name, out _) && regex.IsMatch(a.Value))
                .Select(a => a.Name)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
            return string.Join(" ", matches);
        }
        catch (RegexMatchTimeoutException)
        {
            return MushError.RegexTimeout.Text();
        }
        catch (ArgumentException ex)
        {
            return $"#-1 REGEXP ERROR: {ex.Message}";
        }
    }

    public static string IsRegexp(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        try
        {
            _ = new Regex(args[0], RegexOptions.None, DefaultRegexTimeout);
            return "1";
        }
        catch
        {
            return "0";
        }
    }
}
