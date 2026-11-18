namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;

public class ShutdownCommandHandler : ICommandHandler
{
    private readonly Action _save;
    private readonly Action _stop;
    private readonly Action _reboot;

    public string Name => "@shutdown";

    public ShutdownCommandHandler(Action save, Action stop, Action reboot)
    {
        _save = save;
        _stop = stop;
        _reboot = reboot;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.IsWizard)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        if (Has(context, "reboot"))
            return Reboot(context, db, actor);

        if (Has(context, "panic") && !actor.IsGod)
        {
            context.Notify(Feedback.ItTakesAGodToMakeMePanic);
            return false;
        }

        string name = AnnouncedName(db, actor);
        if (!TrySave())
            context.NotifyBroadcast(Feedback.GameErrorDatabaseSaveFailed.Text(), wizardOnly: true);
        context.NotifyBroadcast(Feedback.GameShutdownBy.Text(("value", $"{name}")));
        if (Has(context, "panic"))
            context.NotifyBroadcast(Feedback.EmergencyShutdownShutdownPanic.Text());
        _stop();
        return true;
    }

    private bool Reboot(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        if (!TrySave())
        {
            context.NotifyBroadcast(Feedback.GameRebootFailed.Text());
            return false;
        }

        context.NotifyBroadcast($"GAME: Reboot w/o disconnect by {AnnouncedName(db, actor)}, please wait.");
        _reboot();
        return true;
    }

    private bool TrySave()
    {
        try
        {
            _save();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool Has(CommandContext context, string name)
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

    private static string AnnouncedName(ObjectDatabase db, MUSHObject actor)
    {
        var owner = db.GetObject(actor.Owner);
        return (owner ?? actor).Name;
    }
}
