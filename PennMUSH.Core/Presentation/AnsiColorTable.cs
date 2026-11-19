namespace PennMUSH.Core.Presentation;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PennMUSH.Core.Protocol;

public readonly record struct ColorEntry(
    string Name,
    byte R,
    byte G,
    byte B,
    int Xterm,
    int AnsiCode)
{
    public int RgbInt => (R << 16) | (G << 8) | B;
    public string Hex => $"#{R:x2}{G:x2}{B:x2}";
    public bool IsHiliteAnsi16 => (AnsiCode & 0x0100) != 0;
    public int Ansi16Index => AnsiCode & 0x07;
}

public readonly record struct ParsedColorValue(
    byte R,
    byte G,
    byte B,
    int Xterm,
    int Ansi16Index,
    bool Ansi16Hilite,
    string SgrCodeFg,
    string SgrCodeBg,
    string AutoToken,
    bool IsDefault = false,
    bool IsLegacyAnsi16 = false);

public sealed class ParsedAnsiSpec
{
    public bool ResetAll { get; set; }
    public bool Bold { get; set; }
    public bool BoldOff { get; set; }
    public bool Underline { get; set; }
    public bool UnderlineOff { get; set; }
    public bool Flash { get; set; }
    public bool FlashOff { get; set; }
    public bool Inverse { get; set; }
    public bool InverseOff { get; set; }
    public ParsedColorValue? Foreground { get; set; }
    public ParsedColorValue? Background { get; set; }

    public bool HasAnyStyling =>
        Bold || BoldOff ||
        Underline || UnderlineOff ||
        Flash || FlashOff ||
        Inverse || InverseOff ||
        Foreground.HasValue ||
        Background.HasValue;

    public string BuildSgrCodes()
    {
        var codes = new List<string>(8);
        if (Bold) codes.Add("1");
        if (Underline) codes.Add("4");
        if (Flash) codes.Add("5");
        if (Inverse) codes.Add("7");
        if (BoldOff) codes.Add("22");
        if (UnderlineOff) codes.Add("24");
        if (FlashOff) codes.Add("25");
        if (InverseOff) codes.Add("27");
        if (Foreground.HasValue) codes.Add(Foreground.Value.SgrCodeFg);
        if (Background.HasValue) codes.Add(Background.Value.SgrCodeBg);
        return string.Join(";", codes);
    }
}

public static class AnsiColorTable
{
    private sealed record Colormap16Entry(int Index, char Letter, bool Hilite, byte R, byte G, byte B);

    // Exact PennMUSH / xterm 16-color palette (src/markup.c colormap_16)
    private static readonly Colormap16Entry[] Colormap16 =
    {
        new(0, 'x', false, 0x00, 0x00, 0x00),
        new(1, 'r', false, 0xcd, 0x00, 0x00),
        new(2, 'g', false, 0x00, 0xcd, 0x00),
        new(3, 'y', false, 0xcd, 0xcd, 0x00),
        new(4, 'b', false, 0x00, 0x00, 0xee),
        new(5, 'm', false, 0xcd, 0x00, 0xcd),
        new(6, 'c', false, 0x00, 0xcd, 0xcd),
        new(7, 'w', false, 0xe5, 0xe5, 0xe5),

        new(0, 'x', true,  0x7f, 0x7f, 0x7f),
        new(1, 'r', true,  0xff, 0x00, 0x00),
        new(2, 'g', true,  0x00, 0xff, 0x00),
        new(3, 'y', true,  0xff, 0xff, 0x00),
        new(4, 'b', true,  0x5c, 0x5c, 0xff),
        new(5, 'm', true,  0xff, 0x00, 0xff),
        new(6, 'c', true,  0x00, 0xff, 0xff),
        new(7, 'w', true,  0xff, 0xff, 0xff),
    };

