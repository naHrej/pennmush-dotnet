namespace PennMUSH.Core.Database;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Flags;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Types;

public static class FlatfileStore
{
    public const int DBF_NO_CHAT_SYSTEM = 0x01;
    public const int DBF_WARNINGS       = 0x02;
    public const int DBF_CREATION_TIMES = 0x04;
    public const int DBF_NO_POWERS      = 0x08;
    public const int DBF_NEW_LOCKS      = 0x10;
    public const int DBF_NEW_STRINGS    = 0x20;
    public const int DBF_TYPE_GARBAGE   = 0x40;
    public const int DBF_SPLIT_IMMORTAL = 0x80;
    public const int DBF_NO_TEMPLE      = 0x100;
    public const int DBF_LESS_GARBAGE   = 0x200;
    public const int DBF_AF_VISUAL      = 0x400;
    public const int DBF_VALUE_IS_COST  = 0x800;
    public const int DBF_LINK_ANYWHERE  = 0x1000;
    public const int DBF_NO_STARTUP_FLAG = 0x2000;
    public const int DBF_PANIC          = 0x4000;
    public const int DBF_AF_NODUMP      = 0x8000;
    public const int DBF_SPIFFY_LOCKS   = 0x10000;
    public const int DBF_NEW_FLAGS      = 0x20000;
    public const int DBF_NEW_POWERS     = 0x40000;
    public const int DBF_POWERS_LOGGED  = 0x80000;
    public const int DBF_LABELS         = 0x100000;
    public const int DBF_SPIFFY_AF_ANSI = 0x200000;
    public const int DBF_HEAR_CONNECT   = 0x400000;
    public const int DBF_NEW_VERSIONS   = 0x800000;

    public static void Save(ObjectDatabase db, TextWriter writer)
    {
        writer.WriteLine("+V5");
        writer.WriteLine("+flags WIZARD ROYALTY DARK QUIET ANSI INHERIT NO_COMMAND");
        writer.WriteLine($"~{db.Count}");

        for (int i = 0; i < db.Count; i++)
        {
            var obj = db.GetObject(new Dbref(i));
            if (obj == null)
            {
                writer.WriteLine($"#{i}");
                writer.WriteLine("Garbage");
                writer.WriteLine("-1");
                writer.WriteLine("-1");
                writer.WriteLine("-1");
                writer.WriteLine("-1");
                writer.WriteLine("-1");
                writer.WriteLine("-1");
                writer.WriteLine("1");
                writer.WriteLine("0");
                writer.WriteLine("6"); // Garbage
                writer.WriteLine("");
                writer.WriteLine("");
                writer.WriteLine("<");
                continue;
            }

            writer.WriteLine($"#{obj.Id.Value}");
            writer.WriteLine(obj.Name);
            writer.WriteLine(obj.Location.Value.ToString());
            writer.WriteLine(obj.Contents.Value.ToString());
            writer.WriteLine(obj.Exits.Value.ToString());
            writer.WriteLine(obj.Next.Value.ToString());
            writer.WriteLine(obj.Parents.Count > 0 ? obj.Parents[0].Value.ToString() : "-1");
            writer.WriteLine(obj.Zones.Count > 0 ? System.Linq.Enumerable.First(obj.Zones).Value.ToString() : "-1");
            writer.WriteLine(obj.Owner.Value.ToString());
            writer.WriteLine(obj.Pennies.ToString());
            writer.WriteLine(((int)obj.Type).ToString());
            writer.WriteLine(string.Join(" ", obj.Flags));
            writer.WriteLine(string.Join(" ", obj.Powers));

            foreach (var l in obj.Locks)
            {
                writer.WriteLine($"_{l.Type}|{l.Key}");
            }

            foreach (var attr in obj.Attributes)
            {
                writer.WriteLine($"]{attr.Name}^{attr.Creator.Value}^{(int)attr.Flags}");
                writer.WriteLine($"\"{attr.Value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"");
            }

            writer.WriteLine("<");
        }

        writer.WriteLine("***END OF DUMP***");
    }

    public static void Load(ObjectDatabase db, TextReader reader)
    {
        db.Clear();

        string? line = reader.ReadLine();
        if (line == null || !line.StartsWith("+V"))
        {
            throw new FormatException("Invalid flatfile header format.");
        }

        uint indbFlags = 0;
        if (int.TryParse(line.Substring(2).Trim(), out int v))
        {
            uint uv = unchecked((uint)v);
            if (uv > 256)
            {
                indbFlags = ((uv - 2) / 256) - 5;
            }
        }

        // Skip header lines (including dbversion, savedtime, +FLAGS/+POWER/+ATTRIBUTES LIST)
        // until object count (~<count> or integer line).
        int dbVersion = 0;
        int objectCount = 0;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.StartsWith("dbversion ", StringComparison.Ordinal)
                && int.TryParse(line.AsSpan(10).Trim(), out int parsedVer))
            {
                dbVersion = parsedVer;
                continue;
            }
            if (line.StartsWith('~') && int.TryParse(line.Substring(1), out objectCount))
            {
                break;
            }
            if (int.TryParse(line, out objectCount))
            {
                break;
            }
        }

