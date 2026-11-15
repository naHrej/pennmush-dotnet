namespace PennMUSH.Server.Engine;

using System;
using System.IO;
using System.Text.RegularExpressions;
using dotless.Core;

public static class ScreenStylesheet
{
    private static readonly object Gate = new();
    private static string? CachedCss;
    private static string? CachedPath;
    private static DateTime CachedStamp;

    public static string? Load(string gameDirectory)
    {
        string lessPath = Path.Combine(gameDirectory, "html", "stylesheet.less");
        string cssPath = Path.Combine(gameDirectory, "html", "stylesheet.css");
        string sourcePath = File.Exists(lessPath) ? lessPath : File.Exists(cssPath) ? cssPath : "";
        if (sourcePath.Length == 0)
            return null;

        var stamp = File.GetLastWriteTimeUtc(sourcePath);
        lock (Gate)
        {
            if (CachedCss != null && CachedPath == sourcePath && CachedStamp == stamp)
                return CachedCss;

            string? css = sourcePath == lessPath
                ? CompileLess(File.ReadAllText(lessPath))
                : File.ReadAllText(cssPath);
            if (string.IsNullOrWhiteSpace(css) && sourcePath == lessPath && File.Exists(cssPath))
                css = File.ReadAllText(cssPath);
            if (string.IsNullOrWhiteSpace(css))
                return null;

            css = Regex.Replace(css, "</style", "<\\/style", RegexOptions.IgnoreCase);
            CachedCss = css;
            CachedPath = sourcePath;
            CachedStamp = stamp;
            return css;
        }
    }

    private static string? CompileLess(string less)
    {
        try
        {
            return Less.Parse(less);
        }
        catch
        {
            return null;
        }
    }
}
