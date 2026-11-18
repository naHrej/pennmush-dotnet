namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using System.Text;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class IncludeCommandHandler : ICommandHandler
{
    private const int RecursionLimit = 50;

    private readonly CommandDispatcher _dispatcher;
    private readonly ExpressionEvaluator? _evaluator;
    private int _depth;

    public string Name => "@include";

    public IncludeCommandHandler(CommandDispatcher dispatcher, ExpressionEvaluator? evaluator = null)
    {
        _dispatcher = dispatcher;
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        string left = context.LeftArg.Trim();
        int slash = left.IndexOf('/');
        if (slash < 0)
        {
            context.Notify(Feedback.INeedToKnowWhatAttributeToInclude);
            return false;
        }

        string objectName = left.Substring(0, slash).Trim();
        string attributeName = left.Substring(slash + 1).Trim();
        if (objectName.Length == 0)
        {
            context.Notify(Feedback.ICantSeeThatHere);
            return false;
        }

        var target = db.MatchObject(context.Executor, objectName);
        if (target == null)
        {
            context.Notify(Feedback.ICantSeeThatHere);
            return false;
        }

        var actor = db.GetObject(context.Executor);
        if (target.IsGod && actor is { IsGod: false })
        {
            context.Notify(Feedback.YouCantIncludeGod);
            return false;
        }

        var attribute = target.GetAttribute(attributeName.ToUpperInvariant(), db);
        if (attribute == null)
        {
            context.Notify(Feedback.NoSuchAttribute);
            return false;
        }

        if (_depth >= RecursionLimit)
            return true;

        string[] args = context.Arguments.Contains('=')
            ? SplitArgs(context.RightArg)
            : Array.Empty<string>();
        var frame = new ActionFrame
        {
            Parent = context.Frame,
            NoBreak = context.Switches.Contains("nobreak")
        };
        for (int i = 0; i < args.Length && i < ActionFrame.ArgumentCount; i++)
            frame.Args[i] = args[i];
        string source = StripListenPrefix(attribute.Value);
        _depth++;
        try
        {
            int passes = 0;
            do
            {
                frame.Retry = false;
                foreach (string piece in SplitCommands(source))
                {
                    string command = Unwrap(piece);
                    if (command.Length == 0)
                        continue;
                    command = Prepare(command, context, frame.Args);
                    if (command.Length == 0)
                        continue;
                    var result = _dispatcher.Dispatch(context.Executor, command, db, frame, context.Enactor);
                    context.TargetedNotifications.AddRange(result.TargetedNotifications);
                    context.Notifications.AddRange(result.Notifications);
                    if (frame.Break)
                        break;
                }

                passes++;
            }
            while (frame.Retry && !frame.Break && passes < RecursionLimit);

            if (frame.Break && !frame.NoBreak && context.Frame != null)
                context.Frame.Break = true;
        }
        finally
        {
            _depth--;
        }

        return true;
    }

    private string Prepare(string command, CommandContext context, string[] args)
    {
        if (_evaluator == null)
            return SubstituteArgs(command, args);

        var evaluation = new EvaluationContext(
            context.Executor,
            context.Enactor,
            caller: context.Enactor,
            args: args);
        return _evaluator.Evaluate(command, evaluation).Trim();
    }

    private static string SubstituteArgs(string command, string[] args)
    {
        var built = new StringBuilder();
        for (int i = 0; i < command.Length; i++)
        {
            if (command[i] == '%' && i + 1 < command.Length && char.IsDigit(command[i + 1]))
            {
                int index = command[i + 1] - '0';
                if (index < args.Length)
                    built.Append(args[index]);
                i++;
                continue;
            }

            built.Append(command[i]);
        }

        return built.ToString().Trim();
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

    internal static string Unwrap(string command)
    {
        string trimmed = command.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '{' && trimmed[^1] == '}' && MatchingBrace(trimmed))
            return trimmed[1..^1].Trim();
        return trimmed;
    }

    private static bool MatchingBrace(string text)
    {
        int depth = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 1 < text.Length)
            {
                i++;
                continue;
            }

            if (text[i] == '{')
                depth++;
            else if (text[i] == '}')
                depth--;
            if (depth == 0)
                return i == text.Length - 1;
        }

        return false;
    }

    internal static List<string> SplitCommands(string action)
    {
        var commands = new List<string>();
        var current = new StringBuilder();
        int braces = 0;
        for (int i = 0; i < action.Length; i++)
        {
            char c = action[i];
            if (c == '\\' && i + 1 < action.Length)
            {
                current.Append(c);
                current.Append(action[++i]);
                continue;
            }

            if (c == '{')
                braces++;
            else if (c == '}' && braces > 0)
                braces--;

            if (braces == 0 && (c == ';' || c == '\n' || c == '\r'))
            {
                Add(commands, current);
                if (c == '\r' && i + 1 < action.Length && action[i + 1] == '\n')
                    i++;
                continue;
            }

            current.Append(c);
        }

        Add(commands, current);
        return commands;
    }

    private static void Add(List<string> commands, StringBuilder current)
    {
        if (current.Length == 0)
            return;
        commands.Add(current.ToString());
        current.Clear();
    }

    private static string[] SplitArgs(string right)
    {
        if (right.Length == 0)
            return Array.Empty<string>();

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

            if (c == ',' && braces == 0)
            {
                args.Add(current.ToString().Trim());
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        args.Add(current.ToString().Trim());
        return args.ToArray();
    }
}
