namespace PennMUSH.Core.Commands;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public enum HookSlot
{
    Before,
    After,
    Ignore,
    Override,
    Extend
}

/// <summary>
/// One @hook slot. /inline and /inplace are stored for the list. The attribute runs immediately.
/// </summary>
public sealed class CommandHook
{
    public Dbref Object { get; set; } = Dbref.Nothing;
    public string? Attribute { get; set; }
    public bool Inplace { get; set; }
    public bool Inline { get; set; }
    public bool NoBreak { get; set; }
    public bool Localize { get; set; }
    public bool ClearRegs { get; set; }

    public string InplaceText()
    {
        if (Inplace)
            return "/inplace";
        if (!Inline)
            return "";
        var text = "/inline";
        if (NoBreak)
            text += "/nobreak";
        if (Localize)
            text += "/localize";
        if (ClearRegs)
            text += "/clearregs";
        return text;
    }
}

/// <summary>
/// Facts @command and @hook keep for one command. They stay in memory.
/// </summary>
public sealed class CommandFacts
{
    public bool UserAdded { get; set; }
    public bool Disabled { get; set; }
    public string Restrictions { get; set; } = "";
    public List<string> ParseFlags { get; } = new();
    public Dictionary<HookSlot, CommandHook> Hooks { get; } = new();
}

public sealed class CommandCatalog
{
    private readonly Dictionary<string, CommandFacts> _facts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string[] RestrictionNames = ["nobody", "noguest", "nofixed", "nogagged", "god", "wizard", "admin"];

    public CommandFacts Facts(string canonical)
    {
        if (!_facts.TryGetValue(canonical, out var facts))
        {
            facts = new CommandFacts();
            _facts[canonical] = facts;
        }
        return facts;
    }

    public bool IsDisabled(string canonical) =>
        _facts.TryGetValue(canonical, out var facts) && facts.Disabled;

    public bool TryFacts(string canonical, out CommandFacts facts) =>
        _facts.TryGetValue(canonical, out facts!);

    public IEnumerable<string> HookedCommands()
    {
        return _facts
            .Where(pair => pair.Value.Hooks.Values.Any(hook => hook.Object.IsValid))
            .Select(pair => pair.Key)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
    }

    public void Copy(string from, string to)
    {
        if (!_facts.TryGetValue(from, out var source))
            return;
        var dest = Facts(to);
        dest.UserAdded = source.UserAdded;
        dest.Disabled = source.Disabled;
        dest.Restrictions = source.Restrictions;
        dest.ParseFlags.Clear();
        dest.ParseFlags.AddRange(source.ParseFlags);
        dest.Hooks.Clear();
        foreach (var (slot, hook) in source.Hooks)
        {
            dest.Hooks[slot] = new CommandHook
            {
                Object = hook.Object,
                Attribute = hook.Attribute,
                Inplace = hook.Inplace,
                Inline = hook.Inline,
                NoBreak = hook.NoBreak,
                Localize = hook.Localize,
                ClearRegs = hook.ClearRegs
            };
        }
    }

    public void Forget(string canonical) => _facts.Remove(canonical);

