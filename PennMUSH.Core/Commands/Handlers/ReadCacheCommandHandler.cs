namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Database;
using PennMUSH.Core.Help;

public class ReadCacheCommandHandler : ICommandHandler
{
    private readonly HelpStore _help;
    private readonly Func<IReadOnlyList<string>> _sizes;
    private readonly ITextFileCache? _textCache;
    private readonly Action? _onReloaded;

    public string Name => "@readcache";

    public ReadCacheCommandHandler(
        HelpStore help,
        Func<IReadOnlyList<string>> sizes,
        ITextFileCache? textCache = null,
        Action? onReloaded = null)
    {
        _help = help;
        _sizes = sizes;
        _textCache = textCache;
        _onReloaded = onReloaded;
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
        _textCache?.Reload();
        _onReloaded?.Invoke();
        context.Notify(Feedback.HelpFilesReindexed);
        return true;
    }
}
