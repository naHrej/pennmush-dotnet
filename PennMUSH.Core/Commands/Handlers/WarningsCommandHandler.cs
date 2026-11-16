namespace PennMUSH.Core.Commands.Handlers;

using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class WarningsCommandHandler : ICommandHandler
{
    public string Name => "@warnings";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        MUSHObject? actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        string name = context.LeftArg.Trim();
        if (name.Length == 0)
        {
            context.Notify("I don't see that object.");
            return false;
        }

        MUSHObject? thing = db.MatchObject(context.Executor, name);
        if (thing == null)
        {
            context.Notify("I don't see that object.");
            return false;
        }

        if (!CopyAttributeCommandHandler.Controls(actor, thing))
        {
            context.Notify("Permission denied.");
            return false;
        }

        if (thing.Type == ObjectType.Garbage || thing.HasFlag("GOING"))
        {
            context.Notify("Why would you want to be warned about garbage?");
            return false;
        }

        uint old = thing.Warnings;
        uint next = WarningBits.Parse(context.RightArg, unknown => context.Notify($"Unknown warning: {unknown}"));
        if (next != old)
        {
            thing.Warnings = next;
            if (next != 0)
                context.Notify($"@warnings set to: {WarningBits.Unparse(next)}");
            else
                context.Notify("@warnings cleared.");
        }
        else
        {
            context.Notify("@warnings not changed.");
        }

        return true;
    }
}
