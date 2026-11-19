namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Linq;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class HookCommandHandler : ICommandHandler
{
    private readonly CommandDispatcher _dispatcher;

    public HookCommandHandler(CommandDispatcher dispatcher) => _dispatcher = dispatcher;

    public string Name => "@hook";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;
        if (!actor.IsWizard && !actor.HasPower("HOOK"))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        HookSlot? slot = Slot(context);
        bool inplace = context.Switches.Contains("inplace");
        bool inline = context.Switches.Contains("inline");
        if ((inplace || inline || context.Switches.Contains("nobreak") || context.Switches.Contains("localize") || context.Switches.Contains("clearregs"))
            && slot is not (HookSlot.Override or HookSlot.Extend))
        {
            context.Notify(Feedback.YouCanOnlyUseInplaceAndInlineWithOverrideOrExtend);
            return false;
        }

        if (context.Switches.Contains("list") && slot == null)
        {
            List(context, db, context.LeftArg.Trim());
            return true;
        }

        if (slot == null)
        {
            context.Notify(Feedback.YouMustGiveASwitchForHook);
            return false;
        }

        string command = (context.LeftArg.Length > 0 ? context.LeftArg : context.Arguments).Trim();
        var lookup = _dispatcher.Table.Lookup(command);
        if (lookup.Status != CommandLookupStatus.Found || lookup.CanonicalName == null)
        {
            context.Notify(Feedback.NoSuchCommand);
            return false;
        }

        if (lookup.CanonicalName.Equals("@password", StringComparison.OrdinalIgnoreCase)
            || lookup.CanonicalName.Equals("@newpassword", StringComparison.OrdinalIgnoreCase))
        {
            context.Notify(Feedback.HooksNotAllowedWithThatCommand);
            return false;
        }

        var facts = _dispatcher.Catalog.Facts(lookup.CanonicalName);
        string shown = lookup.Handler?.Name.ToUpperInvariant() ?? lookup.CanonicalName.ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(context.RightArg))
        {
            facts.Hooks.Remove(slot.Value);
            context.Notify(Feedback.HookRemovedFrom, ("value", $"{shown}"));
            return true;
        }

        string spec = context.RightArg.Trim();
        int slash = spec.IndexOf('/');
        string objectName = slash >= 0 ? spec[..slash] : spec;
        string? attr = slash >= 0 ? spec[(slash + 1)..] : null;
        if (string.IsNullOrWhiteSpace(objectName)
            || ((slot is not (HookSlot.Override or HookSlot.Extend)) && string.IsNullOrWhiteSpace(attr)))
        {
            context.Notify(slot is HookSlot.Override or HookSlot.Extend
                ? "You must give an object."
                : "You must give both an object and attribute.");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(attr) && !AttributeEntry.IsValidName(attr))
        {
            context.Notify(Feedback.YouMustGiveBothAnObjectAndAttribute);
            return false;
        }

        var match = NearbyMatch.Find(db, actor, objectName.Trim(), out bool ambiguous);
        if (match == null || ambiguous || match.Type == ObjectType.Garbage)
        {
            context.Notify(Feedback.InvalidHookObject);
            return false;
        }

        facts.Hooks[slot.Value] = new CommandHook
        {
            Object = match.Id,
            Attribute = string.IsNullOrWhiteSpace(attr) ? null : attr.ToUpperInvariant(),
            Inplace = inplace,
            Inline = inline && !inplace,
            NoBreak = context.Switches.Contains("nobreak"),
            Localize = context.Switches.Contains("localize"),
            ClearRegs = context.Switches.Contains("clearregs")
        };
        context.Notify(Feedback.HookSetFor, ("value", $"{shown}"));
        return true;
    }

    private void List(CommandContext context, ObjectDatabase db, string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            var names = _dispatcher.Catalog.HookedCommands().Select(name => name.ToUpperInvariant()).Distinct().ToList();
            if (names.Count == 0)
            {
                context.Notify(Feedback.ThereAreNoHooksCurrentlySet);
                return;
            }
            string list = names.Count == 1
                ? names[0]
                : string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1];
            context.Notify(Feedback.TheFollowingCommandsHaveHooks, ("value", list));
            return;
        }

        var lookup = _dispatcher.Table.Lookup(command);
        if (lookup.Status != CommandLookupStatus.Found || lookup.CanonicalName == null)
        {
            context.Notify(Feedback.NoSuchCommand);
            return;
        }

        if (!_dispatcher.Catalog.TryFacts(lookup.CanonicalName, out var facts))
        {
            context.Notify(Feedback.ThatCommandHasNoHooks);
            return;
        }

        int count = 0;
        foreach (HookSlot slot in new[] { HookSlot.Before, HookSlot.After, HookSlot.Ignore, HookSlot.Override, HookSlot.Extend })
        {
            if (!facts.Hooks.TryGetValue(slot, out var hook) || !hook.Object.IsValid)
                continue;
            var obj = db.GetObject(hook.Object);
            if (obj == null || obj.Type == ObjectType.Garbage)
                continue;
            count++;
            string extra = slot is HookSlot.Override or HookSlot.Extend ? hook.InplaceText() : "";
            string attr = hook.Attribute ?? "";
            context.Notify(Feedback.HookValueValueValueValue, ("value", $"{slot.ToString().ToLowerInvariant()}"), ("value2", $"{extra}"), ("value3", $"{hook.Object.Value}"), ("value4", $"{attr}"));
        }
        if (count == 0)
            context.Notify(Feedback.ThatCommandHasNoHooks);
    }

    private static HookSlot? Slot(CommandContext context)
    {
        if (context.Switches.Contains("before"))
            return HookSlot.Before;
        if (context.Switches.Contains("after"))
            return HookSlot.After;
        if (context.Switches.Contains("ignore"))
            return HookSlot.Ignore;
        if (context.Switches.Contains("override"))
            return HookSlot.Override;
        if (context.Switches.Contains("extend") || context.Switches.Contains("igswitch"))
            return HookSlot.Extend;
        return null;
    }
}

internal static class NearbyMatch
{
    public static MUSHObject? Find(ObjectDatabase db, MUSHObject actor, string name, out bool ambiguous)
    {
        ambiguous = false;
        if (string.IsNullOrWhiteSpace(name))
            return null;
        if (name.Equals("me", StringComparison.OrdinalIgnoreCase))
            return actor;
        if (name.Equals("here", StringComparison.OrdinalIgnoreCase))
            return actor.Location.IsValid ? db.GetObject(actor.Location) : null;
        if (name.StartsWith('#') && int.TryParse(name[1..], out int number))
            return db.GetObject(new Dbref(number));

        var hits = new System.Collections.Generic.List<MUSHObject>();
        void Add(MUSHObject? obj)
        {
            if (obj != null && obj.Type != ObjectType.Garbage && obj.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                && hits.All(hit => hit.Id != obj.Id))
                hits.Add(obj);
        }

        if (actor.Location.IsValid)
        {
            Add(db.GetObject(actor.Location));
            foreach (var item in db.GetContents(actor.Location))
                Add(item);
        }
        foreach (var item in db.GetContents(actor.Id))
            Add(item);
        if (actor.IsWizard)
        {
            foreach (var obj in db.GetAllObjects())
                Add(obj);
        }

        if (hits.Count > 1)
            ambiguous = true;
        return hits.Count == 0 ? null : hits[0];
    }
}
