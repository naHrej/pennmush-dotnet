namespace PennMUSH.Core.Tests.Commands;

using System.Collections.Concurrent;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using PennMUSH.Server.Session;
using Xunit;

public class SocksetCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly ConcurrentDictionary<string, MUSHConnection> _connections = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _god;
    private readonly Dbref _ada;
    private readonly MUSHConnection _godConn;

    public SocksetCommandTests()
    {
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.Owner = god.Id;
        god.AddFlag("WIZARD");
        _god = god.Id;
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        ada.Owner = ada.Id;
        _ada = ada.Id;
        _godConn = new MUSHConnection(_ => System.Threading.Tasks.Task.CompletedTask) { PlayerId = _god };
        _connections[_godConn.Id] = _godConn;
        var sessions = new ConnectionDirectory(_connections, _ => { });
        var evaluator = new ExpressionEvaluator(database: _db);
        _dispatcher = new CommandDispatcher(evaluator);
        _dispatcher.Register(new SocksetCommandHandler(sessions), "@sockset");
    }

    [Fact]
    public void ShowsAndSetsTheConnectedDescriptor()
    {
        var missing = _dispatcher.Dispatch(_ada, "@sockset", _db);
        Assert.Contains("You are not connected?", missing.Notifications);

        var shown = _dispatcher.Dispatch(_god, "@sockset", _db);
        Assert.Contains("\nPueblo         :  No", shown.Notifications[0]);
        Assert.Contains("Width          :  80", shown.Notifications[0]);

        var set = _dispatcher.Dispatch(_god, "@sockset =width 100 colorstyle plain", _db);
        Assert.Contains("Width set.", set.Notifications[0]);
        Assert.Contains("Colorstyle set to 'plain'", set.Notifications[0]);
        Assert.Equal(100, _godConn.Capabilities.TerminalWidth);
        Assert.Equal("plain", _godConn.ColorStyle);

        var denied = _dispatcher.Dispatch(_ada, "@sockset One", _db);
        Assert.Contains("Permission denied.", denied.Notifications);
    }

    [Fact]
    public void RejectsAnUnknownOptionAndANonPositiveWidth()
    {
        var bad = _dispatcher.Dispatch(_god, "@sockset =sparkle yes", _db);
        Assert.Contains("@sockset option 'sparkle' is not a valid option.", bad.Notifications[0]);

        var width = _dispatcher.Dispatch(_god, "@sockset =width 0", _db);
        Assert.Contains("Width expects a positive integer.", width.Notifications[0]);
    }
}
