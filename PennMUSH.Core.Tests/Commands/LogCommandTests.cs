namespace PennMUSH.Core.Tests.Commands;

using System.IO;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Logging;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class LogCommandTests : IDisposable
{
    private readonly string _directory;
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly WizardLog _log;
    private readonly Dbref _god;
    private readonly Dbref _wiz;
    private readonly Dbref _ada;

    public LogCommandTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "penn-log-" + Guid.NewGuid().ToString("N"));
        _log = new WizardLog(_directory, () => new DateTime(2026, 11, 17, 11, 3, 0));
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
        _dispatcher.Register(new LogCommandHandler(_log), "@log");
        _dispatcher.Register(new LogWipeCommandHandler(_log), "@logwipe");
        _dispatcher.AddRestriction("@log", "nogagged", null);
        _dispatcher.AddRestriction("@logwipe", "nogagged", null);
    }

    [Fact]
    public void MortalCannotWriteTheLog()
    {
        var result = _dispatcher.Dispatch(_ada, "@log hello", _db);
        Assert.Contains("Permission denied.", result.Notifications);
        Assert.False(File.Exists(Path.Combine(_directory, "command.log")));
    }

    [Fact]
    public void WizardWritesAndRecallsALine()
    {
        var written = _dispatcher.Dispatch(_wiz, "@log hello there", _db);
        Assert.Contains("Logged.", written.Notifications);
        Assert.Equal(
            "[2026-11-17 11:03:00] LOG: Wiz(#2PW): hello there" + Environment.NewLine,
            File.ReadAllText(Path.Combine(_directory, "command.log")));

        var recalled = _dispatcher.Dispatch(_wiz, "@log/recall", _db);
        Assert.Equal(
            ["Begin log recall.", "LOG: Wiz(#2PW): hello there", "End log recall."],
            recalled.Notifications);
    }

    [Fact]
    public void RecallKeepsTheRequestedTail()
    {
        _dispatcher.Dispatch(_wiz, "@log one", _db);
        _dispatcher.Dispatch(_wiz, "@log two", _db);
        var recalled = _dispatcher.Dispatch(_wiz, "@log /recall 1", _db);
        Assert.Equal(
            ["Begin log recall.", "LOG: Wiz(#2PW): two", "End log recall."],
            recalled.Notifications);
    }

    [Fact]
    public void ASpacedSwitchSelectsTheLog()
    {
        var result = _dispatcher.Dispatch(_wiz, "@log /err bridge is down", _db);
        Assert.Contains("Logged.", result.Notifications);
        Assert.Contains("LOG: Wiz(#2PW): bridge is down", File.ReadAllText(Path.Combine(_directory, "error.log")));
        Assert.False(File.Exists(Path.Combine(_directory, "command.log")));
    }

    [Fact]
    public void WipeRequiresGodAndThePassword()
    {
        var wizard = _dispatcher.Dispatch(_wiz, "@logwipe zap!", _db);
        Assert.Contains("Only God can do that.", wizard.Notifications);

        var wrong = _dispatcher.Dispatch(_god, "@logwipe nope", _db);
        Assert.Contains("Wrong password.", wrong.Notifications);
        Assert.Contains(
            "WIZ: One(#1PW): Invalid attempt to wipe the error log, password 'nope'",
            File.ReadAllText(Path.Combine(_directory, "wizard.log")));

        _dispatcher.Dispatch(_wiz, "@log keep me", _db);
        var wiped = _dispatcher.Dispatch(_god, "@logwipe /cmd zap!", _db);
        Assert.Contains("Log wiped.", wiped.Notifications);
        Assert.Equal(
            "*** LOG WAS WIPED AFTER GROWING TOO LARGE ***" + Environment.NewLine,
            File.ReadAllText(Path.Combine(_directory, "command.log")));
        Assert.Contains("RPT: command log wiped.", File.ReadAllText(Path.Combine(_directory, "error.log")));
    }

    [Fact]
    public void RotateKeepsACopyAndTrimKeepsTheTail()
    {
        _dispatcher.Dispatch(_wiz, "@log/cmd first", _db);
        _dispatcher.Dispatch(_wiz, "@log/cmd second", _db);
        var rotated = _dispatcher.Dispatch(_god, "@logwipe/cmd/rotate zap!", _db);
        Assert.Contains("Log wiped.", rotated.Notifications);
        Assert.Contains("LOG: Wiz(#2PW): first", File.ReadAllText(Path.Combine(_directory, "command.log.1")));
        Assert.StartsWith("*** LOG WAS ROTATED AFTER GROWING TOO LARGE ***", File.ReadAllText(Path.Combine(_directory, "command.log")));

        for (int n = 1; n <= 10; n++)
            _dispatcher.Dispatch(_wiz, $"@log/trace line {n}", _db);
        _dispatcher.Dispatch(_god, "@logwipe/trace/trim zap!", _db);
        string[] kept = File.ReadAllLines(Path.Combine(_directory, "trace.log"));
        Assert.Single(kept);
        Assert.Contains("line 10", kept[0]);
    }

    [Fact]
    public void AGaggedWizardCannotLog()
    {
        _db.GetObject(_wiz)!.AddFlag("GAGGED");
        var result = _dispatcher.Dispatch(_wiz, "@log hello", _db);
        Assert.Contains("You cannot do that while gagged.", result.Notifications);
        Assert.False(File.Exists(Path.Combine(_directory, "command.log")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
