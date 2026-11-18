namespace PennMUSH.Core.Http;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

/// <summary>
/// One inbound HTTP request. <c>@respond</c> fills the status and headers
/// while the handler attribute runs. Lines the handler would see become the body.
/// </summary>
public sealed class HttpReply
{
    public string StatusLine { get; set; } = "HTTP/1.1 200 OK";
    public string ContentType { get; set; } = "Content-Type: text/plain";
    public List<string> Headers { get; } = new();
    public StringBuilder Body { get; } = new();

    public int StatusCode
    {
        get
        {
            string text = StatusLine.StartsWith("HTTP/1.1 ", StringComparison.Ordinal)
                ? StatusLine["HTTP/1.1 ".Length..]
                : StatusLine;
            return text.Length >= 3 && int.TryParse(text[..3], out int code) ? code : 200;
        }
    }

    public string MediaType
    {
        get
        {
            const string prefix = "Content-Type: ";
            return ContentType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? ContentType[prefix.Length..]
                : ContentType;
        }
    }
}

public sealed class HttpExchange
{
    public bool Handled { get; init; }
    public HttpReply Reply { get; init; } = new();
}

public sealed class HttpChannel
{
    public Dbref Handler { get; set; } = Dbref.Nothing;
    public HttpReply? Active { get; private set; }

    public HttpExchange Handle(
        CommandDispatcher dispatcher,
        ExpressionEvaluator evaluator,
        ObjectDatabase db,
        string method,
        string path,
        string? body,
        IReadOnlyDictionary<string, string>? headers)
    {
        var player = db.GetObject(Handler);
        if (player == null || player.Type != ObjectType.Player)
            return new HttpExchange { Handled = false };

        var reply = new HttpReply();
        Active = reply;
        try
        {
            string attributeName = method.ToUpperInvariant();
            var attribute = player.GetAttribute(attributeName, db);
            if (attribute == null)
            {
                reply.Body.Append("No such attribute.");
                return new HttpExchange { Handled = true, Reply = reply };
            }

            string[] args = { path ?? string.Empty, body ?? string.Empty };
            var frame = new ActionFrame();
            var eval = new EvaluationContext(player.Id, player.Id, location: player.Location, args: args);
            if (headers != null)
            {
                var names = new List<string>();
                foreach (var header in headers)
                {
                    string key = "HDR." + header.Key.ToUpperInvariant().Replace(' ', '_');
                    eval.SetRegister(key, header.Value);
                    names.Add(header.Key);
                }

                eval.SetRegister("HEADERS", string.Join(' ', names));
            }

            foreach (string piece in IncludeCommandHandler.SplitCommands(attribute.Value))
            {
                string command = IncludeCommandHandler.Unwrap(piece);
                if (command.Length == 0)
                    continue;
                string bound = evaluator.Evaluate(command, eval);
                if (string.IsNullOrWhiteSpace(bound))
                    continue;
                var result = dispatcher.Dispatch(player.Id, bound, db, frame, player.Id);
                foreach (string line in result.Notifications)
                {
                    if (reply.Body.Length > 0)
                        reply.Body.Append('\n');
                    reply.Body.Append(line);
                }

                if (frame.Break)
                    break;
            }
        }
        finally
        {
            Active = null;
        }

        return new HttpExchange { Handled = true, Reply = reply };
    }
}
