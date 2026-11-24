namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class ParentCommandHandler : ICommandHandler
{
    public string Name => "@parent";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg))
        {
            context.Notify(Feedback.WhatObjectDoYouWantToParent);
            return false;
        }

        var targetObj = db.MatchObject(context.Executor, context.LeftArg.Trim());
        if (targetObj == null)
        {
            context.Notify(Feedback.ICantSeeThatHere);
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || (!execObj.IsWizard && execObj.Id != targetObj.Owner))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        if (string.IsNullOrWhiteSpace(context.RightArg) || context.RightArg.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            targetObj.ClearParents();
            context.Notify(Feedback.ParentCleared);
            return true;
        }

        var tokens = context.RightArg.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var validParents = new List<Dbref>();

        foreach (var token in tokens)
        {
            var pObj = db.MatchObject(context.Executor, token);
            if (pObj == null)
            {
                context.Notify(Feedback.ICantSeeParentObject, ("object", $"{token}"));
                return false;
            }

            if (MUSHObject.WouldCauseParentCycle(targetObj.Id, pObj.Id, db))
            {
                context.Notify(Feedback.YouWouldCreateALoopInTheParentTree);
                return false;
            }

            validParents.Add(pObj.Id);
        }

        targetObj.SetParents(validParents, db);
        context.Notify(Feedback.ParentSet);
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
