namespace PennMUSH.Core.Commands;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class CommandDispatcher
{
    private readonly CommandTable _commandTable = new();
    private readonly Dictionary<string, List<(string Restriction, string? ErrorMessage)>> _restrictions = new(StringComparer.OrdinalIgnoreCase);
    private readonly ExpressionEvaluator? _evaluator;

    public CommandTable Table => _commandTable;
    public CommandCatalog Catalog { get; } = new();
    public ExpressionEvaluator? Evaluator => _evaluator;

    public CommandDispatcher(ExpressionEvaluator? evaluator = null)
    {
        _evaluator = evaluator;
    }

    public void Register(ICommandHandler handler, params string[] aliases)
    {
        _commandTable.Register(handler, aliases);
    }

    public bool Unregister(string commandName)
    {
        return _commandTable.Unregister(commandName);
    }

    public bool AddAlias(string originalCommand, string alias)
    {
        return _commandTable.AddAlias(originalCommand, alias);
    }

    public void ReserveAlias(string alias)
    {
        _commandTable.ReserveAlias(alias);
    }

    public void AddRestriction(string commandName, string restriction, string? customErrorMessage = null)
    {
        if (!_restrictions.TryGetValue(commandName, out var list))
        {
            list = new List<(string, string?)>();
            _restrictions[commandName] = list;
        }
        list.Add((restriction, customErrorMessage));
    }

    public CommandContext Dispatch(Dbref executor, string input, ObjectDatabase db) =>
        Dispatch(executor, input, db, null, true);

    internal CommandContext DispatchHook(Dbref executor, string input, ObjectDatabase db, Dbref enactor) =>
        Dispatch(executor, input, db, enactor, false);

    internal CommandContext Dispatch(Dbref executor, string input, ObjectDatabase db, ActionFrame? frame, Dbref enactor)
    {
        var context = Parse(executor, input, enactor);
        context.Frame = frame;
        return Run(context, db, true);
    }

    internal CommandContext DispatchQueued(Dbref executor, string input, ObjectDatabase db, Dbref enactor, string[]? args, IReadOnlyDictionary<string, string>? registers = null)
    {
        var evalCtx = BindQueuedCommand(executor, enactor, input, args, registers, db, out string bound);
        if (string.IsNullOrWhiteSpace(bound))
            return new CommandContext { Executor = executor, Enactor = enactor };
        var context = Parse(executor, bound, enactor);
        context.PositionalArgs = args;
        if (evalCtx != null)
        {
            foreach (var register in evalCtx.Registers)
                context.Registers[register.Key] = register.Value;
        }
        else if (registers != null)
        {
            foreach (var register in registers)
                context.Registers[register.Key] = register.Value;
        }

        return Run(context, db, true);
    }

    private EvaluationContext? BindQueuedCommand(Dbref executor, Dbref enactor, string input, string[]? args, IReadOnlyDictionary<string, string>? registers, ObjectDatabase db, out string bound)
    {
        if (_evaluator == null)
        {
            bound = input;
            return null;
        }
        var ctx = new EvaluationContext(
            executor,
            enactor,
            location: db.GetObject(executor)?.Location ?? Dbref.Nothing,
            args: args);
        if (registers != null)
        {
            foreach (var register in registers)
                ctx.SetRegister(register.Key, register.Value);
        }

        if (IsDeferredRightArgCommand(input, out int eqIndex))
        {
            string head = _evaluator.Evaluate(input.Substring(0, eqIndex), ctx);
            bound = head + input.Substring(eqIndex);
        }
        else if (TrySplitFirstTopLevelEquals(input, out int argEqIndex))
        {
            string head = _evaluator.Evaluate(input.Substring(0, argEqIndex), ctx);
            string tail = _evaluator.Evaluate(input.Substring(argEqIndex + 1), ctx);
            bound = head + "=" + tail;
        }
        else
        {
            bound = _evaluator.Evaluate(input, ctx);
        }
        return ctx;
    }

