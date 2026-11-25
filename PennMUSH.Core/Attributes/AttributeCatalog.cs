namespace PennMUSH.Core.Attributes;

using System.Text.RegularExpressions;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public static partial class StandardAttributes
{
    private sealed class Restriction
    {
        public bool Enum { get; init; }
        public string Stored { get; init; } = string.Empty;
    }

    private static readonly Dictionary<string, Restriction> Restrictions = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Dbref> Creators = new(StringComparer.Ordinal);

    private static readonly (string Name, AttributeFlags Bit)[] ViewNames =
    {
        ("no_command", AttributeFlags.NoProg),
        ("no_inherit", AttributeFlags.Private),
        ("no_clone", AttributeFlags.NoCopy),
        ("wizard", AttributeFlags.Wizard),
        ("visual", AttributeFlags.Visual),
        ("mortal_dark", AttributeFlags.MDark),
        ("regexp", AttributeFlags.Regexp),
        ("case", AttributeFlags.Case),
        ("locked", AttributeFlags.Locked),
        ("safe", AttributeFlags.Safe),
        ("internal", AttributeFlags.Internal),
        ("prefixmatch", AttributeFlags.PrefixMatch),
        ("veiled", AttributeFlags.Veiled),
        ("debug", AttributeFlags.Debug),
        ("no_debug", AttributeFlags.NoDebug),
        ("public", AttributeFlags.Public),
        ("nearby", AttributeFlags.Nearby),
        ("noname", AttributeFlags.NoName),
        ("nospace", AttributeFlags.NoSpace),
        ("amhear", AttributeFlags.MHear),
        ("aahear", AttributeFlags.AHear),
        ("quiet", AttributeFlags.Quiet),
        ("branch", AttributeFlags.Root),
    };

    public static bool ExistsExact(string name)
        => ByName.ContainsKey(Normalize(name));

    public static bool TryLookup(string name, out string canonical)
    {
        name = name.Trim();
        if (name.StartsWith('@'))
            name = name[1..];
        return TryResolve(name, out canonical);
    }

    public static AttributeFlags FlagsOf(string canonical)
        => ByName.TryGetValue(canonical, out var found) ? found.Flags : AttributeFlags.None;

    public static bool IsInternal(string canonical)
        => FlagsOf(canonical).HasFlag(AttributeFlags.Internal);

    public static Dbref CreatorOf(string canonical)
        => Creators.TryGetValue(canonical, out var creator) ? creator : new Dbref(1);

    public static string DescribeFlags(AttributeFlags flags)
        => string.Join(' ', ViewNames.Where(entry => flags.HasFlag(entry.Bit)).Select(entry => entry.Name));

    public static bool TryParseFlags(string text, out AttributeFlags flags)
    {
        flags = AttributeFlags.None;
        if (text.Equals("none", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (string token in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Equals("branch", StringComparison.OrdinalIgnoreCase))
                continue;
            var match = SetNames.FirstOrDefault(entry => entry.Name.Equals(token, StringComparison.OrdinalIgnoreCase));
            if (match.Name == null)
                return false;
            flags |= match.Bit;
        }

        return flags != AttributeFlags.None || text.Equals("none", StringComparison.OrdinalIgnoreCase);
    }

    public static void SetAccess(string canonical, AttributeFlags flags, Dbref creator)
    {
        if (ByName.TryGetValue(canonical, out var existing))
            canonical = existing.Name;
        AttributeFlags combined = flags | (FlagsOf(canonical) & (AttributeFlags.RLimit | AttributeFlags.Enum));
        if (!ByName.ContainsKey(canonical))
            ByName[canonical] = new Definition(canonical, combined);
        foreach (string key in ByName.Keys.Where(key => key == canonical || ByName[key].Name == canonical).ToList())
        {
            string name = ByName[key].Name;
            ByName[key] = new Definition(name.Length == 0 ? canonical : name, combined);
        }
        Creators[canonical] = creator;
    }

    public static int Delete(string name)
    {
        name = Normalize(name);
        if (!ByName.TryGetValue(name, out var found))
            return 0;
        string canonical = found.Name;
        var keys = ByName.Keys.Where(key => key == canonical || ByName[key].Name == canonical).ToList();
        foreach (string key in keys)
            ByName.Remove(key);
        Restrictions.Remove(canonical);
        Creators.Remove(canonical);
        return keys.Count;
    }

    public static bool Rename(string oldName, string newName)
    {
        oldName = Normalize(oldName);
        newName = Normalize(newName);
        if (!ByName.TryGetValue(oldName, out var found))
            return false;
        string canonical = found.Name;
        var definition = ByName[oldName];
        ByName.Remove(oldName);
        ByName[newName] = new Definition(oldName == canonical ? newName : canonical, definition.Flags);
        if (oldName == canonical)
        {
            foreach (string key in ByName.Keys.Where(key => ByName[key].Name == canonical).ToList())
            {
                var current = ByName[key];
                ByName[key] = new Definition(newName, current.Flags);
            }
            if (Restrictions.Remove(canonical, out var restriction))
                Restrictions[newName] = restriction;
            if (Creators.Remove(canonical, out var creator))
                Creators[newName] = creator;
        }
        return true;
    }

    public static bool TryCompileLimit(string pattern)
    {
        try
        {
            _ = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static bool TrySetRestriction(string name, string? pattern, bool isEnum, char delim, out string message)
    {
        name = Normalize(name);
        if (!ByName.TryGetValue(name, out var found))
        {
            message = "I don't know that attribute. Please use @attribute/access to create it, first.";
            return false;
        }

        string canonical = found.Name;
        if (IsInternal(canonical))
        {
            message = "That attribute's permissions cannot be changed.";
            return false;
        }

        bool unset = string.IsNullOrEmpty(pattern);
        bool had = Restrictions.ContainsKey(canonical);
        ClearRestrictionBits(canonical);
        if (unset)
        {
            Restrictions.Remove(canonical);
            message = had
                ? $"{canonical} -- Attribute limit or enum unset."
                : $"{canonical} -- Attribute limit or enum already unset.";
            return true;
        }

        string stored = isEnum ? EnumStored(delim, pattern!) : pattern!;
        Restrictions[canonical] = new Restriction { Enum = isEnum, Stored = stored };
        AttributeFlags bit = isEnum ? AttributeFlags.Enum : AttributeFlags.RLimit;
        Stamp(canonical, FlagsOf(canonical) | bit);
        string shown = isEnum ? DisplayEnum(stored) : stored;
        message = $"{canonical} -- Attribute {(isEnum ? "enum" : "limit")} set to: {shown}";
        return true;
    }

    public static bool HasRestriction(string canonical, out bool isEnum, out string shown)
    {
        if (Restrictions.TryGetValue(canonical, out var restriction))
        {
            isEnum = restriction.Enum;
            shown = restriction.Enum ? DisplayEnum(restriction.Stored) : restriction.Stored;
            return true;
        }

        isEnum = false;
        shown = string.Empty;
        return false;
    }

    public static IEnumerable<string> CanonicalNames()
    {
        foreach (var pair in ByName)
        {
            if (string.Equals(pair.Key, pair.Value.Name, StringComparison.Ordinal))
                yield return pair.Value.Name;
        }
    }

    public static IEnumerable<string> Decompile(string pattern, bool retroactive)
    {
        foreach (string name in ByName.Keys.OrderBy(key => key, StringComparer.Ordinal))
        {
            var definition = ByName[name];
            if (!string.Equals(name, definition.Name, StringComparison.Ordinal))
                continue;
            if (!Wild(pattern, name))
                continue;
            string flags = DescribeFlags(definition.Flags & ~(AttributeFlags.RLimit | AttributeFlags.Enum));
            yield return $"@attribute/access{(retroactive ? "/retroactive" : "")} {name}={flags}";
            if (!HasRestriction(name, out bool isEnum, out string shown))
                continue;
            yield return isEnum
                ? $"@attribute/enum {name}={shown}"
                : $"@attribute/limit {name}={shown}";
        }
    }

    public static void ApplyRetroactive(ObjectDatabase db, string canonical, AttributeFlags flags, Dbref creator)
    {
        foreach (var obj in db.GetAllObjects())
        {
            var attr = obj.GetAttribute(canonical);
            if (attr == null)
                continue;
            attr.Flags = attr.Flags.HasFlag(AttributeFlags.Root) ? flags | AttributeFlags.Root : flags;
            attr.Creator = creator;
        }
    }

    public static bool AcceptsValue(string name, string value, out string stored, out string error)
    {
        stored = value;
        error = string.Empty;
        name = Normalize(name);
        if (!ByName.TryGetValue(name, out var found))
            return true;
        if (!Restrictions.TryGetValue(found.Name, out var restriction))
            return true;

        if (!restriction.Enum)
        {
            try
            {
                var regex = new Regex(restriction.Stored, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));
                if (regex.IsMatch(value))
                    return true;
            }
            catch (RegexMatchTimeoutException)
            {
            }
            catch (ArgumentException)
            {
                return true;
            }

            error = "Attribute value does not match the /limit regexp.";
            return false;
        }

        if (restriction.Stored.Length == 0)
            return true;
        char delim = restriction.Stored[0];
        if (value.Length == 0 || value.Contains(delim))
        {
            error = $"Value for {found.Name} needs to be one of: {DisplayEnum(restriction.Stored)}";
            return false;
        }

        string upper = restriction.Stored.ToUpperInvariant();
        string probe = $"{delim}{value}{delim}".ToUpperInvariant();
        int at = upper.IndexOf(probe, StringComparison.Ordinal);
        if (at < 0)
        {
            probe = $"{delim}{value}".ToUpperInvariant();
            at = upper.IndexOf(probe, StringComparison.Ordinal);
            if (at >= 0 && at + probe.Length < upper.Length && upper[at + probe.Length] != delim && !probe.EndsWith(delim))
                at = -1;
        }

        if (at < 0)
        {
            error = $"Value for {found.Name} needs to be one of: {DisplayEnum(restriction.Stored)}";
            return false;
        }

        int start = at + 1;
        int end = restriction.Stored.IndexOf(delim, start);
        if (end < 0)
            end = restriction.Stored.Length;
        stored = restriction.Stored[start..end];
        return true;
    }

    public static bool TryFindSetFlag(string name, out AttributeFlags bit)
    {
        bit = AttributeFlags.None;
        if (string.IsNullOrWhiteSpace(name))
            return false;
        string trimmed = name.Trim();
        foreach (var (flagName, flagBit) in SetNames)
        {
            if (flagName.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
            {
                bit = flagBit;
                return true;
            }
        }
        return false;
    }

    private static readonly (string Name, AttributeFlags Bit)[] SetNames =
    {
        ("no_command", AttributeFlags.NoProg),
        ("no_inherit", AttributeFlags.Private),
        ("private", AttributeFlags.Private),
        ("no_clone", AttributeFlags.NoCopy),
        ("wizard", AttributeFlags.Wizard),
        ("visual", AttributeFlags.Visual),
        ("mortal_dark", AttributeFlags.MDark),
        ("hidden", AttributeFlags.MDark),
        ("regexp", AttributeFlags.Regexp),
        ("case", AttributeFlags.Case),
        ("locked", AttributeFlags.Locked),
        ("safe", AttributeFlags.Safe),
        ("prefixmatch", AttributeFlags.PrefixMatch),
        ("veiled", AttributeFlags.Veiled),
        ("debug", AttributeFlags.Debug),
        ("no_debug", AttributeFlags.NoDebug),
        ("public", AttributeFlags.Public),
        ("nearby", AttributeFlags.Nearby),
        ("noname", AttributeFlags.NoName),
        ("no_name", AttributeFlags.NoName),
        ("nospace", AttributeFlags.NoSpace),
        ("no_space", AttributeFlags.NoSpace),
        ("amhear", AttributeFlags.MHear),
        ("aahear", AttributeFlags.AHear),
        ("quiet", AttributeFlags.Quiet),
    };

    private static void ClearRestrictionBits(string canonical)
        => Stamp(canonical, FlagsOf(canonical) & ~(AttributeFlags.RLimit | AttributeFlags.Enum));

    private static void Stamp(string canonical, AttributeFlags flags)
    {
        foreach (string key in ByName.Keys.Where(key => key == canonical || ByName[key].Name == canonical).ToList())
            ByName[key] = new Definition(ByName[key].Name, flags);
    }

    private static string EnumStored(char delim, string pattern)
    {
        var text = new System.Text.StringBuilder();
        text.Append(delim);
        text.Append(pattern);
        text.Append(delim);
        for (int i = text.Length - 1; i > 0; i--)
        {
            if (text[i] == delim && text[i - 1] == delim)
                text.Remove(i, 1);
        }
        return text.ToString();
    }

    private static string DisplayEnum(string stored)
        => stored.Length >= 2 ? stored[1..^1] : stored;

    private static string Normalize(string name)
    {
        name = name.Trim();
        if (name.StartsWith('@'))
            name = name[1..];
        return name.ToUpperInvariant();
    }

    private static bool Wild(string pattern, string name)
    {
        pattern = pattern.Trim();
        if (pattern.Length == 0)
            return true;
        int pi = 0;
        int ni = 0;
        int star = -1;
        int mark = -1;
        while (ni < name.Length)
        {
            if (pi < pattern.Length && (pattern[pi] == '?' || char.ToUpperInvariant(pattern[pi]) == char.ToUpperInvariant(name[ni])))
            {
                pi++;
                ni++;
            }
            else if (pi < pattern.Length && pattern[pi] == '*')
            {
                star = pi++;
                mark = ni;
            }
            else if (star >= 0)
            {
                pi = star + 1;
                ni = ++mark;
            }
            else
            {
                return false;
            }
        }

        while (pi < pattern.Length && pattern[pi] == '*')
            pi++;
        return pi == pattern.Length;
    }
}
