namespace PennMUSH.Core.Commands.Handlers;

using PennMUSH.Core.Database;

public class SlaveCommandHandler : ICommandHandler
{
    public string Name => "@slave";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.IsWizard)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        if (context.Switches.Contains("restart"))
        {
            context.Notify(Feedback.NoSuchService);
            return false;
        }

        context.Notify(Feedback.ImSorryDaveImAfraidICantDoThat);
        return true;
    }
}
