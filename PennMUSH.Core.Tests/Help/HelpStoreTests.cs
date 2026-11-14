namespace PennMUSH.Core.Tests.Help;

using System.IO;
using PennMUSH.Core.Help;
using Xunit;

public class HelpStoreTests
{
    [Fact]
    public void IndexStaysOnHelpWhileLaterHelpEntriesKeepTheirNames()
    {
        var store = Load("""
& help
This is the index.
& help search
& help/search
& helpfile
How to search.
& helpfile2
& help query
How to query.
""");

        Assert.Contains("This is the index.", store.Lookup("help"));
        Assert.DoesNotContain("How to query.", store.Lookup("help"));
        Assert.Contains("How to search.", store.Lookup("help search"));
        Assert.Contains("How to search.", store.Lookup("help/search"));
        Assert.Contains("How to search.", store.Lookup("helpfile"));
        Assert.Contains("How to query.", store.Lookup("help query"));
        Assert.Contains("How to query.", store.Lookup("helpfile2"));
    }

    [Fact]
    public void DuplicateTopicIsFiledUnderTheRepeatedName()
    {
        var store = Load("""
& help
This is the index.
& help
This is the later page.
""");

        Assert.Equal("This is the index.", store.Lookup("help"));
        Assert.Equal("This is the later page.", store.Lookup("help help"));
    }

    [Fact]
    public void DuplicateTopicIsAppendedWhenTheRepeatedNameIsTaken()
    {
        var store = Load("""
& help
This is the index.
& help help
This name is already used.
& help
This is the later page.
""");

        string index = store.Lookup("help") ?? "";
        Assert.Contains("This is the index.", index);
        Assert.Contains("This is the later page.", index);
        Assert.Equal("This name is already used.", store.Lookup("help help"));
    }

    private static HelpStore Load(string text)
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, text.Trim());
            var store = new HelpStore();
            store.LoadFromFile(path);
            return store;
        }
        finally
        {
            File.Delete(path);
        }
    }
}
