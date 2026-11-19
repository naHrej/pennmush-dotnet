namespace PennMUSH.Core.Presentation;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class AnsiToHtmlTranslator
{
    private struct AnsiState
    {
        public int? BaseFgIndex;
        public string? ExplicitFgHex;
        public string? BgHex;
        public bool Bold;
        public bool Dim;
        public bool Italic;
        public bool Underline;
        public bool Blink;
        public bool Inverse;
        public bool Strikethrough;

        public void Reset()
        {
            BaseFgIndex = null;
            ExplicitFgHex = null;
            BgHex = null;
            Bold = false;
            Dim = false;
            Italic = false;
            Underline = false;
            Blink = false;
            Inverse = false;
            Strikethrough = false;
        }

        public RenderedSpanStyle ToSpanStyle()
        {
            string? fg = ExplicitFgHex;
            if (fg == null && BaseFgIndex.HasValue)
                fg = AnsiColorTable.GetAnsi16Hex(BaseFgIndex.Value, Bold);

            string? bg = BgHex;
            if (Inverse)
            {
                string swappedFg = bg ?? "#000000";
                string swappedBg = fg ?? "#e5e5e5";
                fg = swappedFg;
                bg = swappedBg;
            }

            return new RenderedSpanStyle(fg, bg, Bold, Dim, Italic, Underline, Strikethrough, Blink);
        }
    }

    private readonly record struct RenderedSpanStyle(
        string? Fg,
        string? Bg,
        bool Bold,
        bool Dim,
        bool Italic,
        bool Underline,
        bool Strikethrough,
        bool Blink)
    {
        public bool IsEmpty =>
            Fg == null &&
            Bg == null &&
            !Bold &&
            !Dim &&
            !Italic &&
            !Underline &&
            !Strikethrough &&
            !Blink;
    }

    public static bool ContainsAnsi(string? text)
        => !string.IsNullOrEmpty(text) && text.IndexOf('\x1B') >= 0;

    public static string StripAnsi(string? text)
        => AnsiColorTable.StripAnsi(text);

    public static string Translate(string text, bool escapeHtml = true, ColorDepth colorMode = ColorDepth.TrueColor)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (!ContainsAnsi(text))
            return escapeHtml ? EncodeHtmlText(text) : text;

        if (colorMode == ColorDepth.None)
        {
            string stripped = StripAnsi(text);
            return escapeHtml ? EncodeHtmlText(stripped) : stripped;
        }

        var sb = new StringBuilder(text.Length + 64);
        var state = new AnsiState();
        var openStyle = new RenderedSpanStyle();
        bool spanOpen = false;
        int textStart = 0;
        int i = 0;

        void FlushTextSegment(int segmentEnd)
        {
            if (segmentEnd <= textStart)
                return;

            var targetStyle = state.ToSpanStyle();
            if (spanOpen && !targetStyle.Equals(openStyle))
            {
                sb.Append("</span>");
                spanOpen = false;
                openStyle = default;
            }

            if (!spanOpen && !targetStyle.IsEmpty)
            {
                AppendOpenSpan(sb, targetStyle);
                spanOpen = true;
                openStyle = targetStyle;
            }

            AppendText(sb, text.AsSpan(textStart, segmentEnd - textStart), escapeHtml);
        }

        while (i < text.Length)
        {
            if (text[i] == '\x1B' && i + 1 < text.Length && text[i + 1] == '[')
            {
                int end = i + 2;
                while (end < text.Length && (char.IsDigit(text[end]) || text[end] == ';' || text[end] == '?'))
                    end++;

                if (end < text.Length && char.IsLetter(text[end]))
                {
                    FlushTextSegment(i);
                    char finalChar = text[end];
                    if (finalChar == 'm')
                    {
                        string paramStr = text.Substring(i + 2, end - (i + 2));
                        ApplySgr(ref state, paramStr, colorMode);
                    }
                    i = end + 1;
                    textStart = i;
                    continue;
                }
            }

            i++;
        }

        FlushTextSegment(text.Length);

        if (spanOpen)
            sb.Append("</span>");

        return sb.ToString();
    }

    private static void ApplySgr(ref AnsiState state, string paramStr, ColorDepth colorMode)
    {
        if (string.IsNullOrEmpty(paramStr))
        {
            state.Reset();
            return;
        }

        string[] parts = paramStr.Split(';');
        for (int k = 0; k < parts.Length; k++)
        {
            if (string.IsNullOrEmpty(parts[k]))
            {
                state.Reset();
                continue;
            }

            if (!int.TryParse(parts[k], NumberStyles.Integer, CultureInfo.InvariantCulture, out int code))
                continue;

            switch (code)
            {
                case 0:
                    state.Reset();
                    break;
                case 1:
                    state.Bold = true;
                    state.Dim = false;
                    break;
                case 2:
                    state.Dim = true;
                    break;
                case 3:
                    state.Italic = true;
                    break;
                case 4:
                    state.Underline = true;
                    break;
                case 5:
                case 6:
                    state.Blink = true;
                    break;
                case 7:
                    state.Inverse = true;
                    break;
                case 9:
                    state.Strikethrough = true;
                    break;
                case 22:
                    state.Bold = false;
                    state.Dim = false;
                    break;
                case 23:
                    state.Italic = false;
                    break;
                case 24:
                    state.Underline = false;
                    break;
                case 25:
                    state.Blink = false;
                    break;
                case 27:
                    state.Inverse = false;
                    break;
                case 29:
                    state.Strikethrough = false;
                    break;
                case >= 30 and <= 37:
                    state.BaseFgIndex = code - 30;
                    state.ExplicitFgHex = null;
                    break;
                case 38:
                case 48:
                    bool isBg = code == 48;
                    if (k + 1 < parts.Length &&
                        int.TryParse(parts[k + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int mode))
                    {
                        if (mode == 5 && k + 2 < parts.Length &&
                            int.TryParse(parts[k + 2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int xterm))
                        {
                            int clamped = Math.Clamp(xterm, 0, 255);
                            string resolved = ResolveXtermColorHex(clamped, colorMode);
                            if (isBg)
                                state.BgHex = resolved;
                            else
                            {
                                state.BaseFgIndex = null;
                                state.ExplicitFgHex = resolved;
                            }
                            k += 2;
                        }
                        else if (mode == 2 && k + 4 < parts.Length &&
                            int.TryParse(parts[k + 2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int r) &&
                            int.TryParse(parts[k + 3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int g) &&
                            int.TryParse(parts[k + 4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int b))
                        {
                            string resolved = ResolveRgbColorHex(
                                (byte)Math.Clamp(r, 0, 255),
                                (byte)Math.Clamp(g, 0, 255),
                                (byte)Math.Clamp(b, 0, 255),
                                colorMode);
                            if (isBg)
                                state.BgHex = resolved;
                            else
                            {
                                state.BaseFgIndex = null;
                                state.ExplicitFgHex = resolved;
                            }
                            k += 4;
                        }
                    }
                    break;
                case 39:
                    state.BaseFgIndex = null;
                    state.ExplicitFgHex = null;
                    break;
                case >= 40 and <= 47:
                    state.BgHex = AnsiColorTable.GetAnsi16Hex(code - 40, hilite: false);
                    break;
                case 49:
                    state.BgHex = null;
                    break;
                case >= 90 and <= 97:
                    state.BaseFgIndex = null;
                    state.ExplicitFgHex = AnsiColorTable.GetAnsi16Hex(code - 90, hilite: true);
                    break;
                case >= 100 and <= 107:
                    state.BgHex = AnsiColorTable.GetAnsi16Hex(code - 100, hilite: true);
                    break;
            }
        }
    }

    private static string ResolveXtermColorHex(int index, ColorDepth colorMode)
    {
        if (colorMode == ColorDepth.Standard16)
        {
            if (index is >= 0 and <= 7)
                return AnsiColorTable.GetAnsi16Hex(index, hilite: false);
            if (index is >= 8 and <= 15)
                return AnsiColorTable.GetAnsi16Hex(index - 8, hilite: true);
            var (xr, xg, xb) = AnsiColorTable.GetXtermRgb(index);
            return AnsiColorTable.RgbToNearestAnsi16Hex(xr, xg, xb);
        }

        return AnsiColorTable.GetXtermHex(index);
    }

    private static string ResolveRgbColorHex(byte r, byte g, byte b, ColorDepth colorMode)
    {
        return colorMode switch
        {
            ColorDepth.Standard16 => AnsiColorTable.RgbToNearestAnsi16Hex(r, g, b),
            ColorDepth.Color256 => AnsiColorTable.GetXtermHex(AnsiColorTable.RgbToNearestXterm(r, g, b)),
            _ => $"#{r:x2}{g:x2}{b:x2}"
        };
    }

    private static void AppendOpenSpan(StringBuilder sb, RenderedSpanStyle style)
    {
        sb.Append("<span");
        if (style.Blink)
            sb.Append(" class=\"ansi-blink\"");

        var css = new List<string>(6);
        if (style.Fg != null) css.Add($"color:{style.Fg}");
        if (style.Bg != null) css.Add($"background-color:{style.Bg}");
        if (style.Bold) css.Add("font-weight:bold");
        if (style.Dim) css.Add("opacity:0.75");
        if (style.Italic) css.Add("font-style:italic");
        if (style.Underline && style.Strikethrough)
            css.Add("text-decoration:underline line-through");
        else if (style.Underline)
            css.Add("text-decoration:underline");
        else if (style.Strikethrough)
            css.Add("text-decoration:line-through");

        if (css.Count > 0)
            sb.Append(" style=\"").Append(string.Join(";", css)).Append('"');

        sb.Append('>');
    }

    private static readonly SearchValues<char> HtmlSpecialChars = SearchValues.Create("&<>\"");

    private static string EncodeHtmlText(string text)
    {
        if (text.AsSpan().IndexOfAny(HtmlSpecialChars) < 0)
            return text;

        var sb = new StringBuilder(text.Length + 8);
        AppendText(sb, text.AsSpan(), escapeHtml: true);
        return sb.ToString();
    }

    private static void AppendText(StringBuilder sb, ReadOnlySpan<char> span, bool escapeHtml)
    {
        if (!escapeHtml || span.IndexOfAny(HtmlSpecialChars) < 0)
        {
            sb.Append(span);
            return;
        }

        foreach (char c in span)
        {
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"': sb.Append("&quot;"); break;
                default: sb.Append(c); break;
            }
        }
    }
}
