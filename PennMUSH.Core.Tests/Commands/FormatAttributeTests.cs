namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class FormatAttributeTests
{
    private readonly ObjectDatabase _db = new();
    private readonly ExpressionEvaluator _evaluator;
    private readonly CommandDispatcher _dispatcher = new();
    private readonly ChannelRegistry _channels = new();

    public FormatAttributeTests()
    {
        var registry = new FunctionRegistry(_db);
        _evaluator = new ExpressionEvaluator(registry.Dispatch, registry.Database);
        registry.Evaluator = _evaluator;
        _dispatcher.Register(new LookCommandHandler(_evaluator), "look");
        _dispatcher.Register(new InventoryCommandHandler(_evaluator), "inventory");
        _dispatcher.Register(new PageCommandHandler(_evaluator), "page");
        _dispatcher.Register(new ChatCommandHandler(_channels, _evaluator), "@chat");
    }

    [Fact]
    public void Look_UsesDescriptionNameContentsAndExitFormats()
    {
        var room = _db.CreateObject("Hall", ObjectType.Room, new Dbref(1));
        room.SetAttribute("DESCRIBE", "Stone.", new Dbref(1));
        room.SetAttribute("DESCFORMAT", "Scene: %0", new Dbref(1));
        room.SetAttribute("NAMEFORMAT", "Place %0", new Dbref(1));
        room.SetAttribute("CONFORMAT", "Inside: %1", new Dbref(1));
        room.SetAttribute("EXITFORMAT", "Doors: %0", new Dbref(1));
        var player = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        var lamp = _db.CreateObject("Lamp;lantern", ObjectType.Thing, new Dbref(1));
        var north = _db.CreateObject("North;n", ObjectType.Exit, new Dbref(1));
        _db.MoveObject(player.Id, room.Id);
        _db.MoveObject(lamp.Id, room.Id);
        _db.AttachExit(room.Id, north.Id);

        var looked = _dispatcher.Dispatch(player.Id, "look", _db);

        Assert.Contains("Place " + room.Id, looked.Notifications);
        Assert.Contains("Scene: Stone.", looked.Notifications);
        Assert.Contains("Inside: Lamp", looked.Notifications);
        Assert.Contains("Doors: " + north.Id, looked.Notifications);
        Assert.DoesNotContain("Contents:", looked.Notifications);
        Assert.DoesNotContain("Obvious exits:", looked.Notifications);
        Assert.DoesNotContain("Hall", looked.Notifications);
    }

    [Fact]
    public void LookInside_PrefersTheInsideFormat()
    {
        var box = _db.CreateObject("Box", ObjectType.Thing, new Dbref(1));
        box.SetAttribute("DESCRIBE", "A wooden box.", new Dbref(1));
        box.SetAttribute("IDESCFORMAT", "Within: %0", new Dbref(1));
        var player = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        _db.MoveObject(player.Id, box.Id);

        var looked = _dispatcher.Dispatch(player.Id, "look", _db);

        Assert.Contains("Within: A wooden box.", looked.Notifications);
        Assert.DoesNotContain(looked.Notifications, line => line == "A wooden box.");
    }

    [Fact]
    public void LookAtObject_UsesDescformatButNotNameformat()
    {
        var room = _db.CreateObject("Hall", ObjectType.Room, new Dbref(1));
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, new Dbref(1));
        lamp.SetAttribute("DESCRIBE", "Brass.", new Dbref(1));
        lamp.SetAttribute("DESCFORMAT", "Seen: %0", new Dbref(1));
        lamp.SetAttribute("NAMEFORMAT", "Renamed", new Dbref(1));
        var player = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        _db.MoveObject(player.Id, room.Id);
        _db.MoveObject(lamp.Id, room.Id);

        var looked = _dispatcher.Dispatch(player.Id, "look lamp", _db);

        Assert.Contains("Lamp", looked.Notifications);
        Assert.Contains("Seen: Brass.", looked.Notifications);
        Assert.DoesNotContain("Renamed", looked.Notifications);
    }

    [Fact]
    public void InventoryAndPage_UseTheirFormatAttributes()
    {
        var room = _db.CreateObject("Hall", ObjectType.Room, new Dbref(1));
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        var bea = _db.CreateObject("Bea", ObjectType.Player, new Dbref(1));
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, ada.Id);
        _db.MoveObject(ada.Id, room.Id);
        _db.MoveObject(bea.Id, room.Id);
        _db.MoveObject(lamp.Id, ada.Id);
        ada.SetAttribute("INVFORMAT", "Carried %1", ada.Id);
        ada.SetAttribute("OUTPAGEFORMAT", "Sent %0 to %3", ada.Id);
        bea.SetAttribute("PAGEFORMAT", "Heard %0 from %4", bea.Id);

        var carried = _dispatcher.Dispatch(ada.Id, "inventory", _db);
        var paged = _dispatcher.Dispatch(ada.Id, "page Bea=hello", _db);

        Assert.Equal("Carried Lamp", carried.Notifications.Single());
        Assert.Contains("Sent hello to " + bea.Id, paged.Notifications);
        Assert.Contains(paged.TargetedNotifications, note => note.Target == bea.Id && note.Message == "Heard hello from Ada pages: hello");
    }

    [Fact]
    public void Chatformat_ReplacesOneListenerAndCanSilenceAnother()
    {
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        var bea = _db.CreateObject("Bea", ObjectType.Player, new Dbref(1));
        var cam = _db.CreateObject("Cam", ObjectType.Player, new Dbref(1));
        _channels.CreateChannel("Public", ada.Id);
        var channel = _channels.GetChannel("Public")!;
        channel.AddMember(ada.Id);
        channel.AddMember(bea.Id);
        channel.AddMember(cam.Id);
        bea.SetAttribute("CHATFORMAT", "<%1> %3: %2", bea.Id);
        cam.SetAttribute("CHATFORMAT", "", cam.Id);

        var said = _dispatcher.Dispatch(ada.Id, "@chat Public=hello", _db);

        Assert.Contains("[Public] Ada: hello", said.Notifications);
        Assert.Contains(said.TargetedNotifications, note => note.Target == bea.Id && note.Message == "<Public> Ada: hello");
        Assert.DoesNotContain(said.TargetedNotifications, note => note.Target == cam.Id);
    }
}
