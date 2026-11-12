namespace PennMUSH.Core.Tests.Communication;

using System;
using System.Linq;
using Xunit;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Communication.Mail;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class ComsysAndMailTests
{
    private readonly ObjectDatabase _db;
    private readonly ChannelRegistry _channels;
    private readonly MailStore _mail;
    private readonly CommandDispatcher _dispatcher;
    private readonly MUSHObject _room;
    private readonly MUSHObject _wizard;
    private readonly MUSHObject _alice;
    private readonly MUSHObject _bob;

    public ComsysAndMailTests()
    {
        _db = new ObjectDatabase();
        _room = _db.CreateObject("Town Square", ObjectType.Room, Dbref.Nothing); // #0
        _wizard = _db.CreateObject("God", ObjectType.Player, Dbref.Nothing);    // #1
        _wizard.AddFlag("WIZARD");
        _alice = _db.CreateObject("Alice", ObjectType.Player, Dbref.Nothing);   // #2
        _bob = _db.CreateObject("Bob", ObjectType.Player, Dbref.Nothing);       // #3

        _wizard.Location = _room.Id;
        _alice.Location = _room.Id;
        _bob.Location = _room.Id;

        _channels = new ChannelRegistry();
        _mail = new MailStore();

        _dispatcher = new CommandDispatcher();
        _dispatcher.Register(new ChannelCommandHandler(_channels));
        _dispatcher.Register(new ChatCommandHandler(_channels), "+c");
        _dispatcher.Register(new CemitCommandHandler(_channels));
        _dispatcher.Register(new MailCommandHandler(_mail));
    }

    [Fact]
    public void Channel_CreationAndMembership_WorkCorrectly()
    {
        // Alice (mortal) tries to create channel -> denied
        var ctx1 = _dispatcher.Dispatch(_alice.Id, "@channel/add Gossip", _db);
        Assert.Contains("Permission denied.", ctx1.Notifications);

        // Wizard creates channel -> success
        var ctx2 = _dispatcher.Dispatch(_wizard.Id, "@channel/add Gossip", _db);
        Assert.Contains("Channel <Gossip> created.", ctx2.Notifications);

        // Alice joins Gossip
        var ctx3 = _dispatcher.Dispatch(_alice.Id, "@channel/join Gossip", _db);
        Assert.Contains("You join channel <Gossip>.", ctx3.Notifications);

        // Alice sets title
        var ctx4 = _dispatcher.Dispatch(_alice.Id, "@channel/title Gossip=Wanderer", _db);
        Assert.Contains("Title for channel <Gossip> set to: Wanderer", ctx4.Notifications);

        // Check channel who
        var ctx5 = _dispatcher.Dispatch(_wizard.Id, "@channel/who Gossip", _db);
        Assert.Contains(ctx5.Notifications, n => n.Contains("Alice [Wanderer]"));

        // Alice leaves Gossip
        var ctx6 = _dispatcher.Dispatch(_alice.Id, "@channel/leave Gossip", _db);
        Assert.Contains("You leave channel <Gossip>.", ctx6.Notifications);
    }

    [Fact]
    public void Channel_ChatAndRecall_WorkCorrectly()
    {
        _channels.CreateChannel("Public", _wizard.Id);
        var ch = _channels.GetChannel("Public")!;
        ch.AddMember(_alice.Id, "Scout");
        ch.AddMember(_bob.Id);

        // Alice chats
        var ctx1 = _dispatcher.Dispatch(_alice.Id, "@chat Public=Greetings everyone!", _db);
        Assert.Contains("[Public] <Scout> Alice: Greetings everyone!", ctx1.Notifications);

        // Bob poses
        var ctx2 = _dispatcher.Dispatch(_bob.Id, "@chat Public=:smiles warmly.", _db);
        Assert.Contains("[Public] Bob smiles warmly.", ctx2.Notifications);

        // Bob recalls buffer
        var ctx3 = _dispatcher.Dispatch(_bob.Id, "@channel/recall Public", _db);
        Assert.Contains(ctx3.Notifications, n => n.Contains("Greetings everyone!"));
        Assert.Contains(ctx3.Notifications, n => n.Contains("smiles warmly."));
    }

    [Fact]
    public void Channel_Cemit_WorkCorrectly()
    {
        _channels.CreateChannel("News", _wizard.Id);
        var ch = _channels.GetChannel("News")!;
        ch.AddMember(_alice.Id);

        // Wizard emits
        var ctx1 = _dispatcher.Dispatch(_wizard.Id, "@cemit News=Server restart in 10 minutes.", _db);
        Assert.Contains("[News] [God:] Server restart in 10 minutes.", ctx1.Notifications);

        // Mortal alice cannot cemit
        var ctx2 = _dispatcher.Dispatch(_alice.Id, "@cemit News=Hacked!", _db);
        Assert.Contains("Permission denied.", ctx2.Notifications);
    }

    [Fact]
    public void Channel_PlusPrefixShortcut_DispatchesCleanly()
    {
        _channels.CreateChannel("General", _wizard.Id);
        var ch = _channels.GetChannel("General")!;
        ch.AddMember(_alice.Id);

        // +General Hello
        var ctx = _dispatcher.Dispatch(_alice.Id, "+General Hello from shortcut!", _db);
        Assert.Contains("[General] Alice: Hello from shortcut!", ctx.Notifications);

        // +General/who
        var ctxWho = _dispatcher.Dispatch(_alice.Id, "+General/who", _db);
        Assert.Contains(ctxWho.Notifications, n => n.Contains("Alice"));
    }

    [Fact]
    public void Mail_SendReadClearPurge_LifecycleWorksCorrectly()
    {
        // 1. Alice sends mail to Bob with subject
        var ctx1 = _dispatcher.Dispatch(_alice.Id, "@mail Bob=Secret Quest/Meet me near the dragon lair.", _db);
        Assert.Contains("Mail sent to Bob (Subject: Secret Quest).", ctx1.Notifications);

        // 2. Bob checks mail status
        var ctxStatus = _dispatcher.Dispatch(_bob.Id, "@mail/status", _db);
        Assert.Contains(ctxStatus.Notifications, n => n.Contains("1 message(s), 1 unread, 0 cleared."));

        // 3. Bob lists mail
        var ctxList = _dispatcher.Dispatch(_bob.Id, "@mail", _db);
        Assert.Contains(ctxList.Notifications, n => n.Contains("Secret Quest"));

        // 4. Bob reads message 1
        var ctxRead = _dispatcher.Dispatch(_bob.Id, "@mail/read 1", _db);
        Assert.Contains(ctxRead.Notifications, n => n.Contains("Meet me near the dragon lair."));

        // 5. Check status: now 0 unread
        var ctxStatus2 = _dispatcher.Dispatch(_bob.Id, "@mail/status", _db);
        Assert.Contains(ctxStatus2.Notifications, n => n.Contains("1 message(s), 0 unread, 0 cleared."));

        // 6. Bob clears message 1
        var ctxClear = _dispatcher.Dispatch(_bob.Id, "@mail/clear 1", _db);
        Assert.Contains("Message 1 marked as cleared.", ctxClear.Notifications);

        // 7. Bob purges cleared messages
        var ctxPurge = _dispatcher.Dispatch(_bob.Id, "@mail/purge", _db);
        Assert.Contains("1 cleared message(s) purged.", ctxPurge.Notifications);

        // 8. Bob checks status: 0 messages
        var ctxStatus3 = _dispatcher.Dispatch(_bob.Id, "@mail/status", _db);
        Assert.Contains(ctxStatus3.Notifications, n => n.Contains("0 message(s), 0 unread, 0 cleared."));
    }
}
