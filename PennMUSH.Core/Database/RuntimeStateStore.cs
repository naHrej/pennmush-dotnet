namespace PennMUSH.Core.Database;

using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using PennMUSH.Core.Access;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Communication.Mail;
using PennMUSH.Core.Locks;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

/// <summary>
/// Persists core game &amp; player runtime state (Channels, Mail, MailAliases, SoftFunctions, SiteLocks) to SQLite.
/// </summary>
public sealed class RuntimeStateStore
{
    private readonly string _connectionString;

    public RuntimeStateStore(string connectionString)
    {
        _connectionString = connectionString;
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS channels (
    name TEXT PRIMARY KEY,
    owner INTEGER NOT NULL,
    flags INTEGER NOT NULL,
    description TEXT NOT NULL,
    mogrifier INTEGER NOT NULL,
    buffer_capacity INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS channel_members (
    channel TEXT NOT NULL,
    player INTEGER NOT NULL,
    title TEXT NOT NULL,
    is_muted INTEGER NOT NULL,
    is_hidden INTEGER NOT NULL,
    PRIMARY KEY (channel, player)
);
CREATE TABLE IF NOT EXISTS channel_locks (
    channel TEXT NOT NULL,
    lock_kind TEXT NOT NULL,
    lock_text TEXT NOT NULL,
    PRIMARY KEY (channel, lock_kind)
);
CREATE TABLE IF NOT EXISTS mail_messages (
    id INTEGER PRIMARY KEY,
    sender INTEGER NOT NULL,
    recipient INTEGER NOT NULL,
    folder INTEGER NOT NULL,
    subject TEXT NOT NULL,
    body TEXT NOT NULL,
    sent_at INTEGER NOT NULL,
    flags INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS mail_active_folders (
    player INTEGER PRIMARY KEY,
    folder_num INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS mail_aliases (
    name TEXT PRIMARY KEY,
    owner INTEGER NOT NULL,
    description TEXT NOT NULL,
    name_flags INTEGER NOT NULL,
    member_flags INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS mail_alias_members (
    alias_name TEXT NOT NULL,
    member INTEGER NOT NULL,
    PRIMARY KEY (alias_name, member)
);
CREATE TABLE IF NOT EXISTS soft_functions (
    name TEXT PRIMARY KEY,
    thing INTEGER NOT NULL,
    attribute TEXT NOT NULL,
    min_args INTEGER NOT NULL,
    max_args INTEGER NOT NULL,
    disabled INTEGER NOT NULL,
    restrictions TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS sitelocks (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    pattern TEXT NOT NULL,
    can_flags INTEGER NOT NULL,
    cant_flags INTEGER NOT NULL,
    marker INTEGER NOT NULL
);";
        cmd.ExecuteNonQuery();
    }

    public void Save(
        ChannelRegistry? channels = null,
        MailStore? mail = null,
        MailAliasTable? mailAliases = null,
        FunctionRegistry? functions = null,
        SiteLockTable? siteLocks = null)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var tx = conn.BeginTransaction();

        if (channels != null)
        {
            Exec(conn, tx, "DELETE FROM channel_locks; DELETE FROM channel_members; DELETE FROM channels;");
            foreach (var ch in channels.GetAllChannels())
            {
                using var c = conn.CreateCommand();
                c.Transaction = tx;
                c.CommandText = "INSERT INTO channels (name, owner, flags, description, mogrifier, buffer_capacity) VALUES ($n, $o, $f, $d, $m, $b);";
                c.Parameters.AddWithValue("$n", ch.Name);
                c.Parameters.AddWithValue("$o", ch.Owner.Value);
                c.Parameters.AddWithValue("$f", (int)ch.Flags);
                c.Parameters.AddWithValue("$d", ch.Description);
                c.Parameters.AddWithValue("$m", ch.Mogrifier.Value);
                c.Parameters.AddWithValue("$b", ch.BufferCapacity);
                c.ExecuteNonQuery();

                foreach (var mem in ch.Members)
                {
                    using var mc = conn.CreateCommand();
                    mc.Transaction = tx;
                    mc.CommandText = "INSERT INTO channel_members (channel, player, title, is_muted, is_hidden) VALUES ($c, $p, $t, $mu, $h);";
                    mc.Parameters.AddWithValue("$c", ch.Name);
                    mc.Parameters.AddWithValue("$p", mem.PlayerId.Value);
                    mc.Parameters.AddWithValue("$t", mem.Title);
                    mc.Parameters.AddWithValue("$mu", mem.IsMuted ? 1 : 0);
                    mc.Parameters.AddWithValue("$h", mem.IsHidden ? 1 : 0);
                    mc.ExecuteNonQuery();
                }

                foreach (var lk in ch.GetAllLockTexts())
                {
                    using var lc = conn.CreateCommand();
                    lc.Transaction = tx;
                    lc.CommandText = "INSERT INTO channel_locks (channel, lock_kind, lock_text) VALUES ($c, $k, $t);";
                    lc.Parameters.AddWithValue("$c", ch.Name);
                    lc.Parameters.AddWithValue("$k", lk.Key);
                    lc.Parameters.AddWithValue("$t", lk.Value);
                    lc.ExecuteNonQuery();
                }
            }
        }

        if (mail != null)
        {
            Exec(conn, tx, "DELETE FROM mail_messages; DELETE FROM mail_active_folders;");
            foreach (var m in mail.ExportAllMessages())
            {
                using var c = conn.CreateCommand();
                c.Transaction = tx;
                c.CommandText = "INSERT INTO mail_messages (id, sender, recipient, folder, subject, body, sent_at, flags) VALUES ($id, $s, $r, $fo, $su, $bo, $ts, $fl);";
                c.Parameters.AddWithValue("$id", m.Id);
                c.Parameters.AddWithValue("$s", m.Sender.Value);
                c.Parameters.AddWithValue("$r", m.Recipient.Value);
                c.Parameters.AddWithValue("$fo", m.Folder);
                c.Parameters.AddWithValue("$su", m.Subject);
                c.Parameters.AddWithValue("$bo", m.Body);
                c.Parameters.AddWithValue("$ts", m.Timestamp.ToUnixTimeSeconds());
                c.Parameters.AddWithValue("$fl", (int)m.Flags);
                c.ExecuteNonQuery();
            }

            foreach (var kv in mail.ExportActiveFolders())
            {
                using var c = conn.CreateCommand();
                c.Transaction = tx;
                c.CommandText = "INSERT INTO mail_active_folders (player, folder_num) VALUES ($p, $f);";
                c.Parameters.AddWithValue("$p", kv.Key.Value);
                c.Parameters.AddWithValue("$f", kv.Value);
                c.ExecuteNonQuery();
            }
        }

        if (mailAliases != null)
        {
            Exec(conn, tx, "DELETE FROM mail_alias_members; DELETE FROM mail_aliases;");
            foreach (var a in mailAliases.Aliases)
            {
                using var c = conn.CreateCommand();
                c.Transaction = tx;
                c.CommandText = "INSERT INTO mail_aliases (name, owner, description, name_flags, member_flags) VALUES ($n, $o, $d, $nf, $mf);";
                c.Parameters.AddWithValue("$n", a.Name);
                c.Parameters.AddWithValue("$o", a.Owner.Value);
                c.Parameters.AddWithValue("$d", a.Description);
                c.Parameters.AddWithValue("$nf", a.NameFlags);
                c.Parameters.AddWithValue("$mf", a.MemberFlags);
                c.ExecuteNonQuery();

                foreach (var mem in a.Members)
                {
                    using var mc = conn.CreateCommand();
                    mc.Transaction = tx;
                    mc.CommandText = "INSERT INTO mail_alias_members (alias_name, member) VALUES ($a, $m);";
                    mc.Parameters.AddWithValue("$a", a.Name);
                    mc.Parameters.AddWithValue("$m", mem.Value);
                    mc.ExecuteNonQuery();
                }
            }
        }

        if (functions != null)
        {
            Exec(conn, tx, "DELETE FROM soft_functions;");
            foreach (var sf in functions.ListSoft())
            {
                using var c = conn.CreateCommand();
                c.Transaction = tx;
                c.CommandText = "INSERT INTO soft_functions (name, thing, attribute, min_args, max_args, disabled, restrictions) VALUES ($n, $t, $a, $mi, $ma, $d, $r);";
                c.Parameters.AddWithValue("$n", sf.Name);
                c.Parameters.AddWithValue("$t", sf.Thing.Value);
                c.Parameters.AddWithValue("$a", sf.Attribute);
                c.Parameters.AddWithValue("$mi", sf.MinArgs);
                c.Parameters.AddWithValue("$ma", sf.MaxArgs);
                c.Parameters.AddWithValue("$d", sf.Gate.Disabled ? 1 : 0);
                c.Parameters.AddWithValue("$r", string.Join(" ", sf.Gate.Restrictions));
                c.ExecuteNonQuery();
            }
        }

        if (siteLocks != null)
        {
            Exec(conn, tx, "DELETE FROM sitelocks;");
            foreach (var rule in siteLocks.Rules)
            {
                using var c = conn.CreateCommand();
                c.Transaction = tx;
                c.CommandText = "INSERT INTO sitelocks (pattern, can_flags, cant_flags, marker) VALUES ($p, $ca, $cn, $mk);";
                c.Parameters.AddWithValue("$p", rule.Host);
                c.Parameters.AddWithValue("$ca", (long)rule.Can);
                c.Parameters.AddWithValue("$cn", (long)rule.Cant);
                c.Parameters.AddWithValue("$mk", rule.Marker ? 1 : 0);
                c.ExecuteNonQuery();
            }
        }

        tx.Commit();
    }

    public void Load(
        ChannelRegistry? channels = null,
        MailStore? mail = null,
        MailAliasTable? mailAliases = null,
        FunctionRegistry? functions = null,
        SiteLockTable? siteLocks = null)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        if (channels != null)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name, owner, flags, description, mogrifier, buffer_capacity FROM channels;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                string name = r.GetString(0);
                var ch = channels.GetChannel(name);
                if (ch == null)
                {
                    channels.CreateChannel(name, new Dbref(r.GetInt32(1)), (ChannelFlags)r.GetInt32(2), r.GetString(3));
                    ch = channels.GetChannel(name);
                }
                if (ch != null)
                {
                    ch.Mogrifier = new Dbref(r.GetInt32(4));
                    ch.BufferCapacity = r.GetInt32(5);
                }
            }

            using var mCmd = conn.CreateCommand();
            mCmd.CommandText = "SELECT channel, player, title, is_muted, is_hidden FROM channel_members;";
            using var mr = mCmd.ExecuteReader();
            while (mr.Read())
            {
                var ch = channels.GetChannel(mr.GetString(0));
                if (ch == null) continue;
                var pid = new Dbref(mr.GetInt32(1));
                ch.AddMember(pid, mr.GetString(2));
                var mem = ch.GetMember(pid);
                if (mem != null)
                {
                    mem.IsMuted = mr.GetInt32(3) != 0;
                    mem.IsHidden = mr.GetInt32(4) != 0;
                }
            }

            using var lCmd = conn.CreateCommand();
            lCmd.CommandText = "SELECT channel, lock_kind, lock_text FROM channel_locks;";
            using var lr = lCmd.ExecuteReader();
            while (lr.Read())
            {
                var ch = channels.GetChannel(lr.GetString(0));
                if (ch == null) continue;
                string lockText = lr.GetString(2);
                var key = LockParser.Parse(lockText);
                if (key != null)
                    ch.SetLock(lr.GetString(1), lockText, key);
            }
        }

        if (mail != null)
        {
            var msgs = new List<MailMessage>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT id, sender, recipient, folder, subject, body, sent_at, flags FROM mail_messages ORDER BY id ASC;";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    msgs.Add(new MailMessage
                    {
                        Id = r.GetInt32(0),
                        Sender = new Dbref(r.GetInt32(1)),
                        Recipient = new Dbref(r.GetInt32(2)),
                        Folder = r.GetInt32(3),
                        Subject = r.GetString(4),
                        Body = r.GetString(5),
                        Timestamp = DateTimeOffset.FromUnixTimeSeconds(r.GetInt64(6)),
                        Flags = (MailFlags)r.GetInt32(7)
                    });
                }
            }

