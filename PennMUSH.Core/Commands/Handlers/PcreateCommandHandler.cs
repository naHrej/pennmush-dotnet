namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;

public class PcreateCommandHandler : ICommandHandler
{
    private readonly IPlayerPasswords? _passwords;
    private readonly Func<Dbref> _homeProvider;
    private readonly Func<Dbref> _startProvider;
    private readonly Func<int> _penniesProvider;

    public string Name => "@pcreate";

    public PcreateCommandHandler(IPlayerPasswords? passwords, Dbref home, Dbref start, int pennies)
        : this(passwords, () => home, () => start, () => pennies)
    {
    }

    public PcreateCommandHandler(IPlayerPasswords? passwords, Func<Dbref> homeProvider, Func<Dbref> startProvider, Func<int> penniesProvider)
    {
        _passwords = passwords;
        _homeProvider = homeProvider;
        _startProvider = startProvider;
        _penniesProvider = penniesProvider;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;
        if (string.IsNullOrWhiteSpace(context.LeftArg) || !context.Arguments.Contains('='))
        {
            context.Notify(Feedback.UsagePcreateNamePassword);
            return false;
        }

        string right = context.RightArg;
        string password = right;
        string? reuse = null;
        int comma = right.IndexOf(',');
        if (comma >= 0)
        {
            password = right[..comma].Trim();
            reuse = right[(comma + 1)..].Trim();
        }

        var created = PlayerCreator.Create(
            db, actor, context.LeftArg, password, reuse, _passwords, _homeProvider(), _startProvider(), _penniesProvider(), context.Notify);
        return created.IsValid;
    }
}
