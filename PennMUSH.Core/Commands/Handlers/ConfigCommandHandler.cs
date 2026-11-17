namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Configuration;
using PennMUSH.Core.Database;

public class ConfigCommandHandler : ICommandHandler
{
    private readonly MushConfiguration _config;

    public string Name => "@config";

    public ConfigCommandHandler(MushConfiguration config)
    {
        _config = config;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        if (context.Switches.Contains("set") || context.Switches.Contains("save"))
            return Set(context, actor.IsWizard, actor.IsGod);

        return List(context, actor.IsRoyalty);
    }

    private bool Set(CommandContext context, bool wizard, bool god)
    {
        bool save = context.Switches.Contains("save");
        if (!wizard || (save && !god))
        {
            context.Notify("You can't remake the world in your image.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(context.LeftArg))
        {
            context.Notify("What did you want to set?");
            return false;
        }
        if (!ConfigCatalog.TrySet(_config, context.LeftArg.Trim(), context.RightArg))
        {
            context.Notify("Couldn't set that option.");
            return false;
        }
        context.Notify(save
            ? "Option set but not saved (Saves disabled.)"
            : "Option set.");
        return true;
    }

    private bool List(CommandContext context, bool royalty)
    {
        string type = context.LeftArg.Trim();
        bool lower = context.Switches.Contains("lowercase");
        if (type.Length == 0)
        {
            context.Notify("Use: @config/list <type of options> where type is one of:");
            ShowGroups(context, royalty);
            return true;
        }

        foreach (var group in ConfigCatalog.Groups)
        {
            if (!group.Name.StartsWith(type, StringComparison.OrdinalIgnoreCase) || !ConfigCatalog.CanSee(group, royalty))
                continue;
            context.Notify(group.Description);
            foreach (var option in ConfigCatalog.Options)
            {
                if (option.Group.Equals(group.Name, StringComparison.Ordinal))
                    Show(context, option, lower);
            }
            return true;
        }

        var matches = new List<ConfigOption>();
        foreach (var option in Visible(royalty))
        {
            if (option.Name.StartsWith(type, StringComparison.OrdinalIgnoreCase))
                matches.Add(option);
        }
        if (matches.Count == 0)
        {
            foreach (var option in Visible(royalty))
            {
                if (option.Name.Contains(type, StringComparison.OrdinalIgnoreCase))
                    matches.Add(option);
            }
        }
        if (matches.Count == 0)
        {
            context.Notify("I only know the following types of options:");
            ShowGroups(context, royalty);
            return false;
        }
        foreach (var option in matches)
            Show(context, option, lower);
        return true;
    }

    private IEnumerable<ConfigOption> Visible(bool royalty)
    {
        foreach (var option in ConfigCatalog.Options)
        {
            foreach (var group in ConfigCatalog.Groups)
            {
                if (group.Name == option.Group && ConfigCatalog.CanSee(group, royalty))
                {
                    yield return option;
                    break;
                }
            }
        }
    }

    private void ShowGroups(CommandContext context, bool royalty)
    {
        foreach (var group in ConfigCatalog.Groups)
        {
            if (ConfigCatalog.CanSee(group, royalty))
                context.Notify($" {group.Name.PadRight(15)} {group.Description}");
        }
    }

    private void Show(CommandContext context, ConfigOption option, bool lower)
    {
        string name = lower ? option.Name.ToLowerInvariant() : option.Name;
        context.Notify($" {name.PadRight(40)} {option.Read(_config)}");
    }
}
