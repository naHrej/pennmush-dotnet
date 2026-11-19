namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PennMUSH.Core.Commands;
using PennMUSH.Core.MUSHcode;

public static class TextFormatFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Register("ANSI", AnsiFunc);
        registry.Register("STRIPANSI", StringFunctions.Strip);
        registry.Register("COLORS", ColorsFunc);
        registry.Register("ACCENT", AccentFunc);
        registry.Register("ACCNAME", (ctx, args) => AccNameFunc(registry, ctx, args));
        registry.Register("RENDER", RenderFunc);
        registry.Register("HTML", (ctx, args) => HtmlFunc(registry, ctx, args));
        registry.Register("TAG", TagFunc);
        registry.Register("ENDTAG", EndTagFunc);
        registry.Register("TAGWRAP", TagWrapFunc);
        registry.Register("PUEBLO", PuebloFunc);
        registry.Register("OOB", (ctx, args) => OobFunc(registry, ctx, args));
        registry.Register("WSHTML", (ctx, args) => WsHtmlFunc(registry, ctx, args));
    }

    public static string AnsiFunc(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return args.Length > 0 ? args[0] : string.Empty;
        string spec = args[0].Trim();
        string text = args[1];
        if (string.IsNullOrEmpty(spec) || spec == "n") return text;

        var codes = new List<string>();
        foreach (char c in spec)
        {
            string? code = c switch
            {
                'h' => "1",
                'u' => "4",
                'f' => "5",
                'i' => "7",
                'n' => "0",
                'x' => "30",
                'r' => "31",
                'g' => "32",
                'y' => "33",
                'b' => "34",
                'm' => "35",
                'c' => "36",
                'w' => "37",
                'X' => "40",
                'R' => "41",
                'G' => "42",
                'Y' => "43",
                'B' => "44",
                'M' => "45",
                'C' => "46",
                'W' => "47",
                _ => null
            };
            if (code != null) codes.Add(code);
        }

        if (codes.Count == 0) return text;
        return $"\x1B[{string.Join(";", codes)}m{text}\x1B[0m";
    }

    public static string ColorsFunc(EvaluationContext ctx, string[] args)
    {
        const string standardColors = "black red green yellow blue magenta cyan white";
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            return standardColors;
        return standardColors;
    }

    public static string AccentFunc(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return args.Length > 0 ? args[0] : string.Empty;
        string text = args[0];
        string tmpl = args[1];
        var sb = new StringBuilder(text.Length);
        int len = Math.Min(text.Length, tmpl.Length);

        for (int i = 0; i < len; i++)
        {
            sb.Append(ApplyAccent(text[i], tmpl[i]));
        }
        if (text.Length > len)
            sb.Append(text, len, text.Length - len);
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

    public static string RenderFunc(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string text = args[0];
        string fmt = args.Length > 1 ? args[1].Trim().ToLowerInvariant() : "ansi";
        return fmt switch
        {
            "ascii" => StringFunctions.Strip(ctx, [text]),
            "html" or "pueblo" => System.Net.WebUtility.HtmlEncode(StringFunctions.Strip(ctx, [text])),
            _ => text
        };
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

    public static string PuebloFunc(EvaluationContext ctx, string[] args) => "0";

    public static string OobFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || reg.Database == null || reg.NotificationDispatcher == null)
            return string.Empty;
        var target = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx))
            ?? reg.Database.MatchObject(ctx.Executor, args[0]);
        if (target == null) return MushError.NoMatch.Text();

        string pkg = args[1].Trim();
        string payload = args.Length > 2 ? args[2] : string.Empty;
        reg.NotificationDispatcher.Invoke(new Notification(
            NotificationScope.Target,
            string.Empty,
            Target: target.Id,
            ProtocolType: pkg,
            ProtocolPayload: payload,
            Speaker: ctx.Executor));
        return string.Empty;
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
