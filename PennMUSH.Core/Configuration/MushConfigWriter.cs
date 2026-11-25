namespace PennMUSH.Core.Configuration;

using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Updates or appends configuration directives in a <c>mush.cnf</c> file while preserving comments and formatting.
/// </summary>
public static class MushConfigWriter
{
    public static void SaveOption(string filePath, string optionName, string value)
    {
        var lines = new List<string>(File.ReadAllLines(filePath));
        string formattedLine = string.IsNullOrEmpty(value) ? optionName : $"{optionName} {value}";

        int matchIndex = -1;
        for (int i = 0; i < lines.Count; i++)
        {
            string trimmed = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                continue;

            int firstSpace = trimmed.IndexOfAny([' ', '\t']);
            string directive = firstSpace == -1 ? trimmed : trimmed[..firstSpace];
            if (directive.Equals(optionName, StringComparison.OrdinalIgnoreCase))
                matchIndex = i;
        }

        if (matchIndex >= 0)
        {
            lines[matchIndex] = formattedLine;
        }
        else
        {
            lines.Add(formattedLine);
        }

        File.WriteAllLines(filePath, lines);
    }
}
