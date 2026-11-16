namespace PennMUSH.Core.Help;

using System;
using System.Collections.Generic;
using System.Linq;

public sealed class SuggestionVocabulary
{
    private readonly Dictionary<string, HashSet<string>> _words = new(StringComparer.Ordinal);

    public bool Add(string category, string word)
    {
        string cat = category.ToUpperInvariant();
        string token = word.ToLowerInvariant();
        if (!_words.TryGetValue(cat, out var set))
        {
            set = new HashSet<string>(StringComparer.Ordinal);
            _words[cat] = set;
        }

        if (!set.Add(token))
            return false;
        if (set.Count > 500000)
        {
            set.Remove(token);
            return false;
        }

        return true;
    }

    public bool Delete(string category, string word)
    {
        string cat = category.ToUpperInvariant();
        string token = word.ToLowerInvariant();
        if (_words.TryGetValue(cat, out var set))
            set.Remove(token);
        return true;
    }

    public IReadOnlyList<string> Categories()
        => _words.Keys.OrderBy(name => name, StringComparer.Ordinal).ToList();

    public string? Closest(string category, string typed, int maxDistance = 2)
    {
        string token = typed.Trim().ToLowerInvariant();
        if (token.Length == 0 || !_words.TryGetValue(category.ToUpperInvariant(), out var set))
            return null;

        string? best = null;
        int bestDistance = maxDistance + 1;
        foreach (string word in set.OrderBy(name => name, StringComparer.Ordinal))
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
