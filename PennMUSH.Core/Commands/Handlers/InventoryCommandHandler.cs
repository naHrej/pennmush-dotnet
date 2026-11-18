namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class InventoryCommandHandler : ICommandHandler
{
    private readonly ExpressionEvaluator? _evaluator;

    public string Name => "inventory";

    public InventoryCommandHandler(ExpressionEvaluator? evaluator = null)
    {
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var execObj = db.GetObject(context.Executor);
        if (execObj == null)
        {
            context.Notify(Feedback.YouAreNowhere);
            return false;
        }

        var carried = db.GetContents(context.Executor).ToList();
        string? formatted = FormatAttributes.Evaluate(
            db,
            _evaluator,
            execObj,
            execObj.Id,
            execObj.Id,
            "INVFORMAT",
            string.Join(' ', carried.Select(item => item.Id.ToString())),
            string.Join('|', carried.Select(item => item.Name)));
        if (formatted != null)
        {
            if (!string.IsNullOrWhiteSpace(formatted))
                context.Notify(formatted);
            return true;
        }

        var items = new List<string>();
        foreach (var item in carried)
        {
            items.Add(item.Name);
        }

        if (items.Count == 0)
        {
            context.Notify(Feedback.YouAreEmptyHanded);
        }
        else
        {
            context.Notify(Feedback.YouAreCarrying);
            foreach (var item in items)
            {
                context.Notify(Feedback.Value, ("value", $"{item}"));
            }
        }

        return true;
    }
}
