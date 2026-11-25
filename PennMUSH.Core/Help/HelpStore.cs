namespace PennMUSH.Core.Help;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Markdig;

public sealed class HelpEntry
{
    private readonly string? _rawMarkdown;
    private readonly MarkdownPipeline? _pipeline;
    private string? _plainText;
    private string? _html;

    public HelpEntry(string plainText, string? html = null)
    {
        _plainText = plainText;
        _html = html;
    }

    internal HelpEntry(string rawMarkdown, MarkdownPipeline pipeline, bool isMarkdown)
    {
        _rawMarkdown = rawMarkdown;
        _pipeline = pipeline;
    }

    public string PlainText => _plainText ??= _rawMarkdown != null && _pipeline != null
        ? Markdown.ToPlainText(_rawMarkdown, _pipeline).TrimEnd()
        : string.Empty;

    public string? Html => _html ??= _rawMarkdown != null && _pipeline != null
        ? "<div class=\"help-topic\">" + Markdown.ToHtml(_rawMarkdown, _pipeline).TrimEnd() + "</div>"
        : null;
}

public class HelpStore
{
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    private readonly Dictionary<string, HelpEntry> _topics = new(StringComparer.OrdinalIgnoreCase);
    private string? _directory;

    public int TopicCount => _topics.Count;
    public IReadOnlyCollection<string> Topics => _topics.Keys;

    public void Reload()
    {
        _topics.Clear();
        if (!string.IsNullOrEmpty(_directory))
            LoadFromDirectory(_directory);
    }

    public void LoadFromDirectory(string directoryPath)
    {
        _directory = directoryPath;
        if (!Directory.Exists(directoryPath))
            return;

        var hlpFiles = Directory.GetFiles(directoryPath, "*.hlp", SearchOption.AllDirectories);
        Array.Sort(hlpFiles, StringComparer.OrdinalIgnoreCase);
        foreach (var file in hlpFiles)
        {
            LoadFromFile(file);
        }

        var mdFiles = Directory.GetFiles(directoryPath, "*.md", SearchOption.AllDirectories);
        Array.Sort(mdFiles, StringComparer.OrdinalIgnoreCase);
        foreach (var file in mdFiles)
        {
            LoadFromMarkdownFile(file);
        }
    }

    public void LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        var lines = File.ReadAllLines(filePath);
        var names = new List<string>();
        var body = new List<string>();
        bool inHeader = false;
        bool started = false;

        void Flush()
        {
            if (names.Count > 0 && body.Count > 0)
            {
                string text = string.Join("\r\n", body).TrimEnd();
                var entry = new HelpEntry(text);
                foreach (var name in names)
                    AddTopic(name, entry);
            }

            names.Clear();
            body.Clear();
        }

        foreach (var rawLine in lines)
        {
            if (rawLine.StartsWith('&'))
            {
                started = true;
                if (!inHeader && (names.Count > 0 || body.Count > 0))
                    Flush();

                inHeader = true;
                string afterAmp = rawLine.Substring(1).TrimStart();
                if (!afterAmp.StartsWith('&'))
                {
                    string name = TopicName(afterAmp);
                    if (name.Length > 0)
                        names.Add(name);
                }
            }
            else if (started)
            {
                inHeader = false;
                body.Add(rawLine);
            }
        }

