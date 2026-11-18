namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Session;
using PennMUSH.Core.Types;

public class SocksetCommandHandler : ICommandHandler
{
    private readonly ISessionDirectory _sessions;

    public string Name => "@sockset";

    public SocksetCommandHandler(ISessionDirectory sessions)
    {
        _sessions = sessions;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        bool setting = context.Arguments.Contains('=');
        string target = context.LeftArg.Trim();
        bool own = target.Length == 0;
        int? descriptor = null;
        Dbref subject = context.Executor;
        if (!own)
        {
            if (int.TryParse(target, out int number) && number.ToString() == target)
            {
                descriptor = number;
            }
            else
            {
                var player = SessionAccess.FindPlayer(db, context.Executor, target);
                if (player == null || player.Type != ObjectType.Player)
                {
                    context.Notify(Feedback.InvalidDescriptor);
                    return false;
                }
                subject = player.Id;
            }
        }

        string message = _sessions.Sockset(
            context.Executor,
            subject,
            descriptor,
            own,
            setting,
            context.RightArg,
            actor.IsWizard,
            SessionAccess.SeesConnections(actor),
            id => db.GetObject(id)?.IsWizard == true);
        context.Notify(message);
        return message != "You are not connected?"
            && message != "Invalid descriptor."
            && message != "Permission denied.";
    }
}
