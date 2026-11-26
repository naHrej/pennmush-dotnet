namespace PennMUSH.Core.Commands.Handlers;

using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;

public class PoseCommandHandler : ICommandHandler
{
    private readonly ExpressionEvaluator? _evaluator;

    public string Name => "pose";

    public PoseCommandHandler(ExpressionEvaluator? evaluator = null)
    {
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrEmpty(context.Arguments))
        {
            context.Notify(Feedback.PoseWhat);
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        string name = execObj != null ? execObj.Name : context.Executor.ToString();

        bool noSpace = context.CommandName.Equals(";", StringComparison.Ordinal)
            || context.CommandName.Equals("semipose", StringComparison.OrdinalIgnoreCase)
            || context.Switches.Contains("nospace");
        string token = noSpace ? ";" : ":";
        string spoken = SpeechMod.Apply(db, _evaluator, context.Executor, context.Arguments, token);
        string message = noSpace ? $"{name}{spoken}" : $"{name} {spoken}";

        string? mogrified = FormatAttributes.MogrifySpeech(db, _evaluator, execObj, token, spoken, message, message);
        if (mogrified != null)
        {
            if (string.IsNullOrEmpty(mogrified))
                return true;
            message = mogrified;
        }

        if (execObj != null && execObj.Location.IsValid)
        {
            context.NotifyRoom(execObj.Location, message);
        }
        else
        {
            context.Notify(message);
        }

        return true;
    }
}
