namespace PennMUSH.Server.Session;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Presentation;
using PennMUSH.Core.Session;
using PennMUSH.Core.Types;

public sealed class ConnectionDirectory : ISessionDirectory
{
    private readonly ConcurrentDictionary<string, MUSHConnection> _connections;
    private readonly Action<MUSHConnection> _drop;
    private readonly HashSet<int> _pending = new();

    public ConnectionDirectory(ConcurrentDictionary<string, MUSHConnection> connections, Action<MUSHConnection> drop)
    {
        _connections = connections;
        _drop = drop;
    }

    public bool IgnoresQuota(Dbref player) =>
        _connections.Values.Any(conn => conn.PlayerId == player && conn.NoQuota);

    public int? SourceDescriptor { get; set; }
    public string Poll { get; set; } = "Doing";

    public IReadOnlyList<SessionInfo> List()
    {
        return _connections.Values
            .Where(conn => !_pending.Contains(conn.Descriptor))
            .OrderBy(conn => conn.Descriptor)
            .Select(conn => new SessionInfo
            {
                Descriptor = conn.Descriptor,
                PlayerId = conn.PlayerId,
                Hidden = conn.Hidden,
                Host = conn.Host,
                ConnectedAt = conn.ConnectedAt,
                LastActivity = conn.LastActivity,
                Commands = conn.Commands,
                BytesSent = conn.BytesSent,
                BytesReceived = conn.BytesReceived,
                TerminalType = conn.Capabilities.TerminalType,
                Width = conn.Capabilities.TerminalWidth,
                Height = conn.Capabilities.TerminalHeight,
                SupportsTelnet = conn.TelnetEnabled,
                SupportsGmcp = conn.Capabilities.SupportsGmcp,
                IsSsl = conn.Capabilities.IsSsl,
                IsWebSocket = conn.IsWebSocket,
                SupportsHtml = conn.Capabilities.SupportsHtml,
                SupportsPueblo = conn.Capabilities.SupportsPueblo,
                StripAccents = conn.StripAccents,
                PromptNewlines = conn.PromptNewlines,
                SupportsXterm256 = conn.Capabilities.SupportsXterm256,
                SupportsRgb24 = conn.Capabilities.SupportsRgb24,
                ColorStyle = conn.ColorStyle
            })
            .ToList();
    }

    public void SetHidden(int descriptor, bool hidden)
    {
        var conn = Find(descriptor);
        if (conn != null)
            conn.Hidden = hidden;
    }

    public void Disconnect(int descriptor)
    {
        if (Find(descriptor) != null)
            _pending.Add(descriptor);
    }

    public void FlushDisconnects()
    {
        foreach (int descriptor in _pending.ToArray())
        {
            _pending.Remove(descriptor);
            var conn = Find(descriptor);
            if (conn != null)
                _drop(conn);
        }
    }

    private MUSHConnection? Find(int descriptor)
    {
        return _connections.Values.FirstOrDefault(conn => conn.Descriptor == descriptor);
    }

    public string Sockset(Dbref executor, Dbref subject, int? descriptor, bool own, bool setting, string values, bool wizard, bool seeAll, Func<Dbref, bool> isWizard)
    {
        var conn = descriptor.HasValue
            ? FindVisible(executor, descriptor.Value, seeAll)
            : LeastIdle(subject, seeAll || own);
        if (conn == null)
            return own ? "You are not connected?" : "Invalid descriptor.";
        if (!setting)
        {
            if (conn.PlayerId != executor && !seeAll)
                return "Permission denied.";
            return Show(conn);
        }
        if (conn.PlayerId != executor && !wizard)
            return "Permission denied.";

        string[] tokens = values.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
            return "Set what option?";

        var results = new List<string>();
        for (int i = 0; i < tokens.Length; i += 2)
        {
            string option = tokens[i];
            string optionValue = i + 1 < tokens.Length ? tokens[i + 1] : "";
            results.Add(Set(conn, option, optionValue, isWizard(conn.PlayerId)));
        }
        return string.Join("\n", results);
    }

    private MUSHConnection? FindVisible(Dbref executor, int descriptor, bool seeAll)
    {
        var conn = Find(descriptor);
        if (conn != null && (seeAll || conn.PlayerId == executor))
            return conn;
        return null;
    }

    private MUSHConnection? LeastIdle(Dbref player, bool includeHidden)
    {
        return _connections.Values
            .Where(conn => conn.PlayerId == player && (includeHidden || !conn.Hidden))
            .OrderByDescending(conn => conn.LastActivity)
            .FirstOrDefault();
    }

    private static string Show(MUSHConnection conn)
    {
        var lines = new List<string> { "" };
        if (!string.IsNullOrEmpty(conn.OutputPrefix))
            lines.Add(Line("OUTPUTPREFIX", conn.OutputPrefix));
        if (!string.IsNullOrEmpty(conn.OutputSuffix))
            lines.Add(Line("OUTPUTSUFFIX", conn.OutputSuffix));
        lines.Add(Line("Pueblo", conn.Capabilities.SupportsPueblo ? "Yes" : "No"));
        lines.Add(Line("Telnet", conn.TelnetEnabled ? "Yes" : "No"));
        lines.Add(Line("Width", conn.Capabilities.TerminalWidth.ToString()));
        lines.Add(Line("Height", conn.Capabilities.TerminalHeight.ToString()));
        lines.Add(Line("Terminal Type", string.IsNullOrEmpty(conn.Capabilities.TerminalType) ? "UNKNOWN" : conn.Capabilities.TerminalType));
        lines.Add(Line("Stripaccents", conn.StripAccents ? "Yes" : "No"));
        string detected = DetectedColor(conn);
        lines.Add(conn.ColorStyle == null
            ? Line("Color Style", $"auto ({detected})")
            : Line("Color Style", conn.ColorStyle));
        lines.Add(Line("Prompt Newlines", conn.PromptNewlines ? "Yes" : "No"));
        return string.Join("\n", lines);
    }

