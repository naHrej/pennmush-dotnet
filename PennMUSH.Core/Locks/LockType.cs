namespace PennMUSH.Core.Locks;

public static class LockType
{
    public const string Basic = "Basic";
    public const string Enter = "Enter";
    public const string Use = "Use";
    public const string Page = "Page";
    public const string Leave = "Leave";
    public const string Drop = "Drop";
    public const string Give = "Give";
    public const string Teleport = "Teleport";
    public const string Speech = "Speech";
    public const string Chzone = "Chzone";
    public const string Zone = "Zone";
    public const string Follow = "Follow";
    public const string Pay = "Pay";

    public static readonly string[] Names =
    [
        "Basic", "Enter", "Use", "Zone", "Page", "Tport", "Teleport", "Speech",
        "Listen", "Command", "Parent", "Link", "Leave", "Drop", "Give", "From",
        "Receive", "Pay", "Chown", "Chzone", "Forward", "Control", "Dropto",
        "Destroy", "Interact", "Mail", "Follow", "Examine"
    ];
}
