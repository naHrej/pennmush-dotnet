namespace PennMUSH.Core.Presentation;

using System;

public static class OutputFormatter
{
    public static string FormatOutput(string message, ClientCapabilities capabilities)
        => FormatOutput(message, capabilities, escapeHtml: false);

    public static string FormatOutput(string message, ClientCapabilities capabilities, bool escapeHtml)
    {
        if (string.IsNullOrEmpty(message)) return string.Empty;

        if (capabilities.SupportsHtml)
        {
            return AnsiToHtmlTranslator.Translate(message, escapeHtml, capabilities.ColorMode);
        }

        // Fallback: Translate HTML to ANSI / plaintext
        string translated = HtmlToAnsiTranslator.Translate(message, capabilities.ColorMode);
        return capabilities.ColorMode == ColorDepth.None
            ? AnsiToHtmlTranslator.StripAnsi(translated)
            : translated;
    }

    public static string BuildPuebloInit(string stylesheetUrl)
    {
        if (string.IsNullOrEmpty(stylesheetUrl))
        {
            return "</xch_muc_open><xch_page>\r\n";
        }
        return $"</xch_muc_open><xch_page><link rel=\"stylesheet\" href=\"{stylesheetUrl}\">\r\n";
    }
}
