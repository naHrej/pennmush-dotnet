namespace PennMUSH.Core.Access;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public sealed class NameLockFile
{
    private readonly string _path;

    public NameLockFile(string path) => _path = path;

    public bool Lock(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(_path))
            return false;
        if (!TryRead(out var lines))
            return false;
        if (!lines.Any(line => Same(line, name)))
            lines.Add(name.Trim());
        return TryWrite(lines);
    }

    public bool Delete(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(_path))
            return false;
        if (!File.Exists(_path) || !TryRead(out var lines))
            return false;
        int removed = lines.RemoveAll(line => Same(line, name));
        return removed > 0 && TryWrite(lines);
    }

    private bool TryRead(out List<string> lines)
    {
        try
        {
            lines = File.Exists(_path)
                ? File.ReadAllLines(_path).ToList()
                : new List<string>();
            return true;
        }
        catch (IOException)
        {
            lines = new List<string>();
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            lines = new List<string>();
            return false;
        }
    }

    private bool TryWrite(List<string> lines)
    {
        try
        {
            string? directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllLines(_path, lines);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool Same(string line, string name) =>
        !line.TrimStart().StartsWith('#') && line.Trim().Equals(name.Trim(), StringComparison.OrdinalIgnoreCase);
}