    private sealed class ColorTableData
    {
        public required Dictionary<string, ColorEntry> ByName { get; init; }
        public required Dictionary<int, List<string>> ByRgbNames { get; init; }
        public required Dictionary<int, ColorEntry> ByRgbEntry { get; init; }
        public required IReadOnlyList<string> NamedColorNames { get; init; }
        public required IReadOnlyList<string> AllColorNames { get; init; }
        public required (byte R, byte G, byte B)[] XtermRgb { get; init; }
    }

    private static readonly Lazy<ColorTableData> Table = new(LoadEmbeddedTable);

    private static readonly Regex AnsiEscapeRegex = new(@"\x1B\[[0-9;?]*[a-zA-Z]", RegexOptions.Compiled);

    public static IReadOnlyList<string> NamedColorNames => Table.Value.NamedColorNames;
    public static IReadOnlyList<string> AllColorNames => Table.Value.AllColorNames;

    private static ColorTableData LoadEmbeddedTable()
    {
        var assembly = typeof(AnsiColorTable).Assembly;
        using Stream? stream = assembly.GetManifestResourceStream("PennMUSH.Core.Presentation.colors.json");
        if (stream == null)
            throw new InvalidOperationException("Embedded resource 'PennMUSH.Core.Presentation.colors.json' not found.");

        using var doc = JsonDocument.Parse(stream);
        var byName = new Dictionary<string, ColorEntry>(StringComparer.OrdinalIgnoreCase);
        var byRgbNames = new Dictionary<int, List<string>>();
        var byRgbEntry = new Dictionary<int, ColorEntry>();
        var namedList = new List<string>();
        var allList = new List<string>();
        var xtermRgb = new (byte R, byte G, byte B)[256];

        foreach (JsonElement item in doc.RootElement.EnumerateArray())
        {
            string name = item.GetProperty("name").GetString() ?? string.Empty;
            string rgbText = item.GetProperty("rgb").GetString() ?? "0x000000";
            int xterm = item.GetProperty("xterm").GetInt32();
            int ansi = item.GetProperty("ansi").GetInt32();

            int rgbVal = Convert.ToInt32(rgbText, 16);
            byte r = (byte)((rgbVal >> 16) & 0xFF);
            byte g = (byte)((rgbVal >> 8) & 0xFF);
            byte b = (byte)(rgbVal & 0xFF);

            var entry = new ColorEntry(name, r, g, b, xterm, ansi);
            byName[name] = entry;
            allList.Add(name);

            bool isXtermName = name.StartsWith("xterm", StringComparison.OrdinalIgnoreCase);
            if (isXtermName && int.TryParse(name.AsSpan(5), out int xIdx) && xIdx is >= 0 and <= 255)
            {
                xtermRgb[xIdx] = (r, g, b);
                byRgbEntry.TryAdd(rgbVal, entry);
            }
            else if (!isXtermName)
            {
                namedList.Add(name);
                if (!byRgbNames.TryGetValue(rgbVal, out var names))
                {
                    names = new List<string>();
                    byRgbNames[rgbVal] = names;
                }
                names.Add(name);
                byRgbEntry[rgbVal] = entry;
            }
        }

        namedList.Sort(StringComparer.OrdinalIgnoreCase);
        allList.Sort(StringComparer.OrdinalIgnoreCase);
        foreach (var list in byRgbNames.Values)
            list.Sort(StringComparer.OrdinalIgnoreCase);

        return new ColorTableData
        {
            ByName = byName,
            ByRgbNames = byRgbNames,
            ByRgbEntry = byRgbEntry,
            NamedColorNames = namedList,
            AllColorNames = allList,
            XtermRgb = xtermRgb
        };
    }

    public static (byte R, byte G, byte B) GetAnsi16Rgb(int index, bool hilite)
    {
        int clamped = Math.Clamp(index, 0, 7);
        var entry = Colormap16[hilite ? clamped + 8 : clamped];
        return (entry.R, entry.G, entry.B);
    }

    public static string GetAnsi16Hex(int index, bool hilite)
    {
        var (r, g, b) = GetAnsi16Rgb(index, hilite);
        return $"#{r:x2}{g:x2}{b:x2}";
    }

