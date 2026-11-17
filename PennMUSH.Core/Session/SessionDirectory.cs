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
