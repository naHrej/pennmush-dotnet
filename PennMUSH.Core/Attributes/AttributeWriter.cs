namespace PennMUSH.Core.Attributes;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public readonly record struct AttributeWriteResult(bool Ok, string Message)
{
    public static AttributeWriteResult Done(string message) => new(true, message);
    public static AttributeWriteResult Silent() => new(true, string.Empty);
    public static AttributeWriteResult Fail(string message) => new(false, message);
}

public static class AttributeWriter
{
    private const string BranchValue = " ";

    public static AttributeWriteResult Apply(MUSHObject actor, MUSHObject target, string rawName, string? value)
    {
        string name = rawName.Trim().ToUpperInvariant();
        if (!AttributeEntry.IsValidName(name))
            return AttributeWriteResult.Fail("That's not a very good name for an attribute.");

        bool clearing = string.IsNullOrEmpty(value);
        var leaf = target.GetAttribute(name);
        var prefixes = BranchPrefixes(name);

        if (clearing)
            return Clear(actor, target, name, leaf, prefixes);

        if (!StandardAttributes.AcceptsValue(name, value!, out string accepted, out string rejection))
            return AttributeWriteResult.Fail(rejection);
        value = accepted;

        if (leaf != null)
        {
            if (leaf.Flags.HasFlag(AttributeFlags.Safe))
                return Safe(name);
            if (!CanChange(actor, leaf))
                return Denied();
            foreach (string prefix in prefixes)
            {
                var branch = target.GetAttribute(prefix);
                if (branch == null)
                    return AttributeWriteResult.Fail($"You must set {prefix} first.");
                if (!CanChange(actor, branch))
                    return Denied();
            }

            target.WriteAttributeValue(name, value!, Creator(actor), leaf.Flags);
            return Announce(target, name, true, leaf.Flags);
        }

        foreach (string prefix in prefixes)
        {
            var branch = target.GetAttribute(prefix);
            if (branch == null)
            {
                if (!CanCreate(actor, StandardAttributes.DefaultFlags(prefix)))
                    return Denied();
            }
            else if (!CanChange(actor, branch))
            {
                return Denied();
            }
        }

        AttributeFlags created = StandardAttributes.DefaultFlags(name);
        if (!CanCreate(actor, created))
            return Denied();

        Dbref creator = Creator(actor);
        foreach (string prefix in prefixes)
        {
            if (target.GetAttribute(prefix) == null)
            {
                target.WriteAttributeValue(
                    prefix,
                    BranchValue,
                    creator,
                    StandardAttributes.DefaultFlags(prefix) | AttributeFlags.Root);
            }
            else
            {
                target.AddAttributeFlags(prefix, AttributeFlags.Root);
            }
        }

        target.WriteAttributeValue(name, value!, creator, created);
        return Announce(target, name, true, created);
    }

    private static AttributeWriteResult Clear(
        MUSHObject actor,
        MUSHObject target,
        string name,
        AttributeEntry? leaf,
        List<string> prefixes)
    {
        if (leaf == null)
            return AttributeWriteResult.Fail("No such attribute to reset.");
        if (leaf.Flags.HasFlag(AttributeFlags.Safe))
            return Safe(name);
        if (HasChildren(target, name))
            return AttributeWriteResult.Fail($"Unable to remove '{name}' because of a protected tree attribute.");
        if (!CanChange(actor, leaf))
            return Denied();

        foreach (string prefix in prefixes)
        {
            var branch = target.GetAttribute(prefix);
            if (branch == null)
                return AttributeWriteResult.Fail($"{prefix} is a branch attribute; remove its children first.");
            if (!CanChange(actor, branch))
                return Denied();
        }

        AttributeFlags flags = leaf.Flags;
        target.RemoveAttribute(name);
        if (prefixes.Count > 0)
        {
            string parent = prefixes[^1];
            if (!HasChildren(target, parent))
                target.RemoveAttributeFlags(parent, AttributeFlags.Root);
        }

        return Announce(target, name, false, flags);
    }

    private static AttributeWriteResult Announce(
        MUSHObject target,
        string name,
        bool set,
        AttributeFlags flags)
    {
        if (flags.HasFlag(AttributeFlags.Quiet))
            return AttributeWriteResult.Silent();
        string verb = set ? "Set" : "Cleared";
        return AttributeWriteResult.Done($"{target.Name}/{name} - {verb}.");
    }

    private static AttributeWriteResult Safe(string name)
        => AttributeWriteResult.Fail($"Attribute {name} is SAFE. Set it !SAFE to modify it.");

    private static AttributeWriteResult Denied()
        => AttributeWriteResult.Fail("That attribute cannot be changed by you.");

    private static bool CanCreate(MUSHObject actor, AttributeFlags flags)
    {
        if (actor.IsGod)
            return true;
        if (flags.HasFlag(AttributeFlags.Internal) || flags.HasFlag(AttributeFlags.NoDump))
            return false;
        if (flags.HasFlag(AttributeFlags.Wizard) && !actor.IsWizard)
            return false;
        return true;
    }

    private static bool CanChange(MUSHObject actor, AttributeEntry entry)
    {
        if (actor.IsGod)
            return true;
        if (entry.Flags.HasFlag(AttributeFlags.Internal) || entry.Flags.HasFlag(AttributeFlags.NoDump))
            return false;
        if (entry.Flags.HasFlag(AttributeFlags.Wizard) && !actor.IsWizard)
            return false;
        if (entry.Flags.HasFlag(AttributeFlags.Locked) && !actor.IsWizard)
        {
            Dbref creator = Creator(actor);
            if (entry.Creator != creator && entry.Creator != actor.Id)
                return false;
        }
        return true;
    }

    private static Dbref Creator(MUSHObject actor)
        => actor.Owner.IsValid ? actor.Owner : actor.Id;

    private static List<string> BranchPrefixes(string name)
    {
        var prefixes = new List<string>();
        int start = 0;
        while (true)
        {
            int tick = name.IndexOf('`', start);
            if (tick < 0)
                return prefixes;
            prefixes.Add(name.Substring(0, tick));
            start = tick + 1;
        }
    }

    public static bool HasChildren(MUSHObject target, string name)
    {
        string prefix = name + "`";
        foreach (var attr in target.Attributes)
        {
            if (attr.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
