namespace PennMUSH.Core.Tests.Configuration;

using System;
using System.IO;
using PennMUSH.Core.Configuration;
using PennMUSH.Server.Engine;

public class ScreenFileTests
{
    [Fact]
    public void WebSocketScreen_UsesHtmlCounterpart()
    {
        string game = MakeGame();
        string? html = GameScreenFiles.Resolve(game, "txt/connect.txt", webSocket: true);
        string? text = GameScreenFiles.Resolve(game, "txt/connect.txt", webSocket: false);

        Assert.Equal(Path.GetFullPath(Path.Combine(game, "html", "connect.html")), html);
        Assert.Equal(Path.GetFullPath(Path.Combine(game, "txt", "connect.txt")), text);
    }

    [Fact]
    public void MissingHtml_FallsBackToText()
    {
        string game = MakeGame();
        File.Delete(Path.Combine(game, "html", "connect.html"));

        string? resolved = GameScreenFiles.Resolve(game, "txt/connect.txt", webSocket: true);

        Assert.Equal(Path.GetFullPath(Path.Combine(game, "txt", "connect.txt")), resolved);
    }

    [Fact]
    public void StylesheetLess_CompilesToCss()
    {
        string game = MakeGame();
        File.WriteAllText(Path.Combine(game, "html", "stylesheet.less"), "@accent: #7eb6ff;\n.system-msg { color: @accent; }\n");

        string? css = ScreenStylesheet.Load(game);

        Assert.NotNull(css);
        Assert.Contains("#7eb6ff", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".system-msg", css, StringComparison.Ordinal);
    }

    [Fact]
    public void ShippedStylesheet_Compiles()
    {
        string? game = FindShippedGame();
        Assert.NotNull(game);

        string? css = ScreenStylesheet.Load(game!);

        Assert.NotNull(css);
        Assert.Contains(".game-screen", css, StringComparison.Ordinal);
        Assert.Contains(".system-msg", css, StringComparison.Ordinal);
        foreach (string name in new[] { "connect", "down", "full", "guest", "motd", "newuser", "quit", "register", "wizmotd" })
        {
            Assert.True(File.Exists(Path.Combine(game!, "html", name + ".html")), name);
            Assert.True(File.Exists(Path.Combine(game!, "txt", name + ".txt")), name);
        }
    }

    private static string? FindShippedGame()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "game");
            if (File.Exists(Path.Combine(candidate, "html", "stylesheet.less")))
                return candidate;
            dir = dir.Parent;
        }

        return null;
    }

    private static string MakeGame()
    {
        string game = Path.Combine(Path.GetTempPath(), "penn-screens-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(game, "txt"));
        Directory.CreateDirectory(Path.Combine(game, "html"));
        File.WriteAllText(Path.Combine(game, "txt", "connect.txt"), "text connect");
        File.WriteAllText(Path.Combine(game, "html", "connect.html"), "<div>html connect</div>");
        return game;
    }
}
