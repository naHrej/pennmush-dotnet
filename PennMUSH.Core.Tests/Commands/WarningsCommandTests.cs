namespace PennMUSH.Core.Tests.Commands;

using System.IO;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class WarningsCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _ada;
    private readonly Dbref _god;
    private readonly Dbref _lamp;

    public WarningsCommandTests()
    {
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.Owner = god.Id;
        god.AddFlag("WIZARD");
        _god = god.Id;
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        ada.Owner = ada.Id;
        _ada = ada.Id;
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, ada.Id);
        _lamp = lamp.Id;
        var evaluator = new ExpressionEvaluator(database: _db);
        _dispatcher = new CommandDispatcher(evaluator);
        _dispatcher.Register(new WarningsCommandHandler(), "@warnings");
    }

    [Fact]
    public void SetClearAndNameTheBits()
    {
        var set = _dispatcher.Dispatch(_ada, "@warnings lamp=normal", _db);
        Assert.Contains("@warnings set to: normal ", set.Notifications);
        Assert.Equal(WarningBits.Normal, _db.GetObject(_lamp)!.Warnings);

        var same = _dispatcher.Dispatch(_ada, "@warnings lamp=normal", _db);
        Assert.Contains("@warnings not changed.", same.Notifications);

        var narrowed = _dispatcher.Dispatch(_ada, "@warnings lamp=all !exit-desc", _db);
        Assert.Contains("@warnings set to: extra ", narrowed.Notifications);
        Assert.Equal(WarningBits.Extra, _db.GetObject(_lamp)!.Warnings);

        var two = _dispatcher.Dispatch(_ada, "@warnings lamp=thing-desc room-desc", _db);
        Assert.Contains("@warnings set to: room-desc thing-desc ", two.Notifications);

        var unknown = _dispatcher.Dispatch(_ada, "@warnings lamp=zz thing-desc", _db);
        Assert.Contains("Unknown warning: zz", unknown.Notifications);
        Assert.Contains("@warnings set to: thing-desc ", unknown.Notifications);

        var every = _dispatcher.Dispatch(_ada, "@warnings lamp=nope", _db);
        Assert.Contains("Unknown warning: nope", every.Notifications);
        Assert.Contains("@warnings set to: all none ", every.Notifications);
        Assert.Equal(uint.MaxValue, _db.GetObject(_lamp)!.Warnings);

        var cleared = _dispatcher.Dispatch(_ada, "@warnings lamp=none", _db);
        Assert.Contains("@warnings cleared.", cleared.Notifications);
        Assert.Equal(WarningBits.None, _db.GetObject(_lamp)!.Warnings);
    }

    [Fact]
    public void RefusesWhatThePlayerDoesNotControl()
    {
        var missing = _dispatcher.Dispatch(_ada, "@warnings nosuch=normal", _db);
        Assert.Contains("I don't see that object.", missing.Notifications);

        var denied = _dispatcher.Dispatch(_ada, "@warnings #1=normal", _db);
        Assert.Contains("Permission denied.", denied.Notifications);
        Assert.Equal(WarningBits.None, _db.GetObject(_god)!.Warnings);

        _db.GetObject(_lamp)!.AddFlag("GOING");
        var garbage = _dispatcher.Dispatch(_ada, "@warnings lamp=normal", _db);
        Assert.Contains("Why would you want to be warned about garbage?", garbage.Notifications);
    }

    [Fact]
    public void NewPlayerStartsOnNormalAndTheBitsRoundTrip()
    {
        var messages = new System.Collections.Generic.List<string>();
        Dbref created = PlayerCreator.Create(
            _db, _db.GetObject(_god)!, "Bea", "secret", null, null, new Dbref(0), new Dbref(0), 0, messages.Add);
        Assert.Equal(WarningBits.Normal, _db.GetObject(created)!.Warnings);

        _db.GetObject(_lamp)!.Warnings = WarningBits.Normal;
        string path = Path.Combine(Path.GetTempPath(), "warnings-" + Path.GetRandomFileName() + ".db");
        try
        {
            var store = new SqliteStore($"Data Source={path}");
            store.Save(_db);
            var loaded = new ObjectDatabase();
            Assert.True(store.Load(loaded));
            Assert.Equal(WarningBits.Normal, loaded.GetObject(_lamp)!.Warnings);
            Assert.Equal(WarningBits.Normal, loaded.GetObject(created)!.Warnings);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
