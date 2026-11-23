namespace PennMUSH.Core.Tests.Server;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using PennMUSH.Core.Types;
using PennMUSH.Server.Engine;
using PennMUSH.Server.Session;
using Xunit;

public class FlatfileImportIntegrationTests
{
    private static string GetTestDataPath(string filename)
    {
        string localPath = Path.Combine(AppContext.BaseDirectory, "TestData", filename);
        if (File.Exists(localPath)) return localPath;

        string sourcePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", filename);
        if (File.Exists(sourcePath)) return Path.GetFullPath(sourcePath);

        throw new FileNotFoundException($"Cannot locate test file {filename}");
    }

    [Fact]
    public async Task MUSHServer_ImportsPennMUSH188Outdb_PersistsToSqliteAndExecutesCommands()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"server_import188_{Guid.NewGuid():N}.db");
        string outdbPath = GetTestDataPath("pennmush-1.8.8p0.outdb");

        try
        {
            var server = new MUSHServer(dbPath, importFlatfilePath: outdbPath);
            Assert.Equal(10, server.Database.Count);

            var outputs = new List<string>();
            var conn = new MUSHConnection(line =>
            {
                outputs.Add(line);
                return Task.CompletedTask;
            });
            conn.PlayerId = new Dbref(1);
            server.RegisterConnection(conn);

            await server.HandleInputAsync(conn, "look");
            await server.HandleInputAsync(conn, "think [name(#8)]|[get(#8/NOTE)]");
            await server.HandleInputAsync(conn, "think [lflags(#1)]");

            string combined = string.Join("\n", outputs);
            Assert.Contains("Room Zero", combined);
            Assert.Contains("Widget|He said \"hi\" \\ back", combined);
            Assert.Contains("WIZARD", combined);
            server.Stop();

            // Re-open from SQLite without importFlatfilePath to verify full persistence.
            var reloaded = new MUSHServer(dbPath);
            Assert.Equal(10, reloaded.Database.Count);
            var widget = reloaded.Database.GetObject(new Dbref(8));
            Assert.NotNull(widget);
            Assert.Equal("Widget", widget.Name);
            Assert.Equal("first line\nsecond line", widget.GetAttribute("MULTI")?.Value.Replace("\r\n", "\n"));
            Assert.NotNull(widget.GetLock("Basic"));
            Assert.NotNull(widget.GetLock("Use"));
            Assert.NotEqual(0u, widget.Warnings);
            reloaded.Stop();
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }

    [Fact]
    public async Task MUSHServer_ImportsPennMUSH214Dump_PersistsToSqliteAndEvaluatesSoftcode()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"server_import214_{Guid.NewGuid():N}.db");
        string dumpPath = GetTestDataPath("pennmush_214.dump");

        try
        {
            var server = new MUSHServer(dbPath, importFlatfilePath: dumpPath);
            Assert.Equal(214, server.Database.Count);

            var outputs = new List<string>();
            var conn = new MUSHConnection(line =>
            {
                outputs.Add(line);
                return Task.CompletedTask;
            });
            conn.PlayerId = new Dbref(1);
            server.RegisterConnection(conn);

            await server.HandleInputAsync(conn, "think [name(#2)]|[name(#29)]");
            string combined = string.Join("\n", outputs);
            Assert.Contains("Master Room|JHV Mordak", combined);
            server.Stop();

            var reloaded = new MUSHServer(dbPath);
            Assert.Equal(214, reloaded.Database.Count);
            reloaded.Stop();
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }

    [Fact]
    public void MUSHServer_AutoBootstrapsFromConfiguredGzippedInputDatabase()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"server_indb_{Guid.NewGuid():N}");
        string dataDir = Path.Combine(tempDir, "data");
        Directory.CreateDirectory(dataDir);

        string cnfPath = Path.Combine(tempDir, "mush.cnf");
        File.WriteAllText(cnfPath, "input_database data/indb\n");

        string gzPath = Path.Combine(dataDir, "indb.gz");
        using (var src = File.OpenRead(GetTestDataPath("pennmush-1.8.8p0.outdb")))
        using (var dst = File.Create(gzPath))
        using (var gz = new GZipStream(dst, CompressionLevel.Fastest))
        {
            src.CopyTo(gz);
        }

        string dbPath = Path.Combine(tempDir, "mush.db");
        try
        {
            var server = new MUSHServer(dbPath, configPath: cnfPath);
            Assert.Equal(10, server.Database.Count);
            Assert.Equal("Widget", server.Database.GetObject(new Dbref(8))?.Name);
            server.Stop();
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
}
