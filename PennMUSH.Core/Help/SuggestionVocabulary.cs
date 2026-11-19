namespace PennMUSH.Core.Help;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public sealed class SuggestionVocabulary
{
    private const int MaxCategoryWords = 500000;
    private readonly object _sync = new();
    private readonly Dictionary<string, HashSet<string>> _words = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _cachedWords = new(StringComparer.Ordinal);

    public bool Add(string category, string word)
    {
        string cat = category.ToUpperInvariant();
        string token = word.ToLowerInvariant();
        lock (_sync)
        {
            if (!_words.TryGetValue(cat, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                _words[cat] = set;
            }

            if (!set.Add(token))
                return false;
            if (set.Count > MaxCategoryWords)
            {
                set.Remove(token);
                return false;
            }

            return true;
        }
    }

    public bool Delete(string category, string word)
    {
        string cat = category.ToUpperInvariant();
        string token = word.ToLowerInvariant();
        lock (_sync)
        {
            if (_words.TryGetValue(cat, out var set))
                set.Remove(token);
            if (_cachedWords.TryGetValue(cat, out var cached))
                cached.Remove(token);
            return true;
        }
    }

    public bool HasCategory(string category)
    {
        string cat = category.Trim().ToUpperInvariant();
        lock (_sync)
        {
            return _words.ContainsKey(cat)
                || (_cachedWords.TryGetValue(cat, out var cached) && cached.Count > 0);
        }
    }

    public IReadOnlyList<string> GetWords(string category)
    {
        string cat = category.Trim().ToUpperInvariant();
        lock (_sync)
        {
            var merged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_words.TryGetValue(cat, out var manual))
            {
                foreach (string word in manual)
                    merged.Add(word);
            }
            if (_cachedWords.TryGetValue(cat, out var cached))
            {
                foreach (string word in cached)
                    merged.Add(word);
            }
            return merged.ToList();
        }
    }

    public void ReplaceCachedCategory(string category, IEnumerable<string> words)
    {
        string cat = category.Trim().ToUpperInvariant();
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (string raw in words)
        {
            string token = raw.Trim().ToLowerInvariant();
            if (token.Length == 0)
                continue;
            set.Add(token);
            if (set.Count >= MaxCategoryWords)
                break;
        }

        lock (_sync)
        {
            if (set.Count == 0)
                _cachedWords.Remove(cat);
            else
                _cachedWords[cat] = set;
        }
    }

    public void LoadCachedWordFile(string category, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            ReplaceCachedCategory(category, Array.Empty<string>());
            return;
        }

        ReplaceCachedCategory(
            category,
            File.ReadLines(filePath)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0));
    }

    public IReadOnlyList<string> Categories()
    {
        lock (_sync)
        {
            return _words.Keys
                .Concat(_cachedWords.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
        }
    }

    public string? Closest(string category, string typed, int maxDistance = 2)
    {
        string token = typed.Trim().ToLowerInvariant();
        if (token.Length == 0)
            return null;

        List<string> candidates;
        string cat = category.ToUpperInvariant();
        lock (_sync)
        {
            var merged = new HashSet<string>(StringComparer.Ordinal);
            if (_words.TryGetValue(cat, out var manual))
            {
                foreach (string word in manual)
                    merged.Add(word);
            }
            if (_cachedWords.TryGetValue(cat, out var cached))
            {
                foreach (string word in cached)
                    merged.Add(word);
            }
            if (merged.Count == 0)
                return null;
            candidates = merged.OrderBy(name => name, StringComparer.Ordinal).ToList();
        }

        string? best = null;
        int bestDistance = maxDistance + 1;
        foreach (string word in candidates)
        {
            int distance = EditDistance(token, word, bestDistance - 1);
            if (distance == 0 || distance >= bestDistance)
                continue;
            best = word;
            bestDistance = distance;
        }

        return best;
    }

    private static int EditDistance(string left, string right, int cap)
    {
        if (Math.Abs(left.Length - right.Length) > cap)
            return cap + 1;

        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];
        for (int column = 0; column <= right.Length; column++)
            previous[column] = column;

        for (int row = 1; row <= left.Length; row++)
        {
            current[0] = row;
            int bestInRow = current[0];
            for (int column = 1; column <= right.Length; column++)
            {
                int cost = left[row - 1] == right[column - 1] ? 0 : 1;
                current[column] = Math.Min(
                    Math.Min(current[column - 1] + 1, previous[column] + 1),
                    previous[column - 1] + cost);
                if (current[column] < bestInRow)
                    bestInRow = current[column];
            }

            if (bestInRow > cap)
                return cap + 1;
            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }
}
