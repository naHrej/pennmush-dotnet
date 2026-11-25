namespace PennMUSH.Core.Help;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PennMUSH.Core.Configuration;

public sealed class TextFileCache : ITextFileCache
{
    private readonly object _sync = new();
    private readonly Dictionary<string, SourceSpec> _diskSources = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CachedCategory> _inMemoryCategories = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, CachedCategory> _activeCategories = new(StringComparer.OrdinalIgnoreCase);

    private sealed record DirectorySpec(string DirectoryPath, string SearchPattern);

    private sealed record SourceSpec(
        string Category,
        string? FilePath,
        IReadOnlyList<DirectorySpec> Directories,
        bool AdminOnly,
        bool Required);

    private sealed record CachedCategory(
        string Name,
        bool AdminOnly,
        Dictionary<string, string> Entries);

    public IReadOnlyCollection<string> Categories
    {
        get
        {
            lock (_sync)
            {
                return _activeCategories.Values
                    .Select(c => c.Name)
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }
    }

    public void RegisterSource(
        string category,
        string? filePath = null,
        string? directoryPath = null,
        string? searchPattern = null,
        bool adminOnly = false,
        bool required = true)
    {
        if (string.IsNullOrWhiteSpace(category))
            return;

        string name = category.Trim();
        var dirs = string.IsNullOrWhiteSpace(directoryPath)
            ? Array.Empty<DirectorySpec>()
            : new[] { new DirectorySpec(Path.GetFullPath(directoryPath), string.IsNullOrWhiteSpace(searchPattern) ? "*.*" : searchPattern) };

        lock (_sync)
        {
            _diskSources[name] = new SourceSpec(
                name,
                string.IsNullOrWhiteSpace(filePath) ? null : Path.GetFullPath(filePath),
                dirs,
                adminOnly,
                required);
            ReloadLocked();
        }
    }

    public void RegisterInMemory(string category, IDictionary<string, string> entries, bool adminOnly = false)
    {
        if (string.IsNullOrWhiteSpace(category))
            return;

        string name = category.Trim();
        var copy = new Dictionary<string, string>(entries, StringComparer.OrdinalIgnoreCase);
        lock (_sync)
        {
            _inMemoryCategories[name] = new CachedCategory(name, adminOnly, copy);
            ReloadLocked();
        }
    }

    public void ConfigureFromGameDirectory(string gameDirectory, MushConfiguration? config = null)
    {
        if (string.IsNullOrWhiteSpace(gameDirectory))
            return;

        string fullGameDir = Path.GetFullPath(gameDirectory);

        lock (_sync)
        {
            _diskSources.Clear();

            if (config != null)
            {
                foreach (var directive in config.HelpFiles)
                {
                    if (string.IsNullOrWhiteSpace(directive.CommandName) || string.IsNullOrWhiteSpace(directive.FilePath))
                        continue;

                    string name = directive.CommandName.Trim();
                    string fullFile = ResolveRelativePath(fullGameDir, directive.FilePath);
                    string stem = Path.GetFileNameWithoutExtension(directive.FilePath).Trim().ToLowerInvariant();
                    var dirs = ResolveStemDirectories(fullGameDir, stem);

                    _diskSources[name] = new SourceSpec(name, fullFile, dirs, directive.AdminOnly, Required: true);
                }
            }

            AddDefaultHelpCategory(fullGameDir, "help", "txt/help.txt", "help", adminOnly: false);
            AddDefaultHelpCategory(fullGameDir, "news", "txt/news.txt", "news", adminOnly: false);
            AddDefaultHelpCategory(fullGameDir, "ahelp", "txt/help.txt", "help", adminOnly: true);
            AddDefaultHelpCategory(fullGameDir, "anews", "txt/news.txt", "news", adminOnly: true);

            AddOptionalIndexedCategory(fullGameDir, "events", "txt/events.txt", "events");
            AddOptionalIndexedCategory(fullGameDir, "rules", "txt/rules.txt", "rules");

            AddOptionalFlatCategory(fullGameDir, "motd", config?.MotdFile ?? "txt/motd.txt", adminOnly: false);
            AddOptionalFlatCategory(fullGameDir, "wizmotd", config?.WizmotdFile ?? "txt/wizmotd.txt", adminOnly: true);
            AddOptionalFlatCategory(fullGameDir, "connect", config?.ConnectFile ?? "txt/connect.txt", adminOnly: false);
            AddOptionalFlatCategory(fullGameDir, "quit", config?.QuitFile ?? "txt/quit.txt", adminOnly: false);
            AddOptionalFlatCategory(fullGameDir, "newuser", config?.NewuserFile ?? "txt/newuser.txt", adminOnly: false);
            AddOptionalFlatCategory(fullGameDir, "down", config?.DownFile ?? "txt/down.txt", adminOnly: false);
            AddOptionalFlatCategory(fullGameDir, "full", config?.FullFile ?? "txt/full.txt", adminOnly: false);
            AddOptionalFlatCategory(fullGameDir, "guest", config?.GuestFile ?? "txt/guest.txt", adminOnly: false);
            AddOptionalFlatCategory(fullGameDir, "register", config?.RegisterFile ?? "txt/register.txt", adminOnly: false);

            ReloadLocked();
        }
    }

    public bool HasCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return false;

        lock (_sync)
        {
            return _activeCategories.ContainsKey(category.Trim());
        }
    }

