namespace PennMUSH.Core.Help;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class HelpStore
{
    private readonly Dictionary<string, string> _topics = new(StringComparer.OrdinalIgnoreCase);
    private string? _directory;

    public int TopicCount => _topics.Count;

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

        var files = Directory.GetFiles(directoryPath, "*.hlp", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            LoadFromFile(file);
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
                foreach (var name in names)
                    AddTopic(name, text);
            }

            names.Clear();
            body.Clear();
        }

        foreach (var rawLine in lines)
        {
            if (rawLine.StartsWith('&'))
            {
                started = true;
                if (!inHeader && names.Count > 0)
                    Flush();

                inHeader = true;
                string name = TopicName(rawLine.Substring(1));
                if (name.Length > 0)
                    names.Add(name);
            }
            else if (started)
            {
                inHeader = false;
                body.Add(rawLine);
            }
        }

        Flush();
    }

    public string? Lookup(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            topic = "help";
        }

        string trimmed = topic.Trim();

        if (_topics.TryGetValue(trimmed, out var text))
        {
            return text;
        }

        var prefixMatches = _topics.Keys.Where(k => k.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase)).ToList();
        if (prefixMatches.Count == 1)
        {
            return _topics[prefixMatches[0]];
        }
        else if (prefixMatches.Count > 1)
        {
            return $"Here are the entries matching '{trimmed}':\r\n" + string.Join(", ", prefixMatches.Take(20));
        }

        return null;
    }

    private void AddTopic(string name, string text)
    {
        if (!_topics.TryGetValue(name, out var existing))
        {
            _topics[name] = text;
            return;
        }

        if (string.Equals(existing, text, StringComparison.Ordinal))
            return;

        string alternate = name + " " + name;
        if (!_topics.ContainsKey(alternate))
        {
            _topics[alternate] = text;
            return;
        }

        _topics[name] = existing.TrimEnd() + "\r\n\r\n" + text;
    }

    private static string TopicName(string header)
    {
        var parts = header.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts);
    }
}
