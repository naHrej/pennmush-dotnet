namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Communication.Mail;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class MaliasCommandHandler : ICommandHandler
{
    private readonly MailAliasTable _aliases;

    public string Name => "@malias";

    public MaliasCommandHandler(MailAliasTable aliases)
    {
        _aliases = aliases;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        MUSHObject? actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        if (Has(context, "list"))
            return List(context, db, actor);
        if (Has(context, "all"))
            return All(context, db, actor);
        if (Has(context, "members", "who"))
            return Members(context, db, actor);
        if (Has(context, "create"))
            return Create(context, db, actor);
        if (Has(context, "set"))
            return Set(context, db, actor);
        if (Has(context, "destroy"))
            return Destroy(context, actor);
        if (Has(context, "add"))
            return Add(context, db, actor);
        if (Has(context, "remove"))
            return Remove(context, db, actor);
        if (Has(context, "describe", "desc"))
            return Describe(context, actor);
        if (Has(context, "rename"))
            return Rename(context, actor);
        if (Has(context, "stats", "stat"))
            return Stats(context, actor);
        if (Has(context, "chown"))
            return Chown(context, db, actor);
        if (Has(context, "useflag"))
            return Privs(context, actor, members: false);
        if (Has(context, "seeflag"))
            return Privs(context, actor, members: true);
        if (Has(context, "nuke"))
            return Nuke(context, actor);

        if (string.IsNullOrEmpty(context.LeftArg))
        {
            if (!string.IsNullOrEmpty(context.RightArg))
            {
                context.Notify("MAIL: Invalid malias command.");
                return false;
            }
            return List(context, db, actor);
        }
        if (!string.IsNullOrEmpty(context.RightArg))
            return Create(context, db, actor);
        return Members(context, db, actor);
    }

    private bool List(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        bool header = false;
        foreach (var alias in _aliases.Visible(actor))
        {
            if (!header)
            {
                context.Notify($"{Fit("Name", 13)} {Fit("Alias Description", 35)} Use See {Fit("Owner", 15)}");
                header = true;
            }
            var owner = Shown(db, alias.Owner);
            context.Notify($"+{Fit(alias.Name, 12)} {Fit(alias.Description, 35)} {MailAliasTable.ShortPrivs(alias.NameFlags, alias.MemberFlags)} {Fit(owner, 15)}");
        }
        context.Notify("*****  End of Mail Aliases *****");
        return true;
    }

    private bool All(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        if (!actor.IsRoyalty)
            return List(context, db, actor);

        context.Notify("Num   Name       Description                              Owner       Count");
        for (int i = 0; i < _aliases.Aliases.Count; i++)
        {
            var alias = _aliases.Aliases[i];
            context.Notify($"#{i,-4} +{Fit(alias.Name, 10)} {Fit(alias.Description, 40)} {Fit(Shown(db, alias.Owner), 11)} ({alias.Members.Count,3})");
        }
        context.Notify("***** End of Mail Aliases *****");
        return true;
    }

    private bool Members(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        var alias = _aliases.Find(actor, context.LeftArg.Trim());
        if (alias == null)
        {
            context.Notify($"MAIL: Alias '{context.LeftArg.Trim()}' not found.");
            return false;
        }
        if (!MailAliasTable.CanSeeMembers(actor, alias))
        {
            context.Notify("MAIL: Permission denied.");
            return false;
        }
        var names = new List<string>();
        foreach (var id in alias.Members)
            names.Add(Shown(db, id));
        context.Notify($"MAIL: Alias +{alias.Name}: {string.Join(", ", names)}");
        return true;
    }

    private bool Create(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        if (actor.Type != ObjectType.Player)
        {
            context.Notify("MAIL: Only players may create mail aliases.");
            return false;
        }
        string aliasName = context.LeftArg.Trim();
        string list = context.RightArg;
        if (aliasName.Length == 0 || string.IsNullOrEmpty(list))
        {
            context.Notify("MAIL: What alias do you want to create?");
            return false;
        }
        if (!MailAliasTable.LegalName(aliasName, out string error))
        {
            context.Notify(error);
            return false;
        }
        if (_aliases.FindAny(aliasName) != null)
        {
            context.Notify($"MAIL: Mail Alias '{aliasName}' already exists.");
            return false;
        }

        var members = Collect(context, db, actor, aliasName, list, existing: null, out bool capped);
        if (capped)
            context.Notify("MAIL: Alias list is restricted to maximal 100 entries!");
        if (members.Count == 0)
        {
            context.Notify("MAIL: No valid recipients for alias-list!");
            return false;
        }

        var alias = new MailAlias
        {
            Name = aliasName[1..],
            Description = aliasName[1..],
            Owner = actor.Id,
            NameFlags = MailAliasTable.OwnerBit | MailAliasTable.Members,
            MemberFlags = MailAliasTable.OwnerBit
        };
        alias.Members.AddRange(members);
        _aliases.Add(alias);
        context.Notify($"MAIL: Alias set '{aliasName}' defined.");
        return true;
    }

    private bool Set(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        var alias = Require(context, actor);
        if (alias == null)
            return false;
        if (string.IsNullOrEmpty(context.RightArg))
        {
            context.Notify("MAIL: You must set the alias to a non-empty list.");
            return false;
        }
        if (!actor.IsWizard && alias.Owner != actor.Id)
        {
            context.Notify("MAIL: Permission denied!");
            return false;
        }
        var members = Collect(context, db, actor, context.LeftArg.Trim(), context.RightArg, null, out bool capped);
        if (capped)
            context.Notify("MAIL: Alias list is restricted to maximal 100 entries!");
        if (members.Count == 0)
        {
            context.Notify("MAIL: No valid recipients for alias-list!");
            return false;
        }
        alias.Members.Clear();
        alias.Members.AddRange(members);
        context.Notify("MAIL: Alias list set.");
        return true;
    }

    private bool Destroy(CommandContext context, MUSHObject actor)
    {
        var alias = _aliases.Find(actor, context.LeftArg.Trim());
        if (alias == null)
        {
            context.Notify($"MAIL: Not a valid alias. Remember to prefix the alias name with {MailAliasTable.Token}.");
            return false;
        }
        if (!actor.IsWizard && alias.Owner != actor.Id)
        {
            context.Notify("MAIL: Permission denied!");
            return false;
        }
        context.Notify("MAIL: Alias Destroyed.");
        _aliases.RemoveAt(_aliases.IndexOf(alias));
        return true;
    }

    private bool Add(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        string shown = context.LeftArg.Trim();
        var alias = _aliases.Find(actor, shown);
        if (alias == null)
        {
            context.Notify($"MAIL: Mail Alias '{shown}' not found.");
            return false;
        }
        if (!actor.IsWizard && alias.Owner != actor.Id)
        {
            context.Notify("Permission denied.");
            return false;
        }
        var members = Collect(context, db, actor, shown, context.RightArg, alias, out bool capped);
        if (capped)
            context.Notify("MAIL: Alias list is restricted to maximal 100 entries!");
        if (members.Count == 0)
        {
            context.Notify("MAIL: No valid recipients for alias-list!");
            return false;
        }
        alias.Members.AddRange(members);
        context.Notify($"MAIL: Alias set '{shown}' redefined.");
        return true;
    }

    private bool Remove(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        string shown = context.LeftArg.Trim();
        var alias = _aliases.Find(actor, shown);
        if (alias == null)
        {
            context.Notify($"MAIL: Mail Alias '{shown}' not found.");
            return false;
        }
        if (!actor.IsWizard && alias.Owner != actor.Id)
        {
            context.Notify("Permission denied.");
            return false;
        }
        foreach (string token in MailAliasTable.Tokens(context.RightArg ?? ""))
        {
            if (token.Length == 0)
                continue;
            var player = MailAliasTable.ResolvePlayer(db, actor, token);
            if (player == null)
            {
                context.Notify($"MAIL: No such player '{token}'.");
                continue;
            }
            int index = MailAliasTable.MemberIndex(alias, player.Id);
            if (index == 0)
            {
                context.Notify($"MAIL: player '{token}' is not in alias {shown}.");
                continue;
            }
            int last = alias.Members.Count - 1;
            alias.Members[index - 1] = alias.Members[last];
            alias.Members.RemoveAt(last);
            context.Notify($"MAIL: {MailAliasTable.Unparse(player)} removed from alias {shown}");
        }
        context.Notify($"MAIL: Alias set '{shown}' redefined.");
        return true;
    }

    private bool Describe(CommandContext context, MUSHObject actor)
    {
        var alias = _aliases.Find(actor, context.LeftArg.Trim());
        if (alias == null)
        {
            context.Notify($"MAIL: Alias {context.LeftArg.Trim()} not found.");
            return false;
        }
        if (!actor.IsWizard && alias.Owner != actor.Id)
        {
            context.Notify("MAIL: Permission denied.");
            return false;
        }
        alias.Description = context.RightArg;
        context.Notify("MAIL: Description changed.");
        return true;
    }

    private bool Rename(CommandContext context, MUSHObject actor)
    {
        var alias = _aliases.Find(actor, context.LeftArg.Trim());
        if (alias == null)
        {
            context.Notify("MAIL: I cannot find that alias!");
            return false;
        }
        string next = context.RightArg.Trim();
        if (next.Length == 0 || next[0] != MailAliasTable.Token)
        {
            context.Notify($"MAIL: Bad alias. Aliases must start with '{MailAliasTable.Token}'.");
            return false;
        }
        if (_aliases.FindAny(next) != null)
        {
            context.Notify("MAIL: That name already exists!");
            return false;
        }
        if (!actor.IsWizard && alias.Owner != actor.Id)
        {
            context.Notify("MAIL: Permission denied.");
            return false;
        }
        alias.Name = next[1..];
        context.Notify("MAIL: Mail Alias renamed.");
        return true;
    }

    private bool Stats(CommandContext context, MUSHObject actor)
    {
        if (!actor.IsRoyalty)
        {
            context.Notify("MAIL: Permission denied.");
            return false;
        }
        context.Notify($"MAIL: Number of mail aliases defined: {_aliases.Count}");
        context.Notify($"MAIL: Allocated slots {_aliases.Capacity}");
        return true;
    }

    private bool Chown(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        var alias = _aliases.Find(actor, context.LeftArg.Trim());
        if (alias == null)
        {
            context.Notify($"MAIL: Alias {context.LeftArg.Trim()} not found.");
            return false;
        }
        if (!actor.IsWizard)
        {
            context.Notify("MAIL: You cannot do that!");
            return false;
        }
        var next = MailAliasTable.ResolvePlayer(db, actor, context.RightArg.Trim());
        if (next == null)
        {
            context.Notify("MAIL: I cannot find that player.");
            return false;
        }
        alias.Owner = next.Id;
        context.Notify("MAIL: Owner changed for alias.");
        return true;
    }

    private bool Privs(CommandContext context, MUSHObject actor, bool members)
    {
        var alias = _aliases.Find(actor, context.LeftArg.Trim());
        if (alias == null)
        {
            context.Notify("MAIL: I cannot find that alias!");
            return false;
        }
        if (!actor.IsWizard && alias.Owner != actor.Id)
        {
            context.Notify("MAIL: Permission denied.");
            return false;
        }
        int flags = MailAliasTable.ParsePrivs(context.RightArg);
        if (members)
            alias.MemberFlags = flags;
        else
            alias.NameFlags = flags;
        context.Notify($"MAIL: Permission to see/use alias '{context.LeftArg.Trim()}' changed to {MailAliasTable.PrivsText(flags)}");
        return true;
    }

    private bool Nuke(CommandContext context, MUSHObject actor)
    {
        if (!actor.IsGod)
        {
            context.Notify("MAIL: Only god can do that!");
            return false;
        }
        _aliases.Clear();
        context.Notify("MAIL: All mail aliases destroyed!");
        return true;
    }

    private MailAlias? Require(CommandContext context, MUSHObject actor)
    {
        var alias = _aliases.Find(actor, context.LeftArg.Trim());
        if (alias == null)
            context.Notify($"MAIL: Not a valid alias. Remember to prefix the alias name with {MailAliasTable.Token}.");
        return alias;
    }

    private List<Dbref> Collect(
        CommandContext context,
        ObjectDatabase db,
        MUSHObject actor,
        string aliasName,
        string list,
        MailAlias? existing,
        out bool capped)
    {
        var members = new List<Dbref>();
        capped = false;
        foreach (string token in MailAliasTable.Tokens(list ?? ""))
        {
            if (members.Count == MailAliasTable.MaxBatch)
            {
                capped = true;
                break;
            }
            if (token.Length == 0)
                continue;
            var player = MailAliasTable.ResolvePlayer(db, actor, token);
            if (player == null)
            {
                context.Notify($"MAIL: No such player '{token}'.");
                continue;
            }
            if (existing != null && MailAliasTable.IsMember(existing, player.Id))
            {
                context.Notify($"MAIL: player '{token}' exists already in alias {aliasName}.");
                continue;
            }
            context.Notify($"MAIL: {MailAliasTable.Unparse(player)} added to alias {aliasName}");
            members.Add(player.Id);
        }
        return members;
    }

    private static string Shown(ObjectDatabase db, Dbref id)
    {
        var obj = db.GetObject(id);
        return obj == null ? id.ToString() : MailAliasTable.ShownName(obj);
    }

    private static string Fit(string text, int width)
    {
        if (text.Length > width)
            text = text[..width];
        return text.PadRight(width);
    }

    private static bool Has(CommandContext context, params string[] names)
    {
        foreach (string name in names)
        {
            if (context.Switches.Contains(name))
                return true;
        }
        return false;
    }
}
