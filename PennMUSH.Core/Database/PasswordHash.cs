namespace PennMUSH.Core.Database;

using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

/// <summary>
/// PennMUSH password hashes stored on <c>XYXXY</c>. Version 2 is
/// <c>2:sha512:</c>, two salt characters, the hex digest, and a unix time.
/// </summary>
public static class PasswordHash
{
    public const string AttributeName = "XYXXY";

    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private static readonly Regex Form = new(@"^(\d+):(\w+):([0-9a-zA-Z]+):(\d+)$", RegexOptions.CultureInvariant);

    public static string Create(string password)
    {
        string salt = Salt();
        string digest = Hex(SHA512.HashData(Encoding.UTF8.GetBytes(salt + password)));
        long stamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return $"2:sha512:{salt}{digest}:{stamp}";
    }

    public static string Generate()
    {
        int length = Random.Shared.Next(7, 13);
        var chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = Alphabet[Random.Shared.Next(Alphabet.Length)];
        return new string(chars);
    }

    public static bool Ok(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return false;
        foreach (char ch in password)
        {
            if (char.IsWhiteSpace(ch) || char.IsControl(ch) || ch == '=')
                return false;
        }
        return true;
    }

    public static bool Matches(string saved, string password)
    {
        var match = Form.Match(saved);
        if (!match.Success)
            return saved == password;

        string version = match.Groups[1].Value;
        string algo = match.Groups[2].Value;
        string hashed = match.Groups[3].Value;
        byte[] digest = Digest(algo, version == "2" && hashed.Length >= 2
            ? hashed[..2] + password
            : password);
        if (digest.Length == 0)
            return false;
        string hex = version == "2" && hashed.Length >= 2 ? hashed[..2] + Hex(digest) : Hex(digest);
        return hex == hashed;
    }

    public static bool Accepts(MUSHObject player, string password, string? memory)
    {
        string? saved = player.GetAttribute(AttributeName)?.Value;
        if (!string.IsNullOrEmpty(saved))
            return Matches(saved, password);
        if (memory != null)
            return memory == password;
        return true;
    }

    public static void Store(MUSHObject player, string password)
    {
        player.SetAttribute(
            AttributeName,
            Create(password),
            new Dbref(1),
            StandardAttributes.FlagsOf(AttributeName),
            wizardOverride: true);
    }

    private static string Salt()
        => new(new[] { Alphabet[Random.Shared.Next(Alphabet.Length)], Alphabet[Random.Shared.Next(Alphabet.Length)] });

    private static byte[] Digest(string algo, string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        return algo.ToLowerInvariant() switch
        {
            "sha512" => SHA512.HashData(bytes),
            "sha1" => SHA1.HashData(bytes),
            "sha256" => SHA256.HashData(bytes),
            "md5" => MD5.HashData(bytes),
            _ => Array.Empty<byte>()
        };
    }

    private static string Hex(byte[] bytes)
    {
        var text = new StringBuilder(bytes.Length * 2);
        foreach (byte value in bytes)
            text.Append(value.ToString("x2"));
        return text.ToString();
    }
}
