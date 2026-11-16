namespace PennMUSH.Core.Tests.Commands;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;
using PennMUSH.Server.Engine;
using PennMUSH.Server.Session;
using Xunit;

public class MultiUserNotificationTests
{
    [Fact]
    public async Task SayAndPose_BroadcastsCorrectlyToRoomOccupants()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"multiuser_say_{Guid.NewGuid():N}.db");
        try
        {
            var server = new MUSHServer(dbPath);
            var aliceOutputs = new List<string>();
            var bobOutputs = new List<string>();
            var charlieOutputs = new List<string>();

            var aliceConn = new MUSHConnection(line => { aliceOutputs.Add(line); return Task.CompletedTask; });
            var bobConn = new MUSHConnection(line => { bobOutputs.Add(line); return Task.CompletedTask; });
            var charlieConn = new MUSHConnection(line => { charlieOutputs.Add(line); return Task.CompletedTask; });

            server.RegisterConnection(aliceConn);
            server.RegisterConnection(bobConn);
            server.RegisterConnection(charlieConn);

            await server.HandleInputAsync(aliceConn, "create Alice pass1");
            await server.HandleInputAsync(bobConn, "create Bob pass2");
            await server.HandleInputAsync(charlieConn, "create Charlie pass3");

            // Alice and Bob are in Room #0. Move Charlie to Room #2 (Master Room)
            server.Database.MoveObject(charlieConn.PlayerId, new Dbref(2));

            aliceOutputs.Clear();
            bobOutputs.Clear();
            charlieOutputs.Clear();

            // 1. Alice says something
            await server.HandleInputAsync(aliceConn, "say Greetings everyone!");

            // Alice receives self feedback
            Assert.Contains(aliceOutputs, o => o.Contains("You say, \"Greetings everyone!\""));
            // Bob in the same room hears Alice
            Assert.Contains(bobOutputs, o => o.Contains("Alice says, \"Greetings everyone!\""));
            // Charlie in another room hears nothing
            Assert.DoesNotContain(charlieOutputs, o => o.Contains("Greetings everyone!"));

            aliceOutputs.Clear();
            bobOutputs.Clear();
            charlieOutputs.Clear();

            // 2. Alice poses
            await server.HandleInputAsync(aliceConn, ":waves enthusiastically.");

            Assert.Contains(aliceOutputs, o => o.Contains("Alice waves enthusiastically."));
            Assert.Contains(bobOutputs, o => o.Contains("Alice waves enthusiastically."));
            Assert.DoesNotContain(charlieOutputs, o => o.Contains("waves enthusiastically."));

            aliceOutputs.Clear();
            bobOutputs.Clear();

            // 3. Alice semiposes with ;
            await server.HandleInputAsync(aliceConn, ";'s eyes glow.");

            Assert.Contains(aliceOutputs, o => o.Contains("Alice's eyes glow."));
            Assert.Contains(bobOutputs, o => o.Contains("Alice's eyes glow."));

            server.Stop();
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
    public async Task Emit_PrefixesOnlyANospoofListener()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"multiuser_emit_{Guid.NewGuid():N}.db");
        try
        {
            var server = new MUSHServer(dbPath);
            var aliceOutputs = new List<string>();
            var bobOutputs = new List<string>();
            var aliceConn = new MUSHConnection(line => { aliceOutputs.Add(line); return Task.CompletedTask; });
            var bobConn = new MUSHConnection(line => { bobOutputs.Add(line); return Task.CompletedTask; });
            server.RegisterConnection(aliceConn);
            server.RegisterConnection(bobConn);
            await server.HandleInputAsync(aliceConn, "create Alice pass1");
            await server.HandleInputAsync(bobConn, "create Bob pass2");

            aliceOutputs.Clear();
            bobOutputs.Clear();
            await server.HandleInputAsync(aliceConn, "@emit The lights dim.");
            Assert.Contains(bobOutputs, o => o.Contains("The lights dim."));
            Assert.DoesNotContain(bobOutputs, o => o.Contains("[Alice"));
            Assert.DoesNotContain(aliceOutputs, o => o.Contains("[Alice"));

            var bob = server.Database.GetObject(bobConn.PlayerId)!;
            bob.AddFlag("NOSPOOF");
            aliceOutputs.Clear();
            bobOutputs.Clear();
            await server.HandleInputAsync(aliceConn, "@emit The lights dim.");
            Assert.Contains(bobOutputs, o => o.Contains("[Alice:] The lights dim."));
            Assert.DoesNotContain(aliceOutputs, o => o.Contains("[Alice"));

            bobOutputs.Clear();
            await server.HandleInputAsync(aliceConn, "say hello there");
            Assert.Contains(bobOutputs, o => o.Contains("Alice says, \"hello there\""));
            Assert.DoesNotContain(bobOutputs, o => o.Contains("[Alice"));

            bob.AddFlag("PARANOID");
            bobOutputs.Clear();
            await server.HandleInputAsync(aliceConn, "@emit The lights dim.");
            Assert.Contains(bobOutputs, o => o.Contains($"[Alice(#{aliceConn.PlayerId.Value})] The lights dim."));

            var alice = server.Database.GetObject(aliceConn.PlayerId)!;
            alice.AddFlag("NOSPOOF");
            alice.AddFlag("PARANOID");
            aliceOutputs.Clear();
            await server.HandleInputAsync(aliceConn, "@emit The lights dim.");
            Assert.Contains(aliceOutputs, o => o.Contains($"[Alice(#{aliceConn.PlayerId.Value})] The lights dim."));

            alice.AddFlag("WIZARD");
            bobOutputs.Clear();
            await server.HandleInputAsync(aliceConn, "@nsemit The lights dim.");
            Assert.Contains(bobOutputs, o => o.Contains("The lights dim."));
            Assert.DoesNotContain(bobOutputs, o => o.Contains("[Alice"));

            server.Stop();
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
    public async Task MovementAndItemManipulation_BroadcastsToRoom()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"multiuser_move_{Guid.NewGuid():N}.db");
        try
        {
            var server = new MUSHServer(dbPath);
            var aliceOutputs = new List<string>();
            var bobOutputs = new List<string>();
            var charlieOutputs = new List<string>();

            var aliceConn = new MUSHConnection(line => { aliceOutputs.Add(line); return Task.CompletedTask; });
            var bobConn = new MUSHConnection(line => { bobOutputs.Add(line); return Task.CompletedTask; });
            var charlieConn = new MUSHConnection(line => { charlieOutputs.Add(line); return Task.CompletedTask; });

            server.RegisterConnection(aliceConn);
            server.RegisterConnection(bobConn);
            server.RegisterConnection(charlieConn);

            await server.HandleInputAsync(aliceConn, "create Alice pass1");
            await server.HandleInputAsync(bobConn, "create Bob pass2");
            await server.HandleInputAsync(charlieConn, "create Charlie pass3");

            // Setup a new room and an exit
            var destRoom = server.Database.CreateObject("Garden", ObjectType.Room, aliceConn.PlayerId);
            var exit = server.Database.CreateObject("out;garden", ObjectType.Exit, aliceConn.PlayerId);
            server.Database.AttachExit(new Dbref(0), exit.Id);
            server.Database.LinkExit(exit.Id, destRoom.Id);

            // Move Charlie to the destination Garden
            server.Database.MoveObject(charlieConn.PlayerId, destRoom.Id);

            aliceOutputs.Clear();
            bobOutputs.Clear();
            charlieOutputs.Clear();

            // 1. Item manipulation: Alice creates a thing and drops/takes it
            var rock = server.Database.CreateObject("Shiny Rock", ObjectType.Thing, aliceConn.PlayerId);
            server.Database.MoveObject(rock.Id, aliceConn.PlayerId);

            await server.HandleInputAsync(aliceConn, "drop Shiny Rock");
            Assert.Contains(aliceOutputs, o => o.Contains("Dropped."));
            Assert.Contains(bobOutputs, o => o.Contains("Alice drops Shiny Rock."));

            aliceOutputs.Clear();
            bobOutputs.Clear();

            await server.HandleInputAsync(aliceConn, "get Shiny Rock");
            Assert.Contains(aliceOutputs, o => o.Contains("Taken."));
            Assert.Contains(bobOutputs, o => o.Contains("Alice takes Shiny Rock."));

            aliceOutputs.Clear();
            bobOutputs.Clear();
            charlieOutputs.Clear();

            // 2. Traversal: Alice goes to the garden
            await server.HandleInputAsync(aliceConn, "garden");

            // Bob in Room 0 sees Alice leave
            Assert.Contains(bobOutputs, o => o.Contains("Alice leaves."));
            // Charlie in Garden sees Alice arrive
            Assert.Contains(charlieOutputs, o => o.Contains("Alice has arrived."));
            // Alice sees room name
            Assert.Contains(aliceOutputs, o => o.Contains("Garden"));

            server.Stop();
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }
}
