namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Communication;
using PennMUSH.Core.Database;

public class MotdCommandHandler : ICommandHandler
{
    private readonly MotdBoard _motd;

    public string Name => "@motd";

    public MotdCommandHandler(MotdBoard motd) => _motd = motd;

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        var alias = AliasOf(context.CommandName);
        if ((alias == MotdAlias.Wizard || alias == MotdAlias.Reject) && !actor.IsWizard)
        {
            context.Notify("Permission denied.");
            return false;
        }

        bool list = alias == MotdAlias.List || context.Switches.Contains("list");
        bool clear = context.Switches.Contains("clear");
        if (list || (context.Arguments.Length == 0 && !clear))
        {
            Report(context, _motd, list && actor.IsRoyalty);
            return true;
        }

        var kind = KindOf(alias);
        if (context.Switches.Contains("wizard"))
            kind = MotdKind.Wizard;
        else if (context.Switches.Contains("down"))
            kind = MotdKind.Down;
        else if (context.Switches.Contains("full"))
            kind = MotdKind.Full;

        bool allowed = kind == MotdKind.Connect
            ? actor.IsWizard || actor.HasPower("Announce")
            : actor.IsRoyalty;
        if (!allowed)
        {
            context.Notify("You may get 15 minutes of fame and glory in life, but not right now.");
            return false;
        }

        _motd.Set(kind, clear ? "" : context.Arguments);
        context.Notify($"{Label(kind)} {(clear ? "cleared" : "set")}.");
        return true;
    }

    public static void Report(CommandContext context, MotdBoard motd, bool includePrivileged)
    {
        context.Notify($"MOTD: {motd.Connect}");
        if (!includePrivileged)
            return;

        context.Notify($"Wiz MOTD: {motd.Wizard}");
        context.Notify($"Down MOTD: {motd.Down}");
        context.Notify($"Full MOTD: {motd.Full}");
    }

    private static MotdKind KindOf(MotdAlias alias) => alias switch
    {
        MotdAlias.Wizard => MotdKind.Wizard,
        MotdAlias.Reject => MotdKind.Down,
        _ => MotdKind.Connect
    };

    private static string Label(MotdKind kind) => kind switch
    {
        MotdKind.Wizard => "Wizard motd",
        MotdKind.Down => "Down motd",
        MotdKind.Full => "Full motd",
        _ => "Motd"
    };

    private static MotdAlias AliasOf(string typed)
    {
        string? match = null;
        foreach (var name in new[] { "@listmotd", "@rejectmotd", "@wizmotd", "@motd" })
        {
            if (!name.StartsWith(typed, StringComparison.OrdinalIgnoreCase))
                continue;
            if (match != null)
                return MotdAlias.Motd;
            match = name;
        }

        return match switch
        {
            "@listmotd" => MotdAlias.List,
            "@rejectmotd" => MotdAlias.Reject,
            "@wizmotd" => MotdAlias.Wizard,
            _ => MotdAlias.Motd
        };
    }

    private enum MotdAlias
    {
        Motd,
        List,
        Wizard,
        Reject
    }
}
