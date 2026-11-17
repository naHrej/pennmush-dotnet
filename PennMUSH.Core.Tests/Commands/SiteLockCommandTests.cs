namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Access;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class SiteLockCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly SiteLockTable _table = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _god;
    private readonly Dbref _ada;

    public SiteLockCommandTests()
    {
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.Owner = god.Id;
        god.AddFlag("WIZARD");
        _god = god.Id;
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        ada.Owner = ada.Id;
        _ada = ada.Id;
        var evaluator = new ExpressionEvaluator(database: _db);
        _dispatcher = new CommandDispatcher(evaluator);
        _dispatcher.Register(new SiteLockCommandHandler(_table), "@sitelock");
    }

    [Fact]
    public void MortalIsRefused()
    {
        var result = _dispatcher.Dispatch(_ada, "@sitelock", _db);
        Assert.Contains("Your delusions of grandeur have been noted.", result.Notifications);
    }

    [Fact]
    public void BanListsAndBlocksTheHost()
    {
        var empty = _dispatcher.Dispatch(_god, "@sitelock", _db);
        Assert.Contains("There are no access rules.", empty.Notifications);

        var banned = _dispatcher.Dispatch(_god, "@sitelock /ban 10.0.0.*", _db);
        Assert.Contains("Site 10.0.0.* banned", banned.Notifications);
        Assert.False(_table.Can("10.0.0.5", SiteLockTable.Connect));
        Assert.True(_table.Can("10.1.0.5", SiteLockTable.Connect));

        var listed = _dispatcher.Dispatch(_god, "@sitelock", _db);
        Assert.Contains("---- @sitelock will add sites immediately below this line ----", listed.Notifications);
        Assert.Contains($"  2 SITE: {"10.0.0.*".PadRight(20)}  DBREF: {"#-2".PadRight(6)} FLAGS: !connect !create !guest", listed.Notifications);

        var check = _dispatcher.Dispatch(_god, "@sitelock/check 10.0.0.5", _db);
        Assert.Contains("Matched line 2: 10.0.0.* ", check.Notifications[0]);
        Assert.Contains("Connections allowed by: No one", check.Notifications[0]);
        Assert.Contains("Creation is NOT allowed", check.Notifications[0]);

        var locked = _dispatcher.Dispatch(_god, "@sitelock 192.168.1.*", _db);
        Assert.Contains("Site 192.168.1.* locked", locked.Notifications);
        Assert.True(_table.Can("192.168.1.4", SiteLockTable.Connect));
        Assert.False(_table.Can("192.168.1.4", SiteLockTable.Create));

        var removed = _dispatcher.Dispatch(_god, "@sitelock/remove 10.0.0.*", _db);
        Assert.Contains("1 sitelocks removed.", removed.Notifications);
        Assert.True(_table.Can("10.0.0.5", SiteLockTable.Connect));
    }
}
