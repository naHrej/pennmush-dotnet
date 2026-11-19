namespace PennMUSH.Core.Presentation;

using System;
using System.Globalization;
using System.Text;

public static class AccentStripper
{
    public static string Strip(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        string s = text
            .Replace("ß", "ss", StringComparison.Ordinal)
            .Replace("æ", "ae", StringComparison.Ordinal)
            .Replace("Æ", "AE", StringComparison.Ordinal)
            .Replace("ø", "o", StringComparison.Ordinal)
            .Replace("Ø", "O", StringComparison.Ordinal)
            .Replace("ð", "d", StringComparison.Ordinal)
            .Replace("Ð", "D", StringComparison.Ordinal)
            .Replace("þ", "th", StringComparison.Ordinal)
            .Replace("Þ", "TH", StringComparison.Ordinal)
            .Replace("ł", "l", StringComparison.Ordinal)
            .Replace("Ł", "L", StringComparison.Ordinal);

        string normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
