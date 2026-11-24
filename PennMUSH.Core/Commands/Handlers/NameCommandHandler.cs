namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Types;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;

public class NameCommandHandler : ICommandHandler
{
    public string Name => "@name";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify(Feedback.YouMustSpecifyATargetAndANewName);
            return false;
        }

        var target = db.MatchObject(context.Executor, context.LeftArg.Trim());
        if (target == null)
        {
            context.Notify(Feedback.ICantSeeThatHere);
            return false;
        }

        target.Name = context.RightArg.Trim();
        context.Notify(Feedback.NameSet);
        return true;
    }
}