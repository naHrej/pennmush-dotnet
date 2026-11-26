namespace PennMUSH.Core.Commands.Handlers;

using System.Collections.Generic;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public static class FormatAttributes
{
    public static string? Evaluate(
        ObjectDatabase db,
        ExpressionEvaluator? evaluator,
        MUSHObject host,
        Dbref executor,
        Dbref enactor,
        string name,
        params string[] args)
    {
        var attr = host.GetAttribute(name, db);
        if (attr == null)
            return null;

        if (evaluator == null)
        {
            string value = attr.Value ?? string.Empty;
            for (int i = 0; i < args.Length && i < 10; i++)
                value = value.Replace("%" + i, args[i] ?? string.Empty, StringComparison.Ordinal);
            return value;
        }

        // %! is the object the attribute is on. The caller and the enactor
        // are the object that caused this evaluation.
        var ctx = new EvaluationContext(
            host.Id,
            enactor,
            caller: executor,
            location: host.Location,
            args: args);
        return evaluator.Evaluate(attr.Value ?? string.Empty, ctx);
    }

    public static void DeliverChannel(
        CommandContext context,
        ObjectDatabase db,
        ExpressionEvaluator? evaluator,
        ChannelBroadcastResult result,
        Dbref sender,
        string channelName,
        string message,
        string senderName,
        string title,
        string token,
        string noise,
        bool spoofable)
    {
        var targets = new List<Dbref>(result.Recipients);
        if (!targets.Contains(sender))
            targets.Insert(0, sender);

        foreach (var id in targets)
        {
            var listener = db.GetObject(id);
            string line = result.FormattedMessage;
            if (listener != null)
            {
                string? custom = Evaluate(
                    db,
                    evaluator,
                    listener,
                    listener.Id,
                    sender,
                    "CHATFORMAT",
                    token,
                    channelName,
                    message,
                    senderName,
                    title,
                    result.FormattedMessage,
                    "says",
                    noise);
                if (custom != null)
                {
                    if (string.IsNullOrEmpty(custom))
                        continue;
                    line = custom;
                }
            }

            if (id == context.Executor)
                context.Notify(line, sender, spoofable);
            else
                context.NotifyTarget(id, line, speaker: sender, spoofable: spoofable);
        }
    }

    public static string? MogrifySpeech(
        ObjectDatabase db,
        ExpressionEvaluator? evaluator,
        MUSHObject? speaker,
        string token,
        string spokenText,
        string roomDefault,
        string selfDefault)
    {
        if (speaker == null || !db.SpeechMogrifierId.IsValid)
            return null;

        var mogrifier = db.GetObject(db.SpeechMogrifierId);
        if (mogrifier == null)
            return null;

        string[] candidates = token switch
        {
            "\"" => ["MOGRIFYSAY", "MOGRIFY"],
            ";" => ["MOGRIFYSEMIPOSE", "MOGRIFYPOSE", "MOGRIFY"],
            _ => ["MOGRIFYPOSE", "MOGRIFY"]
        };

        string? attrName = null;
        foreach (string name in candidates)
        {
            if (mogrifier.GetAttribute(name, db) != null)
            {
                attrName = name;
                break;
            }
        }
        if (attrName == null)
            return null;

        return Evaluate(
            db,
            evaluator,
            mogrifier,
            speaker.Id,
            speaker.Id,
            attrName,
            token,
            speaker.Name,
            spokenText,
            roomDefault,
            selfDefault,
            speaker.Id.ToString(),
            speaker.Location.ToString());
    }
}
