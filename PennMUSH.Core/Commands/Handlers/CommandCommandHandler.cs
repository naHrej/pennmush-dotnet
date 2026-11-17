namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Linq;
using PennMUSH.Core.Database;

public sealed class StubCommandHandler : ICommandHandler
{
    public StubCommandHandler(string name) => Name = name;

    public string Name { get; }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        context.Notify("This command has not been implemented.");
        return true;
    }
}

public sealed class DelegatingCommandHandler : ICommandHandler
{
    private readonly ICommandHandler _inner;

    public DelegatingCommandHandler(string name, ICommandHandler inner)
    {
        Name = name;
        _inner = inner;
    }

    public string Name { get; }

    public bool Execute(CommandContext context, ObjectDatabase db) => _inner.Execute(context, db);
}

public class CommandCommandHandler : ICommandHandler
{
    private readonly CommandDispatcher _dispatcher;

    public CommandCommandHandler(CommandDispatcher dispatcher) => _dispatcher = dispatcher;

    public string Name => "@command";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        string left = (context.LeftArg.Length > 0 ? context.LeftArg : context.Arguments).Trim();
        if (left.Length == 0)
        {
            context.Notify("You must specify a command.");
            return false;
        }

        bool quiet = context.Switches.Contains("quiet");
        if (context.Switches.Contains("add"))
        {
            if (!actor.IsWizard)
            {
                context.Notify("Permission denied.");
                return false;
            }
            if (context.Switches.Contains("noeval"))
                context.Notify("WARNING: /NOEVAL no longer creates a Noparse command.\n         Use /NOPARSE if that's what you meant.");
            Add(context, left);
            return true;
        }

        if (context.Switches.Contains("alias"))
        {
            if (!actor.IsWizard)
            {
                context.Notify("Permission denied.");
                return false;
            }
            Alias(context, left, context.RightArg.Trim(), quiet);
            return true;
        }

        if (context.Switches.Contains("clone"))
        {
            if (!actor.IsWizard)
            {
                context.Notify("Permission denied.");
                return false;
            }
            Clone(context, left, context.RightArg.Trim());
            return true;
        }

        if (context.Switches.Contains("delete"))
        {
            if (!actor.IsGod)
            {
                context.Notify("Permission denied.");
                return false;
            }
            Delete(context, left);
            return true;
        }

        var lookup = _dispatcher.Table.Lookup(left);
        if (lookup.Status != CommandLookupStatus.Found || lookup.Handler == null || lookup.CanonicalName == null)
        {
            context.Notify("No such command.");
            return false;
        }

        string canonical = lookup.CanonicalName;
        if (actor.IsWizard)
        {
            var facts = _dispatcher.Catalog.Facts(canonical);
            if (context.Switches.Contains("on") || context.Switches.Contains("enable"))
                facts.Disabled = false;
            else if (context.Switches.Contains("off") || context.Switches.Contains("disable"))
            {
                if (canonical.Equals("@command", StringComparison.OrdinalIgnoreCase))
                {
                    context.Notify("@command is ALWAYS enabled.");
                    facts.Disabled = false;
                }
                else
                    facts.Disabled = true;
            }

            if (context.Switches.Contains("restrict"))
            {
                if (string.IsNullOrWhiteSpace(context.RightArg))
                {
                    context.Notify("How do you want to restrict the command?");
                    return false;
                }
                if (!CommandCatalog.TryRestrictions(context.RightArg, out var names))
                {
                    context.Notify("Restrict attempt failed.");
                    return false;
                }
                facts.Restrictions = string.Join(" ", names);
                foreach (string name in names)
                    _dispatcher.AddRestriction(canonical, name, null);
            }
        }

