namespace PennMUSH.Core.Configuration;

using System.Collections.Generic;
using System.IO;

public static class ScreenFileReport
{
    public static IReadOnlyList<string> Lines(string gameDirectory, MushConfiguration config)
    {
        int index = Measure(gameDirectory, "txt/index.html");
        return new[]
        {
            Format("File", gameDirectory, config, index, false),
            Format("HTMLFile", gameDirectory, config, index, true)
        };
    }

    public static string Format(string label, string gameDirectory, MushConfiguration config, int index, bool html)
    {
        int Size(string? relative) => Measure(gameDirectory, html ? HtmlOf(relative) : relative);
        return $"{label} sizes:  Index...{index}  NewUser...{Size(config.NewuserFile)}  Connect...{Size(config.ConnectFile)}  Guest...{Size(config.GuestFile)}  Motd...{Size(config.MotdFile)}  Wizmotd...{Size(config.WizmotdFile)}  Quit...{Size(config.QuitFile)}  Register...{Size("txt/register.txt")}  Down...{Size(config.DownFile)}  Full...{Size(config.FullFile)}  Who...{Size("txt/who.txt")}";
    }

    public static int Measure(string gameDirectory, string? relative)
    {
        if (string.IsNullOrWhiteSpace(relative))
            return -1;
        string full = Path.GetFullPath(Path.Combine(gameDirectory, relative.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)));
        return File.Exists(full) ? (int)new FileInfo(full).Length : -1;
    }

    private static string? HtmlOf(string? relative)
    {
        if (string.IsNullOrWhiteSpace(relative))
            return null;
        string name = Path.GetFileNameWithoutExtension(relative.Replace('\\', '/'));
        return Path.Combine("html", name + ".html");
    }
}
