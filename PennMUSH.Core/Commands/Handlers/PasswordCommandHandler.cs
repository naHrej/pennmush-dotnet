namespace PennMUSH.Core.Commands.Handlers;

using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Logging;
using PennMUSH.Core.Types;

public class PasswordCommandHandler : ICommandHandler
{
    public string Name => "@password";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || actor.Type != ObjectType.Player)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        if (actor.HasFlag("GUEST"))
        {
            context.Notify(Feedback.GuestsAreNotAllowedToDoThat);
            return false;
        }

        if (!PasswordHash.Accepts(actor, context.LeftArg, memory: null))
        {
            context.Notify(Feedback.TheOldPasswordThatYouEnteredWasIncorrect);
            return false;
        }

        if (!PasswordHash.Ok(context.RightArg))
        {
            context.Notify(Feedback.BadNewPassword);
            return false;
        }

        PasswordHash.Store(actor, context.RightArg);
        context.Notify(Feedback.YouHaveChangedYourPassword);
        return true;
    }
}

public class NewPasswordCommandHandler : ICommandHandler
{
    private readonly WizardLog? _log;

    public string Name => "@newpassword";

    public NewPasswordCommandHandler(WizardLog? log = null) => _log = log;

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.IsWizard)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        bool generate = context.Switches.Contains("generate");
        string password = generate ? PasswordHash.Generate() : context.RightArg;
        string name = context.LeftArg.Trim();
        MUSHObject? victim = name.Equals("me", System.StringComparison.OrdinalIgnoreCase)
            ? actor
            : db.FindPlayer(name);
        if (victim == null || victim.Type != ObjectType.Player)
        {
            context.Notify(Feedback.NoSuchPlayer);
            return false;
        }

        if (password.Length > 0 && !PasswordHash.Ok(password))
        {
            context.Notify(Feedback.BadPassword);
            return false;
        }

        if (victim.IsGod && !actor.IsGod)
        {
            context.Notify(Feedback.YouCannotChangeThatPlayersPassword);
            return false;
        }

        PasswordHash.Store(victim, password);
        if (generate)
            context.Notify(Feedback.PasswordForChangedTo, ("value", $"{victim.Name}"), ("value2", $"{password}"));
        else
            context.Notify(Feedback.PasswordForChanged, ("value", $"{victim.Name}"));
        context.NotifyTarget(victim.Id, Feedback.YourPasswordHasBeenChangedBy.Text(("value", $"{actor.Name}")));
        _log?.Append(
            WizardLogKind.Wiz,
            $"WIZ: {Who(actor)} --> {Who(victim)}: *** NEWPASSWORD ***");
        return true;
    }

    private static string Who(MUSHObject actor)
        => $"{actor.Name}(#{actor.Id.Value}{LogSwitches.FlagText(actor)})";
}
