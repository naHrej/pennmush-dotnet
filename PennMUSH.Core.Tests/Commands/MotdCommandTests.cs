namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Communication;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;
using Xunit;

public class MotdCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly MotdBoard _motd = new();
    private readonly CommandDispatcher _dispatcher = new();
    private readonly Dbref _god;
    private readonly Dbref _mortal;
    private readonly Dbref _royal;

    public MotdCommandTests()
    {
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        _god = _db.CreateObject("One", ObjectType.Player, new Dbref(1)).Id;
        _mortal = _db.CreateObject("Ada", ObjectType.Player, _god).Id;
        _royal = _db.CreateObject("Rex", ObjectType.Player, _god).Id;
        _db.GetObject(_royal)!.AddFlag("ROYALTY");
        _dispatcher.Register(new MotdCommandHandler(_motd), "@motd", "@listmotd", "@wizmotd", "@rejectmotd");
        _dispatcher.Register(new ListCommandHandler(_dispatcher, motd: _motd), "@list");
    }

    [Fact]
    public void Mortal_CannotSetTheConnectAnnouncement()
    {
        var denied = _dispatcher.Dispatch(_mortal, "@motd The gate is open.", _db);

        Assert.Contains("You may get 15 minutes of fame and glory in life, but not right now.", denied.Notifications);
        Assert.Equal("", _motd.Connect);
    }

    [Fact]
    public void AnnouncePower_SetsTheConnectAnnouncement()
    {
        _db.GetObject(_mortal)!.AddPower("Announce");

        var set = _dispatcher.Dispatch(_mortal, "@motd hello=there", _db);
        var listed = _dispatcher.Dispatch(_mortal, "@motd", _db);

        Assert.Contains("Motd set.", set.Notifications);
        Assert.Equal("hello=there", _motd.Connect);
        Assert.Equal(new[] { "MOTD: hello=there" }, listed.Notifications);
    }

    [Fact]
    public void List_ShowsEveryAnnouncementToRoyalty()
    {
        _motd.Set(MotdKind.Connect, "hello");
        _motd.Set(MotdKind.Wizard, "staff");
        _motd.Set(MotdKind.Down, "closed");
        _motd.Set(MotdKind.Full, "packed");

        var mortal = _dispatcher.Dispatch(_mortal, "@listmotd", _db);
        var royal = _dispatcher.Dispatch(_royal, "@motd/list leave this", _db);

        Assert.Equal(new[] { "MOTD: hello" }, mortal.Notifications);
        Assert.Equal("hello", _motd.Connect);
        Assert.Equal(new[]
        {
            "MOTD: hello",
            "Wiz MOTD: staff",
            "Down MOTD: closed",
            "Full MOTD: packed"
        }, royal.Notifications);
    }

    [Fact]
    public void WizardNames_RequireAWizard()
    {
        var named = _dispatcher.Dispatch(_royal, "@wizmotd staff only", _db);
        Assert.Contains("Permission denied.", named.Notifications);
        Assert.Equal("", _motd.Wizard);

        var switched = _dispatcher.Dispatch(_royal, "@motd/wizard staff only", _db);
        Assert.Contains("Wizard motd set.", switched.Notifications);
        Assert.Equal("staff only", _motd.Wizard);

        var mortal = _dispatcher.Dispatch(_mortal, "@motd/down closed", _db);
        Assert.Contains("You may get 15 minutes of fame and glory in life, but not right now.", mortal.Notifications);
        Assert.Equal("", _motd.Down);
    }

    [Fact]
    public void RejectMotd_SetsTheDownAnnouncement()
    {
        var set = _dispatcher.Dispatch(_god, "@rejectmotd logins are closed", _db);
        var cleared = _dispatcher.Dispatch(_god, "@motd/clear/full", _db);
        _dispatcher.Dispatch(_god, "@motd/full packed", _db);
        var clearedFull = _dispatcher.Dispatch(_god, "@motd/clear/full", _db);

        Assert.Contains("Down motd set.", set.Notifications);
        Assert.Equal("logins are closed", _motd.Down);
        Assert.Contains("Full motd cleared.", cleared.Notifications);
        Assert.Equal("", _motd.Full);
        Assert.Contains("Full motd cleared.", clearedFull.Notifications);
        Assert.Equal("", _motd.Full);
    }

    [Fact]
    public void ListCommand_UsesTheSameReport()
    {
        _motd.Set(MotdKind.Connect, "hello");
        _motd.Set(MotdKind.Full, "packed");

        var listed = _dispatcher.Dispatch(_god, "@list motd", _db);

        Assert.Equal("MOTD: hello", listed.Notifications[0]);
        Assert.Equal("Full MOTD: packed", listed.Notifications[3]);
    }

    [Fact]
    public void ConnectLines_AddTheWizardAnnouncementForRoyalty()
    {
        Assert.Empty(_motd.ConnectLines(true));

        _motd.Set(MotdKind.Connect, "hello");
        _motd.Set(MotdKind.Wizard, "staff");

        Assert.Equal(new[] { "hello" }, _motd.ConnectLines(false));
        Assert.Equal(new[] { "hello", MotdBoard.AsteriskLine, "staff" }, _motd.ConnectLines(true));
    }
}
