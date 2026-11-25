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

    internal bool MatchesChannel(PennMUSH.Core.Entities.MUSHObject executor, string chName, ObjectDatabase db)
    {
        var ch = _channels.MatchJoinedChannel(executor.Id, chName, out bool ambiguous);
        return ambiguous || (ch != null && ch.Visible(executor, db));
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
            context.Notify(Feedback.UsageChatChannelMessage);
            return false;
        }

        var ch = _channels.MatchJoinedChannel(executor.Id, chName, out bool ambiguous);
        if (ambiguous)
        {
            context.Notify(Feedback.IDontKnowWhichChannelYouMean);
            return false;
        }
        if (ch == null || !ch.Visible(executor, db))
        {
            context.Notify(Feedback.ChatNoSuchChannel);
            return false;
        }

        if (!ch.Passes("speak", executor.Id, db))
        {
            context.Notify(Feedback.SorryYoureNotAllowedToSpeakOnChannel, ("channel", $"{ch.Name}"));
            return false;
        }

        if (!ch.HasMember(executor.Id) && !executor.IsWizard)
        {
            context.Notify(Feedback.YouAreNotOnChannel, ("channel", $"{ch.Name}"));
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
