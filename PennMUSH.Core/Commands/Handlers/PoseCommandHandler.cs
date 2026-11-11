namespace PennMUSH.Core.Commands.Handlers;

using PennMUSH.Core.Database;

public class PoseCommandHandler : ICommandHandler
{
    public string Name => "pose";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrEmpty(context.Arguments))
        {
            context.Notify("Pose what?");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        string name = execObj != null ? execObj.Name : context.Executor.ToString();

        bool noSpace = context.CommandName.Equals(";", StringComparison.Ordinal)
            || context.CommandName.Equals("semipose", StringComparison.OrdinalIgnoreCase)
            || context.Switches.Contains("nospace");
        string message = noSpace ? $"{name}{context.Arguments}" : $"{name} {context.Arguments}";

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
