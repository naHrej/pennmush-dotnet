namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Attributes;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;
using Xunit;

public class AttributeSetCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher = new();
    private readonly Dbref _god;
    private readonly Dbref _mortal;
    private readonly Dbref _room;

    public AttributeSetCommandTests()
    {
        var room = _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        var mortal = _db.CreateObject("Mortal", ObjectType.Player, new Dbref(1));
        mortal.Owner = mortal.Id;
        room.Owner = mortal.Id;
        mortal.Location = room.Id;
        god.Location = room.Id;
        _room = room.Id;
        _god = god.Id;
        _mortal = mortal.Id;
        _dispatcher.Register(new SetCommandHandler(), "@set");
        _dispatcher.Register(new NameCommandHandler(), "@name");
    }

    [Fact]
    public void AmpersandAndAtForms_WriteTheAttributeSoftcodeNames()
    {
        var mortal = Player(_mortal);
        var literal = Run(_mortal, "&DESC me=literal");
        var described = Run(_mortal, "@desc me=the hall");
        var underscored = Run(_mortal, "@_va me=ready");

        Assert.Equal($"{mortal.Name}(#{mortal.Id.Value})/DESC - Set.", Single(literal));
        Assert.Equal("literal", mortal.GetAttribute("DESC")!.Value);
        Assert.Equal("the hall", mortal.GetAttribute("DESCRIBE")!.Value);
        Assert.Equal(
            AttributeFlags.Visual | AttributeFlags.Public | AttributeFlags.Nearby | AttributeFlags.NoProg | AttributeFlags.PrefixMatch,
            mortal.GetAttribute("DESCRIBE")!.Flags);
        Assert.Equal("ready", mortal.GetAttribute("VA")!.Value);
        Assert.Equal($"{mortal.Name}(#{mortal.Id.Value})/DESCRIBE - Set.", Single(described));
        Assert.Equal($"{mortal.Name}(#{mortal.Id.Value})/VA - Set.", Single(underscored));
    }

    [Fact]
    public void StandardUserAttributes_AcceptTheAtForm()
    {
        var mortal = Player(_mortal);
        for (char series = 'V'; series <= 'X'; series++)
        {
            for (char letter = 'A'; letter <= 'Z'; letter++)
            {
                string attr = $"{series}{letter}";
                var ctx = Run(_mortal, $"@{attr.ToLowerInvariant()} me={attr}");
                Assert.Equal($"{mortal.Name}(#{mortal.Id.Value})/{attr} - Set.", Single(ctx));
                Assert.Equal(attr, mortal.GetAttribute(attr)!.Value);
            }
        }
    }

    [Fact]
    public void DescribePrefix_ResolvesWhenItIsUnique()
    {
        var mortal = Player(_mortal);
        var ctx = Run(_mortal, "@descr me=short");
        Assert.Equal($"{mortal.Name}(#{mortal.Id.Value})/DESCRIBE - Set.", Single(ctx));
        Assert.Equal("short", mortal.GetAttribute("DESCRIBE")!.Value);
    }

    [Fact]
    public void AmbiguousAttributePrefix_StaysHuh()
    {
        var ctx = Run(_mortal, "@de me=nope");
        Assert.Equal("Huh?  (Type \"help\" for help.)", Single(ctx));
        Assert.Null(Player(_mortal).GetAttribute("DESCRIBE"));
    }

    [Fact]
    public void RealCommand_WinsOverAnAttributeOfTheSameSpelling()
    {
        var mortal = Player(_mortal);
        var ctx = Run(_mortal, "@name me=Renamed");
        Assert.Equal("Name set.", Single(ctx));
        Assert.Equal("Renamed", mortal.Name);
        Assert.Null(mortal.GetAttribute("NAME"));
    }

    [Fact]
    public void BacktickTree_CreatesBranchesAndRefusesToRemoveThemEarly()
    {
        var mortal = Player(_mortal);
        var set = Run(_mortal, "&KEEP`MID`LEAF me=3");
        Assert.Equal($"{mortal.Name}(#{mortal.Id.Value})/KEEP`MID`LEAF - Set.", Single(set));
        Assert.Equal(" ", mortal.GetAttribute("KEEP")!.Value);
        Assert.Equal(AttributeFlags.Root, mortal.GetAttribute("KEEP")!.Flags);
        Assert.Equal(" ", mortal.GetAttribute("KEEP`MID")!.Value);
        Assert.Equal(AttributeFlags.Root, mortal.GetAttribute("KEEP`MID")!.Flags);
        Assert.Equal("3", mortal.GetAttribute("KEEP`MID`LEAF")!.Value);
        Assert.Equal(AttributeFlags.None, mortal.GetAttribute("KEEP`MID`LEAF")!.Flags);

        var branch = Run(_mortal, "&KEEP me=trunk");
        Assert.Equal("trunk", mortal.GetAttribute("KEEP")!.Value);
        Assert.True(mortal.GetAttribute("KEEP")!.Flags.HasFlag(AttributeFlags.Root));
        Assert.Contains("protected tree attribute", Single(Run(_mortal, "&KEEP me")));
        Assert.Contains("protected tree attribute", Single(Run(_mortal, "&KEEP`MID me")));

        Assert.Contains("Cleared.", Single(Run(_mortal, "&KEEP`MID`LEAF me")));
        Assert.Null(mortal.GetAttribute("KEEP`MID`LEAF"));
        Assert.False(mortal.GetAttribute("KEEP`MID")!.Flags.HasFlag(AttributeFlags.Root));
        Assert.True(mortal.GetAttribute("KEEP")!.Flags.HasFlag(AttributeFlags.Root));

        Assert.Contains("Cleared.", Single(Run(_mortal, "&KEEP`MID me")));
        Assert.False(mortal.GetAttribute("KEEP")!.Flags.HasFlag(AttributeFlags.Root));
        Assert.Contains("Cleared.", Single(Run(_mortal, "&KEEP me")));
        Assert.Null(mortal.GetAttribute("KEEP"));
    }

    [Fact]
    public void DottedName_IsOneAttribute()
    {
        var mortal = Player(_mortal);
        Run(_mortal, "&CHARGES.MAX me=4");
        Assert.Equal("4", mortal.GetAttribute("CHARGES.MAX")!.Value);
        Assert.Null(mortal.GetAttribute("CHARGES"));
        Assert.False(AttributeWriter.HasChildren(mortal, "CHARGES"));
    }

    [Fact]
    public void MissingBranch_MustBeSetBeforeAnExistingLeafChanges()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _mortal);
        lamp.SetAttribute("ORPHAN`LEAF", "old", _mortal);
        var again = Run(_mortal, $"&ORPHAN`LEAF #{lamp.Id.Value}=new");
        var cleared = Run(_mortal, $"&ORPHAN`LEAF #{lamp.Id.Value}");
        Assert.Equal("You must set ORPHAN first.", Single(again));
        Assert.Equal("ORPHAN is a branch attribute; remove its children first.", Single(cleared));
        Assert.Equal("old", lamp.GetAttribute("ORPHAN`LEAF")!.Value);
    }

    [Fact]
    public void RestrictionsOnABranch_BlockTheLeaf()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _mortal);
        lamp.SetAttribute("SECURE", "branch", _god, AttributeFlags.Wizard);
        var denied = Run(_mortal, $"&SECURE`LEAF #{lamp.Id.Value}=no");
        Assert.Equal("That attribute cannot be changed by you.", Single(denied));
        Assert.Null(lamp.GetAttribute("SECURE`LEAF"));

        var god = Player(_god);
        var allowed = Run(_god, $"&SECURE`LEAF #{lamp.Id.Value}=yes");
        Assert.Equal($"{lamp.Name}(#{lamp.Id.Value})/SECURE`LEAF - Set.", Single(allowed));
        Assert.Equal("yes", lamp.GetAttribute("SECURE`LEAF")!.Value);
        Assert.True(god.IsGod);
    }

    [Fact]
    public void BuiltInRestrictions_BlockMortals()
    {
        var mortal = Player(_mortal);
        Assert.Equal("That attribute cannot be changed by you.", Single(Run(_mortal, "&COMMENT me=no")));
        Assert.Null(mortal.GetAttribute("COMMENT"));
        Assert.Equal("That attribute cannot be changed by you.", Single(Run(_mortal, "&SEMAPHORE me=1")));
        Assert.Null(mortal.GetAttribute("SEMAPHORE"));

        var set = Run(_god, $"&SEMAPHORE #{mortal.Id.Value}=1");
        Assert.Contains("SEMAPHORE - Set.", Single(set));
        Assert.True(mortal.GetAttribute("SEMAPHORE")!.Flags.HasFlag(AttributeFlags.NoDump));
    }

    [Fact]
    public void SafeLockedAndQuiet_UseTheOriginalResults()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _mortal);
        lamp.SetAttribute("KEEP", "old", _mortal, AttributeFlags.Safe);
        lamp.SetAttribute("SECRET", "hidden", _god, AttributeFlags.Locked);
        lamp.SetAttribute("QUIETONE", "old", _mortal, AttributeFlags.Quiet);

        Assert.Equal("Attribute KEEP is SAFE. Set it !SAFE to modify it.",
            Single(Run(_mortal, $"&KEEP #{lamp.Id.Value}=new")));
        Assert.Equal("old", lamp.GetAttribute("KEEP")!.Value);
        Assert.Equal("That attribute cannot be changed by you.",
            Single(Run(_mortal, $"&SECRET #{lamp.Id.Value}=new")));
        Assert.Equal("hidden", lamp.GetAttribute("SECRET")!.Value);

        var quiet = Run(_mortal, $"&QUIETONE #{lamp.Id.Value}=new");
        Assert.Empty(quiet.Notifications);
        Assert.Equal("new", lamp.GetAttribute("QUIETONE")!.Value);
    }

    [Fact]
    public void ClearAndMatchFailures_UseTheOriginalText()
    {
        var mortal = Player(_mortal);
        Run(_mortal, "&VA me=1");
        Assert.Equal($"{mortal.Name}(#{mortal.Id.Value})/VA - Cleared.", Single(Run(_mortal, "&VA me")));
        Assert.Equal("No such attribute to reset.", Single(Run(_mortal, "&VA me=")));
        Assert.Equal("That's not a very good name for an attribute.", Single(Run(_mortal, "&`NOPE me=1")));
        Assert.Equal("I don't see that here.", Single(Run(_mortal, "&VA #99=1")));

        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _mortal);
        var other = _db.CreateObject("Other", ObjectType.Player, new Dbref(1));
        other.Owner = other.Id;
        Assert.Equal("Permission denied.", Single(Run(other.Id, $"&VA #{lamp.Id.Value}=1")));
    }

    [Fact]
    public void AtSet_StillSetsABacktickTree()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _mortal);
        var ctx = Run(_mortal, $"@set #{lamp.Id.Value}=FOO`BAR:leaf");
        Assert.Equal("Set.", Single(ctx));
        Assert.Equal(" ", lamp.GetAttribute("FOO")!.Value);
        Assert.True(lamp.GetAttribute("FOO")!.Flags.HasFlag(AttributeFlags.Root));
        Assert.Equal("leaf", lamp.GetAttribute("FOO`BAR")!.Value);
    }

    [Fact]
    public void AtDesc_WritesTheRoomDescription()
    {
        var room = _db.GetObject(_room)!;
        var ctx = Run(_mortal, "@describe here=A stone hall");
        Assert.Contains("DESCRIBE - Set.", Single(ctx));
        Assert.Equal("A stone hall", room.GetAttribute("DESCRIBE")!.Value);
    }

    private CommandContext Run(Dbref who, string command) => _dispatcher.Dispatch(who, command, _db);

    private static string Single(CommandContext ctx) => Assert.Single(ctx.Notifications);

    private PennMUSH.Core.Entities.MUSHObject Player(Dbref id) => _db.GetObject(id)!;
}
