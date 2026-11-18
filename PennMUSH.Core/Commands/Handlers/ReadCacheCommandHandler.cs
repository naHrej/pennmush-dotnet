namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Database;
using PennMUSH.Core.Help;

public class ReadCacheCommandHandler : ICommandHandler
{
    private readonly HelpStore _help;
    private readonly Func<IReadOnlyList<string>> _sizes;

    public string Name => "@readcache";

    public ReadCacheCommandHandler(HelpStore help, Func<IReadOnlyList<string>> sizes)
    {
        _help = help;
        _sizes = sizes;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.IsWizard)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        foreach (string line in _sizes())
            context.Notify(line);
        _help.Reload();
        context.Notify(Feedback.HelpFilesReindexed);
        return true;
    }
}
