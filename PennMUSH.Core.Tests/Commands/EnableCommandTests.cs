namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Configuration;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class EnableCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly MushConfiguration _config = new() { UseQuota = true, PlayerCreation = true };
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _ada;
    private readonly Dbref _god;

    public EnableCommandTests()
    {
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.Owner = god.Id;
        _god = god.Id;
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        ada.Owner = ada.Id;
        _ada = ada.Id;

        var evaluator = new ExpressionEvaluator(database: _db);
        _dispatcher = new CommandDispatcher(evaluator);
        _dispatcher.Register(new EnableCommandHandler(_config, false), "@disable");
        _dispatcher.Register(new EnableCommandHandler(_config, true), "@enable");
        _dispatcher.Register(new QuotaCommandHandler(liveUseQuota: () => _config.UseQuota), "@quota");
    }

    [Fact]
    public void DisableAndEnable_ChangeABooleanOption()
    {
        Assert.Contains("Permission denied.", _dispatcher.Dispatch(_ada, "@disable use_quota", _db).Notifications);
        Assert.True(_config.UseQuota);

        var limited = _dispatcher.Dispatch(_ada, "@quota", _db);
        Assert.DoesNotContain(limited.Notifications, note => note.Contains("UNLIMITED"));

        var off = _dispatcher.Dispatch(_god, "@disable use_quota", _db);
        Assert.Contains("Disabled.", off.Notifications);
        Assert.False(_config.UseQuota);
        Assert.Equal("no", _config.Directives["use_quota"]);
        Assert.Contains("UNLIMITED", _dispatcher.Dispatch(_ada, "@quota", _db).Notifications.First());

        var creation = _dispatcher.Dispatch(_god, "@disable PLAYER_CREATION", _db);
        Assert.Contains("Disabled.", creation.Notifications);
        Assert.False(_config.PlayerCreation);

        _db.GetObject(_god)!.AddFlag("GAGGED");
        var gagged = _dispatcher.Dispatch(_god, "@enable use_quota", _db);
        Assert.Contains("You cannot do that while gagged.", gagged.Notifications);
        Assert.False(_config.UseQuota);
        Assert.Contains("Disabled.", _dispatcher.Dispatch(_god, "@disable guests", _db).Notifications);
        Assert.False(_config.Guests);

        _db.GetObject(_god)!.RemoveFlag("GAGGED");
        Assert.Contains("Enabled.", _dispatcher.Dispatch(_god, "@enable use_quota", _db).Notifications);
        Assert.True(_config.UseQuota);
        Assert.Equal("yes", _config.Directives["use_quota"]);
        Assert.DoesNotContain(_dispatcher.Dispatch(_ada, "@quota", _db).Notifications, note => note.Contains("UNLIMITED"));
    }

    [Fact]
    public void Enable_RejectsAMissingNameAndAValueThatIsNotASwitch()
    {
        Assert.Contains("No such option.", _dispatcher.Dispatch(_god, "@enable", _db).Notifications);
        Assert.Contains("No such option.", _dispatcher.Dispatch(_god, "@disable nosuch", _db).Notifications);
        Assert.Contains("That isn't an on/off option.", _dispatcher.Dispatch(_god, "@enable port", _db).Notifications);
        Assert.Equal(4201, _config.Port);
    }
}
