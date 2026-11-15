namespace PennMUSH.Core.Tests.Configuration;

using System.IO;
using PennMUSH.Core.Configuration;

public class ScreenTokenTests
{
    [Fact]
    public void ScreenFiles_SubstituteConfigDirectives()
    {
        string path = WriteConfig("mud_name Penn & MUSH\nmud_url http://example.test\nport 4201\nchat_token hello there\n");

        var config = MushConfigParser.ParseFile(path);
        string template = "Welcome to {{mud_name}} ({{mud_url}} {{port}}) {{chat_token}} {{missing}}";

        Assert.Equal(
            "Welcome to Penn & MUSH (http://example.test 4201) hello there {{missing}}",
            config.ExpandScreenText(template, html: false));
        Assert.Equal(
            "Welcome to Penn &amp; MUSH (http://example.test 4201) hello there {{missing}}",
            config.ExpandScreenText(template, html: true));
    }

    [Fact]
    public void ScreenTokens_IgnoreCaseAndInnerSpace()
    {
        string path = WriteConfig("mud_name Harbor\n");
        var config = MushConfigParser.ParseFile(path);

        Assert.Equal("Harbor", config.ExpandScreenText("{{ MUD_NAME }}", html: false));
    }

    [Fact]
    public void EmptyDirective_ReplacesTheToken()
    {
        string path = WriteConfig("mud_url\n");
        var config = MushConfigParser.ParseFile(path);

        Assert.Equal("()", config.ExpandScreenText("({{mud_url}})", html: false));
    }

    private static string WriteConfig(string body)
    {
        string path = Path.Combine(Path.GetTempPath(), "penn-cnf-" + Path.GetRandomFileName() + ".cnf");
        File.WriteAllText(path, body);
        return path;
    }
}
