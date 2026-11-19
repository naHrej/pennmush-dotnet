namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PennMUSH.Core.Presentation;
using PennMUSH.Core.Protocol;
using PennMUSH.Core.Queue;

public static class StringFunctions
{
    private static readonly Random _random = new();

    public static void Register(FunctionRegistry registry)
    {
        registry.Register("CAT", Cat);
        registry.Register("STRCAT", Strcat);
        registry.Register("STRLEN", Strlen);
        registry.Register("MID", Mid);
        registry.Register("TRIM", Trim);
        registry.Register("TRIMPENN", TrimPenn);
        registry.Register("TRIMTINY", TrimTiny);
        registry.Register("UCSTR", Ucstr);
        registry.Register("UCSTR2", Ucstr);
        registry.Register("LCSTR", Lcstr);
        registry.Register("LCSTR2", Lcstr);
        registry.Register("REPEAT", Repeat);
        registry.Register("COMP", Comp);
        registry.Register("EDIT", Edit);
        registry.Register("LJUST", LJust);
        registry.Register("RJUST", RJust);
        registry.Register("CENTER", Center);
        registry.Register("BEFORE", Before);
        registry.Register("AFTER", After);
        registry.Register("POS", Pos);
        registry.Register("LPOS", LPos);
        registry.Register("RPOS", RPos);
        registry.Register("LEFT", Left);
        registry.Register("RIGHT", Right);
        registry.Register("SQUISH", Squish);
        registry.Register("BRACKETS", Brackets);
        registry.Register("ALIGN", Align);
        registry.Register("LALIGN", LAlign);
        registry.Register("TR", Tr);
        registry.Register("STRINSERT", StrInsert);
        registry.Register("STRDELETE", StrDelete);
        registry.Register("DELETE", StrDelete);
        registry.Register("STRREPLACE", StrReplace);
        registry.Register("STRIPACCENTS", StripAccents);
        registry.Register("DECOMPOSE", Decompose);
        registry.Register("STRMATCH", StrMatch);
        registry.Register("MERGE", Merge);
        registry.Register("FLIP", Flip);
        registry.Register("REVERSE", Flip);
        registry.Register("CHR", Chr);
        registry.Register("ORD", Ord);
        registry.Register("CAPSTR", Capstr);
        registry.Register("ART", Art);
        registry.Register("SUBJ", (ctx, args) => Subj(registry, ctx, args));
        registry.Register("POSS", (ctx, args) => Poss(registry, ctx, args));
        registry.Register("APOSS", (ctx, args) => Aposs(registry, ctx, args));
        registry.Register("OBJ", (ctx, args) => Obj(registry, ctx, args));
        registry.Register("SPACE", Space);
        registry.Register("SCRAMBLE", Scramble);
        registry.Register("SPELLNUM", SpellNum);
        registry.Register("ORDINAL", Ordinal);
        registry.Register("ORDNUMBER", OrdNumber);
        registry.Register("ALPHAMAX", AlphaMax);
        registry.Register("ALPHAMIN", AlphaMin);
        registry.Register("COLUMNS", Columns);
        registry.Register("TABLE", Table);
        registry.Register("WRAP", Wrap);
        registry.Register("STRIP", Strip);
        registry.Register("SECURE", Secure);
        registry.Register("ESCAPE", Escape);
        registry.Register("SOUNDEX", Soundex);
        registry.Register("SOUNDSLIKE", SoundsLike);
        registry.Register("SOUNDLIKE", SoundsLike);
    }

    public static string Cat(EvaluationContext ctx, string[] args)
    {
        return string.Join(" ", args);
    }

    public static string Strcat(EvaluationContext ctx, string[] args)
    {
        return string.Concat(args);
    }

    public static string Strlen(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0";
        return AnsiString.VisibleLength(args[0]).ToString();
    }

    public static string Mid(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3) return MushError.TooFewArguments.Text();
        string str = args[0];
        if (!int.TryParse(args[1].Trim(), out int start) || !int.TryParse(args[2].Trim(), out int length))
            return MushError.ArgumentMustBeInteger.Text();
        if (start < 0) return MushError.OutOfRange.Text();
        if (length == 0) return string.Empty;

        if (length < 0)
        {
            int count = -length;
            int actualStart = start - count + 1;
            if (actualStart < 0)
            {
                count += actualStart;
                actualStart = 0;
            }
            return count <= 0 ? string.Empty : AnsiString.Substring(str, actualStart, count);
        }

