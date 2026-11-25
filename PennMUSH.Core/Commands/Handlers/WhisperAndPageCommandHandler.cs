namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class WhisperCommandHandler : ICommandHandler
{
    public string Name => "whisper";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify(Feedback.UsageWhisperPlayerMessage);
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify(Feedback.YouAreNowhere);
            return false;
        }

        string targetName = context.LeftArg.Trim();
        var target = targetName.Equals("me", StringComparison.OrdinalIgnoreCase)
            ? execObj
            : db.FindInContents(execObj.Location, targetName);
        if (target == null || target.Type != ObjectType.Player)
        {
            context.Notify(Feedback.YouCannotWhisperToSomeoneWhoIsntHere);
            return false;
        }

        context.Notify(Feedback.YouWhisperTo, ("value", $"{context.RightArg}"), ("value2", $"{target.Name}"));
        context.NotifyTarget(target.Id, Feedback.Whispers.Text(("value", $"{execObj.Name}"), ("value2", $"{context.RightArg}")));
        return true;
    }
}

public class PageCommandHandler : ICommandHandler
{
    private readonly ExpressionEvaluator? _evaluator;

    public string Name => "page";

    public PageCommandHandler(ExpressionEvaluator? evaluator = null)
    {
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        string targetName;
        string message;

        if (!string.IsNullOrWhiteSpace(context.RightArg))
        {
            targetName = context.LeftArg.Trim();
            message = context.RightArg.Trim();
        }
        else if (!string.Equals(context.LeftArg, context.Arguments, StringComparison.Ordinal))
        {
            context.Notify("What do you want to page?");
            return false;
        }
        else
        {
            int firstSpace = context.Arguments.IndexOf(' ');
            if (firstSpace == -1)
            {
                context.Notify(Feedback.UsagePagePlayerMessage);
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
            context.Notify(Feedback.ICannotFindPlayer, ("player", $"{targetName}"));
            return false;
        }

        if (!target.EvalLock(LockType.Page, context.Executor, db))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        string outgoing = Feedback.YouPageWith.Text(("target", target.Name), ("message", message));
        string incoming = Feedback.PagesMessage.Text(("sender", senderName), ("message", message));
        string targetRef = target.Id.ToString();
        if (execObj != null)
        {
            string? sent = FormatAttributes.Evaluate(db, _evaluator, execObj, execObj.Id, execObj.Id, "OUTPAGEFORMAT", message, "\"", "", targetRef, outgoing);
            if (!string.IsNullOrEmpty(sent))
                outgoing = sent;
        }

        string? heard = FormatAttributes.Evaluate(db, _evaluator, target, target.Id, context.Executor, "PAGEFORMAT", message, "\"", "", targetRef, incoming);
        if (!string.IsNullOrEmpty(heard))
            incoming = heard;

        context.Notify(outgoing);
        context.NotifyTarget(target.Id, incoming);
        return true;
    }
}
