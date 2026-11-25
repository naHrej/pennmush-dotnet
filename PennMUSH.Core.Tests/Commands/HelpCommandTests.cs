namespace PennMUSH.Core.Tests.Commands;

using System;
using System.IO;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Help;
using PennMUSH.Core.Types;
using Xunit;

public class HelpCommandTests
{
    [Fact]
    public void Help_LeavesParameterBracketsWhenTheClientIsNotHtml()
    {
        var result = Run("help move", _ => false);

        Assert.Contains(result.Notifications, line => line.Contains("MOVE <direction>"));
    }

    [Fact]
    public void Help_EscapesParameterBracketsInHtmlMode()
    {
        var result = Run("help move", _ => true);

        Assert.Contains(result.Notifications, line => line.Contains("MOVE &lt;direction&gt;"));
        Assert.Contains(result.Notifications, line => line.Contains("white-space:pre-wrap"));
        Assert.DoesNotContain(result.Notifications, line => line.Contains("<direction>"));
    }

    [Fact]
    public void Help_EscapesAMissingTopicInHtmlMode()
    {
        var result = Run("help <missing>", _ => true);

        Assert.Contains("No entry for '&lt;missing&gt;'.", result.Notifications[0]);
        Assert.Contains("white-space:pre-wrap", result.Notifications[0]);
    }


    [Fact]
    public void Help_KeepsColumnSpacesInHtmlMode()
    {
        var result = Run("help cols", _ => true);

        Assert.Contains(result.Notifications, line => line.Contains("a()&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;b()"));
        Assert.DoesNotContain(result.Notifications, line => line.Contains("a()        b()"));
    }

    [Fact]
    public void Help_RendersMarkdownToStyledHtmlAndPlainText()
    {
        string dir = Path.Combine(Path.GetTempPath(), "penn-md-help-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "system_object.md"),
                "---\ntopics: system_object, $name\n---\n# System Object\n\n@lock[/<switch>] <object>=<key>\n\nUse `<player>` or `@set $system/COMBAT=#42`.\n\n| Flag | Meaning |\n|---|---|\n| `SAFE` | Protected |\n");

            var store = new HelpStore();
            store.LoadFromDirectory(dir);
            var db = new ObjectDatabase();
            db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
            var player = db.CreateObject("One", ObjectType.Player, new Dbref(1)).Id;

            var htmlDispatcher = new CommandDispatcher();
            htmlDispatcher.Register(new HelpCommandHandler(store, _ => true), "help");
            var htmlRes = htmlDispatcher.Dispatch(player, "help $name", db);
            Assert.Contains(htmlRes.Notifications, n =>
                n.Contains("<div class=\"help-topic\">")
                && n.Contains(">System Object</h1>")
                && n.Contains("<table>")
                && n.Contains("<span class=\"help-opt\">[</span>")
                && n.Contains("<span class=\"help-arg\">&lt;switch&gt;</span>")
                && n.Contains("<span class=\"help-arg\">&lt;object&gt;</span>")
                && n.Contains("<span class=\"help-arg\">&lt;key&gt;</span>"));

            var plainDispatcher = new CommandDispatcher();
            plainDispatcher.Register(new HelpCommandHandler(store, _ => false), "help");
            var plainRes = plainDispatcher.Dispatch(player, "help system_object", db);
            Assert.Contains(plainRes.Notifications, n => n.Contains("System Object") && n.Contains("@lock[/<switch>] <object>=<key>") && !n.Contains("<div class=\"help-topic\">"));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    private static CommandContext Run(string command, Func<Dbref, bool> html)
    {
        string dir = Path.Combine(Path.GetTempPath(), "penn-help-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "move.hlp"), "& move\nMOVE <direction>\nGo that way.\n& cols\na()        b()\n");
            var store = new HelpStore();
            store.LoadFromDirectory(dir);
            var db = new ObjectDatabase();
            db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
            var player = db.CreateObject("One", ObjectType.Player, new Dbref(1)).Id;
            var dispatcher = new CommandDispatcher();
            dispatcher.Register(new HelpCommandHandler(store, html), "help");
            return dispatcher.Dispatch(player, command, db);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
