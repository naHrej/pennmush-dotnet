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
    public void Emit_StoresBareTextAndMarksWhoMaySeeAPrefix()
    {
        var plain = _dispatcher.Dispatch(_owner, "@emit Hello", _db);
        var local = _dispatcher.Dispatch(_owner, "@lemit Hello", _db);
        var mortalNs = _dispatcher.Dispatch(_owner, "@nsemit Hello", _db);

        Assert.Equal("Hello", plain.Notifications.Single());
        Assert.True(plain.TargetedNotifications.Single().Spoofable);
        Assert.True(local.TargetedNotifications.Single().Spoofable);
        Assert.True(mortalNs.TargetedNotifications.Single().Spoofable);

        _db.GetObject(_owner)!.AddFlag("WIZARD");
        var wizardEmit = _dispatcher.Dispatch(_owner, "@emit Hello", _db);
        var wizardNs = _dispatcher.Dispatch(_owner, "@nsemit Hello", _db);
        var wizardLocal = _dispatcher.Dispatch(_owner, "@nslemit Hello", _db);
        Assert.True(wizardEmit.TargetedNotifications.Single().Spoofable);
        Assert.False(wizardNs.TargetedNotifications.Single().Spoofable);
        Assert.False(wizardLocal.TargetedNotifications.Single().Spoofable);
        Assert.Equal("Hello", wizardNs.Notifications.Single());

        _db.GetObject(_owner)!.RemoveFlag("WIZARD");
        _db.GetObject(_owner)!.AddPower("Can_spoof");
        var allowed = _dispatcher.Dispatch(_owner, "@emit Hello", _db);
        var forced = _dispatcher.Dispatch(_owner, "@nsemit Hello", _db);
        Assert.True(allowed.TargetedNotifications.Single().Spoofable);
        Assert.False(forced.TargetedNotifications.Single().Spoofable);
        Assert.Equal("Hello", forced.Notifications.Single());
    }

    [Fact]
    public void Pemit_StoresBareTextForTheListenerRule()
    {
        var sent = _dispatcher.Dispatch(_owner, $"@pemit #{_owner.Value}=Hi", _db);
        var delivered = sent.TargetedNotifications.Single(n => n.Scope == NotificationScope.Target);
        Assert.Equal("Hi", delivered.Message);
        Assert.True(delivered.Spoofable);
        Assert.False(delivered.Prompt);

        _db.GetObject(_owner)!.AddPower("Can_spoof");
        var prompt = _dispatcher.Dispatch(_owner, $"@prompt #{_owner.Value}=Ready?", _db);
        var marked = prompt.TargetedNotifications.Single(n => n.Scope == NotificationScope.Target);
        Assert.Equal("Ready?", marked.Message);
        Assert.True(marked.Spoofable);
        Assert.True(marked.Prompt);

        var forced = _dispatcher.Dispatch(_owner, $"@nsprompt #{_owner.Value}=Ready?", _db);
        var forcedNote = forced.TargetedNotifications.Single(n => n.Scope == NotificationScope.Target);
        Assert.Equal("Ready?", forcedNote.Message);
        Assert.False(forcedNote.Spoofable);
        Assert.True(forcedNote.Prompt);
    }

    [Fact]
    public void OemitAndRemit_UseTheSameListenerRule()
    {
        var friend = _db.CreateObject("Friend", ObjectType.Player, _owner);
        _db.MoveObject(friend.Id, _room);

        var hidden = _dispatcher.Dispatch(_owner, "@oemit Friend=secret", _db);
        Assert.Contains("secret", hidden.Notifications);
        Assert.True(hidden.TargetedNotifications.Single().Spoofable);

        var remote = _dispatcher.Dispatch(_owner, $"@remit #{_room.Value}=remote", _db);
        Assert.Contains("remote", remote.Notifications);
        Assert.Contains(remote.TargetedNotifications, n => n.Scope == NotificationScope.Room && n.Spoofable && n.Message == "remote");

        _db.GetObject(_owner)!.AddPower("Can_spoof");
        var forced = _dispatcher.Dispatch(_owner, "@nsoemit Friend=secret", _db);
        Assert.Contains("secret", forced.Notifications);
        Assert.False(forced.TargetedNotifications.Single().Spoofable);
        var forcedRemote = _dispatcher.Dispatch(_owner, $"@nsremit #{_room.Value}=remote", _db);
        Assert.Contains(forcedRemote.TargetedNotifications, n => n.Scope == NotificationScope.Room && !n.Spoofable && n.Message == "remote");
    }

    [Fact]
    public void Cemit_MarksANormalEmitSpoofable()
    {
        _channels.CreateChannel("News", _owner);
        var plain = _dispatcher.Dispatch(_owner, "@cemit News=Hello", _db);
        Assert.Contains("[News] Hello", plain.Notifications);
        Assert.True(plain.TargetedNotifications.Single().Spoofable);

        _db.GetObject(_owner)!.AddPower("Can_spoof");
        var allowed = _dispatcher.Dispatch(_owner, "@cemit News=Hello", _db);
        var forced = _dispatcher.Dispatch(_owner, "@nscemit News=Hello", _db);
        Assert.Contains("[News] Hello", allowed.Notifications);
        Assert.Contains("[News] Hello", forced.Notifications);
        Assert.True(allowed.TargetedNotifications.Single().Spoofable);
        Assert.False(forced.TargetedNotifications.Single().Spoofable);
    }

    [Fact]
    public void Listener_SeesAPrefixOnlyWhenNospoof()
    {
        var db = new ObjectDatabase();
        var speaker = db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        var quiet = db.CreateObject("Bea", ObjectType.Player, new Dbref(1));
        var nosy = db.CreateObject("Cara", ObjectType.Player, new Dbref(1));
        nosy.AddFlag("NOSPOOF");
        var wary = db.CreateObject("Dora", ObjectType.Player, new Dbref(1));
        wary.AddFlag("NOSPOOF");
        wary.AddFlag("PARANOID");
        var onlyParanoid = db.CreateObject("Eve", ObjectType.Player, new Dbref(1));
        onlyParanoid.AddFlag("PARANOID");
        var owned = db.CreateObject("Lamp", ObjectType.Thing, nosy.Id);
        var ownedByWary = db.CreateObject("Globe", ObjectType.Thing, wary.Id);
        string paranoid = $"[Ada(#{speaker.Id.Value})] Hello";

        Assert.Equal("Hello", SpoofPrefix.ForListener(db, speaker, quiet, "Hello"));
        Assert.Equal("Hello", SpoofPrefix.ForListener(db, speaker, speaker, "Hello"));
        Assert.Equal("Hello", SpoofPrefix.ForListener(db, speaker, onlyParanoid, "Hello"));
        Assert.Equal("[Ada:] Hello", SpoofPrefix.ForListener(db, speaker, nosy, "Hello"));
        Assert.Equal("[Ada:] Hello", SpoofPrefix.ForListener(db, speaker, owned, "Hello"));
        Assert.Equal(paranoid, SpoofPrefix.ForListener(db, speaker, wary, "Hello"));
        Assert.Equal(paranoid, SpoofPrefix.ForListener(db, speaker, ownedByWary, "Hello"));

        speaker.AddFlag("NOSPOOF");
        Assert.Equal("Hello", SpoofPrefix.ForListener(db, speaker, speaker, "Hello"));
        speaker.AddFlag("PARANOID");
        Assert.Equal(paranoid, SpoofPrefix.ForListener(db, speaker, speaker, "Hello"));
        Assert.False(SpoofPrefix.Omits(speaker, "@emit"));
        Assert.False(SpoofPrefix.Omits(speaker, "@nsemit"));
        speaker.AddFlag("WIZARD");
        Assert.False(SpoofPrefix.Omits(speaker, "@emit"));
        Assert.True(SpoofPrefix.Omits(speaker, "@nsemit"));
    }
}
