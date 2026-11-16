namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class DigCommandHandler : ICommandHandler
{
    private readonly LookCommandHandler? _lookHandler;

    public string Name => "@dig";

    public DigCommandHandler(LookCommandHandler? lookHandler = null)
    {
        _lookHandler = lookHandler;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg))
        {
            context.Notify("Dig what?");
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null)
        {
            context.Notify("Permission denied.");
            return false;
        }

        string roomName = context.LeftArg.Trim();
        var newRoom = db.CreateObject(roomName, ObjectType.Room, context.Executor);
        context.Notify($"{newRoom.Name} created with room number {newRoom.Id}.");

        // Parse optional exits: RightArg = "<exit_to>[,<exit_back>]"
        if (!string.IsNullOrWhiteSpace(context.RightArg))
        {
            string exitTo = context.RightArg;
            string exitBack = string.Empty;

            int comma = context.RightArg.IndexOf(',');
            if (comma != -1)
            {
                exitTo = context.RightArg.Substring(0, comma).Trim();
                exitBack = context.RightArg.Substring(comma + 1).Trim();
            }

            if (!string.IsNullOrWhiteSpace(exitTo) && execObj.Location.IsValid)
            {
                var exitToObj = db.CreateObject(exitTo, ObjectType.Exit, context.Executor);
                db.AttachExit(execObj.Location, exitToObj.Id);
                db.LinkExit(exitToObj.Id, newRoom.Id);
                context.Notify($"Opening exit {exitTo} to {newRoom.Id}.");
            }

            if (!string.IsNullOrWhiteSpace(exitBack) && execObj.Location.IsValid)
            {
                var exitBackObj = db.CreateObject(exitBack, ObjectType.Exit, context.Executor);
                db.AttachExit(newRoom.Id, exitBackObj.Id);
                db.LinkExit(exitBackObj.Id, execObj.Location);
                context.Notify($"Opening exit {exitBack} to {execObj.Location}.");
            }
        }

        // Teleport switch: @dig/teleport
        if (context.Switches.Contains("teleport") || context.Switches.Contains("tel"))
        {
            db.MoveObject(context.Executor, newRoom.Id);
            context.Notify($"Teleported to {newRoom.Name}.");
            _lookHandler?.ShowHere(context, db, context.Executor, LookKind.Automatic);
        }

        return true;
    }
}
