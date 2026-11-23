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
        string bound = BindQueuedCommand(executor, enactor, input, args, registers, db);
        if (string.IsNullOrWhiteSpace(bound))
            return new CommandContext { Executor = executor, Enactor = enactor };
        var context = Parse(executor, bound, enactor);
        if (registers != null)
        {
            foreach (var register in registers)
                context.Registers[register.Key] = register.Value;
        }

        return Run(context, db, true);
    }

    private string BindQueuedCommand(Dbref executor, Dbref enactor, string input, string[]? args, IReadOnlyDictionary<string, string>? registers, ObjectDatabase db)
    {
        if (_evaluator == null)
            return input;
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

        return _evaluator.Evaluate(input, ctx);
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
            var goLookup = _commandTable.Lookup("go");
            if (exitObj != null && goLookup.Status == CommandLookupStatus.Found && goLookup.Handler != null)
            {
                var goCtx = new CommandContext
                {
                    Executor = executor,
                    Enactor = executor,
                    RawCommand = input,
                    CommandName = "go",
                    Arguments = context.CommandName,
                    LeftArg = context.CommandName
                };
                goLookup.Handler.Execute(goCtx, db);
                foreach (var note in goCtx.TargetedNotifications)
                    context.TargetedNotifications.Add(note);
                foreach (var note in goCtx.Notifications)
                    context.Notifications.Add(note);
                return context;
            }
        }

        // 2. Check $command:action listeners (local room, inventory, self, Master Room)
        if (execObj != null && TryMatchCommandListener(execObj, input, db, context))
        {
            return context;
        }

        // 3. Check channel prefix (+channel message or +channel/who)
        if (context.CommandName.StartsWith('+') && context.CommandName.Length > 1)
        {
            string chName = context.CommandName.Substring(1);
            var chanLookup = _commandTable.Lookup("@channel");
            var chatLookup = _commandTable.Lookup("@chat");

            if (context.Switches.Count > 0 && chanLookup.Status == CommandLookupStatus.Found && chanLookup.Handler != null)
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
            else if (chatLookup.Status == CommandLookupStatus.Found && chatLookup.Handler != null)
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

        // 4. Unrecognized command
        context.Notify(Feedback.HuhTypeHelpForHelp);
        return context;
    }

    private bool TryMatchCommandListener(MUSHObject executor, string input, ObjectDatabase db, CommandContext context)
    {
        var candidates = new List<MUSHObject>();
        var seen = new HashSet<Dbref>();

        void AddCandidate(MUSHObject? obj)
        {
            if (obj != null && seen.Add(obj.Id))
                candidates.Add(obj);
        }

        // Current room
        if (executor.Location.IsValid)
        {
            AddCandidate(db.GetObject(executor.Location));

            // Objects in room
            foreach (var item in db.GetContents(executor.Location))
            {
                if (item.Id != executor.Id)
                    AddCandidate(item);
            }
        }

        // Executor's inventory
        foreach (var item in db.GetContents(executor.Id))
            AddCandidate(item);

        // Self
        AddCandidate(executor);

        // Master Room (#2) globals
        var masterRoom = new Dbref(2);
        foreach (var item in db.GetContents(masterRoom))
        {
            AddCandidate(item);
        }

        foreach (var candidate in candidates)
        {
            if (candidate.HasFlag("NO_COMMAND") || candidate.HasFlag("HALT"))
                continue;

            foreach (var listener in candidate.Listeners)
            {
                if (!listener.Command)
                    continue;
                if (PatternMatcher.Match(listener.Pattern, input, out var captures))
                {
                    ExecuteListenerAction(candidate, executor, listener.Action, captures, db, context);
                    return true;
                }
            }
        }

        return false;
    }

    private void ExecuteListenerAction(MUSHObject listener, MUSHObject enactor, string action, string[] captures, ObjectDatabase db, CommandContext context)
    {
        foreach (string piece in Handlers.IncludeCommandHandler.SplitCommands(action))
        {
            string trimmed = piece.TrimStart();
            if (trimmed.StartsWith('@') || trimmed.StartsWith('"') || trimmed.StartsWith(':') || trimmed.StartsWith(';') || trimmed.StartsWith('&'))
            {
                var subCtx = DispatchQueued(listener.Id, piece, db, enactor.Id, captures);
                foreach (var note in subCtx.TargetedNotifications) context.TargetedNotifications.Add(note);
                foreach (var note in subCtx.Notifications) context.Notifications.Add(note);
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
        int eqIndex = args.IndexOf('=');
        if (eqIndex != -1)
        {
            left = args.Substring(0, eqIndex).Trim();
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
}
