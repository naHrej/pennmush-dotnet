namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;

public class ChatCommandHandler : ICommandHandler
{
    private readonly ChannelRegistry _channels;
    private readonly ExpressionEvaluator? _evaluator;

    public string Name => "@chat";

    public ChatCommandHandler(ChannelRegistry channels, ExpressionEvaluator? evaluator = null)
    {
        _channels = channels;
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var executor = db.GetObject(context.Executor);
        if (executor == null) return false;

        string chName = context.LeftArg.Trim();
        string message = context.RightArg;

        // If no '=' was provided, try splitting by first space: "+channel message"
        if (string.IsNullOrEmpty(context.RightArg) && !string.IsNullOrEmpty(context.Arguments))
        {
            int space = context.Arguments.IndexOf(' ');
            if (space > 0)
            {
                chName = context.Arguments.Substring(0, space).TrimStart('+');
                message = context.Arguments.Substring(space + 1).TrimStart();
            }
        }

        if (string.IsNullOrEmpty(chName) || string.IsNullOrEmpty(message))
        {
            context.Notify("Usage: @chat <channel>=<message>");
            return false;
        }

        var ch = _channels.GetChannel(chName);
        if (ch == null || !ch.Visible(executor, db))
        {
            context.Notify("CHAT: No such channel.");
            return false;
        }

        if (!ch.Passes("speak", executor.Id, db))
        {
            context.Notify($"Sorry, you're not allowed to speak on channel <{ch.Name}>.");
            return false;
        }

        if (!ch.HasMember(executor.Id) && !executor.IsWizard)
        {
            context.Notify($"You are not on channel <{ch.Name}>.");
            return false;
        }

        string token = "\"";
        bool isPose = false;
        if (message.StartsWith(':'))
        {
            token = ":";
            isPose = true;
            message = message.Substring(1);
        }
        else if (message.StartsWith(';'))
        {
            token = ";";
            isPose = true;
            message = message.Substring(1);
        }

        var result = _channels.Broadcast(ch.Name, executor.Id, executor.Name, message, isPose: isPose);
        if (result != null)
        {
            string title = ch.GetMember(executor.Id)?.Title ?? "";
            FormatAttributes.DeliverChannel(context, db, _evaluator, result, executor.Id, ch.Name, message, executor.Name, title, token, "noisy", false);
            return true;
        }

        return false;
    }
}
