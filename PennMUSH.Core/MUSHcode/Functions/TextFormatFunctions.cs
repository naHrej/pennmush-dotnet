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
using PennMUSH.Core.Queue;

public static class TextFormatFunctions
{
    private static readonly Dictionary<string, (int R, int G, int B, int Xterm, string Ansi16)> NamedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["black"] = (0, 0, 0, 0, "x"),
        ["red"] = (205, 0, 0, 1, "r"),
        ["green"] = (0, 205, 0, 2, "g"),
        ["yellow"] = (205, 205, 0, 3, "y"),
        ["blue"] = (0, 0, 238, 4, "b"),
        ["magenta"] = (205, 0, 205, 5, "m"),
        ["cyan"] = (0, 205, 205, 6, "c"),
        ["white"] = (229, 229, 229, 7, "w"),
        ["gray"] = (127, 127, 127, 8, "hx"),
        ["grey"] = (127, 127, 127, 8, "hx"),
        ["orange"] = (255, 165, 0, 214, "y"),
        ["pink"] = (255, 192, 203, 218, "hr"),
        ["purple"] = (128, 0, 128, 129, "m"),
        ["brown"] = (165, 42, 42, 130, "y")
    };

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
        string spec = args[0].Trim();
        string text = args[1];
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (string.IsNullOrEmpty(spec) || spec == "n") return text;

        var codes = new List<string>();
        foreach (string part in spec.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string token = part;
            bool isBg = false;
            if (token.StartsWith('/'))
            {
                isBg = true;
                token = token[1..];
            }
            if (token.StartsWith('+'))
                token = token[1..];

            int slash = token.IndexOf('/');
            if (slash > 0)
            {
                if (!TryParseColorSpec(token[..slash], isBg: false, codes) ||
                    !TryParseColorSpec(token[(slash + 1)..], isBg: true, codes))
                    return "#-1 INVALID ANSI DEFINITION";
                continue;
            }

            if (TryParseColorSpec(token, isBg, codes))
                continue;

            // Legacy compact single-letter codes (e.g. "hr", "u", "n")
            foreach (char c in token)
            {
                string? code = c switch
                {
                    'h' => "1",
                    'u' => "4",
                    'f' => "5",
                    'i' => "7",
                    'n' => "0",
                    'x' => "30", 'r' => "31", 'g' => "32", 'y' => "33",
                    'b' => "34", 'm' => "35", 'c' => "36", 'w' => "37",
                    'X' => "40", 'R' => "41", 'G' => "42", 'Y' => "43",
                    'B' => "44", 'M' => "45", 'C' => "46", 'W' => "47",
                    _ => null
                };
                if (code == null) return "#-1 INVALID ANSI DEFINITION";
                codes.Add(code);
            }
        }

        if (codes.Count == 0) return text;
        return $"\x1B[{string.Join(";", codes)}m{text}\x1B[0m";
    }

    private static bool TryParseColorSpec(string token, bool isBg, List<string> codes)
    {
        if (string.IsNullOrEmpty(token)) return false;

        if (token.StartsWith("xterm", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(token.AsSpan(5), out int xterm) && xterm is >= 0 and <= 255)
        {
            codes.Add($"{(isBg ? 48 : 38)};5;{xterm}");
            return true;
        }

        if (token.StartsWith('#') && token.Length == 7 &&
            int.TryParse(token.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r) &&
            int.TryParse(token.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g) &&
            int.TryParse(token.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b))
        {
            codes.Add($"{(isBg ? 48 : 38)};2;{r};{g};{b}");
            return true;
        }

        if (token.Length > 1 && NamedColors.TryGetValue(token, out var nc))
        {
            codes.Add($"{(isBg ? 48 : 38)};2;{nc.R};{nc.G};{nc.B}");
            return true;
        }

        return false;
    }

    public static string ColorsFunc(EvaluationContext ctx, string[] args)
    {
        if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
        {
            string cSpec = args[0].Trim().TrimStart('+');
            string fmt = args[1].Trim().ToLowerInvariant();
            if (!NamedColors.TryGetValue(cSpec, out var info))
            {
                if (cSpec.StartsWith('#') && cSpec.Length == 7 &&
                    int.TryParse(cSpec.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r) &&
                    int.TryParse(cSpec.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g) &&
                    int.TryParse(cSpec.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b))
                {
                    info = (r, g, b, 0, "w");
                }
                else
                {
                    return "#-1 INVALID COLOR";
                }
            }

            return fmt switch
            {
                "hex" => $"#{info.R:x2}{info.G:x2}{info.B:x2}",
                "rgb" => $"{info.R} {info.G} {info.B}",
                "256color" or "xterm" => $"xterm{info.Xterm}",
                "16color" => info.Ansi16,
                "name" => cSpec.ToLowerInvariant(),
                _ => $"#{info.R:x2}{info.G:x2}{info.B:x2}"
            };
        }

        string pattern = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) ? args[0].Trim() : "*";
        return string.Join(" ", NamedColors.Keys
            .Where(k => PatternMatcher.Match(pattern, k, out _))
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase));
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
            result = System.Net.WebUtility.HtmlEncode(StringFunctions.Strip(ctx, [result]));
        return result;
    }

    public static string HtmlFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var actor = reg.Database?.GetObject(ctx.Executor);
        if (actor == null || !actor.IsWizard)
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
        return snap.TerminalType.Contains("pueblo", StringComparison.OrdinalIgnoreCase)
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
            if (actor == null || (!actor.IsWizard && !AttributePermissions.Controls(actor, target)))
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
        if (args.Length == 0) return string.Empty;
        if (args.Length >= 2 && reg.Database != null && reg.NotificationDispatcher != null)
        {
            var target = reg.Database.GetObject(DbFunctions.ParseDbref(args[1], ctx));
            if (target != null)
                reg.NotificationDispatcher.Invoke(new Notification(NotificationScope.Target, args[0], Target: target.Id, ProtocolType: "wshtml", ProtocolPayload: args[0]));
        }
        return args[0];
    }
}
