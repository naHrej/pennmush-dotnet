namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using System.Text;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class ActionListCommandHandler : ICommandHandler
{
    private readonly CommandDispatcher _dispatcher;
    private readonly ExpressionEvaluator? _evaluator;
    private readonly ActionQueue? _queue;

    public string Name { get; }

    public ActionListCommandHandler(CommandDispatcher dispatcher, string name, ExpressionEvaluator? evaluator = null, ActionQueue? queue = null)
    {
        Name = name;
        _dispatcher = dispatcher;
        _evaluator = evaluator;
        _queue = queue;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        string name = context.CommandName;
        if (name.Equals("@retry", StringComparison.OrdinalIgnoreCase))
            return Retry(context, db);
        if (name.Equals("@break", StringComparison.OrdinalIgnoreCase))
            return Stop(context, db, MushBoolean.IsTrue(Evaluate(context, context.LeftArg)));
        return Stop(context, db, !MushBoolean.IsTrue(Evaluate(context, context.LeftArg)));
    }

    private bool Retry(CommandContext context, ObjectDatabase db)
    {
        if (!MushBoolean.IsTrue(Evaluate(context, context.LeftArg)))
            return true;

        if (context.Arguments.Contains('=') && context.Frame != null)
        {
            var parts = SplitArgs(context.RightArg);
            for (int i = 0; i < parts.Count && i < ActionFrame.ArgumentCount; i++)
                context.Frame.Args[i] = Evaluate(context, parts[i]);
        }

        if (context.Frame != null)
            context.Frame.Retry = true;
        return true;
    }

    private bool Stop(CommandContext context, ObjectDatabase db, bool stop)
    {
        if (!stop)
            return true;

        if (context.Frame != null)
            context.Frame.Break = true;
        if (context.Arguments.Contains('='))
            RunList(context, db, context.RightArg);
        return true;
    }

    private void RunList(CommandContext context, ObjectDatabase db, string source)
    {
        bool queued = context.Switches.Contains("queued") && _queue != null;
        foreach (string piece in IncludeCommandHandler.SplitCommands(source))
        {
            string command = IncludeCommandHandler.Unwrap(piece);
            if (command.Length == 0)
                continue;
            if (queued)
            {
                _queue!.Enqueue(context.Executor, context.Enactor, Substitute(command, context.Frame));
                continue;
            }

            var result = _dispatcher.Dispatch(context.Executor, command, db, context.Frame, context.Enactor);
            context.TargetedNotifications.AddRange(result.TargetedNotifications);
            context.Notifications.AddRange(result.Notifications);
        }
    }

    private string Evaluate(CommandContext context, string text)
    {
        if (_evaluator == null || text.Length == 0)
            return text;
        return _evaluator.Evaluate(text, new EvaluationContext(
            context.Executor,
            context.Enactor,
            caller: context.Enactor,
            args: context.Frame?.Args));
    }

    internal static string Substitute(string command, ActionFrame? frame)
    {
        if (frame == null || command.IndexOf('%') < 0)
            return command;
        var built = new StringBuilder();
        for (int i = 0; i < command.Length; i++)
        {
            if (command[i] == '%' && i + 1 < command.Length && char.IsDigit(command[i + 1]))
            {
                int index = command[i + 1] - '0';
                if (index < frame.Args.Length)
                    built.Append(frame.Args[index]);
                i++;
                continue;
            }

            built.Append(command[i]);
        }

        return built.ToString();
    }

    internal static List<string> SplitArgs(string right)
    {
        var args = new List<string>();
        var current = new StringBuilder();
        int braces = 0;
        for (int i = 0; i < right.Length; i++)
        {
            char c = right[i];
            if (c == '\\' && i + 1 < right.Length)
            {
                current.Append(c);
                current.Append(right[++i]);
                continue;
            }

            if (c == '{')
                braces++;
            else if (c == '}' && braces > 0)
                braces--;

            if (braces == 0 && c == ',')
            {
                args.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        args.Add(current.ToString().Trim());
        return args;
    }
}

public class VerbCommandHandler : ICommandHandler
{
    private readonly CommandDispatcher _dispatcher;
    private readonly ExpressionEvaluator? _evaluator;

    public string Name => "@verb";

    public VerbCommandHandler(CommandDispatcher dispatcher, ExpressionEvaluator? evaluator = null)
    {
        _dispatcher = dispatcher;
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var executor = db.GetObject(context.Executor);
        if (executor == null)
            return false;

        var victim = db.MatchObject(context.Executor, context.LeftArg);
        if (victim == null || victim.Type == ObjectType.Garbage)
        {
            context.Notify(Feedback.WhatWasTheVictimOfTheVerb);
            return false;
        }

        if (!context.Arguments.Contains('='))
        {
            context.Notify(Feedback.WhatDoYouWantToDoWithTheVerb);
            return false;
        }

        var parts = ActionListCommandHandler.SplitArgs(context.RightArg);
        for (int i = 0; i < parts.Count; i++)
            parts[i] = Evaluate(context, parts[i]);

        if (parts.Count == 0 || parts[0].Length == 0)
        {
            context.Notify(Feedback.WhatDoYouWantToDoWithTheVerb);
            return false;
        }

        var actor = db.MatchObject(context.Executor, parts[0]);
        if (actor == null || actor.Type == ObjectType.Garbage)
        {
            context.Notify(Feedback.WhatDoYouWantToDoTheVerb);
            return false;
        }

        var enactor = db.GetObject(context.Enactor) ?? executor;
        if (!Allows(executor, enactor, victim, actor))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        string what = Arg(parts, 1);
        string playerDefault = Arg(parts, 2);
        string others = Arg(parts, 3);
        string othersDefault = Arg(parts, 4);
        string action = Arg(parts, 5);
        var frame = new ActionFrame { Parent = context.Frame };
        for (int i = 0; i < ActionFrame.ArgumentCount; i++)
        {
            int source = i + 6;
            frame.Args[i] = source < parts.Count ? parts[source] : string.Empty;
        }

        if (actor.Location.IsValid)
        {
            if (what.Length > 0)
                Tell(context, db, victim, actor, frame, what, playerDefault, actor.Id, prefix: null);
            if (!actor.HasFlag("DARK") && (others.Length > 0 || othersDefault.Length > 0))
            {
                string? heard = TellText(context, db, victim, actor, frame, others, null);
                if (heard == null && othersDefault.Length > 0)
                    heard = actor.Name + " " + othersDefault;
                if (!string.IsNullOrEmpty(heard))
                {
                    foreach (var listener in db.GetContents(actor.Location))
                    {
                        if (listener.Id == actor.Id || listener.Id == victim.Id)
                            continue;
                        if (listener.Type != ObjectType.Player && !listener.HasFlag("PUPPET"))
                            continue;
                        context.NotifyTarget(listener.Id, heard);
                    }
                }
            }
        }

        if (action.Length > 0)
            RunAction(context, db, victim, actor, frame, action);
        return true;
    }

    private void RunAction(CommandContext context, ObjectDatabase db, MUSHObject victim, MUSHObject actor, ActionFrame frame, string attributeName)
    {
        var attribute = victim.GetAttribute(attributeName.ToUpperInvariant(), db);
        if (attribute == null || string.IsNullOrWhiteSpace(attribute.Value))
            return;
        foreach (string piece in IncludeCommandHandler.SplitCommands(attribute.Value))
        {
            string command = IncludeCommandHandler.Unwrap(piece);
            if (command.Length == 0)
                continue;
            command = ActionListCommandHandler.Substitute(command, frame);
            var result = _dispatcher.Dispatch(victim.Id, command, db, frame, actor.Id);
            context.TargetedNotifications.AddRange(result.TargetedNotifications);
            context.Notifications.AddRange(result.Notifications);
            if (frame.Break)
                break;
        }
    }

    private void Tell(CommandContext context, ObjectDatabase db, MUSHObject victim, MUSHObject actor, ActionFrame frame, string attributeName, string fallback, Dbref target, string? prefix)
    {
        string? text = TellText(context, db, victim, actor, frame, attributeName, fallback);
        if (string.IsNullOrEmpty(text))
            return;
        context.NotifyTarget(target, prefix == null ? text : prefix + text);
    }

    private string? TellText(CommandContext context, ObjectDatabase db, MUSHObject victim, MUSHObject actor, ActionFrame frame, string attributeName, string? fallback)
    {
        if (attributeName.Length > 0)
        {
            var attribute = victim.GetAttribute(attributeName.ToUpperInvariant(), db);
            if (attribute != null)
            {
                string text = EvaluateAs(victim.Id, actor.Id, frame, attribute.Value);
                return text.Length == 0 ? null : text;
            }
        }

        return string.IsNullOrEmpty(fallback) ? null : fallback;
    }

    private string Evaluate(CommandContext context, string text)
        => EvaluateAs(context.Executor, context.Enactor, context.Frame, text);

    private string EvaluateAs(Dbref executor, Dbref enactor, ActionFrame? frame, string text)
    {
        if (_evaluator == null || text.Length == 0)
            return text;
        return _evaluator.Evaluate(text, new EvaluationContext(executor, enactor, caller: enactor, args: frame?.Args));
    }

    private static string Arg(List<string> parts, int index)
        => index < parts.Count ? parts[index] : string.Empty;

    private static bool Allows(MUSHObject executor, MUSHObject enactor, MUSHObject victim, MUSHObject actor)
    {
        if (executor.IsWizard && !victim.IsGod && !actor.IsGod)
            return true;
        if (executor.IsGod)
            return true;
        if (Controls(executor, victim) && Controls(executor, actor))
            return true;
        return Controls(enactor, actor) && ListenerSearch.CanExamine(executor, victim);
    }

    private static bool Controls(MUSHObject actor, MUSHObject target)
    {
        if (target.IsGod && !actor.IsGod)
            return false;
        return ListenerSearch.Controls(actor, target);
    }
}
