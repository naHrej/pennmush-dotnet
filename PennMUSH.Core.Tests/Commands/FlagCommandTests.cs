namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Flags;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class FlagCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _ada;
    private readonly Dbref _god;
    private readonly Dbref _widget;

    public FlagCommandTests()
    {
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.Owner = god.Id;
        _god = god.Id;
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        ada.Owner = ada.Id;
        _ada = ada.Id;
        var widget = _db.CreateObject("Widget", ObjectType.Thing, new Dbref(1));
        widget.Owner = ada.Id;
        _widget = widget.Id;

        var evaluator = new ExpressionEvaluator(database: _db);
        _dispatcher = new CommandDispatcher(evaluator);
        _dispatcher.Register(new FlagCommandHandler(), "@flag");
    }

    [Fact]
    public void GodDefinesAFlagAndChangesItsLetterTypeAndAlias()
    {
        try
        {
            Assert.Contains("You don't have enough magic for that.", _dispatcher.Dispatch(_ada, "@flag/add CUSTOM=1,PLAYER,wizard", _db).Notifications);

            var added = _dispatcher.Dispatch(_god, "@flag/add CUSTOM = 1, PLAYER, wizard", _db);
            Assert.Contains("     Name: CUSTOM", added.Notifications);
            Assert.Contains("Character: 1", added.Notifications);
            Assert.Contains("  Type(s): PLAYER", added.Notifications);
            Assert.Contains("    Perms: wizard", added.Notifications);
            Assert.Contains("ResetPrms: wizard", added.Notifications);
            Assert.Contains("CUSTOM (1)", _dispatcher.Dispatch(_ada, "@flag/list CUSTOM", _db).Notifications.Single());

            var alias = _dispatcher.Dispatch(_god, "@flag/alias CUSTOM=FOO", _db);
            Assert.Contains("  Aliases: FOO", alias.Notifications);
            Assert.Equal("CUSTOM", FlagTable.FindFlag("FOO")!.Name);

            Assert.Contains("Letter for flag CUSTOM cleared.", _dispatcher.Dispatch(_god, "@flag/letter CUSTOM=", _db).Notifications);
            Assert.Contains("Permissions on CUSTOM flag set.", _dispatcher.Dispatch(_god, "@flag/restrict CUSTOM=royalty", _db).Notifications);

            _db.GetObject(_ada)!.AddFlag("CUSTOM");
            Assert.Contains(
                "Objects of other types already have this flag set. Search for them and remove it first.",
                _dispatcher.Dispatch(_god, "@flag/type CUSTOM=ROOM", _db).Notifications);
            Assert.Contains("Type of CUSTOM flag set.", _dispatcher.Dispatch(_god, "@flag/type CUSTOM=PLAYER", _db).Notifications);
            Assert.False(FlagTable.CanSetFlag(_db.GetObject(_god)!, _db.GetObject(_widget)!, FlagTable.FindFlag("CUSTOM")!, false, out _));
        }
        finally
        {
            Remove("CUSTOM");
        }
    }

    [Fact]
    public void DisableHidesAFlagUntilItIsDeleted()
    {
        try
        {
            Assert.Contains("     Name: BEACON", _dispatcher.Dispatch(_god, "@flag/add BEACON", _db).Notifications);
            _db.GetObject(_ada)!.AddFlag("BEACON");
            Assert.Contains("Flag BEACON disabled.", _dispatcher.Dispatch(_god, "@flag/disable BEACON", _db).Notifications);
            Assert.Contains("No such flag.", _dispatcher.Dispatch(_ada, "@flag BEACON", _db).Notifications);
            Assert.DoesNotContain(_dispatcher.Dispatch(_ada, "@flag/list BEACON", _db).Notifications, note => note.Contains("BEACON"));
            Assert.Contains(_dispatcher.Dispatch(_god, "@flag/list BEACON", _db).Notifications, note => note.Contains("BEACON (disabled)"));
            Assert.False(FlagTable.CanSetFlag(_db.GetObject(_god)!, _db.GetObject(_ada)!, FlagTable.FindFlag("BEACON")!, false, out string error));
            Assert.Equal("That flag is disabled.", error);

            Assert.Contains("Flag BEACON enabled.", _dispatcher.Dispatch(_god, "@flag/enable BEACON", _db).Notifications);
            Assert.Contains("Flag BEACON deleted.", _dispatcher.Dispatch(_god, "@flag/delete BEACON", _db).Notifications);
            Assert.False(_db.GetObject(_ada)!.HasFlag("BEACON"));
            Assert.Null(FlagTable.FindFlag("BEACON"));
        }
        finally
        {
            Remove("BEACON");
        }
    }

    [Fact]
    public void BadNamesLettersAndAGaggedGodAreRejected()
    {
        _db.GetObject(_god)!.AddFlag("GAGGED");
        Assert.Contains("You cannot do that while gagged.", _dispatcher.Dispatch(_god, "@flag/list", _db).Notifications);
        _db.GetObject(_god)!.RemoveFlag("GAGGED");

        Assert.Contains("You must provide a name for the flag.", _dispatcher.Dispatch(_god, "@flag/add", _db).Notifications);
        Assert.Contains("Flag names must be longer than one character.", _dispatcher.Dispatch(_god, "@flag/add Z", _db).Notifications);
        Assert.Contains("Flag names may not contain spaces.", _dispatcher.Dispatch(_god, "@flag/add BAD NAME", _db).Notifications);
        Assert.Contains("That's not a valid flag name.", _dispatcher.Dispatch(_god, "@flag/add BAD*NAME", _db).Notifications);
        Assert.Contains("Flag characters must be single characters.", _dispatcher.Dispatch(_god, "@flag/add LONG=QQ", _db).Notifications);
        Assert.Contains("Letter conflicts with the flag WIZARD.", _dispatcher.Dispatch(_god, "@flag/add LONG=W", _db).Notifications);
        Assert.Null(FlagTable.FindFlag("LONG"));

        var again = _dispatcher.Dispatch(_god, "@flag/add DARK", _db);
        Assert.Contains("     Name: DARK", again.Notifications);
        Assert.Contains("No such flag.", _dispatcher.Dispatch(_god, "@flag MISSING", _db).Notifications);
        Assert.Contains("Permission denied.", _dispatcher.Dispatch(_ada, "@flag/debug", _db).Notifications);
        Assert.Contains("Flagspace name: FLAG", _dispatcher.Dispatch(_god, "@flag/debug", _db).Notifications);
        Assert.Contains(_dispatcher.Dispatch(_god, "@flag/decompile", _db).Notifications, note => note.Contains("@flag/add DARK"));
        Assert.Contains("You don't look like God.", _dispatcher.Dispatch(_ada, "@flag/delete DARK", _db).Notifications);
    }

    private void Remove(string name)
    {
        var flag = FlagTable.FindFlag(name);
        if (flag == null)
            return;
        foreach (var obj in _db.GetAllObjects())
            obj.RemoveFlag(flag.Name);
        FlagTable.RemoveFlag(flag);
    }
}
