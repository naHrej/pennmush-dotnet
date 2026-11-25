namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class IncludeCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _ada;
    private readonly Dbref _bea;
    private readonly Dbref _god;

    public IncludeCommandTests()
    {
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.Owner = god.Id;
        _god = god.Id;
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        ada.Owner = ada.Id;
        _ada = ada.Id;
        var bea = _db.CreateObject("Bea", ObjectType.Player, new Dbref(1));
        bea.Owner = bea.Id;
        _bea = bea.Id;
        var evaluator = new ExpressionEvaluator(database: _db);
        _dispatcher = new CommandDispatcher(evaluator);
        _dispatcher.Register(new IncludeCommandHandler(_dispatcher, evaluator), "@include");
        _dispatcher.Register(new PemitCommandHandler(), "@pemit");
    }

    [Fact]
    public void Include_ReportsAMissingObjectOrAttribute()
    {
        var noAttribute = _dispatcher.Dispatch(_ada, "@include Ada", _db);
        Assert.Contains("I need to know what attribute to include.", noAttribute.Notifications);

        var missing = _dispatcher.Dispatch(_ada, "@include Nobody/FOO", _db);
        Assert.Contains("I can't see that here.", missing.Notifications);

        var absent = _dispatcher.Dispatch(_ada, "@include Bea/FOO", _db);
        Assert.Contains("No such attribute.", absent.Notifications);

        _db.GetObject(_god)!.SetAttribute("SECRET", "@pemit me=hidden", _god);
        var denied = _dispatcher.Dispatch(_ada, "@include One/SECRET", _db);
        Assert.Contains("You can't include God!", denied.Notifications);
        Assert.DoesNotContain(denied.Notifications, note => note.Contains("hidden"));
    }

    [Fact]
    public void Include_RunsEachCommandAsTheIncluder()
    {
        _db.GetObject(_bea)!.SetAttribute(
            "FOO",
            "$greet:@pemit me=Hello %0\n@pemit me=Again %1;{@pemit me=left;right}",
            _bea);

        var sent = _dispatcher.Dispatch(_ada, "@include/localize Bea/FOO=Ada,west", _db);
        Assert.Equal(
            new[] { "Hello Ada", "Again west", "left;right" },
            sent.TargetedNotifications
                .Where(note => note.Scope == NotificationScope.Target)
                .Select(note => note.Message));
        Assert.DoesNotContain("Message sent to Ada.", sent.Notifications);
        Assert.DoesNotContain(sent.Notifications, note => note.Contains("Bea."));
    }

    [Fact]
    public void Include_UsesAnAttributeFromTheParent()
    {
        var parent = _db.CreateObject("Form", ObjectType.Thing, _ada);
        parent.SetAttribute("FOO", "@pemit me=inherited", _ada);
        var child = _db.CreateObject("Copy", ObjectType.Thing, _ada);
        child.AddParent(parent.Id, _db);

        var sent = _dispatcher.Dispatch(_ada, "@include Copy/FOO", _db);
        Assert.Contains("inherited", sent.Notifications);
    }
}
