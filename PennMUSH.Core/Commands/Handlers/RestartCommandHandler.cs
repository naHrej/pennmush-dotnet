namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Linq;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class RestartCommandHandler : ICommandHandler
{
    private readonly ActionQueue _queue;

    public string Name => "@restart";

    public RestartCommandHandler(ActionQueue queue)
    {
        _queue = queue;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        if (Has(context, "all"))
            return RestartAll(context, db, actor);

        string name = context.LeftArg.Trim();
        if (name.Length == 0)
        {
            Halt(context, db, actor);
            QueueOwned(db, actor.Id);
            return true;
        }

        var victim = db.MatchObject(context.Executor, name);
        if (victim == null || victim.Type == ObjectType.Garbage)
        {
            context.Notify("I can't see that here.");
            return false;
        }

        if (actor.Owner != victim.Owner && !CanHaltAny(actor))
        {
            context.Notify("Permission denied.");
            return false;
        }

        Announce(context, db, actor, victim);
        Halt(context, db, victim);
        if (victim.Type == ObjectType.Player)
            QueueOwned(db, victim.Id);
        else
            QueueStartup(db, victim);
        return true;
    }

    private bool RestartAll(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        if (!CanHaltAny(actor))
        {
            context.Notify("You do not have the power to restart the world.");
            return false;
        }

        foreach (var player in db.GetAllObjects().Where(obj => obj.Type == ObjectType.Player).ToList())
        {
            context.NotifyTarget(player.Id, $"Your objects have been globally halted by {actor.Name}");
            Halt(context, db, player);
        }

        foreach (var obj in db.GetAllObjects())
            QueueStartup(db, obj);

        foreach (var player in db.GetAllObjects().Where(obj => obj.Type == ObjectType.Player))
            context.NotifyTarget(player.Id, $"Your objects are being globally restarted by {actor.Name}");
        return true;
    }

    private static void Announce(CommandContext context, ObjectDatabase db, MUSHObject actor, MUSHObject victim)
    {
        if (victim.Owner != actor.Id)
        {
            if (victim.Type == ObjectType.Player)
            {
                context.Notify($"All objects for {victim.Name} are being restarted.");
                context.NotifyTarget(victim.Id, $"All of your objects are being restarted by {actor.Name}.");
            }
            else
            {
                string ownerName = db.GetObject(victim.Owner)?.Name ?? victim.Owner.ToString();
                context.Notify($"Restarting: {ownerName}'s {victim.Name}(#{victim.Id.Value})");
                context.NotifyTarget(victim.Owner, $"Restarting: {victim.Name}(#{victim.Id.Value}), by {actor.Name}");
            }
            return;
        }

        if (victim.Id == actor.Id)
            context.Notify("All of your objects are being restarted.");
        else
            context.Notify($"Restarting: {victim.Name}(#{victim.Id.Value})");
    }

    private void Halt(CommandContext context, ObjectDatabase db, MUSHObject victim)
    {
        _queue.HaltWhere(executor =>
        {
            if (executor == victim.Id)
                return true;
            var obj = db.GetObject(executor);
            return obj != null && obj.Owner == victim.Id;
        });

        var owner = db.GetObject(victim.Owner);
        if (owner != null && !owner.HasFlag("QUIET"))
            context.NotifyTarget(owner.Id, $"Halted: {victim.Name}(#{victim.Id.Value})");
    }

    private void QueueOwned(ObjectDatabase db, Dbref owner)
    {
        foreach (var obj in db.GetAllObjects())
        {
            if (obj.Owner == owner)
                QueueStartup(db, obj);
        }
    }

    private void QueueStartup(ObjectDatabase db, MUSHObject obj)
    {
        if (obj.Type == ObjectType.Garbage || obj.HasFlag("HALT"))
            return;
        var startup = obj.GetAttribute("STARTUP");
        if (startup == null || string.IsNullOrWhiteSpace(startup.Value))
            return;
        foreach (string command in IncludeCommandHandler.SplitCommands(startup.Value))
            _queue.Enqueue(obj.Id, obj.Id, command);
    }

    private static bool CanHaltAny(MUSHObject actor)
        => actor.IsWizard || actor.HasPower("Halt");

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
}
