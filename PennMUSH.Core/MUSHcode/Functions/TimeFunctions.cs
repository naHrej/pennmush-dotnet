namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Globalization;

public static class TimeFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Register("TIME", Time);
        registry.Register("SECS", Secs);
        registry.Register("CONVSECS", ConvSecs);
        registry.Register("CONVTIME", ConvTime);
        registry.Register("SECS2TIME", Secs2Time);
        registry.Register("TIMEFMT", TimeFmt);
        registry.Register("ETIMEFMT", ETimeFmt);
        registry.Register("ISDAYLIGHT", IsDaylight);
        registry.Register("TZOFFSET", TzOffset);
        registry.Register("MTIME", (ctx, args) => MTime(registry, ctx, args));

        // Section 8: Clock and time arithmetic
        registry.Register("UTCTIME", UtcTime);
        registry.Register("CONVUTCSECS", ConvUtcSecs);
        registry.Register("CONVUTCTIME", ConvUtcTime);
        registry.Register("CSECS", (ctx, args) => CSecs(registry, ctx, args));
        registry.Register("CTIME", (ctx, args) => CTime(registry, ctx, args));
        registry.Register("MSECS", (ctx, args) => MTime(registry, ctx, args));
        registry.Register("ETIME", ETime);
        registry.Register("STRINGSECS", StringSecs);
        registry.Register("TIMESTRING", TimeString);
        registry.Register("SECSCALC", SecsCalc);
        registry.Register("TIMECALC", TimeCalc);
        registry.Register("STARTTIME", (ctx, args) => StartTime(registry));
        registry.Register("RESTARTTIME", (ctx, args) => StartTime(registry));
        registry.Register("UPTIME", (ctx, args) => Uptime(registry));
        registry.Register("RESTARTS", (ctx, args) => registry.RestartCount.ToString());
    }

    public static string Time(EvaluationContext ctx, string[] args)
    {
        return DateTime.Now.ToString("ddd MMM d HH:mm:ss yyyy", CultureInfo.InvariantCulture);
    }

    public static string Secs(EvaluationContext ctx, string[] args)
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    }

    public static string ConvSecs(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || !long.TryParse(args[0].Trim(), out long secs))
        {
            secs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        var dt = DateTimeOffset.FromUnixTimeSeconds(secs).ToLocalTime();
        return dt.ToString("ddd MMM d HH:mm:ss yyyy", CultureInfo.InvariantCulture);
    }

    public static string ConvTime(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "-1";
        string input = args[0].Trim();
        if (DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
        {
            return dto.ToUnixTimeSeconds().ToString();
        }
        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return new DateTimeOffset(dt).ToUnixTimeSeconds().ToString();
        }
        return "-1";
    }

    public static string Secs2Time(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || !long.TryParse(args[0].Trim(), out long totalSeconds)) return "0s";
        if (totalSeconds <= 0) return "0s";

        long days = totalSeconds / 86400;
        long hours = (totalSeconds % 86400) / 3600;
        long minutes = (totalSeconds % 3600) / 60;
        long seconds = totalSeconds % 60;

        var parts = new System.Collections.Generic.List<string>();
        if (days > 0) parts.Add($"{days}d");
        if (hours > 0) parts.Add($"{hours}h");
        if (minutes > 0) parts.Add($"{minutes}m");
        if (seconds > 0 || parts.Count == 0) parts.Add($"{seconds}s");

        return string.Join(" ", parts);
    }

    public static string TimeFmt(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string format = args[0];
        long secs = args.Length > 1 && long.TryParse(args[1].Trim(), out long s)
            ? s
            : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var dt = DateTimeOffset.FromUnixTimeSeconds(secs).ToLocalTime();

        // PennMUSH timefmt uses $-prefixed escape codes (leaving % literal)
        return format
            .Replace("$$", "\u0001")
            .Replace("$Y", dt.ToString("yyyy"))
            .Replace("$y", dt.ToString("yy"))
            .Replace("$m", dt.ToString("MM"))
            .Replace("$B", dt.ToString("MMMM"))
            .Replace("$b", dt.ToString("MMM"))
            .Replace("$d", dt.ToString("dd"))
            .Replace("$H", dt.ToString("HH"))
            .Replace("$I", dt.ToString("hh"))
            .Replace("$M", dt.ToString("mm"))
            .Replace("$S", dt.ToString("ss"))
            .Replace("$p", dt.ToString("tt"))
            .Replace("$a", dt.ToString("ddd"))
            .Replace("$A", dt.ToString("dddd"))
            .Replace("\u0001", "$");
    }

    public static string ETimeFmt(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        string format = args[0];
        if (!ulong.TryParse(args[1].Trim(), out ulong totalSeconds))
            return MushError.ArgumentMustBeUnsignedInteger.Text();

        // Scan format once to see which large units are actually requested
        var tokens = new List<(int Start, int Length, int Width, bool Total, bool OmitZero, bool UnitSuffix, char Unit)>();
        bool hasYears = false, hasWeeks = false, hasDays = false, hasHours = false, hasMinutes = false;

        for (int i = 0; i < format.Length; i++)
        {
            if (format[i] != '$' || i + 1 >= format.Length) continue;
            if (format[i + 1] == '$')
            {
                tokens.Add((i, 2, 0, false, false, false, '$'));
                i++;
                continue;
            }

            int j = i + 1;
            int width = 0;
            while (j < format.Length && char.IsDigit(format[j]))
            {
                width = width * 10 + (format[j] - '0');
                j++;
            }

            bool total = false, omitZero = false, unitSuffix = false;
            while (j < format.Length && format[j] is 't' or 'T' or 'x' or 'X' or 'z' or 'Z')
            {
                switch (char.ToLowerInvariant(format[j]))
                {
                    case 't': total = true; break;
                    case 'x': unitSuffix = true; omitZero = format[j] == 'x'; break;
                    case 'z': omitZero = true; break;
                }
                j++;
            }

            if (j < format.Length && format[j] is 'y' or 'Y' or 'w' or 'W' or 'd' or 'D' or 'h' or 'H' or 'm' or 'M' or 's' or 'S')
            {
                char unit = format[j];
                if (width == 0 && char.IsUpper(unit)) width = 2;
                char lower = char.ToLowerInvariant(unit);
                if (!total)
                {
                    if (lower == 'y') hasYears = true;
                    else if (lower == 'w') hasWeeks = true;
                    else if (lower == 'd') hasDays = true;
                    else if (lower == 'h') hasHours = true;
                    else if (lower == 'm') hasMinutes = true;
                }
                tokens.Add((i, j - i + 1, width, total, omitZero, unitSuffix, unit));
                i = j;
            }
        }

        ulong rem = totalSeconds;
        ulong years = hasYears ? rem / 31536000 : 0; if (hasYears) rem %= 31536000;
        ulong weeks = hasWeeks ? rem / 604800 : 0; if (hasWeeks) rem %= 604800;
        ulong days = hasDays ? rem / 86400 : 0; if (hasDays) rem %= 86400;
        ulong hours = hasHours ? rem / 3600 : 0; if (hasHours) rem %= 3600;
        ulong minutes = hasMinutes ? rem / 60 : 0; if (hasMinutes) rem %= 60;
        ulong seconds = rem;

        var sb = new System.Text.StringBuilder(format.Length + 16);
        int cursor = 0;
        foreach (var tok in tokens)
        {
            if (tok.Start > cursor)
                sb.Append(format, cursor, tok.Start - cursor);

            if (tok.Unit == '$')
            {
                sb.Append('$');
            }
            else
            {
                char lower = char.ToLowerInvariant(tok.Unit);
                ulong val = tok.Total
                    ? lower switch
                    {
                        'y' => totalSeconds / 31536000,
                        'w' => totalSeconds / 604800,
                        'd' => totalSeconds / 86400,
                        'h' => totalSeconds / 3600,
                        'm' => totalSeconds / 60,
                        _ => totalSeconds
                    }
                    : lower switch
                    {
                        'y' => years,
                        'w' => weeks,
                        'd' => days,
                        'h' => hours,
                        'm' => minutes,
                        _ => seconds
                    };

                if (!(tok.OmitZero && val == 0))
                {
                    string num = tok.Width > 0
                        ? (char.IsUpper(tok.Unit)
                            ? val.ToString($"D{tok.Width}")
                            : val.ToString().PadLeft(tok.Width, ' '))
                        : val.ToString();
                    sb.Append(num);
                    if (tok.UnitSuffix)
                        sb.Append(lower);
                }
            }

            cursor = tok.Start + tok.Length;
        }
        if (cursor < format.Length)
            sb.Append(format, cursor, format.Length - cursor);

        return sb.ToString();
    }

    public static string IsDaylight(EvaluationContext ctx, string[] args)
    {
        return TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now) ? "1" : "0";
    }

    public static string TzOffset(EvaluationContext ctx, string[] args)
    {
        return ((long)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalSeconds).ToString();
    }

    public static string MTime(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return "-1";
        var dbref = DbFunctions.ParseDbref(args[0], ctx);
        if (!dbref.IsValid) return "-1";

        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return "-1";

        return obj.ModificationTime.ToUnixTimeSeconds().ToString();
    }

    public static string UtcTime(EvaluationContext ctx, string[] args)
        => DateTime.UtcNow.ToString("ddd MMM d HH:mm:ss yyyy", CultureInfo.InvariantCulture);

    public static string ConvUtcSecs(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || !long.TryParse(args[0].Trim(), out long secs))
            secs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return DateTimeOffset.FromUnixTimeSeconds(secs).ToUniversalTime()
            .ToString("ddd MMM d HH:mm:ss yyyy", CultureInfo.InvariantCulture);
    }

    public static string ConvUtcTime(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "-1";
        string input = args[0].Trim();
        string[] formats = ["ddd MMM d HH:mm:ss yyyy", "ddd MMM dd HH:mm:ss yyyy", "ddd MMM  d HH:mm:ss yyyy"];
        if (DateTimeOffset.TryParseExact(input, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var exactDto))
            return exactDto.ToUnixTimeSeconds().ToString();
        if (DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
            return dto.ToUnixTimeSeconds().ToString();
        return "-1";
    }

    public static string CSecs(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return "-1";
        var dbref = DbFunctions.ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj != null)
            return obj.CreationTime.ToUnixTimeSeconds().ToString();
        return ConvTime(ctx, args);
    }

    public static string CTime(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var dbref = DbFunctions.ParseDbref(args[0], ctx);
        var obj = reg.Database.GetObject(dbref);
        if (obj == null) return MushError.NoMatch.Text();

        long secs = obj.CreationTime.ToUnixTimeSeconds();
        bool utc = args.Length > 1 && LogicFunctions.IsTruthy(args[1]);
        return utc ? ConvUtcSecs(ctx, [secs.ToString()]) : ConvSecs(ctx, [secs.ToString()]);
    }

    public static string ETime(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || !long.TryParse(args[0].Trim(), out long secs))
            return MushError.ArgumentMustBeInteger.Text();
        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]) && !int.TryParse(args[1].Trim(), out _))
            return MushError.ArgumentMustBeInteger.Text();
        if (secs < 0) return MushError.OutOfRange.Text();
        if (secs == 0) return "0s";

        long years = secs / 31536000; secs %= 31536000;
        long weeks = secs / 604800; secs %= 604800;
        long days = secs / 86400; secs %= 86400;
        long hours = secs / 3600; secs %= 3600;
        long mins = secs / 60; secs %= 60;

        var parts = new List<string>();
        void AddUnit(long val, char unit)
        {
            if (val <= 0) return;
            parts.Add(parts.Count > 0 ? $"{val,2}{unit}" : $"{val}{unit}");
        }
        AddUnit(years, 'y');
        AddUnit(weeks, 'w');
        AddUnit(days, 'd');
        AddUnit(hours, 'h');
        AddUnit(mins, 'm');
        AddUnit(secs, 's');

        if (args.Length > 1 && int.TryParse(args[1].Trim(), out int maxWidth) && maxWidth > 0)
        {
            while (parts.Count > 1 && string.Join(" ", parts).Length > maxWidth)
                parts.RemoveAt(parts.Count - 1);
        }
        return string.Join(" ", parts);
    }

    public static string StringSecs(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0])) return "0";
        if (TryParseDuration(args[0], out long total))
            return total.ToString();
        return MushError.ArgumentMustBeInteger.Text();
    }

    private static bool TryParseDuration(string raw, out long totalSeconds)
    {
        totalSeconds = 0;
        var tokens = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0) return false;

        foreach (string tok in tokens)
        {
            char unit = char.ToLowerInvariant(tok[^1]);
            if (char.IsDigit(unit))
            {
                if (!long.TryParse(tok, out long n)) return false;
                totalSeconds += n;
                continue;
            }

            if (!long.TryParse(tok.Substring(0, tok.Length - 1), out long val))
                return false;

            totalSeconds += unit switch
            {
                'y' => val * 31536000,
                'w' => val * 604800,
                'd' => val * 86400,
                'h' => val * 3600,
                'm' => val * 60,
                's' => val,
                _ => 0
            };
            if (unit is not ('y' or 'w' or 'd' or 'h' or 'm' or 's'))
                return false;
        }
        return true;
    }

    public static string TimeString(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || !long.TryParse(args[0].Trim(), out long secs))
            return MushError.ArgumentMustBeInteger.Text();
        if (secs < 0) return MushError.ArgumentMustBePositiveInteger.Text();

        int pad = args.Length > 1 && int.TryParse(args[1].Trim(), out int p) ? p : 0;
        long days = secs / 86400;
        long hours = (secs % 86400) / 3600;
        long minutes = (secs % 3600) / 60;
        long seconds = secs % 60;

        if (pad >= 2)
            return $"{days:D2}d {hours:D2}h {minutes:D2}m {seconds:D2}s";
        if (pad == 1)
            return $"{days}d {hours,2}h {minutes,2}m {seconds,2}s";

        var parts = new List<string>();
        if (days > 0) parts.Add($"{days,2}d");
        if (hours > 0 || parts.Count > 0) parts.Add($"{hours,2}h");
        if (minutes > 0 || parts.Count > 0) parts.Add($"{minutes,2}m");
        parts.Add($"{seconds,2}s");
        return string.Join(" ", parts);
    }

    private static bool TryComputeSecsCalc(string[] args, out long result)
    {
        result = 0;
        if (args.Length == 0) return false;

        string first = args[0].Trim();
        if (first.Equals("now", StringComparison.OrdinalIgnoreCase))
            result = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        else if (long.TryParse(first, out long ts))
            result = ts;
        else if (DateTimeOffset.TryParse(first, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
            result = dto.ToUnixTimeSeconds();
        else
            return false;

        for (int i = 1; i < args.Length; i++)
        {
            string arg = args[i].Trim();
            if (string.IsNullOrEmpty(arg)) continue;
            bool sub = arg.StartsWith('-');
            string body = arg.TrimStart('+', '-');
            if (!TryParseDuration(body, out long delta))
                return false;
            result += sub ? -delta : delta;
        }
        return true;
    }

    public static string SecsCalc(EvaluationContext ctx, string[] args)
        => TryComputeSecsCalc(args, out long result) ? result.ToString() : MushError.Nothing.Text();

    public static string TimeCalc(EvaluationContext ctx, string[] args)
        => TryComputeSecsCalc(args, out long result) ? ConvSecs(ctx, [result.ToString()]) : MushError.Nothing.Text();

    public static string StartTime(FunctionRegistry reg)
        => reg.ServerStartTime.ToLocalTime().ToString("ddd MMM d HH:mm:ss yyyy", CultureInfo.InvariantCulture);

    public static string Uptime(FunctionRegistry reg)
        => Math.Max(0, (long)(DateTimeOffset.UtcNow - reg.ServerStartTime).TotalSeconds).ToString();
}
