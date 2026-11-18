namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Protocol;
using PennMUSH.Core.Types;

public class ClientCommandHandler : ICommandHandler
{
    public string Name => "@client";
    private readonly EditorSessionManager _editorSessions;

    public ClientCommandHandler(EditorSessionManager? editorSessions = null)
    {
        _editorSessions = editorSessions ?? new EditorSessionManager();
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var executor = db.GetObject(context.Executor);
        if (executor == null) return false;

        if (context.Switches.Contains("edit"))
        {
            return HandleEdit(context, db, executor);
        }
        else if (context.Switches.Contains("window"))
        {
            return HandleWindow(context, db, executor);
        }
        else if (context.Switches.Contains("style"))
        {
            return HandleStyle(context, db, executor);
        }
        else if (context.Switches.Contains("audio") || context.Switches.Contains("sound"))
        {
            return HandleAudio(context, db, executor);
        }

        context.Notify(Feedback.UsageClientEditTargetAttrClientWindowPlayerIdTargetActionHtmlUrlClient);
        return true;
    }

    private bool HandleEdit(CommandContext context, ObjectDatabase db, MUSHObject executor)
    {
        string arg = string.IsNullOrEmpty(context.LeftArg) ? context.Arguments : context.LeftArg;
        int slash = arg.IndexOf('/');
        if (slash <= 0)
        {
            context.Notify(Feedback.UsageClientEditTargetAttribute);
            return false;
        }

        string targetStr = arg[..slash].Trim();
        string attrName = arg[(slash + 1)..].Trim();

        Dbref targetId = Dbref.Nothing;
        if (targetStr.Equals("me", StringComparison.OrdinalIgnoreCase) || targetStr.Equals("here", StringComparison.OrdinalIgnoreCase))
        {
            targetId = targetStr.Equals("me", StringComparison.OrdinalIgnoreCase) ? executor.Id : executor.Location;
        }
        else if (!Dbref.TryParse(targetStr, out targetId))
        {
            var match = db.FindObject(targetStr);
            if (match != null) targetId = match.Id;
        }

        var target = db.GetObject(targetId);
        if (target == null)
        {
            context.Notify(Feedback.CouldNotFindTargetObject, ("object", $"{targetStr}"));
            return false;
        }

        var openPayload = _editorSessions.StartAttributeSession(executor.Id, target.Id, attrName, db);
        if (openPayload == null)
        {
            context.Notify(Feedback.PermissionDeniedOrFailedToOpenEditorSession);
            return false;
        }

        context.NotifyProtocol(
            executor.Id,
            Feedback.ClientCodeEditorOpenedForUseClientSupportingMUjsProtocolForVisualEditor.Text(("value", $"{target.Name}"), ("value2", $"{(attrName.ToUpperInvariant())}")),
            "editor.open",
            openPayload
        );
        return true;
    }

    private bool HandleWindow(CommandContext context, ObjectDatabase db, MUSHObject executor)
    {
        if (string.IsNullOrEmpty(context.LeftArg) || string.IsNullOrEmpty(context.RightArg))
        {
            context.Notify(Feedback.UsageClientWindowPlayerWindowIdTargetActionContentCssUrl);
            return false;
        }

        Dbref player = ResolvePlayer(context.LeftArg, db, executor);
        if (!player.IsValid)
        {
            context.Notify(Feedback.CannotFindPlayer, ("player", $"{context.LeftArg}"));
            return false;
        }

        string[] parts = context.RightArg.Split(':', 5);
        if (parts.Length < 4)
        {
            context.Notify(Feedback.UsageClientWindowPlayerWindowIdTargetActionContentCssUrl);
            return false;
        }

        string windowId = parts[0].Trim();
        string target = parts[1].Trim();
        string action = parts[2].Trim();
        string content = parts[3];
        string? cssUrl = parts.Length > 4 ? parts[4].Trim() : null;

        var payload = new WindowUpdatePayload(
            WindowId: windowId,
            Target: target,
            Title: windowId,
            Action: action,
            Html: content,
            CssUrl: cssUrl
        );

        context.NotifyProtocol(
            player,
            Feedback.ClientWindow.Text(("value", $"{windowId}"), ("value2", $"{target}"), ("value3", $"{action}")),
            "window.update",
            payload
        );
        return true;
    }

    private bool HandleStyle(CommandContext context, ObjectDatabase db, MUSHObject executor)
    {
        if (string.IsNullOrEmpty(context.LeftArg) || string.IsNullOrEmpty(context.RightArg))
        {
            context.Notify(Feedback.UsageClientStylePlayerUrl);
            return false;
        }

        Dbref player = ResolvePlayer(context.LeftArg, db, executor);
        if (!player.IsValid)
        {
            context.Notify(Feedback.CannotFindPlayer, ("player", $"{context.LeftArg}"));
            return false;
        }

        var payload = new StyleLoadPayload(
            Id: "style_" + Guid.NewGuid().ToString("N")[..8],
            Url: context.RightArg.Trim(),
            Scope: "global"
        );

        context.NotifyProtocol(
            player,
            Feedback.ClientStyleLoaded.Text(("value", $"{payload.Url}")),
            "style.load",
            payload
        );
        return true;
    }

    private bool HandleAudio(CommandContext context, ObjectDatabase db, MUSHObject executor)
    {
        if (string.IsNullOrEmpty(context.LeftArg) || string.IsNullOrEmpty(context.RightArg))
        {
            context.Notify(Feedback.UsageClientAudioPlayerUrl);
            return false;
        }

        Dbref player = ResolvePlayer(context.LeftArg, db, executor);
        if (!player.IsValid)
        {
            context.Notify(Feedback.CannotFindPlayer, ("player", $"{context.LeftArg}"));
            return false;
        }

        var payload = new AudioPlayPayload(
            Url: context.RightArg.Trim(),
            Volume: 1.0,
            Loop: false
        );

        context.NotifyProtocol(
            player,
            Feedback.ClientAudioCue.Text(("value", $"{payload.Url}")),
            "audio.play",
            payload
        );
        return true;
    }

    private static Dbref ResolvePlayer(string input, ObjectDatabase db, MUSHObject executor)
    {
        if (input.Equals("me", StringComparison.OrdinalIgnoreCase)) return executor.Id;
        if (Dbref.TryParse(input, out var d)) return d;
        var match = db.FindPlayer(input);
        return match?.Id ?? Dbref.Nothing;
    }
}
