namespace PennMUSH.Core.Presentation;

public enum ColorDepth
{
    None = 0,
    Standard16 = 1,
    Color256 = 2,
    TrueColor = 3
}

public class ClientCapabilities
{
    public bool SupportsHtml { get; set; } = false;
    public bool SupportsPueblo { get; set; } = false;
    public bool SupportsGmcp { get; set; } = false;
    public bool IsSsl { get; set; } = false;
    public bool IsWebSocket { get; set; } = false;
    public bool StripAccents { get; set; } = false;
    public bool PromptNewlines { get; set; } = false;
    public ColorDepth ColorMode { get; set; } = ColorDepth.Standard16;
    public bool SupportsXterm256 => ColorMode >= ColorDepth.Color256;
    public bool SupportsRgb24 => ColorMode >= ColorDepth.TrueColor;
    public int TerminalWidth { get; set; } = 80;
    public int TerminalHeight { get; set; } = 24;
    public string TerminalType { get; set; } = "UNKNOWN";
    public string StylesheetUrl { get; set; } = string.Empty;
}
