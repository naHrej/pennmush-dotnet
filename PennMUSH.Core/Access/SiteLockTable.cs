namespace PennMUSH.Core.Access;

using System;
using System.Collections.Generic;
using System.Text;

public sealed class SiteRule
{
    public required string Host { get; init; }
    public uint Can { get; init; }
    public uint Cant { get; init; }
    public bool Marker { get; init; }
}

/// <summary>
/// The in-memory ban table consulted at login. Patterns use * and ? and ignore case.
/// The first matching rule decides.
/// </summary>
public sealed class SiteLockTable
{
    public const uint Connect = 0x1;
    public const uint Create = 0x2;
    public const uint Guest = 0x4;
    public const uint Register = 0x8;
    public const uint MarkerFlag = 0x10;
    public const uint Default = Connect | Create | Guest;

    private readonly List<SiteRule> _rules = new();

    public IReadOnlyList<SiteRule> Rules => _rules;

    public bool Can(string host, uint flag)
    {
        if (string.IsNullOrEmpty(host))
            return true;

        foreach (var rule in _rules)
        {
            if (rule.Marker || !Matches(rule.Host, host))
                continue;
            if (rule.Cant != 0 && (rule.Cant & flag) == flag)
                return false;
            if (rule.Can != 0 && (rule.Can & flag) != 0)
                return true;
            break;
        }

        return flag is Connect or Create or Guest;
    }

    public void Add(string host, uint can, uint cant)
    {
        int marker = _rules.FindIndex(rule => rule.Marker);
        var rule = new SiteRule { Host = host, Can = can, Cant = cant };
        if (marker < 0)
        {
            _rules.Add(new SiteRule { Host = "@sitelock", Can = MarkerFlag, Marker = true });
            _rules.Add(rule);
            return;
        }
        _rules.Insert(marker + 1, rule);
    }

    public int Remove(string pattern)
    {
        int marker = _rules.FindIndex(rule => rule.Marker);
        if (marker < 0)
            return 0;

        int removed = 0;
        if (int.TryParse(pattern, out int ruleNumber) && ruleNumber > 0)
        {
            int index = ruleNumber - 1;
            if (index > marker && index < _rules.Count)
            {
                _rules.RemoveAt(index);
                removed = 1;
            }
            return removed;
        }

        for (int i = _rules.Count - 1; i > marker; i--)
        {
            if (_rules[i].Host.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            {
                _rules.RemoveAt(i);
                removed++;
            }
        }
        return removed;
    }

    public (SiteRule? Rule, int Number) Check(string host)
    {
        for (int i = 0; i < _rules.Count; i++)
        {
            var rule = _rules[i];
            if (!rule.Marker && Matches(rule.Host, host))
                return (rule, i + 1);
        }
        return (null, 0);
    }

    public IEnumerable<string> ListLines()
    {
        if (_rules.Count == 0)
        {
            yield return "There are no access rules.";
            yield break;
        }

        for (int i = 0; i < _rules.Count; i++)
        {
            var rule = _rules[i];
            if (rule.Marker)
            {
                yield return "---- @sitelock will add sites immediately below this line ----";
                continue;
            }
            yield return $"{i + 1,3} SITE: {rule.Host.PadRight(20)}  DBREF: {"#-2".PadRight(6)} FLAGS:{Flags(rule)}";
            yield return " COMMENT: ";
        }
    }

    public static string Describe(SiteRule rule, int number)
    {
        var text = new StringBuilder();
        text.Append("Matched line ").Append(number).Append(": ").Append(rule.Host).Append(' ');
        text.Append('\n');
        text.Append("Comment: ");
        text.Append('\n');
        text.Append("Connections allowed by: ");
        if ((rule.Cant & Connect) != 0)
            text.Append("No one");
        else
            text.Append("All");
        text.Append('\n');
        text.Append((rule.Cant & Guest) != 0
            ? "Guest connections are NOT allowed"
            : "Guest connections are allowed");
        text.Append('\n');
        text.Append((rule.Cant & Create) != 0
            ? "Creation is NOT allowed"
            : "Creation is allowed");
        return text.ToString();
    }

    public static bool Matches(string pattern, string host)
    {
        return Match(pattern, 0, host, 0);
    }

    private static string Flags(SiteRule rule)
    {
        var text = new StringBuilder();
        Append(text, "connect", Connect, toggle: true);
        Append(text, "create", Create, toggle: true);
        Append(text, "guest", Guest, toggle: true);
        Append(text, "register", Register, toggle: false);
        return text.ToString();

        void Append(StringBuilder buffer, string name, uint flag, bool toggle)
        {
            if ((rule.Can & flag) != 0)
                buffer.Append(' ').Append(name);
            if (toggle && (rule.Cant & flag) != 0)
                buffer.Append(" !").Append(name);
        }
    }

    private static bool Match(string pattern, int patternIndex, string host, int hostIndex)
    {
        while (patternIndex < pattern.Length)
        {
            char token = pattern[patternIndex];
            if (token == '*')
            {
                if (patternIndex == pattern.Length - 1)
                    return true;
                for (int i = hostIndex; i <= host.Length; i++)
                {
                    if (Match(pattern, patternIndex + 1, host, i))
                        return true;
                }
                return false;
            }
            if (hostIndex >= host.Length)
                return false;
            if (token != '?' && char.ToLowerInvariant(token) != char.ToLowerInvariant(host[hostIndex]))
                return false;
            patternIndex++;
            hostIndex++;
        }
        return hostIndex == host.Length;
    }
}
