namespace PennMUSH.Server.Engine;

using System;
using System.Collections.Concurrent;
using System.IO;
using PennMUSH.Core.Flags;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Communication.Mail;
using PennMUSH.Core.Configuration;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Help;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.MUSHcode.Functions;
using PennMUSH.Core.Protocol;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Scheduling;
using PennMUSH.Core.Types;
using PennMUSH.Server.Commands;
using PennMUSH.Server.Extensions;
using PennMUSH.Server.Session;

public class MUSHServer
{
    private readonly ConcurrentDictionary<string, MUSHConnection> _connections = new();
    private readonly ConcurrentDictionary<Dbref, string> _playerPasswords = new();
    private readonly CancellationTokenSource _cts = new();

    public MushConfiguration Config { get; }
    public string GameDirectory { get; }

    public ObjectDatabase Database { get; }
    public SqliteStore Storage { get; }
    public ActionQueue Queue { get; }
    public CronScheduler Cron { get; }
    public ChannelRegistry Channels { get; }
    public MailStore Mail { get; }
    public HelpStore Help { get; } = new();
    public EditorSessionManager EditorSessions { get; } = new();
    private readonly byte[] _tokenSecret = Guid.NewGuid().ToByteArray();
    public FunctionRegistry Functions { get; }
    public ExpressionEvaluator Evaluator { get; }
    public CommandDispatcher Dispatcher { get; }
    public ExtensionManager Extensions { get; }

    public IReadOnlyCollection<MUSHConnection> Connections => _connections.Values.ToList();

