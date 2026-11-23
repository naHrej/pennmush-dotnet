namespace PennMUSH.Core.Tests.Server;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PennMUSH.Server.Engine;
using PennMUSH.Server.Session;

public class RoomSessionTests
{
    [Fact]
    public async Task SpeechStaysInTheRoomAfterAMove()
    {
        string path = Path.Combine(Path.GetTempPath(), "rooms-" + Guid.NewGuid().ToString("N") + ".db");
        var server = new MUSHServer(path) { AutosaveEnabled = false };
        var oneLines = new List<string>();
        var heroLines = new List<string>();
        try
        {
            var one = new MUSHConnection(line =>
            {
                oneLines.Add(line);
                return Task.CompletedTask;
            });
            var hero = new MUSHConnection(line =>
            {
                heroLines.Add(line);
                return Task.CompletedTask;
            });
            server.RegisterConnection(one);
            server.RegisterConnection(hero);
            await server.HandleInputAsync(one, "co One");
            await server.HandleInputAsync(hero, "cr Hero secret");

            int oneMark = oneLines.Count;
            await server.HandleInputAsync(hero, "say hello room");
            Assert.Contains(oneLines.Skip(oneMark), line => line.Contains("hello room"));

            await server.HandleInputAsync(one, "@teleport #2");
            oneMark = oneLines.Count;
            int heroMark = heroLines.Count;
            await server.HandleInputAsync(hero, "say still here");
            await server.HandleInputAsync(one, "say other room");

            Assert.DoesNotContain(oneLines.Skip(oneMark), line => line.Contains("still here"));
            Assert.DoesNotContain(heroLines.Skip(heroMark), line => line.Contains("other room"));
        }
        finally
        {
            server.Stop();
            SqliteConnection.ClearAllPools();
            foreach (string suffix in new[] { "", "-wal", "-shm" })
            {
                string file = path + suffix;
                if (File.Exists(file))
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }
    }
}
