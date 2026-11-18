namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Http;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class HttpCommandHandler : ICommandHandler
{
    private readonly HttpFetch _fetch;
    private readonly ActionQueue _queue;

    public string Name => "@http";

    public HttpCommandHandler(HttpFetch fetch, ActionQueue queue)
    {
        _fetch = fetch;
        _queue = queue;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !(actor.IsWizard || actor.HasPower("Can_HTTP")))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        if (string.IsNullOrEmpty(context.RightArg))
        {
            context.Notify(Feedback.WhatDoYouWantToQuery);
            return false;
        }

        bool post = context.Switches.Contains("post");
        bool put = context.Switches.Contains("put");
        bool delete = context.Switches.Contains("delete");
        if ((post && put) || (post && delete) || (put && delete))
        {
            context.Notify(Feedback.YouCantMakeMultipleRequestsAtTheSameTime);
            return false;
        }

        if (!CopyAttributeCommandHandler.TrySplitAttr(context.LeftArg, out string objectName, out string attribute))
        {
            context.Notify(Feedback.INeedToKnowWhatAttributeToTrigger);
            return false;
        }

        var thing = db.MatchObject(context.Executor, objectName);
        if (thing == null)
        {
            context.Notify(Feedback.ICantSeeThatHere);
            return false;
        }

        if (!CopyAttributeCommandHandler.Controls(actor, thing))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        int comma = context.RightArg.IndexOf(',');
        string url = comma < 0 ? context.RightArg.Trim() : context.RightArg[..comma].Trim();
        string? payload = comma < 0 ? null : context.RightArg[(comma + 1)..];
        string method = post ? "POST" : put ? "PUT" : delete ? "DELETE" : "GET";
        context.Registers.TryGetValue("content-type", out string? contentType);
        context.Registers.TryGetValue("userpass", out string? userPass);
        HttpFetchResult result = _fetch.Send(url, method, payload, contentType, userPass);
        if (!result.Ok)
        {
            context.NotifyTarget(thing.Id, Feedback.RequestFailed.Text(("value", $"{result.Error}")));
            return false;
        }

        if (result.Truncated)
            context.NotifyTarget(thing.Id, Feedback.TooMuchHttpDataReceivedExcessTruncated.Text());

        var stored = thing.GetAttribute(attribute, db);
        if (stored == null || string.IsNullOrEmpty(stored.Value))
            return true;

        var registers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (result.Status > 0)
            registers["status"] = result.Status.ToString();
        if (!string.IsNullOrEmpty(result.ContentType))
            registers["content-type"] = result.ContentType;
        string[] args = result.Body.Length > 0 ? new[] { result.Body } : Array.Empty<string>();
        _queue.Enqueue(thing.Id, context.Enactor, StripListenPrefix(stored.Value), args: args, registers: registers);
        return true;
    }

    private static string StripListenPrefix(string value)
    {
        if (value.Length == 0 || (value[0] != '$' && value[0] != '^'))
            return value;
        for (int i = 1; i < value.Length; i++)
        {
            if (value[i] == '\\' && i + 1 < value.Length)
            {
                i++;
                continue;
            }

            if (value[i] == ':')
                return value[(i + 1)..];
        }

        return value;
    }
}

public class RespondCommandHandler : ICommandHandler
{
    private readonly HttpChannel _channel;

    public string Name => "@respond";

    public RespondCommandHandler(HttpChannel channel) => _channel = channel;

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var handler = db.GetObject(_channel.Handler);
        if (handler == null || handler.Type != ObjectType.Player)
            context.Notify(Feedback.InvalidHttpHandlerItShouldBeAPlayer);

        if (string.IsNullOrEmpty(context.LeftArg))
        {
            context.Notify(Feedback.InvalidUseOfRespondPleaseCheckHelpRespond);
            return false;
        }

        if (HasNonPrintable(context.LeftArg) || HasNonPrintable(context.RightArg))
        {
            context.Notify(Feedback.NoNonprintableCharactersAllowedInRespond);
            return false;
        }

        bool type = context.Switches.Contains("type");
        bool header = context.Switches.Contains("header");
        if (type && header)
        {
            context.Notify(Feedback.InvalidRespondYouCantUseMoreThanOneSwitch);
            return false;
        }

        HttpReply? reply = _channel.Active;
        if (type)
        {
            if (context.RightArg.Length > 0)
            {
                context.Notify(Feedback.InvalidRespondTypeCannotHaveArgRightUseS);
                return false;
            }

            if (reply != null)
                reply.ContentType = "Content-Type: " + context.LeftArg;
            else
                context.NotifyTarget(context.Enactor, "(HTTP): Content-Type: " + context.LeftArg);
            return true;
        }

        if (header)
        {
            if (context.LeftArg.Length == 0 || context.RightArg.Length == 0)
            {
                context.Notify(Feedback.InvalidFormatUseRespondHeaderHeaderNameValue);
                return false;
            }

            if (context.LeftArg.Equals("content-length", StringComparison.OrdinalIgnoreCase))
            {
                context.Notify(Feedback.YouCannotSetContentLengthHeader);
                return false;
            }

            if (!IsAscii(context.LeftArg))
            {
                context.Notify(Feedback.InvalidHttpHeaderName);
                return false;
            }

            if (reply != null)
                reply.Headers.Add(context.LeftArg + ": " + context.RightArg);
            else
                context.NotifyTarget(context.Enactor, Feedback.HttpValueValue.Text(("value", $"{context.LeftArg}"), ("value2", $"{context.RightArg}")));
            return true;
        }

        string status = context.LeftArg;
        if (status.Length < 5
            || !char.IsDigit(status[0]) || !char.IsDigit(status[1]) || !char.IsDigit(status[2])
            || status[3] != ' '
            || !char.IsLetterOrDigit(status[4]))
        {
            context.Notify(Feedback.RespondMustBe3DigitsSpaceThenText);
            return false;
        }

        if (context.RightArg.Length > 0)
        {
            context.Notify(Feedback.InvalidRespondTypeCannotHaveArgRightUseS);
            return false;
        }

        if (!IsAscii(status))
        {
            context.Notify(Feedback.RespondMustBe3DigitsSpaceThenText);
            return false;
        }

        if (status.Length >= 40)
        {
            context.Notify(Feedback.RespondStatusCodeTooLong);
            return false;
        }

        if (reply != null)
            reply.StatusLine = "HTTP/1.1 " + status;
        else
            context.NotifyTarget(context.Enactor, "(HTTP): HTTP/1.1 " + status);
        return true;
    }

    private static bool HasNonPrintable(string text)
    {
        foreach (char ch in text)
        {
            if (ch < ' ' || ch > '~')
                return true;
        }

        return false;
    }

    private static bool IsAscii(string text)
    {
        foreach (char ch in text)
        {
            if (ch > 127)
                return false;
        }

        return true;
    }
}
