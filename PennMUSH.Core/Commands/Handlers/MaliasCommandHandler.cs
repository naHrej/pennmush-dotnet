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
                context.Notify(Feedback.MailInvalidMaliasCommand);
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
        context.Notify(Feedback.EndOfMailAliases);
        return true;
    }

    private bool All(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        if (!actor.IsRoyalty)
            return List(context, db, actor);

        context.Notify(Feedback.NumNameDescriptionOwnerCount);
        for (int i = 0; i < _aliases.Aliases.Count; i++)
        {
            var alias = _aliases.Aliases[i];
            context.Notify($"#{i,-4} +{Fit(alias.Name, 10)} {Fit(alias.Description, 40)} {Fit(Shown(db, alias.Owner), 11)} ({alias.Members.Count,3})");
        }
        context.Notify(Feedback.EndOfMailAliases2);
        return true;
    }

    private bool Members(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        var alias = _aliases.Find(actor, context.LeftArg.Trim());
        if (alias == null)
        {
            context.Notify(Feedback.MailAliasNotFound, ("alias", $"{context.LeftArg.Trim()}"));
            return false;
        }
        if (!MailAliasTable.CanSeeMembers(actor, alias))
        {
            context.Notify(Feedback.MailPermissionDenied);
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
            context.Notify(Feedback.MailOnlyPlayersMayCreateMailAliases);
            return false;
        }
        string aliasName = context.LeftArg.Trim();
        string list = context.RightArg;
        if (aliasName.Length == 0 || string.IsNullOrEmpty(list))
        {
            context.Notify(Feedback.MailWhatAliasDoYouWantToCreate);
            return false;
        }
        if (!MailAliasTable.LegalName(aliasName, out string error))
        {
            context.Notify(error);
            return false;
        }
        if (_aliases.FindAny(aliasName) != null)
        {
            context.Notify(Feedback.MailMailAliasAlreadyExists, ("alias", $"{aliasName}"));
            return false;
        }

        var members = Collect(context, db, actor, aliasName, list, existing: null, out bool capped);
        if (capped)
            context.Notify(Feedback.MailAliasListIsRestrictedToMaximal100Entries);
        if (members.Count == 0)
        {
            context.Notify(Feedback.MailNoValidRecipientsForAliasList);
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
        context.Notify(Feedback.MailAliasSetDefined, ("value", $"{aliasName}"));
        return true;
    }

    private bool Set(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        var alias = Require(context, actor);
        if (alias == null)
            return false;
        if (string.IsNullOrEmpty(context.RightArg))
        {
            context.Notify(Feedback.MailYouMustSetTheAliasToANonEmptyList);
            return false;
        }
        if (!actor.IsWizard && alias.Owner != actor.Id)
        {
            context.Notify(Feedback.MailPermissionDenied2);
            return false;
        }
        var members = Collect(context, db, actor, context.LeftArg.Trim(), context.RightArg, null, out bool capped);
        if (capped)
            context.Notify(Feedback.MailAliasListIsRestrictedToMaximal100Entries);
        if (members.Count == 0)
        {
            context.Notify(Feedback.MailNoValidRecipientsForAliasList);
            return false;
        }
        alias.ReplaceMembers(members);
        context.Notify(Feedback.MailAliasListSet);
        return true;
    }

    private bool Destroy(CommandContext context, MUSHObject actor)
    {
        var alias = _aliases.Find(actor, context.LeftArg.Trim());
        if (alias == null)
        {
            context.Notify(Feedback.MailNotAValidAliasRememberToPrefixTheAliasNameWith, ("value", $"{MailAliasTable.Token}"));
            return false;
        }
        if (!actor.IsWizard && alias.Owner != actor.Id)
        {
            context.Notify(Feedback.MailPermissionDenied2);
            return false;
        }
        context.Notify(Feedback.MailAliasDestroyed);
        _aliases.RemoveAt(_aliases.IndexOf(alias));
        return true;
    }

    private bool Add(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        string shown = context.LeftArg.Trim();
        var alias = _aliases.Find(actor, shown);
        if (alias == null)
        {
            context.Notify(Feedback.MailMailAliasNotFound, ("alias", $"{shown}"));
            return false;
        }
        if (!actor.IsWizard && alias.Owner != actor.Id)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }
        var members = Collect(context, db, actor, shown, context.RightArg, alias, out bool capped);
        if (capped)
            context.Notify(Feedback.MailAliasListIsRestrictedToMaximal100Entries);
        if (members.Count == 0)
        {
            context.Notify(Feedback.MailNoValidRecipientsForAliasList);
            return false;
        }
        alias.AddMembers(members);
        context.Notify(Feedback.MailAliasSetRedefined, ("value", $"{shown}"));
        return true;
    }

    private bool Remove(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        string shown = context.LeftArg.Trim();
        var alias = _aliases.Find(actor, shown);
        if (alias == null)
        {
            context.Notify(Feedback.MailMailAliasNotFound, ("alias", $"{shown}"));
            return false;
        }
        if (!actor.IsWizard && alias.Owner != actor.Id)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }
        foreach (string token in MailAliasTable.Tokens(context.RightArg ?? ""))
        {
            if (token.Length == 0)
                continue;
            var player = MailAliasTable.ResolvePlayer(db, actor, token);
            if (player == null)
            {
                context.Notify(Feedback.MailNoSuchPlayer, ("player", $"{token}"));
                continue;
            }
            int index = MailAliasTable.MemberIndex(alias, player.Id);
            if (index == 0)
            {
                context.Notify(Feedback.MailPlayerIsNotInAlias, ("player", $"{token}"), ("alias", $"{shown}"));
                continue;
            }
            alias.RemoveMemberSlot(index - 1);
            context.Notify(Feedback.MailRemovedFromAlias, ("value", $"{MailAliasTable.Unparse(player)}"), ("alias", $"{shown}"));
        }
        context.Notify(Feedback.MailAliasSetRedefined, ("value", $"{shown}"));
        return true;
    }

    private bool Describe(CommandContext context, MUSHObject actor)
    {
        var alias = _aliases.Find(actor, context.LeftArg.Trim());
        if (alias == null)
        {
            context.Notify(Feedback.MailAliasNotFound2, ("alias", $"{context.LeftArg.Trim()}"));
            return false;
        }
        if (!actor.IsWizard && alias.Owner != actor.Id)
        {
            context.Notify(Feedback.MailPermissionDenied);
            return false;
        }
        alias.Description = context.RightArg;
        context.Notify(Feedback.MailDescriptionChanged);
        return true;
    }

    private bool Rename(CommandContext context, MUSHObject actor)
    {
        var alias = _aliases.Find(actor, context.LeftArg.Trim());
        if (alias == null)
        {
            context.Notify(Feedback.MailICannotFindThatAlias);
            return false;
        }
        string next = context.RightArg.Trim();
        if (next.Length == 0 || next[0] != MailAliasTable.Token)
        {
            context.Notify(Feedback.MailBadAliasAliasesMustStartWith, ("value", $"{MailAliasTable.Token}"));
            return false;
        }
        if (_aliases.FindAny(next) != null)
        {
            context.Notify(Feedback.MailThatNameAlreadyExists);
            return false;
        }
        if (!actor.IsWizard && alias.Owner != actor.Id)
        {
            context.Notify(Feedback.MailPermissionDenied);
            return false;
        }
        alias.Name = next[1..];
        context.Notify(Feedback.MailMailAliasRenamed);
        return true;
    }

    private bool Stats(CommandContext context, MUSHObject actor)
    {
        if (!actor.IsRoyalty)
        {
            context.Notify(Feedback.MailPermissionDenied);
            return false;
        }
        context.Notify(Feedback.MailNumberOfMailAliasesDefined, ("value", $"{_aliases.Count}"));
        context.Notify(Feedback.MailAllocatedSlots, ("value", $"{_aliases.Capacity}"));
        return true;
    }

    private bool Chown(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        var alias = _aliases.Find(actor, context.LeftArg.Trim());
        if (alias == null)
        {
            context.Notify(Feedback.MailAliasNotFound2, ("alias", $"{context.LeftArg.Trim()}"));
            return false;
        }
        if (!actor.IsWizard)
        {
            context.Notify(Feedback.MailYouCannotDoThat);
            return false;
        }
        var next = MailAliasTable.ResolvePlayer(db, actor, context.RightArg.Trim());
        if (next == null)
        {
            context.Notify(Feedback.MailICannotFindThatPlayer);
            return false;
        }
        alias.Owner = next.Id;
        context.Notify(Feedback.MailOwnerChangedForAlias);
        return true;
    }

    private bool Privs(CommandContext context, MUSHObject actor, bool members)
    {
        var alias = _aliases.Find(actor, context.LeftArg.Trim());
        if (alias == null)
        {
            context.Notify(Feedback.MailICannotFindThatAlias);
            return false;
        }
        if (!actor.IsWizard && alias.Owner != actor.Id)
        {
            context.Notify(Feedback.MailPermissionDenied);
            return false;
        }
        int flags = MailAliasTable.ParsePrivs(context.RightArg);
        if (members)
            alias.MemberFlags = flags;
        else
            alias.NameFlags = flags;
        context.Notify(Feedback.MailPermissionToSeeUseAliasChangedTo, ("alias", $"{context.LeftArg.Trim()}"), ("value", $"{MailAliasTable.PrivsText(flags)}"));
        return true;
    }

    private bool Nuke(CommandContext context, MUSHObject actor)
    {
        if (!actor.IsGod)
        {
            context.Notify(Feedback.MailOnlyGodCanDoThat);
            return false;
        }
        _aliases.Clear();
        context.Notify(Feedback.MailAllMailAliasesDestroyed);
        return true;
    }

    private MailAlias? Require(CommandContext context, MUSHObject actor)
    {
        var alias = _aliases.Find(actor, context.LeftArg.Trim());
        if (alias == null)
            context.Notify(Feedback.MailNotAValidAliasRememberToPrefixTheAliasNameWith, ("value", $"{MailAliasTable.Token}"));
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
                context.Notify(Feedback.MailNoSuchPlayer, ("player", $"{token}"));
                continue;
            }
            if (existing != null && MailAliasTable.IsMember(existing, player.Id))
            {
                context.Notify(Feedback.MailPlayerExistsAlreadyInAlias, ("player", $"{token}"), ("alias", $"{aliasName}"));
                continue;
            }
            context.Notify(Feedback.MailAddedToAlias, ("value", $"{MailAliasTable.Unparse(player)}"), ("alias", $"{aliasName}"));
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