    public bool IsAdminOnly(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return false;

        lock (_sync)
        {
            return _activeCategories.TryGetValue(category.Trim(), out var cat) && cat.AdminOnly;
        }
    }

    public IReadOnlyList<string> GetEntries(string category, bool isAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Array.Empty<string>();

        lock (_sync)
        {
            if (!_activeCategories.TryGetValue(category.Trim(), out var cat))
                return Array.Empty<string>();
            if (cat.AdminOnly && !isAdmin)
                return Array.Empty<string>();

            return cat.Entries.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }

    public string? GetEntry(string category, string topic, bool isAdmin = false, bool allowPrefix = false)
    {
        if (string.IsNullOrWhiteSpace(category))
            return null;

        lock (_sync)
        {
            if (!_activeCategories.TryGetValue(category.Trim(), out var cat))
                return null;
            if (cat.AdminOnly && !isAdmin)
                return null;

            string key = string.IsNullOrWhiteSpace(topic) ? cat.Name : topic.Trim();
            if (cat.Entries.TryGetValue(key, out var exact))
                return exact;

            if (!allowPrefix || key.Length == 0)
                return null;

            var prefixMatches = cat.Entries.Keys
                .Where(k => k.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return prefixMatches.Count == 1 ? cat.Entries[prefixMatches[0]] : null;
        }
    }

    public IReadOnlyList<string> SearchEntries(string category, string pattern, bool isAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(category) || string.IsNullOrEmpty(pattern))
            return Array.Empty<string>();

        Dictionary<string, string> snapshot;
        lock (_sync)
        {
            if (!_activeCategories.TryGetValue(category.Trim(), out var cat))
                return Array.Empty<string>();
            if (cat.AdminOnly && !isAdmin)
                return Array.Empty<string>();

            snapshot = new Dictionary<string, string>(cat.Entries, StringComparer.OrdinalIgnoreCase);
        }

        if (snapshot.Count == 0)
            return Array.Empty<string>();

        Func<string, bool> matcher = BuildSearchPredicate(pattern);
        var hits = new List<string>();
        foreach (var pair in snapshot)
        {
            if (matcher(pair.Value))
                hits.Add(pair.Key);
        }

        hits.Sort(StringComparer.OrdinalIgnoreCase);
        return hits;
    }

    public void Reload()
    {
        lock (_sync)
        {
            ReloadLocked();
        }
    }

    private void ReloadLocked()
    {
        var updated = new Dictionary<string, CachedCategory>(StringComparer.OrdinalIgnoreCase);

        var publicBackingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in _diskSources.Values)
        {
            if (source.AdminOnly)
                continue;
            if (!string.IsNullOrEmpty(source.FilePath))
                publicBackingPaths.Add(source.FilePath);
            foreach (var dir in source.Directories)
                publicBackingPaths.Add(dir.DirectoryPath);
        }

        foreach (var source in _diskSources.Values)
        {
            var category = LoadSource(source, publicBackingPaths);
            if (category != null)
                updated[source.Category] = category;
        }

        foreach (var pair in _inMemoryCategories)
        {
            updated[pair.Key] = new CachedCategory(
                pair.Value.Name,
                pair.Value.AdminOnly,
                new Dictionary<string, string>(pair.Value.Entries, StringComparer.OrdinalIgnoreCase));
        }

        _activeCategories = updated;
    }

    private static CachedCategory? LoadSource(SourceSpec source, HashSet<string> publicBackingPaths)
    {
        var files = CollectFiles(source);
        if (files.Count == 0)
            return source.Required
                ? new CachedCategory(source.Category, source.AdminOnly, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))
                : null;

