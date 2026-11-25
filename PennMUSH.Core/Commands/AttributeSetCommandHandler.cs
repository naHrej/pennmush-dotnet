namespace PennMUSH.Core.Commands;

using PennMUSH.Core.Attributes;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;

public static class AttributeSetCommandHandler
{
    public static bool TryExecute(CommandContext context, ObjectDatabase db)
    {
        if (!TryReadName(context.CommandName, out string name))
            return false;

        var actor = db.GetObject(context.Executor);
        if (actor == null)
        {
            context.Notify(Feedback.PermissionDenied);
            return true;
        }

        if (string.IsNullOrWhiteSpace(context.LeftArg))
        {
            context.Notify(Feedback.IDontSeeThatHere);
            return true;
        }

        var target = db.MatchObject(context.Executor, context.LeftArg);
        if (target == null)
        {
            context.Notify(Feedback.IDontSeeThatHere);
            return true;
        }

        if (!Controls(actor, target))
        {
            context.Notify(Feedback.PermissionDenied);
            return true;
        }

        bool hasValue = context.Arguments != null && context.Arguments.Contains('=') && context.RightArg.Length > 0;
        var written = AttributeWriter.Apply(actor, target, name, hasValue ? context.RightArg : null);
        if (context.CommandName.Equals("@alias", System.StringComparison.OrdinalIgnoreCase) && written.Ok)
            context.Notify(hasValue ? "Alias set." : "Alias removed.");
        else if (written.Message.Length > 0)
            context.Notify(written.Message);
        return true;
    }

    private static bool Controls(MUSHObject actor, MUSHObject target)
    {
        return actor.IsWizard || actor.Id == target.Owner || actor.Owner == target.Owner;
    }

    private static bool TryReadName(string command, out string name)
    {
        if (command.Length >= 2 && command[0] == '&')
        {
            name = command[1..].ToUpperInvariant();
            return true;
        }

        if (command.Length >= 3 && command.StartsWith("@_", System.StringComparison.Ordinal))
        {
            name = command[2..].ToUpperInvariant();
            return true;
        }

        if (command.Length >= 2 && command[0] == '@' && StandardAttributes.TryResolve(command[1..], out name))
            return true;

        name = string.Empty;
        return false;
    }
}
