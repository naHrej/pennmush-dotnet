namespace PennMUSH.Core.Commands;

using PennMUSH.Core.Types;

public enum NotificationScope
{
    Self,
    Target,
    Room,
    RoomExcept,
    Broadcast,
    Wizards
}

public record Notification(
    NotificationScope Scope,
    string Message,
    Dbref Target = default,
    Dbref Except = default,
    string? ProtocolType = null,
    object? ProtocolPayload = null,
    bool Prompt = false);
