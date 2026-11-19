namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Flags;
using PennMUSH.Core.MUSHcode;

public static class LateSubsystemFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Register("QUOTA", (ctx, args) => QuotaFunc(registry, ctx, args));
        registry.Register("FOLLOWERS", (ctx, args) => AttrListFunc(registry, ctx, args, "FOLLOWERS"));
        registry.Register("FOLLOWING", (ctx, args) => AttrListFunc(registry, ctx, args, "FOLLOWING"));
        registry.Register("CONFIG", (ctx, args) => ConfigFunc(registry, args));
        registry.Register("CHECKPASS", (ctx, args) => CheckPassFunc(registry, ctx, args));
        registry.Register("SQL", (ctx, args) => "#-1 SQL IS NOT ENABLED");
        registry.Register("MAPSQL", (ctx, args) => "#-1 SQL IS NOT ENABLED");
        registry.Register("ADDRLOG", (ctx, args) => WizardLogFunc(registry, ctx));
        registry.Register("CONNLOG", (ctx, args) => WizardLogFunc(registry, ctx));
        registry.Register("CONNRECORD", (ctx, args) => WizardLogFunc(registry, ctx));
        registry.Register("BEEP", (ctx, args) => BeepFunc(registry, ctx, args));
        registry.Register("BENCHMARK", (ctx, args) => BenchmarkFunc(registry, ctx, args), noParse: true);
        registry.Register("POLL", (ctx, args) => registry.PollQuestion);
        registry.Register("SUGGEST", (ctx, args) => SuggestFunc(registry, args));
        registry.Register("TEXTENTRIES", (ctx, args) => TextEntriesFunc(registry, args));
        registry.Register("TEXTFILE", (ctx, args) => TextFileFunc(registry, args));
        registry.Register("TEXTSEARCH", (ctx, args) => TextSearchFunc(registry, args));
        registry.Register("ROOT", RootFunc);
    }

    public static string QuotaFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.Database == null) return "100";
        var obj = args.Length > 0
            ? (reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx)) ?? reg.Database.MatchObject(ctx.Executor, args[0]))
            : reg.Database.GetObject(ctx.Executor);
        if (obj == null) return MushError.NoMatch.Text();
        if (obj.Type != PennMUSH.Core.Types.ObjectType.Player) return "#-1 NOT A PLAYER";
        if (obj.IsGod || obj.IsWizard || obj.IsRoyalty || obj.HasPower("No_Quota"))
            return "99999";

        var attr = obj.GetAttribute("RQUOTA", reg.Database);
        int rquota = attr != null && int.TryParse(attr.Value.Trim(), out int q) ? q : 100;
        int owned = reg.Database.GetAllObjects().Count(o => o.Type != PennMUSH.Core.Types.ObjectType.Garbage && o.Owner == obj.Id);
        return (owned + rquota).ToString();
    }

    public static string AttrListFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args, string attrName)
    {
        if (args.Length == 0 || reg.Database == null) return string.Empty;
        var obj = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx))
            ?? reg.Database.MatchObject(ctx.Executor, args[0]);
        if (obj == null) return MushError.NoMatch.Text();
        var actor = reg.Database.GetObject(ctx.Executor);
        if (!PennMUSH.Core.Attributes.AttributePermissions.Controls(actor, obj))
            return MushError.PermissionDenied.Text();
        return obj.GetAttribute(attrName, reg.Database)?.Value ?? string.Empty;
    }

    public static string ConfigFunc(FunctionRegistry reg, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            return "mud_name mud_url starting_money";

        string key = args[0].Trim().ToLowerInvariant();
        if (reg.ConfigProvider != null)
        {
            string? val = reg.ConfigProvider(key);
            if (val != null) return val;
        }

        return key switch
        {
            "mud_name" => reg.MudName,
            "mud_url" => reg.MudUrl,
            "starting_money" => reg.StartingMoney.ToString(),
            _ => MushError.Nothing.Text()
        };
    }

    public static string CheckPassFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null) return MushError.TooFewArguments.Text();
        var actor = reg.Database.GetObject(ctx.Executor);
        if (actor == null || !actor.IsWizard) return MushError.PermissionDenied.Text();

        var target = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx))
            ?? reg.Database.FindPlayer(args[0].Trim().TrimStart('*'));
        if (target == null || target.Type != PennMUSH.Core.Types.ObjectType.Player) return "#-1 NO SUCH PLAYER";
        if (reg.PasswordVerifier == null) return "0";
        return reg.PasswordVerifier(target.Id, args[1]) ? "1" : "0";
    }

    public static string WizardLogFunc(FunctionRegistry reg, EvaluationContext ctx)
    {
        var actor = reg.Database?.GetObject(ctx.Executor);
        if (actor == null || (!actor.IsWizard && !actor.IsRoyalty))
            return MushError.PermissionDenied.Text();
        return string.Empty;
    }

    public static string BeepFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var actor = reg.Database?.GetObject(ctx.Executor);
        if (actor == null || (!actor.IsWizard && !actor.IsRoyalty))
            return MushError.PermissionDenied.Text();

        int count = 1;
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            if (!int.TryParse(args[0].Trim(), out count))
                return MushError.ArgumentMustBeInteger.Text();
            if (count <= 0 || count > 5)
                return MushError.OutOfRange.Text();
        }
        return new string('\a', count);
    }

    public static string BenchmarkFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        string countStr = reg.Evaluator != null ? reg.Evaluator.Evaluate(args[1], ctx) : args[1];
        if (!int.TryParse(countStr.Trim(), out int count) || count <= 0)
            return MushError.ArgumentMustBePositiveInteger.Text();
        count = Math.Min(count, 10000);

        double minUs = double.MaxValue, maxUs = 0, sumUs = 0;
        string lastResult = string.Empty;
        var sw = new Stopwatch();
        for (int i = 0; i < count; i++)
        {
            sw.Restart();
            if (reg.Evaluator != null)
                lastResult = reg.Evaluator.Evaluate(args[0], ctx);
            sw.Stop();
            double us = sw.Elapsed.TotalMicroseconds;
            sumUs += us;
            if (us < minUs) minUs = us;
            if (us > maxUs) maxUs = us;
        }
        double avgUs = sumUs / count;
        string report = $"Average: {Math.Round(avgUs, 2).ToString("G", CultureInfo.InvariantCulture)}   Min: {Math.Round(minUs, 2).ToString("G", CultureInfo.InvariantCulture)}   Max: {Math.Round(maxUs, 2).ToString("G", CultureInfo.InvariantCulture)}";

        if (args.Length >= 3 && !string.IsNullOrWhiteSpace(args[2]) && reg.Database != null && reg.NotificationDispatcher != null)
        {
            string sendToRaw = reg.Evaluator != null ? reg.Evaluator.Evaluate(args[2], ctx) : args[2];
            var target = reg.Database.GetObject(DbFunctions.ParseDbref(sendToRaw, ctx))
                ?? reg.Database.MatchObject(ctx.Executor, sendToRaw);
            if (target != null)
                reg.NotificationDispatcher.Invoke(new PennMUSH.Core.Commands.Notification(PennMUSH.Core.Commands.NotificationScope.Target, report, Target: target.Id, Speaker: ctx.Executor));
            return lastResult;
        }

        return report;
    }

    public static string SuggestFunc(FunctionRegistry reg, string[] args)
    {
        if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1])) return string.Empty;
        string cat = args[0].Trim().ToLowerInvariant();
        string word = args[1].Trim();
        string delim = args.Length > 2 && !string.IsNullOrEmpty(args[2]) ? args[2] : " ";
        int limit = 20;
        if (args.Length > 3 && int.TryParse(args[3].Trim(), out int lim) && lim > 0)
            limit = lim;

        IEnumerable<string> candidates = cat switch
        {
            "functions" => reg.Names,
            "attributes" or "attribs" => StandardAttributes.CanonicalNames(),
            "flags" => FlagTable.Definitions.Select(d => d.Name),
            "powers" => FlagTable.PowerNames,
            _ => Array.Empty<string>()
        };

        return string.Join(delim, candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(c => (Name: c, Dist: Levenshtein(word.ToUpperInvariant(), c.ToUpperInvariant())))
            .Where(x => x.Dist <= 2 || x.Name.StartsWith(word, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Dist)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(x => x.Name));
    }

    private static int Levenshtein(string a, string b)
    {
        int[,] d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[a.Length, b.Length];
    }

    public static string TextEntriesFunc(FunctionRegistry reg, string[] args)
    {
        if (args.Length < 2 || reg.TextEntriesProvider == null) return string.Empty;
        string sep = args.Length > 2 && !string.IsNullOrEmpty(args[2]) ? args[2] : " ";
        var entries = reg.TextEntriesProvider(args[0].Trim())
            .Where(e => PennMUSH.Core.Queue.PatternMatcher.Match(args[1].Trim(), e, out _));
        return string.Join(sep, entries);
    }

    public static string TextFileFunc(FunctionRegistry reg, string[] args)
    {
        if (args.Length < 2 || reg.TextFileEntryProvider == null) return MushError.Nothing.Text();
        return reg.TextFileEntryProvider(args[0].Trim(), args[1].Trim()) ?? MushError.Nothing.Text();
    }

    public static string TextSearchFunc(FunctionRegistry reg, string[] args)
    {
        if (args.Length < 2 || reg.TextEntriesProvider == null || reg.TextFileEntryProvider == null)
            return string.Empty;
        string sep = args.Length > 2 && !string.IsNullOrEmpty(args[2]) ? args[2] : " ";
        string file = args[0].Trim();
        string needle = args[1];
        bool isWild = needle.Contains('*') || needle.Contains('?');
        var hits = reg.TextEntriesProvider(file).Where(e =>
        {
            string body = reg.TextFileEntryProvider(file, e) ?? string.Empty;
            return isWild
                ? PennMUSH.Core.Queue.PatternMatcher.Match(needle, body, out _)
                : body.Contains(needle, StringComparison.OrdinalIgnoreCase);
        });
        return string.Join(sep, hits);
    }

    public static string RootFunc(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        if (!double.TryParse(args[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double x))
            return MushError.ArgumentMustBeNumber.Text();
        if (!int.TryParse(args[1].Trim(), out int n))
            return MushError.ArgumentMustBeInteger.Text();
        if (n < 0) return "#-1 ROOT OUT OF RANGE";
        if (n == 0) return "1";

        if (x < 0)
        {
            if (n % 2 == 0) return "#-1 IMAGINARY NUMBER";
            double res = -Math.Pow(-x, 1.0 / n);
            return Math.Abs(res - Math.Round(res)) < 1e-12 ? ((long)Math.Round(res)).ToString() : res.ToString("G", CultureInfo.InvariantCulture);
        }

        double val = Math.Pow(x, 1.0 / n);
        return Math.Abs(val - Math.Round(val)) < 1e-12 ? ((long)Math.Round(val)).ToString() : val.ToString("G", CultureInfo.InvariantCulture);
    }
}
