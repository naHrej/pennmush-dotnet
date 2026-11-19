namespace PennMUSH.Core.Commands.Handlers;

using PennMUSH.Core.Configuration;
using PennMUSH.Core.Database;

public class EnableCommandHandler : ICommandHandler
{
    private readonly MushConfiguration _config;
    private readonly bool _enable;

    public EnableCommandHandler(MushConfiguration config, bool enable)
    {
        _config = config;
        _enable = enable;
    }

    public string Name => _enable ? "@enable" : "@disable";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.IsWizard)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }
        if (_enable && actor.HasFlag("GAGGED"))
        {
            context.Notify(Feedback.YouCannotDoThatWhileGagged);
            return false;
        }
        if (!_config.TrySetSwitch(context.LeftArg, _enable, out string failure))
        {
            context.Notify(failure);
            return false;
        }
        context.Notify(_enable ? Feedback.Enabled : Feedback.Disabled);
        return true;
    }
}