        var publicTopics = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var adminTopics = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        bool anyIndexedFile = false;
        string? flatText = null;

        foreach (string file in files)
        {
            if (file.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                var (mdTopics, mdAdminTopics, mdEntry) = HelpStore.ParseMarkdownFile(file);
                if (mdEntry != null)
                {
                    anyIndexedFile = true;
                    foreach (string t in mdTopics)
                    {
                        if (!publicTopics.ContainsKey(t))
                            publicTopics[t] = mdEntry.PlainText;
                    }
                    foreach (string t in mdAdminTopics)
                    {
                        if (!adminTopics.ContainsKey(t))
                            adminTopics[t] = mdEntry.PlainText;
                    }
                }
                continue;
            }

            string[] lines = File.ReadAllLines(file);
            if (lines.Any(l => l.StartsWith('&')))
            {
                anyIndexedFile = true;
                ParseIndexedLines(lines, publicTopics, adminTopics);
            }
            else if (flatText == null)
            {
                flatText = string.Join("\r\n", lines).TrimEnd();
            }
        }

        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!source.AdminOnly)
        {
            foreach (var kv in publicTopics)
                entries[kv.Key] = kv.Value;

            if (!anyIndexedFile && flatText != null)
                entries[source.Category] = flatText;
        }
        else
        {
            bool sharesWithPublic = IsSharedWithPublic(source, publicBackingPaths);
            if (adminTopics.Count > 0)
            {
                foreach (var kv in adminTopics)
                    entries[kv.Key] = kv.Value;
            }
            else if (!sharesWithPublic)
            {
                foreach (var kv in publicTopics)
                    entries[kv.Key] = kv.Value;

                if (!anyIndexedFile && flatText != null)
                    entries[source.Category] = flatText;
            }
        }

        return new CachedCategory(source.Category, source.AdminOnly, entries);
    }

    private static bool IsSharedWithPublic(SourceSpec source, HashSet<string> publicBackingPaths)
    {
        if (!string.IsNullOrEmpty(source.FilePath) && publicBackingPaths.Contains(source.FilePath))
            return true;

        foreach (var dir in source.Directories)
        {
            if (publicBackingPaths.Contains(dir.DirectoryPath))
                return true;
        }

        return false;
    }

    private static List<string> CollectFiles(SourceSpec source)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(source.FilePath) && File.Exists(source.FilePath))
        {
            if (seen.Add(source.FilePath))
                result.Add(source.FilePath);
        }

        foreach (var dir in source.Directories)
        {
            if (!Directory.Exists(dir.DirectoryPath))
                continue;

            var matched = Directory.GetFiles(dir.DirectoryPath, dir.SearchPattern, SearchOption.AllDirectories);
            if (matched.Length == 0 && !string.Equals(dir.SearchPattern, "*.*", StringComparison.Ordinal))
                matched = Directory.GetFiles(dir.DirectoryPath, "*.txt", SearchOption.AllDirectories);

            Array.Sort(matched, StringComparer.OrdinalIgnoreCase);
            foreach (string file in matched)
            {
                if (seen.Add(file))
                    result.Add(file);
            }
        }

        return result;
    }

    private static void ParseIndexedLines(
        string[] lines,
        Dictionary<string, string> publicTopics,
        Dictionary<string, string> adminTopics)
    {
        var headers = new List<(string Name, bool AdminOnly)>();
        var body = new List<string>();
        bool inHeader = false;
        bool started = false;

        void Flush()
        {
            if (headers.Count > 0 && body.Count > 0)
            {
                string text = string.Join("\r\n", body).TrimEnd();
                foreach (var (name, adminOnly) in headers)
                {
                    AddTopic(adminOnly ? adminTopics : publicTopics, name, text);
                }
            }

            headers.Clear();
            body.Clear();
        }

        foreach (string rawLine in lines)
        {
            if (rawLine.StartsWith('&'))
            {
                started = true;
                if (!inHeader && headers.Count > 0)
                    Flush();

                inHeader = true;
                string afterAmp = rawLine.Substring(1).TrimStart();
                bool isAdminTopic = afterAmp.StartsWith('&');
                string topicSource = isAdminTopic ? afterAmp.Substring(1) : afterAmp;
                string name = TopicName(topicSource);
                if (name.Length > 0)
                    headers.Add((name, isAdminTopic));
            }
            else if (started)
            {
                inHeader = false;
                body.Add(rawLine);
            }
        }

        Flush();
    }

    private static void AddTopic(Dictionary<string, string> topics, string name, string text)
    {
        if (!topics.TryGetValue(name, out var existing))
        {
            topics[name] = text;
            return;
        }

        if (string.Equals(existing, text, StringComparison.Ordinal))
            return;

        string alternate = name + " " + name;
        if (!topics.ContainsKey(alternate))
        {
            topics[alternate] = text;
            return;
        }

        topics[name] = existing.TrimEnd() + "\r\n\r\n" + text;
    }

    private static string TopicName(string header)
    {
        var parts = header.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts);
    }

    private void AddDefaultHelpCategory(string gameDir, string name, string defaultFile, string stem, bool adminOnly)
    {
        if (_diskSources.ContainsKey(name))
            return;

        _diskSources[name] = new SourceSpec(
            name,
            ResolveRelativePath(gameDir, defaultFile),
            ResolveStemDirectories(gameDir, stem),
            adminOnly,
            Required: true);
    }

    private void AddOptionalIndexedCategory(string gameDir, string name, string defaultFile, string stem)
    {
        if (_diskSources.ContainsKey(name))
            return;

        _diskSources[name] = new SourceSpec(
            name,
            ResolveRelativePath(gameDir, defaultFile),
            ResolveStemDirectories(gameDir, stem),
            AdminOnly: false,
            Required: false);
    }

    private void AddOptionalFlatCategory(string gameDir, string name, string relativePath, bool adminOnly)
    {
        if (_diskSources.ContainsKey(name) || string.IsNullOrWhiteSpace(relativePath))
            return;

        _diskSources[name] = new SourceSpec(
            name,
            ResolveRelativePath(gameDir, relativePath),
            Array.Empty<DirectorySpec>(),
            adminOnly,
            Required: false);
    }

    private static string ResolveRelativePath(string gameDir, string configuredPath)
    {
        string normalized = configuredPath.Trim()
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.IsPathRooted(normalized) ? normalized : Path.Combine(gameDir, normalized));
    }

    private static IReadOnlyList<DirectorySpec> ResolveStemDirectories(string gameDir, string stem)
    {
        string txtRoot = Path.Combine(gameDir, "txt");
        return stem switch
        {
            "help" => new[]
            {
                new DirectorySpec(Path.Combine(txtRoot, "hlp"), "*.hlp"),
                new DirectorySpec(Path.Combine(txtRoot, "hlp"), "*.md"),
                new DirectorySpec(Path.Combine(txtRoot, "help"), "*.hlp"),
                new DirectorySpec(Path.Combine(txtRoot, "help"), "*.md")
            },
            "news" => new[]
            {
                new DirectorySpec(Path.Combine(txtRoot, "nws"), "*.nws"),
                new DirectorySpec(Path.Combine(txtRoot, "news"), "*.nws")
            },
            "events" => new[]
            {
                new DirectorySpec(Path.Combine(txtRoot, "evt"), "*.evt"),
                new DirectorySpec(Path.Combine(txtRoot, "events"), "*.events")
            },
            "rules" => new[]
            {
                new DirectorySpec(Path.Combine(txtRoot, "rul"), "*.rul"),
                new DirectorySpec(Path.Combine(txtRoot, "rules"), "*.rules")
            },
            _ => new[]
            {
                new DirectorySpec(Path.Combine(txtRoot, stem), $"*.{stem}")
            }
        };
    }

    private static Func<string, bool> BuildSearchPredicate(string pattern)
    {
        if (!pattern.Contains('*') && !pattern.Contains('?'))
            return body => body.Contains(pattern, StringComparison.OrdinalIgnoreCase);

        string core = pattern.Trim('*');
        if (core.Length == 0)
            return _ => true;

        var sb = new StringBuilder();
        bool prevStar = false;
        foreach (char c in core)
        {
            if (c == '*')
            {
                if (!prevStar)
                {
                    sb.Append(".*?");
                    prevStar = true;
                }
            }
            else if (c == '?')
            {
                sb.Append('.');
                prevStar = false;
            }
            else
            {
                sb.Append(Regex.Escape(c.ToString()));
                prevStar = false;
            }
        }

        var regex = new Regex(
            sb.ToString(),
            RegexOptions.IgnoreCase | RegexOptions.Singleline,
            TimeSpan.FromSeconds(1));

        return body =>
        {
            try
            {
                return regex.IsMatch(body);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        };
    }
}
