namespace PennMUSH.Core.Commands.Handlers;

using PennMUSH.Core.Database;
using PennMUSH.Core.Types;

public class CreateCommandHandler : ICommandHandler
{
    public string Name => "@create";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        string name = string.IsNullOrWhiteSpace(context.LeftArg) ? "Thing" : context.LeftArg;
        var obj = db.CreateObject(name, ObjectType.Thing, context.Executor);

        if (!context.Switches.Contains("silent"))
            context.Notify(Feedback.CreatedValueValue, ("value", $"{obj.Name}"), ("value2", $"{obj.Id}"));

        return true;
    }
}