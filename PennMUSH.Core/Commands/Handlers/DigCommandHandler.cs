namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class DigCommandHandler : ICommandHandler
{
    private readonly LookCommandHandler? _lookHandler;
    private readonly Func<bool>? _liveUseQuota;
    private readonly Func<int>? _liveStartingQuota;
    private bool UseQuotaNow => _liveUseQuota?.Invoke() ?? true;
    private int StartingQuotaNow => _liveStartingQuota?.Invoke() ?? 20;

    public string Name => "@dig";

    public DigCommandHandler(
        LookCommandHandler? lookHandler = null,
        Func<bool>? liveUseQuota = null,
        Func<int>? liveStartingQuota = null)
    {
        _lookHandler = lookHandler;
        _liveUseQuota = liveUseQuota;
        _liveStartingQuota = liveStartingQuota;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg))
        {
            context.Notify(Feedback.DigWhat);
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        string exitTo = string.Empty;
        string exitBack = string.Empty;
        if (!string.IsNullOrWhiteSpace(context.RightArg))
        {
            exitTo = context.RightArg;
            int comma = context.RightArg.IndexOf(',');
            if (comma != -1)
            {
                exitTo = context.RightArg.Substring(0, comma).Trim();
                exitBack = context.RightArg.Substring(comma + 1).Trim();
            }
        }

        bool createExitTo = !string.IsNullOrWhiteSpace(exitTo) && execObj.Location.IsValid;
        bool createExitBack = !string.IsNullOrWhiteSpace(exitBack) && execObj.Location.IsValid;
        int quotaCost = 1 + (createExitTo ? 1 : 0) + (createExitBack ? 1 : 0);

        if (!QuotaRules.TryPay(db, execObj, quotaCost, UseQuotaNow, StartingQuotaNow))
        {
            context.Notify(Feedback.SorryYourBuildingQuotaHasRunOut);
            return false;
        }

        string roomName = context.LeftArg.Trim();
        var newRoom = db.CreateObject(roomName, ObjectType.Room, context.Executor);
        context.Notify(Feedback.CreatedWithRoomNumber, ("value", $"{newRoom.Name}"), ("value2", $"{newRoom.Id}"));

        // Parse optional exits: RightArg = "<exit_to>[,<exit_back>]"
        if (createExitTo)
        {
            var exitToObj = db.CreateObject(exitTo, ObjectType.Exit, context.Executor);
            db.AttachExit(execObj.Location, exitToObj.Id);
            db.LinkExit(exitToObj.Id, newRoom.Id);
            context.Notify(Feedback.OpeningExitTo, ("exit", $"{exitTo}"), ("value", $"{newRoom.Id}"));
        }

        if (createExitBack)
        {
            var exitBackObj = db.CreateObject(exitBack, ObjectType.Exit, context.Executor);
            db.AttachExit(newRoom.Id, exitBackObj.Id);
            db.LinkExit(exitBackObj.Id, execObj.Location);
            context.Notify(Feedback.OpeningExitTo, ("exit", $"{exitBack}"), ("value", $"{execObj.Location}"));
        }

        // Teleport switch: @dig/teleport
        if (context.Switches.Contains("teleport") || context.Switches.Contains("tel"))
        {
            db.MoveObject(context.Executor, newRoom.Id);
            context.Notify(Feedback.TeleportedTo, ("value", $"{newRoom.Name}"));
            _lookHandler?.ShowHere(context, db, context.Executor, LookKind.Automatic);
        }

        return true;
    }
}