    private static bool TrySplitFirstTopLevelEquals(string input, out int eqIndex)
    {
        eqIndex = -1;
        int depth = 0;
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c == '\\' && i + 1 < input.Length) { i++; continue; }
            if (c is '[' or '{' or '(') depth++;
            else if (c is ']' or '}' or ')') depth = Math.Max(0, depth - 1);
            else if (c == '=' && depth == 0)
            {
                eqIndex = i;
                return true;
            }
        }
        return false;
    }

    private static bool IsDeferredRightArgCommand(string input, out int eqIndex)
    {
        eqIndex = -1;
        string trimmed = input.TrimStart();
        int space = trimmed.IndexOf(' ');
        if (space <= 0) return false;
        string cmd = trimmed.Substring(0, space);
        int slash = cmd.IndexOf('/');
        if (slash > 0) cmd = cmd.Substring(0, slash);

        bool isDeferred = cmd.Equals("@switch", StringComparison.OrdinalIgnoreCase)
            || cmd.Equals("@sw", StringComparison.OrdinalIgnoreCase)
            || cmd.Equals("@select", StringComparison.OrdinalIgnoreCase)
            || cmd.Equals("@ifelse", StringComparison.OrdinalIgnoreCase)
            || cmd.Equals("@skip", StringComparison.OrdinalIgnoreCase)
            || cmd.Equals("@dolist", StringComparison.OrdinalIgnoreCase)
            || cmd.Equals("@wait", StringComparison.OrdinalIgnoreCase)
            || cmd.Equals("@force", StringComparison.OrdinalIgnoreCase);

        if (!isDeferred) return false;

        int leading = input.Length - trimmed.Length;
        int relEq = trimmed.IndexOf('=');
        if (relEq <= space) return false;
        eqIndex = leading + relEq;
        return true;
    }

    private CommandContext Dispatch(Dbref executor, string input, ObjectDatabase db, Dbref? enactor, bool hooks)
    {
        return Run(Parse(executor, input, enactor), db, hooks);
    }

    private CommandContext Run(CommandContext context, ObjectDatabase db, bool hooks)
    {
        Dbref executor = context.Executor;
        string input = context.RawCommand;
        if (string.IsNullOrEmpty(context.CommandName))
        {
            return context;
        }

        var execObj = db.GetObject(executor);
        var lookup = _commandTable.Lookup(context.CommandName);

        if (lookup.Status == CommandLookupStatus.Found && lookup.Handler != null)
        {
            string canonical = lookup.CanonicalName ?? context.CommandName;
            if (!Catalog.IsDisabled(canonical))
            {
                if (execObj != null && !CheckRestrictions(execObj, canonical, context))
                    return context;
                if (hooks && !Catalog.Prepare(this, canonical, context, db))
                    return context;

                lookup.Handler.Execute(context, db);
                if (hooks)
                    Catalog.After(this, canonical, context, db);
                return context;
            }
        }

        if (lookup.Status == CommandLookupStatus.Ambiguous)
        {
            context.Notify(Feedback.WhichOfThoseCommandsDoYouMean);
            return context;
        }

        if (lookup.Status == CommandLookupStatus.NotFound &&
            AttributeSetCommandHandler.TryExecute(context, db))
        {
            return context;
        }

        // Status is NotFound or Reserved: try non-command fallbacks
        // 1. Check implicit exit matching in executor's location
        if (execObj != null && execObj.Location.IsValid)
        {
            var exitObj = db.FindExit(execObj.Location, context.CommandName);
            if (exitObj != null && TryDispatchGo(execObj, input, context.CommandName, db, context))
                return context;
        }

        // 2. Check EALIAS / LALIAS for mobile executors (Players and Things)
        if (execObj != null && execObj.Location.IsValid && (execObj.Type == ObjectType.Player || execObj.Type == ObjectType.Thing))
        {
            if (TryMatchEnterLeaveAlias(execObj, input, db, context))
                return context;
        }

        // 3. Tiered $command:action listeners (local -> zone -> Master Room #2)
        if (execObj != null && !execObj.HasFlag("GAGGED") && TryMatchCommandListener(execObj, input, db, context))
        {
            return context;
        }

        // 4. Check channel prefix (+channel message or +channel/who) only if a matching channel exists.
        if (execObj != null
            && context.CommandName.StartsWith('+') && context.CommandName.Length > 1
            && (context.Switches.Count > 0 || context.Arguments.Length > 0))
        {
            string chName = context.CommandName.Substring(1);
            var chanLookup = _commandTable.Lookup("@channel");
            var chatLookup = _commandTable.Lookup("@chat");
            bool matchesChannel = chatLookup.Handler is Handlers.ChatCommandHandler chatHandler
                && chatHandler.MatchesChannel(execObj, chName, db);

            if (matchesChannel && context.Switches.Count > 0 && chanLookup.Status == CommandLookupStatus.Found && chanLookup.Handler != null)
            {
                var chCtx = new CommandContext
                {
                    Executor = executor,
                    Enactor = executor,
                    RawCommand = input,
                    CommandName = "@channel",
                    Switches = context.Switches,
                    Arguments = chName,
                    LeftArg = chName,
                    RightArg = context.Arguments
                };
                chanLookup.Handler.Execute(chCtx, db);
                foreach (var note in chCtx.TargetedNotifications) context.TargetedNotifications.Add(note);
                foreach (var note in chCtx.Notifications) context.Notifications.Add(note);
                return context;
            }
            else if (matchesChannel && chatLookup.Status == CommandLookupStatus.Found && chatLookup.Handler != null)
            {
                var chatCtx = new CommandContext
                {
                    Executor = executor,
                    Enactor = executor,
                    RawCommand = input,
                    CommandName = "@chat",
                    LeftArg = chName,
                    RightArg = context.Arguments,
                    Arguments = $"{chName}={context.Arguments}"
                };
                chatLookup.Handler.Execute(chatCtx, db);
                foreach (var note in chatCtx.TargetedNotifications) context.TargetedNotifications.Add(note);
                foreach (var note in chatCtx.Notifications) context.Notifications.Add(note);
                return context;
            }
        }

        // 5. Unrecognized command
        context.Notify(Feedback.HuhTypeHelpForHelp);
        return context;
    }

    private bool TryDispatchGo(MUSHObject execObj, string input, string exitTarget, ObjectDatabase db, CommandContext context)
    {
        var goLookup = _commandTable.Lookup("go");
        if (goLookup.Status != CommandLookupStatus.Found || goLookup.Handler == null)
            return false;

        var goCtx = new CommandContext
        {
            Executor = execObj.Id,
            Enactor = execObj.Id,
            RawCommand = input,
            CommandName = "go",
            Arguments = exitTarget,
            LeftArg = exitTarget
        };
        if (CheckRestrictions(execObj, "go", goCtx))
        {
            goLookup.Handler.Execute(goCtx, db);
        }
        foreach (var note in goCtx.TargetedNotifications)
            context.TargetedNotifications.Add(note);
        foreach (var note in goCtx.Notifications)
            context.Notifications.Add(note);
        return true;
    }

    private bool TryMatchEnterLeaveAlias(MUSHObject executor, string input, ObjectDatabase db, CommandContext context)
    {
        string trimmed = input.Trim();
        if (trimmed.Length == 0)
            return false;

        // Check EALIAS on objects in executor's location
        var enterLookup = _commandTable.Lookup("enter");
        if (enterLookup.Status == CommandLookupStatus.Found && enterLookup.Handler != null)
        {
            foreach (var item in db.GetContents(executor.Location))
            {
                if (item.Id == executor.Id)
                    continue;
                string? ealias = item.GetAttribute("EALIAS")?.Value;
                if (!string.IsNullOrWhiteSpace(ealias) && MatchesAliasList(ealias, trimmed))
                {
                    var enterCtx = new CommandContext
                    {
                        Executor = executor.Id,
                        Enactor = executor.Id,
                        RawCommand = $"enter #{item.Id.Value}",
                        CommandName = "enter",
                        Arguments = $"#{item.Id.Value}"
                    };
                    if (CheckRestrictions(executor, "enter", enterCtx))
                        enterLookup.Handler.Execute(enterCtx, db);
                    foreach (var note in enterCtx.TargetedNotifications) context.TargetedNotifications.Add(note);
                    foreach (var note in enterCtx.Notifications) context.Notifications.Add(note);
                    return true;
                }
            }
        }

        // Check LALIAS on executor's location if it is not a room
        var loc = db.GetObject(executor.Location);
        var leaveLookup = _commandTable.Lookup("leave");
        if (loc != null && loc.Type != ObjectType.Room && leaveLookup.Status == CommandLookupStatus.Found && leaveLookup.Handler != null)
        {
            string? lalias = loc.GetAttribute("LALIAS")?.Value;
            if (!string.IsNullOrWhiteSpace(lalias) && MatchesAliasList(lalias, trimmed))
            {
                var leaveCtx = new CommandContext
                {
                    Executor = executor.Id,
                    Enactor = executor.Id,
                    RawCommand = "leave",
                    CommandName = "leave",
                    Arguments = string.Empty
                };
                if (CheckRestrictions(executor, "leave", leaveCtx))
                    leaveLookup.Handler.Execute(leaveCtx, db);
                foreach (var note in leaveCtx.TargetedNotifications) context.TargetedNotifications.Add(note);
                foreach (var note in leaveCtx.Notifications) context.Notifications.Add(note);
                return true;
            }
        }

        return false;
    }

    private static bool MatchesAliasList(string aliasList, string input)
    {
        foreach (string part in aliasList.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.Trim().Equals(input, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private bool TryMatchCommandListener(MUSHObject executor, string input, ObjectDatabase db, CommandContext context)
    {
        int matched = 0;
        var checkedLocal = new HashSet<Dbref>();

        // Tier 1: Executor itself, contents of location, location itself, executor's inventory
        if (checkedLocal.Add(executor.Id))
            matched += MatchObjectListeners(executor, executor, input, db, context);

        if (executor.Location.IsValid)
        {
            foreach (var item in db.GetContents(executor.Location))
            {
                if (checkedLocal.Add(item.Id))
                    matched += MatchObjectListeners(item, executor, input, db, context);
            }

            if (executor.Location != executor.Id && checkedLocal.Add(executor.Location))
            {
                var loc = db.GetObject(executor.Location);
                if (loc != null)
                    matched += MatchObjectListeners(loc, executor, input, db, context);
            }
        }

        if (executor.Location != executor.Id)
        {
            foreach (var item in db.GetContents(executor.Id))
            {
                if (checkedLocal.Add(item.Id))
                    matched += MatchObjectListeners(item, executor, input, db, context);
            }
        }

        if (matched > 0)
            return true;

        // Tier 2: Zones (location's zone, then executor's personal zone)
        var locationObj = executor.Location.IsValid ? db.GetObject(executor.Location) : null;
        if (locationObj != null && locationObj.Zone.IsValid)
        {
            var locZone = db.GetObject(locationObj.Zone);
            if (locZone != null)
            {
                if (locZone.Type == ObjectType.Room)
                {
                    var zoneExit = db.FindExit(locZone.Id, context.CommandName);
                    if (zoneExit != null && TryDispatchGo(executor, input, context.CommandName, db, context))
                        return true;

                    foreach (var item in db.GetContents(locZone.Id))
                        matched += MatchObjectListeners(item, executor, input, db, context);
                }
                else
                {
                    matched += MatchObjectListeners(locZone, executor, input, db, context);
                }
            }
        }

        if (matched == 0 && executor.Zone.IsValid && (locationObj == null || executor.Zone != locationObj.Zone))
        {
            var execZone = db.GetObject(executor.Zone);
            if (execZone != null)
            {
                if (execZone.Type == ObjectType.Room)
                {
                    foreach (var item in db.GetContents(execZone.Id))
                        matched += MatchObjectListeners(item, executor, input, db, context);
                }
                else
                {
                    matched += MatchObjectListeners(execZone, executor, input, db, context);
                }
            }
        }

        if (matched > 0)
            return true;

        // Tier 3: Master Room (#2) global exits and global $ commands
        var masterRoom = new Dbref(2);
        if (executor.Location != masterRoom)
        {
            var globalExit = db.FindExit(masterRoom, context.CommandName);
            if (globalExit != null && TryDispatchGo(executor, input, context.CommandName, db, context))
                return true;

            foreach (var item in db.GetContents(masterRoom))
                matched += MatchObjectListeners(item, executor, input, db, context);
        }

        return matched > 0;
    }

    private int MatchObjectListeners(MUSHObject candidate, MUSHObject executor, string input, ObjectDatabase db, CommandContext context)
    {
        if (candidate.HasFlag("NO_COMMAND") || candidate.HasFlag("HALT"))
            return 0;

        int matched = 0;
        var seenAttrs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visitedObjs = new HashSet<Dbref>();
        var queue = new Queue<MUSHObject>();
        queue.Enqueue(candidate);
        visitedObjs.Add(candidate.Id);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            bool isSelf = current.Id == candidate.Id;

            foreach (var listener in current.Listeners.ToArray())
            {
                if (!listener.Command)
                    continue;
                var attr = current.GetAttribute(listener.AttributeName);
                if (attr == null)
                    continue;
                if (!isSelf && IsPrivateOrUnderPrivateBranch(current, listener.AttributeName, attr))
                    continue;
                if (!seenAttrs.Add(listener.AttributeName))
                    continue;
                if (attr.Flags.HasFlag(Attributes.AttributeFlags.NoProg))
                    continue;

                if (TryMatchListenerPattern(listener, input, out var captures))
                {
                    ExecuteListenerAction(candidate, executor, listener.Action, captures, db, context);
                    matched++;
                }
            }

            foreach (var parentId in current.Parents)
            {
                if (visitedObjs.Add(parentId))
                {
                    var parent = db.GetObject(parentId);
                    if (parent != null)
                        queue.Enqueue(parent);
                }
            }
        }
        return matched;
    }

    private static bool IsPrivateOrUnderPrivateBranch(MUSHObject obj, string attrName, Attributes.AttributeEntry attr)
    {
        if (attr.Flags.HasFlag(Attributes.AttributeFlags.Private))
            return true;
        int tick = attrName.IndexOf('`');
        while (tick > 0)
        {
            var branch = obj.GetAttribute(attrName[..tick]);
            if (branch != null && branch.Flags.HasFlag(Attributes.AttributeFlags.Private))
                return true;
            tick = attrName.IndexOf('`', tick + 1);
        }
        return false;
    }

    internal static bool TryMatchListenerPattern(StoredListener listener, string input, out string[] captures)
    {
        if (!listener.Regexp)
            return PatternMatcher.Match(listener.Pattern, input, out captures);

        try
        {
            var options = listener.Case
                ? System.Text.RegularExpressions.RegexOptions.None
                : System.Text.RegularExpressions.RegexOptions.IgnoreCase;
            var match = System.Text.RegularExpressions.Regex.Match(input, listener.Pattern, options, TimeSpan.FromSeconds(1));
            if (!match.Success)
            {
                captures = Array.Empty<string>();
                return false;
            }

            captures = new string[match.Groups.Count];
            for (int i = 0; i < match.Groups.Count; i++)
                captures[i] = match.Groups[i].Value;
            return true;
        }
        catch (ArgumentException)
        {
            captures = Array.Empty<string>();
            return false;
        }
        catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
        {
            captures = Array.Empty<string>();
            return false;
        }
    }

    internal void ExecuteListenerAction(MUSHObject listener, MUSHObject enactor, string action, string[] captures, ObjectDatabase db, CommandContext context)
    {
        foreach (string piece in Handlers.IncludeCommandHandler.SplitCommands(action))
        {
            string trimmed = piece.TrimStart();
            if (trimmed.Length == 0)
                continue;

            int firstSpace = trimmed.IndexOf(' ');
            string firstWord = firstSpace >= 0 ? trimmed[..firstSpace] : trimmed;
            int slash = firstWord.IndexOf('/');
            string cmdHead = slash >= 0 ? firstWord[..slash] : firstWord;

            bool isCommand = trimmed.StartsWith('@') || trimmed.StartsWith('"') || trimmed.StartsWith(':') || trimmed.StartsWith(';') || trimmed.StartsWith('&')
                || _commandTable.Lookup(cmdHead).Status == CommandLookupStatus.Found;
            if (isCommand)
            {
                var subCtx = DispatchQueued(listener.Id, piece, db, enactor.Id, captures);
                foreach (var note in subCtx.TargetedNotifications) context.TargetedNotifications.Add(note);
                if (listener.Id == enactor.Id)
                {
                    foreach (var note in subCtx.Notifications) context.Notifications.Add(note);
                }
                else
                {
                    foreach (var note in subCtx.Notifications) context.NotifyTarget(listener.Id, note);
                }
            }
            else if (_evaluator != null)
            {
                var evalCtx = new EvaluationContext(listener.Id, enactor.Id, location: listener.Location, args: captures);
                string evaluated = _evaluator.Evaluate(piece, evalCtx);
                if (!string.IsNullOrWhiteSpace(evaluated))
                {
                    context.Notify(evaluated);
                }
            }
            else
            {
                context.Notify(piece);
            }
        }
    }

    private bool CheckRestrictions(MUSHObject executor, string canonicalName, CommandContext context)
    {
        if (!_restrictions.TryGetValue(canonicalName, out var list))
            return true;

        foreach (var (rule, customError) in list)
        {
            string r = rule.ToLowerInvariant();
            if (r == "nobody")
            {
                context.Notify(customError ?? Feedback.CommandDisabled.Text());
                return false;
            }
            if (r == "noguest" && executor.HasFlag("GUEST"))
            {
                context.Notify(customError ?? Feedback.GuestsAreNotAllowedToDoThat.Text());
                return false;
            }
            if (r == "nofixed" && executor.HasFlag("FIXED"))
            {
                context.Notify(customError ?? Feedback.YouCantDoThatWhileFixed.Text());
                return false;
            }
            if (r == "nogagged" && executor.HasFlag("GAGGED"))
            {
                context.Notify(customError ?? Feedback.YouCannotDoThatWhileGagged.Text());
                return false;
            }
            if (r == "god" && !executor.IsGod)
            {
                context.Notify(customError ?? Feedback.OnlyGodCanDoThat.Text());
                return false;
            }
            if (r == "wizard" && !executor.IsWizard)
            {
                context.Notify(customError ?? Feedback.PermissionDenied.Text());
                return false;
            }
            if (r == "admin" && !(executor.IsWizard || executor.IsRoyalty))
            {
                context.Notify(customError ?? Feedback.PermissionDenied.Text());
                return false;
            }
        }

        return true;
    }

    public static CommandContext Parse(Dbref executor, string input, Dbref? enactor = null)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new CommandContext { Executor = executor, Enactor = enactor ?? executor };

        string trimmed = input.TrimStart();
        string cmdToken;
        string args = string.Empty;

        // Shortcut prefixes: " for say, : for pose, ; for semipose, \ for @emit
        if (trimmed.StartsWith('\"') || trimmed.StartsWith('\''))
        {
            cmdToken = "say";
            args = trimmed.Substring(1);
        }
        else if (trimmed.StartsWith('\\'))
        {
            cmdToken = "@emit";
            args = trimmed.Substring(1);
        }
        else if (trimmed.StartsWith(':'))
        {
            cmdToken = "pose";
            args = trimmed.Substring(1);
        }
        else if (trimmed.StartsWith(';'))
        {
            cmdToken = ";";
            args = trimmed.Substring(1);
        }
        else
        {
            int firstSpace = trimmed.IndexOf(' ');
            if (firstSpace == -1)
            {
                cmdToken = trimmed;
            }
            else
            {
                cmdToken = trimmed.Substring(0, firstSpace);
                args = trimmed.Substring(firstSpace + 1).TrimStart();
            }
        }

        var switches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string cmdName = cmdToken;
        if (cmdToken.Contains('/'))
        {
            var parts = cmdToken.Split('/');
            cmdName = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                if (!string.IsNullOrEmpty(parts[i]))
                    switches.Add(parts[i]);
            }
        }

        string left = args;
        string right = string.Empty;
        int eqIndex = FindUnescapedEquals(args);
        if (eqIndex != -1)
        {
            left = args.Substring(0, eqIndex).Replace(@"\=", "=").Trim();
            right = args.Substring(eqIndex + 1).Trim();
        }

        return new CommandContext
        {
            Executor = executor,
            Enactor = enactor ?? executor,
            RawCommand = input,
            CommandName = cmdName,
            Switches = switches,
            Arguments = args,
            LeftArg = left,
            RightArg = right
        };
    }

    private static int FindUnescapedEquals(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 1 < s.Length)
            {
                i++;
                continue;
            }
            if (s[i] == '=')
                return i;
        }
        return -1;
    }
}
