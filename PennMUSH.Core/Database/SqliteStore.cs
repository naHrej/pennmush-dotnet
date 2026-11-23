namespace PennMUSH.Core.Database;

using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Communication.Mail;
using PennMUSH.Core.Access;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class SqliteStore : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteCommand _upsertObject;
    private readonly SqliteCommand _insertAttribute;
    private readonly SqliteCommand _insertFlag;
    private readonly SqliteCommand _insertPower;
    private readonly SqliteCommand _insertParent;
    private readonly SqliteCommand _insertZone;
    private readonly SqliteCommand _deleteObject;
    private bool _disposed;

    public SqliteStore(string connectionString = "Data Source=mush.db")
    {
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
        using (var pragma = _connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode=WAL;";
            pragma.ExecuteNonQuery();
        }
        InitializeSchema();
        _upsertObject = Prepare(@"
            INSERT INTO objects (id, name, type, location, contents, exits, next, owner, home, pennies, warnings, created_at, modified_at)
            VALUES ($id, $name, $type, $loc, $con, $ex, $next, $owner, $home, $pennies, $warnings, $created, $modified)
            ON CONFLICT(id) DO UPDATE SET
                name = excluded.name,
                type = excluded.type,
                location = excluded.location,
                contents = excluded.contents,
                exits = excluded.exits,
                next = excluded.next,
                owner = excluded.owner,
                home = excluded.home,
                pennies = excluded.pennies,
                warnings = excluded.warnings,
                created_at = excluded.created_at,
                modified_at = excluded.modified_at;");
        _insertAttribute = Prepare("INSERT INTO attributes (object_id, name, value, setter, flags) VALUES ($id, $name, $val, $setter, $flags);");
        _insertFlag = Prepare("INSERT INTO flags (object_id, flag) VALUES ($id, $flag);");
        _insertPower = Prepare("INSERT INTO powers (object_id, power) VALUES ($id, $power);");
        _insertParent = Prepare("INSERT INTO parents (object_id, parent_id, priority) VALUES ($id, $pid, $prio);");
        _insertZone = Prepare("INSERT INTO zones (object_id, zone_id) VALUES ($id, $zid);");
        _deleteObject = Prepare("DELETE FROM objects WHERE id = $id;");
    }

    private SqliteCommand Prepare(string sql)
    {
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        return command;
    }

    public void InitializeSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS objects (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                type INTEGER NOT NULL,
                location INTEGER NOT NULL,
                contents INTEGER NOT NULL,
                exits INTEGER NOT NULL,
                next INTEGER NOT NULL,
                owner INTEGER NOT NULL,
                home INTEGER NOT NULL,
                pennies INTEGER NOT NULL,
                warnings INTEGER NOT NULL DEFAULT 0,
                created_at INTEGER NOT NULL,
                modified_at INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS attributes (
                object_id INTEGER NOT NULL,
                name TEXT NOT NULL,
                value TEXT NOT NULL,
                setter INTEGER NOT NULL,
                flags INTEGER NOT NULL,
                PRIMARY KEY (object_id, name)
            );

            CREATE TABLE IF NOT EXISTS flags (
                object_id INTEGER NOT NULL,
                flag TEXT NOT NULL,
                PRIMARY KEY (object_id, flag)
            );

            CREATE TABLE IF NOT EXISTS powers (
                object_id INTEGER NOT NULL,
                power TEXT NOT NULL,
                PRIMARY KEY (object_id, power)
            );

            CREATE TABLE IF NOT EXISTS parents (
                object_id INTEGER NOT NULL,
                parent_id INTEGER NOT NULL,
                priority INTEGER NOT NULL,
                PRIMARY KEY (object_id, parent_id)
            );

            CREATE TABLE IF NOT EXISTS zones (
                object_id INTEGER NOT NULL,
                zone_id INTEGER NOT NULL,
                PRIMARY KEY (object_id, zone_id)
            );

            CREATE TABLE IF NOT EXISTS cron_jobs (
                id TEXT PRIMARY KEY,
                expression TEXT NOT NULL,
                creator INTEGER NOT NULL,
                target INTEGER NOT NULL,
                command TEXT NOT NULL,
                last_run INTEGER,
                next_run INTEGER NOT NULL,
                is_paused INTEGER NOT NULL
            );
        ";
        cmd.ExecuteNonQuery();
        using var alter = _connection.CreateCommand();
        alter.CommandText = "ALTER TABLE objects ADD COLUMN warnings INTEGER NOT NULL DEFAULT 0";
        try
        {
            alter.ExecuteNonQuery();
        }
        catch (SqliteException)
        {
            // The column is already present on a database created with this schema.
        }
    }

    public void Save(ObjectDatabase db, Scheduling.CronScheduler? cron = null)
    {
        if (cron != null)
            db.Changes.MarkCron();
        Flush(db, cron, null, null, null, null, null, null);
    }

    public void Flush(
        ObjectDatabase db,
        Scheduling.CronScheduler? cron,
        RuntimeStateStore? runtime,
        ChannelRegistry? channels,
        MailStore? mail,
        MailAliasTable? mailAliases,
        FunctionRegistry? functions,
        SiteLockTable? siteLocks)
    {
        DirtyBatch batch = db.Changes.Take();
        if (!batch.Any)
            return;

        using var tx = _connection.BeginTransaction();
        try
        {
            foreach (int id in batch.Objects)
                WriteObject(tx, db.GetObject(new Dbref(id)));
            foreach (int id in batch.Removed)
                DeleteObject(tx, id);
            if (batch.Cron && cron != null)
                WriteCron(tx, cron);
            if (batch.Runtime && runtime != null)
                runtime.Write(_connection, tx, channels, mail, mailAliases, functions, siteLocks);
            tx.Commit();
        }
        catch
        {
            try { tx.Rollback(); } catch (SqliteException) { }
            db.Changes.Restore(batch);
            throw;
        }
    }

    private void WriteObject(SqliteTransaction tx, MUSHObject? obj)
    {
        if (obj == null)
            return;

        DeleteChildren(tx, obj.Id.Value);

        Bind(_upsertObject, tx,
            ("$id", obj.Id.Value),
            ("$name", obj.Name),
            ("$type", (int)obj.Type),
            ("$loc", obj.Location.Value),
            ("$con", obj.Contents.Value),
            ("$ex", obj.Exits.Value),
            ("$next", obj.Next.Value),
            ("$owner", obj.Owner.Value),
            ("$home", obj.Home.Value),
            ("$pennies", obj.Pennies),
            ("$warnings", (long)obj.Warnings),
            ("$created", obj.CreationTime.ToUnixTimeSeconds()),
            ("$modified", obj.ModificationTime.ToUnixTimeSeconds()));
        _upsertObject.ExecuteNonQuery();

        foreach (var attr in obj.Attributes)
        {
            Bind(_insertAttribute, tx,
                ("$id", obj.Id.Value),
                ("$name", attr.Name),
                ("$val", attr.Value),
                ("$setter", attr.Creator.Value),
                ("$flags", (int)attr.Flags));
            _insertAttribute.ExecuteNonQuery();
        }

        foreach (var flag in obj.Flags)
        {
            Bind(_insertFlag, tx, ("$id", obj.Id.Value), ("$flag", flag));
            _insertFlag.ExecuteNonQuery();
        }

        foreach (var power in obj.Powers)
        {
            Bind(_insertPower, tx, ("$id", obj.Id.Value), ("$power", power));
            _insertPower.ExecuteNonQuery();
        }

        for (int index = 0; index < obj.Parents.Count; index++)
        {
            Bind(_insertParent, tx,
                ("$id", obj.Id.Value),
                ("$pid", obj.Parents[index].Value),
                ("$prio", index));
            _insertParent.ExecuteNonQuery();
        }

        foreach (var zone in obj.Zones)
        {
            Bind(_insertZone, tx, ("$id", obj.Id.Value), ("$zid", zone.Value));
            _insertZone.ExecuteNonQuery();
        }
    }

    private void DeleteChildren(SqliteTransaction tx, int id)
    {
        foreach (string table in new[] { "attributes", "flags", "powers", "parents", "zones" })
        {
            using var command = _connection.CreateCommand();
            command.Transaction = tx;
            command.CommandText = $"DELETE FROM {table} WHERE object_id = $id;";
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }
    }

    private void DeleteObject(SqliteTransaction tx, int id)
    {
        DeleteChildren(tx, id);
        Bind(_deleteObject, tx, ("$id", id));
        _deleteObject.ExecuteNonQuery();
    }

    private void WriteCron(SqliteTransaction tx, Scheduling.CronScheduler cron)
    {
        using var clear = _connection.CreateCommand();
        clear.Transaction = tx;
        clear.CommandText = "DELETE FROM cron_jobs;";
        clear.ExecuteNonQuery();

        foreach (var job in cron.GetAllJobs())
        {
            using var command = _connection.CreateCommand();
            command.Transaction = tx;
            command.CommandText = @"
                INSERT INTO cron_jobs (id, expression, creator, target, command, last_run, next_run, is_paused)
                VALUES ($jid, $expr, $creator, $target, $cmd, $last, $next, $paused);";
            command.Parameters.AddWithValue("$jid", job.Id);
            command.Parameters.AddWithValue("$expr", job.Expression);
            command.Parameters.AddWithValue("$creator", job.Creator.Value);
            command.Parameters.AddWithValue("$target", job.Target.Value);
            command.Parameters.AddWithValue("$cmd", job.Command);
            command.Parameters.AddWithValue("$last", job.LastRun.HasValue ? job.LastRun.Value.ToUnixTimeSeconds() : DBNull.Value);
            command.Parameters.AddWithValue("$next", job.NextRun.ToUnixTimeSeconds());
            command.Parameters.AddWithValue("$paused", job.IsPaused ? 1 : 0);
            command.ExecuteNonQuery();
        }
    }

    private static void Bind(SqliteCommand command, SqliteTransaction tx, params (string Name, object Value)[] parameters)
    {
        command.Transaction = tx;
        command.Parameters.Clear();
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _upsertObject.Dispose();
        _insertAttribute.Dispose();
        _insertFlag.Dispose();
        _insertPower.Dispose();
        _insertParent.Dispose();
        _insertZone.Dispose();
        _deleteObject.Dispose();
        _connection.Dispose();
    }

    public bool Load(ObjectDatabase db, Scheduling.CronScheduler? cron = null)
    {
        db.Clear();
        MUSHObject.Quiet++;
        db.Changes.Suspend();
        try
        {
            return LoadInto(db, cron);
        }
        finally
        {
            db.Changes.Clear();
            db.Changes.Resume();
            MUSHObject.Quiet--;
        }
    }

    private bool LoadInto(ObjectDatabase db, Scheduling.CronScheduler? cron)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, type, location, contents, exits, next, owner, home, pennies, created_at, modified_at, warnings FROM objects ORDER BY id;";
        using var reader = cmd.ExecuteReader();

        bool hasRows = false;
        while (reader.Read())
        {
            hasRows = true;
            int id = reader.GetInt32(0);
            string name = reader.GetString(1);
            int type = reader.GetInt32(2);
            int loc = reader.GetInt32(3);
            int con = reader.GetInt32(4);
            int ex = reader.GetInt32(5);
            int next = reader.GetInt32(6);
            int owner = reader.GetInt32(7);
            int home = reader.GetInt32(8);
            int pennies = reader.GetInt32(9);
            long created = reader.GetInt64(10);
            long modified = reader.GetInt64(11);
            uint warnings = reader.IsDBNull(12) ? 0u : (uint)reader.GetInt64(12);

            var obj = new MUSHObject(new Dbref(id), name, (ObjectType)type, new Dbref(owner))
            {
                Location = new Dbref(loc),
                Contents = new Dbref(con),
                Exits = new Dbref(ex),
                Next = new Dbref(next),
                Home = new Dbref(home),
                Pennies = pennies,
                CreationTime = DateTimeOffset.FromUnixTimeSeconds(created),
                ModificationTime = DateTimeOffset.FromUnixTimeSeconds(modified),
                Warnings = warnings
            };

            db.SetObject(obj);
        }

        if (!hasRows) return false;

        // Load attributes
        using (var aCmd = _connection.CreateCommand())
        {
            aCmd.CommandText = "SELECT object_id, name, value, setter, flags FROM attributes;";
            using var aReader = aCmd.ExecuteReader();
            while (aReader.Read())
            {
                var obj = db.GetObject(new Dbref(aReader.GetInt32(0)));
                if (obj != null)
                {
                    obj.SetAttribute(
                        aReader.GetString(1),
                        aReader.GetString(2),
                        new Dbref(aReader.GetInt32(3)),
                        (PennMUSH.Core.Attributes.AttributeFlags)aReader.GetInt32(4)
                    );
                }
            }
        }

        // Load flags
        using (var fCmd = _connection.CreateCommand())
        {
            fCmd.CommandText = "SELECT object_id, flag FROM flags;";
            using var fReader = fCmd.ExecuteReader();
            while (fReader.Read())
            {
                var obj = db.GetObject(new Dbref(fReader.GetInt32(0)));
                obj?.AddFlag(fReader.GetString(1));
            }
        }

        // Load powers
        using (var pCmd = _connection.CreateCommand())
        {
            pCmd.CommandText = "SELECT object_id, power FROM powers;";
            using var pReader = pCmd.ExecuteReader();
            while (pReader.Read())
            {
                var obj = db.GetObject(new Dbref(pReader.GetInt32(0)));
                obj?.AddPower(pReader.GetString(1));
            }
        }

        // Load parents
        using (var parentCmd = _connection.CreateCommand())
        {
            parentCmd.CommandText = "SELECT object_id, parent_id FROM parents ORDER BY object_id, priority;";
            using var parentReader = parentCmd.ExecuteReader();
            while (parentReader.Read())
            {
                var obj = db.GetObject(new Dbref(parentReader.GetInt32(0)));
                obj?.AddParent(new Dbref(parentReader.GetInt32(1)));
            }
        }

        // Load zones
        using (var zCmd = _connection.CreateCommand())
        {
            zCmd.CommandText = "SELECT object_id, zone_id FROM zones;";
            using var zReader = zCmd.ExecuteReader();
            while (zReader.Read())
            {
                var obj = db.GetObject(new Dbref(zReader.GetInt32(0)));
                obj?.AddZone(new Dbref(zReader.GetInt32(1)));
            }
        }

        // Load cron jobs
        if (cron != null)
        {
            using var cronCmd = _connection.CreateCommand();
            cronCmd.CommandText = "SELECT id, expression, creator, target, command, last_run, next_run, is_paused FROM cron_jobs;";
            using var cronReader = cronCmd.ExecuteReader();
            while (cronReader.Read())
            {
                string id = cronReader.GetString(0);
                string expr = cronReader.GetString(1);
                var creator = new Dbref(cronReader.GetInt32(2));
                var target = new Dbref(cronReader.GetInt32(3));
                string jobCmd = cronReader.GetString(4);
                DateTimeOffset? lastRun = cronReader.IsDBNull(5) ? null : DateTimeOffset.FromUnixTimeSeconds(cronReader.GetInt64(5));
                DateTimeOffset nextRun = DateTimeOffset.FromUnixTimeSeconds(cronReader.GetInt64(6));
                bool isPaused = cronReader.GetInt32(7) == 1;

                if (Scheduling.CronExpression.TryParse(expr, out var parsedExpr, out _))
                {
                    var job = new Scheduling.CronJob(id, expr, parsedExpr!, creator, target, jobCmd, nextRun)
                    {
                        LastRun = lastRun,
                        IsPaused = isPaused
                    };
                    cron.AddOrUpdateJob(id, expr, creator, target, jobCmd, out _, isPaused);
                }
            }
        }

        db.RebuildIndexes();
        return true;
    }
}
