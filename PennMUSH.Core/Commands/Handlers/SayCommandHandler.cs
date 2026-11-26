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
            context.Notify(Feedback.SayWhat);
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

        message = SpeechMod.Apply(db, _evaluator, context.Executor, message, "\"");

        var execObj = db.GetObject(context.Executor);
        string name = execObj != null ? execObj.Name : context.Executor.ToString();
        string selfLine = Feedback.YouSayValue.Text(("value", $"{message}"));
        string roomLine = Feedback.ValueSaysValue.Text(("value", $"{name}"), ("value2", $"{message}"));

        string? mogrified = FormatAttributes.MogrifySpeech(db, _evaluator, execObj, "\"", message, roomLine, selfLine);
        if (mogrified != null)
        {
            if (string.IsNullOrEmpty(mogrified))
                return true;

            if (execObj != null && execObj.Location.IsValid)
                context.NotifyRoom(execObj.Location, mogrified);
            else
                context.Notify(mogrified);
            return true;
        }

        context.Notify(selfLine);

        if (execObj != null && execObj.Location.IsValid)
        {
            context.NotifyExcept(execObj.Location, context.Executor, roomLine);
        }

        return true;
    }
}
