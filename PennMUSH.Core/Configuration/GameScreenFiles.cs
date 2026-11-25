namespace PennMUSH.Core.Configuration;

using System;
using System.IO;

public static class GameScreenFiles
{
    public static string? Resolve(string gameDirectory, string configuredTextPath, bool webSocket, string? configuredHtmlPath = null)
    {
        string relative = configuredTextPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        if (webSocket)
        {
            if (!string.IsNullOrWhiteSpace(configuredHtmlPath))
            {
                string explicitHtml = Path.GetFullPath(Path.Combine(
                    gameDirectory,
                    configuredHtmlPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)));
                if (File.Exists(explicitHtml))
                    return explicitHtml;
            }

            string htmlFull = Path.GetFullPath(Path.Combine(
                gameDirectory,
                "html",
                Path.GetFileNameWithoutExtension(relative) + ".html"));
            if (File.Exists(htmlFull))
                return htmlFull;
        }

        string textFull = Path.GetFullPath(Path.Combine(gameDirectory, relative));
        return File.Exists(textFull) ? textFull : null;
    }

    public static bool IsHtml(string path) =>
        path.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
}
