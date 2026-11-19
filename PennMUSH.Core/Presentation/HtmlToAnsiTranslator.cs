namespace PennMUSH.Core.Presentation;

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static class HtmlToAnsiTranslator
{
    private const string AnsiReset = "\x1B[0m";
    private const string AnsiBold = "\x1B[1m";
    private const string AnsiBoldReset = "\x1B[22m";
    private const string AnsiItalic = "\x1B[3m";
    private const string AnsiItalicReset = "\x1B[23m";
    private const string AnsiUnderline = "\x1B[4m";
    private const string AnsiUnderlineReset = "\x1B[24m";
    private const string AnsiColorReset = "\x1B[39m";

    public static string Translate(string html, ColorDepth colorMode = ColorDepth.Standard16)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        // 1. Remove non-renderable block tags completely: <style>...</style>, <script>...</script>, <head>...</head>
        string processed = Regex.Replace(html, @"<head\b[^>]*>.*?</head>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        processed = Regex.Replace(processed, @"<style\b[^>]*>.*?</style>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        processed = Regex.Replace(processed, @"<script\b[^>]*>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // 2. Remove standalone non-renderable tags like <link ...>, <meta ...>
        processed = Regex.Replace(processed, @"<(link|meta)\b[^>]*>", "", RegexOptions.IgnoreCase);

        // 3. Process structural tags
        processed = Regex.Replace(processed, @"<br\s*/?>", Environment.NewLine, RegexOptions.IgnoreCase);
        processed = Regex.Replace(processed, @"<hr\s*/?>", "----------------------------------------------------------------", RegexOptions.IgnoreCase);
        processed = Regex.Replace(processed, @"</(p|div|tr|h1|h2|h3|h4|h5|h6)>", Environment.NewLine, RegexOptions.IgnoreCase);

        // 4. Process formatting tags into ANSI or Plaintext
        if (colorMode == ColorDepth.None)
        {
            // Dumb terminal: strip all tags
            processed = Regex.Replace(processed, @"<[^>]+>", "");
        }
        else
        {
            processed = ProcessFormattingTags(processed, colorMode);
        }

        // 5. Decode common HTML entities
        processed = processed
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Replace("&apos;", "'")
            .Replace("&nbsp;", " ");

        return processed;
    }

    private static string ProcessFormattingTags(string text, ColorDepth colorMode)
    {
        var sb = new StringBuilder();
        int i = 0;

        while (i < text.Length)
        {
            if (text[i] == '<')
            {
                int closeTag = text.IndexOf('>', i);
                if (closeTag != -1)
                {
                    string tagContent = text.Substring(i + 1, closeTag - i - 1).Trim();
                    string ansiCode = ConvertTagToAnsi(tagContent, colorMode);
                    sb.Append(ansiCode);
                    i = closeTag + 1;
                    continue;
                }
            }

            sb.Append(text[i]);
            i++;
        }

        return sb.ToString();
    }

    private static string ConvertTagToAnsi(string tag, ColorDepth colorMode)
    {
        string lower = tag.ToLowerInvariant();

        // Bold
        if (lower is "b" or "strong") return AnsiBold;
        if (lower is "/b" or "/strong") return AnsiBoldReset;

        // Italic
        if (lower is "i" or "em") return AnsiItalic;
        if (lower is "/i" or "/em") return AnsiItalicReset;

        // Underline
        if (lower == "u") return AnsiUnderline;
        if (lower == "/u") return AnsiUnderlineReset;

        // Color tags
        if (lower is "/font" or "/color" or "/span") return AnsiColorReset;

        if (lower.StartsWith("font") || lower.StartsWith("color") || lower.StartsWith("span"))
        {
            // Check for color="#RRGGBB" or color="name" or fg="#RRGGBB"
            var matchHex = Regex.Match(tag, @"(color|fg)\s*=\s*""#([0-9a-fA-F]{6})""", RegexOptions.IgnoreCase);
            if (matchHex.Success)
            {
                string hex = matchHex.Groups[2].Value;
                int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);

                return FormatRgb(r, g, b, colorMode);
            }

            var matchName = Regex.Match(tag, @"(color|fg)\s*=\s*""([a-zA-Z0-9\s]+)""", RegexOptions.IgnoreCase);
            if (matchName.Success)
            {
                string name = matchName.Groups[2].Value.Trim().ToLowerInvariant();
                return GetNamedColorAnsi(name, colorMode);
            }
        }

        // Unknown or stripped tag
        return string.Empty;
    }

    private static string FormatRgb(int r, int g, int b, ColorDepth colorMode)
    {
        if (colorMode == ColorDepth.TrueColor)
        {
            return $"\x1B[38;2;{r};{g};{b}m";
        }
        if (colorMode == ColorDepth.Color256)
        {
            int code = 16 + (36 * (r / 51)) + (6 * (g / 51)) + (b / 51);
            return $"\x1B[38;5;{code}m";
        }

        // Standard 16-color downsampling
        return DownsampleTo16Color(r, g, b);
    }

    private static string DownsampleTo16Color(int r, int g, int b)
    {
        bool bright = (r > 128 || g > 128 || b > 128);
        if (r > g && r > b) return bright ? "\x1B[91m" : "\x1B[31m"; // Red
        if (g > r && g > b) return bright ? "\x1B[92m" : "\x1B[32m"; // Green
        if (b > r && b > g) return bright ? "\x1B[94m" : "\x1B[34m"; // Blue
        if (r > 100 && g > 100 && b < 80) return bright ? "\x1B[93m" : "\x1B[33m"; // Yellow
        if (r > 100 && b > 100 && g < 80) return bright ? "\x1B[95m" : "\x1B[35m"; // Magenta
        if (g > 100 && b > 100 && r < 80) return bright ? "\x1B[96m" : "\x1B[36m"; // Cyan
        if (r > 200 && g > 200 && b > 200) return "\x1B[97m"; // Bright white
        if (r < 50 && g < 50 && b < 50) return "\x1B[30m"; // Black
        return "\x1B[37m"; // White
    }

    private static string GetNamedColorAnsi(string name, ColorDepth colorMode)
    {
        return name switch
        {
            "red" => "\x1B[31m",
            "green" => "\x1B[32m",
            "yellow" => "\x1B[33m",
            "blue" => "\x1B[34m",
            "magenta" => "\x1B[35m",
            "cyan" => "\x1B[36m",
            "white" => "\x1B[37m",
            "black" => "\x1B[30m",
            _ => AnsiColorTable.TryGetNamedColor(name, out var entry)
                ? FormatRgb(entry.R, entry.G, entry.B, colorMode)
                : string.Empty
        };
    }
}
