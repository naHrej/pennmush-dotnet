namespace PennMUSH.Core.Commands.Handlers;

using PennMUSH.Core.Database;
using PennMUSH.Core.Types;

public class PcreateCommandHandler : ICommandHandler
{
    private readonly IPlayerPasswords? _passwords;
    private readonly Dbref _home;
    private readonly Dbref _start;
    private readonly int _pennies;

    public string Name => "@pcreate";

    public PcreateCommandHandler(IPlayerPasswords? passwords, Dbref home, Dbref start, int pennies)
    {
        _passwords = passwords;
        _home = home;
        _start = start;
        _pennies = pennies;
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
            db, actor, context.LeftArg, password, reuse, _passwords, _home, _start, _pennies, context.Notify);
        return created.IsValid;
    }
}
