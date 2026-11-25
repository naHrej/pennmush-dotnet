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

public class ComprehensiveCommandTests
{
    [Fact]
    public async Task EmitsAndMessaging_WorkAcrossMultiUsers()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"comprehensive_emits_{Guid.NewGuid():N}.db");
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

            // Move Charlie to Master Room (#2)
            server.Database.MoveObject(charlieConn.PlayerId, new Dbref(2));

            aliceOutputs.Clear();
            bobOutputs.Clear();
            charlieOutputs.Clear();

            // 1. @emit
            await server.HandleInputAsync(aliceConn, "@emit A soft bell rings.");
            Assert.Contains(aliceOutputs, o => o.Contains("A soft bell rings."));
            Assert.Contains(bobOutputs, o => o.Contains("A soft bell rings."));
            Assert.DoesNotContain(charlieOutputs, o => o.Contains("A soft bell rings."));

            aliceOutputs.Clear();
            bobOutputs.Clear();

            // 2. @oemit (everyone in room except Bob)
            await server.HandleInputAsync(aliceConn, "@oemit Bob=Alice winks secretly.");
            Assert.Contains(aliceOutputs, o => o.Contains("Alice winks secretly."));
            Assert.DoesNotContain(bobOutputs, o => o.Contains("Alice winks secretly."));

            aliceOutputs.Clear();
            bobOutputs.Clear();
            charlieOutputs.Clear();

            // 3. @pemit
            await server.HandleInputAsync(aliceConn, "@pemit Charlie=Secret message for Charlie");
            Assert.Contains(aliceOutputs, o => o.Contains("Message sent to Charlie."));
            Assert.Contains(charlieOutputs, o => o.Contains("Secret message for Charlie"));
            Assert.DoesNotContain(bobOutputs, o => o.Contains("Secret message for Charlie"));

            aliceOutputs.Clear();
            bobOutputs.Clear();
            charlieOutputs.Clear();

            // 4. @remit (to Room #2 where Charlie is)
            await server.HandleInputAsync(aliceConn, "@remit #2=Remote broadcast to Master Room");
            Assert.Contains(charlieOutputs, o => o.Contains("Remote broadcast to Master Room"));
            Assert.DoesNotContain(bobOutputs, o => o.Contains("Remote broadcast to Master Room"));

            aliceOutputs.Clear();
            bobOutputs.Clear();
            charlieOutputs.Clear();

            // 5. whisper (in-room)
            await server.HandleInputAsync(aliceConn, "whisper Bob=Psst, over here.");
            Assert.Contains(aliceOutputs, o => o.Contains("You whisper, \"Psst, over here.\" to Bob."));
            Assert.Contains(bobOutputs, o => o.Contains("Alice whispers: Psst, over here."));
            Assert.DoesNotContain(charlieOutputs, o => o.Contains("Psst"));

            aliceOutputs.Clear();
            bobOutputs.Clear();
            charlieOutputs.Clear();

            // 6. page (cross-room)
            await server.HandleInputAsync(aliceConn, "page Charlie=Can you hear me across the world?");
            Assert.Contains(aliceOutputs, o => o.Contains("You page Charlie with 'Can you hear me across the world?'."));
            Assert.Contains(charlieOutputs, o => o.Contains("Alice pages: Can you hear me across the world?"));

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
    public async Task SpatialInteractions_HomeAndGive()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"comprehensive_spatial_{Guid.NewGuid():N}.db");
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

            // Give Bob 50 pennies
            aliceOutputs.Clear();
            bobOutputs.Clear();
            await server.HandleInputAsync(aliceConn, "give Bob=20");
            Assert.Contains(aliceOutputs, o => o.Contains("You give 20 pennies to Bob."));
            Assert.Contains(bobOutputs, o => o.Contains("Alice gives you 20 pennies."));

            // Examine
            aliceOutputs.Clear();
            await server.HandleInputAsync(aliceConn, "examine Bob");
            Assert.Contains(aliceOutputs, o => o.Contains("Pennies: 170"));

            // Move Alice to #2, then type 'home'
            server.Database.MoveObject(aliceConn.PlayerId, new Dbref(2));
            aliceOutputs.Clear();
            bobOutputs.Clear();

            await server.HandleInputAsync(aliceConn, "home");
            Assert.Contains(aliceOutputs, o => o.Contains("There's no place like home..."));
            Assert.Contains(bobOutputs, o => o.Contains("Alice arrives from home."));

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
    public async Task ScriptingAndFlowControl_SwitchDolistTrigger()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"comprehensive_scripting_{Guid.NewGuid():N}.db");
        try
        {
            var server = new MUSHServer(dbPath);
            var outputs = new List<string>();

            var conn = new MUSHConnection(line => { outputs.Add(line); return Task.CompletedTask; });
            server.RegisterConnection(conn);
            await server.HandleInputAsync(conn, "create Coder pass1");

            outputs.Clear();

            // 1. @switch
            await server.HandleInputAsync(conn, "@switch 42=10,say Ten,42,say FortyTwo,say Default");
            Assert.Contains(outputs, o => o.Contains("You say, \"FortyTwo\""));

            outputs.Clear();

            // 2. @dolist
            await server.HandleInputAsync(conn, "@dolist Red Green Blue=say Color: ##");
            Assert.Contains(outputs, o => o.Contains("You say, \"Color: Red\""));
            Assert.Contains(outputs, o => o.Contains("You say, \"Color: Green\""));
            Assert.Contains(outputs, o => o.Contains("You say, \"Color: Blue\""));

            outputs.Clear();

            // 3. @trigger
            var player = server.Database.GetObject(conn.PlayerId)!;
            player.SetAttribute("GREET", "say Welcome %0 to %1!", conn.PlayerId);

            await server.HandleInputAsync(conn, "@trigger me/GREET=Adventurer,PennMUSH");
            Assert.Contains(outputs, o => o.Contains("You say, \"Welcome Adventurer to PennMUSH!\""));

            // 4. @ifelse
            outputs.Clear();
            await server.HandleInputAsync(conn, "@ifelse 1=say Truth,say False");
            Assert.Contains(outputs, o => o.Contains("You say, \"Truth\""));

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
    public async Task ObjectLifecycle_CloneWipeLockStats()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"comprehensive_lifecycle_{Guid.NewGuid():N}.db");
        try
        {
            var server = new MUSHServer(dbPath);
            var outputs = new List<string>();

            var conn = new MUSHConnection(line => { outputs.Add(line); return Task.CompletedTask; });
            server.RegisterConnection(conn);
            await server.HandleInputAsync(conn, "create Builder pass1");

            outputs.Clear();

            // 1. Create a thing and set attribute
            await server.HandleInputAsync(conn, "@create Magic Wand");
            var wand = server.Database.FindObject("Magic Wand")!;
            wand.SetAttribute("CHARGES", "10", conn.PlayerId);

            // 2. @clone
            await server.HandleInputAsync(conn, "@clone Magic Wand");
            Assert.Contains(outputs, o => o.Contains("Cloned: Magic Wand"));

            // Verify clone has attribute
            var clones = server.Database.GetAllObjects().Where(o => o.Name == "Magic Wand").ToList();
            Assert.Equal(2, clones.Count);
            Assert.NotNull(clones[1].GetAttribute("CHARGES"));

            outputs.Clear();

            // 3. @lock and @unlock
            await server.HandleInputAsync(conn, "@lock/Use Magic Wand=me");
            Assert.Contains(outputs, o => o.Contains("Locked (Use) on Magic Wand."));

            outputs.Clear();
            await server.HandleInputAsync(conn, "@unlock/Use Magic Wand");
            Assert.Contains(outputs, o => o.Contains("Unlocked (Use) on Magic Wand."));

            // 4. @wipe
            outputs.Clear();
            await server.HandleInputAsync(conn, "@wipe Magic Wand/CHARG*");
            Assert.Contains(outputs, o => o.Contains("1 attributes wiped."));

            // 5. @stats
            outputs.Clear();
            await server.HandleInputAsync(conn, "@stats");
            Assert.Contains(outputs, o => o.Contains("Total objects:"));

            // 6. doing & score
            outputs.Clear();
            await server.HandleInputAsync(conn, "doing Building awesome things!");
            Assert.Contains(outputs, o => o.Contains("DOING set to: Building awesome things!"));

            outputs.Clear();
            await server.HandleInputAsync(conn, "score");
            Assert.Contains(outputs, o => o.Contains("pennies."));

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
