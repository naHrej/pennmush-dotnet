namespace PennMUSH.Core.Commands;

using System;
using System.Collections.Generic;
using System.Linq;

public enum CommandLookupStatus
{
    NotFound,
    Found,
    Ambiguous,
    Reserved
}

public record CommandLookupResult(
    CommandLookupStatus Status,
    ICommandHandler? Handler = null,
    string? CanonicalName = null,
    string? MatchedName = null);

public class CommandTable
{
    private class TrieNode
    {
        public Dictionary<char, TrieNode> Children { get; } = new();
        public ICommandHandler? Handler { get; set; }
        public string? CanonicalName { get; set; }
        public bool IsPrimary { get; set; }
    }

    private readonly TrieNode _root = new();
    private readonly Dictionary<string, (ICommandHandler Handler, string CanonicalName)> _exactCommands = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _reservedAliases = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ICommandHandler handler, params string[] aliases)
    {
        string primary = handler.Name.ToLowerInvariant();
        _exactCommands[primary] = (handler, primary);
        InsertTrie(primary, handler, primary, isPrimary: true);

        foreach (var alias in aliases)
        {
            AddAlias(primary, alias);
        }
    }

    public bool AddAlias(string originalCommand, string alias)
    {
        string orig = originalCommand.ToLowerInvariant();
        string al = alias.ToLowerInvariant();

        if (!_exactCommands.TryGetValue(orig, out var entry))
        {
            return false;
        }

        _exactCommands[al] = (entry.Handler, entry.CanonicalName);
        InsertTrie(al, entry.Handler, entry.CanonicalName, isPrimary: false);
        return true;
    }

    public bool Unregister(string commandOrAlias)
    {
        string normalized = commandOrAlias.Trim().ToLowerInvariant();
        if (!_exactCommands.TryGetValue(normalized, out var entry))
        {
            return false;
        }

        if (string.Equals(entry.CanonicalName, normalized, StringComparison.OrdinalIgnoreCase))
        {
            var keysToRemove = _exactCommands
                .Where(kv => string.Equals(kv.Value.CanonicalName, normalized, StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _exactCommands.Remove(key);
            }
        }
        else
        {
            _exactCommands.Remove(normalized);
        }

        RebuildTrie();
        return true;
    }

    private void RebuildTrie()
    {
        _root.Children.Clear();
        _root.Handler = null;
        _root.CanonicalName = null;

        foreach (var (name, (handler, canonical)) in _exactCommands)
        {
            bool isPrimary = string.Equals(name, canonical, StringComparison.OrdinalIgnoreCase);
            InsertTrie(name, handler, canonical, isPrimary);
        }
    }

    public void ReserveAlias(string alias)
    {
        if (!string.IsNullOrWhiteSpace(alias))
        {
            _reservedAliases.Add(alias.Trim().ToLowerInvariant());
        }
    }

    public bool IsReserved(string alias)
    {
        return _reservedAliases.Contains(alias.Trim().ToLowerInvariant());
    }

    public IEnumerable<string> PrimaryNames =>
        _exactCommands
            .Where(pair => pair.Key.Equals(pair.Value.CanonicalName, StringComparison.OrdinalIgnoreCase))
            .Select(pair => pair.Key);

    public CommandLookupResult Lookup(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new CommandLookupResult(CommandLookupStatus.NotFound);
        }

        string normalized = input.Trim().ToLowerInvariant();

        // 1. Reserved aliases (e.g. 'n', 's', 'e', 'w' for exits)
        if (IsReserved(normalized))
        {
            return new CommandLookupResult(CommandLookupStatus.Reserved, MatchedName: normalized);
        }

        // 2. Exact match has priority over prefixes
        if (_exactCommands.TryGetValue(normalized, out var exact))
        {
            return new CommandLookupResult(CommandLookupStatus.Found, exact.Handler, exact.CanonicalName, normalized);
        }

        // 3. Prefix search in Trie
        var node = _root;
        foreach (char c in normalized)
        {
            if (!node.Children.TryGetValue(c, out var next))
            {
                return new CommandLookupResult(CommandLookupStatus.NotFound);
            }
            node = next;
        }

        // Collect all distinct primary command handlers in subtree
        var matches = new Dictionary<string, (ICommandHandler Handler, string CanonicalName)>(StringComparer.OrdinalIgnoreCase);
        CollectMatches(node, matches);

        if (matches.Count == 1)
        {
            var single = matches.First();
            return new CommandLookupResult(CommandLookupStatus.Found, single.Value.Handler, single.Value.CanonicalName, single.Key);
        }
        else if (matches.Count > 1)
        {
            return new CommandLookupResult(CommandLookupStatus.Ambiguous, MatchedName: normalized);
        }

        return new CommandLookupResult(CommandLookupStatus.NotFound);
    }

    private void InsertTrie(string name, ICommandHandler handler, string canonical, bool isPrimary)
    {
        var node = _root;
        foreach (char c in name)
        {
            if (!node.Children.TryGetValue(c, out var child))
            {
                child = new TrieNode();
                node.Children[c] = child;
            }
            node = child;
        }

        node.Handler = handler;
        node.CanonicalName = canonical;
        node.IsPrimary = isPrimary;
    }

    private void CollectMatches(TrieNode node, Dictionary<string, (ICommandHandler Handler, string CanonicalName)> matches)
    {
        if (node.Handler != null && node.CanonicalName != null)
        {
            matches[node.CanonicalName] = (node.Handler, node.CanonicalName);
        }

        foreach (var child in node.Children.Values)
        {
            CollectMatches(child, matches);
        }
    }
}
