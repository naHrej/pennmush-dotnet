namespace PennMUSH.Core.Tests.Commands;

using System.Collections.Concurrent;
using PennMUSH.Core.Access;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;
using PennMUSH.Server.Session;
using Xunit;

public class CommandModeSwitchTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly ExpressionEvaluator _evaluator;
    private readonly ActionQueue _queue = new();
    private readonly Dbref _god;
    private readonly Dbref _ada;
    private readonly Dbref _bea;
    private string _dumpMode = "";

    public CommandModeSwitchTests()
    {
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.Owner = god.Id;
        _god = god.Id;
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        ada.Owner = ada.Id;
        _ada = ada.Id;
        var bea = _db.CreateObject("Bea", ObjectType.Player, new Dbref(1));
        bea.Owner = bea.Id;
        _bea = bea.Id;
        var functions = new FunctionRegistry(_db);
        _evaluator = new ExpressionEvaluator(functions.Dispatch, _db);
        functions.Evaluator = _evaluator;
        _dispatcher = new CommandDispatcher(_evaluator);
        _dispatcher.Register(new ListCommandHandler(_dispatcher, new FunctionRegistry()), "@list");
        _dispatcher.Register(new SearchCommandHandler(_evaluator), "@search");
        _dispatcher.Register(new DumpCommandHandler(mode => _dumpMode = mode), "@dump");
        _dispatcher.Register(new EditAttributeCommandHandler(), "@edit");
        _dispatcher.Register(new PsCommandHandler(_queue), "@ps");
        _dispatcher.Register(new WaitCommandHandler(_queue), "@wait");
        _dispatcher.Register(new StatsCommandHandler(), "@stats");
        _dispatcher.Register(new IncludeCommandHandler(_dispatcher, _evaluator), "@include");
        _dispatcher.Register(new PemitCommandHandler(), "@pemit");
        _dispatcher.Register(new HookCommandHandler(_dispatcher), "@hook");
        _dispatcher.Register(new PingCommandHandler("ping", "pong"), "ping");
        _dispatcher.Register(new GrepCommandHandler(), "@grep");
        _dispatcher.Register(new CommandCommandHandler(_dispatcher), "@command");
    }

    [Fact]
    public void List_LocksAllocationsBuiltinLocalAndLowercase()
    {
        var locks = _dispatcher.Dispatch(_ada, "@list/locks", _db);
        Assert.Contains("Basic", locks.Notifications.Single());
        Assert.Contains("Examine", locks.Notifications.Single());

        var lower = _dispatcher.Dispatch(_ada, "@list/lowercase locks", _db);
        Assert.Contains("basic", lower.Notifications.Single());
        Assert.DoesNotContain("Basic", lower.Notifications.Single());

        var denied = _dispatcher.Dispatch(_ada, "@list/allocations", _db);
        Assert.Equal("Permission denied.", denied.Notifications.Single());
        var memory = _dispatcher.Dispatch(_god, "@list allocations", _db);
        Assert.Contains("Memory allocations:", memory.Notifications);
        Assert.Contains(memory.Notifications, line => line.StartsWith("GC heap:"));
        Assert.Contains(memory.Notifications, line => line.StartsWith("Working set:"));

        var flags = _dispatcher.Dispatch(_ada, "@list/lowercase flags", _db);
        Assert.Contains("wizard", flags.Notifications.Single());

        _dispatcher.Dispatch(_god, "@command/add frob", _db);
        var local = _dispatcher.Dispatch(_god, "@list/local commands", _db);
        Assert.Contains("frob", local.Notifications.Single());
        Assert.DoesNotContain("@dump", local.Notifications.Single());
        var builtin = _dispatcher.Dispatch(_god, "@list/builtin commands", _db);
        Assert.Contains("@dump", builtin.Notifications.Single());
        Assert.DoesNotContain("frob", builtin.Notifications.Single());

        var registry = new FunctionRegistry();
        registry.SaveSoft(new SoftFunction { Name = "frobnicate" });
        var listed = new CommandDispatcher();
        listed.Register(new ListCommandHandler(listed, registry), "@list");
        var soft = listed.Dispatch(_ada, "@list/local functions", _db);
        Assert.Equal("frobnicate", soft.Notifications.Single());
        var hard = listed.Dispatch(_ada, "@list/builtin functions", _db);
        Assert.Contains("ADD", hard.Notifications.Single());
        Assert.DoesNotContain("frobnicate", hard.Notifications.Single());
    }

    [Fact]
    public void Search_EvalLockCommandListenAndTypedEval()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _ada);
        lamp.SetAttribute("CMD", "$frob:say hi", _ada);
        lamp.SetAttribute("LISTEN", "hello", _ada);
        var room = _db.CreateObject("Study", ObjectType.Room, _ada);
        room.SetAttribute("EAR", "^knock:say who", _ada);

        var eval = _dispatcher.Dispatch(_ada, "@search eval=1", _db);
        Assert.Contains($"Lamp (#{lamp.Id.Value})", eval.Notifications);
        var none = _dispatcher.Dispatch(_ada, "@search eval=0", _db);
        Assert.Contains("Nothing found.", none.Notifications);

        var players = _dispatcher.Dispatch(_ada, "@search eplayer=1", _db);
        Assert.Contains(players.Notifications, line => line.Contains("Ada"));
        Assert.DoesNotContain(players.Notifications, line => line.Contains("Lamp"));

        var things = _dispatcher.Dispatch(_ada, "@search ething=1", _db);
        Assert.Contains($"Lamp (#{lamp.Id.Value})", things.Notifications);
        Assert.DoesNotContain(things.Notifications, line => line.Contains("Study"));

        var rooms = _dispatcher.Dispatch(_ada, "@search eroom=1", _db);
        Assert.Contains($"Study (#{room.Id.Value})", rooms.Notifications);

        var locked = _dispatcher.Dispatch(_ada, $"@search elock=#{lamp.Id.Value}", _db);
        Assert.Contains($"Lamp (#{lamp.Id.Value})", locked.Notifications);
        Assert.DoesNotContain(locked.Notifications, line => line.Contains("Study"));

        var command = _dispatcher.Dispatch(_ada, "@search command=frob", _db);
        Assert.Contains($"Lamp (#{lamp.Id.Value})", command.Notifications);
        Assert.DoesNotContain(command.Notifications, line => line.Contains("Study"));

        var heard = _dispatcher.Dispatch(_ada, "@search listen=hello", _db);
        Assert.Contains($"Lamp (#{lamp.Id.Value})", heard.Notifications);
        var knock = _dispatcher.Dispatch(_ada, "@search listen=knock", _db);
        Assert.Contains($"Study (#{room.Id.Value})", knock.Notifications);
    }

    [Fact]
    public void Dump_SelectsParanoidDebugAndNofork()
    {
        var paranoid = _dispatcher.Dispatch(_god, "@dump/paranoid", _db);
        Assert.Equal("paranoid", _dumpMode);
        Assert.Equal("Paranoid dump complete.", paranoid.Notifications.Single());

        var debug = _dispatcher.Dispatch(_god, "@dump/debug", _db);
        Assert.Equal("debug", _dumpMode);
        Assert.Equal("Debug dump complete.", debug.Notifications.Single());

        var nofork = _dispatcher.Dispatch(_god, "@dump/nofork", _db);
        Assert.Equal("nofork", _dumpMode);
        Assert.Equal("Database saved without forking.", nofork.Notifications.Single());
    }

    [Fact]
    public void Edit_FirstRegexpCheckAndQuiet()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _ada);
        lamp.SetAttribute("DESC", "bright bright", _ada);

        var first = _dispatcher.Dispatch(_ada, $"@edit/first #{lamp.Id.Value}/DESC=bright,dim", _db);
        Assert.Equal("Edited.", first.Notifications.Single());
        Assert.Equal("dim bright", lamp.GetAttribute("DESC")!.Value);

        lamp.SetAttribute("DESC", "bright bright", _ada);
        _dispatcher.Dispatch(_ada, $"@edit/regexp #{lamp.Id.Value}/DESC=bright,dim", _db);
        Assert.Equal("dim bright", lamp.GetAttribute("DESC")!.Value);

        lamp.SetAttribute("DESC", "bright bright", _ada);
        _dispatcher.Dispatch(_ada, $"@edit/regexp/all #{lamp.Id.Value}/DESC=bright,dim", _db);
        Assert.Equal("dim dim", lamp.GetAttribute("DESC")!.Value);

        lamp.SetAttribute("DESC", "Bright", _ada);
        _dispatcher.Dispatch(_ada, $"@edit/regexp/nocase #{lamp.Id.Value}/DESC=bright,dim", _db);
        Assert.Equal("dim", lamp.GetAttribute("DESC")!.Value);

        lamp.SetAttribute("DESC", "bright", _ada);
        var check = _dispatcher.Dispatch(_ada, $"@edit/check #{lamp.Id.Value}/DESC=bright,dim", _db);
        Assert.Equal("Would edit to: dim", check.Notifications.Single());
        Assert.Equal("bright", lamp.GetAttribute("DESC")!.Value);

        var quiet = _dispatcher.Dispatch(_ada, $"@edit/quiet #{lamp.Id.Value}/DESC=bright,dim", _db);
        Assert.Empty(quiet.Notifications);
        Assert.Equal("dim", lamp.GetAttribute("DESC")!.Value);

        var bad = _dispatcher.Dispatch(_ada, $"@edit/regexp #{lamp.Id.Value}/DESC=[,x", _db);
        Assert.Equal("Invalid Regular Expression.", bad.Notifications.Single());
    }

    [Fact]
    public void Ps_QuickAndDebug()
    {
        var entry = _queue.Enqueue(
            _ada,
            _bea,
            "say hi",
            args: new[] { "one" },
            registers: new Dictionary<string, string> { ["0"] = "kept" });
        Assert.NotNull(entry);

        var quick = _dispatcher.Dispatch(_ada, "@ps/quick", _db);
        Assert.Contains($"#{entry!.Pid} say hi", quick.Notifications);
        Assert.DoesNotContain(quick.Notifications, line => line.StartsWith("Queue for"));

        var debug = _dispatcher.Dispatch(_ada, $"@ps/debug {entry.Pid}", _db);
        Assert.Contains($"Enactor: #{_bea.Value}", debug.Notifications);
        Assert.Contains("Args: one", debug.Notifications);
        Assert.Contains("%0: kept", debug.Notifications);

        var plain = _dispatcher.Dispatch(_ada, "@ps/debug", _db);
        Assert.Contains(plain.Notifications, line => line.StartsWith("Queue for"));
        Assert.DoesNotContain(plain.Notifications, line => line.StartsWith("Enactor:"));
    }

    [Fact]
    public void Stats_MemoryAndFlags()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _ada);
        lamp.AddFlag("VISUAL");

        var denied = _dispatcher.Dispatch(_ada, "@stats/tables", _db);
        Assert.Equal("Permission denied.", denied.Notifications.Single());

        var tables = _dispatcher.Dispatch(_god, "@stats/tables", _db);
        Assert.Contains("Heap:", tables.Notifications.Single());
        var chunks = _dispatcher.Dispatch(_god, "@stats/chunks", _db);
        Assert.Contains("Chunks:", chunks.Notifications.Single());
        var regions = _dispatcher.Dispatch(_god, "@stats/regions", _db);
        Assert.Contains("Gen0 collections:", regions.Notifications.Single());
        var both = _dispatcher.Dispatch(_god, "@stats/chunks/regions", _db);
        Assert.Contains("Regions:", both.Notifications.Single());
        var paging = _dispatcher.Dispatch(_god, "@stats/paging", _db);
        Assert.Contains("Paging:", paging.Notifications.Single());
        var free = _dispatcher.Dispatch(_god, "@stats/freespace", _db);
        Assert.Contains("Freespace:", free.Notifications.Single());

        var flags = _dispatcher.Dispatch(_ada, "@stats/flags", _db);
        Assert.Contains("VISUAL: 1", flags.Notifications.Single());
    }

    [Fact]
    public void IncludeAndHook_LocalizeAndClearRegisters()
    {
        var bea = _db.GetObject(_bea)!;
        bea.SetAttribute("FOO", "[setq(0,changed)]@pemit me=%q0", _bea);
        var include = new IncludeCommandHandler(_dispatcher, _evaluator);

        var plain = Context("@include", "Bea/FOO");
        plain.Registers["0"] = "kept";
        Assert.True(include.Execute(plain, _db));
        Assert.Equal("changed", plain.Registers["0"]);
        Assert.Contains("changed", Messages(plain));

        var localize = Context("@include", "Bea/FOO", "localize");
        localize.Registers["0"] = "kept";
        Assert.True(include.Execute(localize, _db));
        Assert.Equal("kept", localize.Registers["0"]);
        Assert.Contains("changed", Messages(localize));

        var cleared = Context("@include", "Bea/FOO", "clearregs");
        cleared.Registers["0"] = "kept";
        Assert.True(include.Execute(cleared, _db));
        Assert.Equal("kept", cleared.Registers["0"]);
        Assert.Contains("changed", Messages(cleared));

        bea.SetAttribute("SWAP", "$ping:[setq(0,hooked)]ping", _bea);
        var hooked = _dispatcher.Dispatch(_god, $"@hook/override ping=#{_bea.Value}/SWAP", _db);
        Assert.Equal("Hook set for PING.", hooked.Notifications[0]);
        var ran = _dispatcher.Dispatch(_ada, "ping", _db);
        Assert.Equal("pong", ran.Notifications[0]);
        Assert.Equal("hooked", ran.Registers["0"]);

        _dispatcher.Dispatch(_god, $"@hook/override/localize ping=#{_bea.Value}/SWAP", _db);
        var hidden = _dispatcher.Dispatch(_ada, "ping", _db);
        Assert.Equal("pong", hidden.Notifications[0]);
        Assert.False(hidden.Registers.ContainsKey("0"));

        _dispatcher.Dispatch(_god, $"@hook/override/clearregs ping=#{_bea.Value}/SWAP", _db);
        var reset = _dispatcher.Dispatch(_ada, "ping", _db);
        Assert.Equal("pong", reset.Notifications[0]);
        Assert.False(reset.Registers.ContainsKey("0"));
    }

    private sealed class PingCommandHandler : ICommandHandler
    {
        private readonly string _text;
        public PingCommandHandler(string name, string text)
        {
            Name = name;
            _text = text;
        }
        public string Name { get; }
        public bool Execute(CommandContext context, ObjectDatabase db)
        {
            context.Notify(_text);
            return true;
        }
    }

    [Fact]
    public async Task Sockset_PrefixSuffixAccentsPromptAndQuota()
    {
        var lines = new List<string>();
        var conn = new MUSHConnection(text =>
        {
            lines.Add(text);
            return Task.CompletedTask;
        })
        {
            PlayerId = _god,
            OutputPrefix = "PRE",
            OutputSuffix = "POST",
            StripAccents = true
        };

        await conn.SendLineAsync("café");
        Assert.Contains(lines, line => line.Contains("PRE"));
        Assert.Contains(lines, line => line.Contains("cafe") && !line.Contains("café"));
        Assert.Contains(lines, line => line.Contains("POST"));

        lines.Clear();
        conn.StripAccents = false;
        conn.PromptNewlines = false;
        await conn.SendRawAsync("go");
        Assert.DoesNotContain(lines, line => line.Contains("\n") || line.Contains("\r"));

        lines.Clear();
        conn.PromptNewlines = true;
        await conn.SendPromptAsync("go");
        Assert.Contains(lines, line => line.Contains("go") && (line.Contains("\n") || line.Contains("\r")));

        var connections = new ConcurrentDictionary<string, MUSHConnection>();
        connections[conn.Id] = conn;
        var sessions = new ConnectionDirectory(connections, _ => { });
        Assert.False(sessions.IgnoresQuota(_god));
        conn.NoQuota = true;
        Assert.True(sessions.IgnoresQuota(_god));

        var queue = new ActionQueue
        {
            MaxQueueEntriesPerPlayer = 1,
            IgnoresQueueLimit = id => sessions.IgnoresQuota(id)
        };
        Assert.NotNull(queue.Enqueue(_ada, _ada, "one"));
        Assert.Null(queue.Enqueue(_ada, _ada, "two"));
        Assert.NotNull(queue.Enqueue(_god, _god, "one"));
        Assert.NotNull(queue.Enqueue(_god, _god, "two"));
    }

    [Fact]
    public void Grep_ListPrintRegexpWildCaseAndParent()
    {
        var parent = _db.CreateObject("Form", ObjectType.Thing, _ada);
        parent.SetAttribute("DESC", "Dimmer", _ada);
        var child = _db.CreateObject("Copy", ObjectType.Thing, _ada);
        child.AddParent(parent.Id, _db);
        child.SetAttribute("NOTE", "a1b", _ada);

        var sensitive = _dispatcher.Dispatch(_ada, $"@grep #{child.Id.Value}=dimmer", _db);
        Assert.Equal("No match.", sensitive.Notifications.Single());
        var folded = _dispatcher.Dispatch(_ada, $"@grep/nocase #{child.Id.Value}=dimmer", _db);
        Assert.Equal("No match.", folded.Notifications.Single());
        var inherited = _dispatcher.Dispatch(_ada, $"@grep/parent/nocase #{child.Id.Value}=dimmer", _db);
        Assert.Contains("DESC: Dimmer", inherited.Notifications.Single());

        var list = _dispatcher.Dispatch(_ada, $"@grep/list #{child.Id.Value}=a1b", _db);
        Assert.Equal("NOTE", list.Notifications.Single());
        var print = _dispatcher.Dispatch(_ada, $"@grep/print #{child.Id.Value}=a1b", _db);
        Assert.Equal("NOTE: a1b", print.Notifications.Single());
        var ilist = _dispatcher.Dispatch(_ada, $"@grep/ilist #{child.Id.Value}=A1B", _db);
        Assert.Equal("NOTE", ilist.Notifications.Single());

        var pattern = _dispatcher.Dispatch(_ada, $"@grep/regexp #{child.Id.Value}=a\\db", _db);
        Assert.Equal("NOTE: a1b", pattern.Notifications.Single());
        var wild = _dispatcher.Dispatch(_ada, $"@grep/wild #{child.Id.Value}=a?b", _db);
        Assert.Equal("NOTE: a1b", wild.Notifications.Single());
    }

    [Fact]
    public void SiteLock_NameFileAndPlayerAddress()
    {
        string path = Path.Combine(Path.GetTempPath(), $"names-{Guid.NewGuid():N}.cnf");
        try
        {
            File.WriteAllText(path, "# keep\nGuest\n");
            var table = new SiteLockTable();
            var names = new NameLockFile(path);
            string logPath = Path.Combine(Path.GetTempPath(), $"conn-{Guid.NewGuid():N}.db");
            var log = new ConnectionLogStore();
            log.OpenFile(logPath);
            try
            {
                log.RecordConnect(_ada, "Ada", "10.2.2.2", "ada.example", 1);
                var handler = new SiteLockCommandHandler(table, log, names);
                var dispatcher = new CommandDispatcher();
                dispatcher.Register(handler, "@sitelock");

                var locked = dispatcher.Dispatch(_god, "@sitelock/name Wizard", _db);
                Assert.Equal("Name locked.", locked.Notifications.Single());
                Assert.Contains("Wizard", File.ReadAllLines(path));
                Assert.Contains("# keep", File.ReadAllLines(path));

                var deleted = dispatcher.Dispatch(_god, "@sitelock/name !Guest", _db);
                Assert.Equal("Name deleted.", deleted.Notifications.Single());
                Assert.DoesNotContain("Guest", File.ReadAllLines(path));
                Assert.Contains("# keep", File.ReadAllLines(path));

                var missing = dispatcher.Dispatch(_god, "@sitelock/name !Nobody", _db);
                Assert.Equal("Unable to delete name.", missing.Notifications.Single());

                var banned = dispatcher.Dispatch(_god, "@sitelock/player Ada", _db);
                Assert.Contains("Site 10.2.2.2 banned", banned.Notifications);
                Assert.False(table.Can("10.2.2.2", SiteLockTable.Connect));
            }
            finally
            {
                log.Close();
                if (File.Exists(logPath))
                    File.Delete(logPath);
            }
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private CommandContext Context(string name, string arguments, params string[] switches)
    {
        var set = new HashSet<string>(switches, StringComparer.OrdinalIgnoreCase);
        return new CommandContext
        {
            Executor = _ada,
            Enactor = _ada,
            CommandName = name,
            Arguments = arguments,
            LeftArg = arguments,
            Switches = set
        };
    }

    private static IEnumerable<string> Messages(CommandContext context) =>
        context.TargetedNotifications.Select(note => note.Message);
}
