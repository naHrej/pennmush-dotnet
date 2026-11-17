namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Configuration;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class ConfigCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly MushConfiguration _config = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _god;
    private readonly Dbref _wiz;
    private readonly Dbref _ada;

    public ConfigCommandTests()
    {
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.Owner = god.Id;
        god.AddFlag("WIZARD");
        _god = god.Id;
        var wiz = _db.CreateObject("Wiz", ObjectType.Player, new Dbref(1));
        wiz.Owner = wiz.Id;
        wiz.AddFlag("WIZARD");
        _wiz = wiz.Id;
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        ada.Owner = ada.Id;
        _ada = ada.Id;
        var evaluator = new ExpressionEvaluator(database: _db);
        _dispatcher = new CommandDispatcher(evaluator);
        _dispatcher.Register(new ConfigCommandHandler(_config), "@config");
    }

    [Fact]
    public void ListShowsParsedOptionsAndHidesFilePathsFromMortals()
    {
        var bare = _dispatcher.Dispatch(_ada, "@config", _db);
        Assert.Contains("Use: @config/list <type of options> where type is one of:", bare.Notifications);
        Assert.Contains(" net             Networking and connection-related options", bare.Notifications);
        Assert.DoesNotContain(bare.Notifications, line => line.Contains("files"));

        var net = _dispatcher.Dispatch(_ada, "@config net", _db);
        Assert.Contains("Networking and connection-related options", net.Notifications);
        Assert.Contains($" {"mud_name".PadRight(40)} PennMUSH", net.Notifications);
        Assert.Contains($" {"use_ws".PadRight(40)} Yes", net.Notifications);

        var missing = _dispatcher.Dispatch(_ada, "@config nosuch", _db);
        Assert.Contains("I only know the following types of options:", missing.Notifications);

        var files = _dispatcher.Dispatch(_god, "@config files", _db);
        Assert.Contains("Files used by the MUSH", files.Notifications);
        Assert.Contains($" {"input_database".PadRight(40)} data/indb", files.Notifications);
    }

    [Fact]
    public void SetChangesMemoryAndSaveDoesNotWrite()
    {
        var denied = _dispatcher.Dispatch(_ada, "@config/set mud_name=Nope", _db);
        Assert.Contains("You can't remake the world in your image.", denied.Notifications);
        Assert.Equal("PennMUSH", _config.MudName);

        var set = _dispatcher.Dispatch(_wiz, "@config/set mud_name=Renamed", _db);
        Assert.Contains("Option set.", set.Notifications);
        Assert.Equal("Renamed", _config.MudName);
        Assert.Equal("Renamed", _config.Directives["mud_name"]);

        var wizardSave = _dispatcher.Dispatch(_wiz, "@config/save mud_name=Later", _db);
        Assert.Contains("You can't remake the world in your image.", wizardSave.Notifications);
        Assert.Equal("Renamed", _config.MudName);

        var saved = _dispatcher.Dispatch(_god, "@config/save port=not-a-number", _db);
        Assert.Contains("Couldn't set that option.", saved.Notifications);

        var godSave = _dispatcher.Dispatch(_god, "@config/save port=4202", _db);
        Assert.Contains("Option set but not saved (Saves disabled.)", godSave.Notifications);
        Assert.Equal(4202, _config.Port);
    }
}
