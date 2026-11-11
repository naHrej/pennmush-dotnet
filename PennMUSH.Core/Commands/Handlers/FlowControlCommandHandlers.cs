namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class SwitchCommandHandler : ICommandHandler
{
    private readonly ExpressionEvaluator? _evaluator;
    private readonly CommandDispatcher _dispatcher;

    public string Name => "@switch";

    public SwitchCommandHandler(CommandDispatcher dispatcher, ExpressionEvaluator? evaluator = null)
    {
        _dispatcher = dispatcher;
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            string name = string.IsNullOrEmpty(context.CommandName) ? "@switch" : context.CommandName;
            context.Notify($"Usage: {name} <expression>=<pattern>,<action>[,<pattern>,<action>...][,<default>]");
            return false;
        }

        string expr = context.LeftArg;
        if (_evaluator != null)
        {
            var evalCtx = new EvaluationContext(context.Executor, context.Enactor);
            expr = _evaluator.Evaluate(expr, evalCtx);
        }

        // Split right arg by commas (respecting braces if needed)
        var pairs = SplitArgs(context.RightArg);
        bool select = context.CommandName.Equals("@select", StringComparison.OrdinalIgnoreCase);
        bool firstOnly = select || context.Switches.Contains("first") || !context.Switches.Contains("all");
        bool matched = false;

        for (int i = 0; i < pairs.Count; i += 2)
        {
            if (i + 1 >= pairs.Count)
            {
                // Odd last argument is the default action
                if (!matched)
                {
                    ExecuteAction(context.Executor, pairs[i], db, context);
                }
                break;
            }

            string pattern = pairs[i].Trim();
            string action = pairs[i + 1].Trim();

            if (PatternMatcher.Match(pattern, expr, out _))
            {
                matched = true;
                ExecuteAction(context.Executor, action, db, context);
                if (firstOnly) break;
            }
        }

        return true;
    }

    private void ExecuteAction(Dbref executor, string action, ObjectDatabase db, CommandContext parentContext)
    {
        var res = _dispatcher.Dispatch(executor, action, db);
        foreach (var note in res.TargetedNotifications) parentContext.TargetedNotifications.Add(note);
        foreach (var note in res.Notifications) parentContext.Notifications.Add(note);
    }

    private static List<string> SplitArgs(string input)
    {
        var list = new List<string>();
        int depth = 0;
        int start = 0;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c is '[' or '{' or '(') depth++;
            else if (c is ']' or '}' or ')') depth--;
            else if (c == ',' && depth == 0)
            {
                list.Add(input.Substring(start, i - start));
                start = i + 1;
            }
        }

        if (start < input.Length)
        {
            list.Add(input.Substring(start));
        }

        return list;
    }
}

public class DolistCommandHandler : ICommandHandler
{
    private readonly ExpressionEvaluator? _evaluator;
    private readonly CommandDispatcher _dispatcher;

    public string Name => "@dolist";

    public DolistCommandHandler(CommandDispatcher dispatcher, ExpressionEvaluator? evaluator = null)
    {
        _dispatcher = dispatcher;
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify("Usage: @dolist <list>=<command>");
            return false;
        }

        string rawList = context.LeftArg;
        if (_evaluator != null)
        {
            var evalCtx = new EvaluationContext(context.Executor, context.Enactor);
            rawList = _evaluator.Evaluate(rawList, evalCtx);
        }

        var items = rawList.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string template = context.RightArg;

        for (int i = 0; i < items.Length; i++)
        {
            string item = items[i];
            string substituted = template
                .Replace("##", item)
                .Replace("#@", (i + 1).ToString())
                .Replace("%0", item);

            var res = _dispatcher.Dispatch(context.Executor, substituted, db);
            foreach (var note in res.TargetedNotifications) context.TargetedNotifications.Add(note);
            foreach (var note in res.Notifications) context.Notifications.Add(note);
        }

        return true;
    }
}

public class TriggerCommandHandler : ICommandHandler
{
    private readonly ExpressionEvaluator? _evaluator;
    private readonly CommandDispatcher _dispatcher;

    public string Name => "@trigger";

