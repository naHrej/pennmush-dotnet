namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Attributes;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class AttributeCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _ada;
    private readonly Dbref _god;

    public AttributeCommandTests()
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
        _dispatcher.Register(new AttributeCommandHandler(), "@attribute");
    }

    [Fact]
    public void GodDefinesFlagsALimitAndAnEnum()
    {
        try
        {
            Assert.Contains("Permission denied.", _dispatcher.Dispatch(_ada, "@attribute/access CUSTOM=visual", _db).Notifications);

            var added = _dispatcher.Dispatch(_god, "@attribute/access CUSTOM=visual wizard", _db);
            Assert.Contains("CUSTOM -- Attribute permissions now: wizard visual", added.Notifications);
            var info = _dispatcher.Dispatch(_god, "@attribute CUSTOM", _db);
            Assert.Contains("Attribute: CUSTOM", info.Notifications);
            Assert.Contains("    Flags: wizard visual", info.Notifications);
            Assert.Contains("  Creator: #1", info.Notifications);

            var limit = _dispatcher.Dispatch(_god, "@attribute/limit CUSTOM=^[0-9]+$", _db);
            Assert.Contains("CUSTOM -- Attribute limit set to: ^[0-9]+$", limit.Notifications);
            var rejected = AttributeWriter.Apply(_db.GetObject(_god)!, _db.GetObject(_ada)!, "CUSTOM", "abc");
            Assert.Equal("Attribute value does not match the /limit regexp.", rejected.Message);
            var accepted = AttributeWriter.Apply(_db.GetObject(_god)!, _db.GetObject(_ada)!, "CUSTOM", "12");
            Assert.True(accepted.Ok);
            Assert.Equal("12", _db.GetObject(_ada)!.GetAttribute("CUSTOM")!.Value);

            Assert.Contains("COLOR -- Attribute permissions now: visual", _dispatcher.Dispatch(_god, "@attribute/access COLOR=visual", _db).Notifications);
            Assert.Contains("COLOR -- Attribute enum set to: Red Green", _dispatcher.Dispatch(_god, "@attribute/enum COLOR=Red Green", _db).Notifications);
            Assert.True(AttributeWriter.Apply(_db.GetObject(_god)!, _db.GetObject(_ada)!, "COLOR", "green").Ok);
            Assert.Equal("Green", _db.GetObject(_ada)!.GetAttribute("COLOR")!.Value);
        }
        finally
        {
            StandardAttributes.Delete("CUSTOM");
            StandardAttributes.Delete("COLOR");
        }
    }

    [Fact]
    public void RetroactiveRenameAndDeleteChangeTheDefinition()
    {
        try
        {
            Assert.Contains("MARK -- Attribute permissions now:", _dispatcher.Dispatch(_god, "@attribute/access MARK=none", _db).Notifications.First());
            _db.GetObject(_ada)!.WriteAttributeValue("MARK", "here", _ada, AttributeFlags.None);
            Assert.Contains("MARK -- Attribute permissions now: visual", _dispatcher.Dispatch(_god, "@attribute/access/retroactive MARK=visual", _db).Notifications);
            Assert.True(_db.GetObject(_ada)!.GetAttribute("MARK")!.Flags.HasFlag(AttributeFlags.Visual));

            Assert.Contains("Renamed MARK to TOKEN in attribute table.", _dispatcher.Dispatch(_god, "@attribute/rename MARK=TOKEN", _db).Notifications);
            Assert.False(StandardAttributes.ExistsExact("MARK"));
            Assert.True(StandardAttributes.ExistsExact("TOKEN"));
            var removed = _dispatcher.Dispatch(_god, "@attribute/delete TOKEN", _db);
            Assert.Contains("Removed TOKEN from attribute table.", removed.Notifications);
            Assert.Contains("That attribute isn't in the attribute table", _dispatcher.Dispatch(_god, "@attribute TOKEN", _db).Notifications);

            Assert.Contains("That attribute's permissions can not be changed.", _dispatcher.Dispatch(_god, "@attribute/access XYXXY=visual", _db).Notifications);
            Assert.Contains("Invalid Regular Expression.", _dispatcher.Dispatch(_god, "@attribute/limit DESCRIBE=(", _db).Notifications);
            Assert.Contains("@@ Standard Attributes:", _dispatcher.Dispatch(_ada, "@attribute/decompile DESCRIBE", _db).Notifications);
        }
        finally
        {
            StandardAttributes.Delete("MARK");
            StandardAttributes.Delete("TOKEN");
        }
    }

    [Fact]
    public void ListAttribsShowsTheDefinitionTable()
    {
        try
        {
            _dispatcher.Register(new ListCommandHandler(_dispatcher), "@list");
            Assert.Contains("CUSTOM -- Attribute permissions now: visual", _dispatcher.Dispatch(_god, "@attribute/access CUSTOM=visual", _db).Notifications);
            var listed = _dispatcher.Dispatch(_god, "@list attribs", _db).Notifications.Single();
            Assert.StartsWith("Attribs: ", listed);
            var names = listed["Attribs: ".Length..].Split(' ');
            Assert.Contains("CUSTOM", names);
            Assert.Contains("DESCRIBE", names);
            Assert.DoesNotContain("DESC", names);

            var lower = _dispatcher.Dispatch(_god, "@list/lowercase attribs", _db).Notifications.Single();
            Assert.StartsWith("Attribs: ", lower);
            Assert.Contains("describe", lower["Attribs: ".Length..].Split(' '));

            var switched = _dispatcher.Dispatch(_god, "@list/attribs", _db).Notifications.Single();
            Assert.Contains("CUSTOM", switched["Attribs: ".Length..].Split(' '));
        }
        finally
        {
            StandardAttributes.Delete("CUSTOM");
        }
    }
}
