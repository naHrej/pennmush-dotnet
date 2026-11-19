namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using PennMUSH.Core.MUSHcode;

public static class JsonFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Register("ISJSON", IsJson);
        registry.Register("JSON", JsonBuild);
        registry.Register("JSON_QUERY", JsonQuery);
        registry.Register("JSON_MOD", JsonMod);
        registry.Register("JSON_MAP", (ctx, args) => JsonMap(registry, ctx, args));
        registry.Register("WSJSON", (ctx, args) => WsJson(registry, ctx, args));
    }

    public static string IsJson(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0])) return "0";
        try
        {
            using var _ = JsonDocument.Parse(args[0]);
            return "1";
        }
        catch
        {
            return "0";
        }
    }

    private static JsonNode? ParseOrString(string raw)
    {
        string trimmed = raw.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return JsonValue.Create(string.Empty);
        try
        {
            return JsonNode.Parse(trimmed);
        }
        catch
        {
            return JsonValue.Create(raw);
        }
    }

    public static string JsonBuild(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return MushError.TooFewArguments.Text();
        string type = StringFunctions.Strip(ctx, [args[0]]).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(type)) type = "string";

        switch (type)
        {
            case "null":
                if (args.Length > 2) return "#-1 WRONG NUMBER OF ARGUMENTS";
                if (args.Length == 2 && !args[1].Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
                    return "#-1 INVALID VALUE";
                return "null";
            case "boolean":
            {
                if (args.Length != 2) return "#-1 WRONG NUMBER OF ARGUMENTS";
                string b = StringFunctions.Strip(ctx, [args[1]]).Trim().ToLowerInvariant();
                return b switch
                {
                    "true" or "1" => "true",
                    "false" or "0" => "false",
                    _ => "#-1 INVALID VALUE"
                };
            }
            case "number":
            {
                if (args.Length != 2) return "#-1 WRONG NUMBER OF ARGUMENTS";
                string numStr = StringFunctions.Strip(ctx, [args[1]]).Trim();
                if (!double.TryParse(numStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
                    return MushError.ArgumentMustBeNumber.Text();
                return numStr;
            }
            case "string":
                if (args.Length != 2) return "#-1 WRONG NUMBER OF ARGUMENTS";
                return JsonSerializer.Serialize(args[1]);
            case "array":
            {
                var arr = new JsonArray();
                for (int i = 1; i < args.Length; i++)
                {
                    try { arr.Add(JsonNode.Parse(StringFunctions.Strip(ctx, [args[i]]))); }
                    catch { return "#-1 INVALID VALUE"; }
                }
                return arr.ToJsonString();
            }
            case "object":
            {
                if (args.Length % 2 == 0) return "#-1 WRONG NUMBER OF ARGUMENTS";
                var obj = new JsonObject();
                for (int i = 1; i + 1 < args.Length; i += 2)
                {
                    if (string.IsNullOrEmpty(args[i])) return "#-1 INVALID VALUE";
                    try { obj[args[i]] = JsonNode.Parse(StringFunctions.Strip(ctx, [args[i + 1]])); }
                    catch { return "#-1 INVALID VALUE"; }
                }
                return obj.ToJsonString();
            }
            default:
                return "#-1 INVALID TYPE";
        }
    }

    private static string NodeToMushString(JsonNode? node)
    {
        if (node == null) return string.Empty;
        if (node is JsonValue val)
        {
            if (val.TryGetValue<string>(out var s)) return s;
            if (val.TryGetValue<bool>(out var b)) return b ? "1" : "0";
            return val.ToJsonString();
        }
        return node.ToJsonString();
    }

    private static bool TryNavigate(JsonNode? root, IEnumerable<string> keys, out JsonNode? target)
    {
        target = root;
        foreach (string rawKey in keys)
        {
            if (target == null) return false;
            string key = rawKey.Trim();
            if (key == "$") continue;

            if (target is JsonObject obj)
            {
                if (!obj.TryGetPropertyValue(key, out target))
                    return false;
            }
            else if (target is JsonArray arr && int.TryParse(key, out int idx) && idx >= 0 && idx < arr.Count)
            {
                target = arr[idx];
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    private static IEnumerable<string> ParsePathSegments(string[] pathArgs)
    {
        if (pathArgs.Length == 1 && pathArgs[0].TrimStart().StartsWith("$", StringComparison.Ordinal))
        {
            string p = pathArgs[0].Trim();
            if (p == "$") return Array.Empty<string>();
            return p.TrimStart('$', '.').Split('.', StringSplitOptions.RemoveEmptyEntries);
        }
        return pathArgs;
    }

    private static string GetJsonType(JsonNode? node)
    {
        if (node == null) return "null";
        if (node is JsonObject) return "object";
        if (node is JsonArray) return "array";
        if (node is JsonValue v)
        {
            if (v.TryGetValue<bool>(out _)) return "boolean";
            if (v.TryGetValue<double>(out _) || v.TryGetValue<long>(out _)) return "number";
            return "string";
        }
        return "null";
    }

    public static string JsonQuery(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return MushError.TooFewArguments.Text();
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(args[0]);
        }
        catch
        {
            return MushError.Nothing.Text();
        }

        string op = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1])
            ? args[1].Trim().ToLowerInvariant()
            : "type";
        string[] rest = args.Length > 2 ? args[2..] : Array.Empty<string>();

        switch (op)
        {
            case "type":
                return GetJsonType(root);
            case "size":
                if (root is JsonObject o) return o.Count.ToString();
                if (root is JsonArray a) return a.Count.ToString();
                return root == null ? "0" : "1";
            case "exists":
                return TryNavigate(root, ParsePathSegments(rest), out _) ? "1" : "0";
            case "get":
                return TryNavigate(root, ParsePathSegments(rest), out var found)
                    ? (found?.ToJsonString() ?? "null")
                    : MushError.Nothing.Text();
            case "unescape":
                return root is JsonValue sv && sv.TryGetValue<string>(out var str)
                    ? str
                    : MushError.Nothing.Text();
            default:
                return MushError.UnknownOperation.Text();
        }
    }

    public static string JsonMod(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3) return MushError.TooFewArguments.Text();
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(args[0]);
        }
        catch
        {
            return MushError.Nothing.Text();
        }
        if (root == null) return MushError.Nothing.Text();

        string op = args[1].Trim().ToLowerInvariant();
        var segments = ParsePathSegments(new[] { args[2] }).ToArray();
        if (segments.Length == 0) return root.ToJsonString();

        if (!TryNavigate(root, segments[..^1], out var parent) || parent == null)
            return MushError.NoMatch.Text();

        string leaf = segments[^1];
        switch (op)
        {
            case "set" or "insert" or "replace":
            {
                if (args.Length < 4) return MushError.TooFewArguments.Text();
                var valNode = ParseOrString(args[3]);
                if (parent is JsonObject obj)
                {
                    if (op == "replace" && !obj.ContainsKey(leaf)) return MushError.NoMatch.Text();
                    if (op == "insert" && obj.ContainsKey(leaf)) return root.ToJsonString();
                    obj[leaf] = valNode;
                }
                else if (parent is JsonArray arr && int.TryParse(leaf, out int idx))
                {
                    if (idx >= 0 && idx < arr.Count) arr[idx] = valNode;
                    else if (idx == arr.Count && op != "replace") arr.Add(valNode);
                    else return MushError.OutOfRange.Text();
                }
                else
                {
                    return MushError.Nothing.Text();
                }
                return root.ToJsonString();
            }
            case "remove":
            {
                if (parent is JsonObject obj)
                    obj.Remove(leaf);
                else if (parent is JsonArray arr && int.TryParse(leaf, out int idx) && idx >= 0 && idx < arr.Count)
                    arr.RemoveAt(idx);
                return root.ToJsonString();
            }
            default:
                return MushError.UnknownOperation.Text();
        }
    }

    public static string JsonMap(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(args[1]);
        }
        catch
        {
            return MushError.Nothing.Text();
        }

        string osep = args.Length > 2 ? args[2] : " ";
        var extraArgs = args.Length > 3 ? args[3..] : Array.Empty<string>();
        var results = new List<string>();

        string CallMap(JsonNode? node, string key)
        {
            var uargs = new string[4 + extraArgs.Length];
            uargs[0] = args[0];
            uargs[1] = GetJsonType(node);
            uargs[2] = NodeToMushString(node);
            uargs[3] = key;
            Array.Copy(extraArgs, 0, uargs, 4, extraArgs.Length);
            return DbFunctions.Ufun(reg, ctx, uargs);
        }

        if (root is JsonArray arr)
        {
            for (int i = 0; i < arr.Count; i++)
                results.Add(CallMap(arr[i], i.ToString()));
        }
        else if (root is JsonObject obj)
        {
            foreach (var kv in obj)
                results.Add(CallMap(kv.Value, kv.Key));
        }
        else
        {
            results.Add(CallMap(root, string.Empty));
        }

        return string.Join(osep, results);
    }

    public static string WsJson(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var actor = reg.Database?.GetObject(ctx.Executor);
        if (actor == null || (!actor.IsWizard && !actor.HasPower("Pueblo_Send")))
            return MushError.PermissionDenied.Text();
        if (args.Length == 0)
            return string.Empty;

        try
        {
            using var _ = JsonDocument.Parse(args[0]);
        }
        catch
        {
            return "#-1 INVALID JSON";
        }

        string fallback = args.Length > 1 ? args[1] : string.Empty;
        return PennMUSH.Core.Protocol.WebSocketMarkup.EncodeJson(args[0], fallback);
    }
}