        bool hasLabels = (indbFlags & DBF_LABELS) != 0 || dbVersion >= 1;
        bool isAuthenticPenn = indbFlags != 0 || hasLabels;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line == "***END OF DUMP***") break;
            if (string.IsNullOrEmpty(line)) continue;

            if (line.StartsWith('!') || line.StartsWith('#'))
            {
                if (!int.TryParse(line.Substring(1), out int idVal)) continue;
                var id = new Dbref(idVal);

                // Fill gaps if any
                while (db.Count < idVal)
                {
                    var garbage = new MUSHObject(new Dbref(db.Count), "Garbage", ObjectType.Garbage, new Dbref(1));
                    db.SetObject(garbage);
                }

                if (hasLabels)
                {
                    db.SetObject(LoadLabeledObject(id, reader));
                    continue;
                }

                string name = ReadString(reader);
                int loc = int.Parse(ReadString(reader));
                int con = int.Parse(ReadString(reader));
                int ex = int.Parse(ReadString(reader));
                int next = int.Parse(ReadString(reader));
                int parent = int.Parse(ReadString(reader));

                var locks = new List<(string type, string key)>();

                if (isAuthenticPenn)
                {
                    if ((indbFlags & DBF_SPIFFY_LOCKS) != 0)
                    {
                        string lCountLine = ReadString(reader);
                        int lockCount = 0;
                        if (lCountLine.StartsWith("lockcount "))
                            int.TryParse(lCountLine.Substring(10), out lockCount);
                        else
                            int.TryParse(lCountLine, out lockCount);

                        for (int l = 0; l < lockCount; l++)
                        {
                            string lType = ReadString(reader);
                            string lCreator = ReadString(reader);
                            string lFlags = ReadString(reader);
                            string lKey = ReadString(reader);
                            locks.Add((lType, lKey));
                        }
                    }
                    else if ((indbFlags & DBF_NEW_LOCKS) != 0)
                    {
                        while (true)
                        {
                            while (reader.Peek() == '\r' || reader.Peek() == '\n')
                            {
                                reader.ReadLine();
                            }

                            if (reader.Peek() != '_')
                                break;

                            string lockLine = reader.ReadLine() ?? string.Empty;
                            int pipeIdx = lockLine.IndexOf('|');
                            if (pipeIdx > 1 && lockLine.StartsWith('_'))
                            {
                                string lockType = lockLine.Substring(1, pipeIdx - 1);
                                string lockKeyRest = lockLine.Substring(pipeIdx + 1);

                                int nextChar = reader.Peek();
                                if (nextChar == '/' || nextChar == ':' || nextChar == '^')
                                {
                                    string opLine = reader.ReadLine() ?? string.Empty;
                                    lockKeyRest += opLine;
                                    if (reader.Peek() == '\r' || reader.Peek() == '\n')
                                    {
                                        reader.ReadLine();
                                    }
                                }
                                locks.Add((lockType, lockKeyRest));
                            }
                        }
                    }
                    else
                    {
                        string[] fixedLockNames = { "Basic", "Use", "Enter" };
                        for (int l = 0; l < 3; l++)
                        {
                            string lockKey = reader.ReadLine() ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(lockKey))
                            {
                                locks.Add((fixedLockNames[l], lockKey));
                            }
                        }
                    }

                    int owner = int.Parse(ReadString(reader));
                    int zone = int.Parse(ReadString(reader));
                    int pennies = int.Parse(ReadString(reader));

                    ObjectType objType = ObjectType.None;
                    string flagsStr = string.Empty;
                    string powersStr = string.Empty;
                    int oldFlags = 0;
                    int oldToggles = 0;

                    if ((indbFlags & DBF_NEW_FLAGS) != 0)
                    {
                        int typeNum = int.Parse(ReadString(reader));
                        objType = (ObjectType)typeNum;
                        flagsStr = ReadString(reader);
                    }
                    else
                    {
                        oldFlags = int.Parse(ReadString(reader));
                        oldToggles = int.Parse(ReadString(reader));
                        int oldType = oldFlags & 0x7;
                        objType = oldType switch
                        {
                            0 => ObjectType.Room,
                            1 => ObjectType.Thing,
                            2 => ObjectType.Exit,
                            3 => ObjectType.Player,
                            6 => ObjectType.Garbage,
                            _ => ObjectType.None
                        };
                    }

                    if ((indbFlags & DBF_NO_POWERS) != 0)
                    {
                        // No powers
                    }
                    else if ((indbFlags & DBF_NEW_POWERS) != 0)
                    {
                        powersStr = ReadString(reader);
                    }
                    else
                    {
                        int powersNum = int.Parse(ReadString(reader));
                    }

                    if ((indbFlags & DBF_NO_CHAT_SYSTEM) == 0)
                    {
                        ReadString(reader);
                    }

                    uint warnings = 0;
                    if ((indbFlags & DBF_WARNINGS) != 0)
                    {
                        uint.TryParse(ReadString(reader), out warnings);
                    }

                    long creationTime = 0;
                    long modTime = 0;
                    if ((indbFlags & DBF_CREATION_TIMES) != 0)
                    {
                        long.TryParse(ReadString(reader), out creationTime);
                        long.TryParse(ReadString(reader), out modTime);
                    }

                    var obj = new MUSHObject(id, name, objType, new Dbref(owner))
                    {
                        Location = new Dbref(loc),
                        Contents = new Dbref(con),
                        Exits = new Dbref(ex),
                        Next = new Dbref(next),
                        Pennies = pennies,
                        Warnings = warnings
                    };

                    if (creationTime > 0) obj.CreationTime = DateTimeOffset.FromUnixTimeSeconds(creationTime);
                    if (modTime > 0) obj.ModificationTime = DateTimeOffset.FromUnixTimeSeconds(modTime);

                    if (parent >= 0) obj.AddParent(new Dbref(parent));
                    if (zone >= 0) obj.AddZone(new Dbref(zone));

                    if ((indbFlags & DBF_NEW_FLAGS) != 0)
                    {
                        foreach (var f in flagsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                            obj.AddFlag(f);
                    }
                    else
                    {
                        ApplyOldFlags(obj, oldFlags, oldToggles);
                    }

                    foreach (var p in powersStr.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        obj.AddPower(p);

                    foreach (var (lType, lKey) in locks)
                    {
                        obj.SetLock(lType, LockParser.Parse(lKey), obj.Owner);
                    }

                    // Attributes until '<'
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line == "<") break;

                        if (line.StartsWith(']'))
                        {
                            var parts = line.Substring(1).Split('^');
                            string attrName = parts[0];
                            int attrCreator = parts.Length > 1 && int.TryParse(parts[1], out int c) ? c : owner;
                            int attrFlags = parts.Length > 2 && int.TryParse(parts[2], out int f) ? f : 0;
                            string attrVal = ReadString(reader);
                            obj.SetAttribute(attrName, attrVal, new Dbref(attrCreator), (AttributeFlags)attrFlags);
                        }
                        else if (line.StartsWith("> "))
                        {
                            string attrName = line.Substring(2).Trim();
                            string attrVal = ReadString(reader);
                            obj.SetAttribute(attrName, attrVal, obj.Owner);
                        }
                    }

                    db.SetObject(obj);
                }
                else
                {
                    // Existing simplified format support
                    int zone = int.Parse(reader.ReadLine() ?? "-1");
                    int owner = int.Parse(reader.ReadLine() ?? "1");
                    int pennies = int.Parse(reader.ReadLine() ?? "0");
                    int typeVal = int.Parse(reader.ReadLine() ?? "0");
                    string flagsStr = reader.ReadLine() ?? string.Empty;
                    string powersStr = reader.ReadLine() ?? string.Empty;

                    var obj = new MUSHObject(id, name, (ObjectType)typeVal, new Dbref(owner))
                    {
                        Location = new Dbref(loc),
                        Contents = new Dbref(con),
                        Exits = new Dbref(ex),
                        Next = new Dbref(next),
                        Pennies = pennies
                    };

                    if (parent >= 0) obj.AddParent(new Dbref(parent));
                    if (zone >= 0) obj.AddZone(new Dbref(zone));

                    foreach (var f in flagsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        obj.AddFlag(f);

                    foreach (var p in powersStr.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                        obj.AddPower(p);

                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line == "<") break;

                        if (line.StartsWith('_'))
                        {
                            int pipeIdx = line.IndexOf('|');
                            if (pipeIdx > 1)
                            {
                                string lockType = line.Substring(1, pipeIdx - 1);
                                string lockKey = line.Substring(pipeIdx + 1);
                                obj.SetLock(lockType, LockParser.Parse(lockKey), obj.Owner);
                            }
                        }
                        else if (line.StartsWith("> "))
                        {
                            string attrName = line.Substring(2).Trim();
                            string attrVal = ReadString(reader);
                            obj.SetAttribute(attrName, attrVal, obj.Owner);
                        }
                        else if (line.StartsWith(']'))
                        {
                            var parts = line.Substring(1).Split('^');
                            string attrName = parts[0];
                            int attrCreator = parts.Length > 1 && int.TryParse(parts[1], out int c) ? c : owner;
                            int attrFlags = parts.Length > 2 && int.TryParse(parts[2], out int f) ? f : 0;
                            string attrVal = ReadString(reader);
                            obj.SetAttribute(attrName, attrVal, new Dbref(attrCreator), (AttributeFlags)attrFlags);
                        }
                    }

                    db.SetObject(obj);
                }
            }
        }

        while (db.Count < objectCount)
        {
            var garbage = new MUSHObject(new Dbref(db.Count), "Garbage", ObjectType.Garbage, new Dbref(1));
            db.SetObject(garbage);
        }

        db.RebuildIndexes();
    }

    /// <summary>
    /// Loads a PennMUSH flatfile database from disk, transparently decompressing gzip-compressed files.
    /// </summary>
    public static void LoadFile(ObjectDatabase db, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Flatfile path cannot be empty.", nameof(path));

        using var fileStream = File.OpenRead(path);
        bool isGzip = path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
        if (!isGzip && fileStream.Length >= 2)
        {
            int b0 = fileStream.ReadByte();
            int b1 = fileStream.ReadByte();
            fileStream.Position = 0;
            isGzip = b0 == 0x1F && b1 == 0x8B;
        }

        if (isGzip)
        {
            using var gzip = new GZipStream(fileStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            Load(db, reader);
        }
        else
        {
            using var reader = new StreamReader(fileStream);
            Load(db, reader);
        }
    }

    /// <summary>
    /// Parses a modern PennMUSH <c>DBF_LABELS</c> (<c>dbversion 6</c>) object record.
    /// </summary>
    private static MUSHObject LoadLabeledObject(Dbref id, TextReader reader)
    {
        string name = ReadLabeledValue(reader, "name");
        int loc = ParseDbrefId(ReadLabeledValue(reader, "location"));
        int con = ParseDbrefId(ReadLabeledValue(reader, "contents"));
        int ex = ParseDbrefId(ReadLabeledValue(reader, "exits"));
        int next = ParseDbrefId(ReadLabeledValue(reader, "next"));
        int parent = ParseDbrefId(ReadLabeledValue(reader, "parent"));

        int lockCount = int.Parse(ReadLabeledValue(reader, "lockcount"));
        var locks = new List<(string type, Dbref creator, LockFlags flags, string key)>(lockCount);
        for (int i = 0; i < lockCount; i++)
        {
            string lType = ReadLabeledValue(reader, "type");
            int lCreator = ParseDbrefId(ReadLabeledValue(reader, "creator"));
            LockFlags lFlags = ParseLockFlags(ReadLabeledValue(reader, "flags"));
            var (nextLabel, nextVal) = ReadLabeledField(reader);
            if (nextLabel == "derefs")
            {
                (nextLabel, nextVal) = ReadLabeledField(reader);
            }
            string lKey = nextLabel == "key" ? nextVal : string.Empty;
            locks.Add((lType, new Dbref(lCreator), lFlags, lKey));
        }

        int owner = ParseDbrefId(ReadLabeledValue(reader, "owner"));
        int zone = ParseDbrefId(ReadLabeledValue(reader, "zone"));
        int pennies = int.Parse(ReadLabeledValue(reader, "pennies"));
        var objType = (ObjectType)int.Parse(ReadLabeledValue(reader, "type"));
        string flagsStr = ReadLabeledValue(reader, "flags");
        string powersStr = ReadLabeledValue(reader, "powers");
        uint warnings = ParseWarnings(ReadLabeledValue(reader, "warnings"));
        long.TryParse(ReadLabeledValue(reader, "created"), out long creationTime);
        long.TryParse(ReadLabeledValue(reader, "modified"), out long modTime);

        var obj = new MUSHObject(id, name, objType, new Dbref(owner))
        {
            Location = new Dbref(loc),
            Contents = new Dbref(con),
            Exits = new Dbref(ex),
            Next = new Dbref(next),
            Pennies = pennies,
            Warnings = warnings
        };

        if (creationTime > 0) obj.CreationTime = DateTimeOffset.FromUnixTimeSeconds(creationTime);
        if (modTime > 0) obj.ModificationTime = DateTimeOffset.FromUnixTimeSeconds(modTime);

        if (parent >= 0) obj.AddParent(new Dbref(parent));
        if (zone >= 0) obj.AddZone(new Dbref(zone));

        foreach (var f in flagsStr.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            obj.AddFlag(f);

        foreach (var p in powersStr.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            obj.AddPower(p);

        foreach (var (lType, lCreator, lFlags, lKey) in locks)
        {
            obj.SetLock(lType, LockParser.Parse(lKey), lCreator, lFlags);
        }

        int attrCount = int.Parse(ReadLabeledValue(reader, "attrcount"));
        for (int i = 0; i < attrCount; i++)
        {
            string attrName = ReadLabeledValue(reader, "name");
            int attrCreator = ParseDbrefId(ReadLabeledValue(reader, "owner"));
            AttributeFlags attrFlags = ParseAttributeFlags(ReadLabeledValue(reader, "flags"));
            var (nextLabel, nextVal) = ReadLabeledField(reader);
            if (nextLabel == "derefs")
            {
                (nextLabel, nextVal) = ReadLabeledField(reader);
            }
            string attrVal = nextLabel == "value" ? nextVal : string.Empty;
            obj.SetAttribute(attrName, attrVal, new Dbref(attrCreator), attrFlags);
        }

        return obj;
    }

    private static string ReadLabeledValue(TextReader reader, string expectedLabel)
    {
        var (label, value) = ReadLabeledField(reader);
        if (!string.Equals(label, expectedLabel, StringComparison.Ordinal))
        {
            throw new FormatException($"Expected DBF_LABELS field '{expectedLabel}' but found '{label}'.");
        }
        return value;
    }

    private static (string label, string value) ReadLabeledField(TextReader reader)
    {
        while (true)
        {
            int ch = reader.Peek();
            if (ch == -1) return (string.Empty, string.Empty);
            if (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n')
            {
                reader.Read();
            }
            else
            {
                break;
            }
        }

        var labelBuilder = new StringBuilder();
        while (true)
        {
            int ch = reader.Peek();
            if (ch == -1 || ch == ' ' || ch == '\r' || ch == '\n') break;
            labelBuilder.Append((char)reader.Read());
        }

        if (reader.Peek() == ' ')
        {
            reader.Read();
        }

        string value = ReadString(reader);
        return (labelBuilder.ToString(), value);
    }

    private static int ParseDbrefId(string text)
    {
        ReadOnlySpan<char> span = text.AsSpan().Trim();
        if (span.StartsWith('#')) span = span.Slice(1);
        return int.Parse(span);
    }

    private static uint ParseWarnings(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        if (uint.TryParse(text.Trim(), out uint numeric)) return numeric;
        return WarningBits.Parse(text, null);
    }

    private static LockFlags ParseLockFlags(string text)
    {
        LockFlags result = LockFlags.None;
        foreach (string token in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            result |= token.ToLowerInvariant() switch
            {
                "visual" => LockFlags.Visual,
                "no_inherit" or "private" => LockFlags.Private,
                "wizard" => LockFlags.Wizard,
                "locked" => LockFlags.Locked,
                "no_clone" => LockFlags.NoClone,
                "owner" => LockFlags.Owner,
                _ => LockFlags.None
            };
        }
        return result;
    }

    private static AttributeFlags ParseAttributeFlags(string text)
    {
        AttributeFlags result = AttributeFlags.None;
        foreach (string token in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            result |= token.ToLowerInvariant() switch
            {
                "quiet" => AttributeFlags.Quiet,
                "internal" => AttributeFlags.Internal,
                "wizard" => AttributeFlags.Wizard,
                "locked" => AttributeFlags.Locked,
                "no_command" or "noprog" => AttributeFlags.NoProg,
                "mortal_dark" or "mdark" => AttributeFlags.MDark,
                "no_inherit" or "private" => AttributeFlags.Private,
                "no_clone" or "nocopy" => AttributeFlags.NoCopy,
                "visual" => AttributeFlags.Visual,
                "regexp" => AttributeFlags.Regexp,
                "case" => AttributeFlags.Case,
                "safe" => AttributeFlags.Safe,
                "branch" or "root" => AttributeFlags.Root,
                "rlimit" => AttributeFlags.RLimit,
                "enum" => AttributeFlags.Enum,
                "command" => AttributeFlags.Command,
                "listen" => AttributeFlags.Listen,
                "nodump" or "no_dump" => AttributeFlags.NoDump,
                "prefixmatch" => AttributeFlags.PrefixMatch,
                "veiled" => AttributeFlags.Veiled,
                "debug" => AttributeFlags.Debug,
                "no_debug" => AttributeFlags.NoDebug,
                "nearby" => AttributeFlags.Nearby,
                "public" => AttributeFlags.Public,
                "anon" => AttributeFlags.Anon,
                "noname" => AttributeFlags.NoName,
                "nospace" => AttributeFlags.NoSpace,
                "amhear" => AttributeFlags.MHear,
                "aahear" => AttributeFlags.AHear,
                _ => AttributeFlags.None
            };
        }
        return result;
    }

    private static string ReadString(TextReader reader)
    {
        int first = reader.Peek();
        if (first == -1) return string.Empty;

        if (first == '"')
        {
            reader.Read(); // Consume '"'
            var sb = new StringBuilder();
            while (true)
            {
                int ch = reader.Read();
                if (ch == -1) break;
                if (ch == '\\')
                {
                    int next = reader.Read();
                    if (next != -1) sb.Append((char)next);
                }
                else if (ch == '"')
                {
                    // Chomp trailing newline
                    int c = reader.Peek();
                    if (c == '\r') { reader.Read(); c = reader.Peek(); }
                    if (c == '\n') { reader.Read(); }
                    return sb.ToString();
                }
                else
                {
                    sb.Append((char)ch);
                }
            }
            return sb.ToString();
        }
        else
        {
            return reader.ReadLine() ?? string.Empty;
        }
    }

    private static void ApplyOldFlags(MUSHObject obj, int oldFlags, int oldToggles)
    {
        if ((oldFlags & 0x10) != 0) obj.AddFlag("WIZARD");
        if ((oldFlags & 0x20) != 0) obj.AddFlag("LINK_OK");
        if ((oldFlags & 0x40) != 0) obj.AddFlag("DARK");
        if ((oldFlags & 0x80) != 0) obj.AddFlag("VERBOSE");
        if ((oldFlags & 0x100) != 0) obj.AddFlag("STICKY");
        if ((oldFlags & 0x200) != 0) obj.AddFlag("TRANSPARENT");
        if ((oldFlags & 0x400) != 0) obj.AddFlag("HAVEN");
        if ((oldFlags & 0x800) != 0) obj.AddFlag("QUIET");
        if ((oldFlags & 0x1000) != 0) obj.AddFlag("HALT");
        if ((oldFlags & 0x2000) != 0) obj.AddFlag("UNFIND");
        if ((oldFlags & 0x4000) != 0) obj.AddFlag("GOING");
        if ((oldFlags & 0x8000) != 0) obj.AddFlag("ACCESSED");
        if ((oldFlags & 0x10000) != 0) obj.AddFlag("MARKED");
        if ((oldFlags & 0x20000) != 0) obj.AddFlag("NOWARN");
        if ((oldFlags & 0x40000) != 0) obj.AddFlag("CHOWN_OK");
        if ((oldFlags & 0x80000) != 0) obj.AddFlag("ENTER_OK");
        if ((oldFlags & 0x100000) != 0) obj.AddFlag("VISUAL");
        if ((oldFlags & 0x200000) != 0) obj.AddFlag("LIGHT");
        if ((oldFlags & 0x400000) != 0) obj.AddFlag("ROYALTY");
        if ((oldFlags & 0x800000) != 0) obj.AddFlag("OPAQUE");
        if ((oldFlags & 0x1000000) != 0) obj.AddFlag("INHERIT");
        if ((oldFlags & 0x2000000) != 0) obj.AddFlag("DEBUG");
        if ((oldFlags & 0x4000000) != 0) obj.AddFlag("SAFE");
        if ((oldFlags & 0x8000000) != 0) obj.AddFlag("STARTUP");
        if ((oldFlags & 0x10000000) != 0) obj.AddFlag("AUDIBLE");
        if ((oldFlags & 0x20000000) != 0) obj.AddFlag("NO_COMMAND");
        if ((oldFlags & 0x40000000) != 0) obj.AddFlag("GOING_TWICE");

        switch (obj.Type)
        {
            case ObjectType.Player:
                if ((oldToggles & 0x8) != 0) obj.AddFlag("TERSE");
                if ((oldToggles & 0x10) != 0) obj.AddFlag("MYOPIC");
                if ((oldToggles & 0x20) != 0) obj.AddFlag("NOSPOOF");
                if ((oldToggles & 0x40) != 0) obj.AddFlag("SUSPECT");
                if ((oldToggles & 0x80) != 0) obj.AddFlag("GAGGED");
                if ((oldToggles & 0x100) != 0) obj.AddFlag("MONITOR");
                if ((oldToggles & 0x200) != 0) obj.AddFlag("CONNECT");
                if ((oldToggles & 0x400) != 0) obj.AddFlag("ANSI");
                if ((oldToggles & 0x800) != 0) obj.AddFlag("ZONE");
                if ((oldToggles & 0x1000) != 0) obj.AddFlag("JURY");
                if ((oldToggles & 0x2000) != 0) obj.AddFlag("JUDGE");
                if ((oldToggles & 0x4000) != 0) obj.AddFlag("FIXED");
                if ((oldToggles & 0x8000) != 0) obj.AddFlag("UNREGISTERED");
                if ((oldToggles & 0x10000) != 0) obj.AddFlag("VACATION");
                if ((oldToggles & 0x80000) != 0) obj.AddFlag("COLOR");
                if ((oldToggles & 0x100000) != 0) obj.AddFlag("NOACCENTS");
                if ((oldToggles & 0x200000) != 0) obj.AddFlag("PARANOID");
                break;
            case ObjectType.Thing:
                if ((oldToggles & 0x8) != 0) obj.AddFlag("DEST_OK");
                if ((oldToggles & 0x10) != 0) obj.AddFlag("PUPPET");
                if ((oldToggles & 0x20) != 0) obj.AddFlag("LISTEN");
                if ((oldToggles & 0x40) != 0) obj.AddFlag("NOLEAVE");
                if ((oldToggles & 0x80) != 0) obj.AddFlag("INHEARIT");
                if ((oldToggles & 0x100) != 0) obj.AddFlag("Z_TEL");
                break;
            case ObjectType.Room:
                if ((oldToggles & 0x8) != 0) obj.AddFlag("FLOATING");
                if ((oldToggles & 0x10) != 0) obj.AddFlag("ABODE");
                if ((oldToggles & 0x20) != 0) obj.AddFlag("JUMP_OK");
                if ((oldToggles & 0x40) != 0) obj.AddFlag("NO_TEL");
                if ((oldToggles & 0x80) != 0) obj.AddFlag("TEMPLE");
                if ((oldToggles & 0x100) != 0) obj.AddFlag("LISTEN");
                if ((oldToggles & 0x200) != 0) obj.AddFlag("Z_TEL");
                if ((oldToggles & 0x400) != 0) obj.AddFlag("INHEARIT");
                if ((oldToggles & 0x1000) != 0) obj.AddFlag("UNINSPECT");
                break;
            case ObjectType.Exit:
                if ((oldToggles & 0x8) != 0) obj.AddFlag("CLOUDY");
                break;
        }
    }
}
