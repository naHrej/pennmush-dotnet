namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;
using Xunit;

public class ComCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly ChannelRegistry _channels = new();
    private readonly CommandDispatcher _dispatcher = new();
    private readonly Dbref _one;

    public ComCommandTests()
    {
        _one = _db.CreateObject("One", ObjectType.Player, new Dbref(1)).Id;
        _channels.CreateChannel("Public", _one);
        _channels.CreateChannel("PubTalk", _one);
        _dispatcher.Register(new ComCommandHandler(_channels), "addcom", "delcom", "comtitle", "comlist", "@clist");
    }

    [Fact]
    public void Addcom_JoinsAndRemembersTheAlias()
    {
        var denied = _dispatcher.Dispatch(_one, "addcom pub=Missing", _db);
        Assert.Contains("I don't recognise that channel.", denied.Notifications);
        Assert.Empty(_channels.GetChannelsForPlayer(_one));

        var ambiguous = _dispatcher.Dispatch(_one, "addcom x=pub", _db);
        Assert.Contains("I don't know which channel you mean.", ambiguous.Notifications);

        var joined = _dispatcher.Dispatch(_one, "addcom pub=Public", _db);
        Assert.Contains("You join channel <Public>.", joined.Notifications);
        Assert.Contains("Alias pub added for channel <Public>.", joined.Notifications);
        Assert.True(_channels.GetChannel("Public")!.HasMember(_one));

        var again = _dispatcher.Dispatch(_one, "addcom chat=Publ", _db);
        Assert.DoesNotContain("You join channel <Public>.", again.Notifications);
        Assert.Contains("Alias chat added for channel <Public>.", again.Notifications);
        Assert.Contains("That alias is already in use.", _dispatcher.Dispatch(_one, "addcom pub=Public", _db).Notifications);
        Assert.Contains("Invalid alias.", _dispatcher.Dispatch(_one, "addcom bad alias=Public", _db).Notifications);
    }

    [Fact]
    public void Delcom_LeavesWhenTheLastAliasGoes()
    {
        _dispatcher.Dispatch(_one, "addcom pub=Public", _db);
        _dispatcher.Dispatch(_one, "addcom chat=Public", _db);

        var kept = _dispatcher.Dispatch(_one, "delcom chat", _db);
        Assert.Contains("Alias removed.", kept.Notifications);
        Assert.True(_channels.GetChannel("Public")!.HasMember(_one));

        var left = _dispatcher.Dispatch(_one, "delcom pub", _db);
        Assert.Contains("You leave channel <Public>.", left.Notifications);
        Assert.False(_channels.GetChannel("Public")!.HasMember(_one));
        Assert.Contains("No such alias.", _dispatcher.Dispatch(_one, "delcom pub", _db).Notifications);
    }

    [Fact]
    public void ComtitleAndLists_UseTheAlias()
    {
        _dispatcher.Dispatch(_one, "addcom pub=Public", _db);
        _channels.GetChannel("Public")!.AddMessage("One", _one, "[Public] One: hi");

        var titled = _dispatcher.Dispatch(_one, "comtitle pub=Ops", _db);
        Assert.Contains("Title set for channel <Public>.", titled.Notifications);
        Assert.Equal("Ops", _channels.GetChannel("Public")!.GetMember(_one)!.Title);
        Assert.Contains("Your title on <Public> is 'Ops'.", _dispatcher.Dispatch(_one, "comtitle pub", _db).Notifications);

        var aliases = _dispatcher.Dispatch(_one, "comlist", _db);
        Assert.Contains(aliases.Notifications, line => line.StartsWith("Alias") && line.Contains("Channel") && line.Contains("Status"));
        Assert.Contains(aliases.Notifications, line => line.Contains("pub") && line.Contains("Public") && line.Contains("On") && line.Contains("Ops"));
        Assert.Contains("-- End of comlist --", aliases.Notifications);

        var brief = _dispatcher.Dispatch(_one, "@clist Publ", _db);
        Assert.Contains(brief.Notifications, line => line.Contains("Public") && line.TrimEnd().EndsWith("On"));
        Assert.DoesNotContain(brief.Notifications, line => line.Contains("PubTalk"));

        var full = _dispatcher.Dispatch(_one, "@clist/full", _db);
        Assert.Contains(full.Notifications, line => line.Contains("Msgs"));
        Assert.Contains(full.Notifications, line => line.Contains("Public") && line.TrimEnd().EndsWith("On"));
    }
}
