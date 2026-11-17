namespace PennMUSH.Core.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public enum WizardLogKind
{
    Check,
    Cmd,
    Conn,
    Err,
    Trace,
    Wiz
}

/// <summary>
/// The six wizard logs <c>@log</c> and <c>@logwipe</c> write.
/// Recall reads the lines written in this process. The files keep the timestamped text.
/// </summary>
public sealed class WizardLog
{
    public const string WipePassword = "zap!";

    private readonly string _directory;
    private readonly Func<DateTime> _clock;
    private readonly Dictionary<WizardLogKind, List<string>> _lines = new();
    private readonly object _gate = new();

    public WizardLog(string directory, Func<DateTime>? clock = null)
    {
        _directory = directory;
        _clock = clock ?? (() => DateTime.Now);
        foreach (WizardLogKind kind in Enum.GetValues<WizardLogKind>())
            _lines[kind] = new List<string>();
    }

    public static string NameOf(WizardLogKind kind) => kind switch
    {
        WizardLogKind.Check => "checkpoint",
        WizardLogKind.Cmd => "command",
        WizardLogKind.Conn => "connection",
        WizardLogKind.Err => "error",
        WizardLogKind.Trace => "trace",
        WizardLogKind.Wiz => "wizard",
        _ => "error"
    };

    public string FilePath(WizardLogKind kind)
        => Path.Combine(_directory, FileName(kind));

    public void Append(WizardLogKind kind, string body)
    {
        string stamped = $"[{_clock():yyyy-MM-dd HH:mm:ss}] {body}";
        lock (_gate)
        {
            Directory.CreateDirectory(_directory);
            File.AppendAllText(FilePath(kind), stamped + Environment.NewLine);
            _lines[kind].Add(body);
        }
    }

    public IReadOnlyList<string> Recall(WizardLogKind kind, int lines)
    {
        lock (_gate)
        {
            List<string> all = _lines[kind];
            if (lines <= 0 || lines >= all.Count)
                return all.ToList();
            return all.Skip(all.Count - lines).ToList();
        }
    }

    public void Wipe(WizardLogKind kind)
    {
        lock (_gate)
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(FilePath(kind), "*** LOG WAS WIPED AFTER GROWING TOO LARGE ***" + Environment.NewLine);
        }
    }

    public void Rotate(WizardLogKind kind)
    {
        lock (_gate)
        {
            Directory.CreateDirectory(_directory);
            string path = FilePath(kind);
            if (File.Exists(path))
                File.Copy(path, path + ".1", overwrite: true);
            File.WriteAllText(path, "*** LOG WAS ROTATED AFTER GROWING TOO LARGE ***" + Environment.NewLine);
        }
    }

    public void Trim(WizardLogKind kind)
    {
        lock (_gate)
        {
            string path = FilePath(kind);
            if (!File.Exists(path))
                return;
            string[] lines = File.ReadAllLines(path);
            if (lines.Length == 0)
                return;
            int keep = Math.Max(1, (int)Math.Ceiling(lines.Length * 0.1));
            File.WriteAllLines(path, lines.Skip(lines.Length - keep));
        }
    }

    private static string FileName(WizardLogKind kind) => kind switch
    {
        WizardLogKind.Check => "checkpoint.log",
        WizardLogKind.Cmd => "command.log",
        WizardLogKind.Conn => "connect.log",
        WizardLogKind.Err => "error.log",
        WizardLogKind.Trace => "trace.log",
        WizardLogKind.Wiz => "wizard.log",
        _ => "error.log"
    };
}
