namespace PennMUSH.Server.Commands;

using System;
using System.Threading.Tasks;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Database;

public class RebootCommandHandler : ICommandHandler
{
    private readonly Action _onReboot;

    public string Name => "@reboot";

    public RebootCommandHandler(Action onReboot)
    {
        _onReboot = onReboot;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.IsWizard)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        context.Notify(Feedback.InitiatingHotRebootAllPlayerConnectionsAndWorldSimulationStateWillBePreserved);
        Task.Run(async () =>
        {
            await Task.Delay(200);
            _onReboot();
        });
        return true;
    }
}
