namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class ChownAllCommandHandler : ICommandHandler
{
    private readonly int _startingQuota;

    public ChownAllCommandHandler(int startingQuota = 20)
    {
        _startingQuota = startingQuota;
    }

    public string Name => "@chownall";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.IsWizard)
        {
            context.Notify("Try asking them first!");
            return false;
        }

        if (!TryPlayer(db, actor, context.LeftArg, out var victim))
        {
            context.Notify("I can't see that here.");
            return false;
        }

        MUSHObject recipient = actor;
        if (!string.IsNullOrWhiteSpace(context.RightArg))
        {
            if (!TryPlayer(db, actor, context.RightArg, out recipient))
            {
                context.Notify("I can't see that here.");
                return false;
            }
        }

        var types = SelectedTypes(context.Switches);
        bool preserve = context.Switches.Contains("preserve");
        int count = 0;
        foreach (var obj in db.GetAllObjects())
        {
            if (obj.Owner != victim.Id || !types.Contains(obj.Type))
                continue;
            Transfer(context, actor, obj, recipient, preserve);
            count++;
        }

        QuotaRules.Adjust(db, victim, count, _startingQuota);
        QuotaRules.Adjust(db, recipient, -count, _startingQuota);
        context.Notify($"Ownership changed for {count} objects.");
        return true;
    }

    private static HashSet<ObjectType> SelectedTypes(IReadOnlySet<string> switches)
    {
        var types = new HashSet<ObjectType>();
        if (switches.Contains("things"))
            types.Add(ObjectType.Thing);
        if (switches.Contains("rooms"))
            types.Add(ObjectType.Room);
        if (switches.Contains("exits"))
            types.Add(ObjectType.Exit);
        if (types.Count == 0)
        {
            types.Add(ObjectType.Thing);
            types.Add(ObjectType.Room);
            types.Add(ObjectType.Exit);
        }
        return types;
    }

    private static bool TryPlayer(ObjectDatabase db, MUSHObject actor, string name, out MUSHObject player)
    {
        string token = name.Trim();
        if (token.Equals("me", StringComparison.OrdinalIgnoreCase))
        {
            player = actor;
            return actor.Type == ObjectType.Player;
        }
        if (token.StartsWith('*') && token.Length > 1)
            token = token.Substring(1);
        var found = db.FindPlayer(token);
        if (found == null || found.Type != ObjectType.Player)
        {
            player = actor;
            return false;
        }
        player = found;
        return true;
    }

    private static void Transfer(CommandContext context, MUSHObject actor, MUSHObject obj, MUSHObject recipient, bool preserve)
    {
        obj.RemoveFlag("GOING");
        obj.Owner = actor.IsGod ? recipient.Id : recipient.Owner;
        obj.ClearZones();
        foreach (var zone in recipient.Zones)
            obj.AddZone(zone);
        obj.RemoveFlag("CHOWN_OK");

        if (!preserve)
        {
            obj.RemoveFlag("WIZARD");
            obj.RemoveFlag("ROYALTY");
            obj.RemoveFlag("TRUST");
            obj.AddFlag("HALT");
            foreach (var power in obj.Powers.ToList())
                obj.RemovePower(power);
            return;
        }

        if (recipient.Id != actor.Id && obj.HasFlag("WIZARD") && !recipient.IsWizard)
        {
            context.Notify($"Warning: WIZ flag reset on #{obj.Id.Value} because @CHOWN/PRESERVE is to a non-WIZ flagged third-party.");
            obj.RemoveFlag("WIZARD");
        }
        if (obj.Powers.Count > 0 || obj.HasFlag("WIZARD") || obj.HasFlag("ROYALTY") || obj.HasFlag("INHERIT"))
        {
            context.Notify($"Warning: @CHOWN/PRESERVE on an object (#{obj.Id.Value}) with WIZ, ROY, INHERIT, or @power privileges.");
        }
    }
}
