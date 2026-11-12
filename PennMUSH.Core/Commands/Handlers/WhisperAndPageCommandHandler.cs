namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class WhisperCommandHandler : ICommandHandler
{
    public string Name => "whisper";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify("Usage: whisper <player>=<message>");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify("You are nowhere.");
            return false;
        }

        string targetName = context.LeftArg.Trim();
        var target = db.FindInContents(execObj.Location, targetName);
        if (target == null || target.Type != ObjectType.Player)
        {
            context.Notify("You cannot whisper to someone who isn't here.");
            return false;
        }

        if (target.Id == context.Executor)
        {
            context.Notify("You whisper to yourself.");
            return true;
        }

        context.Notify($"You whisper, \"{context.RightArg}\" to {target.Name}.");
        context.NotifyTarget(target.Id, $"{execObj.Name} whispers, \"{context.RightArg}\"");
        return true;
    }
}

public class PageCommandHandler : ICommandHandler
{
    public string Name => "page";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        string targetName;
        string message;

        if (!string.IsNullOrWhiteSpace(context.RightArg))
        {
            targetName = context.LeftArg.Trim();
            message = context.RightArg.Trim();
        }
        else
        {
            int firstSpace = context.Arguments.IndexOf(' ');
            if (firstSpace == -1)
            {
                context.Notify("Usage: page <player>=<message>");
                return false;
            }
            targetName = context.Arguments.Substring(0, firstSpace).Trim();
            message = context.Arguments.Substring(firstSpace + 1).Trim();
        }

        var execObj = db.GetObject(context.Executor);
        string senderName = execObj != null ? execObj.Name : context.Executor.ToString();

        var target = db.FindPlayer(targetName);
        if (target == null)
        {
            context.Notify($"I cannot find player '{targetName}'.");
            return false;
        }

        if (!target.EvalLock(LockType.Page, context.Executor, db))
        {
            context.Notify("Permission denied.");
            return false;
        }

        context.Notify($"You page {target.Name} with '{message}'.");
        context.NotifyTarget(target.Id, $"{senderName} pages: {message}");
        return true;
    }
}