    public static bool GoodName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        char first = name[0];
        if ("\"':;\\[]#&".Contains(first))
            return false;
        if (!char.IsUpper(first) && !char.IsDigit(first) && !char.IsPunctuation(first))
            return false;
        bool letter = false;
        foreach (char ch in name)
        {
            if (char.IsWhiteSpace(ch) || char.IsControl(ch))
                return false;
            if (char.IsUpper(ch))
                letter = true;
        }
        return letter;
    }

    public static bool TryRestrictions(string text, out List<string> names)
    {
        names = new List<string>();
        foreach (string raw in text.Split(new[] { ' ', '&', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string name = raw.TrimStart('!').ToLowerInvariant();
            if (!RestrictionNames.Contains(name))
                return false;
            names.Add(name);
        }
        return names.Count > 0;
    }

    /// <summary>
    /// Runs ignore, extend, override, and before. Returns false when the builtin must not run.
    /// </summary>
    public bool Prepare(CommandDispatcher dispatcher, string canonical, CommandContext context, ObjectDatabase db)
    {
        if (!TryFacts(canonical, out var facts))
            return true;

        if (ExpressionIsFalse(dispatcher, facts, HookSlot.Ignore, context, db))
            return false;

        if (context.Switches.Count > 0 && RunPattern(dispatcher, facts, HookSlot.Extend, context, db))
            return false;

        if (RunPattern(dispatcher, facts, HookSlot.Override, context, db))
            return false;

        RunExpression(dispatcher, facts, HookSlot.Before, context, db);
        return true;
    }

    public void After(CommandDispatcher dispatcher, string canonical, CommandContext context, ObjectDatabase db)
    {
        if (TryFacts(canonical, out var facts))
            RunExpression(dispatcher, facts, HookSlot.After, context, db);
    }

    private static bool ExpressionIsFalse(CommandDispatcher dispatcher, CommandFacts facts, HookSlot slot, CommandContext context, ObjectDatabase db)
    {
        string? value = Read(facts, slot, db);
        if (value == null)
            return false;
        string result = Evaluate(dispatcher, facts.Hooks[slot], value, context, db);
        return !IsTrue(result);
    }

    private static void RunExpression(CommandDispatcher dispatcher, CommandFacts facts, HookSlot slot, CommandContext context, ObjectDatabase db)
    {
        string? value = Read(facts, slot, db);
        if (value != null)
            Evaluate(dispatcher, facts.Hooks[slot], value, context, db);
    }

    private static bool RunPattern(CommandDispatcher dispatcher, CommandFacts facts, HookSlot slot, CommandContext context, ObjectDatabase db)
    {
        if (!facts.Hooks.TryGetValue(slot, out var hook) || !hook.Object.IsValid)
            return false;
        var obj = db.GetObject(hook.Object);
        if (obj == null || obj.Type == ObjectType.Garbage)
            return false;

        string raw = string.IsNullOrEmpty(context.RawCommand) ? context.CommandName : context.RawCommand;
        if (!string.IsNullOrEmpty(hook.Attribute))
        {
            var attr = obj.GetAttribute(hook.Attribute, db);
            return attr != null && MatchLines(dispatcher, hook, obj, attr.Value, raw, context, db);
        }

        foreach (var attr in obj.Attributes)
        {
            if (MatchLines(dispatcher, hook, obj, attr.Value, raw, context, db))
                return true;
        }
        return false;
    }

    private static bool MatchLines(CommandDispatcher dispatcher, CommandHook hook, MUSHObject obj, string body, string raw, CommandContext context, ObjectDatabase db)
    {
        foreach (string line in body.Replace("\r\n", "\n").Split('\n', ';'))
        {
            string text = line.Trim();
            if (!text.StartsWith('$'))
                continue;
            int colon = text.IndexOf(':');
            if (colon <= 1)
                continue;
            string pattern = text[1..colon].Trim();
            string action = text[(colon + 1)..].Trim();
            if (!PatternMatcher.Match(pattern, raw.Trim(), out _))
                continue;
            if (!string.IsNullOrWhiteSpace(action))
            {
                action = ApplyRegisters(dispatcher, hook, action, context, obj.Id);
                var inner = dispatcher.DispatchHook(obj.Id, action, db, context.Enactor);
                foreach (var note in inner.TargetedNotifications)
                    context.TargetedNotifications.Add(note);
                foreach (var note in inner.Notifications)
                    context.Notifications.Add(note);
            }
            return true;
        }
        return false;
    }

    private static string? Read(CommandFacts facts, HookSlot slot, ObjectDatabase db)
    {
        if (!facts.Hooks.TryGetValue(slot, out var hook) || !hook.Object.IsValid || string.IsNullOrEmpty(hook.Attribute))
            return null;
        var obj = db.GetObject(hook.Object);
        if (obj == null || obj.Type == ObjectType.Garbage)
            return null;
        return obj.GetAttribute(hook.Attribute, db)?.Value;
    }

    private static string Evaluate(CommandDispatcher dispatcher, CommandHook hook, string value, CommandContext context, ObjectDatabase db)
    {
        if (dispatcher.Evaluator == null)
            return value;
        return ApplyRegisters(dispatcher, hook, value, context, hook.Object);
    }

    private static string ApplyRegisters(CommandDispatcher dispatcher, CommandHook hook, string value, CommandContext context, Dbref executor)
    {
        if (dispatcher.Evaluator == null)
            return value;
        var ctx = new EvaluationContext(executor, context.Enactor);
        if (!hook.ClearRegs)
        {
            foreach (var register in context.Registers)
                ctx.SetRegister(register.Key, register.Value);
        }

        string result = dispatcher.Evaluator.Evaluate(value, ctx);
        if (!hook.Localize && !hook.ClearRegs)
        {
            context.Registers.Clear();
            foreach (var register in ctx.Registers)
                context.Registers[register.Key] = register.Value;
        }

        return result;
    }

    public static bool IsTrue(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        string trimmed = text.TrimStart();
        if (trimmed.StartsWith("#-", StringComparison.Ordinal))
            return false;
        if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
            && trimmed.TrimEnd().Length == trimmed.Length)
            return number != 0;
        return text.Any(ch => !char.IsWhiteSpace(ch));
    }
}