    public static char GetAnsi16Letter(int index)
        => index is >= 0 and <= 7 ? Colormap16[index].Letter : 'd';

    public static (byte R, byte G, byte B) GetXtermRgb(int index)
        => Table.Value.XtermRgb[Math.Clamp(index, 0, 255)];

    public static string GetXtermHex(int index)
    {
        var (r, g, b) = GetXtermRgb(index);
        return $"#{r:x2}{g:x2}{b:x2}";
    }

    public static bool TryGetNamedColor(string? name, out ColorEntry entry)
    {
        entry = default;
        if (string.IsNullOrWhiteSpace(name))
            return false;

        string cleaned = CollapseColorNameWhitespace(name.Trim().TrimStart('+'));
        if (cleaned.Length == 0)
            return false;

        return Table.Value.ByName.TryGetValue(cleaned, out entry);
    }

    public static IReadOnlyList<string> FindNamesByRgb(byte r, byte g, byte b)
    {
        int key = (r << 16) | (g << 8) | b;
        return Table.Value.ByRgbNames.TryGetValue(key, out var list)
            ? list
            : Array.Empty<string>();
    }

    public static int RgbToNearestXterm(byte r, byte g, byte b)
    {
        int key = (r << 16) | (g << 8) | b;
        if (Table.Value.ByRgbEntry.TryGetValue(key, out var cached))
            return cached.Xterm;

        var xtermRgb = Table.Value.XtermRgb;
        int best = 0;
        long bestDiff = long.MaxValue;
        for (int i = 0; i < 256; i++)
        {
            var (xr, xg, xb) = xtermRgb[i];
            long dr = r - xr;
            long dg = g - xg;
            long db = b - xb;
            long diff = dr * dr + dg * dg + db * db;
            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = i;
            }
        }
        return best;
    }

    public static int RgbToNearestAnsi16(byte r, byte g, byte b, out bool hilite)
    {
        int key = (r << 16) | (g << 8) | b;
        if (Table.Value.ByRgbEntry.TryGetValue(key, out var cached))
        {
            hilite = cached.IsHiliteAnsi16;
            return cached.Ansi16Index;
        }

        int best = 7;
        long bestDiff = long.MaxValue;
        for (int i = 0; i < 8; i++)
        {
            var entry = Colormap16[i];
            long dr = r - entry.R;
            long dg = g - entry.G;
            long db = b - entry.B;
            long diff = dr * dr + dg * dg + db * db;
            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = i;
            }
        }
        hilite = false;
        return best;
    }

    public static string RgbToNearestAnsi16Hex(byte r, byte g, byte b)
    {
        int idx = RgbToNearestAnsi16(r, g, b, out bool hilite);
        return GetAnsi16Hex(idx, hilite);
    }

    public static bool TryBuildAnsiSgr(string? spec, out string sgrOpen, out string sgrClose, out bool hasAnsi)
    {
        sgrOpen = string.Empty;
        sgrClose = string.Empty;
        hasAnsi = false;

        if (string.IsNullOrWhiteSpace(spec))
            return true;

        if (!TryParseAnsiSpec(spec, out var parsed))
            return false;

        if (!parsed.HasAnyStyling)
            return true;

        string codes = parsed.BuildSgrCodes();
        if (string.IsNullOrEmpty(codes))
            return true;

        sgrOpen = $"\x1B[{codes}m";
        sgrClose = "\x1B[0m";
        hasAnsi = true;
        return true;
    }

    public static bool TryParseAnsiSpec(string? spec, out ParsedAnsiSpec parsed)
    {
        parsed = new ParsedAnsiSpec();
        if (string.IsNullOrWhiteSpace(spec))
            return true;

        var tokens = TokenizeSpec(spec);
        if (tokens == null)
            return false;

        foreach (string rawToken in tokens)
        {
            if (rawToken.StartsWith('/') || rawToken.StartsWith('!'))
            {
                string bgToken = rawToken[1..];
                if (string.IsNullOrEmpty(bgToken))
                    continue;
                if (!TryParseColorValue(bgToken, isBg: true, out var bgVal))
                {
                    if (!ApplyCompactCodes(bgToken, parsed, forceBg: true))
                        return false;
                }
                else
                {
                    parsed.Background = bgVal;
                }
                continue;
            }

            int splitIdx = FindBackgroundSeparatorOutsideAngles(rawToken);
            if (splitIdx > 0)
            {
                string fgPart = rawToken[..splitIdx];
                string bgPart = rawToken[(splitIdx + 1)..];
                if (string.IsNullOrEmpty(fgPart) || string.IsNullOrEmpty(bgPart))
                    return false;

                if (!TryParseSinglePart(fgPart, parsed, forceBg: false))
                    return false;
                if (!TryParseSinglePart(bgPart, parsed, forceBg: true))
                    return false;
                continue;
            }

            if (!TryParseSinglePart(rawToken, parsed, forceBg: false))
                return false;
        }

        return true;
    }

    private static bool TryParseSinglePart(string token, ParsedAnsiSpec parsed, bool forceBg)
    {
        if (TryParseColorValue(token, forceBg, out var val))
        {
            if (forceBg)
                parsed.Background = val;
            else
                parsed.Foreground = val;
            return true;
        }

        return ApplyCompactCodes(token, parsed, forceBg);
    }

    private static bool TryParseColorValue(string rawToken, bool isBg, out ParsedColorValue value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(rawToken))
            return false;

        string token = rawToken.Trim();

        // 1. Angle-bracket form: <#RRGGBB> or <R G B>
        if (token.StartsWith('<') && token.EndsWith('>'))
        {
            string inner = token[1..^1].Trim();
            if (TryParseHex6(inner, out byte r, out byte g, out byte b))
            {
                value = BuildRgbColorValue(r, g, b, $"#{r:x2}{g:x2}{b:x2}");
                return true;
            }

            var parts = inner.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 3 &&
                int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int rInt) && rInt is >= 0 and <= 255 &&
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int gInt) && gInt is >= 0 and <= 255 &&
                int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int bInt) && bInt is >= 0 and <= 255)
            {
                value = BuildRgbColorValue((byte)rInt, (byte)gInt, (byte)bInt, $"#{rInt:x2}{gInt:x2}{bInt:x2}");
                return true;
            }
            return false;
        }

        // 2. Hex form: #RRGGBB or #RGB
        if (token.StartsWith('#'))
        {
            if (TryParseHex6(token, out byte r6, out byte g6, out byte b6))
            {
                value = BuildRgbColorValue(r6, g6, b6, $"#{r6:x2}{g6:x2}{b6:x2}");
                return true;
            }
            if (token.Length == 4 &&
                int.TryParse(token.AsSpan(1, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r3) &&
                int.TryParse(token.AsSpan(2, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g3) &&
                int.TryParse(token.AsSpan(3, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b3))
            {
                byte r = (byte)(r3 * 17);
                byte g = (byte)(g3 * 17);
                byte b = (byte)(b3 * 17);
                value = BuildRgbColorValue(r, g, b, $"#{r:x2}{g:x2}{b:x2}");
                return true;
            }
            return false;
        }

        // 3. 0x... form: 0xN/0xNN (xterm), 0xRGB, 0xRRGGBB
        if (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            string hexDigits = token[2..];
            if (hexDigits.Length is 1 or 2 &&
                int.TryParse(hexDigits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int xIdx) &&
                xIdx is >= 0 and <= 255)
            {
                value = BuildXtermColorValue(xIdx);
                return true;
            }
            if (hexDigits.Length == 3 &&
                int.TryParse(hexDigits.AsSpan(0, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r3) &&
                int.TryParse(hexDigits.AsSpan(1, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g3) &&
                int.TryParse(hexDigits.AsSpan(2, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b3))
            {
                byte r = (byte)(r3 * 17);
                byte g = (byte)(g3 * 17);
                byte b = (byte)(b3 * 17);
                value = BuildRgbColorValue(r, g, b, $"#{r:x2}{g:x2}{b:x2}");
                return true;
            }
            if (hexDigits.Length == 6 &&
                byte.TryParse(hexDigits.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r6) &&
                byte.TryParse(hexDigits.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g6) &&
                byte.TryParse(hexDigits.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b6))
            {
                value = BuildRgbColorValue(r6, g6, b6, $"#{r6:x2}{g6:x2}{b6:x2}");
                return true;
            }
            return false;
        }

        // 4. Bare integer 0..255 -> xterm index
        if (AllAsciiDigits(token))
        {
            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int xIdx) && xIdx is >= 0 and <= 255)
            {
                value = BuildXtermColorValue(xIdx);
                return true;
            }
            return false;
        }

        // 5. Explicit +name (or +xtermN)
        if (token.StartsWith('+'))
        {
            string named = token[1..];
            if (named.StartsWith("xterm", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(named.AsSpan(5), NumberStyles.Integer, CultureInfo.InvariantCulture, out int plusXterm) &&
                plusXterm is >= 0 and <= 255)
            {
                value = BuildXtermColorValue(plusXterm);
                return true;
            }

            if (TryGetNamedColor(named, out var entry))
            {
                value = BuildNamedColorValue(entry);
                return true;
            }
            return false;
        }

        // 6. Bare xtermN
        if (token.StartsWith("xterm", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(token.AsSpan(5), NumberStyles.Integer, CultureInfo.InvariantCulture, out int bareXterm) &&
            bareXterm is >= 0 and <= 255)
        {
            value = BuildXtermColorValue(bareXterm);
            return true;
        }

        // 7. Bare multi-character X11/CSS named color (e.g., "red", "cornflowerblue")
        if (token.Length > 1 && TryGetNamedColor(token, out var namedEntry))
        {
            value = BuildNamedColorValue(namedEntry);
            return true;
        }

        // 8. When parsing a background segment (e.g., after '/'), single lowercase color letter maps to bg
        if (isBg && token.Length == 1 && TryGetAnsi16Index(token[0], out int bgIdx))
        {
            var (r, g, b) = GetAnsi16Rgb(bgIdx, hilite: false);
            int xterm = RgbToNearestXterm(r, g, b);
            char upper = char.ToUpperInvariant(token[0]);
            value = new ParsedColorValue(
                R: r, G: g, B: b,
                Xterm: xterm,
                Ansi16Index: bgIdx,
                Ansi16Hilite: false,
                SgrCodeFg: (30 + bgIdx).ToString(),
                SgrCodeBg: (40 + bgIdx).ToString(),
                AutoToken: upper.ToString(),
                IsDefault: false,
                IsLegacyAnsi16: true);
            return true;
        }

        return false;
    }

    private static bool ApplyCompactCodes(string token, ParsedAnsiSpec parsed, bool forceBg)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        foreach (char ch in token)
        {
            if (forceBg)
            {
                if (TryGetAnsi16Index(ch, out int bgIdx))
                {
                    var (r, g, b) = GetAnsi16Rgb(bgIdx, hilite: false);
                    parsed.Background = new ParsedColorValue(
                        r, g, b,
                        RgbToNearestXterm(r, g, b),
                        bgIdx,
                        Ansi16Hilite: false,
                        SgrCodeFg: (30 + bgIdx).ToString(),
                        SgrCodeBg: (40 + bgIdx).ToString(),
                        AutoToken: char.ToUpperInvariant(ch).ToString(),
                        IsDefault: false,
                        IsLegacyAnsi16: true);
                    continue;
                }
                if (ch is 'd' or 'D')
                {
                    parsed.Background = new ParsedColorValue(0, 0, 0, 0, -1, false, "39", "49", "D", IsDefault: true, IsLegacyAnsi16: true);
                    continue;
                }
                return false;
            }

            switch (ch)
            {
                case 'n':
                    parsed.ResetAll = true;
                    parsed.Bold = parsed.Underline = parsed.Flash = parsed.Inverse = false;
                    parsed.BoldOff = parsed.UnderlineOff = parsed.FlashOff = parsed.InverseOff = false;
                    parsed.Foreground = null;
                    parsed.Background = null;
                    break;
                case 'h':
                    parsed.Bold = true;
                    parsed.BoldOff = false;
                    break;
                case 'H':
                    parsed.BoldOff = true;
                    parsed.Bold = false;
                    break;
                case 'u':
                    parsed.Underline = true;
                    parsed.UnderlineOff = false;
                    break;
                case 'U':
                    parsed.UnderlineOff = true;
                    parsed.Underline = false;
                    break;
                case 'f':
                    parsed.Flash = true;
                    parsed.FlashOff = false;
                    break;
                case 'F':
                    parsed.FlashOff = true;
                    parsed.Flash = false;
                    break;
                case 'i':
                    parsed.Inverse = true;
                    parsed.InverseOff = false;
                    break;
                case 'I':
                    parsed.InverseOff = true;
                    parsed.Inverse = false;
                    break;
                case 'd':
                    parsed.Foreground = new ParsedColorValue(0, 0, 0, 0, -1, false, "39", "49", "d", IsDefault: true, IsLegacyAnsi16: true);
                    break;
                case 'D':
                    parsed.Background = new ParsedColorValue(0, 0, 0, 0, -1, false, "39", "49", "D", IsDefault: true, IsLegacyAnsi16: true);
                    break;
                default:
                    if (TryGetAnsi16Index(ch, out int colIdx))
                    {
                        bool bgUpper = char.IsUpper(ch);
                        var (r, g, b) = GetAnsi16Rgb(colIdx, hilite: false);
                        var cv = new ParsedColorValue(
                            r, g, b,
                            RgbToNearestXterm(r, g, b),
                            colIdx,
                            Ansi16Hilite: false,
                            SgrCodeFg: (30 + colIdx).ToString(),
                            SgrCodeBg: (40 + colIdx).ToString(),
                            AutoToken: ch.ToString(),
                            IsDefault: false,
                            IsLegacyAnsi16: true);
                        if (bgUpper)
                            parsed.Background = cv;
                        else
                            parsed.Foreground = cv;
                        break;
                    }
                    return false;
            }
        }

        return true;
    }

    private static ParsedColorValue BuildRgbColorValue(byte r, byte g, byte b, string autoToken)
    {
        int xterm = RgbToNearestXterm(r, g, b);
        int ansi16 = RgbToNearestAnsi16(r, g, b, out bool hilite);
        return new ParsedColorValue(
            R: r, G: g, B: b,
            Xterm: xterm,
            Ansi16Index: ansi16,
            Ansi16Hilite: hilite,
            SgrCodeFg: $"38;2;{r};{g};{b}",
            SgrCodeBg: $"48;2;{r};{g};{b}",
            AutoToken: autoToken);
    }

    private static ParsedColorValue BuildXtermColorValue(int index)
    {
        int clamped = Math.Clamp(index, 0, 255);
        var (r, g, b) = GetXtermRgb(clamped);
        string xName = $"xterm{clamped}";
        int ansi16 = Table.Value.ByName.TryGetValue(xName, out var entry)
            ? entry.Ansi16Index
            : RgbToNearestAnsi16(r, g, b, out _);
        bool hilite = Table.Value.ByName.TryGetValue(xName, out var entry2) && entry2.IsHiliteAnsi16;
        return new ParsedColorValue(
            R: r, G: g, B: b,
            Xterm: clamped,
            Ansi16Index: ansi16,
            Ansi16Hilite: hilite,
            SgrCodeFg: $"38;5;{clamped}",
            SgrCodeBg: $"48;5;{clamped}",
            AutoToken: $"+xterm{clamped}");
    }

    private static ParsedColorValue BuildNamedColorValue(ColorEntry entry)
    {
        return new ParsedColorValue(
            R: entry.R, G: entry.G, B: entry.B,
            Xterm: entry.Xterm,
            Ansi16Index: entry.Ansi16Index,
            Ansi16Hilite: entry.IsHiliteAnsi16,
            SgrCodeFg: $"38;2;{entry.R};{entry.G};{entry.B}",
            SgrCodeBg: $"48;2;{entry.R};{entry.G};{entry.B}",
            AutoToken: $"+{entry.Name}");
    }

    private static List<string>? TokenizeSpec(string spec)
    {
        var tokens = new List<string>();
        int i = 0;
        while (i < spec.Length)
        {
            while (i < spec.Length && char.IsWhiteSpace(spec[i]))
                i++;
            if (i >= spec.Length)
                break;

            var sb = new StringBuilder();
            bool inAngles = false;
            while (i < spec.Length)
            {
                char ch = spec[i];
                if (ch == '<')
                {
                    inAngles = true;
                    sb.Append(ch);
                    i++;
                }
                else if (ch == '>')
                {
                    inAngles = false;
                    sb.Append(ch);
                    i++;
                }
                else if (!inAngles && char.IsWhiteSpace(ch))
                {
                    break;
                }
                else
                {
                    sb.Append(ch);
                    i++;
                }
            }

            if (inAngles)
                return null;

            tokens.Add(sb.ToString());
        }

        return tokens;
    }

    private static int FindBackgroundSeparatorOutsideAngles(string token)
    {
        bool inAngles = false;
        for (int i = 0; i < token.Length; i++)
        {
            char c = token[i];
            if (c == '<') inAngles = true;
            else if (c == '>') inAngles = false;
            else if (!inAngles && (c == '/' || c == '!')) return i;
        }
        return -1;
    }

    private static bool TryParseHex6(string token, out byte r, out byte g, out byte b)
    {
        r = g = b = 0;
        return token.Length == 7
            && token[0] == '#'
            && byte.TryParse(token.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r)
            && byte.TryParse(token.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)
            && byte.TryParse(token.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
    }

    private static bool AllAsciiDigits(string token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        foreach (char c in token)
        {
            if (c < '0' || c > '9')
                return false;
        }
        return true;
    }

    private static bool TryGetAnsi16Index(char ch, out int index)
    {
        index = char.ToLowerInvariant(ch) switch
        {
            'x' => 0,
            'r' => 1,
            'g' => 2,
            'y' => 3,
            'b' => 4,
            'm' => 5,
            'c' => 6,
            'w' => 7,
            _ => -1
        };
        return index >= 0;
    }

    private static string CollapseColorNameWhitespace(string text)
    {
        bool hasWhitespace = false;
        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                hasWhitespace = true;
                break;
            }
        }
        if (!hasWhitespace)
            return text;

        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (!char.IsWhiteSpace(c))
                sb.Append(c);
        }
        return sb.ToString();
    }

    public static string StripAnsi(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        if (text.IndexOf('\x1B') < 0)
            return text;
        return AnsiEscapeRegex.Replace(text, string.Empty);
    }

    /// <summary>
    /// Sanitizes a moniker string by removing WebSocket/Pueblo markup frames, non-SGR escape
    /// sequences, underline (SGR 4/24), and flash/blink (SGR 5/6/25), matching PennMUSH sanitize_moniker().
    /// </summary>
    public static string SanitizeMoniker(string? input, out bool hasRemainingAnsi)
    {
        hasRemainingAnsi = false;
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        string working = WebSocketMarkup.ContainsFrames(input)
            ? WebSocketMarkup.Render(input, isWebSocket: false).Text
            : input;

        var sb = new StringBuilder(working.Length);
        bool ansiActive = false;
        int i = 0;

        while (i < working.Length)
        {
            if (working[i] == '\x1B' && i + 1 < working.Length && working[i + 1] == '[')
            {
                int end = i + 2;
                while (end < working.Length && (char.IsDigit(working[end]) || working[end] == ';' || working[end] == '?'))
                    end++;

                if (end < working.Length && char.IsLetter(working[end]))
                {
                    char finalChar = working[end];
                    string paramStr = working.Substring(i + 2, end - (i + 2));
                    i = end + 1;

                    if (finalChar != 'm')
                        continue;

                    string sanitizedSgr = SanitizeMonikerSgr(paramStr, ref ansiActive, ref hasRemainingAnsi);
                    if (sanitizedSgr.Length > 0)
                        sb.Append("\x1B[").Append(sanitizedSgr).Append('m');
                    continue;
                }
            }

            sb.Append(working[i]);
            i++;
        }

        return sb.ToString();
    }

    private static string SanitizeMonikerSgr(string paramStr, ref bool ansiActive, ref bool hasRemainingAnsi)
    {
        if (string.IsNullOrEmpty(paramStr))
        {
            if (!ansiActive) return string.Empty;
            ansiActive = false;
            return "0";
        }

        string[] parts = paramStr.Split(';');
        var kept = new List<string>(parts.Length);

        for (int k = 0; k < parts.Length; k++)
        {
            if (string.IsNullOrEmpty(parts[k]))
            {
                if (ansiActive)
                {
                    kept.Add("0");
                    ansiActive = false;
                }
                continue;
            }

            if (!int.TryParse(parts[k], NumberStyles.Integer, CultureInfo.InvariantCulture, out int code))
                continue;

            if (code == 0)
            {
                if (ansiActive)
                {
                    kept.Add("0");
                    ansiActive = false;
                }
                continue;
            }

            // Strip underline (4, 24) and flash/blink (5, 6, 25) per PennMUSH sanitize_moniker()
            if (code is 4 or 5 or 6 or 24 or 25)
                continue;

            // Extended color sequences: 38/48 ; 5 ; N  or  38/48 ; 2 ; R ; G ; B
            if (code is 38 or 48 && k + 1 < parts.Length &&
                int.TryParse(parts[k + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int mode))
            {
                if (mode == 5 && k + 2 < parts.Length &&
                    int.TryParse(parts[k + 2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int xterm) &&
                    xterm is >= 0 and <= 255)
                {
                    kept.Add(parts[k]);
                    kept.Add("5");
                    kept.Add(xterm.ToString(CultureInfo.InvariantCulture));
                    k += 2;
                    ansiActive = true;
                    hasRemainingAnsi = true;
                    continue;
                }

                if (mode == 2 && k + 4 < parts.Length &&
                    int.TryParse(parts[k + 2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int r) &&
                    int.TryParse(parts[k + 3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int g) &&
                    int.TryParse(parts[k + 4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int b))
                {
                    kept.Add(parts[k]);
                    kept.Add("2");
                    kept.Add(Math.Clamp(r, 0, 255).ToString(CultureInfo.InvariantCulture));
                    kept.Add(Math.Clamp(g, 0, 255).ToString(CultureInfo.InvariantCulture));
                    kept.Add(Math.Clamp(b, 0, 255).ToString(CultureInfo.InvariantCulture));
                    k += 4;
                    ansiActive = true;
                    hasRemainingAnsi = true;
                    continue;
                }
            }

            if (code is 1 or 2 or 3 or 7 or 9 or (>= 30 and <= 37) or (>= 40 and <= 47) or (>= 90 and <= 97) or (>= 100 and <= 107))
            {
                kept.Add(parts[k]);
                ansiActive = true;
                hasRemainingAnsi = true;
            }
            else if (code is 22 or 23 or 27 or 29 or 39 or 49)
            {
                if (ansiActive)
                    kept.Add(parts[k]);
            }
        }

        return string.Join(";", kept);
    }
}
