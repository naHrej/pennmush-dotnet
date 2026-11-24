namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Flags;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public static class LateSubsystemFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Register("QUOTA", (ctx, args) => QuotaFunc(registry, ctx, args));
        registry.Register("FOLLOWERS", (ctx, args) => AttrListFunc(registry, ctx, args, "FOLLOWERS"));
        registry.Register("FOLLOWING", (ctx, args) => AttrListFunc(registry, ctx, args, "FOLLOWING"));
        registry.Register("CONFIG", (ctx, args) => ConfigFunc(registry, args));
        registry.Register("CHECKPASS", (ctx, args) => CheckPassFunc(registry, ctx, args));
        registry.Register("SQL", (ctx, args) => SqlFunc(registry, ctx, args));
        registry.Register("MAPSQL", (ctx, args) => MapSqlFunc(registry, ctx, args));
        registry.Register("ADDRLOG", (ctx, args) => AddrLogFunc(registry, ctx, args));
        registry.Register("CONNLOG", (ctx, args) => ConnLogFunc(registry, ctx, args));
        registry.Register("CONNRECORD", (ctx, args) => ConnRecordFunc(registry, ctx, args));
        registry.Register("BEEP", (ctx, args) => BeepFunc(registry, ctx, args));
        registry.Register("BENCHMARK", (ctx, args) => BenchmarkFunc(registry, ctx, args), noParse: true);
        registry.Register("POLL", (ctx, args) => registry.PollQuestion);
        registry.Register("SUGGEST", (ctx, args) => SuggestFunc(registry, ctx, args));
        registry.Register("TEXTENTRIES", (ctx, args) => TextEntriesFunc(registry, ctx, args));
        registry.Register("TEXTFILE", (ctx, args) => TextFileFunc(registry, ctx, args));
        registry.Register("TEXTSEARCH", (ctx, args) => TextSearchFunc(registry, ctx, args));
        registry.Register("ROOT", RootFunc);
    }

    public static string QuotaFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.Database == null) return "100";
        var actor = reg.Database.GetObject(ctx.Executor);
        var obj = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
            ? (reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx)) ?? reg.Database.MatchObject(ctx.Executor, args[0]))
            : actor;
        if (obj == null) return MushError.NoMatch.Text();
        if (obj.Type != ObjectType.Player) return MushError.NotAPlayer.Text();
        if (actor != null && !QuotaRules.CanSee(actor, obj))
            return MushError.PermissionDenied.Text();
        if (!reg.IsQuotaEnabled || QuotaRules.IsUnlimited(obj, reg.Database))
            return "99999";

        int rquota = QuotaRules.PeekRemaining(reg.Database, obj, reg.EffectiveStartingQuota);
        int owned = reg.Database.ByOwner(obj.Id).Count();
        return (owned + rquota).ToString(CultureInfo.InvariantCulture);
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
            "starting_money" => reg.StartingMoney.ToString(CultureInfo.InvariantCulture),
            "use_quota" => reg.IsQuotaEnabled ? "yes" : "no",
            "starting_quota" => reg.EffectiveStartingQuota.ToString(CultureInfo.InvariantCulture),
            "player_name_len" => "16",
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
        if (target == null || target.Type != PennMUSH.Core.Types.ObjectType.Player) return MushError.NoSuchPlayer.Text();
        if (reg.PasswordVerifier != null)
            return reg.PasswordVerifier(target.Id, args[1]) ? "1" : "0";
        return PennMUSH.Core.Database.PasswordHash.Accepts(target, args[1], null) ? "1" : "0";
    }

    public static string SqlFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var actor = reg.Database?.GetObject(ctx.Executor);
        if (actor == null || (!actor.IsWizard && !actor.HasPower("Sql_Ok")))
            return MushError.PermissionDenied.Text();
        if (reg.SqlDatabase == null || !reg.SqlDatabase.IsConnected)
            return MushError.SqlIsNotEnabled.Text();
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            return MushError.TooFewArguments.Text();

        string rowSep = args.Length > 1 ? args[1] : " ";
        string colSep = args.Length > 2 ? args[2] : " ";

        var res = reg.SqlDatabase.Query(args[0]);
        if (res.Error != null) return MushError.SqlError.Text("error", res.Error);
        if (res.Fields.Count == 0) return string.Empty;

        return string.Join(rowSep, res.Rows.Select(row =>
            string.Join(colSep, row.Select(cell => cell ?? string.Empty))));
    }

    public static string MapSqlFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var actor = reg.Database?.GetObject(ctx.Executor);
        if (actor == null || (!actor.IsWizard && !actor.HasPower("Sql_Ok")))
            return MushError.PermissionDenied.Text();
        if (reg.SqlDatabase == null || !reg.SqlDatabase.IsConnected)
            return MushError.SqlIsNotEnabled.Text();
        if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
            return MushError.TooFewArguments.Text();

        string osep = args.Length > 2 ? args[2] : " ";
        bool includeColNames = args.Length > 3 && LogicFunctions.IsTruthy(args[3]);

        var res = reg.SqlDatabase.Query(args[1]);
        if (res.Error != null) return MushError.SqlError.Text("error", res.Error);
        if (res.Fields.Count == 0) return string.Empty;

        var results = new List<string>();
        if (includeColNames)
        {
            var headerArgs = new string[2 + res.Fields.Count];
            headerArgs[0] = args[0];
            headerArgs[1] = "0";
            for (int i = 0; i < res.Fields.Count; i++)
                headerArgs[2 + i] = res.Fields[i];
            results.Add(DbFunctions.Ufun(reg, ctx, headerArgs));
        }

        int rowNum = 1;
        foreach (var row in res.Rows)
        {
            var uargs = new string[2 + res.Fields.Count];
            uargs[0] = args[0];
            uargs[1] = rowNum.ToString();
            for (int i = 0; i < res.Fields.Count; i++)
            {
                string val = i < row.Length ? (row[i] ?? string.Empty) : string.Empty;
                uargs[2 + i] = val;
                string colName = res.Fields[i];
                if (!int.TryParse(colName, out _) && LogicFunctions.IsValidQregName(colName))
                    ctx.SetRegister(colName, val);
            }
            results.Add(DbFunctions.Ufun(reg, ctx, uargs));
            rowNum++;
        }

        return string.Join(osep, results);
    }

    public static string WizardLogFunc(FunctionRegistry reg, EvaluationContext ctx)
    {
        var actor = reg.Database?.GetObject(ctx.Executor);
        if (actor == null || (!actor.IsWizard && !actor.IsRoyalty))
            return MushError.PermissionDenied.Text();
        return string.Empty;
    }

    public static string AddrLogFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var actor = reg.Database?.GetObject(ctx.Executor);
        if (actor == null || !PennMUSH.Core.Attributes.AttributePermissions.CanSeeAll(actor))
            return MushError.PermissionDenied.Text();
        if (reg.ConnectionLog == null || !reg.ConnectionLog.IsOpen)
            return string.Empty;

        int i = 0;
        bool countOnly = false;
        if (args.Length > 0 && args[0].Trim().Equals("count", StringComparison.OrdinalIgnoreCase))
        {
            countOnly = true;
            i = 1;
        }
        if (args.Length <= i + 1) return MushError.MissingArguments.Text();

        string mode = args[i].Trim();
        string pat = args[i + 1].Trim();
        string sep = args.Length > i + 2 ? args[i + 2] : "|";

        var hits = reg.ConnectionLog.QueryAddrLog(mode, pat);
        if (countOnly) return hits.Count.ToString();
        return string.Join(sep, hits.Select(h => $"{h.Ip} {h.Host}"));
    }

    public static string ConnLogFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var actor = reg.Database?.GetObject(ctx.Executor);
        if (actor == null || !PennMUSH.Core.Attributes.AttributePermissions.CanSeeAll(actor))
            return MushError.PermissionDenied.Text();
        if (reg.ConnectionLog == null || !reg.ConnectionLog.IsOpen)
            return string.Empty;

        int i = 0;
        bool countOnly = false;
        if (args.Length > 0 && args[0].Trim().Equals("count", StringComparison.OrdinalIgnoreCase))
        {
            countOnly = true;
            i = 1;
        }

        string mode = args.Length > i && !string.IsNullOrWhiteSpace(args[i]) ? args[i].Trim() : "all";
        long? after = args.Length > i + 1 && long.TryParse(args[i + 1].Trim(), out long a) ? a : null;
        long? before = args.Length > i + 2 && long.TryParse(args[i + 2].Trim(), out long b) ? b : null;
        string? spec = args.Length > i + 3 ? args[i + 3].Trim() : null;
        string sep = args.Length > i + 4 ? args[i + 4] : " ";

        var ids = reg.ConnectionLog.QueryConnLogIds(mode, after, before, spec);
        if (countOnly) return ids.Count.ToString();
        return string.Join(sep, ids);
    }

    public static string ConnRecordFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var actor = reg.Database?.GetObject(ctx.Executor);
        if (actor == null || !PennMUSH.Core.Attributes.AttributePermissions.CanSeeAll(actor))
            return MushError.PermissionDenied.Text();
        if (reg.ConnectionLog == null || !reg.ConnectionLog.IsOpen)
            return string.Empty;
        if (args.Length == 0) return MushError.TooFewArguments.Text();
        if (!long.TryParse(args[0].Trim(), out long id))
            return MushError.ArgumentMustBeInteger.Text();

        var rec = reg.ConnectionLog.GetRecord(id);
        if (rec == null) return MushError.NoSuchRecord.Text();

        string sep = args.Length > 1 ? args[1] : " ";
        return string.Join(sep, new[]
        {
            $"#{rec.Dbref}",
            rec.Name,
            rec.IpAddress,
            rec.Hostname,
            rec.ConnectedAt.ToString(),
            rec.DisconnectedAt.ToString(),
            rec.Reason,
            rec.Ssl ? "1" : "0",
            rec.WebSocket ? "1" : "0"
        });
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
        string report = Feedback.BenchmarkReport.Text(
            ("avg", Math.Round(avgUs, 2).ToString("G", CultureInfo.InvariantCulture)),
            ("min", Math.Round(minUs, 2).ToString("G", CultureInfo.InvariantCulture)),
            ("max", Math.Round(maxUs, 2).ToString("G", CultureInfo.InvariantCulture)));

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

    private static bool IsAdmin(FunctionRegistry reg, EvaluationContext ctx)
        => reg.Database?.GetObject(ctx.Executor)?.IsRoyalty == true;

    public static string SuggestFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
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
            "commands" => reg.CommandNamesProvider?.Invoke() ?? Enumerable.Empty<string>(),
            "attributes" or "attribs" => StandardAttributes.CanonicalNames(),
            "flags" => FlagTable.Definitions.Select(d => d.Name),
            "powers" => FlagTable.PowerNames,
            _ => Enumerable.Empty<string>()
        };

        if (reg.TextCache != null && reg.TextCache.HasCategory(cat))
        {
            bool admin = IsAdmin(reg, ctx);
            if (!reg.TextCache.IsAdminOnly(cat) || admin)
                candidates = candidates.Concat(reg.TextCache.GetEntries(cat, admin));
        }

        if (reg.Suggestions != null)
        {
            bool adminBlocked = reg.TextCache != null
                && reg.TextCache.HasCategory(cat)
                && reg.TextCache.IsAdminOnly(cat)
                && !IsAdmin(reg, ctx);
            if (!adminBlocked)
                candidates = candidates.Concat(reg.Suggestions.GetWords(cat));
        }

        string wordUpper = word.ToUpperInvariant();
        return string.Join(delim, candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(c => c.StartsWith(word, StringComparison.OrdinalIgnoreCase) || Math.Abs(c.Length - word.Length) <= 2)
            .Select(c => (Name: c, Dist: Levenshtein(wordUpper, c.ToUpperInvariant())))
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

    public static string TextEntriesFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string file = args[0].Trim();
        string pattern = args[1].Trim();
        string sep = args.Length > 2 && !string.IsNullOrEmpty(args[2]) ? args[2] : " ";

        if (reg.TextEntriesProvider != null)
        {
            var entries = reg.TextEntriesProvider(file)
                .Where(e => PennMUSH.Core.Queue.PatternMatcher.Match(pattern, e, out _));
            return string.Join(sep, entries);
        }

        if (reg.TextCache == null)
            return string.Empty;
        if (!reg.TextCache.HasCategory(file))
            return MushError.NoSuchFile.Text();

        bool admin = IsAdmin(reg, ctx);
        if (reg.TextCache.IsAdminOnly(file) && !admin)
            return MushError.PermissionDenied.Text();

        return string.Join(sep, reg.TextCache.GetEntries(file, admin)
            .Where(e => PennMUSH.Core.Queue.PatternMatcher.Match(pattern, e, out _)));
    }

    public static string TextFileFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.Nothing.Text();
        string file = args[0].Trim();
        string topic = args[1].Trim();

        if (reg.TextFileEntryProvider != null)
            return reg.TextFileEntryProvider(file, topic) ?? MushError.Nothing.Text();

        if (reg.TextCache == null)
            return MushError.Nothing.Text();
        if (!reg.TextCache.HasCategory(file))
            return MushError.NoSuchFile.Text();

        bool admin = IsAdmin(reg, ctx);
        if (reg.TextCache.IsAdminOnly(file) && !admin)
            return MushError.PermissionDenied.Text();

        return reg.TextCache.GetEntry(file, topic, admin, allowPrefix: true) ?? MushError.NoEntry.Text();
    }

    public static string TextSearchFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string file = args[0].Trim();
        string needle = args[1];
        string sep = args.Length > 2 && !string.IsNullOrEmpty(args[2]) ? args[2] : " ";

        if (reg.TextEntriesProvider != null && reg.TextFileEntryProvider != null)
        {
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

        if (reg.TextCache == null)
            return string.Empty;
        if (!reg.TextCache.HasCategory(file))
            return MushError.NoSuchFile.Text();

        bool admin = IsAdmin(reg, ctx);
        if (reg.TextCache.IsAdminOnly(file) && !admin)
            return MushError.PermissionDenied.Text();

        return string.Join(sep, reg.TextCache.SearchEntries(file, needle, admin));
    }

    public static string RootFunc(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        if (!double.TryParse(args[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double x))
            return MushError.ArgumentMustBeNumber.Text();
        if (!int.TryParse(args[1].Trim(), out int n))
            return MushError.ArgumentMustBeInteger.Text();
        if (n < 0) return MushError.RootOutOfRange.Text();
        if (n == 0) return "1";

        if (x < 0)
        {
            if (n % 2 == 0) return MushError.ImaginaryNumber.Text();
            double res = -Math.Pow(-x, 1.0 / n);
            return FormatRoot(res);
        }

        double val = Math.Pow(x, 1.0 / n);
        return FormatRoot(val);
    }

    private static string FormatRoot(double val)
    {
        if (Math.Abs(val - Math.Round(val)) < 1e-12)
            return ((long)Math.Round(val)).ToString(CultureInfo.InvariantCulture);
        return Math.Round(val, 10, MidpointRounding.AwayFromZero).ToString("0.##########", CultureInfo.InvariantCulture);
    }
}
