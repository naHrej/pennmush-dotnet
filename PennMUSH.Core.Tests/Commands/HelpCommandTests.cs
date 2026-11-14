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
