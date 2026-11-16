namespace PennMUSH.Core.Database;

using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class SqliteStore
{
    private readonly string _connectionString;

    public SqliteStore(string connectionString = "Data Source=mush.db")
    {
        _connectionString = connectionString;
        InitializeSchema();
    }

    public void InitializeSchema()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
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
        using var alter = conn.CreateCommand();
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
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var tx = conn.BeginTransaction();

        using (var clearCmd = conn.CreateCommand())
        {
            clearCmd.Transaction = tx;
            clearCmd.CommandText = "DELETE FROM attributes; DELETE FROM flags; DELETE FROM powers; DELETE FROM parents; DELETE FROM zones; DELETE FROM objects; DELETE FROM cron_jobs;";
            clearCmd.ExecuteNonQuery();
        }

        foreach (var obj in db.GetAllObjects())
        {
            using (var objCmd = conn.CreateCommand())
            {
                objCmd.Transaction = tx;
                objCmd.CommandText = @"
                    INSERT INTO objects (id, name, type, location, contents, exits, next, owner, home, pennies, warnings, created_at, modified_at)
                    VALUES ($id, $name, $type, $loc, $con, $ex, $next, $owner, $home, $pennies, $warnings, $created, $modified);
                ";
                objCmd.Parameters.AddWithValue("$id", obj.Id.Value);
                objCmd.Parameters.AddWithValue("$name", obj.Name);
                objCmd.Parameters.AddWithValue("$type", (int)obj.Type);
                objCmd.Parameters.AddWithValue("$loc", obj.Location.Value);
                objCmd.Parameters.AddWithValue("$con", obj.Contents.Value);
                objCmd.Parameters.AddWithValue("$ex", obj.Exits.Value);
                objCmd.Parameters.AddWithValue("$next", obj.Next.Value);
                objCmd.Parameters.AddWithValue("$owner", obj.Owner.Value);
                objCmd.Parameters.AddWithValue("$home", obj.Home.Value);
                objCmd.Parameters.AddWithValue("$pennies", obj.Pennies);
                objCmd.Parameters.AddWithValue("$warnings", (long)obj.Warnings);
                objCmd.Parameters.AddWithValue("$created", obj.CreationTime.ToUnixTimeSeconds());
                objCmd.Parameters.AddWithValue("$modified", obj.ModificationTime.ToUnixTimeSeconds());
                objCmd.ExecuteNonQuery();
            }

            foreach (var attr in obj.Attributes)
            {
                using var attrCmd = conn.CreateCommand();
                attrCmd.Transaction = tx;
                attrCmd.CommandText = "INSERT INTO attributes (object_id, name, value, setter, flags) VALUES ($id, $name, $val, $setter, $flags);";
                attrCmd.Parameters.AddWithValue("$id", obj.Id.Value);
                attrCmd.Parameters.AddWithValue("$name", attr.Name);
                attrCmd.Parameters.AddWithValue("$val", attr.Value);
                attrCmd.Parameters.AddWithValue("$setter", attr.Creator.Value);
                attrCmd.Parameters.AddWithValue("$flags", (int)attr.Flags);
                attrCmd.ExecuteNonQuery();
            }

            foreach (var f in obj.Flags)
            {
                using var fCmd = conn.CreateCommand();
                fCmd.Transaction = tx;
                fCmd.CommandText = "INSERT INTO flags (object_id, flag) VALUES ($id, $flag);";
                fCmd.Parameters.AddWithValue("$id", obj.Id.Value);
                fCmd.Parameters.AddWithValue("$flag", f);
                fCmd.ExecuteNonQuery();
            }

            foreach (var p in obj.Powers)
            {
                using var pCmd = conn.CreateCommand();
                pCmd.Transaction = tx;
                pCmd.CommandText = "INSERT INTO powers (object_id, power) VALUES ($id, $power);";
                pCmd.Parameters.AddWithValue("$id", obj.Id.Value);
                pCmd.Parameters.AddWithValue("$power", p);
                pCmd.ExecuteNonQuery();
            }

            for (int pIdx = 0; pIdx < obj.Parents.Count; pIdx++)
            {
                using var parentCmd = conn.CreateCommand();
                parentCmd.Transaction = tx;
                parentCmd.CommandText = "INSERT INTO parents (object_id, parent_id, priority) VALUES ($id, $pid, $prio);";
                parentCmd.Parameters.AddWithValue("$id", obj.Id.Value);
                parentCmd.Parameters.AddWithValue("$pid", obj.Parents[pIdx].Value);
                parentCmd.Parameters.AddWithValue("$prio", pIdx);
                parentCmd.ExecuteNonQuery();
            }

            foreach (var z in obj.Zones)
            {
                using var zCmd = conn.CreateCommand();
                zCmd.Transaction = tx;
                zCmd.CommandText = "INSERT INTO zones (object_id, zone_id) VALUES ($id, $zid);";
                zCmd.Parameters.AddWithValue("$id", obj.Id.Value);
                zCmd.Parameters.AddWithValue("$zid", z.Value);
                zCmd.ExecuteNonQuery();
            }
        }

        if (cron != null)
        {
            foreach (var job in cron.GetAllJobs())
            {
                using var cCmd = conn.CreateCommand();
                cCmd.Transaction = tx;
                cCmd.CommandText = @"
                    INSERT INTO cron_jobs (id, expression, creator, target, command, last_run, next_run, is_paused)
                    VALUES ($jid, $expr, $creator, $target, $cmd, $last, $next, $paused);
                ";
                cCmd.Parameters.AddWithValue("$jid", job.Id);
                cCmd.Parameters.AddWithValue("$expr", job.Expression);
                cCmd.Parameters.AddWithValue("$creator", job.Creator.Value);
                cCmd.Parameters.AddWithValue("$target", job.Target.Value);
                cCmd.Parameters.AddWithValue("$cmd", job.Command);
                cCmd.Parameters.AddWithValue("$last", job.LastRun.HasValue ? job.LastRun.Value.ToUnixTimeSeconds() : (object)DBNull.Value);
                cCmd.Parameters.AddWithValue("$next", job.NextRun.ToUnixTimeSeconds());
                cCmd.Parameters.AddWithValue("$paused", job.IsPaused ? 1 : 0);
                cCmd.ExecuteNonQuery();
            }
        }

        tx.Commit();
    }

    public bool Load(ObjectDatabase db, Scheduling.CronScheduler? cron = null)
    {
        db.Clear();

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
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
        using (var aCmd = conn.CreateCommand())
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
        using (var fCmd = conn.CreateCommand())
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
        using (var pCmd = conn.CreateCommand())
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
        using (var parentCmd = conn.CreateCommand())
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
        using (var zCmd = conn.CreateCommand())
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
            using var cronCmd = conn.CreateCommand();
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

        return true;
    }
}
