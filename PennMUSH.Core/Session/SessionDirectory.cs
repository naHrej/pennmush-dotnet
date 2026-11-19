namespace PennMUSH.Core.Session;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Types;

public sealed class SessionInfo
{
    public int Descriptor { get; set; }
    public Dbref PlayerId { get; set; } = Dbref.Nothing;
    public bool Hidden { get; set; }
    public string Host { get; set; } = "";
    public DateTimeOffset ConnectedAt { get; set; }
    public DateTimeOffset LastActivity { get; set; }
    public int Commands { get; set; }
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public int Pending { get; set; }
    public string TerminalType { get; set; } = "UNKNOWN";
    public int Width { get; set; } = 80;
    public int Height { get; set; } = 24;
    public bool SupportsTelnet { get; set; } = true;
    public bool SupportsGmcp { get; set; }
    public bool IsSsl { get; set; }
    public bool IsWebSocket { get; set; }
    public bool SupportsHtml { get; set; }
    public bool SupportsPueblo { get; set; }
    public bool StripAccents { get; set; }
    public bool PromptNewlines { get; set; }
    public bool SupportsXterm256 { get; set; }
    public bool SupportsRgb24 { get; set; }
    public string? ColorStyle { get; set; }

    public bool LoggedIn => PlayerId.IsValid;
}

public interface ISessionDirectory
{
    IReadOnlyList<SessionInfo> List();
    int? SourceDescriptor { get; set; }
    string Poll { get; set; }
    void SetHidden(int descriptor, bool hidden);
    void Disconnect(int descriptor);
    void FlushDisconnects();
    string Sockset(Dbref executor, Dbref subject, int? descriptor, bool own, bool setting, string values, bool wizard, bool seeAll, Func<Dbref, bool> isWizard)
        => "You are not connected?";
}
