namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Flags;
using PennMUSH.Core.Types;

public class PowerCommandHandler : ICommandHandler
{
    public string Name => "@power";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify(Feedback.WhatPowerDoYouWantToSet);
            return false;
        }

        Dbref targetRef = ResolveDbref(context.LeftArg, context.Executor);
        var target = db.GetObject(targetRef);
        if (target == null)
        {
            context.Notify(Feedback.ICantSeeThatHere);
            return false;
        }

        var executorObj = db.GetObject(context.Executor);
        if (executorObj == null || !executorObj.IsWizard)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        bool negate = context.RightArg.StartsWith('!');
        string powerName = negate ? context.RightArg.Substring(1).Trim() : context.RightArg.Trim();

        var powerDef = FlagTable.FindPower(powerName);
        if (powerDef == null)
        {
            context.Notify(Feedback.IDontRecognizeThatPower);
            return false;
        }

        if (negate)
            target.RemovePower(powerDef.Name);
        else
            target.AddPower(powerDef.Name);

        context.Notify(Feedback.Set);
        return true;
    }

    private static Dbref ResolveDbref(string s, Dbref executor)
    {
        if (s.Equals("me", StringComparison.OrdinalIgnoreCase)) return executor;
        if (s.StartsWith('#') && int.TryParse(s.Substring(1), out int id)) return new Dbref(id);
        if (int.TryParse(s, out int plainId)) return new Dbref(plainId);
        return Dbref.Nothing;
    }
}
