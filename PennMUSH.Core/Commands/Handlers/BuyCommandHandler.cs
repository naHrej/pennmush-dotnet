namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using System.Globalization;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Locks;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class BuyCommandHandler : ICommandHandler
{
    private readonly ExpressionEvaluator? _evaluator;
    private readonly ActionQueue? _queue;

    public BuyCommandHandler(ExpressionEvaluator? evaluator = null, ActionQueue? queue = null)
    {
        _evaluator = evaluator;
        _queue = queue;
    }

    public string Name => "buy";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.Location.IsValid)
            return false;

        string text = (context.Arguments.Length > 0 ? context.Arguments : context.LeftArg).ToUpperInvariant();
        string? from = Cut(ref text, " FROM ");
        string? forWhat = Cut(ref text, " FOR ");
        int price = -1;
        if (forWhat != null)
        {
            if (!int.TryParse(forWhat.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out price))
            {
                context.Notify("Buy for WHAT price?");
                return false;
            }
            if (price < 0)
            {
                context.Notify("You can't buy things by taking money.");
                return false;
            }
        }

        MUSHObject? vendor;
        bool named = false;
        if (!string.IsNullOrWhiteSpace(from))
        {
            vendor = NearbyMatch.Find(db, actor, from.Trim(), out bool ambiguous);
            if (ambiguous)
            {
                context.Notify("I don't know who you mean!");
                return false;
            }
            if (vendor == null || vendor.Type is not (ObjectType.Player or ObjectType.Thing))
            {
                context.Notify("Buy from whom?");
                return false;
            }
            if (vendor.Id == actor.Id)
            {
                context.Notify("You can't buy from yourself!");
                return false;
            }
            named = true;
        }
        else
        {
            vendor = FirstOther(db, actor);
            if (vendor == null)
            {
                context.Notify("There's nobody here to buy things from.");
                return false;
            }
        }

        string item = text.Trim();
        if (item.Length == 0)
        {
            context.Notify("Buy what?");
            return false;
        }

        string needle = item.Replace(' ', '_') + ":";
        MUSHObject? refused = null;
        bool affordable = true;
        var start = vendor;
        do
        {
            if (vendor!.Id == actor.Id)
                continue;
            var list = vendor.GetAttribute("PRICELIST", db)?.Value;
            if (string.IsNullOrWhiteSpace(list))
                continue;
            foreach (string token in list.ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!token.StartsWith(needle, StringComparison.OrdinalIgnoreCase))
                    continue;
                string costs = token[needle.Length..];
                if (costs.Length == 0)
                    continue;
                foreach (string cost in costs.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!TryPrice(cost, price, out int charge))
                        continue;
                    if (!vendor.EvalLock(LockType.Pay, actor.Id, db))
                    {
                        refused ??= vendor;
                        continue;
                    }
                    if (!actor.IsWizard && actor.Pennies < charge)
                    {
                        affordable = false;
                        continue;
                    }
                    if (!actor.IsWizard)
                        actor.Pennies -= charge;
                    vendor.Pennies += charge;
                    string bought = item.Replace(' ', '_').ToLowerInvariant();
                    Deliver(context, db, actor, vendor, bought, charge);
                    return true;
                }
            }
        }
        while (!named && (vendor = Next(db, vendor)) != null && vendor.Id != start.Id);

        if (refused != null)
            context.Notify($"{refused.Name} doesn't want your money.");
        else if (price >= 0)
            context.Notify(named ? $"{start.Name} isn't selling that item for that price" : "I can't find that item with that price here.");
        else if (affordable)
            context.Notify(named ? $"{start.Name} isn't selling that item." : "I can't find that item here.");
        else
            context.Notify("You can't afford that.");
        return false;
    }

    private void Deliver(CommandContext context, ObjectDatabase db, MUSHObject actor, MUSHObject vendor, string item, int charge)
    {
        string you = $"You buy a {item} from {vendor.Name}.";
        string others = $"{actor.Name} buys a {item} from {vendor.Name}.";
        string? buy = Eval(vendor, "BUY", actor, item, charge, db);
        string? obuy = Eval(vendor, "OBUY", actor, item, charge, db);
        context.Notify(string.IsNullOrEmpty(buy) ? you : buy);
        if (actor.Location.IsValid)
            context.NotifyExcept(actor.Location, actor.Id, string.IsNullOrEmpty(obuy) ? others : obuy);
        string? action = vendor.GetAttribute("ABUY", db)?.Value;
        if (!string.IsNullOrWhiteSpace(action) && _queue != null
            && (vendor.Type == ObjectType.Player || !vendor.HasFlag("HALT")))
        {
            _queue.EnqueueActionList(
                vendor.Id,
                actor.Id,
                action,
                new[] { item, charge.ToString(CultureInfo.InvariantCulture) });
        }
    }

    private string? Eval(MUSHObject vendor, string attr, MUSHObject actor, string item, int charge, ObjectDatabase db)
    {
        var entry = vendor.GetAttribute(attr, db);
        if (entry == null || _evaluator == null)
            return null;
        var ctx = new EvaluationContext(vendor.Id, actor.Id, location: vendor.Location, args: new[] { item, charge.ToString(CultureInfo.InvariantCulture) });
        return _evaluator.Evaluate(entry.Value, ctx);
    }

    private static bool TryPrice(string cost, int offered, out int charge)
    {
        charge = -1;
        string text = cost.Trim();
        int dash = text.IndexOf('-');
        if (dash > 0)
        {
            if (!int.TryParse(text[..dash], out int low) || !int.TryParse(text[(dash + 1)..], out int high))
                return false;
            charge = offered < 0 ? low : (offered >= low && offered <= high ? offered : -1);
            return charge >= 0;
        }
        if (text.EndsWith('+'))
        {
            if (!int.TryParse(text[..^1], out int low))
                return false;
            charge = offered < 0 ? low : (offered > low ? offered : -1);
            return charge >= 0;
        }
        if (!int.TryParse(text, out int exact))
            return false;
        charge = offered < 0 ? exact : (exact == offered ? offered : -1);
        return charge >= 0;
    }

    private static string? Cut(ref string text, string marker)
    {
        int at = text.IndexOf(marker, StringComparison.Ordinal);
        if (at < 0)
            return null;
        string rest = text[(at + marker.Length)..];
        text = text[..at];
        return rest;
    }

    private static MUSHObject? FirstOther(ObjectDatabase db, MUSHObject actor)
    {
        foreach (var item in db.GetContents(actor.Location))
        {
            if (item.Id != actor.Id && item.Type is ObjectType.Player or ObjectType.Thing)
                return item;
        }
        return null;
    }

    private static MUSHObject? Next(ObjectDatabase db, MUSHObject current)
    {
        if (!current.Next.IsValid)
            return null;
        var next = db.GetObject(current.Next);
        return next != null && next.Type is ObjectType.Player or ObjectType.Thing ? next : null;
    }
}
