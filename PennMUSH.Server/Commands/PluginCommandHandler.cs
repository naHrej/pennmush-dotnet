namespace PennMUSH.Server.Commands;

using System;
using System.Text;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Database;
using PennMUSH.Server.Extensions;

public class PluginCommandHandler : ICommandHandler
{
    private readonly ExtensionManager _extensionManager;

    public string Name => "@plugin";

    public PluginCommandHandler(ExtensionManager extensionManager)
    {
        _extensionManager = extensionManager;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.IsWizard)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        if (context.Switches.Contains("load"))
        {
            if (string.IsNullOrWhiteSpace(context.Arguments))
            {
                context.Notify(Feedback.UsagePluginLoadAssemblyPath);
                return false;
            }

            try
            {
                var record = _extensionManager.LoadExtension(context.Arguments.Trim());
                context.Notify(Feedback.SuccessfullyLoadedPluginVBy, ("value", $"{record.Name}"), ("value2", $"{record.Version}"), ("value3", $"{record.Author}"));
                return true;
            }
            catch (Exception ex)
            {
                context.Notify(Feedback.FailedToLoadPlugin, ("value", $"{ex.Message}"));
                return false;
            }
        }

        if (context.Switches.Contains("unload"))
        {
            if (string.IsNullOrWhiteSpace(context.Arguments))
            {
                context.Notify(Feedback.UsagePluginUnloadPluginName);
                return false;
            }

            string name = context.Arguments.Trim();
            if (_extensionManager.UnloadExtension(name))
            {
                context.Notify(Feedback.PluginHasBeenUnloaded, ("value", $"{name}"));
                return true;
            }
            else
            {
                context.Notify(Feedback.PluginIsNotCurrentlyLoaded, ("value", $"{name}"));
                return false;
            }
        }

        if (context.Switches.Contains("reload"))
        {
            if (string.IsNullOrWhiteSpace(context.Arguments))
            {
                context.Notify(Feedback.UsagePluginReloadPluginName);
                return false;
            }

            string name = context.Arguments.Trim();
            try
            {
                var record = _extensionManager.ReloadExtension(name);
                context.Notify(Feedback.SuccessfullyReloadedPluginV, ("value", $"{record.Name}"), ("value2", $"{record.Version}"));
                return true;
            }
            catch (Exception ex)
            {
                context.Notify(Feedback.FailedToReloadPlugin, ("value", $"{ex.Message}"));
                return false;
            }
        }

        // Default or /list: display loaded plugins table
        var plugins = _extensionManager.LoadedExtensions;
        if (plugins.Count == 0)
        {
            context.Notify(Feedback.NoExternalPluginsAreCurrentlyLoaded);
            return true;
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== Loaded PennMUSH Extensions ===");
        sb.AppendLine(string.Format("{0,-20} {1,-10} {2,-15} {3}", "Plugin Name", "Version", "Author", "Description"));
        sb.AppendLine(new string('-', 75));

        foreach (var p in plugins)
        {
            sb.AppendLine(string.Format("{0,-20} {1,-10} {2,-15} {3}", p.Name, p.Version, p.Author, p.Description));
        }

        sb.AppendLine(new string('-', 75));
        sb.AppendLine($"Total plugins loaded: {plugins.Count}");
        context.Notify(sb.ToString().TrimEnd());
        return true;
    }
}
