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
            context.Notify("Permission denied.");
            return false;
        }

        if (actor.HasFlag("GUEST"))
        {
            context.Notify("Guests are not allowed to do that.");
            return false;
        }

        if (!PasswordHash.Accepts(actor, context.LeftArg, memory: null))
        {
            context.Notify("The old password that you entered was incorrect.");
            return false;
        }

        if (!PasswordHash.Ok(context.RightArg))
        {
            context.Notify("Bad new password.");
            return false;
        }

        PasswordHash.Store(actor, context.RightArg);
        context.Notify("You have changed your password.");
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
            context.Notify("Permission denied.");
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
            context.Notify("No such player.");
            return false;
        }

        if (password.Length > 0 && !PasswordHash.Ok(password))
        {
            context.Notify("Bad password.");
            return false;
        }

        if (victim.IsGod && !actor.IsGod)
        {
            context.Notify("You cannot change that player's password.");
            return false;
        }

        PasswordHash.Store(victim, password);
        if (generate)
            context.Notify($"Password for {victim.Name} changed to {password}.");
        else
            context.Notify($"Password for {victim.Name} changed.");
        context.NotifyTarget(victim.Id, $"Your password has been changed by {actor.Name}.");
        _log?.Append(
            WizardLogKind.Wiz,
            $"WIZ: {Who(actor)} --> {Who(victim)}: *** NEWPASSWORD ***");
        return true;
    }

    private static string Who(MUSHObject actor)
        => $"{actor.Name}(#{actor.Id.Value}{LogSwitches.FlagText(actor)})";
}
