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
        registry.Register("REGEDIT", RegEdit);
        registry.Register("REGEDITI", RegEditI);
        registry.Register("REGEDITALL", RegEditAll);
        registry.Register("REGEDITALLI", RegEditAllI);
        registry.Register("REGSUB", RegEdit);
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
        if (args.Length < 2) return "0";
        string input = args[0];
        string pattern = args[1];

        try
        {
            var match = Regex.Match(input, pattern, options, DefaultRegexTimeout);
            if (!match.Success) return "0";

            // If register names are provided in args[2..], store group captures
            for (int i = 2; i < args.Length; i++)
            {
                int groupIndex = i - 2;
                if (groupIndex < match.Groups.Count)
                {
                    string regName = args[i].Trim().TrimStart('%', 'q', 'Q');
                    string captureValue = match.Groups[groupIndex].Value;
                    ctx.SetRegister(regName, captureValue);
                }
            }

            return "1";
        }
        catch (RegexMatchTimeoutException)
        {
            return MushError.RegexTimeout.Text();
        }
        catch
        {
            return "0";
        }
    }

    public static string RegEdit(EvaluationContext ctx, string[] args)
        => DoRegEdit(args, RegexOptions.None, replaceAll: false);

    public static string RegEditI(EvaluationContext ctx, string[] args)
        => DoRegEdit(args, RegexOptions.IgnoreCase, replaceAll: false);

    public static string RegEditAll(EvaluationContext ctx, string[] args)
        => DoRegEdit(args, RegexOptions.None, replaceAll: true);

    public static string RegEditAllI(EvaluationContext ctx, string[] args)
        => DoRegEdit(args, RegexOptions.IgnoreCase, replaceAll: true);

    private static string DoRegEdit(string[] args, RegexOptions options, bool replaceAll)
    {
        if (args.Length < 3) return args.Length > 0 ? args[0] : string.Empty;
        string current = args[0];

        try
        {
            for (int i = 1; i + 1 < args.Length; i += 2)
            {
                var regex = new Regex(args[i], options, DefaultRegexTimeout);
                string replacement = args[i + 1];
                current = replaceAll
                    ? regex.Replace(current, replacement)
                    : regex.Replace(current, replacement, 1);
            }
            return current;
        }
        catch (RegexMatchTimeoutException)
        {
            return MushError.RegexTimeout.Text();
        }
        catch
        {
            return current;
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
        catch
        {
            return string.Empty;
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
        catch
        {
            return string.Empty;
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