    private static string Set(MUSHConnection conn, string name, string value, bool subjectIsWizard)
    {
        if (name.Equals("OUTPUTPREFIX", StringComparison.OrdinalIgnoreCase))
        {
            conn.OutputPrefix = value;
            return value.Length == 0 ? "OUTPUTPREFIX cleared." : "OUTPUTPREFIX set.";
        }
        if (name.Equals("OUTPUTSUFFIX", StringComparison.OrdinalIgnoreCase))
        {
            conn.OutputSuffix = value;
            return value.Length == 0 ? "OUTPUTSUFFIX cleared." : "OUTPUTSUFFIX set.";
        }
        if (name.Equals("PUEBLO", StringComparison.OrdinalIgnoreCase))
        {
            bool enabled = value.Length > 0;
            conn.Capabilities.SupportsPueblo = enabled;
            conn.Capabilities.SupportsHtml = enabled;
            return enabled ? "Pueblo flag set." : "Pueblo flag cleared.";
        }
        if (name.Equals("TELNET", StringComparison.OrdinalIgnoreCase))
        {
            conn.TelnetEnabled = Yes(value);
            return conn.TelnetEnabled ? "Telnet flag set." : "Telnet flag cleared.";
        }
        if (name.Equals("WIDTH", StringComparison.OrdinalIgnoreCase))
        {
            if (!Positive(value, out int width))
                return "Width expects a positive integer.";
            conn.Capabilities.TerminalWidth = width;
            return "Width set.";
        }
        if (name.Equals("HEIGHT", StringComparison.OrdinalIgnoreCase))
        {
            if (!Positive(value, out int height))
                return "Height expects a positive integer.";
            conn.Capabilities.TerminalHeight = height;
            return "Height set.";
        }
        if (name.Equals("TERMINALTYPE", StringComparison.OrdinalIgnoreCase))
        {
            conn.Capabilities.TerminalType = value;
            return "Terminal Type set.";
        }
        if (name.Equals("COLORSTYLE", StringComparison.OrdinalIgnoreCase) || name.Equals("COLOURSTYLE", StringComparison.OrdinalIgnoreCase))
            return Color(conn, value);
        if (name.Equals("PROMPT_NEWLINES", StringComparison.OrdinalIgnoreCase))
        {
            conn.PromptNewlines = Yes(value);
            return conn.PromptNewlines
                ? "A newline will be sent after a prompt."
                : "No newline will be sent after a prompt.";
        }
        if (name.Equals("STRIPACCENTS", StringComparison.OrdinalIgnoreCase) || name.Equals("NOACCENTS", StringComparison.OrdinalIgnoreCase))
        {
            conn.StripAccents = Yes(value);
            return conn.StripAccents ? "Accents will be stripped." : "Accents will not be stripped.";
        }
        if (name.Equals("NOQUOTA", StringComparison.OrdinalIgnoreCase))
        {
            if (!subjectIsWizard)
                return "Only Wizards can set this option.";
            conn.NoQuota = Yes(value);
            return conn.NoQuota
                ? "NOQUOTA turned on. Command quota is now ignored."
                : "NOQUOTA turned off. Command quota will be respected.";
        }
        return $"@sockset option '{name}' is not a valid option.";
    }

    private static string Color(MUSHConnection conn, string value)
    {
        if (value.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            conn.ColorStyle = null;
            return "Colorstyle set to 'auto'";
        }
        string? style = value.ToLowerInvariant() switch
        {
            "plain" or "none" => "plain",
            "hilite" or "highlight" => "hilite",
            "16color" => "16color",
            "xterm256" or "256" => "xterm256",
            "rgb24" or "truecolor" => "rgb24",
            _ => null
        };
        if (style == null)
            return "Unknown color style. Valid color styles: 'auto', 'plain', 'hilite', '16color', 'xterm256', 'rgb24'.";
        conn.ColorStyle = style;
        conn.Capabilities.ColorMode = style switch
        {
            "plain" => ColorDepth.None,
            "xterm256" => ColorDepth.Color256,
            "rgb24" => ColorDepth.TrueColor,
            _ => ColorDepth.Standard16
        };
        return $"Colorstyle set to '{style}'";
    }

    private static string DetectedColor(MUSHConnection conn) => conn.Capabilities.ColorMode switch
    {
        ColorDepth.None => "plain",
        ColorDepth.TrueColor => "rgb24",
        ColorDepth.Color256 => "xterm256",
        _ => "16color"
    };

    private static string Line(string name, string value) => $"{name,-15}:  {value}";

    private static bool Yes(string value)
    {
        return value.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || value.Equals("y", StringComparison.OrdinalIgnoreCase)
            || value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("on", StringComparison.OrdinalIgnoreCase);
    }

    private static bool Positive(string value, out int number)
    {
        number = 0;
        if (value.Length == 0 || !value.All(char.IsAsciiDigit))
            return false;
        return int.TryParse(value, out number) && number >= 1;
    }
}
