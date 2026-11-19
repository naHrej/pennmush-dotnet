namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Access;
using PennMUSH.Core.Database;

public class SiteLockCommandHandler : ICommandHandler
{
    private readonly SiteLockTable _table;
    private readonly ConnectionLogStore? _connLog;

    public string Name => "@sitelock";

    public SiteLockCommandHandler(SiteLockTable table, ConnectionLogStore? connLog = null)
    {
        _table = table;
        _connLog = connLog;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.IsWizard)
        {
            context.Notify(Feedback.YourDelusionsOfGrandeurHaveBeenNoted);
            return false;
        }

        string site = Site(context);
        if (Has(context, "name"))
            return Names(context, site);
        if (Has(context, "player"))
            return Player(context, db, site);
        if (Has(context, "ban"))
            return Add(context, site, 0, SiteLockTable.Default, "banned");
        if (Has(context, "register"))
            return Add(context, site, SiteLockTable.Register, SiteLockTable.Create, "locked");
        if (Has(context, "remove"))
        {
            int removed = string.IsNullOrEmpty(site) ? 0 : _table.Remove(site);
            context.Notify(Feedback.SitelocksRemoved, ("value", $"{removed}"));
            return removed > 0;
        }
        if (Has(context, "check"))
        {
            if (site.Length == 0)
            {
                List(context);
                return true;
            }
            var (rule, number) = _table.Check(site);
            context.Notify(rule == null ? "No matching access rule" : SiteLockTable.Describe(rule, number));
            return rule != null;
        }
        if (site.Length == 0)
        {
            List(context);
            return true;
        }
        return Add(context, site, 0, SiteLockTable.Create, "locked");
    }

    private bool Add(CommandContext context, string site, uint can, uint cant, string verb)
    {
        if (site.Length == 0)
        {
            context.Notify(Feedback.WhatSiteDidYouWantToLock);
            return false;
        }
        _table.Add(site, can, cant);
        context.Notify(Feedback.SiteValueValue, ("site", $"{site}"), ("value", $"{verb}"));
        return true;
    }

    private bool Player(CommandContext context, ObjectDatabase db, string name)
    {
        if (name.Length == 0)
        {
            context.Notify(Feedback.WhatSiteDidYouWantToLock);
            return false;
        }
        var target = db.GetAllObjects().FirstOrDefault(obj =>
            obj.Type == PennMUSH.Core.Types.ObjectType.Player
            && obj.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            context.Notify(Feedback.IDontSeeThatObject);
            return false;
        }
        string? lastIp = _connLog?.GetLastIpForPlayer(target.Id);
        if (!string.IsNullOrEmpty(lastIp))
            return Add(context, lastIp, 0, SiteLockTable.Default, "banned");
        context.Notify(Feedback.UnableToSitelockNoKnownIpHostToBan, ("value", $"{target.Name}"));
        return false;
    }

    private static bool Names(CommandContext context, string name)
    {
        context.Notify(name.Length == 0
            ? "Unable to open names file."
            : name.StartsWith('!') ? "Unable to delete name." : "Unable to lock name.");
        return false;
    }

    private void List(CommandContext context)
    {
        foreach (string line in _table.ListLines())
            context.Notify(line);
    }

    private static string Site(CommandContext context)
    {
        var tokens = new List<string>();
        foreach (string token in context.LeftArg.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (tokens.Count == 0 && token.StartsWith('/'))
                continue;
            tokens.Add(token);
        }
        return string.Join(' ', tokens);
    }

    private static bool Has(CommandContext context, string name)
    {
        if (context.Switches.Contains(name))
            return true;
        foreach (string token in context.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Equals("/" + name, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
