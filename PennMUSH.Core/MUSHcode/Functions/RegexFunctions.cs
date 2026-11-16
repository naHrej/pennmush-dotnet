namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PennMUSH.Core.MUSHcode;

public static class RegexFunctions
{
    public static readonly TimeSpan DefaultRegexTimeout = TimeSpan.FromMilliseconds(100);

    public static void Register(FunctionRegistry registry)
    {
        registry.Register("REGMATCH", RegMatch);
        registry.Register("REGMATCHI", RegMatchI);
        registry.Register("REGEDIT", RegEdit);
        registry.Register("REGEDITALL", RegEditAll);
        registry.Register("REGSUB", RegEdit);
        registry.Register("REGRAB", RegGrab);
        registry.Register("REGRABALL", RegGrabAll);
        registry.Register("REGREP", RegRep);
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
    {
        if (args.Length < 3) return args.Length > 0 ? args[0] : string.Empty;
        string input = args[0];
        string pattern = args[1];
        string replacement = args[2];

        try
        {
            var regex = new Regex(pattern, RegexOptions.None, DefaultRegexTimeout);
            return regex.Replace(input, replacement, 1);
        }
        catch (RegexMatchTimeoutException)
        {
            return MushError.RegexTimeout.Text();
        }
        catch
        {
            return input;
        }
    }

    public static string RegEditAll(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3) return args.Length > 0 ? args[0] : string.Empty;
        string input = args[0];
        string pattern = args[1];
        string replacement = args[2];

        try
        {
            var regex = new Regex(pattern, RegexOptions.None, DefaultRegexTimeout);
            return regex.Replace(input, replacement);
        }
        catch (RegexMatchTimeoutException)
        {
            return MushError.RegexTimeout.Text();
        }
        catch
        {
            return input;
        }
    }

    public static string RegGrab(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string delim = args.Length > 2 ? args[2] : " ";
        var list = ListFunctions.SplitList(args[0], delim);
        string pattern = args[1];

        try
        {
            var regex = new Regex(pattern, RegexOptions.None, DefaultRegexTimeout);
            foreach (var item in list)
            {
                if (regex.IsMatch(item)) return item;
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return MushError.RegexTimeout.Text();
        }
        catch
        {
            // invalid regex
        }
        return string.Empty;
    }

    public static string RegGrabAll(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string delim = args.Length > 2 ? args[2] : " ";
        var list = ListFunctions.SplitList(args[0], delim);
        string pattern = args[1];

        try
        {
            var regex = new Regex(pattern, RegexOptions.None, DefaultRegexTimeout);
            var matches = list.Where(item => regex.IsMatch(item));
            return string.Join(delim, matches);
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

    public static string RegRep(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3) return args.Length > 0 ? args[0] : string.Empty;
        string delim = args.Length > 3 ? args[3] : " ";
        var list = ListFunctions.SplitList(args[0], delim);
        string pattern = args[1];
        string replacement = args[2];

        try
        {
            var regex = new Regex(pattern, RegexOptions.None, DefaultRegexTimeout);
            var results = list.Select(item => regex.Replace(item, replacement));
            return string.Join(delim, results);
        }
        catch (RegexMatchTimeoutException)
        {
            return MushError.RegexTimeout.Text();
        }
        catch
        {
            return string.Join(delim, list);
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
