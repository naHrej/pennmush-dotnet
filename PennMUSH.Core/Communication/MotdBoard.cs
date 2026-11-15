namespace PennMUSH.Core.Communication;

using System.Collections.Generic;

public enum MotdKind
{
    Connect,
    Wizard,
    Down,
    Full
}

/// <summary>
/// The four announcements set by @motd. They are shown in addition to the
/// connect files and are cleared when the process restarts.
/// </summary>
public sealed class MotdBoard
{
    public const int MaxLength = 8191;

    public const string AsteriskLine =
        "**********************************************************************";

    public string Connect { get; private set; } = "";
    public string Wizard { get; private set; } = "";
    public string Down { get; private set; } = "";
    public string Full { get; private set; } = "";

    public void Set(MotdKind kind, string message)
    {
        if (message.Length > MaxLength)
            message = message[..MaxLength];

        switch (kind)
        {
            case MotdKind.Wizard:
                Wizard = message;
                break;
            case MotdKind.Down:
                Down = message;
                break;
            case MotdKind.Full:
                Full = message;
                break;
            default:
                Connect = message;
                break;
        }
    }

    public IReadOnlyList<string> ConnectLines(bool privileged)
    {
        var lines = new List<string>();
        bool connect = Connect.Length > 0;
        if (connect)
            lines.Add(Connect);
        if (privileged && Wizard.Length > 0)
        {
            if (connect)
                lines.Add(AsteriskLine);
            lines.Add(Wizard);
        }
        return lines;
    }
}
