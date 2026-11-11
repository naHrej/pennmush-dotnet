namespace PennMUSH.Core.Tests.Commands;

using System;
using Xunit;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class ExtendedCommandTests
{
    private readonly ObjectDatabase _db;
    private readonly CommandDispatcher _dispatcher;
    private readonly ExpressionEvaluator _evaluator;
    private readonly FunctionRegistry _registry;
    private readonly MUSHObject _player;
    private readonly MUSHObject _room;
    private readonly MUSHObject _wand;

    public ExtendedCommandTests()
    {
        _db = new ObjectDatabase();
        _registry = new FunctionRegistry(_db);
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;

        _player = _db.CreateObject("Wizard", ObjectType.Player, new Dbref(1));
        _room = _db.CreateObject("Great Hall", ObjectType.Room, _player.Id);
        _wand = _db.CreateObject("Wand", ObjectType.Thing, _player.Id);

        _player.Location = _room.Id;
        _wand.Location = _room.Id;

        _dispatcher = new CommandDispatcher();
        _dispatcher.Register(new SetCommandHandler());
        _dispatcher.Register(new ThinkCommandHandler(_evaluator));
        _dispatcher.Register(new SayCommandHandler(_evaluator), "\"");
        _dispatcher.Register(new NameCommandHandler());
        _dispatcher.Register(new TeleportCommandHandler(), "@tel");
        _dispatcher.Register(new LookCommandHandler(_evaluator), "l");
    }

    [Fact]
    public void SetCommand_SetsAttributeOnObject()
    {
        var cmd = string.Format("@set {0}=DESCRIBE:A glowing golden wand.", _wand.Id);
        var ctx = _dispatcher.Dispatch(_player.Id, cmd, _db);

        Assert.Contains("Set.", ctx.Notifications);
        var attr = _wand.GetAttribute("DESCRIBE");
        Assert.NotNull(attr);
        Assert.Equal("A glowing golden wand.", attr.Value);
    }

    [Fact]
    public void ThinkCommand_EvaluatesSoftcodeExpression()
    {
        var ctx = _dispatcher.Dispatch(_player.Id, "think [add(15, 25)]", _db);

        Assert.Single(ctx.Notifications);
        Assert.Equal("40", ctx.Notifications[0]);
    }

    [Fact]
    public void SayCommand_EvaluatesSoftcodeBeforeSpeaking()
    {
        var ctx = _dispatcher.Dispatch(_player.Id, "say The sum is [add(15, 25)].", _db);

        Assert.Contains("You say, \"The sum is 40.\"", ctx.Notifications);
    }

    [Fact]
    public void SayCommand_NoEvalSwitchKeepsSoftcodeLiteral()
    {
        var ctx = _dispatcher.Dispatch(_player.Id, "say/noeval The sum is [add(15, 25)].", _db);

        Assert.Contains("You say, \"The sum is [add(15, 25)].\"", ctx.Notifications);
    }

    [Fact]
    public void SayCommand_SubstitutesEnactorNameAndPronoun()
    {
        _player.SetAttribute("SEX", "female", _player.Id);
        var ctx = _dispatcher.Dispatch(_player.Id, "say %N says %s is here.", _db);

        Assert.Contains("You say, \"Wizard says she is here.\"", ctx.Notifications);
    }

    [Fact]
    public void ThinkCommand_EvaluatesStringFunctions()
    {
        var ctx = _dispatcher.Dispatch(_player.Id, "think Hello [ucstr(world)]!", _db);

        Assert.Single(ctx.Notifications);
        Assert.Equal("Hello WORLD!", ctx.Notifications[0]);
    }

    [Fact]
    public void NameCommand_RenamesTargetObject()
    {
        var cmd = string.Format("@name {0}=Elder Wand", _wand.Id);
        var ctx = _dispatcher.Dispatch(_player.Id, cmd, _db);

        Assert.Contains("Name set.", ctx.Notifications);
        Assert.Equal("Elder Wand", _wand.Name);
    }

    [Fact]
    public void TeleportCommand_RelocatesObject()
    {
        var newRoom = _db.CreateObject("Observatory", ObjectType.Room, _player.Id);

        var cmd = string.Format("@teleport {0}={1}", _wand.Id, newRoom.Id);
        var ctx = _dispatcher.Dispatch(_player.Id, cmd, _db);

        Assert.Contains("Teleported.", ctx.Notifications);
        Assert.Equal(newRoom.Id, _wand.Location);
    }

    [Fact]
    public void LookCommand_DisplaysNameAndEvaluatedDescription()
    {
        _wand.SetAttribute("DESCRIBE", "Value is [mul(3, 4)].", _player.Id);

        var cmd = string.Format("look {0}", _wand.Id);
        var ctx = _dispatcher.Dispatch(_player.Id, cmd, _db);

        Assert.Contains("Wand", ctx.Notifications);
        Assert.Contains("Value is 12.", ctx.Notifications);
    }
}
