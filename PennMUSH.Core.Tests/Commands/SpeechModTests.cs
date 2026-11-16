namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class SpeechModTests
{
    private readonly ObjectDatabase _db = new();
    private readonly ExpressionEvaluator _evaluator;
    private readonly CommandDispatcher _dispatcher = new();

    public SpeechModTests()
    {
        var registry = new FunctionRegistry(_db);
        _evaluator = new ExpressionEvaluator(registry.Dispatch, registry.Database);
        registry.Evaluator = _evaluator;
        _dispatcher.Register(new SayCommandHandler(_evaluator), "say", "\"");
        _dispatcher.Register(new PoseCommandHandler(_evaluator), "pose", ";", "semipose");
        _dispatcher.Register(new EmitCommandHandler(_evaluator), "@emit");
        _dispatcher.Register(new ExamineCommandHandler(), "examine");
    }

    [Fact]
    public void Say_UsesSpeechModForTheSpokenText()
    {
        var player = _db.CreateObject("Krenath", ObjectType.Player, Dbref.Nothing);
        player.SetAttribute("SPEECHMOD", "<span style=\"color:yellow\">%0</span>", player.Id);

        var said = _dispatcher.Dispatch(player.Id, "\"test", _db);

        Assert.Equal("You say, \"<span style=\"color:yellow\">test</span>\"", said.Notifications.Single());
    }

    [Fact]
    public void PoseAndEmit_PassTheMatchingToken()
    {
        var room = _db.CreateObject("Bridge", ObjectType.Room, Dbref.Nothing);
        var player = _db.CreateObject("Krenath", ObjectType.Player, Dbref.Nothing);
        player.Location = room.Id;
        player.SetAttribute("SPEECHMOD", "%1%0", player.Id);

        var posed = _dispatcher.Dispatch(player.Id, "pose waves", _db);
        var emitted = _dispatcher.Dispatch(player.Id, "@emit hello", _db);

        Assert.Equal("Krenath :waves", posed.Notifications.Single());
        Assert.Equal("|hello", emitted.Notifications.Single());
    }

    [Fact]
    public void EmptySpeechMod_LeavesTheOriginalText()
    {
        var player = _db.CreateObject("Krenath", ObjectType.Player, Dbref.Nothing);
        player.SetAttribute("SPEECHMOD", "", player.Id);

        var said = _dispatcher.Dispatch(player.Id, "say test", _db);

        Assert.Equal("You say, \"test\"", said.Notifications.Single());
    }

    [Fact]
    public void Examine_KeepsTheAttributeSourceAndMarksItForEscaping()
    {
        var player = _db.CreateObject("Krenath", ObjectType.Player, Dbref.Nothing);
        player.SetAttribute("SPEECHMOD", "<span style=\"color:yellow\">%0</span>", player.Id);

        var examined = _dispatcher.Dispatch(player.Id, "examine me", _db);
        var note = examined.TargetedNotifications.Single();

        Assert.Contains("<span style=\"color:yellow\">%0</span>", note.Message);
        Assert.True(note.EscapeHtml);
    }
}
