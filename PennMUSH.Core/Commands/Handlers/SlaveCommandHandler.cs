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
            context.Notify("Permission denied.");
            return false;
        }

        if (context.Switches.Contains("restart"))
        {
            context.Notify("No such service.");
            return false;
        }

        context.Notify("I'm sorry, Dave, I'm afraid I can't do that.");
        return true;
    }
}