    public TriggerCommandHandler(CommandDispatcher dispatcher, ExpressionEvaluator? evaluator = null)
    {
        _dispatcher = dispatcher;
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg))
        {
            context.Notify("Usage: @trigger <obj>/<attr>[=<args>]");
            return false;
        }

        string targetStr = context.LeftArg;
        int slash = targetStr.IndexOf('/');
        if (slash == -1)
        {
            context.Notify("You must specify both an object and attribute: <obj>/<attr>");
            return false;
        }

        string objRef = targetStr.Substring(0, slash).Trim();
        string attrName = targetStr.Substring(slash + 1).Trim();

        var target = ResolveTarget(objRef, context.Executor, db);
        if (target == null)
        {
            context.Notify("I can't find that object.");
            return false;
        }

        var attr = target.GetAttribute(attrName);
        if (attr == null || string.IsNullOrWhiteSpace(attr.Value))
        {
            return false;
        }

        string[] args = string.IsNullOrWhiteSpace(context.RightArg)
            ? Array.Empty<string>()
            : context.RightArg.Split(',');

        string code = attr.Value;
        if (_evaluator != null)
        {
            var evalCtx = new EvaluationContext(target.Id, context.Executor, location: target.Location, args: args);
            code = _evaluator.Evaluate(code, evalCtx);
        }

        if (!string.IsNullOrWhiteSpace(code))
        {
            var res = _dispatcher.Dispatch(target.Id, code, db);
            foreach (var note in res.TargetedNotifications) context.TargetedNotifications.Add(note);
            foreach (var note in res.Notifications) context.Notifications.Add(note);
        }

        return true;
    }

    private static MUSHObject? ResolveTarget(string nameOrRef, Dbref executor, ObjectDatabase db)
    {
        if (nameOrRef.Equals("me", StringComparison.OrdinalIgnoreCase))
            return db.GetObject(executor);
        if (nameOrRef.Equals("here", StringComparison.OrdinalIgnoreCase))
        {
            var exec = db.GetObject(executor);
            return exec != null && exec.Location.IsValid ? db.GetObject(exec.Location) : null;
        }
        if (nameOrRef.StartsWith('#') && int.TryParse(nameOrRef.Substring(1), out int id))
            return db.GetObject(new Dbref(id));
        return db.FindObject(nameOrRef);
    }
}

public class ForceCommandHandler : ICommandHandler
{
    private readonly CommandDispatcher _dispatcher;

    public string Name => "@force";

    public ForceCommandHandler(CommandDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify("Usage: @force <target>=<command>");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        var target = db.FindObject(context.LeftArg.Trim());
        if (target == null)
        {
            context.Notify("I can't find that target.");
            return false;
        }

        if (execObj == null || (!execObj.IsWizard && target.Owner != context.Executor && target.Id != context.Executor))
        {
            context.Notify("Permission denied.");
            return false;
        }

        var res = _dispatcher.Dispatch(target.Id, context.RightArg.Trim(), db);
        foreach (var note in res.TargetedNotifications) context.TargetedNotifications.Add(note);
        foreach (var note in res.Notifications) context.Notifications.Add(note);
        return true;
    }
}

public class IfElseCommandHandler : ICommandHandler
{
    private readonly ExpressionEvaluator? _evaluator;
    private readonly CommandDispatcher _dispatcher;

    public string Name => "@ifelse";

    public IfElseCommandHandler(CommandDispatcher dispatcher, ExpressionEvaluator? evaluator = null)
    {
        _dispatcher = dispatcher;
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            bool skip = context.CommandName.Equals("@skip", StringComparison.OrdinalIgnoreCase);
            string name = string.IsNullOrEmpty(context.CommandName) ? "@ifelse" : context.CommandName;
            string branches = skip ? "<false_action>[,<true_action>]" : "<true_action>[,<false_action>]";
            context.Notify($"Usage: {name} <condition>={branches}");
            return false;
        }

        string cond = context.LeftArg;
        if (_evaluator != null)
        {
            var evalCtx = new EvaluationContext(context.Executor, context.Enactor);
            cond = _evaluator.Evaluate(cond, evalCtx);
        }

        bool isTrue = !string.IsNullOrWhiteSpace(cond) && cond.Trim() != "0" && !cond.Trim().StartsWith("#-");
        if (context.CommandName.Equals("@skip", StringComparison.OrdinalIgnoreCase))
            isTrue = !isTrue;
        var parts = context.RightArg.Split(new[] { ',' }, 2);

        string action = isTrue ? parts[0].Trim() : (parts.Length > 1 ? parts[1].Trim() : "");
        if (!string.IsNullOrEmpty(action))
        {
            var res = _dispatcher.Dispatch(context.Executor, action, db);
            foreach (var note in res.TargetedNotifications) context.TargetedNotifications.Add(note);
            foreach (var note in res.Notifications) context.Notifications.Add(note);
        }

        return true;
    }
}

public class CommentCommandHandler : ICommandHandler
{
    public string Name => "@@";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        // Null command comment: does nothing
        return true;
    }
}
