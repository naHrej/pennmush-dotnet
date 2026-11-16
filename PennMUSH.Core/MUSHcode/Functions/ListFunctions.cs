namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Queue;

public static class ListFunctions
{
    private static readonly Random _random = new();

    public static void Register(FunctionRegistry registry)
    {
        registry.Register("FIRST", (ctx, args) => First(args));
        registry.Register("REST", (ctx, args) => Rest(args));
        registry.Register("LAST", (ctx, args) => Last(args));
        registry.Register("WORDS", (ctx, args) => Words(args));
        registry.Register("MATCH", (ctx, args) => Match(args));
        registry.Register("MATCHALL", (ctx, args) => MatchAll(args));
        registry.Register("GRAB", (ctx, args) => Grab(args));
        registry.Register("GRABALL", (ctx, args) => GrabAll(args));
        registry.Register("MEMBER", (ctx, args) => Member(args));
        registry.Register("EXTRACT", (ctx, args) => Extract(args));
        registry.Register("INDEX", (ctx, args) => Index(args));
        registry.Register("ITEMS", (ctx, args) => Items(args));
        registry.Register("ELEMENTS", (ctx, args) => Elements(args));
        registry.Register("INSERT", (ctx, args) => Insert(args));
        registry.Register("REPLACE", (ctx, args) => Replace(args));
        registry.Register("LDELETE", (ctx, args) => LDelete(args));
        registry.Register("REMOVE", (ctx, args) => Remove(args));
        registry.Register("REVWORDS", (ctx, args) => RevWords(args));
        registry.Register("REVERSE", (ctx, args) => RevWords(args));
        registry.Register("UNIQUE", (ctx, args) => Unique(args));
        registry.Register("SHUFFLE", (ctx, args) => Shuffle(args));
        registry.Register("SORT", (ctx, args) => Sort(args));
        registry.Register("SORTBY", (ctx, args) => SortBy(registry, ctx, args));
        registry.Register("SETUNION", (ctx, args) => SetUnion(args));
        registry.Register("SETINTER", (ctx, args) => SetInter(args));
        registry.Register("SETDIFF", (ctx, args) => SetDiff(args));
        registry.Register("LNUM", (ctx, args) => LNum(args));
        registry.Register("ITER", (ctx, args) => Iter(registry, ctx, args));
        registry.Register("MAP", (ctx, args) => Map(registry, ctx, args));
        registry.Register("FILTER", (ctx, args) => Filter(registry, ctx, args));
        registry.Register("FOLD", (ctx, args) => Fold(registry, ctx, args));
        registry.Register("ITEMIZE", (ctx, args) => Itemize(args));
        registry.Register("SPLICE", (ctx, args) => Splice(args));
        registry.Register("STEP", (ctx, args) => Step(args));
        registry.Register("MUNGE", (ctx, args) => Munge(registry, ctx, args));
    }

