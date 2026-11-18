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
            context.Notify(Feedback.IDontSeeThatObject);
            return false;
        }

        MUSHObject? thing = db.MatchObject(context.Executor, name);
        if (thing == null)
        {
            context.Notify(Feedback.IDontSeeThatObject);
            return false;
        }

        if (!CopyAttributeCommandHandler.Controls(actor, thing))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        if (thing.Type == ObjectType.Garbage || thing.HasFlag("GOING"))
        {
            context.Notify(Feedback.WhyWouldYouWantToBeWarnedAboutGarbage);
            return false;
        }

        uint old = thing.Warnings;
        uint next = WarningBits.Parse(context.RightArg, unknown => context.Notify(Feedback.UnknownWarning, ("value", $"{unknown}")));
        if (next != old)
        {
            thing.Warnings = next;
            if (next != 0)
                context.Notify(Feedback.WarningsSetTo, ("value", $"{WarningBits.Unparse(next)}"));
            else
                context.Notify(Feedback.WarningsCleared);
        }
        else
        {
            context.Notify(Feedback.WarningsNotChanged);
        }

        return true;
    }
}