        if (!quiet)
            Report(context, lookup.Handler, canonical);
        return true;
    }

    private void Add(CommandContext context, string rawName)
    {
        string name = rawName.ToUpperInvariant();
        if (!CommandCatalog.GoodName(name))
        {
            context.Notify("Bad command name.");
            return;
        }
        if (_dispatcher.Table.ContainsExact(name))
        {
            context.Notify($"Command {name} already exists.");
            return;
        }

        var handler = new StubCommandHandler(name);
        _dispatcher.Register(handler);
        var facts = _dispatcher.Catalog.Facts(name);
        facts.UserAdded = true;
        RememberFlags(context, facts);
        context.Notify($"Command {name} added.");
    }

    private void Alias(CommandContext context, string command, string alias, bool quiet)
    {
        string target = alias.ToUpperInvariant();
        if (!CommandCatalog.GoodName(target))
        {
            context.Notify("I can't alias a command to that!");
            return;
        }
        if (!_dispatcher.Table.ContainsExact(command) || _dispatcher.Table.ContainsExact(target) || !_dispatcher.AddAlias(command, target))
        {
            context.Notify("Unable to set alias.");
            return;
        }
        if (!quiet)
            context.Notify("Alias set.");
    }

    private void Clone(CommandContext context, string original, string clone)
    {
        string source = original.ToUpperInvariant();
        string copy = clone.ToUpperInvariant();
        var lookup = _dispatcher.Table.Lookup(source);
        if (lookup.Status != CommandLookupStatus.Found || lookup.Handler == null || lookup.CanonicalName == null
            || !_dispatcher.Table.ContainsExact(source))
        {
            context.Notify("No such command.");
            return;
        }
        if (!CommandCatalog.GoodName(copy) || _dispatcher.Table.ContainsExact(copy))
        {
            context.Notify("Bad command name.");
            return;
        }

        _dispatcher.Register(new DelegatingCommandHandler(copy, lookup.Handler));
        _dispatcher.Catalog.Copy(lookup.CanonicalName, copy);
        _dispatcher.Catalog.Facts(copy).UserAdded = _dispatcher.Catalog.Facts(lookup.CanonicalName).UserAdded;
        context.Notify("Command cloned.");
    }

    private void Delete(CommandContext context, string rawName)
    {
        string name = rawName.ToUpperInvariant();
        if (!_dispatcher.Table.ContainsExact(name))
        {
            context.Notify("No such command.");
            return;
        }

        var lookup = _dispatcher.Table.Lookup(name);
        string canonical = lookup.CanonicalName ?? name;
        bool primary = canonical.Equals(name, StringComparison.OrdinalIgnoreCase);
        if (primary && !_dispatcher.Catalog.Facts(canonical).UserAdded)
        {
            context.Notify("You can't delete built-in commands. @command/disable instead.");
            return;
        }

        int names = _dispatcher.Table.CountNames(canonical);
        _dispatcher.Unregister(name);
        if (primary)
            _dispatcher.Catalog.Forget(canonical);
        if (primary && names > 1)
            context.Notify($"Removed {name} and aliases from command table.");
        else
            context.Notify($"Removed {name} from command table.");
    }

    private static void RememberFlags(CommandContext context, CommandFacts facts)
    {
        if (context.Switches.Contains("eqsplit"))
            facts.ParseFlags.Add("Eqsplit");
        if (context.Switches.Contains("lsargs"))
            facts.ParseFlags.Add("Args");
        if (context.Switches.Contains("rsargs"))
            facts.ParseFlags.Add("RsArgs");
        if (context.Switches.Contains("noparse"))
            facts.ParseFlags.Add("Noparse");
        if (context.Switches.Contains("rsnoparse"))
            facts.ParseFlags.Add("RsNoparse");
    }

    private void Report(CommandContext context, ICommandHandler handler, string canonical)
    {
        var facts = _dispatcher.Catalog.TryFacts(canonical, out var found) ? found : new CommandFacts();
        string state = facts.Disabled ? "Disabled" : "Enabled";
        context.Notify($"Name       : {handler.Name.ToUpperInvariant()} ({state})");
        context.Notify($"Flags      : {string.Join(" ", facts.ParseFlags)}");
        context.Notify($"Lock       : {(string.IsNullOrEmpty(facts.Restrictions) ? "1" : facts.Restrictions)}");
        context.Notify("Switches   :");
        if (facts.ParseFlags.Contains("Eqsplit"))
        {
            string left = string.Join(" ", facts.ParseFlags.Where(flag => flag is "Args" or "Noparse"));
            string right = string.Join(" ", facts.ParseFlags.Where(flag => flag is "RsArgs" or "RsNoparse"));
            context.Notify($"Leftside   : {left}");
            context.Notify($"Rightside  : {right}");
        }
    }
}
