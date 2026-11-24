namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using PennMUSH.Core.MUSHcode;

public static class CryptoFunctions
{
    public static void Register(FunctionRegistry registry)
    {
        registry.Register("ENCODE64", Encode64);
        registry.Register("DECODE64", Decode64);
        registry.Register("URLENCODE", UrlEncode);
        registry.Register("URLDECODE", UrlDecode);
        registry.Register("FORMDECODE", FormDecode);
        registry.Register("DIGEST", Digest);
        registry.Register("HMAC", Hmac);
        registry.Register("SHA0", Sha0);
        registry.Register("ENCRYPT", Encrypt);
        registry.Register("DECRYPT", Decrypt);
        registry.Register("SSL", (ctx, args) => Ssl(registry, ctx, args));
        registry.Register("SQLESCAPE", (ctx, args) => SqlEscape(registry, ctx, args));
    }

    public static string Encode64(EvaluationContext ctx, string[] args)
        => args.Length == 0 ? string.Empty : Convert.ToBase64String(Encoding.UTF8.GetBytes(args[0]));

    public static string Decode64(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrEmpty(args[0])) return string.Empty;
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(args[0].Trim()));
        }
        catch
        {
            return MushError.Nothing.Text();
        }
    }

    public static string UrlEncode(EvaluationContext ctx, string[] args)
        => args.Length == 0 ? string.Empty : Uri.EscapeDataString(args[0]);

    private static string SanitizePrintable(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
            sb.Append(char.IsControl(c) ? '?' : c);
        return sb.ToString();
    }

    public static string UrlDecode(EvaluationContext ctx, string[] args)
        => args.Length == 0 ? string.Empty : SanitizePrintable(Uri.UnescapeDataString(args[0]));

    public static string FormDecode(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0])) return string.Empty;
        var pairs = args[0].Split('&', StringSplitOptions.RemoveEmptyEntries);
        var keys = new List<string>();
        var values = new List<string>();
        string? targetKey = args.Length > 1 ? args[1] : null;
        string sep = args.Length > 2 ? args[2] : " ";

        foreach (string pair in pairs)
        {
            int eq = pair.IndexOf('=');
            string k = SanitizePrintable(Uri.UnescapeDataString(eq >= 0 ? pair.Substring(0, eq) : pair));
            string v = eq >= 0 ? SanitizePrintable(Uri.UnescapeDataString(pair.Substring(eq + 1))) : string.Empty;
            if (targetKey == null)
            {
                keys.Add(k);
            }
            else if (string.Equals(k, targetKey, StringComparison.Ordinal))
            {
                values.Add(v);
            }
        }

        return targetKey == null ? string.Join(" ", keys) : string.Join(sep, values);
    }

    public static string Digest(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return MushError.TooFewArguments.Text();
        string algo = args[0].Trim().ToLowerInvariant();
        if (args.Length == 1)
        {
            return algo == "list"
                ? "md5 md4 sha1 sha224 sha256 sha384 sha512 ripemd160 whirlpool"
                : MushError.TooFewArguments.Text();
        }
        byte[] data = Encoding.UTF8.GetBytes(args[1]);
        byte[]? hash = algo switch
        {
            "md5" => MD5.HashData(data),
            "md4" => ComputeBcDigest(new Org.BouncyCastle.Crypto.Digests.MD4Digest(), data),
            "sha1" or "sha0" => SHA1.HashData(data),
            "sha224" => ComputeBcDigest(new Org.BouncyCastle.Crypto.Digests.Sha224Digest(), data),
            "sha256" => SHA256.HashData(data),
            "sha384" => SHA384.HashData(data),
            "sha512" => SHA512.HashData(data),
            "ripemd160" => ComputeBcDigest(new Org.BouncyCastle.Crypto.Digests.RipeMD160Digest(), data),
            "whirlpool" => ComputeBcDigest(new Org.BouncyCastle.Crypto.Digests.WhirlpoolDigest(), data),
            _ => null
        };
        return hash != null ? Convert.ToHexStringLower(hash) : MushError.UnsupportedDigestType.Text();
    }

    private static byte[] ComputeBcDigest(Org.BouncyCastle.Crypto.IDigest digest, byte[] data)
    {
        digest.BlockUpdate(data, 0, data.Length);
        byte[] output = new byte[digest.GetDigestSize()];
        digest.DoFinal(output, 0);
        return output;
    }

    public static string Sha0(EvaluationContext ctx, string[] args)
        => args.Length == 0 ? string.Empty : Convert.ToHexStringLower(SHA1.HashData(Encoding.UTF8.GetBytes(args[0])));

    public static string Hmac(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3) return MushError.TooFewArguments.Text();
        string algo = args[0].Trim().ToLowerInvariant();
        byte[] key = Encoding.UTF8.GetBytes(args[1]);
        byte[] data = Encoding.UTF8.GetBytes(args[2]);
        string encoding = args.Length > 3 ? args[3].Trim().ToLowerInvariant() : "hex";

        byte[]? mac = algo switch
        {
            "md5" => HMACMD5.HashData(key, data),
            "sha1" => HMACSHA1.HashData(key, data),
            "sha256" => HMACSHA256.HashData(key, data),
            "sha384" => HMACSHA384.HashData(key, data),
            "sha512" => HMACSHA512.HashData(key, data),
            _ => null
        };
        if (mac == null) return MushError.Nothing.Text();
        return encoding == "base64" ? Convert.ToBase64String(mac) : Convert.ToHexStringLower(mac);
    }

    public static string Encrypt(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string text = args[0];
        string key = args[1];
        if (string.IsNullOrEmpty(key)) return text;

        bool encode64 = args.Length > 2 && LogicFunctions.IsTruthy(args[2]);
        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c is >= (char)32 and <= (char)126)
            {
                char k = key[i % key.Length];
                int kOff = (k is >= (char)32 and <= (char)126) ? (k - 32) : (k % 95);
                sb.Append((char)(32 + ((c - 32 + kOff) % 95)));
            }
            else
            {
                sb.Append(c);
            }
        }
        string result = sb.ToString();
        return encode64 ? Convert.ToBase64String(Encoding.UTF8.GetBytes(result)) : result;
    }

    public static string Decrypt(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string text = args[0];
        string key = args[1];
        if (string.IsNullOrEmpty(key)) return text;

        bool decode64 = args.Length > 2 && LogicFunctions.IsTruthy(args[2]);
        if (decode64)
        {
            try
            {
                text = Encoding.UTF8.GetString(Convert.FromBase64String(text.Trim()));
            }
            catch
            {
                return MushError.Nothing.Text();
            }
        }

        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c is >= (char)32 and <= (char)126)
            {
                char k = key[i % key.Length];
                int kOff = (k is >= (char)32 and <= (char)126) ? (k - 32) : (k % 95);
                sb.Append((char)(32 + ((c - 32 - kOff + 95) % 95)));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    public static string Ssl(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (reg.ConnectionsProvider == null || reg.Database == null) return "0";
        var targetId = ctx.Executor;
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            var p = reg.Database.GetObject(DbFunctions.ParseDbref(args[0], ctx))
                ?? reg.Database.FindPlayer(args[0].Trim().TrimStart('*'));
            if (p == null) return MushError.NotConnected.Text();
            targetId = p.Id;
        }
        var snap = reg.ConnectionsProvider().FirstOrDefault(s => s.PlayerId == targetId);
        if (snap == null) return MushError.NotConnected.Text();
        var actor = reg.Database.GetObject(ctx.Executor);
        if (snap.PlayerId != ctx.Executor && !PennMUSH.Core.Attributes.AttributePermissions.CanSeeAll(actor))
            return MushError.PermissionDenied.Text();
        return snap.IsSsl
            || snap.TerminalType.Contains("ssl", StringComparison.OrdinalIgnoreCase)
            || snap.TerminalType.Contains("tls", StringComparison.OrdinalIgnoreCase)
            ? "1"
            : "0";
    }

    public static string SqlEscape(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var actor = reg.Database?.GetObject(ctx.Executor);
        if (actor != null && !actor.IsWizard && !actor.HasPower("Sql_Ok"))
            return MushError.PermissionDenied.Text();
        if (args.Length == 0) return string.Empty;
        return args[0]
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\0", "\\0", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal);
    }
}