    public static List<string> SplitList(string input, string delim = " ")
    {
        if (string.IsNullOrEmpty(input)) return new List<string>();
        if (string.IsNullOrEmpty(delim) || delim == " ")
        {
            return input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
        return input.Split(delim).ToList();
    }

    public static string First(string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string delim = args.Length > 1 ? args[1] : " ";
        var list = SplitList(args[0], delim);
        return list.Count > 0 ? list[0] : string.Empty;
    }

    public static string Rest(string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string delim = args.Length > 1 ? args[1] : " ";
        var list = SplitList(args[0], delim);
        if (list.Count <= 1) return string.Empty;
        return string.Join(delim, list.Skip(1));
    }

    public static string Last(string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string delim = args.Length > 1 ? args[1] : " ";
        var list = SplitList(args[0], delim);
        return list.Count > 0 ? list[^1] : string.Empty;
    }

    public static string Words(string[] args)
    {
        if (args.Length == 0) return "0";
        string delim = args.Length > 1 ? args[1] : " ";
        return SplitList(args[0], delim).Count.ToString();
    }

    public static string Match(string[] args)
    {
        if (args.Length < 2) return "0";
        string delim = args.Length > 2 ? args[2] : " ";
        var list = SplitList(args[0], delim);
        string pattern = args[1];

        for (int i = 0; i < list.Count; i++)
        {
            if (PatternMatcher.Match(pattern, list[i], out _))
            {
                return (i + 1).ToString();
            }
        }
        return "0";
    }

    public static string MatchAll(string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string delim = args.Length > 2 ? args[2] : " ";
        var list = SplitList(args[0], delim);
        string pattern = args[1];
        var matches = new List<string>();

        for (int i = 0; i < list.Count; i++)
        {
            if (PatternMatcher.Match(pattern, list[i], out _))
            {
                matches.Add((i + 1).ToString());
            }
        }
        return string.Join(" ", matches);
    }

    public static string Grab(string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string delim = args.Length > 2 ? args[2] : " ";
        var list = SplitList(args[0], delim);
        string pattern = args[1];

        foreach (var item in list)
        {
            if (PatternMatcher.Match(pattern, item, out _))
            {
                return item;
            }
        }
        return string.Empty;
    }

    public static string GrabAll(string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string delim = args.Length > 2 ? args[2] : " ";
        var list = SplitList(args[0], delim);
        string pattern = args[1];
        var matches = list.Where(item => PatternMatcher.Match(pattern, item, out _));
        return string.Join(delim, matches);
    }

    public static string Member(string[] args)
    {
        if (args.Length < 2) return "0";
        string delim = args.Length > 2 ? args[2] : " ";
        var list = SplitList(args[0], delim);
        string target = args[1];

        for (int i = 0; i < list.Count; i++)
        {
            if (string.Equals(list[i], target, StringComparison.OrdinalIgnoreCase))
            {
                return (i + 1).ToString();
            }
        }
        return "0";
    }

    public static string Extract(string[] args)
    {
        if (args.Length < 3) return string.Empty;
        string delim = args.Length > 3 ? args[3] : " ";
        var list = SplitList(args[0], delim);

        if (!int.TryParse(args[1].Trim(), out int start) || start <= 0) return string.Empty;
        if (!int.TryParse(args[2].Trim(), out int count) || count <= 0) return string.Empty;

        int index = start - 1;
        if (index >= list.Count) return string.Empty;
        if (index + count > list.Count) count = list.Count - index;

        return string.Join(delim, list.Skip(index).Take(count));
    }

    public static string Index(string[] args)
    {
        // INDEX(list, delim, start, count)
        if (args.Length < 4) return string.Empty;
        string delim = args[1];
        var list = SplitList(args[0], delim);

        if (!int.TryParse(args[2].Trim(), out int start) || start <= 0) return string.Empty;
        if (!int.TryParse(args[3].Trim(), out int count) || count <= 0) return string.Empty;

        int index = start - 1;
        if (index >= list.Count) return string.Empty;
        if (index + count > list.Count) count = list.Count - index;

        return string.Join(delim, list.Skip(index).Take(count));
    }

    public static string Items(string[] args)
    {
        if (args.Length < 2) return "0";
        string delim = args[1];
        return SplitList(args[0], delim).Count.ToString();
    }

    public static string Elements(string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string delim = args.Length > 2 ? args[2] : " ";
        string outDelim = args.Length > 3 ? args[3] : delim;

        var list = SplitList(args[0], delim);
        var indices = SplitList(args[1], " ");
        var result = new List<string>();

        foreach (var idxStr in indices)
        {
            if (int.TryParse(idxStr, out int idx) && idx >= 1 && idx <= list.Count)
            {
                result.Add(list[idx - 1]);
            }
        }
        return string.Join(outDelim, result);
    }

    public static string Insert(string[] args)
    {
        if (args.Length < 3) return string.Empty;
        string delim = args.Length > 3 ? args[3] : " ";
        var list = SplitList(args[0], delim);
        if (!int.TryParse(args[1].Trim(), out int pos)) return args[0];
        string word = args[2];

        int index = pos <= 1 ? 0 : (pos > list.Count ? list.Count : pos - 1);
        list.Insert(index, word);
        return string.Join(delim, list);
    }

    public static string Replace(string[] args)
    {
        if (args.Length < 3) return string.Empty;
        string delim = args.Length > 3 ? args[3] : " ";
        var list = SplitList(args[0], delim);
        if (!int.TryParse(args[1].Trim(), out int pos) || pos < 1 || pos > list.Count) return args[0];

        list[pos - 1] = args[2];
        return string.Join(delim, list);
    }

    public static string LDelete(string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string delim = args.Length > 2 ? args[2] : " ";
        var list = SplitList(args[0], delim);
        if (!int.TryParse(args[1].Trim(), out int pos) || pos < 1 || pos > list.Count) return args[0];

        list.RemoveAt(pos - 1);
        return string.Join(delim, list);
    }

    public static string Remove(string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string delim = args.Length > 2 ? args[2] : " ";
        var list = SplitList(args[0], delim);
        string target = args[1];

        int idx = list.FindIndex(x => string.Equals(x, target, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            list.RemoveAt(idx);
        }
        return string.Join(delim, list);
    }

    public static string RevWords(string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string delim = args.Length > 1 ? args[1] : " ";
        var list = SplitList(args[0], delim);
        list.Reverse();
        return string.Join(delim, list);
    }

    public static string Unique(string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string delim = args.Length > 1 ? args[1] : " ";
        var list = SplitList(args[0], delim);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unique = new List<string>();

        foreach (var item in list)
        {
            if (seen.Add(item))
            {
                unique.Add(item);
            }
        }
        return string.Join(delim, unique);
    }

    public static string Shuffle(string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string delim = args.Length > 1 ? args[1] : " ";
        var list = SplitList(args[0], delim);
        
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return string.Join(delim, list);
    }

    public static string Sort(string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string type = args.Length > 1 ? args[1].Trim().ToLowerInvariant() : "a";
        string delim = args.Length > 2 ? args[2] : " ";
        var list = SplitList(args[0], delim);

        if (type == "n")
        {
            list.Sort((x, y) =>
            {
                double.TryParse(x, out double nx);
                double.TryParse(y, out double ny);
                return nx.CompareTo(ny);
            });
        }
        else
        {
            list.Sort(StringComparer.OrdinalIgnoreCase);
        }

        return string.Join(delim, list);
    }

    public static string SortBy(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string attrRef = args[0];
        string delim = args.Length > 2 ? args[2] : " ";
        var list = SplitList(args[1], delim);

        if (reg.Evaluator == null) return string.Join(delim, list);

        list.Sort((a, b) =>
        {
            string compResult = DbFunctions.Ufun(reg, ctx, [attrRef, a, b]);
            if (int.TryParse(compResult.Trim(), out int res)) return res;
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        });

        return string.Join(delim, list);
    }

    public static string SetUnion(string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string delim = args.Length > 2 ? args[2] : " ";
        var l1 = SplitList(args[0], delim);
        var l2 = SplitList(args[1], delim);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var item in l1.Concat(l2))
        {
            if (seen.Add(item)) result.Add(item);
        }
        return string.Join(delim, result);
    }

    public static string SetInter(string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string delim = args.Length > 2 ? args[2] : " ";
        var l1 = SplitList(args[0], delim);
        var l2 = new HashSet<string>(SplitList(args[1], delim), StringComparer.OrdinalIgnoreCase);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var item in l1)
        {
            if (l2.Contains(item) && seen.Add(item))
            {
                result.Add(item);
            }
        }
        return string.Join(delim, result);
    }

