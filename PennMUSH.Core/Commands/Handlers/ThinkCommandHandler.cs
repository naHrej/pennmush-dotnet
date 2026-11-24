namespace PennMUSH.Core.Commands.Handlers;

using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;

public class ThinkCommandHandler : ICommandHandler
{
    private readonly ExpressionEvaluator? _evaluator;

    public string Name => "think";

    public ThinkCommandHandler(ExpressionEvaluator? evaluator = null)
    {
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        string input = context.Arguments;
        if (_evaluator != null)
        {
            var evalCtx = new EvaluationContext(context.Executor, context.Enactor);
            string evaluated = _evaluator.Evaluate(input, evalCtx);
            if (evaluated.Length > 0)
                context.Notify(evaluated);
        }
        else
        {
            context.Notify(input);
        }
        return true;
    }
}