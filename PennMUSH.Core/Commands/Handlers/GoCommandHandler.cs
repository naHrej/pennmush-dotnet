namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Locks;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class GoCommandHandler : ICommandHandler
{
    private readonly LookCommandHandler? _lookHandler;
    private readonly ExpressionEvaluator? _evaluator;
    private readonly CommandDispatcher? _dispatcher;
    private readonly Func<Dbref, bool>? _connected;

    public string Name => "go";

    public GoCommandHandler(LookCommandHandler? lookHandler = null, ExpressionEvaluator? evaluator = null, CommandDispatcher? dispatcher = null, Func<Dbref, bool>? connected = null)
    {
        _lookHandler = lookHandler;
        _evaluator = evaluator;
        _dispatcher = dispatcher;
        _connected = connected;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify(Feedback.WhereDoYouWantToGo);
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify(Feedback.YouAreNowhere);
            return false;
        }

        string exitName = context.Arguments.Trim();
        var exitObj = db.FindExit(execObj.Location, exitName);

        // Fallback: check Zone Master Room exits, then Master Room (#2) exits
        var here = db.GetObject(execObj.Location);
        if (exitObj == null && here != null && here.Zone.IsValid)
        {
            var zoneObj = db.GetObject(here.Zone);
            if (zoneObj != null && zoneObj.Type == ObjectType.Room)
                exitObj = db.FindExit(zoneObj.Id, exitName);
        }
        if (exitObj == null && execObj.Location != new Dbref(2))
        {
            exitObj = db.FindExit(new Dbref(2), exitName);
        }

        // Fallback: check dbref directly
        if (exitObj == null && exitName.StartsWith('#') && int.TryParse(exitName.Substring(1), out int exitId))
        {
            var cand = db.GetObject(new Dbref(exitId));
            if (cand != null && cand.Type == ObjectType.Exit && cand.Exits == execObj.Location)
                exitObj = cand;
        }

        if (exitObj == null)
        {
            context.Notify(Feedback.AlasYouCannotGoThatWay);
            return false;
        }

        // Check destination
        Dbref destRef = exitObj.Location;
        var destObj = db.GetObject(destRef);
        if (destObj == null)
        {
            context.Notify(Feedback.ThatExitLeadsNowhere);
            return false;
        }

        if (here != null && !here.EvalLock(LockType.Leave, context.Executor, db))
        {
            context.Notify(Feedback.YouCantGoThatWay);
            return false;
        }

        // A stored basic lock is the exit key. The LOCK attribute remains for keys
        // that were written before locks were stored on the object.
        bool locked = false;
        if (exitObj.GetLock(LockType.Basic) != null)
        {
            locked = !exitObj.EvalLock(LockType.Basic, context.Executor, db);
        }
        else
        {
            var lockAttr = exitObj.GetAttribute("LOCK");
            if (lockAttr != null && !string.IsNullOrWhiteSpace(lockAttr.Value))
            {
                var lockNode = LockParser.Parse(lockAttr.Value);
                locked = !lockNode.Evaluate(context.Executor, exitObj.Id, db);
            }
        }

        if (locked)
        {
            var lockCtx = new EvaluationContext(context.Executor, context.Enactor, location: execObj.Location);
            var failAttr = exitObj.GetAttribute("FAIL");
            if (failAttr != null && !string.IsNullOrWhiteSpace(failAttr.Value))
                context.Notify(_evaluator != null ? _evaluator.Evaluate(failAttr.Value, lockCtx) : failAttr.Value);
            else
                context.Notify(Feedback.YouCantGoThatWay);
            return false;
        }

        // Traversal success hooks
        var srcRef = execObj.Location;
        var moveCtx = new EvaluationContext(context.Executor, context.Enactor, location: destRef);
        var succAttr = exitObj.GetAttribute("SUCC");
        if (succAttr != null && !string.IsNullOrWhiteSpace(succAttr.Value))
        {
            context.Notify(_evaluator != null ? _evaluator.Evaluate(succAttr.Value, moveCtx) : succAttr.Value);
        }

        // Notify source room of departure
        var osuccAttr = exitObj.GetAttribute("OSUCC");
        string leaveMsg = osuccAttr != null && !string.IsNullOrWhiteSpace(osuccAttr.Value)
            ? (_evaluator != null ? _evaluator.Evaluate(osuccAttr.Value, moveCtx) : osuccAttr.Value)
            : $"{execObj.Name} leaves.";
        context.NotifyExcept(srcRef, context.Executor, leaveMsg);

        // Move the player
        db.MoveObject(context.Executor, destRef);

        // Notify destination room of arrival
        var odropAttr = exitObj.GetAttribute("ODROP");
        string arriveMsg = odropAttr != null && !string.IsNullOrWhiteSpace(odropAttr.Value)
            ? (_evaluator != null ? _evaluator.Evaluate(odropAttr.Value, moveCtx) : odropAttr.Value)
            : $"{execObj.Name} has arrived.";
        context.NotifyExcept(destRef, context.Executor, arriveMsg);

        var dropAttr = exitObj.GetAttribute("DROP");
        if (dropAttr != null && !string.IsNullOrWhiteSpace(dropAttr.Value))
        {
            context.Notify(_evaluator != null ? _evaluator.Evaluate(dropAttr.Value, moveCtx) : dropAttr.Value);
        }

        // Arrival look. A typed look and this automatic look share one room description.
        if (_lookHandler != null)
            _lookHandler.ShowHere(context, db, context.Executor, LookKind.Automatic);
        else
            context.Notify(destObj.Name);

        FollowerMovement.Bring(context, db, _dispatcher, _connected, srcRef, $"go #{exitObj.Id.Value}");
        return true;
    }
}
