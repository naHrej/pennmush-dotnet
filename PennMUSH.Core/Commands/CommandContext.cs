namespace PennMUSH.Core.Commands;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Types;

public class CommandContext
{
    public Dbref Executor { get; init; }
    public Dbref Enactor { get; init; }
    public string RawCommand { get; init; } = string.Empty;
    public string CommandName { get; init; } = string.Empty;
    public HashSet<string> Switches { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string Arguments { get; init; } = string.Empty;
    public string LeftArg { get; init; } = string.Empty;
    public string RightArg { get; init; } = string.Empty;

    public List<Notification> TargetedNotifications { get; } = new();
    public List<string> Notifications { get; } = new();

    public void Notify(string message)
    {
        TargetedNotifications.Add(new Notification(NotificationScope.Self, message, Executor));
        Notifications.Add(message);
    }

    public void NotifyTarget(Dbref target, string message, bool prompt = false)
    {
        TargetedNotifications.Add(new Notification(NotificationScope.Target, message, target, Prompt: prompt));
        if (target == Executor)
        {
            Notifications.Add(message);
        }
    }

    public void NotifyRoom(Dbref room, string message)
    {
        TargetedNotifications.Add(new Notification(NotificationScope.Room, message, room));
        Notifications.Add(message);
    }

    public void NotifyExcept(Dbref room, Dbref except, string message)
    {
        TargetedNotifications.Add(new Notification(NotificationScope.RoomExcept, message, room, except));
        if (except != Executor)
        {
            Notifications.Add(message);
        }
    }

    public void NotifyBroadcast(string message, bool wizardOnly = false)
    {
        TargetedNotifications.Add(new Notification(wizardOnly ? NotificationScope.Wizards : NotificationScope.Broadcast, message));
        Notifications.Add(message);
    }

    public void NotifyProtocol(Dbref target, string fallbackMessage, string protocolType, object protocolPayload)
    {
        TargetedNotifications.Add(new Notification(NotificationScope.Target, fallbackMessage, target, default, protocolType, protocolPayload));
        if (target == Executor)
        {
            Notifications.Add(fallbackMessage);
        }
    }
}
