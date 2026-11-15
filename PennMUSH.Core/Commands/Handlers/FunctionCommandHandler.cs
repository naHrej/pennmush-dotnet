namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Linq;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class FunctionCommandHandler : ICommandHandler
{
    private readonly FunctionRegistry _functions;

    public string Name => "@function";

    public FunctionCommandHandler(FunctionRegistry functions)
    {
        _functions = functions;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
        {
            context.Notify("Permission denied.");
            return false;
        }

        bool wizard = actor.IsWizard;
        bool global = wizard || actor.HasPower("Functions");
        string name = context.LeftArg.Trim();
        bool hasEquals = context.RawCommand.Contains('=');

        if (context.Switches.Contains("delete"))
            return Delete(context, db, actor, global, name);
        if (context.Switches.Contains("enable"))
            return Toggle(context, wizard, name, enabled: true);
        if (context.Switches.Contains("disable"))
            return Toggle(context, wizard, name, enabled: false);
        if (context.Switches.Contains("restrict"))
            return Restrict(context, wizard, name, hasEquals);
        if (context.Switches.Contains("alias"))
            return Alias(context, wizard, name, hasEquals);
        if (context.Switches.Contains("clone") || context.Switches.Contains("restore"))
        {
            context.Notify(wizard ? "That's not a builtin function." : "Permission denied.");
            return false;
        }

        if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(context.RightArg))
            return List(context, global);
        if (string.IsNullOrEmpty(context.RightArg))
            return Report(context, global, name);
        return Add(context, db, actor, global, name);
    }

    private bool List(CommandContext context, bool global)
    {
        var functions = _functions.ListSoft();
        if (functions.Count == 0)
        {
            context.Notify("No global user-defined functions exist.");
            return true;
        }

        if (!global)
        {
            string names = string.Join(" ", functions.Select(function => function.Name).OrderBy(item => item, StringComparer.Ordinal));
            context.Notify("User functions: " + names);
            return true;
        }

        context.Notify("Function Name                   Dbref #    Attrib");
        foreach (var function in functions)
            context.Notify($"{function.Name,-32} {function.Thing.Value,6}    {function.Attribute}");
        return true;
    }

    private bool Report(CommandContext context, bool global, string name)
    {
        var soft = _functions.FindSoft(name);
        if (soft != null)
        {
            context.Notify(Describe(soft.Name, soft.Gate, builtin: false, global ? $"#{soft.Thing.Value}/{soft.Attribute}" : null, soft.MinArgs, soft.MaxArgs));
            return true;
        }

        if (_functions.TryAlias(name, out string canonical) || _functions.HasBuiltin(name))
        {
            string shown = _functions.TryAlias(name, out canonical) ? canonical : name.ToUpperInvariant();
            var gate = _functions.BuiltinGate(shown);
            context.Notify(Describe(shown, gate, builtin: true, location: null, min: null, max: null));
            return true;
        }

        context.Notify("No such function.");
        return false;
    }

    private static string Describe(string name, FunctionGate gate, bool builtin, string? location, int? min, int? max)
    {
        string state = gate.Disabled ? "Disabled" : "Enabled";
        string kind = builtin ? " builtin" : " @function";
        string text = $"Name      : {name.ToUpperInvariant()}() ({state}{kind})\nFlags     : {FunctionRestrictions.Flags(gate)}";
        if (location != null)
            text += $"\nLocation  : {location}";
        if (min.HasValue && max.HasValue)
            text += min == max
                ? $"\nArguments : {min}"
                : $"\nArguments : {min} to {max}";
        return text;
    }

    private bool Add(CommandContext context, ObjectDatabase db, MUSHObject actor, bool global, string name)
    {
        if (!global)
        {
            context.Notify("Permission denied.");
            return false;
        }

        if (!TrySplit(context, out string objectName, out string attribute, out string? minText, out string? maxText, out string? restrictions, out bool extra))
            return false;
        if (string.IsNullOrWhiteSpace(objectName) || string.IsNullOrWhiteSpace(attribute))
        {
            context.Notify("You must specify an object and an attribute.");
            return false;
        }

        string canonical = name.ToUpperInvariant();
        if (!OkName(canonical))
        {
            context.Notify("Invalid function name.");
            return false;
        }

        var existing = _functions.FindSoft(canonical);
        if (existing == null && (_functions.HasBuiltin(canonical) || _functions.TryAlias(canonical, out _)))
        {
            context.Notify("You cannot change that built-in function.");
            return false;
        }
        if (existing == null && extra)
        {
            context.Notify("Expected between 1 and 5 arguments.");
            return false;
        }

        var target = Resolve(db, context.Executor, objectName);
        if (target == null)
        {
            context.Notify("I can't see that here.");
            return false;
        }
        if (!actor.IsWizard && target.Owner != actor.Id && target.Id != actor.Id)
        {
            context.Notify("No permission to examine object.");
            return false;
        }

        var function = existing ?? new SoftFunction { Name = canonical, MaxArgs = 10 };
        function.Thing = target.Id;
        function.Attribute = attribute.Trim().ToUpperInvariant();
        function.MinArgs = Clamp(minText, 0);
        function.MaxArgs = maxText == null ? 10 : Math.Abs(Clamp(maxText, 10));
        function.Gate = new FunctionGate();
        if (!string.IsNullOrWhiteSpace(restrictions))
            FunctionRestrictions.Apply(function.Gate, restrictions);
        _functions.SaveSoft(function);
        context.Notify(existing == null ? "Function added." : "Function updated.");
        return true;
    }

    private bool Alias(CommandContext context, bool wizard, string name, bool hasEquals)
    {
        if (!wizard)
        {
            context.Notify("Permission denied.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            context.Notify("What function do you want to alias?");
            return false;
        }
        if (!hasEquals || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify("What do you want to alias the function as?");
            return false;
        }

        string alias = context.RightArg.Trim().ToUpperInvariant();
        if (_functions.HasFunction(alias))
        {
            context.Notify("There's already a function with that name.");
            return false;
        }
        if (!OkName(alias))
        {
            context.Notify("Invalid function name.");
            return false;
        }
        if (_functions.FindSoft(name) != null)
        {
            context.Notify("You cannot alias @functions.");
            return false;
        }
        if (!_functions.HasBuiltin(name))
        {
            context.Notify("No such function.");
            return false;
        }

        _functions.SaveAlias(alias, name.ToUpperInvariant());
        context.Notify("Alias added.");
        return true;
    }

    private bool Restrict(CommandContext context, bool wizard, string name, bool hasEquals)
    {
        if (!wizard)
        {
            context.Notify("Permission denied.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            context.Notify("Restrict what function?");
            return false;
        }
        if (!hasEquals)
        {
            context.Notify("Do what with the function?");
            return false;
        }

        var soft = _functions.FindSoft(name);
        FunctionGate gate;
        string label;
        if (soft != null)
        {
            gate = soft.Gate;
            label = $"@function #{soft.Thing.Value}/{soft.Attribute}";
        }
        else if (_functions.TryAlias(name, out string canonical) || _functions.HasBuiltin(name))
        {
            string shown = _functions.HasBuiltin(name) ? name.ToUpperInvariant() : canonical;
            gate = _functions.BuiltinGate(shown);
            label = $"Builtin function {shown.ToUpperInvariant()}";
        }
        else
        {
            context.Notify("No such function.");
            return false;
        }

        bool changed = FunctionRestrictions.Apply(gate, context.RightArg);
        context.Notify(label + (changed ? " - Restrictions modified." : " - Restrictions unchanged."));
        return true;
    }

    private bool Toggle(CommandContext context, bool wizard, string name, bool enabled)
    {
        if (!wizard)
        {
            context.Notify("Permission denied.");
            return false;
        }
        if (_functions.TryAlias(name, out _))
        {
            context.Notify("You can't disable aliases.");
            return false;
        }

        var soft = _functions.FindSoft(name);
        if (soft != null)
        {
            soft.Gate.Disabled = !enabled;
            context.Notify(enabled ? "Enabled." : "Disabled.");
            return true;
        }
        if (_functions.HasBuiltin(name))
        {
            _functions.BuiltinGate(name.ToUpperInvariant()).Disabled = !enabled;
            context.Notify(enabled ? "Enabled." : "Disabled.");
            return true;
        }

        context.Notify("No such function.");
        return false;
    }

    private bool Delete(CommandContext context, ObjectDatabase db, MUSHObject actor, bool global, string name)
    {
        if (!global)
        {
            context.Notify("Permission denied.");
            return false;
        }
        if (_functions.TryAlias(name, out _))
        {
            _functions.RemoveAlias(name);
            context.Notify("Function alias deleted.");
            return true;
        }

        var soft = _functions.FindSoft(name);
        if (soft == null)
        {
            context.Notify(_functions.HasBuiltin(name) ? "You can't delete that @function." : "No such function.");
            return false;
        }

        var thing = db.GetObject(soft.Thing);
        if (!actor.IsWizard && (thing == null || thing.Owner != actor.Id))
        {
            context.Notify("You can't delete that @function.");
            return false;
        }

        _functions.RemoveSoft(soft.Name);
        context.Notify("Function deleted.");
        return true;
    }

    private static bool TrySplit(CommandContext context, out string objectName, out string attribute, out string? min, out string? max, out string? restrictions, out bool extra)
    {
        objectName = "";
        attribute = "";
        min = null;
        max = null;
        restrictions = null;
        extra = false;
        string[] pieces = context.RightArg.Split(',');
        string first = pieces[0].Trim();
        int slash = first.IndexOf('/');
        int offset;
        if (slash >= 0)
        {
            objectName = first[..slash].Trim();
            attribute = first[(slash + 1)..].Trim();
            offset = 1;
        }
        else if (pieces.Length >= 2)
        {
            objectName = first;
            attribute = pieces[1].Trim();
            offset = 2;
        }
        else
        {
            context.Notify(MushError.InvalidSecondArgument.Text());
            return false;
        }

        if (pieces.Length > offset) min = pieces[offset].Trim();
        if (pieces.Length > offset + 1) max = pieces[offset + 1].Trim();
        if (pieces.Length > offset + 2) restrictions = pieces[offset + 2].Trim();
        extra = pieces.Length > offset + 3;
        return true;
    }

    private static MUSHObject? Resolve(ObjectDatabase db, Dbref executor, string token)
    {
        if (token.Equals("me", StringComparison.OrdinalIgnoreCase))
            return db.GetObject(executor);
        if (token.StartsWith('#') && int.TryParse(token.AsSpan(1), out int id))
            return db.GetObject(new Dbref(id));
        return db.FindPlayer(token) ?? db.FindObject(token);
    }

    private static int Clamp(string? text, int fallback)
    {
        if (string.IsNullOrWhiteSpace(text) || !int.TryParse(text, out int value))
            return fallback;
        if (value < 0)
            value = 0;
        if (value > 30)
            value = 30;
        return value;
    }

    private static bool OkName(string name)
    {
        if (name.Length == 0 || name.Length >= 64)
            return false;
        char first = name[0];
        if (first is '"' or ':' or ';' or '\\' or '&' or ']' or '#')
            return false;
        bool letter = false;
        foreach (char c in name)
        {
            if (c < 33 || c > 126)
                return false;
            if (c is >= 'A' and <= 'Z')
                letter = true;
        }
        return letter;
    }
}
