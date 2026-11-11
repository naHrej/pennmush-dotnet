namespace PennMUSH.Core.Commands.Handlers;

using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class SayCommandHandler : ICommandHandler
{
    private readonly ExpressionEvaluator? _evaluator;

    public string Name => "say";

    public SayCommandHandler(ExpressionEvaluator? evaluator = null)
    {
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrEmpty(context.Arguments))
        {
            context.Notify("Say what?");
            return false;
        }

        string message = context.Arguments;
        if (_evaluator != null && !context.Switches.Contains("noeval"))
        {
            var speaker = db.GetObject(context.Executor);
            var evalCtx = new EvaluationContext(
                context.Executor,
                context.Enactor,
                location: speaker?.Location ?? Dbref.Nothing);
            message = _evaluator.Evaluate(message, evalCtx);
        }

        context.Notify($"You say, \"{message}\"");

        var execObj = db.GetObject(context.Executor);
        if (execObj != null && execObj.Location.IsValid)
        {
            string name = execObj.Name;
            context.NotifyExcept(execObj.Location, context.Executor, $"{name} says, \"{message}\"");
        }

        return true;
    }
}
