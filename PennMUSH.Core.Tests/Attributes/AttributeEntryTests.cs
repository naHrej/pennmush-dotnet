namespace PennMUSH.Core.Tests.Attributes;

using Xunit;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Types;

public class AttributeEntryTests
{
    [Fact]
    public void AttributeEntry_DefaultConstructor_SetsDefaultValues()
    {
        var entry = new AttributeEntry();
        Assert.Equal(string.Empty, entry.Name);
        Assert.Equal(string.Empty, entry.Value);
        Assert.Equal(Dbref.Nothing, entry.Creator);
        Assert.Equal(AttributeFlags.None, entry.Flags);
    }

    [Fact]
    public void AttributeEntry_ParameterizedConstructor_SetsProperties()
    {
        var creator = new Dbref(1);
        var entry = new AttributeEntry("DESCRIBE", "A grand hall", creator, AttributeFlags.Visual | AttributeFlags.Public);

        Assert.Equal("DESCRIBE", entry.Name);
        Assert.Equal("A grand hall", entry.Value);
        Assert.Equal(creator, entry.Creator);
        Assert.True(entry.Flags.HasFlag(AttributeFlags.Visual));
        Assert.True(entry.Flags.HasFlag(AttributeFlags.Public));
        Assert.False(entry.Flags.HasFlag(AttributeFlags.Wizard));
    }

    [Theory]
    [InlineData("DESCRIBE", true)]
    [InlineData("VA", true)]
    [InlineData("MY_ATTR", true)]
    [InlineData("TEST-ATTR", true)]
    [InlineData("TREE`SUB", true)]
    [InlineData("FOO.BAR", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("`LEADING_TICK", false)]
    [InlineData("TRAILING_TICK`", false)]
    [InlineData("DOUBLE``TICK", false)]
    [InlineData("HAS SPACE", false)]
    [InlineData("HAS!EXCLAMATION", false)]
    public void IsValidName_ValidatesCorrectly(string? name, bool expected)
    {
        Assert.Equal(expected, AttributeEntry.IsValidName(name));
    }
}
