namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;
using Xunit;

public class NospoofEmitTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher = new();
    private readonly ChannelRegistry _channels = new();
    private readonly Dbref _room;
    private readonly Dbref _owner;

    public NospoofEmitTests()
    {
        _room = _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1)).Id;
        _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        _owner = _db.CreateObject("Owner", ObjectType.Player, new Dbref(1)).Id;
        _db.MoveObject(_owner, _room);
        _dispatcher.Register(new EmitCommandHandler(), "@emit", "@lemit", "@nsemit", "@nslemit");
        _dispatcher.Register(new PemitCommandHandler(), "@pemit", "@prompt", "@nspemit", "@nsprompt");
        _dispatcher.Register(new OemitCommandHandler(), "@oemit", "@nsoemit");
        _dispatcher.Register(new RemitCommandHandler(), "@remit", "@nsremit");
        _dispatcher.Register(new CemitCommandHandler(_channels), "@cemit", "@nscemit");
    }

    [Fact]
    public void Emit_PrefixesUntilTheSpeakerCanSpoof()
    {
        var plain = _dispatcher.Dispatch(_owner, "@emit Hello", _db);
        var local = _dispatcher.Dispatch(_owner, "@lemit Hello", _db);

        Assert.Equal("[Owner:] Hello", plain.Notifications.Single());
        Assert.Equal("[Owner:] Hello", local.Notifications.Single());

        _db.GetObject(_owner)!.AddPower("Can_spoof");
        var allowed = _dispatcher.Dispatch(_owner, "@emit Hello", _db);
        var forced = _dispatcher.Dispatch(_owner, "@nsemit Hello", _db);
        var forcedLocal = _dispatcher.Dispatch(_owner, "@nslemit Hello", _db);

        Assert.Equal("Hello", allowed.Notifications.Single());
        Assert.Equal("[Owner:] Hello", forced.Notifications.Single());
        Assert.Equal("[Owner:] Hello", forcedLocal.Notifications.Single());
    }

    [Fact]
    public void Pemit_PrefixesTheDeliveredMessage()
    {
        var sent = _dispatcher.Dispatch(_owner, $"@pemit #{_owner.Value}=Hi", _db);
        var delivered = sent.TargetedNotifications.Single(n => n.Scope == NotificationScope.Target);
        Assert.Equal("[Owner:] Hi", delivered.Message);
        Assert.False(delivered.Prompt);

        _db.GetObject(_owner)!.AddPower("Can_spoof");
        var prompt = _dispatcher.Dispatch(_owner, $"@prompt #{_owner.Value}=Ready?", _db);
        var marked = prompt.TargetedNotifications.Single(n => n.Scope == NotificationScope.Target);
        Assert.Equal("Ready?", marked.Message);
        Assert.True(marked.Prompt);

        var forced = _dispatcher.Dispatch(_owner, $"@nsprompt #{_owner.Value}=Ready?", _db);
        var forcedNote = forced.TargetedNotifications.Single(n => n.Scope == NotificationScope.Target);
        Assert.Equal("[Owner:] Ready?", forcedNote.Message);
        Assert.True(forcedNote.Prompt);
    }

    [Fact]
    public void OemitAndRemit_UseTheSamePrefix()
    {
        var friend = _db.CreateObject("Friend", ObjectType.Player, _owner);
        _db.MoveObject(friend.Id, _room);

        var hidden = _dispatcher.Dispatch(_owner, "@oemit Friend=secret", _db);
        Assert.Contains("[Owner:] secret", hidden.Notifications);

        var remote = _dispatcher.Dispatch(_owner, $"@remit #{_room.Value}=remote", _db);
        Assert.Contains("[Owner:] remote", remote.Notifications);

        _db.GetObject(_owner)!.AddPower("Can_spoof");
        var forced = _dispatcher.Dispatch(_owner, "@nsoemit Friend=secret", _db);
        Assert.Contains("[Owner:] secret", forced.Notifications);
        var forcedRemote = _dispatcher.Dispatch(_owner, $"@nsremit #{_room.Value}=remote", _db);
        Assert.Contains("[Owner:] remote", forcedRemote.Notifications);
    }

    [Fact]
    public void Cemit_PrefixesWhenTheSpeakerCannotSpoof()
    {
        _channels.CreateChannel("News", _owner);
        var plain = _dispatcher.Dispatch(_owner, "@cemit News=Hello", _db);
        Assert.Contains("[News] [Owner:] Hello", plain.Notifications);

        _db.GetObject(_owner)!.AddPower("Can_spoof");
        var allowed = _dispatcher.Dispatch(_owner, "@cemit News=Hello", _db);
        var forced = _dispatcher.Dispatch(_owner, "@nscemit News=Hello", _db);
        Assert.Contains("[News] Hello", allowed.Notifications);
        Assert.Contains("[News] [Owner:] Hello", forced.Notifications);
    }
}
