namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Commands;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Presentation;
using PennMUSH.Core.Queue;

public static class TextFormatFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Register("ANSI", (ctx, args) => AnsiFunc(registry, ctx, args));
        registry.Register("STRIPANSI", StringFunctions.Strip);
        registry.Register("COLORS", ColorsFunc);
        registry.Register("ACCENT", AccentFunc);
        registry.Register("ACCNAME", (ctx, args) => AccNameFunc(registry, ctx, args));
        registry.Register("RENDER", (ctx, args) => RenderFunc(registry, ctx, args));
        registry.Register("HTML", (ctx, args) => HtmlFunc(registry, ctx, args));
        registry.Register("TAG", TagFunc);
        registry.Register("ENDTAG", EndTagFunc);
        registry.Register("TAGWRAP", TagWrapFunc);
        registry.Register("PUEBLO", (ctx, args) => PuebloFunc(registry, ctx, args));
        registry.Register("OOB", (ctx, args) => OobFunc(registry, ctx, args));
        registry.Register("WSHTML", (ctx, args) => WsHtmlFunc(registry, ctx, args));
    }

    public static string AnsiFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return args.Length > 0 ? args[0] : string.Empty;
        if (string.IsNullOrEmpty(args[1])) return string.Empty;

        string spec = AnsiColorTable.StripAnsi(args[0]).Trim();
        if (!AnsiColorTable.TryBuildAnsiSgr(spec, out string sgrOpen, out string sgrClose, out bool hasAnsi))
            return "#-1 INVALID ANSI DEFINITION";
        if (!hasAnsi)
            return args[1];

        return $"{sgrOpen}{args[1]}{sgrClose}";
    }

    public static string ColorsFunc(EvaluationContext ctx, string[] args)
    {
        if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
            return FormatColorsConversion(args[0], args[1]);

        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            return string.Join(" ", AnsiColorTable.NamedColorNames);

        string pattern = args[0].Trim();
        return string.Join(" ", AnsiColorTable.AllColorNames
            .Where(name => PatternMatcher.Match(pattern, name, out _)));
    }

    private static string FormatColorsConversion(string rawColorSpec, string rawFormats)
    {
        string colorSpec = AnsiColorTable.StripAnsi(rawColorSpec).Trim();
        if (!AnsiColorTable.TryParseAnsiSpec(colorSpec, out var parsed))
        {
            if (AnsiColorTable.TryGetNamedColor(colorSpec, out var namedEntry))
            {
                parsed = new ParsedAnsiSpec
                {
                    Foreground = new ParsedColorValue(
                        namedEntry.R,
                        namedEntry.G,
                        namedEntry.B,
                        namedEntry.Xterm,
                        namedEntry.Ansi16Index,
                        namedEntry.IsHiliteAnsi16,
                        $"38;2;{namedEntry.R};{namedEntry.G};{namedEntry.B}",
                        $"48;2;{namedEntry.R};{namedEntry.G};{namedEntry.B}",
                        $"+{namedEntry.Name}")
                };
            }
            else
            {
                return "#-1 INVALID COLOR";
            }
        }

        bool hasFg = parsed.Foreground is { IsDefault: false };
        bool hasBg = parsed.Background is { IsDefault: false };
        if (!hasFg && !hasBg)
            return "#-1 COLORS() REQUIRES AT LEAST ONE COLOR";

        string formatMode = "hex";
        bool includeStyles = false;

        foreach (string token in rawFormats.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            switch (token.ToLowerInvariant())
            {
                case "hex":
                case "x":
                    formatMode = "hex";
                    break;
                case "rgb":
                case "r":
                    formatMode = "rgb";
                    break;
                case "256color":
                case "xterm256":
                case "d":
                    formatMode = "256color";
                    break;
                case "xterm256x":
                case "h":
                    formatMode = "xterm256x";
                    break;
                case "xterm":
                    formatMode = "xterm";
                    break;
                case "16color":
                case "c":
                    formatMode = "16color";
                    break;
                case "name":
                    formatMode = "name";
                    break;
                case "auto":
                    formatMode = "auto";
                    break;
                case "styles":
                    includeStyles = true;
                    break;
                default:
                    return "#-1 INVALID ARGUMENT";
            }
        }

        var sb = new StringBuilder();
        if (includeStyles)
        {
            if (!hasFg && parsed.Bold) sb.Append('h');
            if (parsed.Underline) sb.Append('u');
            if (parsed.Flash) sb.Append('f');
            if (parsed.Inverse) sb.Append('i');
        }

        bool emitFg = parsed.Foreground.HasValue && (formatMode == "16color" || !parsed.Foreground.Value.IsDefault);
        bool emitBg = parsed.Background.HasValue && (formatMode == "16color" || !parsed.Background.Value.IsDefault);

        if (emitFg)
        {
            string? formattedFg = FormatColorComponent(parsed.Foreground!.Value, isBg: false, parsed.Bold, formatMode);
            if (formattedFg == null)
                return "#-1 NO MATCHING COLOR NAME";
            sb.Append(formattedFg);
        }

        if (emitBg)
        {
            if (formatMode != "16color")
                sb.Append('/');

            string? formattedBg = FormatColorComponent(parsed.Background!.Value, isBg: true, hiliteFg: false, formatMode);
            if (formattedBg == null)
                return "#-1 NO MATCHING COLOR NAME";
            sb.Append(formattedBg);
        }

        return sb.ToString();
    }

    private static string? FormatColorComponent(ParsedColorValue val, bool isBg, bool hiliteFg, string formatMode)
    {
        if (val.IsDefault)
            return formatMode == "16color" ? (isBg ? "D" : "d") : string.Empty;

        bool applyBright = !isBg && val.IsLegacyAnsi16 && hiliteFg;
        byte r = val.R;
        byte g = val.G;
        byte b = val.B;
        int xterm = val.Xterm;
        if (applyBright)
        {
            (r, g, b) = AnsiColorTable.GetAnsi16Rgb(val.Ansi16Index, hilite: true);
            xterm = AnsiColorTable.RgbToNearestXterm(r, g, b);
        }

        switch (formatMode)
        {
            case "hex":
                return $"#{r:x2}{g:x2}{b:x2}";
            case "rgb":
                return $"{r} {g} {b}";
            case "256color":
                return xterm.ToString(CultureInfo.InvariantCulture);
            case "xterm256x":
                return xterm.ToString("x", CultureInfo.InvariantCulture);
            case "xterm":
                return $"xterm{xterm}";
            case "16color":
            {
                char letter = AnsiColorTable.GetAnsi16Letter(val.Ansi16Index);
                if (isBg)
                    return char.ToUpperInvariant(letter).ToString();
                bool isHilite = val.Ansi16Hilite || hiliteFg;
                return isHilite ? $"{letter}h" : letter.ToString();
            }
            case "name":
            {
                var names = AnsiColorTable.FindNamesByRgb(r, g, b);
                return names.Count > 0 ? string.Join(" ", names) : null;
            }
            case "auto":
                return applyBright ? val.AutoToken + "h" : val.AutoToken;
            default:
                return $"#{r:x2}{g:x2}{b:x2}";
        }
    }

    public static string AccentFunc(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return args.Length > 0 ? args[0] : string.Empty;
        string text = args[0];
        string tmpl = args[1];
        if (text.Length != tmpl.Length)
            return "#-1 STRING LENGTHS MUST BE EQUAL";

        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
            sb.Append(ApplyAccent(text[i], tmpl[i]));
        return sb.ToString();
    }

    private static char ApplyAccent(char ch, char mod)
    {
        return (ch, mod) switch
        {
            ('a', '\'') => 'á', ('e', '\'') => 'é', ('i', '\'') => 'í', ('o', '\'') => 'ó', ('u', '\'') => 'ú', ('y', '\'') => 'ý',
            ('A', '\'') => 'Á', ('E', '\'') => 'É', ('I', '\'') => 'Í', ('O', '\'') => 'Ó', ('U', '\'') => 'Ú', ('Y', '\'') => 'Ý',
            ('a', '`') => 'à', ('e', '`') => 'è', ('i', '`') => 'ì', ('o', '`') => 'ò', ('u', '`') => 'ù',
            ('A', '`') => 'À', ('E', '`') => 'È', ('I', '`') => 'Ì', ('O', '`') => 'Ò', ('U', '`') => 'Ù',
            ('a', '^') => 'â', ('e', '^') => 'ê', ('i', '^') => 'î', ('o', '^') => 'ô', ('u', '^') => 'û',
            ('A', '^') => 'Â', ('E', '^') => 'Ê', ('I', '^') => 'Î', ('O', '^') => 'Ô', ('U', '^') => 'Û',
            ('a', ':') => 'ä', ('e', ':') => 'ë', ('i', ':') => 'ï', ('o', ':') => 'ö', ('u', ':') => 'ü', ('y', ':') => 'ÿ',
            ('A', ':') => 'Ä', ('E', ':') => 'Ë', ('I', ':') => 'Ï', ('O', ':') => 'Ö', ('U', ':') => 'Ü', ('Y', ':') => 'Ÿ',
            ('a', '~') => 'ã', ('n', '~') => 'ñ', ('o', '~') => 'õ',
            ('A', '~') => 'Ã', ('N', '~') => 'Ñ', ('O', '~') => 'Õ',
            ('c', ',') => 'ç', ('C', ',') => 'Ç',
            ('a', 'o') => 'å', ('A', 'o') => 'Å',
            ('o', '/') => 'ø', ('O', '/') => 'Ø',
            ('a', 'e') => 'æ', ('A', 'E') => 'Æ',
            ('d', '-') => 'ð', ('D', '-') => 'Ð',
            ('?', 'u') => '¿', ('!', 'u') => '¡',
            ('s', 'B') => 'ß',
            _ => ch
        };
    }

    public static string AccNameFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || reg.Database == null) return MushError.NoMatch.Text();
        var obj = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx))
            ?? reg.Database.MatchObject(ctx.Executor, args[0]);
        if (obj == null) return MushError.NoMatch.Text();

        var attr = obj.GetAttribute("NAMEACCENT", reg.Database);
        if (attr == null || attr.Value.Length != obj.Name.Length)
            return obj.Name;
        return AccentFunc(ctx, [obj.Name, attr.Value]);
    }

    public static string RenderFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string result = args[0];
        string flagsStr = args.Length > 1 ? args[1].Trim().ToLowerInvariant() : "ansi";
        var flags = flagsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (flags.Length == 0) flags = ["ansi"];

        bool keepAnsi = false;
        bool encodeHtml = false;
        bool stripAccents = false;

        foreach (string flag in flags)
        {
            switch (flag)
            {
                case "ansi":
                    var actor = reg.Database?.GetObject(ctx.Executor);
                    if (actor != null && !AttributePermissions.CanSeeAll(actor))
                        return MushError.PermissionDenied.Text();
                    keepAnsi = true;
                    break;
                case "ascii":
                    keepAnsi = false;
                    break;
                case "html":
                    encodeHtml = true;
                    break;
                case "noaccents":
                    stripAccents = true;
                    break;
                case "markup":
                    keepAnsi = true;
                    break;
                default:
                    return "#-1 INVALID SECOND ARGUMENT";
            }
        }

        if (!keepAnsi)
            result = StringFunctions.Strip(ctx, [result]);
        if (stripAccents)
            result = StringFunctions.StripAccents(ctx, [result]);
        if (encodeHtml)
        {
            result = keepAnsi
                ? AnsiToHtmlTranslator.Translate(result, escapeHtml: true, ColorDepth.TrueColor)
                : System.Net.WebUtility.HtmlEncode(result);
        }
        return result;
    }

    public static string HtmlFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var actor = reg.Database?.GetObject(ctx.Executor);
        if (actor == null || (!actor.IsWizard && !actor.HasPower("Pueblo_Send")))
            return MushError.PermissionDenied.Text();
        return args.Length > 0 ? args[0] : string.Empty;
    }

    public static string TagFunc(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0])) return string.Empty;
        string name = args[0].Trim();
        if (args.Length == 1) return $"<{name}>";
        string attrs = string.Join(" ", args.Skip(1).Where(a => !string.IsNullOrWhiteSpace(a)));
        return string.IsNullOrEmpty(attrs) ? $"<{name}>" : $"<{name} {attrs}>";
    }

    public static string EndTagFunc(EvaluationContext ctx, string[] args)
        => args.Length == 0 || string.IsNullOrWhiteSpace(args[0]) ? string.Empty : $"</{args[0].Trim()}>";

    public static string TagWrapFunc(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || string.IsNullOrWhiteSpace(args[0])) return string.Empty;
        string name = args[0].Trim();
        string inner = args[^1];
        string open = args.Length > 2 ? TagFunc(ctx, new[] { name }.Concat(args.Skip(1).Take(args.Length - 2)).ToArray()) : $"<{name}>";
        return $"{open}{inner}</{name}>";
    }

    public static string PuebloFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.ConnectionsProvider == null || reg.Database == null) return "0";
        var targetId = ctx.Executor;
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            var p = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx))
                ?? reg.Database.FindPlayer(args[0].Trim().TrimStart('*'));
            if (p == null) return "#-1 NOT CONNECTED";
            targetId = p.Id;
        }
        var snap = reg.ConnectionsProvider().FirstOrDefault(s => s.PlayerId == targetId);
        if (snap == null) return "#-1 NOT CONNECTED";
        return snap.SupportsPueblo
            || snap.SupportsHtml
            || snap.TerminalType.Contains("pueblo", StringComparison.OrdinalIgnoreCase)
            || snap.TerminalType.Contains("html", StringComparison.OrdinalIgnoreCase)
            ? "1"
            : "0";
    }

    public static string OobFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null)
            return "0";
        string pkg = args[1].Trim();
        string payload = args.Length > 2 ? args[2] : string.Empty;
        if (!string.IsNullOrWhiteSpace(payload))
        {
            try
            {
                using var _ = JsonDocument.Parse(payload);
            }
            catch
            {
                return "#-1 INVALID JSON";
            }
        }

        var actor = reg.Database.GetObject(ctx.Executor);
        var targets = args[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int sent = 0;
        bool sawDenied = false;

        foreach (string spec in targets)
        {
            var target = reg.Database.GetObject(DbFunctions.ParseDbref(spec, ctx))
                ?? reg.Database.MatchObject(ctx.Executor, spec);
            if (target == null) continue;
            if (actor == null || (!actor.IsWizard && !actor.HasPower("Send_OOB") && !AttributePermissions.Controls(actor, target)))
            {
                sawDenied = true;
                continue;
            }
            reg.NotificationDispatcher?.Invoke(new Notification(
                NotificationScope.Target,
                string.Empty,
                Target: target.Id,
                ProtocolType: pkg,
                ProtocolPayload: payload,
                Speaker: ctx.Executor));
            sent++;
        }

        if (sent == 0 && sawDenied) return MushError.PermissionDenied.Text();
        return sent == 0 ? "#-1 NO VALID PLAYERS" : sent.ToString();
    }

    public static string WsHtmlFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var actor = reg.Database?.GetObject(ctx.Executor);
        if (actor == null || (!actor.IsWizard && !actor.HasPower("Pueblo_Send")))
            return MushError.PermissionDenied.Text();
        if (args.Length == 0)
            return string.Empty;
        string fallback = args.Length > 1 ? args[1] : string.Empty;
        return PennMUSH.Core.Protocol.WebSocketMarkup.EncodeHtml(args[0], fallback);
    }
}
