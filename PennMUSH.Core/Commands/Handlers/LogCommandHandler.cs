namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Flags;
using PennMUSH.Core.Logging;
using PennMUSH.Core.Types;

public class LogCommandHandler : ICommandHandler
{
    private readonly WizardLog _log;

    public string Name => "@log";

    public LogCommandHandler(WizardLog log)
    {
        _log = log;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.IsWizard)
        {
            context.Notify("Permission denied.");
            return false;
        }

        WizardLogKind kind = LogSwitches.Kind(context, WizardLogKind.Cmd);
        if (LogSwitches.Has(context, "recall"))
        {
            int.TryParse(LogSwitches.Rest(context), out int lines);
            context.Notify("Begin log recall.");
            foreach (string line in _log.Recall(kind, lines))
                context.Notify(line);
            context.Notify("End log recall.");
            return true;
        }

        _log.Append(kind, $"LOG: {actor.Name}(#{actor.Id.Value}{LogSwitches.FlagText(actor)}): {LogSwitches.Rest(context)}");
        context.Notify("Logged.");
        return true;
    }
}

public class LogWipeCommandHandler : ICommandHandler
{
    private readonly WizardLog _log;

    public string Name => "@logwipe";

    public LogWipeCommandHandler(WizardLog log)
    {
        _log = log;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.IsGod)
        {
            context.Notify("Only God can do that.");
            return false;
        }

        WizardLogKind kind = LogSwitches.Kind(context, WizardLogKind.Err);
        string pass = LogSwitches.Rest(context);
        string who = $"{actor.Name}(#{actor.Id.Value}{LogSwitches.FlagText(actor)})";
        if (pass != WizardLog.WipePassword)
        {
            _log.Append(WizardLogKind.Wiz, $"WIZ: {who}: Invalid attempt to wipe the {WizardLog.NameOf(kind)} log, password '{pass}'");
            context.Notify("Wrong password.");
            return false;
        }

        if (LogSwitches.Has(context, "rotate") && !LogSwitches.Has(context, "wipe"))
            _log.Rotate(kind);
        else if (LogSwitches.Has(context, "trim") && !LogSwitches.Has(context, "wipe"))
            _log.Trim(kind);
        else
            _log.Wipe(kind);

        _log.Append(WizardLogKind.Err, $"RPT: {WizardLog.NameOf(kind)} log wiped.");
        context.Notify("Log wiped.");
        return true;
    }
}

internal static class LogSwitches
{
    private static readonly string[] Names = ["check", "cmd", "conn", "err", "trace", "wiz", "recall", "wipe", "rotate", "trim"];

    public static bool Has(CommandContext context, string name)
    {
        if (context.Switches.Contains(name))
            return true;
        foreach (string token in context.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Equals("/" + name, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public static WizardLogKind Kind(CommandContext context, WizardLogKind fallback)
    {
        if (Has(context, "check")) return WizardLogKind.Check;
        if (Has(context, "cmd")) return WizardLogKind.Cmd;
        if (Has(context, "conn")) return WizardLogKind.Conn;
        if (Has(context, "err")) return WizardLogKind.Err;
        if (Has(context, "trace")) return WizardLogKind.Trace;
        if (Has(context, "wiz")) return WizardLogKind.Wiz;
        return fallback;
    }

    public static string Rest(CommandContext context)
    {
        var tokens = new List<string>();
        foreach (string token in context.LeftArg.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (tokens.Count == 0 && IsSwitch(token))
                continue;
            tokens.Add(token);
        }
        return string.Join(' ', tokens);
    }

    public static string FlagText(MUSHObject actor)
    {
        var text = new StringBuilder();
        text.Append(actor.Type switch
        {
            ObjectType.Player => 'P',
            ObjectType.Room => 'R',
            ObjectType.Thing => 'T',
            ObjectType.Exit => 'E',
            ObjectType.Garbage => 'g',
            _ => '?'
        });
        foreach (string flagName in actor.Flags.OrderBy(name => name, StringComparer.Ordinal))
        {
            var def = FlagTable.FindFlag(flagName);
            if (def != null && def.Letter != '\0')
                text.Append(def.Letter);
        }
        return text.ToString();
    }

    private static bool IsSwitch(string token)
    {
        if (!token.StartsWith('/'))
            return false;
        foreach (string name in Names)
        {
            if (token.Equals("/" + name, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
