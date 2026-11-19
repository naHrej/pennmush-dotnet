namespace PennMUSH.Core.MUSHcode.Functions;

using System;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Protocol;
using PennMUSH.Core.Types;
using PennMUSH.Core.MUSHcode;

public static class ClientFunctions
{
    private static Action<Notification>? _dispatcher;
    private static EditorSessionManager _editorSessions = new();

    public static void Initialize(Action<Notification> dispatcher, EditorSessionManager? editorSessions = null)
    {
        _dispatcher = dispatcher;
        if (editorSessions != null) _editorSessions = editorSessions;
    }

    public static void Register(FunctionRegistry registry)
    {
        registry.Register("CEDIT", (ctx, args) => CEditFunc(registry, ctx, args));
        registry.Register("CWINDOW", (ctx, args) => CWindowFunc(registry, ctx, args));
        registry.Register("CSTYLE", (ctx, args) => CStyleFunc(registry, ctx, args));
        registry.Register("CAUDIO", (ctx, args) => CAudioFunc(registry, ctx, args));
        registry.Register("CLIENT_EMIT", (ctx, args) => CEmitFunc(registry, ctx, args));
    }

    private static string CEditFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3) return MushError.TooFewArguments.Text();

        if (!Dbref.TryParse(args[0], out var player)) return MushError.InvalidPlayer.Text();
        if (!Dbref.TryParse(args[1], out var target)) return MushError.InvalidTarget.Text();
        string attribute = args[2].Trim();

        var editorSessions = reg.EditorSessions ?? _editorSessions;
        var openPayload = editorSessions.StartAttributeSession(player, target, attribute, reg.Database ?? new ObjectDatabase());
        if (openPayload == null) return MushError.PermissionDenied.Text();

        var dispatcher = reg.NotificationDispatcher ?? _dispatcher;
        dispatcher?.Invoke(new Notification(
            NotificationScope.Target,
            Feedback.ClientCodeEditorOpened.Text(("value", openPayload.Title)),
            Target: player,
            ProtocolType: "editor.open",
            ProtocolPayload: openPayload
        ));

        return "1";
    }

    private static string CWindowFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 5) return MushError.TooFewArguments.Text();

        if (!Dbref.TryParse(args[0], out var player)) return MushError.InvalidPlayer.Text();
        string windowId = args[1].Trim();
        string target = args[2].Trim();
        string action = args[3].Trim();
        string html = args[4];
        string? cssUrl = args.Length > 5 ? args[5].Trim() : null;

        var payload = new WindowUpdatePayload(windowId, target, windowId, action, html, cssUrl);

        var dispatcher = reg.NotificationDispatcher ?? _dispatcher;
        dispatcher?.Invoke(new Notification(
            NotificationScope.Target,
            Feedback.ClientWindow.Text(("value", windowId), ("value2", target), ("value3", action)),
            Target: player,
            ProtocolType: "window.update",
            ProtocolPayload: payload
        ));

        return "1";
    }

    private static string CStyleFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();

        if (!Dbref.TryParse(args[0], out var player)) return MushError.InvalidPlayer.Text();
        string url = args[1].Trim();

        var payload = new StyleLoadPayload("style_" + Guid.NewGuid().ToString("N")[..8], url, "global");

        var dispatcher = reg.NotificationDispatcher ?? _dispatcher;
        dispatcher?.Invoke(new Notification(
            NotificationScope.Target,
            Feedback.ClientStyle.Text(("value", url)),
            Target: player,
            ProtocolType: "style.load",
            ProtocolPayload: payload
        ));

        return "1";
    }

    private static string CAudioFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 2) return MushError.TooFewArguments.Text();

        if (!Dbref.TryParse(args[0], out var player)) return MushError.InvalidPlayer.Text();
        string url = args[1].Trim();
        double vol = args.Length > 2 && double.TryParse(args[2], out var v) ? v : 1.0;
        bool loop = args.Length > 3 && (args[3] == "1" || args[3].Equals("true", StringComparison.OrdinalIgnoreCase));

        var payload = new AudioPlayPayload(url, vol, loop);

        var dispatcher = reg.NotificationDispatcher ?? _dispatcher;
        dispatcher?.Invoke(new Notification(
            NotificationScope.Target,
            Feedback.ClientAudio.Text(("value", url)),
            Target: player,
            ProtocolType: "audio.play",
            ProtocolPayload: payload
        ));

        return "1";
    }

    private static string CEmitFunc(FunctionRegistry reg, EvaluationContext ctx, string[] args)
    {
        if (args.Length < 3) return MushError.TooFewArguments.Text();

        if (!Dbref.TryParse(args[0], out var player)) return MushError.InvalidPlayer.Text();
        string type = args[1].Trim();
        string json = args[2].Trim();

        var dispatcher = reg.NotificationDispatcher ?? _dispatcher;
        dispatcher?.Invoke(new Notification(
            NotificationScope.Target,
            Feedback.ClientOob.Text(("value", type)),
            Target: player,
            ProtocolType: type,
            ProtocolPayload: json
        ));

        return "1";
    }
}