            var folders = new Dictionary<Dbref, int>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT player, folder_num FROM mail_active_folders;";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    folders[new Dbref(r.GetInt32(0))] = r.GetInt32(1);
            }

            if (msgs.Count > 0 || folders.Count > 0)
                mail.ClearAndLoad(msgs, folders);
        }

        if (mailAliases != null)
        {
            var byName = new Dictionary<string, MailAlias>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT name, owner, description, name_flags, member_flags FROM mail_aliases;";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var alias = new MailAlias
                    {
                        Name = r.GetString(0),
                        Owner = new Dbref(r.GetInt32(1)),
                        Description = r.GetString(2),
                        NameFlags = r.GetInt32(3),
                        MemberFlags = r.GetInt32(4)
                    };
                    byName[alias.Name] = alias;
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT alias_name, member FROM mail_alias_members;";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    if (byName.TryGetValue(r.GetString(0), out var alias))
                        alias.Members.Add(new Dbref(r.GetInt32(1)));
                }
            }

            if (byName.Count > 0)
            {
                mailAliases.Clear();
                foreach (var a in byName.Values)
                    mailAliases.ImportAlias(a);
            }
        }

        if (functions != null)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name, thing, attribute, min_args, max_args, disabled, restrictions FROM soft_functions;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var sf = new SoftFunction
                {
                    Name = r.GetString(0),
                    Thing = new Dbref(r.GetInt32(1)),
                    Attribute = r.GetString(2),
                    MinArgs = r.GetInt32(3),
                    MaxArgs = r.GetInt32(4)
                };
                sf.Gate.Disabled = r.GetInt32(5) != 0;
                string restr = r.GetString(6);
                if (!string.IsNullOrWhiteSpace(restr))
                    FunctionRestrictions.Apply(sf.Gate, restr);
                functions.SaveSoft(sf);
            }
        }

        if (siteLocks != null)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT pattern, can_flags, cant_flags, marker FROM sitelocks ORDER BY id ASC;";
            using var r = cmd.ExecuteReader();
            var loaded = new List<SiteRule>();
            while (r.Read())
            {
                loaded.Add(new SiteRule
                {
                    Host = r.GetString(0),
                    Can = (uint)r.GetInt64(1),
                    Cant = (uint)r.GetInt64(2),
                    Marker = r.GetInt32(3) != 0
                });
            }
            if (loaded.Count > 0)
            {
                siteLocks.ClearRules();
                foreach (var rule in loaded)
                {
                    if (!rule.Marker)
                        siteLocks.Add(rule.Host, rule.Can, rule.Cant);
                }
            }
        }
    }

    private static void Exec(SqliteConnection conn, SqliteTransaction tx, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
