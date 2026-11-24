namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Markup-aware string utilities that track ANSI SGR escape sequences separately from visible characters.
/// </summary>
public static class AnsiString
{
    public readonly record struct Glyph(string Prefix, char Char);

    public static List<Glyph> Parse(string? input, out string trailingMarkup)
    {
        trailingMarkup = string.Empty;
        if (string.IsNullOrEmpty(input))
            return new List<Glyph>();

        var glyphs = new List<Glyph>(input.Length);
        var pending = new StringBuilder();
        int i = 0;

        while (i < input.Length)
        {
            if (input[i] == '\x1B' && i + 1 < input.Length && input[i + 1] == '[')
            {
                int end = i + 2;
                while (end < input.Length && (char.IsDigit(input[end]) || input[end] == ';' || input[end] == '?'))
                    end++;
                if (end < input.Length && char.IsLetter(input[end]))
                {
                    pending.Append(input, i, end - i + 1);
                    i = end + 1;
                    continue;
                }
            }

            glyphs.Add(new Glyph(pending.ToString(), input[i]));
            pending.Clear();
            i++;
        }

        trailingMarkup = pending.ToString();
        return glyphs;
    }

    public static int VisibleLength(string? input)
        => Parse(input, out _).Count;

    public static string Strip(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var glyphs = Parse(input, out _);
        var sb = new StringBuilder(glyphs.Count);
        foreach (var g in glyphs)
            sb.Append(g.Char);
        return sb.ToString();
    }

    public static string Substring(string? input, int start, int length)
    {
        if (string.IsNullOrEmpty(input) || length <= 0)
            return string.Empty;

        var glyphs = Parse(input, out string trailing);
        if (start >= glyphs.Count)
            return string.Empty;
        if (start < 0) start = 0;

        int take = Math.Min(length, glyphs.Count - start);
        var sb = new StringBuilder();
        bool hasAnsi = false;

        // Carry forward any ANSI sequences active before `start`
        for (int i = 0; i < start; i++)
        {
            if (glyphs[i].Prefix.Length > 0)
            {
                sb.Append(glyphs[i].Prefix);
                hasAnsi = true;
            }
        }

        for (int i = start; i < start + take; i++)
        {
            if (glyphs[i].Prefix.Length > 0)
            {
                sb.Append(glyphs[i].Prefix);
                hasAnsi = true;
            }
            sb.Append(glyphs[i].Char);
        }

        if (start + take == glyphs.Count && trailing.Length > 0)
        {
            sb.Append(trailing);
        }
        else if (hasAnsi)
        {
            string built = sb.ToString();
            if (!built.EndsWith("\x1B[0m", StringComparison.Ordinal))
                sb.Append("\x1B[0m");
        }

        return sb.ToString();
    }

    public static string Splice(string? input, int pos, int len, string replacement)
    {
        if (string.IsNullOrEmpty(input))
            return pos == 0 ? replacement : string.Empty;

        var glyphs = Parse(input, out string trailing);
        if (pos > glyphs.Count)
            return input;

        int rem = Math.Min(len, glyphs.Count - pos);
        var sb = new StringBuilder();
        bool hasOpenAnsi = false;

        for (int i = 0; i < pos; i++)
        {
            if (glyphs[i].Prefix.Length > 0)
            {
                sb.Append(glyphs[i].Prefix);
                hasOpenAnsi = !glyphs[i].Prefix.EndsWith("\x1B[0m", StringComparison.Ordinal);
            }
            sb.Append(glyphs[i].Char);
        }

        if (hasOpenAnsi)
        {
            sb.Append("\x1B[0m");
            hasOpenAnsi = false;
        }

        sb.Append(replacement);

        for (int i = pos + rem; i < glyphs.Count; i++)
        {
            if (glyphs[i].Prefix.Length > 0)
            {
                string prefix = glyphs[i].Prefix;
                if (hasOpenAnsi || prefix != "\x1B[0m")
                {
                    sb.Append(prefix);
                    hasOpenAnsi = !prefix.EndsWith("\x1B[0m", StringComparison.Ordinal);
                }
            }
            sb.Append(glyphs[i].Char);
        }

        if (hasOpenAnsi && trailing.Length > 0)
            sb.Append(trailing);

        return sb.ToString();
    }

    public static string RepeatFill(string fill, int count)
    {
        if (count <= 0) return string.Empty;
        if (string.IsNullOrEmpty(fill)) fill = " ";
        if (fill.Length == 1) return new string(fill[0], count);

        var glyphs = Parse(fill, out string trailing);
        if (glyphs.Count == 0)
            return new string(' ', count);

        var sb = new StringBuilder(count * 2);
        bool hasAnsi = trailing.Length > 0;
        for (int i = 0; i < count; i++)
        {
            var g = glyphs[i % glyphs.Count];
            if (g.Prefix.Length > 0)
            {
                sb.Append(g.Prefix);
                hasAnsi = true;
            }
            sb.Append(g.Char);
        }

        if (trailing.Length > 0)
        {
            sb.Append(trailing);
        }
        else if (hasAnsi)
        {
            sb.Append("\x1B[0m");
        }
        return sb.ToString();
    }

    public static string Pad(string? input, int width, string fill, int align, bool truncate, string? rightFill = null)
    {
        string text = input ?? string.Empty;
        if (width <= 0) return truncate ? string.Empty : text;
        if (string.IsNullOrEmpty(fill)) fill = " ";

        int visLen = VisibleLength(text);
        if (visLen >= width)
            return truncate ? Substring(text, 0, width) : text;

        int padTotal = width - visLen;
        if (align < 0)
            return text + RepeatFill(fill, padTotal);
        if (align > 0)
            return RepeatFill(fill, padTotal) + text;

        int leftPad = padTotal / 2;
        int rightPad = padTotal - leftPad;
        string rFill = string.IsNullOrEmpty(rightFill) ? ReverseFill(fill) : rightFill;
        return RepeatFill(fill, leftPad) + text + RepeatFill(rFill, rightPad);
    }

    private static string ReverseFill(string fill)
    {
        if (fill.Length <= 1) return fill;
        var glyphs = Parse(fill, out string trailing);
        if (glyphs.Count <= 1) return fill;

        var sb = new StringBuilder(fill.Length);
        for (int i = glyphs.Count - 1; i >= 0; i--)
        {
            sb.Append(glyphs[i].Prefix);
            sb.Append(glyphs[i].Char);
        }
        sb.Append(trailing);
        return sb.ToString();
    }
}