        return AnsiString.Substring(str, start, length);
    }

    public static string Trim(EvaluationContext ctx, string[] args)
        => TrimPenn(ctx, args);

    public static string TrimPenn(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string chars = args.Length > 1 ? args[1] : "";
        string style = args.Length > 2 ? args[2] : "";
        return ApplyTrim(args[0], chars, style);
    }

    public static string TrimTiny(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string style = args.Length > 1 ? args[1] : "";
        string chars = args.Length > 2 ? args[2] : "";
        return ApplyTrim(args[0], chars, style);
    }

    private static string ApplyTrim(string input, string chars, string style)
    {
        char[] toTrim = string.IsNullOrEmpty(chars) ? new[] { ' ' } : chars.ToCharArray();
        string s = style.Trim().ToLowerInvariant();
        if (s.StartsWith('l'))
            return input.TrimStart(toTrim);
        if (s.StartsWith('r'))
            return input.TrimEnd(toTrim);
        return input.Trim(toTrim);
    }

    public static string Ucstr(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        return args[0].ToUpperInvariant();
    }

    public static string Lcstr(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        return args[0].ToLowerInvariant();
    }

    public static string Repeat(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return string.Empty;
        string str = args[0];
        if (!int.TryParse(args[1].Trim(), out int count) || count <= 0) return string.Empty;
        if (count > 1000) count = 1000;

        var sb = new StringBuilder(str.Length * count);
        for (int i = 0; i < count; i++)
            sb.Append(str);
        return sb.ToString();
    }

    public static string Comp(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();
        string type = args.Length > 2 && !string.IsNullOrWhiteSpace(args[2]) ? args[2].Trim().ToUpperInvariant() : "A";
        int cmp;
        switch (type)
        {
            case "A":
                cmp = string.CompareOrdinal(args[0], args[1]);
                break;
            case "I":
                cmp = string.Compare(args[0], args[1], StringComparison.OrdinalIgnoreCase);
                break;
            case "N":
                if (!long.TryParse(args[0].Trim(), out long ia) || !long.TryParse(args[1].Trim(), out long ib))
                    return MushError.ArgumentMustBeInteger.Text();
                cmp = ia.CompareTo(ib);
                break;
            case "F":
                if (!double.TryParse(args[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double da) ||
                    !double.TryParse(args[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double db))
                    return MushError.ArgumentMustBeNumber.Text();
                cmp = da.CompareTo(db);
                break;
            case "D":
                var daRef = DbFunctions.ParseDbref(args[0], ctx);
                var dbRef = DbFunctions.ParseDbref(args[1], ctx);
                cmp = daRef.Value.CompareTo(dbRef.Value);
                break;
            default:
                return "#-1 INVALID COMPARE TYPE";
        }
        return cmp < 0 ? "-1" : cmp > 0 ? "1" : "0";
    }

    public static string Edit(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3) return args.Length > 0 ? args[0] : string.Empty;
        string text = args[0];

        for (int i = 1; i < args.Length - 1; i += 2)
        {
            string search = args[i];
            string replace = args[i + 1];
            if (search == "$")
                text += replace;
            else if (search == "^")
                text = replace + text;
            else if (string.IsNullOrEmpty(search))
            {
                var sb = new StringBuilder();
                foreach (char c in text)
                {
                    sb.Append(replace).Append(c);
                }
                sb.Append(replace);
                text = sb.ToString();
            }
            else
                text = text.Replace(search, replace);
        }
        return text;
    }

    public static string LJust(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return args.Length > 0 ? args[0] : string.Empty;
        if (!int.TryParse(args[1].Trim(), out int width) || width < 0)
            return MushError.ArgumentMustBeInteger.Text();
        string fill = args.Length > 2 && args[2].Length > 0 ? args[2] : " ";
        bool truncate = args.Length > 3 && LogicFunctions.IsTruthy(args[3]);
        return AnsiString.Pad(args[0], width, fill, align: -1, truncate: truncate);
    }

    public static string RJust(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return args.Length > 0 ? args[0] : string.Empty;
        if (!int.TryParse(args[1].Trim(), out int width) || width < 0)
            return MushError.ArgumentMustBeInteger.Text();
        string fill = args.Length > 2 && args[2].Length > 0 ? args[2] : " ";
        bool truncate = args.Length > 3 && LogicFunctions.IsTruthy(args[3]);
        return AnsiString.Pad(args[0], width, fill, align: 1, truncate: truncate);
    }

    public static string Center(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return args.Length > 0 ? args[0] : string.Empty;
        if (!int.TryParse(args[1].Trim(), out int width) || width < 0)
            return MushError.ArgumentMustBeInteger.Text();
        string fill = args.Length > 2 && args[2].Length > 0 ? args[2] : " ";
        string? rightFill = args.Length > 3 && args[3].Length > 0 ? args[3] : null;
        return AnsiString.Pad(args[0], width, fill, align: 0, truncate: false, rightFill: rightFill);
    }

    public static string Before(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string text = args[0];
        string search = args.Length > 1 && !string.IsNullOrEmpty(args[1]) ? args[1] : " ";

        int idx = text.IndexOf(search, StringComparison.Ordinal);
        return idx >= 0 ? text.Substring(0, idx) : text;
    }

    public static string After(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string text = args[0];
        string search = args.Length > 1 && !string.IsNullOrEmpty(args[1]) ? args[1] : " ";

        int idx = text.IndexOf(search, StringComparison.Ordinal);
        return idx >= 0 ? text.Substring(idx + search.Length) : string.Empty;
    }

    public static string LPos(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || string.IsNullOrEmpty(args[1])) return string.Empty;
        string text = args[0];
        char ch = args[1][0];
        var indices = new List<string>();
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == ch)
                indices.Add(i.ToString());
        }
        return string.Join(" ", indices);
    }

    public static string RPos(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return "0";
        string text = args[0];
        string search = args[1];
        if (string.IsNullOrEmpty(search)) return "0";

        int idx = text.LastIndexOf(search, StringComparison.OrdinalIgnoreCase);
        return (idx >= 0 ? idx + 1 : 0).ToString();
    }

    public static string Chr(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || !int.TryParse(args[0].Trim(), out int code) || code < 0 || code > 65535)
            return string.Empty;
        return ((char)code).ToString();
    }

    public static string Ord(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrEmpty(args[0])) return "0";
        return ((int)args[0][0]).ToString();
    }

    public static string Capstr(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrEmpty(args[0])) return string.Empty;
        string text = args[0];
        return char.ToUpperInvariant(text[0]) + text.Substring(1);
    }

    public static string Art(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0])) return "a";
        string word = args[0].TrimStart();
        char firstChar = char.ToLowerInvariant(word[0]);
        return "aeiou".Contains(firstChar) ? "an" : "a";
    }

    private static string? LookupGender(FunctionRegistry reg, EvaluationContext ctx, string[] args, out string gender)
    {
        gender = string.Empty;
        if (args.Length == 0) return MushError.TooFewArguments.Text();
        if (reg.Database == null) return MushError.NoMatch.Text();
        string spec = args[0].Trim();
        var obj = reg.Database.GetObject(DbFunctions.ParseDbref(spec, ctx))
            ?? reg.Database.MatchObject(ctx.Executor, spec);
        if (obj == null) return MushError.NoMatch.Text();

        gender = (obj.GetAttribute("SEX", reg.Database)?.Value.Trim() ?? string.Empty).ToLowerInvariant();
        return null;
    }

    public static string Subj(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var err = LookupGender(reg, ctx, args, out string g);
        if (err != null) return err;
        return g switch
        {
            "male" or "m" or "man" or "boy" => "he",
            "female" or "f" or "woman" or "girl" => "she",
            "plural" or "p" or "they" => "they",
            _ => "it"
        };
    }

    public static string Poss(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var err = LookupGender(reg, ctx, args, out string g);
        if (err != null) return err;
        return g switch
        {
            "male" or "m" or "man" or "boy" => "his",
            "female" or "f" or "woman" or "girl" => "her",
            "plural" or "p" or "they" => "their",
            _ => "its"
        };
    }

    public static string Aposs(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var err = LookupGender(reg, ctx, args, out string g);
        if (err != null) return err;
        return g switch
        {
            "male" or "m" or "man" or "boy" => "his",
            "female" or "f" or "woman" or "girl" => "hers",
            "plural" or "p" or "they" => "theirs",
            _ => "its"
        };
    }

    public static string Obj(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        var err = LookupGender(reg, ctx, args, out string g);
        if (err != null) return err;
        return g switch
        {
            "male" or "m" or "man" or "boy" => "him",
            "female" or "f" or "woman" or "girl" => "her",
            "plural" or "p" or "they" => "them",
            _ => "it"
        };
    }

    public static string Space(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || !int.TryParse(args[0].Trim(), out int count) || count <= 0) return string.Empty;
        if (count > 2000) count = 2000;
        return new string(' ', count);
    }

    public static string Scramble(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrEmpty(args[0])) return string.Empty;
        char[] chars = args[0].ToCharArray();
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }

    public static string SpellNum(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || !long.TryParse(args[0].Trim(), out long num)) return "zero";
        if (num == 0) return "zero";
        if (num < 0) return "negative " + SpellNumberPositive(-num);
        return SpellNumberPositive(num);
    }

    private static string SpellNumberPositive(long number)
    {
        if (number == 0) return string.Empty;
        if (number < 20)
        {
            string[] units = ["", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
                              "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen"];
            return units[number];
        }
        if (number < 100)
        {
            string[] tens = ["", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"];
            long remainder = number % 10;
            return tens[number / 10] + (remainder > 0 ? "-" + SpellNumberPositive(remainder) : "");
        }
        if (number < 1000)
        {
            long remainder = number % 100;
            return SpellNumberPositive(number / 100) + " hundred" + (remainder > 0 ? " " + SpellNumberPositive(remainder) : "");
        }
        if (number < 1_000_000)
        {
            long remainder = number % 1000;
            return SpellNumberPositive(number / 1000) + " thousand" + (remainder > 0 ? " " + SpellNumberPositive(remainder) : "");
        }
        if (number < 1_000_000_000)
        {
            long remainder = number % 1_000_000;
            return SpellNumberPositive(number / 1_000_000) + " million" + (remainder > 0 ? " " + SpellNumberPositive(remainder) : "");
        }
        return number.ToString();
    }

    public static string OrdNumber(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || !int.TryParse(args[0].Trim(), out int num)) return "0th";
        int abs = Math.Abs(num);
        string suffix = (abs % 100) switch
        {
            11 or 12 or 13 => "th",
            _ => (abs % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            }
        };
        return $"{num}{suffix}";
    }

    public static string AlphaMax(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string max = args[0];
        for (int i = 1; i < args.Length; i++)
        {
            if (string.Compare(args[i], max, StringComparison.OrdinalIgnoreCase) > 0)
                max = args[i];
        }
        return max;
    }

    public static string AlphaMin(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string min = args[0];
        for (int i = 1; i < args.Length; i++)
        {
            if (string.Compare(args[i], min, StringComparison.OrdinalIgnoreCase) < 0)
                min = args[i];
        }
        return min;
    }

    public static string Columns(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return args.Length > 0 ? args[0] : string.Empty;
        string delim = args.Length > 3 ? args[3] : " ";
        var items = ListFunctions.SplitList(args[0], delim);
        if (!int.TryParse(args[1].Trim(), out int numCols) || numCols <= 0) return args[0];

        int colWidth = 78 / numCols;
        if (args.Length > 2 && int.TryParse(args[2].Trim(), out int customWidth) && customWidth > 0)
        {
            colWidth = customWidth;
        }

        var sb = new StringBuilder();
        for (int i = 0; i < items.Count; i++)
        {
            string item = items[i];
            sb.Append(item.Length >= colWidth ? item.Substring(0, colWidth) : item.PadRight(colWidth));
            if ((i + 1) % numCols == 0 && i < items.Count - 1)
            {
                sb.AppendLine();
            }
        }
        return sb.ToString();
    }

    public static string Table(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        string delim = args.Length > 3 && !string.IsNullOrEmpty(args[3]) ? args[3] : " ";
        var items = ListFunctions.SplitList(args[0], delim);

        int align = -1;
        int fieldWidth = 10;
        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
        {
            string fwSpec = args[1].Trim();
            if (fwSpec.StartsWith('<')) { align = -1; fwSpec = fwSpec[1..]; }
            else if (fwSpec.StartsWith('>')) { align = 1; fwSpec = fwSpec[1..]; }
            else if (fwSpec.StartsWith('-')) { align = 0; fwSpec = fwSpec[1..]; }
            if (!int.TryParse(fwSpec, out fieldWidth) || fieldWidth <= 0)
                return MushError.ArgumentMustBePositiveInteger.Text();
        }

        int lineWidth = 78;
        if (args.Length > 2 && !string.IsNullOrWhiteSpace(args[2]) &&
            (!int.TryParse(args[2].Trim(), out lineWidth) || lineWidth <= 0))
            return MushError.ArgumentMustBePositiveInteger.Text();

        string osep = args.Length > 4 ? args[4] : " ";
        int step = fieldWidth + osep.Length;
        int colsPerLine = Math.Max(1, (lineWidth + osep.Length) / Math.Max(1, step));
        var sb = new StringBuilder();

        for (int i = 0; i < items.Count; i++)
        {
            sb.Append(AnsiString.Pad(items[i], fieldWidth, " ", align, truncate: true));
            bool endOfLine = (i + 1) % colsPerLine == 0;
            if (i < items.Count - 1)
                sb.Append(endOfLine ? Environment.NewLine : osep);
        }
        return sb.ToString();
    }

    public static string Wrap(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return args.Length > 0 ? args[0] : string.Empty;
        string text = args[0];
        if (!int.TryParse(args[1].Trim(), out int width) || width < 2)
            return MushError.OutOfRange.Text();

        int width1st = width;
        if (args.Length > 2 && !string.IsNullOrWhiteSpace(args[2]))
        {
            if (!int.TryParse(args[2].Trim(), out width1st) || width1st < 2)
                return MushError.OutOfRange.Text();
        }
        string lineSep = args.Length > 3 ? args[3] : "\n";

        var allLines = new List<string>();
        bool firstLine = true;
        foreach (string para in text.Replace("\r\n", "\n").Split('\n'))
        {
            var words = para.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                allLines.Add(string.Empty);
                firstLine = false;
                continue;
            }

            var currentLine = new StringBuilder();
            int maxW = firstLine ? width1st : width;
            foreach (string rawWord in words)
            {
                string word = rawWord;
                while (AnsiString.VisibleLength(word) > maxW)
                {
                    if (currentLine.Length > 0)
                    {
                        allLines.Add(currentLine.ToString());
                        currentLine.Clear();
                        firstLine = false;
                        maxW = width;
                    }
                    int chunk = Math.Max(1, maxW - 1);
                    allLines.Add(AnsiString.Substring(word, 0, chunk) + "-");
                    word = AnsiString.Substring(word, chunk, AnsiString.VisibleLength(word) - chunk);
                    firstLine = false;
                    maxW = width;
                }

                int wordLen = AnsiString.VisibleLength(word);
                int curLen = AnsiString.VisibleLength(currentLine.ToString());
                if (curLen > 0 && curLen + 1 + wordLen > maxW)
                {
                    allLines.Add(currentLine.ToString());
                    currentLine.Clear();
                    firstLine = false;
                    maxW = width;
                }
                if (currentLine.Length > 0)
                    currentLine.Append(' ');
                currentLine.Append(word);
            }
            allLines.Add(currentLine.ToString());
            firstLine = false;
        }

        return string.Join(lineSep, allLines);
    }

    public static string Strip(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrEmpty(args[0])) return string.Empty;
        string raw = WebSocketMarkup.ContainsFrames(args[0])
            ? WebSocketMarkup.Render(args[0], isWebSocket: false).Text
            : args[0];
        return AnsiColorTable.StripAnsi(raw);
    }

    public static string Secure(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return string.Empty;
        var sb = new StringBuilder(args[0].Length);
        foreach (char c in args[0])
        {
            if (c is '%' or '[' or ']' or '{' or '}' or '(' or ')' or ',' or ';' or '$' or '^' or '\\')
                sb.Append(' ');
            else
                sb.Append(c);
        }
        return sb.ToString();
    }

    public static string Escape(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrEmpty(args[0])) return string.Empty;
        var sb = new StringBuilder(args[0].Length + 4);
        sb.Append('\\');
        foreach (char c in args[0])
        {
            if (c is '%' or '[' or ']' or '{' or '}' or '(' or ')' or ',' or ';' or '$' or '^' or '\\')
                sb.Append('\\');
            sb.Append(c);
        }
        return sb.ToString();
    }

    public static string Soundex(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return "0000";
        string type = args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]) ? args[1].Trim().ToLowerInvariant() : "soundex";
        if (type != "soundex" && type != "phone")
            return MushError.UnknownHashType.Text();
        return ComputeSoundHash(args[0], type);
    }

    public static string SoundsLike(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return "0";
        string type = args.Length >= 3 && !string.IsNullOrWhiteSpace(args[2]) ? args[2].Trim().ToLowerInvariant() : "soundex";
        if (type != "soundex" && type != "phone")
            return MushError.UnknownHashType.Text();
        return string.Equals(ComputeSoundHash(args[0], type), ComputeSoundHash(args[1], type), StringComparison.Ordinal) ? "1" : "0";
    }

    private static string ComputeSoundHash(string input, string type)
        => type == "phone" ? ComputePhoneHash(input) : ComputeSoundex(input);

    private static string ComputeSoundex(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "0000";
        string word = Regex.Replace(input.ToUpperInvariant(), @"[^A-Z]", "");
        if (string.IsNullOrEmpty(word)) return "0000";
        if (word.StartsWith("PH", StringComparison.Ordinal))
            word = "F" + word.Substring(2);

        var sb = new StringBuilder();
        sb.Append(word[0]);

        char prevCode = GetSoundexCode(word[0]);
        for (int i = 1; i < word.Length && sb.Length < 4; i++)
        {
            char code = GetSoundexCode(word[i]);
            if (code != '0' && code != prevCode)
                sb.Append(code);
            prevCode = code;
        }

        while (sb.Length < 4) sb.Append('0');
        return sb.ToString();
    }

    private static string ComputePhoneHash(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        string w = Regex.Replace(input.ToUpperInvariant(), @"[^A-Z]", "");
        if (string.IsNullOrEmpty(w)) return string.Empty;

        w = w.Replace("PH", "F", StringComparison.Ordinal)
             .Replace("KN", "N", StringComparison.Ordinal)
             .Replace("WR", "R", StringComparison.Ordinal)
             .Replace("QU", "KW", StringComparison.Ordinal)
             .Replace('X', 'S');

        var sb = new StringBuilder();
        char last = '\0';
        for (int i = 0; i < w.Length; i++)
        {
            char c = w[i] switch
            {
                'A' or 'E' or 'I' or 'O' or 'U' or 'Y' => 'A',
                'B' or 'P' => 'B',
                'C' or 'K' or 'Q' => 'K',
                'D' or 'T' => 'T',
                'F' or 'V' => 'F',
                'G' or 'J' => 'J',
                'S' or 'Z' => 'S',
                'M' or 'N' => 'M',
                _ => w[i]
            };
            if (i > 0 && c == 'A') continue;
            if (c != last)
            {
                sb.Append(c);
                last = c;
            }
        }
        return sb.ToString();
    }

    private static char GetSoundexCode(char c)
    {
        return c switch
        {
            'B' or 'F' or 'P' or 'V' => '1',
            'C' or 'G' or 'J' or 'K' or 'Q' or 'S' or 'X' or 'Z' => '2',
            'D' or 'T' => '3',
            'L' => '4',
            'M' or 'N' => '5',
            'R' => '6',
            _ => '0'
        };
    }

    public static string Flip(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrEmpty(args[0])) return string.Empty;
        char[] arr = args[0].ToCharArray();
        Array.Reverse(arr);
        return new string(arr);
    }

    public static string Pos(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.Nothing.Text();
        int idx = args[1].IndexOf(args[0], StringComparison.Ordinal);
        return idx >= 0 ? (idx + 1).ToString() : MushError.Nothing.Text();
    }

    public static string Left(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[1].Trim(), out int len))
            return MushError.ArgumentMustBeInteger.Text();
        if (len < 0) return MushError.OutOfRange.Text();
        return len >= args[0].Length ? args[0] : args[0].Substring(0, len);
    }

    public static string Right(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[1].Trim(), out int len))
            return MushError.ArgumentMustBeInteger.Text();
        if (len < 0) return MushError.OutOfRange.Text();
        return len >= args[0].Length ? args[0] : args[0].Substring(args[0].Length - len, len);
    }

    public static string Squish(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrEmpty(args[0])) return string.Empty;
        if (args.Length > 1 && args[1].Length > 1)
            return MushError.SeparatorMustBeOneCharacter.Text();
        char sep = args.Length > 1 && args[1].Length == 1 ? args[1][0] : ' ';

        var sb = new StringBuilder(args[0].Length);
        bool inSep = true;
        foreach (char c in args[0])
        {
            if (c == sep)
            {
                if (!inSep)
                {
                    sb.Append(sep);
                    inSep = true;
                }
            }
            else
            {
                sb.Append(c);
                inSep = false;
            }
        }
        if (sb.Length > 0 && sb[^1] == sep)
            sb.Length--;
        return sb.ToString();
    }

    public static string Brackets(EvaluationContext ctx, string[] args)
    {
        string text = args.Length > 0 ? args[0] : string.Empty;
        int lbrack = 0, rbrack = 0, lparen = 0, rparen = 0, lcurl = 0, rcurl = 0;
        foreach (char c in text)
        {
            switch (c)
            {
                case '[': lbrack++; break;
                case ']': rbrack++; break;
                case '(': lparen++; break;
                case ')': rparen++; break;
                case '{': lcurl++; break;
                case '}': rcurl++; break;
            }
        }
        return $"{lbrack} {rbrack} {lparen} {rparen} {lcurl} {rcurl}";
    }

    public static string StrInsert(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3 || !int.TryParse(args[1].Trim(), out int pos))
            return MushError.ArgumentsMustBeIntegers.Text();
        if (pos < 0) return MushError.OutOfRange.Text();
        if (pos >= args[0].Length) return args[0] + args[2];
        return args[0].Insert(pos, args[2]);
    }

    public static string StrDelete(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3 || !int.TryParse(args[1].Trim(), out int pos) || !int.TryParse(args[2].Trim(), out int count))
            return MushError.ArgumentsMustBeIntegers.Text();
        if (pos < 0) return MushError.OutOfRange.Text();
        if (pos >= args[0].Length || count == 0) return args[0];

        if (count < 0)
        {
            int end = pos + 1;
            int start = Math.Max(0, pos + count + 1);
            return args[0].Remove(start, end - start);
        }

        int len = Math.Min(count, args[0].Length - pos);
        return args[0].Remove(pos, len);
    }

    public static string StrReplace(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 4 || !int.TryParse(args[1].Trim(), out int pos) || !int.TryParse(args[2].Trim(), out int len))
            return MushError.ArgumentsMustBeIntegers.Text();
        if (pos < 0 || len < 0) return MushError.OutOfRange.Text();
        if (pos > args[0].Length) return args[0];

        int rem = Math.Min(len, args[0].Length - pos);
        return args[0].Remove(pos, rem).Insert(pos, args[3]);
    }

    public static string Tr(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3) return string.Empty;
        if (!TryExpandTrChars(args[1], out string fromChars) || !TryExpandTrChars(args[2], out string toChars))
            return MushError.TrCannotAcceptNonprintingChars.Text();
        if (fromChars.Length != toChars.Length)
            return MushError.StringLengthsMustBeEqual.Text();

        var map = new Dictionary<char, char>();
        for (int i = 0; i < fromChars.Length; i++)
            map[fromChars[i]] = toChars[i];

        var sb = new StringBuilder(args[0].Length);
        foreach (char c in args[0])
            sb.Append(map.TryGetValue(c, out char repl) ? repl : c);
        return sb.ToString();
    }

    private static bool TryExpandTrChars(string spec, out string expanded)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < spec.Length; i++)
        {
            char c = spec[i];
            if ((c < 32 && c != '\n') || c == 127)
            {
                expanded = string.Empty;
                return false;
            }

            if (i + 2 < spec.Length && spec[i + 1] == '-')
            {
                char end = spec[i + 2];
                if ((end < 32 && end != '\n') || end == 127)
                {
                    expanded = string.Empty;
                    return false;
                }

                if (c <= end)
                {
                    for (char cur = c; cur <= end; cur++)
                        sb.Append(cur);
                }
                else
                {
                    for (char cur = c; cur >= end; cur--)
                        sb.Append(cur);
                }
                i += 2;
            }
            else
            {
                sb.Append(c);
            }
        }
        expanded = sb.ToString();
        return true;
    }

    public static string StripAccents(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrEmpty(args[0])) return string.Empty;
        return PennMUSH.Core.Presentation.AccentStripper.Strip(args[0]);
    }

    public static string Decompose(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrEmpty(args[0])) return string.Empty;
        var sb = new StringBuilder(args[0].Length * 2);
        foreach (char c in args[0])
        {
            switch (c)
            {
                case '\\': sb.Append(@"\\"); break;
                case '%': sb.Append("%%"); break;
                case '\r': break;
                case '\n': sb.Append("%r"); break;
                case '\t': sb.Append("%t"); break;
                case ' ': sb.Append("%b"); break;
                case '[': sb.Append(@"\["); break;
                case ']': sb.Append(@"\]"); break;
                case '{': sb.Append(@"\{"); break;
                case '}': sb.Append(@"\}"); break;
                case '(': sb.Append(@"\("); break;
                case ')': sb.Append(@"\)"); break;
                case ',': sb.Append(@"\,"); break;
                case ';': sb.Append(@"\;"); break;
                case '$': sb.Append(@"\$"); break;
                case '^': sb.Append(@"\^"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    public static string StrMatch(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return "0";
        string input = args[0];
        string pattern = args[1];
        bool matched = (string.IsNullOrEmpty(input) && string.IsNullOrEmpty(pattern))
            || PatternMatcher.Match(pattern, input, out var captures);

        string[] capArray = matched && (pattern.Contains('*') || pattern.Contains('?'))
            ? (PatternMatcher.Match(pattern, input, out var c) ? c : Array.Empty<string>())
            : Array.Empty<string>();

        var errSb = new StringBuilder();
        if (matched && args.Length > 2 && !string.IsNullOrWhiteSpace(args[2]))
        {
            var regNames = args[2].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < regNames.Length; i++)
            {
                string rName = regNames[i];
                if (rName == "-") continue;
                if (!IsValidRegisterName(rName))
                {
                    errSb.Append(MushError.RegisterNameInvalid.Text());
                    continue;
                }
                string val = i < capArray.Length ? capArray[i] : string.Empty;
                ctx.SetRegister(rName, val);
            }
        }

        return (matched ? "1" : "0") + errSb;
    }

    private static bool IsValidRegisterName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        foreach (char c in name)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
                return false;
        }
        return true;
    }

    public static string Merge(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3) return string.Empty;
        if (args[0].Length != args[1].Length)
            return MushError.StringLengthsMustBeEqual.Text();

        string matchChars = string.IsNullOrEmpty(args[2]) ? " " : args[2];
        var sb = new StringBuilder(args[0].Length);
        for (int i = 0; i < args[0].Length; i++)
        {
            char c1 = args[0][i];
            sb.Append(matchChars.Contains(c1, StringComparison.Ordinal) ? args[1][i] : c1);
        }
        return sb.ToString();
    }

    public static string Ordinal(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
            return MushError.ArgumentMustBeInteger.Text();

        string raw = args[0].Trim();
        if (raw.Contains('.'))
            return MushError.ArgumentMustBeInteger.Text();
        if (!long.TryParse(raw, out long num))
            return MushError.ArgumentMustBeNumber.Text();

        if (num == 0) return "zeroth";
        string spelled = SpellNum(ctx, new[] { raw });
        return OrdinalizeSpelled(spelled);
    }

    private static string OrdinalizeSpelled(string spelled)
    {
        (string suffix, string replacement)[] irregular =
        {
            ("one", "first"), ("two", "second"), ("three", "third"), ("four", "fourth"),
            ("five", "fifth"), ("six", "sixth"), ("seven", "seventh"), ("eight", "eighth"),
            ("nine", "ninth"), ("twelve", "twelfth")
        };

        foreach (var (suf, rep) in irregular)
        {
            if (spelled.EndsWith(suf, StringComparison.OrdinalIgnoreCase))
                return spelled.Substring(0, spelled.Length - suf.Length) + rep;
        }
        if (spelled.EndsWith('y'))
            return spelled.Substring(0, spelled.Length - 1) + "ieth";
        return spelled + "th";
    }

    private enum AlignMode { Left, Right, Center, Full, WpFull }

    private sealed class AlignColumn
    {
        public AlignMode Mode;
        public int Width;
        public bool Repeat;
        public bool CoalesceLeft;
        public bool CoalesceRight;
        public bool NoFill;
        public bool TruncEach;
        public bool TruncAll;
        public bool NoColSep;
        public string? AnsiOpen;
        public string? AnsiClose;
        public string Text = string.Empty;
        public List<AnsiString.Glyph> Glyphs = new();
        public string TrailingMarkup = string.Empty;
        public int Pos;
    }

    public static string Align(EvaluationContext ctx, string[] args)
    {
        if (args.Length == 0) return MushError.NotEnoughColumnsForAlign.Text();
        if (!TryParseAlignColumns(args[0], out var cols, out string parseError))
            return parseError;

        int n = cols.Count;
        if (args.Length < n + 1 || args.Length > n + 4)
            return MushError.InvalidNumberOfArgumentsToAlign.Text();

        char filler = ' ';
        if (args.Length >= n + 2)
        {
            if (args[n + 1].Length > 1) return MushError.FillerMustBeOneCharacter.Text();
            if (args[n + 1].Length == 1) filler = args[n + 1][0];
        }

        string fieldSep = args.Length >= n + 3 ? args[n + 2] : " ";
        string lineSep = args.Length >= n + 4 ? args[n + 3] : "\n";

        for (int i = 0; i < n; i++)
        {
            cols[i].Text = args[i + 1].Replace("\r\n", "\n", StringComparison.Ordinal);
            cols[i].Glyphs = AnsiString.Parse(cols[i].Text, out string trailing);
            cols[i].TrailingMarkup = trailing;
        }

        return RenderAlignedColumns(cols, filler, fieldSep, lineSep);
    }

    public static string LAlign(EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.InvalidNumberOfArgumentsToAlign.Text();
        if (!TryParseAlignColumns(args[0], out var cols, out string parseError))
            return parseError;

        if (args.Length > 2 && args[2].Length > 1)
            return MushError.SeparatorMustBeOneCharacter.Text();
        string delim = args.Length > 2 && args[2].Length == 1 ? args[2] : " ";
        var items = ListFunctions.SplitList(args[1], delim);
        if (items.Count != cols.Count)
            return MushError.InvalidNumberOfArgumentsToAlign.Text();

        char filler = ' ';
        if (args.Length > 3)
        {
            if (args[3].Length > 1) return MushError.FillerMustBeOneCharacter.Text();
            if (args[3].Length == 1) filler = args[3][0];
        }

        string fieldSep = args.Length > 4 ? args[4] : " ";
        string lineSep = args.Length > 5 ? args[5] : "\n";

        for (int i = 0; i < cols.Count; i++)
        {
            cols[i].Text = items[i].Replace("\r\n", "\n", StringComparison.Ordinal);
            cols[i].Glyphs = AnsiString.Parse(cols[i].Text, out string trailing);
            cols[i].TrailingMarkup = trailing;
        }

        return RenderAlignedColumns(cols, filler, fieldSep, lineSep);
    }

    private static bool TryParseAlignColumns(string spec, out List<AlignColumn> cols, out string error)
    {
        cols = new List<AlignColumn>();
        error = string.Empty;
        int i = 0;
        int totalWidth = 0;

        while (i < spec.Length)
        {
            while (i < spec.Length && char.IsWhiteSpace(spec[i])) i++;
            if (i >= spec.Length) break;

            var col = new AlignColumn { Mode = AlignMode.Left };
            if (spec[i] == '>') { col.Mode = AlignMode.Right; i++; }
            else if (spec[i] == '-') { col.Mode = AlignMode.Center; i++; }
            else if (spec[i] == '<') { col.Mode = AlignMode.Left; i++; }
            else if (spec[i] == '_') { col.Mode = AlignMode.Full; i++; }
            else if (spec[i] == '=') { col.Mode = AlignMode.WpFull; i++; }
            else if (!char.IsDigit(spec[i]))
            {
                error = MushError.InvalidAlignString.Text();
                return false;
            }

            int widthStart = i;
            while (i < spec.Length && char.IsDigit(spec[i])) i++;
            if (i == widthStart || !int.TryParse(spec.AsSpan(widthStart, i - widthStart), out int w) || w < 0)
            {
                error = MushError.InvalidAlignString.Text();
                return false;
            }
            if (w > 8192)
            {
                error = MushError.CannotHaveColumnsThatLarge.Text();
                return false;
            }
            col.Width = w;
            totalWidth += w;
            if (totalWidth > 8192)
            {
                error = MushError.CannotHaveColumnsThatLarge.Text();
                return false;
            }

            while (i < spec.Length && !char.IsWhiteSpace(spec[i]))
            {
                switch (spec[i])
                {
                    case '.': col.Repeat = true; break;
                    case '`': col.CoalesceLeft = true; break;
                    case '\'': col.CoalesceRight = true; break;
                    case '$': col.NoFill = true; break;
                    case 'x': col.TruncEach = true; break;
                    case 'X': col.TruncAll = true; break;
                    case '#': col.NoColSep = true; break;
                    case '(':
                        int close = spec.IndexOf(')', i + 1);
                        if (close < 0)
                        {
                            error = MushError.InvalidAlignString.Text();
                            return false;
                        }
                        string ansiSpec = spec.Substring(i + 1, close - (i + 1)).Trim();
                        if (!string.IsNullOrEmpty(ansiSpec)
                            && AnsiColorTable.TryBuildAnsiSgr(ansiSpec, out string open, out string closeSeq, out bool hasAnsi)
                            && hasAnsi)
                        {
                            col.AnsiOpen = open;
                            col.AnsiClose = closeSeq;
                        }
                        i = close;
                        break;
                }
                i++;
            }

            cols.Add(col);
            if (cols.Count > 30)
            {
                error = MushError.TooManyColumnsForAlign.Text();
                return false;
            }
        }

        if (cols.Count == 0)
        {
            error = MushError.NotEnoughColumnsForAlign.Text();
            return false;
        }
        return true;
    }

    private static string RenderAlignedColumns(List<AlignColumn> cols, char filler, string fieldSep, string lineSep)
    {
        var output = new StringBuilder();
        int lineNum = 0;
        int fieldSepVisLen = AnsiString.VisibleLength(fieldSep);

        while (true)
        {
            var widths = cols.Select(c => c.Width).ToArray();
            var line = new StringBuilder();
            int colsDone = 0;
            bool needSep = false;

            for (int i = 0; i < cols.Count; i++)
            {
                var col = cols[i];
                if (widths[i] <= 0)
                {
                    colsDone++;
                    continue;
                }

                int nextActive = -1;
                for (int j = i + 1; j < cols.Count; j++)
                {
                    if (widths[j] > 0) { nextActive = j; break; }
                }

                if (nextActive >= 0 && !cols[nextActive].Repeat && cols[nextActive].CoalesceLeft && cols[nextActive].Pos >= cols[nextActive].Glyphs.Count)
                {
                    widths[i] += widths[nextActive] + fieldSepVisLen;
                    widths[nextActive] = 0;
                    if (cols[nextActive].NoFill) col.NoFill = true;
                    if (cols[nextActive].NoColSep) col.NoColSep = true;
                }

                if (col.Pos >= col.Glyphs.Count)
                {
                    if (col.Repeat && col.Glyphs.Count > 0)
                    {
                        col.Pos = 0;
                        colsDone++;
                    }
                    else
                    {
                        if (col.CoalesceRight && nextActive >= 0)
                        {
                            widths[nextActive] += widths[i] + fieldSepVisLen;
                            widths[i] = 0;
                            colsDone++;
                            continue;
                        }
                        if (needSep && fieldSep.Length > 0) line.Append(fieldSep);
                        needSep = !col.NoColSep;
                        if (!col.NoFill)
                        {
                            if (col.AnsiOpen != null) line.Append(col.AnsiOpen);
                            line.Append(filler, widths[i]);
                            if (col.AnsiClose != null) line.Append(col.AnsiClose);
                        }
                        colsDone++;
                        continue;
                    }
                }
                else if (col.Repeat)
                {
                    colsDone++;
                }

                string seg = TakeAlignSegment(col, widths[i], out int visLen, out bool skipLeadingSpaces);
                if (skipLeadingSpaces)
                {
                    while (col.Pos < col.Glyphs.Count && col.Glyphs[col.Pos].Char != '\n' && char.IsWhiteSpace(col.Glyphs[col.Pos].Char))
                        col.Pos++;
                }

                if (needSep && fieldSep.Length > 0) line.Append(fieldSep);
                needSep = !col.NoColSep;
                FormatAlignSegment(line, seg, visLen, col.Mode, widths[i], filler, col.NoFill, col.AnsiOpen, col.AnsiClose);
            }

            if (colsDone == cols.Count) break;
            if (lineNum > 0 && lineSep.Length > 0) output.Append(lineSep);
            output.Append(line);
            lineNum++;
        }

        return output.ToString();
    }

    private static string TakeAlignSegment(AlignColumn col, int width, out int visLen, out bool skipSpaces)
    {
        var glyphs = col.Glyphs;
        int start = col.Pos;
        int len = 0;
        int lastSpace = -1;
        skipSpaces = false;

        int cur = start;
        while (len < width && cur < glyphs.Count && glyphs[cur].Char != '\n')
        {
            if (char.IsWhiteSpace(glyphs[cur].Char)) lastSpace = cur;
            cur++;
            len++;
        }
        if (cur < glyphs.Count && char.IsWhiteSpace(glyphs[cur].Char) && glyphs[cur].Char != '\n')
            lastSpace = cur;

        if (cur >= glyphs.Count)
        {
            col.Pos = cur;
            visLen = len;
            return BuildGlyphSlice(col, start, len);
        }

        if (glyphs[cur].Char == '\n')
        {
            col.Pos = col.TruncAll ? glyphs.Count : cur + 1;
            visLen = len;
            return BuildGlyphSlice(col, start, len);
        }

        if (lastSpace >= start && !col.TruncAll && !col.TruncEach)
        {
            int end = lastSpace;
            while (end >= start && char.IsWhiteSpace(glyphs[end].Char)) end--;
            int segLen = Math.Max(0, end - start + 1);
            col.Pos = lastSpace;
            skipSpaces = true;
            visLen = segLen;
            return BuildGlyphSlice(col, start, segLen);
        }

        if (col.TruncAll)
        {
            col.Pos = glyphs.Count;
        }
        else if (col.TruncEach)
        {
            int nl = -1;
            for (int k = cur; k < glyphs.Count; k++)
            {
                if (glyphs[k].Char == '\n') { nl = k; break; }
            }
            col.Pos = nl < 0 ? glyphs.Count : nl + 1;
        }
        else
        {
            col.Pos = cur;
        }
        visLen = len;
        return BuildGlyphSlice(col, start, len);
    }

    private static string BuildGlyphSlice(AlignColumn col, int start, int count)
    {
        if (count <= 0)
            return string.Empty;

        var glyphs = col.Glyphs;
        var sb = new StringBuilder(count * 2);
        bool hasOpenAnsi = false;

        // Track active ANSI sequences before `start`, discarding sequences cleared by \x1B[0m
        var carried = new StringBuilder();
        for (int i = 0; i < start; i++)
        {
            string pfx = glyphs[i].Prefix;
            if (pfx.Length == 0) continue;

            int lastReset = pfx.LastIndexOf("\x1B[0m", StringComparison.Ordinal);
            if (lastReset >= 0)
            {
                carried.Clear();
                string afterReset = pfx.Substring(lastReset + 4);
                if (afterReset.Length > 0)
                    carried.Append(afterReset);
            }
            else
            {
                carried.Append(pfx);
            }
        }

        if (carried.Length > 0)
        {
            sb.Append(carried);
            hasOpenAnsi = true;
        }

        for (int i = start; i < start + count; i++)
        {
            string pfx = glyphs[i].Prefix;
            if (pfx.Length > 0)
            {
                sb.Append(pfx);
                hasOpenAnsi = !pfx.EndsWith("\x1B[0m", StringComparison.Ordinal);
            }
            sb.Append(glyphs[i].Char);
        }

        if (start + count == glyphs.Count && col.TrailingMarkup.Length > 0)
        {
            sb.Append(col.TrailingMarkup);
        }
        else if (hasOpenAnsi)
        {
            sb.Append("\x1B[0m");
        }

        return sb.ToString();
    }

    private static void FormatAlignSegment(
        StringBuilder line,
        string seg,
        int visLen,
        AlignMode mode,
        int width,
        char filler,
        bool noFill,
        string? ansiOpen,
        string? ansiClose)
    {
        int pad = Math.Max(0, width - visLen);
        bool wrapAnsi = !string.IsNullOrEmpty(ansiOpen);
        if (wrapAnsi)
            line.Append(ansiOpen);

        string segOut = wrapAnsi && seg.Contains("\x1B[0m", StringComparison.Ordinal)
            ? seg.Replace("\x1B[0m", "\x1B[0m" + ansiOpen, StringComparison.Ordinal)
            : seg;

        switch (mode)
        {
            case AlignMode.Right:
                if (visLen > 0 || !noFill) line.Append(filler, pad);
                line.Append(segOut);
                break;
            case AlignMode.Center:
                int leftPad = pad >> 1;
                line.Append(filler, leftPad);
                line.Append(segOut);
                if (!noFill) line.Append(filler, (pad >> 1) + (pad & 1));
                break;
            case AlignMode.Full:
            case AlignMode.WpFull:
                bool isWp = mode == AlignMode.WpFull;
                int numSpaces = 0;
                for (int i = 0; i < segOut.Length; i++)
                {
                    if (char.IsWhiteSpace(segOut[i])) numSpaces++;
                }
                if (pad > 0 && numSpaces > 0 && (!isWp || (width / pad) >= 2))
                {
                    int extra = pad / numSpaces;
                    int rem = pad % numSpaces;
                    int seen = 0;
                    foreach (char c in segOut)
                    {
                        line.Append(c);
                        if (char.IsWhiteSpace(c))
                        {
                            int add = extra + (seen < rem ? 1 : 0);
                            line.Append(c, add);
                            seen++;
                        }
                    }
                    if (wrapAnsi)
                        AppendAlignAnsiClose(line, ansiOpen!, ansiClose!);
                    return;
                }
                line.Append(segOut);
                if (!noFill) line.Append(filler, pad);
                break;
            default:
                line.Append(segOut);
                if (!noFill) line.Append(filler, pad);
                break;
        }

        if (wrapAnsi)
            AppendAlignAnsiClose(line, ansiOpen!, ansiClose!);
    }

    private static void AppendAlignAnsiClose(StringBuilder line, string ansiOpen, string ansiClose)
    {
        // Avoid redundant <ansiOpen><ansiClose> if the cell ended right at an internal reset
        string redundantSuffix = "\x1B[0m" + ansiOpen;
        if (line.Length >= redundantSuffix.Length)
        {
            bool match = true;
            int offset = line.Length - redundantSuffix.Length;
            for (int k = 0; k < redundantSuffix.Length; k++)
            {
                if (line[offset + k] != redundantSuffix[k])
                {
                    match = false;
                    break;
                }
            }
            if (match)
            {
                line.Remove(line.Length - ansiOpen.Length, ansiOpen.Length);
                return;
            }
        }
        line.Append(ansiClose);
    }
}
