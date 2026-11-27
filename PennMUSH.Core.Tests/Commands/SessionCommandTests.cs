namespace PennMUSH.Core.Tests.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Session;
using PennMUSH.Core.Types;
using Xunit;

public class SessionCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly MemorySessions _sessions = new();
    private readonly CommandDispatcher _dispatcher = new();
    private readonly DateTimeOffset _now = new(2026, 11, 14, 18, 0, 0, TimeSpan.Zero);
    private readonly Dbref _god;
    private readonly Dbref _ada;
    private readonly Dbref _rex;

    public SessionCommandTests()
    {
        var limbo = _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        _god = _db.CreateObject("One", ObjectType.Player, new Dbref(1)).Id;
        _ada = _db.CreateObject("Ada", ObjectType.Player, _god).Id;
        _rex = _db.CreateObject("Rex", ObjectType.Player, _god).Id;
        var ada = _db.GetObject(_ada)!;
        var rex = _db.GetObject(_rex)!;
        ada.Location = limbo.Id;
        rex.Location = limbo.Id;
        ada.SetAttribute("DOING", "reading", _ada);
        ada.SetAttribute("ALIAS", "reader", _ada);
        rex.AddFlag("ROYALTY");

        _sessions.Add(new SessionInfo
        {
            Descriptor = 2,
            PlayerId = _ada,
            Host = "10.0.0.8",
            ConnectedAt = _now.AddHours(-2),
            LastActivity = _now.AddSeconds(-30),
            Commands = 4,
            BytesSent = 100,
            BytesReceived = 40
        });
        _sessions.Add(new SessionInfo
        {
            Descriptor = 3,
            PlayerId = _db.CreateObject("Bea", ObjectType.Player, _god).Id,
            Hidden = true,
            Host = "10.0.0.3",
            ConnectedAt = _now.AddMinutes(-5),
            LastActivity = _now.AddMinutes(-5),
            Commands = 1
        });
        _sessions.Add(new SessionInfo
        {
            Descriptor = 4,
            PlayerId = _rex,
            Host = "10.0.0.4",
            ConnectedAt = _now.AddMinutes(-10),
            LastActivity = _now.AddMinutes(-2),
            Commands = 2
        });
        _sessions.Add(new SessionInfo
        {
            Descriptor = 7,
            PlayerId = _rex,
            Host = "10.0.0.4",
            ConnectedAt = _now.AddHours(-3),
            LastActivity = _now.AddMinutes(-3),
            Commands = 8
        });
        _sessions.Add(new SessionInfo
        {
            Descriptor = 9,
            Host = "10.0.0.9",
            ConnectedAt = _now.AddMinutes(-1),
            LastActivity = _now.AddSeconds(-10)
        });

        var clock = () => _now;
        _dispatcher.Register(new WhoCommandHandler(_sessions, clock), "who", "session");
        _dispatcher.Register(new HideCommandHandler(_sessions), "@hide");
        _dispatcher.Register(new BootCommandHandler(_sessions, clock), "@boot");
        _dispatcher.Register(new PollCommandHandler(_sessions), "@poll");
    }

    [Fact]
    public void Who_ShowsDoingAndHidesAHiddenConnection()
    {
        var mortal = _dispatcher.Dispatch(_ada, "who", _db);

        Assert.Contains(mortal.Notifications, line => line.Contains("Player Name") && line.Contains("Doing"));
        Assert.Contains(mortal.Notifications, line => line.Contains("Ada") && line.Contains("2h 00m") && line.Contains("30s") && line.Contains("reading"));
        Assert.DoesNotContain(mortal.Notifications, line => line.Contains("Bea"));
        Assert.DoesNotContain(mortal.Notifications, line => line.Contains("Connecting"));
        Assert.Contains("There are 3 players connected.", mortal.Notifications);

        var named = _dispatcher.Dispatch(_ada, "who rea*", _db);
        Assert.Contains(named.Notifications, line => line.Contains("Ada"));
        Assert.DoesNotContain(named.Notifications, line => line.Contains("Rex"));
        Assert.Contains("There are 3 players connected.", named.Notifications);

        var wizard = _dispatcher.Dispatch(_god, "who", _db);
        Assert.Equal("Doing", wizard.Notifications[0]);
        Assert.Contains(wizard.Notifications, line => line.Contains("Host"));
        Assert.Contains(wizard.Notifications, line => line.Contains("Bea") && line.Contains("(Hide)"));
        Assert.Contains(wizard.Notifications, line => line.Contains("Connecting...") && line.Contains("10.0.0.9"));
        Assert.Contains(wizard.Notifications, line => line.Contains("Ada") && line.Contains("#0"));
        Assert.Contains("There are 4 players connected.", wizard.Notifications);

        var filtered = _dispatcher.Dispatch(_god, "who B*", _db);
        Assert.Contains(filtered.Notifications, line => line.Contains("Bea"));
        Assert.DoesNotContain(filtered.Notifications, line => line.Contains("Ada"));
        Assert.DoesNotContain(filtered.Notifications, line => line.Contains("Connecting"));
    }

    [Fact]
    public void Session_AddsByteCountsForAPrivilegedPlayer()
    {
        var mortal = _dispatcher.Dispatch(_ada, "session", _db);
        Assert.Contains(mortal.Notifications, line => line.Contains("On For") && line.Contains("Doing"));
        Assert.DoesNotContain(mortal.Notifications, line => line.Contains("Recv"));

        var seeAll = _db.GetObject(_ada)!;
        seeAll.AddPower("See_All");
        var privileged = _dispatcher.Dispatch(_ada, "session", _db);
        Assert.Contains(privileged.Notifications, line => line.Contains("Recv"));
        Assert.Contains(privileged.Notifications, line => line.Contains("Ada") && line.Contains("100") && line.Contains("40"));
        Assert.Contains(privileged.Notifications, line => line.Contains("Bea"));
    }

    [Fact]
    public void Hide_CoversTheCallersConnectionsUnlessAWizardNamesSomeoneElse()
    {
        var denied = _dispatcher.Dispatch(_ada, "@hide", _db);
        Assert.Contains("Permission denied.", denied.Notifications);

        var self = _dispatcher.Dispatch(_rex, "@hide", _db);
        Assert.Contains("You no longer appear on the WHO list.", self.Notifications);
        Assert.DoesNotContain(_dispatcher.Dispatch(_ada, "who", _db).Notifications, line => line.Contains("Rex"));

        var other = _dispatcher.Dispatch(_rex, "@hide Ada", _db);
        Assert.Contains("Permission denied.", other.Notifications);
        Assert.Contains(_dispatcher.Dispatch(_ada, "who", _db).Notifications, line => line.Contains("Ada"));

        var wizard = _dispatcher.Dispatch(_god, "@hide Ada", _db);
        Assert.Contains("Ada no longer appears on the WHO list.", wizard.Notifications);
        Assert.DoesNotContain(_dispatcher.Dispatch(_ada, "who", _db).Notifications, line => line.Contains("Ada"));

        var shown = _dispatcher.Dispatch(_god, "@hide/off Ada", _db);
        Assert.Contains("Ada now appears on the WHO list.", shown.Notifications);

        var one = _dispatcher.Dispatch(_god, "@hide 2", _db);
        Assert.Contains("Connection hidden.", one.Notifications);
        Assert.DoesNotContain(_dispatcher.Dispatch(_ada, "who", _db).Notifications, line => line.Contains("Ada"));
    }

    [Fact]
    public void Boot_DropsAnotherPlayersConnections()
    {
        var denied = _dispatcher.Dispatch(_ada, "@boot Rex", _db);
        Assert.Contains("You can't boot other people!", denied.Notifications);
        Assert.False(_sessions.IsClosed(4));

        var god = _dispatcher.Dispatch(_ada, "@boot One", _db);
        Assert.Contains("Permission denied.", god.Notifications);

        var booted = _dispatcher.Dispatch(_god, "@boot Ada", _db);
        Assert.Contains("You booted Ada off!", booted.Notifications);
        Assert.Contains("You are politely shown to the door.", booted.TargetedNotifications.Select(note => note.Message));
        Assert.True(_sessions.IsClosed(2));

        _sessions.Reopen(2);
        var quiet = _dispatcher.Dispatch(_god, "@boot/silent Ada", _db);
        Assert.Contains("You booted Ada off!", quiet.Notifications);
        Assert.DoesNotContain(quiet.TargetedNotifications, note => note.Message.Contains("politely"));
        Assert.True(_sessions.IsClosed(2));

        _sessions.SourceDescriptor = 4;
        var ownPort = _dispatcher.Dispatch(_rex, "@boot/port 4", _db);
        Assert.Contains("If you want to quit, use QUIT.", ownPort.Notifications);
        Assert.False(_sessions.IsClosed(7));

        _sessions.SourceDescriptor = 4;
        var idle = _dispatcher.Dispatch(_rex, "@boot/me", _db);
        Assert.Contains("You boot an idle self.", idle.Notifications);
        Assert.True(_sessions.IsClosed(7));
        Assert.False(_sessions.IsClosed(4));
    }

    [Fact]
    public void Poll_ChangesTheHeadingOnWho()
    {
        var shown = _dispatcher.Dispatch(_ada, "@poll", _db);
        Assert.Contains("The current poll is: Doing", shown.Notifications);

        var denied = _dispatcher.Dispatch(_ada, "@poll Mood", _db);
        Assert.Contains("Who do you think you are, Gallup?", denied.Notifications);

        var set = _dispatcher.Dispatch(_god, "@poll Mood", _db);
        Assert.Contains("Poll set to: Mood", set.Notifications);
        Assert.Equal("Mood", _dispatcher.Dispatch(_god, "who", _db).Notifications[0]);
        Assert.Contains(_dispatcher.Dispatch(_ada, "who", _db).Notifications, line => line.Contains("Mood") && !line.Contains("Doing"));

        var lost = _dispatcher.Dispatch(_god, "@poll " + new string('x', 45), _db);
        Assert.Contains(lost.Notifications, line => line.Contains("5 characters lost."));

        var reset = _dispatcher.Dispatch(_god, "@poll/clear", _db);
        Assert.Contains("Poll reset.", reset.Notifications);
        Assert.Contains(_dispatcher.Dispatch(_ada, "who", _db).Notifications, line => line.Contains("Doing"));
    }

    [Fact]
    public void WhoMogrifier_ReplacesPublicLinesAndLeavesSessionAlone()
    {
        var functions = new FunctionRegistry(_db);
        var evaluator = new ExpressionEvaluator(functions.Dispatch, _db);
        var dispatcher = new CommandDispatcher(evaluator);
        dispatcher.Register(new WhoCommandHandler(_sessions, () => _now, evaluator), "who", "session");

        var mog = _db.CreateObject("WhoMog", ObjectType.Thing, _god);
        mog.SetAttribute("WHOHEADER", "H:%0:%1%rNext", _god);
        mog.SetAttribute("WHOROW", "R:%0:%1:%4:%5:%6:%7", _god);
        mog.SetAttribute("WHOFOOTER", "F:%0:%1", _god);

        var untouched = dispatcher.Dispatch(_ada, "who", _db);
        Assert.Contains(untouched.Notifications, line => line.Contains("Player Name"));
        Assert.DoesNotContain(untouched.Notifications, line => line.StartsWith("H:"));

        _db.WhoMogrifierId = mog.Id;
        Assert.Equal(
            [
                "H:Doing:3",
                "Next",
                $"R:Ada:{_ada}:reading::7200:30",
                $"R:Rex:{_rex}:::600:120",
                $"R:Rex:{_rex}:::10800:180",
                "F:3:There are 3 players connected."
            ],
            dispatcher.Dispatch(_ada, "who", _db).Notifications);

        var named = dispatcher.Dispatch(_ada, "who rea*", _db);
        Assert.Contains($"R:Ada:{_ada}:reading::7200:30", named.Notifications);
        Assert.DoesNotContain(named.Notifications, line => line.Contains("Rex"));
        Assert.Contains("F:3:There are 3 players connected.", named.Notifications);

        var session = dispatcher.Dispatch(_rex, "session", _db);
        Assert.Contains(session.Notifications, line => line.Contains("Recv"));
        Assert.DoesNotContain(session.Notifications, line => line.StartsWith("R:"));
    }

    [Fact]
    public void WhoMogrifier_EmptyRowSuppressesAndMissingAdminKeepsTheTable()
    {
        var functions = new FunctionRegistry(_db);
        var evaluator = new ExpressionEvaluator(functions.Dispatch, _db);
        var dispatcher = new CommandDispatcher(evaluator);
        dispatcher.Register(new WhoCommandHandler(_sessions, () => _now, evaluator), "who", "session");

        var mog = _db.CreateObject("WhoMog", ObjectType.Thing, _god);
        mog.SetAttribute("WHOHEADER", "H:%0", _god);
        mog.SetAttribute("WHOROW", "", _god);
        mog.SetAttribute("WHOFOOTER", "F:%0", _god);
        _db.WhoMogrifierId = mog.Id;

        var mortal = dispatcher.Dispatch(_ada, "who", _db);
        Assert.Equal(["H:Doing", "F:3"], mortal.Notifications);

        var admin = dispatcher.Dispatch(_god, "who", _db);
        Assert.Contains(admin.Notifications, line => line.Contains("Host"));
        Assert.Contains(admin.Notifications, line => line.Contains("Bea") && line.Contains("(Hide)"));
        Assert.Contains("There are 4 players connected.", admin.Notifications);
        Assert.DoesNotContain(admin.Notifications, line => line.StartsWith("H:"));

        mog.SetAttribute("WHOAHEADER", "AH:%0:%1:%2", _god);
        mog.SetAttribute("WHOAROW", "AR:%0:%6:%7:%8", _god);
        mog.SetAttribute("WHOAFOOTER", "AF:%0", _god);
        Assert.Equal(
            [
                "AH:Doing:4:5",
                "AR:Ada:2:10.0.0.8:",
                "AR:Bea:3:10.0.0.3:Hide",
                "AR:Rex:4:10.0.0.4:",
                "AR:Rex:7:10.0.0.4:",
                "AR:Connecting...:9:10.0.0.9:Connecting",
                "AF:4"
            ],
            dispatcher.Dispatch(_god, "who", _db).Notifications);
    }

    private sealed class MemorySessions : ISessionDirectory
    {
        private readonly List<SessionInfo> _items = new();
        private readonly HashSet<int> _closed = new();

        public string Poll { get; set; } = "Doing";
        public int? SourceDescriptor { get; set; }

        public void Add(SessionInfo info) => _items.Add(info);

        public bool IsClosed(int descriptor) => _closed.Contains(descriptor);

        public void Reopen(int descriptor) => _closed.Remove(descriptor);

        public IReadOnlyList<SessionInfo> List() =>
            _items.Where(item => !_closed.Contains(item.Descriptor)).ToList();

        public void SetHidden(int descriptor, bool hidden)
        {
            var item = _items.First(session => session.Descriptor == descriptor);
            item.Hidden = hidden;
        }

        public void Disconnect(int descriptor) => _closed.Add(descriptor);

        public void FlushDisconnects()
        {
        }
    }
}
