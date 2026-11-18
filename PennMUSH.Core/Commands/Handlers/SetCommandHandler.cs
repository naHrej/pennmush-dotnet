namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Flags;
using PennMUSH.Core.Types;

public class SetCommandHandler : ICommandHandler
{
    public string Name => "@set";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify(Feedback.WhatDoYouWantToSet);
            return false;
        }

        var target = db.MatchObject(context.Executor, context.LeftArg);
        if (target == null)
        {
            context.Notify(Feedback.ICantSeeThatHere);
            return false;
        }

        var executorObj = db.GetObject(context.Executor);
        if (executorObj == null)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        int colon = context.RightArg.IndexOf(':');
        if (colon != -1)
        {
            // Attribute assignment
            string attrName = context.RightArg.Substring(0, colon).Trim();
            string attrVal = context.RightArg.Substring(colon + 1);

            // Permission check: owner or wizard can set attributes
            if (!executorObj.IsWizard && executorObj.Id != target.Owner)
            {
                context.Notify(Feedback.PermissionDenied);
                return false;
            }

            if (attrName.Contains('`'))
            {
                var written = AttributeWriter.Apply(executorObj, target, attrName, string.IsNullOrEmpty(attrVal) ? null : attrVal);
                if (written.Ok)
                {
                    context.Notify(Feedback.Set);
                    return true;
                }

                context.Notify(written.Message);
                return false;
            }

            var err = target.SetAttribute(attrName, attrVal, context.Executor, wizardOverride: executorObj.IsWizard);
            if (err == AttributeErrorCode.Okay)
            {
                context.Notify(Feedback.Set);
                return true;
            }
            context.Notify(Feedback.FailedToSetAttribute, ("attribute", $"{err}"));
            return false;
        }
        else
        {
            // Flag toggling
            bool negate = context.RightArg.StartsWith('!');
            string flagName = negate ? context.RightArg.Substring(1).Trim() : context.RightArg.Trim();

            var flagDef = FlagTable.FindFlag(flagName);
            if (flagDef == null)
            {
                context.Notify(Feedback.IDontRecognizeThatFlag);
                return false;
            }

            if (!FlagTable.CanSetFlag(executorObj, target, flagDef, negate, out var err))
            {
                context.Notify(err);
                return false;
            }

            if (negate)
                target.RemoveFlag(flagDef.Name);
            else
                target.AddFlag(flagDef.Name);

            context.Notify(Feedback.Set);
            return true;
        }
    }

    private static Dbref ResolveDbref(string s, Dbref executor)
    {
        if (s.Equals("me", StringComparison.OrdinalIgnoreCase)) return executor;
        if (s.StartsWith('#') && int.TryParse(s.Substring(1), out int id)) return new Dbref(id);
        if (int.TryParse(s, out int plainId)) return new Dbref(plainId);
        return Dbref.Nothing;
    }
}