    public static string SetDiff(string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string delim = args.Length > 2 ? args[2] : " ";
        var l1 = SplitList(args[0], delim);
        var l2 = new HashSet<string>(SplitList(args[1], delim), StringComparer.OrdinalIgnoreCase);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var item in l1)
        {
            if (!l2.Contains(item) && seen.Add(item))
            {
                result.Add(item);
            }
        }
        return string.Join(delim, result);
    }

    public static string LNum(string[] args)
    {
        if (args.Length == 0) return string.Empty;
        int start = 0;
        int end = 0;
        int step = 1;
        string delim = " ";

        if (args.Length == 1)
        {
            if (!int.TryParse(args[0].Trim(), out end)) return string.Empty;
            start = 0;
            end -= 1;
        }
        else
        {
            if (!int.TryParse(args[0].Trim(), out start)) return string.Empty;
            if (!int.TryParse(args[1].Trim(), out end)) return string.Empty;
            if (args.Length > 2 && int.TryParse(args[2].Trim(), out int s) && s != 0) step = s;
            if (args.Length > 3) delim = args[3];
        }

        var result = new List<string>();
        if (step > 0)
        {
            for (int i = start; i <= end; i += step)
            {
                result.Add(i.ToString());
                if (result.Count >= 10000) break;
            }
        }
        else
        {
            for (int i = start; i >= end; i += step)
            {
                result.Add(i.ToString());
                if (result.Count >= 10000) break;
            }
        }
        return string.Join(delim, result);
    }

    public static string Iter(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string listText = EvaluateIterText(reg, ctx, args[0]);
        string inDelim = args.Length > 2 ? EvaluateIterText(reg, ctx, args[2]) : " ";
        string outDelim = args.Length > 3 ? EvaluateIterText(reg, ctx, args[3]) : inDelim;

        var list = SplitList(listText, inDelim);
        string pattern = args[1];
        var results = new List<string>();

        for (int i = 0; i < list.Count; i++)
        {
            string item = list[i];
            string substituted = pattern.Replace("##", item).Replace("#@", (i + 1).ToString());
            results.Add(EvaluateIterText(reg, ctx, substituted));
        }

        return string.Join(outDelim, results);
    }

    private static string EvaluateIterText(FunctionRegistry reg, EvaluationContext ctx, string text)
    {
        if (reg.Evaluator == null || string.IsNullOrEmpty(text))
            return text;
        return reg.Evaluator.Evaluate(text, ctx);
    }

    public static string Map(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string attrRef = args[0];
        string inDelim = args.Length > 2 ? args[2] : " ";
        string outDelim = args.Length > 3 ? args[3] : inDelim;

        var list = SplitList(args[1], inDelim);
        var results = new List<string>();

        foreach (var item in list)
        {
            results.Add(DbFunctions.Ufun(reg, ctx, [attrRef, item]));
        }

        return string.Join(outDelim, results);
    }

    public static string Filter(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string attrRef = args[0];
        string inDelim = args.Length > 2 ? args[2] : " ";
        string outDelim = args.Length > 3 ? args[3] : inDelim;

        var list = SplitList(args[1], inDelim);
        var results = new List<string>();

        foreach (var item in list)
        {
            string res = DbFunctions.Ufun(reg, ctx, [attrRef, item]).Trim();
            if (res != "0" && !string.IsNullOrEmpty(res) && !res.StartsWith("#-"))
            {
                results.Add(item);
            }
        }

        return string.Join(outDelim, results);
    }

    public static string Fold(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string attrRef = args[0];
        var list = SplitList(args[1], args.Length > 3 ? args[3] : " ");
        if (list.Count == 0) return string.Empty;

        string accumulator = args.Length > 2 ? args[2] : list[0];
        int startIdx = args.Length > 2 ? 0 : 1;

        for (int i = startIdx; i < list.Count; i++)
        {
            accumulator = DbFunctions.Ufun(reg, ctx, [attrRef, accumulator, list[i]]);
        }

        return accumulator;
    }

    public static string Itemize(string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string delim = args.Length > 1 ? args[1] : " ";
        string conj = args.Length > 2 ? args[2] : "and";
        string punc = args.Length > 3 ? args[3] : ",";

        var list = SplitList(args[0], delim);
        if (list.Count == 0) return string.Empty;
        if (list.Count == 1) return list[0];
        if (list.Count == 2) return $"{list[0]} {conj} {list[1]}";

        return string.Join($"{punc} ", list.Take(list.Count - 1)) + $"{punc} {conj} " + list[^1];
    }

    public static string Splice(string[] args)
    {
        if (args.Length < 3) return string.Empty;
        string delim = args.Length > 3 ? args[3] : " ";
        var l1 = SplitList(args[0], delim);
        string word = args[2];
        var l2 = SplitList(args[1], delim);

        int idx = l1.FindIndex(x => string.Equals(x, word, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            l1.RemoveAt(idx);
            l1.InsertRange(idx, l2);
        }

        return string.Join(delim, l1);
    }

    public static string Step(string[] args)
    {
        if (args.Length < 2) return string.Empty;
        if (!int.TryParse(args[1].Trim(), out int stepSize) || stepSize <= 0) return string.Empty;
        string inDelim = args.Length > 2 ? args[2] : " ";
        string outDelim = args.Length > 3 ? args[3] : inDelim;

        var list = SplitList(args[0], inDelim);
        var results = new List<string>();

        for (int i = 0; i < list.Count; i += stepSize)
        {
            results.Add(list[i]);
        }

        return string.Join(outDelim, results);
    }

    public static string Munge(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3) return string.Empty;
        string attrRef = args[0];
        string delim = args.Length > 3 ? args[3] : " ";
        var l1 = SplitList(args[1], delim);
        var l2 = SplitList(args[2], delim);

        int count = Math.Min(l1.Count, l2.Count);
        var results = new List<string>();

        for (int i = 0; i < count; i++)
        {
            results.Add(DbFunctions.Ufun(reg, ctx, [attrRef, l1[i], l2[i]]));
        }

        return string.Join(delim, results);
    }
}