    public MUSHServer(string dbPath = "mush.db", string? configPath = null)
    {
        string? resolvedConfig = ResolveConfigPath(configPath);
        if (resolvedConfig != null && File.Exists(resolvedConfig))
        {
            Config = MushConfigParser.ParseFile(resolvedConfig);
            GameDirectory = Path.GetDirectoryName(Path.GetFullPath(resolvedConfig)) ?? Directory.GetCurrentDirectory();
        }
        else
        {
            Config = new MushConfiguration();
            GameDirectory = Directory.GetCurrentDirectory();
        }

        Database = new ObjectDatabase();
        Storage = new SqliteStore($"Data Source={dbPath}");
        Queue = new ActionQueue();
        Cron = new CronScheduler();
        Channels = new ChannelRegistry();
        Mail = new MailStore();

        bool loaded = Storage.Load(Database, Cron);
        if (!loaded || Database.Count == 0)
        {
            WorldBootstrapper.GenerateBaseWorld(Database);
            Storage.Save(Database, Cron);
        }

        // Ensure default Public channel
        if (Channels.GetChannel("Public") == null)
        {
            Channels.CreateChannel("Public", new Dbref(1), ChannelFlags.Public | ChannelFlags.Player, "General public discussion channel");
        }

        Functions = new FunctionRegistry(Database);
        Functions.NotificationDispatcher = DispatchNotification;
        Functions.EditorSessions = EditorSessions;
        Functions.Scheduler = Cron;
        Functions.Queue = Queue;
        Evaluator = new ExpressionEvaluator(Functions.Dispatch, Functions.Database);
        Functions.Evaluator = Evaluator;

        AsyncFunctions.Initialize(Queue);
        CronFunctions.Initialize(Cron);
        ClientFunctions.Initialize(DispatchNotification, EditorSessions);

        string hlpDir = Path.Combine(GameDirectory, "txt", "hlp");
        if (Directory.Exists(hlpDir))
        {
            Help.LoadFromDirectory(hlpDir);
        }

        Dispatcher = new CommandDispatcher(Evaluator);
        Extensions = new ExtensionManager(this);
        RegisterCommands();

        // Scan and initialize external plugins
        var pluginCandidates = new[]
        {
            Path.Combine(GameDirectory, "plugins"),
            Path.Combine(AppContext.BaseDirectory, "plugins"),
            Path.Combine(Directory.GetCurrentDirectory(), "plugins"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "plugins")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "plugins")),
            @"C:\Repos\pennmush-dotnet\plugins"
        };
        Extensions.ScanAndLoadPlugins(pluginCandidates);

        // Start queue tick background task
        _ = Task.Run(() => QueueTickLoopAsync(_cts.Token));
    }

    private static string? ResolveConfigPath(string? configPath)
    {
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            return Path.GetFullPath(configPath);
        }

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "game", "mush.cnf"),
            Path.Combine(AppContext.BaseDirectory, "mush.cnf"),
            Path.Combine(Directory.GetCurrentDirectory(), "game", "mush.cnf"),
            Path.Combine(Directory.GetCurrentDirectory(), "mush.cnf"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "mush.cnf"))
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private void RegisterCommands()
    {
        Dispatcher.Register(new SayCommandHandler(Evaluator), "say", "\"", "'");
        Dispatcher.Register(new PoseCommandHandler(), "pose", ":", ";");
        Dispatcher.Register(new LookCommandHandler(Evaluator), "look", "l", "read");
        Dispatcher.Register(new GoCommandHandler(), "go", "goto", "move");
        Dispatcher.Register(new DigCommandHandler(), "@dig");
        Dispatcher.Register(new OpenCommandHandler(), "@open");
        Dispatcher.Register(new LinkCommandHandler(), "@link");
        Dispatcher.Register(new UnlinkCommandHandler(), "@unlink");
        Dispatcher.Register(new CreateCommandHandler(), "@create");
        Dispatcher.Register(new SetCommandHandler(), "@set");
        Dispatcher.Register(new PowerCommandHandler(), "@power");
        Dispatcher.Register(new NameCommandHandler(), "@name");
        Dispatcher.Register(new ParentCommandHandler(), "@parent");
        Dispatcher.Register(new ZoneCommandHandler(), "@zone");
        Dispatcher.Register(new WaitCommandHandler(Queue), "@wait", "@notify");
        Dispatcher.Register(new HaltCommandHandler(Queue), "@halt");
        Dispatcher.Register(new AllHaltCommandHandler(Queue), "@allhalt");
        Dispatcher.Register(new ThinkCommandHandler(Evaluator), "think", "th");
        Dispatcher.Register(new TeleportCommandHandler(), "@teleport", "@tel");
        Dispatcher.Register(new GetCommandHandler(), "get", "take", "g");
        Dispatcher.Register(new DropCommandHandler(), "drop", "throw", "d");
        Dispatcher.Register(new InventoryCommandHandler(), "inventory", "i", "inv");
        Dispatcher.Register(new ChannelCommandHandler(Channels), "@channel");
        Dispatcher.Register(new ChatCommandHandler(Channels), "@chat", "+c");
        Dispatcher.Register(new CemitCommandHandler(Channels), "@cemit");
        Dispatcher.Register(new MailCommandHandler(Mail), "@mail");
        Dispatcher.Register(new CronCommandHandler(Cron, Queue), "@cron");
        Dispatcher.Register(new HelpCommandHandler(Help), "help", "?");
        Dispatcher.Register(new EmitCommandHandler(), "@emit", "\\");
        Dispatcher.Register(new PemitCommandHandler(), "@pemit");
        Dispatcher.Register(new RemitCommandHandler(), "@remit");
        Dispatcher.Register(new OemitCommandHandler(), "@oemit");
        Dispatcher.Register(new WallCommandHandler(), "@wall", "@wizwall", "@rwall");
        Dispatcher.Register(new WhisperCommandHandler(), "whisper", "w");
        Dispatcher.Register(new PageCommandHandler(), "page", "p");

        // Spatial & Interaction
        Dispatcher.Register(new HomeCommandHandler(new LookCommandHandler(Evaluator)), "home");
        Dispatcher.Register(new EnterCommandHandler(), "enter");
        Dispatcher.Register(new LeaveCommandHandler(), "leave");
        Dispatcher.Register(new GiveCommandHandler(), "give");
        Dispatcher.Register(new ExamineCommandHandler(), "examine", "ex", "x");

        // Scripting & Flow Control
        Dispatcher.Register(new SwitchCommandHandler(Dispatcher, Evaluator), "@switch", "@sw");
        Dispatcher.Register(new DolistCommandHandler(Dispatcher, Evaluator), "@dolist");
        Dispatcher.Register(new TriggerCommandHandler(Dispatcher, Evaluator), "@trigger", "@tr");
        Dispatcher.Register(new ForceCommandHandler(Dispatcher), "@force");
        Dispatcher.Register(new IfElseCommandHandler(Dispatcher, Evaluator), "@ifelse", "@if");
        Dispatcher.Register(new CommentCommandHandler(), "@@");

        // Object Lifecycle & Admin
        Dispatcher.Register(new DestroyCommandHandler(), "@destroy", "@recycle");
        Dispatcher.Register(new CloneCommandHandler(), "@clone");
        Dispatcher.Register(new ChownCommandHandler(), "@chown");
        Dispatcher.Register(new WipeCommandHandler(), "@wipe");
        Dispatcher.Register(new LockCommandHandler(), "@lock");
        Dispatcher.Register(new UnlockCommandHandler(), "@unlock");
        Dispatcher.Register(new StatsCommandHandler(), "@stats");
        Dispatcher.Register(new ScoreCommandHandler(), "score");
        Dispatcher.Register(new DoingCommandHandler(), "doing", "@poll");
        Dispatcher.Register(new VersionCommandHandler(), "@version");
        Dispatcher.Register(new ClientCommandHandler(EditorSessions), "@client");
        Dispatcher.Register(new PluginCommandHandler(Extensions), "@plugin", "@extension");
        Dispatcher.Register(new RebootCommandHandler(() => OnRebootRequested?.Invoke()), "@reboot");

        // Apply aliases & reserved aliases from configuration
        foreach (var (alias, orig) in Config.CommandAliases)
        {
            Dispatcher.AddAlias(orig, alias);
        }

        foreach (var reserved in Config.ReservedAliases)
        {
            Dispatcher.ReserveAlias(reserved);
        }

        foreach (var (alias, orig) in Config.FunctionAliases)
        {
            Functions.AddAlias(orig, alias);
        }

        foreach (var (cmd, rules) in Config.CommandRestrictions)
        {
            foreach (var rule in rules)
            {
                Dispatcher.AddRestriction(cmd, rule.Restriction, rule.ErrorMessage);
            }
        }
    }

    public event Action? OnRebootRequested;

    public void RegisterConnection(MUSHConnection conn, bool isReconnect = false)
    {
        _connections[conn.Id] = conn;

        // If player is already authenticated or re-registering across a hot-reboot, do not re-show login screen
        if (isReconnect || conn.PlayerId.IsValid)
        {
            return;
        }

        // Display connect screen
        string connectPath = Path.Combine(GameDirectory, Config.ConnectFile);
        if (File.Exists(connectPath))
        {
            _ = conn.SendRawAsync(File.ReadAllText(connectPath));
        }
        else
        {
            _ = conn.SendLineAsync($"=== Welcome to {Config.MudName} ===");
            _ = conn.SendLineAsync("Valid commands: connect <name> <password>, create <name> <password>, WHO, QUIT");
        }
    }

    public void UnregisterConnection(MUSHConnection conn)
    {
        _connections.TryRemove(conn.Id, out _);
        if (conn.PlayerId.IsValid)
        {
            var player = Database.GetObject(conn.PlayerId);
            if (player != null && player.Location.IsValid)
            {
                BroadcastToRoom(player.Location, $"{player.Name} has disconnected.", conn.PlayerId);
            }
        }
    }

    public async Task HandleInputAsync(MUSHConnection conn, string input, CancellationToken ct = default)
    {
        string trimmed = input.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        // Check for JSON protocol envelope (e.g. from MUjs)
        if (trimmed.StartsWith('{') && ProtocolSerializer.TryParseType(trimmed, out var protoType, out var protoId))
        {
            await HandleProtocolEnvelopeAsync(conn, protoType!, protoId, trimmed, ct);
            return;
        }

        // If not logged in, handle login commands
        if (!conn.PlayerId.IsValid)
        {
            await HandleLoginAsync(conn, trimmed, ct);
            return;
        }

        if (trimmed.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
        {
            string quitPath = Path.Combine(GameDirectory, Config.QuitFile);
            if (File.Exists(quitPath))
            {
                await conn.SendRawAsync(File.ReadAllText(quitPath), ct);
            }
            else
            {
                await conn.SendLineAsync("Goodbye!", ct);
            }
            conn.Close();
            return;
        }

        if (trimmed.Equals("WHO", StringComparison.OrdinalIgnoreCase))
        {
            await ShowWhoAsync(conn, ct);
            return;
        }

        // Dispatch in-game command
        var context = Dispatcher.Dispatch(conn.PlayerId, trimmed, Database);
        foreach (var note in context.TargetedNotifications)
        {
            DispatchNotification(note);
        }

        // Auto-save periodically or after object state changes
        if (context.CommandName.StartsWith('@'))
        {
            Storage.Save(Database, Cron);
        }
    }

    private async Task HandleLoginAsync(MUSHConnection conn, string input, CancellationToken ct)
    {
        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts[0].ToUpperInvariant();

        if (cmd == "QUIT")
        {
            string quitPath = Path.Combine(GameDirectory, Config.QuitFile);
            if (File.Exists(quitPath))
            {
                await conn.SendRawAsync(File.ReadAllText(quitPath), ct);
            }
            else
            {
                await conn.SendLineAsync("Goodbye!", ct);
            }
            conn.Close();
            return;
        }

        if (cmd == "WHO")
        {
            await ShowWhoAsync(conn, ct);
            return;
        }

        if (cmd == "CONNECT" && parts.Length >= 2)
        {
            string name = parts[1];
            string password = parts.Length > 2 ? parts[2] : "";

            var player = Database.GetAllObjects().FirstOrDefault(o =>
                o.Type == ObjectType.Player && string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase));

            if (player != null)
            {
                // Verify password (default "one" for God, or empty for development)
                if (_playerPasswords.TryGetValue(player.Id, out var pass) && pass != password)
                {
                    await conn.SendLineAsync("Incorrect password.", ct);
                    return;
                }

                conn.PlayerId = player.Id;
                await conn.SendLineAsync($"Welcome back to {Config.MudName}, {player.Name}!", ct);

                // Send MOTD
                string motdPath = Path.Combine(GameDirectory, Config.MotdFile);
                if (File.Exists(motdPath))
                {
                    await conn.SendRawAsync(File.ReadAllText(motdPath), ct);
                }

                // If Wizard or God, send Wizard MOTD
                if (player.IsWizard || player.IsGod)
                {
                    string wizmotdPath = Path.Combine(GameDirectory, Config.WizmotdFile);
                    if (File.Exists(wizmotdPath))
                    {
                        await conn.SendRawAsync(File.ReadAllText(wizmotdPath), ct);
                    }
                }

                // Auto-join Public channel
                var pubCh = Channels.GetChannel("Public");
                pubCh?.AddMember(player.Id);

                // Check unread mail
                var stats = Mail.GetMailStats(player.Id);
                if (stats.UnreadCount > 0)
                {
                    await conn.SendLineAsync($"You have {stats.UnreadCount} unread mail message(s). Type '@mail' to read.", ct);
                }

                // Show room
                Dispatcher.Dispatch(player.Id, "look", Database);
                BroadcastToRoom(player.Location, $"{player.Name} has connected.", player.Id);
                return;
            }

            await conn.SendLineAsync("Either that player does not exist, or has a different password.", ct);
            return;
        }

        if (cmd == "CREATE" && parts.Length >= 3)
        {
            string name = parts[1];
            string password = parts[2];

            bool exists = Database.GetAllObjects().Any(o =>
                o.Type == ObjectType.Player && string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                await conn.SendLineAsync("That name is already in use.", ct);
                return;
            }

            var newPlayer = Database.CreateObject(name, ObjectType.Player, Dbref.Nothing);
            newPlayer.Owner = newPlayer.Id;
            newPlayer.Home = Config.DefaultHome;
            newPlayer.Pennies = Config.StartingMoney;
            _playerPasswords[newPlayer.Id] = password;
            Database.MoveObject(newPlayer.Id, Config.PlayerStart);

            Storage.Save(Database);

            conn.PlayerId = newPlayer.Id;
            await conn.SendLineAsync($"Character {name} created! Welcome to {Config.MudName}.", ct);

            // Send New User greeting
            string newuserPath = Path.Combine(GameDirectory, Config.NewuserFile);
            if (File.Exists(newuserPath))
            {
                await conn.SendRawAsync(File.ReadAllText(newuserPath), ct);
            }

            var pubCh = Channels.GetChannel("Public");
            pubCh?.AddMember(newPlayer.Id);

            Dispatcher.Dispatch(newPlayer.Id, "look", Database);
            BroadcastToRoom(newPlayer.Location, $"{newPlayer.Name} has connected.", newPlayer.Id);
            return;
        }

        await conn.SendLineAsync("Valid login commands: connect <name> <password>, create <name> <password>, WHO, QUIT", ct);
    }

    private async Task ShowWhoAsync(MUSHConnection conn, CancellationToken ct)
    {
        var active = _connections.Values.Where(c => c.PlayerId.IsValid).ToList();
        await conn.SendLineAsync("Player Name          Location   Connected", ct);
        await conn.SendLineAsync("----------------------------------------------------------------", ct);
        foreach (var c in active)
        {
            var p = Database.GetObject(c.PlayerId);
            if (p != null)
            {
                var loc = Database.GetObject(p.Location);
                string locName = loc != null ? loc.Name : p.Location.ToString();
                string duration = FormatDuration(DateTimeOffset.UtcNow - c.ConnectedAt);
                await conn.SendLineAsync($"{p.Name,-20} {locName,-10} {duration}", ct);
            }
        }
        await conn.SendLineAsync($"Total online: {active.Count}", ct);
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }

    public void DispatchNotification(Notification notification)
    {
        switch (notification.Scope)
        {
            case NotificationScope.Self:
            case NotificationScope.Target:
                if (notification.Target.IsValid)
                {
                    SendToPlayer(notification.Target, notification.Message, notification.ProtocolType, notification.ProtocolPayload);
                }
                break;

            case NotificationScope.Room:
                if (notification.Target.IsValid)
                {
                    SendToRoom(notification.Target, notification.Message);
                }
                break;

            case NotificationScope.RoomExcept:
                if (notification.Target.IsValid)
                {
                    SendToRoom(notification.Target, notification.Message, notification.Except);
                }
                break;

            case NotificationScope.Broadcast:
                SendToAll(notification.Message);
                break;

            case NotificationScope.Wizards:
                SendToWizards(notification.Message);
                break;
        }
    }

    public void SendToPlayer(Dbref player, string message, string? protocolType = null, object? protocolPayload = null)
    {
        foreach (var c in _connections.Values)
        {
            if (c.PlayerId == player)
            {
                if (protocolType != null && protocolPayload != null && c.ProtocolMode == ClientProtocolMode.MujsWebSocket)
                {
                    _ = c.SendEnvelopeAsync(protocolType, protocolPayload);
                }
                else
                {
                    _ = c.SendLineAsync(message);
                }
            }
        }
    }

    private async Task HandleProtocolEnvelopeAsync(MUSHConnection conn, string type, string? id, string json, CancellationToken ct)
    {
        switch (type.ToLowerInvariant())
        {
            case "client.hello":
                var hello = ProtocolSerializer.DeserializePayload<ClientHelloPayload>(json);
                if (hello != null)
                {
                    conn.ProtocolMode = ClientProtocolMode.MujsWebSocket;
                    foreach (var cap in hello.Capabilities)
                    {
                        conn.ClientCapabilities.Add(cap);
                    }

                    string token = Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData(
                        _tokenSecret,
                        System.Text.Encoding.UTF8.GetBytes(conn.Id + ":" + DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                    ));
                    conn.SessionToken = token;

                    var features = new ServerFeatures(
                        MaxWindowCount: 8,
                        SupportedLanguages: new[] { "mushcode", "csharp", "moocode" },
                        AssetBaseUrl: !string.IsNullOrEmpty(Config.MudUrl) ? Config.MudUrl : "http://localhost:5000"
                    );

                    var serverHello = new ServerHelloPayload(
                        Server: "pennmush-dotnet",
                        Version: "1.0.0",
                        SessionToken: token,
                        Features: features
                    );

                    await conn.SendEnvelopeAsync("server.hello", serverHello, id, ct);
                }
                break;

            case "terminal.input":
                var terminalInput = ProtocolSerializer.DeserializePayload<TerminalInputPayload>(json);
                if (terminalInput != null && !string.IsNullOrEmpty(terminalInput.Command))
                {
                    await HandleInputAsync(conn, terminalInput.Command, ct);
                }
                break;

            case "editor.save":
                var save = ProtocolSerializer.DeserializePayload<EditorSavePayload>(json);
                if (save != null)
                {
                    var result = EditorSessions.SaveSession(save.SessionId, save.Code, Database, conn.PlayerId);
                    await conn.SendEnvelopeAsync("editor.result", result, id, ct);
                    if (result.Success)
                    {
                        Storage.Save(Database, Cron);
                    }
                }
                break;

            case "editor.close":
                var close = ProtocolSerializer.DeserializePayload<EditorSavePayload>(json);
                if (close != null)
                {
                    EditorSessions.CloseSession(close.SessionId);
                }
                break;

            default:
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("payload", out var payload))
                    {
                        await Extensions.TryDispatchProtocolEnvelopeAsync(conn.PlayerId, type, payload);
                    }
                }
                catch
                {
                    // Ignore malformed custom envelopes
                }
                break;
        }
    }

    public void SendToRoom(Dbref roomRef, string message, Dbref excludePlayerId = default)
    {
        foreach (var c in _connections.Values)
        {
            if (c.PlayerId.IsValid && (!excludePlayerId.IsValid || c.PlayerId != excludePlayerId))
            {
                var p = Database.GetObject(c.PlayerId);
                if (p != null && p.Location == roomRef)
                {
                    _ = c.SendLineAsync(message);
                }
            }
        }
    }

    public void SendToAll(string message)
    {
        foreach (var c in _connections.Values)
        {
            if (c.PlayerId.IsValid)
            {
                _ = c.SendLineAsync(message);
            }
        }
    }

    public void SendToWizards(string message)
    {
        foreach (var c in _connections.Values)
        {
            if (c.PlayerId.IsValid)
            {
                var p = Database.GetObject(c.PlayerId);
                if (p != null && (p.IsWizard || p.IsGod))
                {
                    _ = c.SendLineAsync(message);
                }
            }
        }
    }

    public void BroadcastToRoom(Dbref roomRef, string message, Dbref excludePlayerId = default)
    {
        SendToRoom(roomRef, message, excludePlayerId);
    }

    private async Task QueueTickLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                Cron.ProcessDueJobs(DateTimeOffset.UtcNow, Queue);
                var readyContexts = Queue.ProcessReady(DateTimeOffset.UtcNow, Dispatcher, Database);
                foreach (var ctx in readyContexts)
                {
                    foreach (var note in ctx.TargetedNotifications)
                    {
                        DispatchNotification(note);
                    }
                }
            }
            catch
            {
                // Suppress background tick errors
            }

            await Task.Delay(100, ct);
        }
    }

    /// <summary>
    /// Flushes database state to storage and shuts down extensions cleanly for a hot-reboot,
    /// detaching connection references WITHOUT closing the underlying player sockets.
    /// </summary>
    public void PrepareForReboot()
    {
        try { _cts.Cancel(); } catch { }
        Storage.Save(Database, Cron);
        Extensions.Dispose();
        _connections.Clear();
    }

    public void Stop()
    {
        try { _cts.Cancel(); } catch { }
        Storage.Save(Database, Cron);
        Extensions.Dispose();
        foreach (var conn in _connections.Values)
        {
            conn.Close();
        }
    }
}
