namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class ShutdownCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _god;
    private readonly Dbref _wiz;
    private readonly Dbref _ada;
    private int _saves;
    private int _stops;
    private int _reboots;
    private bool _saveFails;

    public ShutdownCommandTests()
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
        _dispatcher.Register(new ShutdownCommandHandler(Save, () => _stops++, () => _reboots++), "@shutdown");
    }

    [Fact]
    public void MortalAndWizardPanicAreRefused()
    {
        var mortal = _dispatcher.Dispatch(_ada, "@shutdown", _db);
        Assert.Contains("Permission denied.", mortal.Notifications);
        Assert.Equal(0, _stops);

        var panic = _dispatcher.Dispatch(_wiz, "@shutdown/panic", _db);
        Assert.Contains("It takes a God to make me panic.", panic.Notifications);
        Assert.Equal(0, _saves);
        Assert.Equal(0, _stops);
    }

    [Fact]
    public void ShutdownSavesAndAnnounces()
    {
        var result = _dispatcher.Dispatch(_wiz, "@shutdown", _db);
        Assert.Contains("GAME: Shutdown by Wiz", result.Notifications);
        Assert.Equal(1, _saves);
        Assert.Equal(1, _stops);
        Assert.Equal(0, _reboots);
    }

    [Fact]
    public void GodPanicAnnouncesEmergencyAndStillStops()
    {
        _saveFails = true;
        var result = _dispatcher.Dispatch(_god, "@shutdown/panic", _db);
        Assert.Contains("GAME: ERROR! Database save failed!", result.Notifications);
        Assert.Contains("GAME: Shutdown by One", result.Notifications);
        Assert.Contains("EMERGENCY SHUTDOWN: @shutdown/panic", result.Notifications);
        Assert.Equal(1, _stops);
        Assert.Equal(0, _reboots);
    }

    [Fact]
    public void RebootKeepsTheProcessUpUnlessTheSaveFails()
    {
        var result = _dispatcher.Dispatch(_wiz, "@shutdown/reboot", _db);
        Assert.Contains("GAME: Reboot w/o disconnect by Wiz, please wait.", result.Notifications);
        Assert.Equal(1, _reboots);
        Assert.Equal(0, _stops);

        var spaced = _dispatcher.Dispatch(_wiz, "@shutdown /reboot", _db);
        Assert.Contains("GAME: Reboot w/o disconnect by Wiz, please wait.", spaced.Notifications);
        Assert.Equal(2, _reboots);
        Assert.Equal(0, _stops);

        _saveFails = true;
        var failed = _dispatcher.Dispatch(_wiz, "@shutdown/reboot", _db);
        Assert.Contains("GAME: Reboot failed.", failed.Notifications);
        Assert.Equal(2, _reboots);
        Assert.Equal(0, _stops);
    }

    private void Save()
    {
        _saves++;
        if (_saveFails)
            throw new InvalidOperationException("disk full");
    }
}