        Flush();
    }

    public void LoadFromMarkdownFile(string filePath)
    {
        var (topicNames, _, entry) = ParseMarkdownFile(filePath);
        if (entry == null)
            return;

        foreach (var name in topicNames)
            AddTopic(name, entry);
    }

    public static (List<string> Topics, List<string> AdminTopics, HelpEntry? Entry) ParseMarkdownFile(string filePath)
    {
        if (!File.Exists(filePath))
            return (new List<string>(), new List<string>(), null);

        string raw = File.ReadAllText(filePath);
        var (topicNames, adminNames, body) = ParseMarkdownFrontmatter(raw, Path.GetFileNameWithoutExtension(filePath));
        if (string.IsNullOrWhiteSpace(body))
            return (topicNames, adminNames, null);

        return (topicNames, adminNames, new HelpEntry(body, MarkdownPipeline, isMarkdown: true));
    }

    public string? Lookup(string topic) => LookupEntry(topic)?.PlainText;

    public HelpEntry? LookupEntry(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            topic = "help";
        }

        string trimmed = topic.Trim();

        if (_topics.TryGetValue(trimmed, out var entry))
        {
            return entry;
        }

        var prefixMatches = _topics.Keys.Where(k => k.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase)).ToList();
        if (prefixMatches.Count == 1)
        {
            return _topics[prefixMatches[0]];
        }
        else if (prefixMatches.Count > 1)
        {
            return new HelpEntry($"Here are the entries matching '{trimmed}':\r\n" + string.Join(", ", prefixMatches.Take(20)));
        }

        return null;
    }

    private void AddTopic(string name, HelpEntry entry)
    {
        if (!_topics.TryGetValue(name, out var existing))
        {
            _topics[name] = entry;
            return;
        }

        if (string.Equals(existing.PlainText, entry.PlainText, StringComparison.Ordinal)
            && string.Equals(existing.Html, entry.Html, StringComparison.Ordinal))
            return;

        string alternate = name + " " + name;
        if (!_topics.ContainsKey(alternate))
        {
            _topics[alternate] = entry;
            return;
        }

        string combinedPlain = existing.PlainText.TrimEnd() + "\r\n\r\n" + entry.PlainText;
        string? combinedHtml = existing.Html != null && entry.Html != null
            ? existing.Html + "\n" + entry.Html
            : null;
        _topics[name] = new HelpEntry(combinedPlain, combinedHtml);
    }

    private static (List<string> Topics, List<string> AdminTopics, string Body) ParseMarkdownFrontmatter(string raw, string fallbackName)
    {
        var topics = new List<string>();
        var adminTopics = new List<string>();
        string body = raw;

        if (raw.StartsWith("---", StringComparison.Ordinal))
        {
            var lines = raw.Replace("\r\n", "\n").Split('\n');
            int endIdx = -1;
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Trim() == "---")
                {
                    endIdx = i;
                    break;
                }
            }

            if (endIdx > 0)
            {
                for (int i = 1; i < endIdx; i++)
                {
                    string line = lines[i].Trim();
                    int colon = line.IndexOf(':');
                    if (colon <= 0)
                        continue;

                    string key = line[..colon].Trim().ToLowerInvariant();
                    string val = line[(colon + 1)..].Trim().Trim('[', ']');
                    if (key is "topics" or "aliases" or "topic" or "title")
                    {
                        foreach (string item in val.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            string cleaned = TopicName(item.Trim().Trim('"', '\''));
                            if (cleaned.Length > 0 && !topics.Contains(cleaned, StringComparer.OrdinalIgnoreCase))
                                topics.Add(cleaned);
                        }
                    }
                    else if (key is "admin_topics" or "admin_topic")
                    {
                        foreach (string item in val.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            string cleaned = TopicName(item.Trim().Trim('"', '\''));
                            if (cleaned.Length > 0 && !adminTopics.Contains(cleaned, StringComparer.OrdinalIgnoreCase))
                                adminTopics.Add(cleaned);
                        }
                    }
                }

                body = string.Join('\n', lines[(endIdx + 1)..]);
            }
        }

        if (topics.Count == 0 && adminTopics.Count == 0 && !string.IsNullOrWhiteSpace(fallbackName))
        {
            string rawName = TopicName(fallbackName);
            if (rawName.Length > 0)
                topics.Add(rawName);
            string spaced = TopicName(fallbackName.Replace('_', ' '));
            if (spaced.Length > 0 && !topics.Contains(spaced, StringComparer.OrdinalIgnoreCase))
                topics.Add(spaced);
        }

        return (topics, adminTopics, body);
    }

    private static string TopicName(string header)
    {
        var parts = header.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts);
    }
}
