namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;

public class CemitCommandHandler : ICommandHandler
{
    private readonly ChannelRegistry _channels;
    private readonly ExpressionEvaluator? _evaluator;

    public string Name => "@cemit";

    public CemitCommandHandler(ChannelRegistry channels, ExpressionEvaluator? evaluator = null)
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

        if (string.IsNullOrEmpty(chName) || string.IsNullOrEmpty(message))
        {
            context.Notify("Usage: @cemit <channel>=<message>");
            return false;
        }

        var ch = _channels.GetChannel(chName);
        if (ch == null)
        {
            context.Notify($"Channel <{chName}> not found.");
            return false;
        }

        if (!executor.IsWizard && !executor.IsGod && ch.Owner != executor.Id)
        {
            context.Notify("Permission denied.");
            return false;
        }

        bool noisy = context.Switches.Contains("noisy");
        string body = SpoofPrefix.Apply(executor, context.CommandName, message);
        var result = _channels.Broadcast(ch.Name, executor.Id, executor.Name, body, isEmit: !noisy, isNoisy: noisy);
        if (result != null)
        {
            FormatAttributes.DeliverChannel(
                context,
                db,
                _evaluator,
                result,
                executor.Id,
                ch.Name,
                message,
                executor.Name,
                "",
                "|",
                noisy ? "noisy" : "silent",
                true);
            return true;
        }

        return false;
    }
}
