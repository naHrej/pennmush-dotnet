namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Linq;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Locks;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class UseCommandHandler : ICommandHandler
{
    private readonly ActionQueue _queue;
    private readonly ExpressionEvaluator? _evaluator;

    public string Name => "use";

    public UseCommandHandler(ActionQueue queue, ExpressionEvaluator? evaluator = null)
    {
        _queue = queue;
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify("Use what?");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify("You are nowhere.");
            return false;
        }

        string name = context.Arguments.Trim();
        var thing = db.FindInContents(execObj.Location, name) ?? db.FindInContents(context.Executor, name);
        if (thing == null || thing.Type != ObjectType.Thing)
        {
            context.Notify("I don't see that here.");
            return false;
        }

        if (!thing.EvalLock(LockType.Use, context.Executor, db))
        {
            context.Notify(Text(thing, "FAILURE", context, execObj.Location) ?? "Permission denied.");
            return false;
        }

        context.Notify(Text(thing, "USE", context, execObj.Location) ?? "Used.");
        var others = Text(thing, "OUSE", context, execObj.Location);
        if (others != null)
            context.NotifyExcept(execObj.Location, context.Executor, others);

        var action = thing.GetAttribute("AUSE")?.Value;
        if (string.IsNullOrWhiteSpace(action))
            action = thing.GetAttribute("RUNOUT")?.Value;
        if (!string.IsNullOrWhiteSpace(action))
            _queue.Enqueue(context.Executor, context.Enactor, action);

        return true;
    }

    private string? Text(MUSHObject thing, string attribute, CommandContext context, Dbref location)
    {
        var entry = thing.GetAttribute(attribute);
        if (entry == null || string.IsNullOrWhiteSpace(entry.Value))
            return null;
        if (_evaluator == null)
            return entry.Value;
        var eval = new EvaluationContext(context.Executor, context.Enactor, location: location);
        return _evaluator.Evaluate(entry.Value, eval);
    }
}

public class EmptyCommandHandler : ICommandHandler
{
    public string Name => "empty";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify("Empty what?");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify("You are nowhere.");
            return false;
        }

        string name = context.Arguments.Trim();
        MUSHObject? target;
        if (name.Equals("me", StringComparison.OrdinalIgnoreCase))
            target = execObj;
        else
            target = db.FindInContents(execObj.Location, name) ?? db.FindInContents(context.Executor, name);

        if (target == null || (target.Type != ObjectType.Thing && target.Type != ObjectType.Player))
        {
            context.Notify("I don't see that here.");
            return false;
        }

        bool held = target.Id == context.Executor || target.Location == context.Executor;
        bool here = target.Location == execObj.Location;
        if (!held && !here)
        {
            context.Notify("You can't empty that from here.");
            return false;
        }

        bool controls = execObj.IsWizard || target.Owner == context.Executor || target.Id == context.Executor;
        if (!controls && !target.EvalLock(LockType.Enter, context.Executor, db))
        {
            context.Notify("Permission denied.");
            return false;
        }

        var room = db.GetObject(execObj.Location);
        int removed = 0;
        foreach (var item in db.GetContents(target.Id).Where(item => item.Type == ObjectType.Thing).ToList())
        {
            if (target.Id == context.Executor)
            {
                if (!item.EvalLock(LockType.Drop, context.Executor, db))
                    continue;
                if (room != null && room.Type == ObjectType.Room && !room.EvalLock(LockType.Drop, context.Executor, db))
                    continue;
                db.MoveObject(item.Id, execObj.Location);
            }
            else if (held)
            {
                if (!item.EvalLock(LockType.Basic, context.Executor, db))
                    continue;
                db.MoveObject(item.Id, context.Executor);
            }
            else
            {
                if (!item.EvalLock(LockType.Basic, context.Executor, db))
                    continue;
                if (!item.EvalLock(LockType.Drop, context.Executor, db))
                    continue;
                if (room != null && room.Type == ObjectType.Room && !room.EvalLock(LockType.Drop, context.Executor, db))
                    continue;
                db.MoveObject(item.Id, execObj.Location);
            }

            removed++;
        }

        string noun = removed == 1 ? "object" : "objects";
        string count = removed == 0 ? "nothing" : $"{removed} {noun}";
        context.Notify($"You remove {count} from {target.Name}.");
        return removed > 0;
    }
}

public class TeachCommandHandler : ICommandHandler
{
    private readonly ActionQueue _queue;

    public string Name => "teach";

    public TeachCommandHandler(ActionQueue queue) => _queue = queue;

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        string command = context.Arguments.Trim();
        if (command.Length == 0)
        {
            context.Notify("What command do you want to teach?");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify("You are nowhere.");
            return false;
        }

        var here = db.GetObject(execObj.Location);
        if (here != null && !here.EvalLock(LockType.Speech, context.Executor, db))
        {
            context.Notify("Permission denied.");
            return false;
        }

        context.NotifyRoom(execObj.Location, $"{execObj.Name} types --> {command}");
        if (_queue.Enqueue(context.Executor, context.Enactor, command) == null)
        {
            context.Notify("Queue is full.");
            return false;
        }

        return true;
    }
}
