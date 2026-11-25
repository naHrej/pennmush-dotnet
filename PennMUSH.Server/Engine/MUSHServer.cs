namespace PennMUSH.Server.Engine;

using System;
using System.Collections.Concurrent;
using System.IO;
using PennMUSH.Core.Flags;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PennMUSH.Core.Access;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Communication;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Communication.Mail;
using PennMUSH.Core.Configuration;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Help;
using PennMUSH.Core.Http;
using PennMUSH.Core.Logging;
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
    private readonly ConcurrentDictionary<Dbref, Func<string, bool>> _inputCaptures = new();
    private readonly ConnectionDirectory _sessions;
    private readonly RoomSessions _rooms = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly GameMailbox _game = new();
    private readonly object _outboundLock = new();
    private int _stopped;
    private DateTimeOffset _nextAutosave = DateTimeOffset.UtcNow;
    private Task _outbound = Task.CompletedTask;
    private bool _shutdownRequested;
    private bool _quietRebootRequested;

    public MushConfiguration Config { get; }
    public string GameDirectory { get; }

    public ObjectDatabase Database { get; }
    public SqliteStore Storage { get; }
    public RuntimeStateStore RuntimeState { get; }
    public ActionQueue Queue { get; }
    public CronScheduler Cron { get; }
    public ChannelRegistry Channels { get; }
    public MotdBoard Motd { get; } = new();
    public MailStore Mail { get; }
    public PennMUSH.Core.Communication.Mail.MailAliasTable MailAliases { get; }
    public SiteLockTable SiteLocks { get; }
    public WizardLog Logs { get; }
    public SqlDatabase Sql { get; } = new();
    public ConnectionLogStore ConnectionLog { get; } = new();
    public HttpFetch Web { get; } = new();
    public HttpChannel Http { get; } = new();
    public HelpStore Help { get; } = new();
    public TextFileCache TextCache { get; } = new();
    public SuggestionVocabulary Suggestions { get; } = new();
    public EditorSessionManager EditorSessions { get; } = new();
    private readonly byte[] _tokenSecret = Guid.NewGuid().ToByteArray();
    public FunctionRegistry Functions { get; }
    public ExpressionEvaluator Evaluator { get; }
    public CommandDispatcher Dispatcher { get; }
    private LookCommandHandler? _roomLook;
    public ExtensionManager Extensions { get; }

    public IReadOnlyCollection<MUSHConnection> Connections => _connections.Values.ToList();

    /// <summary>
    /// When set, the game thread flushes dirty rows about once a second.
    /// <c>@dump</c>, reboot, and shutdown flush immediately either way.
    /// </summary>
    public bool AutosaveEnabled { get; set; } = true;

    public void Run(Action action) => _game.Run(action);

    public T Run<T>(Func<T> action) => _game.Run(action);

    public Task RunAsync(Action action) => _game.RunAsync(action);

    public MUSHServer(string dbPath = "mush.db", string? configPath = null, SiteLockTable? siteLocks = null, string? importFlatfilePath = null)
    {
        _sessions = new ConnectionDirectory(_connections, UnregisterConnection);
        string? resolvedConfig = ResolveConfigPath(configPath);
        if (resolvedConfig != null && File.Exists(resolvedConfig))
        {
            Config = MushConfigParser.ParseFile(resolvedConfig);
            Config.ConfigFilePath = resolvedConfig;
            GameDirectory = Path.GetDirectoryName(Path.GetFullPath(resolvedConfig)) ?? Directory.GetCurrentDirectory();
        }
        else
        {
            Config = new MushConfiguration();
            GameDirectory = Directory.GetCurrentDirectory();
        }

        Database = new ObjectDatabase
        {
            SystemObjectId = Config.SystemObject,
            MasterRoomId = Config.MasterRoom,
            DefaultHomeId = Config.DefaultHome,
            BaseRoomId = Config.BaseRoom,
            PlayerStartId = Config.PlayerStart,
            MoneySingular = Config.MoneySingular,
            MoneyPlural = Config.MoneyPlural,
            EmptyAttrs = Config.EmptyAttrs,
            ReadRemoteDesc = Config.ReadRemoteDesc,
            MaxAttrsPerObj = Config.MaxAttrsPerObj,
            MaxParents = Config.MaxParents,
            MaxDbref = Config.MaxDbref,
            ObjectCost = Config.ObjectCost,
            RoomCost = Config.RoomCost,
            ExitCost = Config.ExitCost,
            LinkCost = Config.LinkCost,
            FindCost = Config.FindCost,
            QueueCost = Config.QueueCost,
            QuotaCost = Config.QuotaCost,
            MaxPennies = Config.MaxPennies,
            PlayerNameLen = Config.PlayerNameLen,
            PlayerNameSpaces = Config.PlayerNameSpaces,
            OnlyAsciiInNames = Config.OnlyAsciiInNames
        };
        Database.PlayerLocationChanged = (player, _, to) => Relocate(player, to);
        string? dbDir = Path.GetDirectoryName(Path.GetFullPath(dbPath));
        if (!string.IsNullOrEmpty(dbDir))
            Directory.CreateDirectory(dbDir);
        Storage = new SqliteStore($"Data Source={dbPath}");
        RuntimeState = new RuntimeStateStore($"Data Source={dbPath}");
        Queue = new ActionQueue { IgnoresQueueLimit = id => _sessions.IgnoresQuota(id) };
        Cron = new CronScheduler();
        Channels = new ChannelRegistry();
        Mail = new MailStore();
        MailAliases = new PennMUSH.Core.Communication.Mail.MailAliasTable();
        SiteLocks = siteLocks ?? new SiteLockTable();
        Cron.Changed = () => Database.Changes.MarkCron();
        Channels.Changed = () => Database.Changes.MarkRuntime();
        Mail.Changed = () => Database.Changes.MarkRuntime();
        MailAliases.Changed = () => Database.Changes.MarkRuntime();
        SiteLocks.Changed = () => Database.Changes.MarkRuntime();
        Logs = new WizardLog(Path.Combine(GameDirectory, "log"));
        Http.Handler = Config.HttpHandler;
        if (!string.IsNullOrWhiteSpace(Config.SqlDatabase))
        {
            string sqlPath = Path.IsPathRooted(Config.SqlDatabase)
                ? Config.SqlDatabase
                : Path.Combine(GameDirectory, Config.SqlDatabase);
            try
            {
                Sql.OpenFile(sqlPath);
            }
            catch (Exception)
            {
                Sql.Close();
            }
        }

        bool loaded = Storage.Load(Database, Cron);
        if (!string.IsNullOrWhiteSpace(importFlatfilePath))
        {
            string resolvedImport = ResolveFlatfilePath(importFlatfilePath)
                ?? throw new FileNotFoundException($"Flatfile database '{importFlatfilePath}' was not found.", importFlatfilePath);
            FlatfileStore.LoadFile(Database, resolvedImport);
            Storage.ReplaceAll(Database, Cron);
        }
        else if (!loaded || Database.Count == 0)
        {
            string? configuredIndb = ResolveFlatfilePath(Config.InputDatabase);
            if (configuredIndb != null)
            {
                FlatfileStore.LoadFile(Database, configuredIndb);
                Storage.ReplaceAll(Database, Cron);
            }
            else
            {
                WorldBootstrapper.GenerateBaseWorld(Database);
                Storage.Save(Database, Cron);
            }
        }

        Functions = new FunctionRegistry(Database);
        Functions.Changed = () => Database.Changes.MarkRuntime();
        Functions.NotificationDispatcher = DispatchNotification;
        Functions.EditorSessions = EditorSessions;
        Functions.Scheduler = Cron;
        Functions.Queue = Queue;
        Functions.SqlDatabase = Sql;
        ConnectionLog.OpenFile(Path.Combine(GameDirectory, "connlog.db"));
        Functions.ConnectionLog = ConnectionLog;
        Database.Changes.Suspend();
        try
        {
            RuntimeState.Load(Channels, Mail, MailAliases, Functions, SiteLocks);
        }
        finally
        {
            Database.Changes.Clear();
            Database.Changes.Resume();
        }

        if (Channels.GetChannel("Public") == null)
        {
            Channels.CreateChannel("Public", new Dbref(1), ChannelFlags.Public | ChannelFlags.Player, "General public discussion channel");
        }
        Functions.Passwords = new CallbackPasswords((id, password) =>
        {
            var player = Database.GetObject(id);
            if (player != null)
                PasswordHash.Store(player, password);
        });
        Functions.PlayerStart = Config.PlayerStart;
        Functions.PlayerStartProvider = () => Config.PlayerStart;
        Functions.DefaultHome = Config.DefaultHome;
        Functions.DefaultHomeProvider = () => Config.DefaultHome;
        Functions.StartingMoney = Config.StartingMoney;
        Functions.StartingMoneyProvider = () => Config.StartingMoney;
        Functions.UseQuota = Config.UseQuota;
        Functions.UseQuotaProvider = () => Config.UseQuota;
        Functions.StartingQuota = Config.StartingQuota;
        Functions.StartingQuotaProvider = () => Config.StartingQuota;
        Functions.UnsafeEvalFunctions = Config.UnsafeEvalFunctions;
        Functions.UnsafeEvalFunctionsProvider = () => Config.UnsafeEvalFunctions;
        Functions.SaferUfun = Config.SaferUfun;
        Functions.SaferUfunProvider = () => Config.SaferUfun;
        Functions.SideEffectsEnabled = Config.FunctionSideEffects;
        Functions.SideEffectsEnabledProvider = () => Config.FunctionSideEffects;
        Functions.FunctionInvocationLimit = Config.FunctionInvocationLimit;
        Functions.FunctionInvocationLimitProvider = () => Config.FunctionInvocationLimit;
        Functions.FunctionRecursionLimit = Config.FunctionRecursionLimit;
        Functions.FunctionRecursionLimitProvider = () => Config.FunctionRecursionLimit;
        Functions.TinyBooleans = Config.TinyBooleans;
        Functions.TinyBooleansProvider = () => Config.TinyBooleans;
        Functions.TinyMath = Config.TinyMath;
        Functions.TinyMathProvider = () => Config.TinyMath;
        Functions.NullEqZero = Config.NullEqZero;
        Functions.NullEqZeroProvider = () => Config.NullEqZero;
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

        TextCache.ConfigureFromGameDirectory(GameDirectory, Config);
        SyncSuggestionsFromCache();
        Functions.TextCache = TextCache;
        Functions.Suggestions = Suggestions;
        Functions.ConnectionsProvider = BuildConnectionSnapshots;
        Functions.ConfigProvider = key =>
            PennMUSH.Core.Configuration.ConfigCatalog.Find(key)?.Read(Config)
            ?? (Config.Directives.TryGetValue(key, out var val) ? val : null);

        Dispatcher = new CommandDispatcher(Evaluator);
        Extensions = new ExtensionManager(this);
        RegisterCommands();
        Functions.CommandNamesProvider = () => Dispatcher.Table.PrimaryNames;

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

    private void SyncSuggestionsFromCache()
    {
        foreach (string category in TextCache.Categories)
        {
            if (TextCache.IsAdminOnly(category))
                continue;
            Suggestions.ReplaceCachedCategory(category, TextCache.GetEntries(category, isAdmin: false));
        }

        if (!string.IsNullOrWhiteSpace(Config.DictFile))
        {
            string dictPath = Path.IsPathRooted(Config.DictFile)
                ? Config.DictFile
                : Path.Combine(GameDirectory, Config.DictFile);
            Suggestions.LoadCachedWordFile("WORDS", dictPath);
        }
    }

    private static readonly (string CnfFile, string DstFile)[] DefaultConfigTemplates =
    [
        ("mush.cnf", "mushcnf.dst"),
        ("alias.cnf", "aliascnf.dst"),
        ("names.cnf", "namescnf.dst"),
        ("restrict.cnf", "restrictcnf.dst"),
    ];

    public static void EnsureDefaultConfigFiles(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return;

        foreach (var (cnfFile, dstFile) in DefaultConfigTemplates)
        {
            string cnfPath = Path.Combine(directory, cnfFile);
            string dstPath = Path.Combine(directory, dstFile);
            if (!File.Exists(cnfPath) && File.Exists(dstPath))
                File.Copy(dstPath, cnfPath);
        }
    }

    private static string? ResolveConfigPath(string? configPath)
    {
        if (!string.IsNullOrEmpty(configPath))
        {
            string fullPath = Path.GetFullPath(configPath);
            EnsureDefaultConfigFiles(Path.GetDirectoryName(fullPath));
            if (File.Exists(fullPath))
                return fullPath;
        }

        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "game", "mush.cnf")),
            Path.Combine(Directory.GetCurrentDirectory(), "game", "mush.cnf"),
            Path.Combine(Directory.GetCurrentDirectory(), "mush.cnf"),
            Path.Combine(AppContext.BaseDirectory, "game", "mush.cnf"),
            Path.Combine(AppContext.BaseDirectory, "mush.cnf")
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                EnsureDefaultConfigFiles(Path.GetDirectoryName(path));
                return path;
            }
        }

        foreach (var path in candidates)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, "mushcnf.dst")))
            {
                EnsureDefaultConfigFiles(dir);
                if (File.Exists(path))
                    return path;
            }
        }

        return null;
    }

    /// <summary>
    /// Imports a PennMUSH flatfile (plain or gzip-compressed) into the live database and persists it to SQLite.
    /// </summary>
    public void ImportFlatfile(string flatfilePath)
    {
        string resolved = ResolveFlatfilePath(flatfilePath)
            ?? throw new FileNotFoundException($"Flatfile database '{flatfilePath}' was not found.", flatfilePath);
        FlatfileStore.LoadFile(Database, resolved);
        Storage.ReplaceAll(Database, Cron);
    }

    /// <summary>
    /// Imports a PennMUSH flatfile from a <see cref="TextReader"/> into the live database and persists it to SQLite.
    /// </summary>
    public void ImportFlatfile(TextReader reader)
    {
        FlatfileStore.Load(Database, reader);
        Storage.ReplaceAll(Database, Cron);
    }

    /// <summary>
    /// Sets (or resets) the password for a player object and persists it to SQLite.
    /// </summary>
    public bool SeedPlayerPassword(Dbref playerId, string password)
    {
        var player = Database.GetObject(playerId);
        if (player == null || player.Type != ObjectType.Player)
            return false;

        PasswordHash.Store(player, password);
        Storage.Save(Database, Cron);
        return true;
    }

    private string? ResolveFlatfilePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var candidates = new List<string>();
        if (Path.IsPathRooted(path))
        {
            candidates.Add(path);
            if (!path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                candidates.Add(path + ".gz");
        }
        else
        {
            candidates.Add(Path.GetFullPath(path));
            if (!path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                candidates.Add(Path.GetFullPath(path + ".gz"));

            string gameRelative = Path.Combine(GameDirectory, path);
            candidates.Add(gameRelative);
            if (!path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                candidates.Add(gameRelative + ".gz");

            string repoRelative = Path.GetFullPath(Path.Combine(GameDirectory, "..", path));
            candidates.Add(repoRelative);
            if (!path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                candidates.Add(repoRelative + ".gz");
        }

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private bool PlayerReceivesHtml(Dbref player)
    {
        foreach (var conn in _connections.Values)
        {
            if (conn.PlayerId == player && conn.Capabilities.SupportsHtml)
                return true;
        }

        return false;
    }

    private void RegisterCommands()
    {
        var look = new LookCommandHandler(Evaluator);
        _roomLook = look;
        var services = new CommandServices
        {
            Dispatcher = Dispatcher,
            Evaluator = Evaluator,
            Queue = Queue,
            Config = Config,
            Channels = Channels,
            Mail = Mail,
            MailAliases = MailAliases,
            Cron = Cron,
            Help = Help,
            Suggestions = Suggestions,
            TextCache = TextCache,
            Motd = Motd,
            Logs = Logs,
            Sql = Sql,
            Web = Web,
            Http = Http,
            Functions = Functions,
            Sessions = _sessions,
            EditorSessions = EditorSessions,
            Extensions = Extensions,
            SiteLocks = SiteLocks,
            ConnectionLog = ConnectionLog,
            GameDirectory = GameDirectory,
            Look = look,
            IsOnline = id => _connections.Values.Any(conn => conn.PlayerId == id),
            PlayerReceivesHtml = PlayerReceivesHtml,
            SaveAll = SaveAll,
            SyncSuggestions = SyncSuggestionsFromCache,
            ScreenLines = () => ScreenFileReport.Lines(GameDirectory, Config),
            Reboot = () => OnRebootRequested?.Invoke(),
            RequestShutdown = () => _shutdownRequested = true,
            RequestQuietReboot = () => _quietRebootRequested = true
        };
        foreach (var module in BuiltinCommandModules.All)
            module.Register(services);
    }

    public event Action? OnRebootRequested;
    public event Action? OnQuietRebootRequested;
    public event Action? OnShutdownRequested;

    public void SaveAll()
    {
        _game.Run(FlushPending);
    }

    private void FlushPending()
    {
        Storage.Flush(Database, Cron, RuntimeState, Channels, Mail, MailAliases, Functions, SiteLocks);
    }

    public void RegisterConnection(MUSHConnection conn, bool isReconnect = false)
    {
        _game.Run(() => RegisterConnectionCore(conn, isReconnect));
    }

    private void RegisterConnectionCore(MUSHConnection conn, bool isReconnect)
    {
        _connections[conn.Id] = conn;
        string host = string.IsNullOrWhiteSpace(conn.Host) ? "localhost" : conn.Host;
        string hostname = string.IsNullOrWhiteSpace(conn.Hostname) ? host : conn.Hostname;
        conn.ConnLogId = ConnectionLog.RecordConnect(
            conn.PlayerId,
            conn.PlayerId.IsValid ? Database.GetObject(conn.PlayerId)?.Name : null,
            host,
            hostname,
            conn.ConnectedAt.ToUnixTimeSeconds(),
            ssl: conn.Capabilities.IsSsl,
            websocket: conn.IsWebSocket);

        // If player is already authenticated or re-registering across a hot-reboot, do not re-show login screen
        if (isReconnect || conn.PlayerId.IsValid)
        {
            var player = Database.GetObject(conn.PlayerId);
            if (player != null)
                _rooms.Place(conn, player.Location);
            return;
        }

        _ = SendConnectScreenAsync(conn);
    }

    private async Task SendConnectScreenAsync(MUSHConnection conn)
    {
        if (conn.IsWebSocket)
        {
            string? css = ScreenStylesheet.Load(GameDirectory);
            if (!string.IsNullOrEmpty(css))
                await conn.SendMarkupAsync("<style id=\"pennmush-screen\">" + css + "</style>");
        }

        if (!await SendScreenAsync(conn, Config.ConnectFile))
        {
            await conn.SendLineAsync($"=== Welcome to {Config.MudName} ===");
            await conn.SendLineAsync("Valid commands: connect <name> <password>, create <name> <password>, WHO, QUIT");
        }
    }

    private async Task<bool> SendScreenAsync(MUSHConnection conn, string configuredPath, CancellationToken ct = default)
    {
        string? path = GameScreenFiles.Resolve(GameDirectory, configuredPath, conn.IsWebSocket);
        if (path == null)
            return false;

        string text = Config.ExpandScreenText(File.ReadAllText(path), conn.IsWebSocket);
        if (conn.IsWebSocket && GameScreenFiles.IsHtml(path))
            await conn.SendMarkupAsync(text, ct);
        else
            await conn.SendRawAsync(text, ct);
        return true;
    }

    public void UnregisterConnection(MUSHConnection conn)
    {
        _game.Run(() => UnregisterConnectionCore(conn));
    }

    private void UnregisterConnectionCore(MUSHConnection conn)
    {
        if (!_connections.TryRemove(conn.Id, out _))
            return;
        _rooms.Remove(conn);
        conn.Close();
        ConnectionLog.RecordDisconnect(conn.ConnLogId, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "quit");
        if (conn.PlayerId.IsValid)
        {
            _inputCaptures.TryRemove(conn.PlayerId, out _);
            Database.SetConnected(conn.PlayerId, false);
            var player = Database.GetObject(conn.PlayerId);
            if (player != null && player.Location.IsValid)
            {
                BroadcastToRoom(player.Location, $"{player.Name} has disconnected.", conn.PlayerId);
            }
        }
    }

    public async Task HandleInputAsync(MUSHConnection conn, string input, CancellationToken ct = default)
    {
        string trimmedAll = input.Trim();
        if (trimmedAll.StartsWith('{') && ProtocolSerializer.TryParseType(trimmedAll, out var protoType, out var protoId))
        {
            conn.NoteInput(trimmedAll.Length);
            await HandleProtocolEnvelopeAsync(conn, protoType!, protoId, trimmedAll, ct);
            return;
        }

        if (input.IndexOfAny(new[] { '\r', '\n' }) >= 0)
        {
            string normalized = input.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
            string[] lines = normalized.Split('\n');
            // Ignore a single trailing empty element produced by a trailing newline when not captured
            int count = lines.Length;
            if (count > 1 && lines[count - 1].Length == 0)
                count--;
            for (int i = 0; i < count; i++)
                await HandleSingleLineAsync(conn, lines[i], ct);
            return;
        }

        await HandleSingleLineAsync(conn, input, ct);
    }

    private async Task HandleSingleLineAsync(MUSHConnection conn, string input, CancellationToken ct)
    {
        string rawLine = input.TrimEnd('\r', '\n');
        string trimmed = rawLine.Trim();
        bool isCaptured = conn.PlayerId.IsValid && _inputCaptures.ContainsKey(conn.PlayerId);
        if (!isCaptured && string.IsNullOrEmpty(trimmed)) return;

        conn.NoteInput(rawLine.Length);

        if (trimmed.StartsWith('{') && ProtocolSerializer.TryParseType(trimmed, out var protoType, out var protoId))
        {
            await HandleProtocolEnvelopeAsync(conn, protoType!, protoId, trimmed, ct);
            return;
        }

        if (!conn.PlayerId.IsValid)
        {
            await HandleLoginAsync(conn, trimmed, ct);
            return;
        }

        if (trimmed.Equals("QUIT", StringComparison.OrdinalIgnoreCase))
        {
            if (!await SendScreenAsync(conn, Config.QuitFile, ct))
                await conn.SendLineAsync("Goodbye!", ct);
            conn.Close();
            await _game.RunAsync(() => ConnectionLog.RecordDisconnect(conn.ConnLogId, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "quit"));
            return;
        }

        if (!string.IsNullOrEmpty(conn.OutputPrefix))
            await conn.SendLineAsync(conn.OutputPrefix, ct);

        var outcome = await _game.RunAsync(() => DispatchCommand(conn, trimmed, rawLine));
        await FlushOutbound();

        if (!string.IsNullOrEmpty(conn.OutputSuffix))
            await conn.SendLineAsync(conn.OutputSuffix, ct);

        if (outcome.Shutdown || outcome.QuietReboot)
        {
            await FlushOutbound();
            await _game.RunAsync(() =>
            {
                if (outcome.QuietReboot)
                    OnQuietRebootRequested?.Invoke();
                else
                    OnShutdownRequested?.Invoke();
            });
        }
    }

    private readonly record struct CommandOutcome(bool Shutdown, bool QuietReboot);

    public void CapturePlayerInput(Dbref player, Func<string, bool> onLine)
    {
        ArgumentNullException.ThrowIfNull(onLine);
        _inputCaptures[player] = onLine;
    }

    public void ReleasePlayerInput(Dbref player)
    {
        _inputCaptures.TryRemove(player, out _);
    }

    public bool TryHandleCapturedInput(Dbref player, string line)
    {
        if (!_inputCaptures.TryGetValue(player, out var handler))
            return false;
        if (!handler(line))
            _inputCaptures.TryRemove(new KeyValuePair<Dbref, Func<string, bool>>(player, handler));
        return true;
    }

    private CommandOutcome DispatchCommand(MUSHConnection conn, string trimmed, string? rawLine = null)
    {
        _sessions.SourceDescriptor = conn.Descriptor;
        try
        {
            if (TryHandleCapturedInput(conn.PlayerId, rawLine ?? trimmed))
                return new CommandOutcome(false, false);

            var context = Dispatcher.Dispatch(conn.PlayerId, trimmed, Database);
            bool shutdown = _shutdownRequested;
            bool quietReboot = _quietRebootRequested;
            _shutdownRequested = false;
            _quietRebootRequested = false;
            foreach (var note in context.TargetedNotifications)
                DispatchNotification(note);
            return new CommandOutcome(shutdown, quietReboot);
        }
        finally
        {
            _sessions.SourceDescriptor = null;
            _sessions.FlushDisconnects();
        }
    }

    private void ShowArrival(Dbref player)
    {
        if (_roomLook == null)
            return;

        var context = new CommandContext { Executor = player, Enactor = player };
        _roomLook.ShowHere(context, Database, player, LookKind.Automatic);
        foreach (var note in context.TargetedNotifications)
            DispatchNotification(note);
    }

    private async Task HandleLoginAsync(MUSHConnection conn, string input, CancellationToken ct)
    {
        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string cmd = parts[0].ToUpperInvariant();

        if (cmd == "QUIT")
        {
            if (!await SendScreenAsync(conn, Config.QuitFile, ct))
                await conn.SendLineAsync("Goodbye!", ct);
            conn.Close();
            await _game.RunAsync(() => ConnectionLog.RecordDisconnect(conn.ConnLogId, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "quit"));
            return;
        }

        if (cmd == "WHO")
        {
            var lines = await _game.RunAsync(() => BuildWhoLines());
            foreach (string line in lines)
                await conn.SendLineAsync(line, ct);
            return;
        }

        var script = await _game.RunAsync(() => PlanLogin(conn, parts, cmd));
        foreach (var step in script.Steps)
        {
            if (step.Line != null)
                await conn.SendLineAsync(step.Line, ct);
            else if (step.Screen != null)
                await SendScreenAsync(conn, step.Screen, ct);
            else if (step.Announced)
                await SendAnnouncedMotdAsync(conn, step.Royalty, ct);
        }

        if (script.Arrive)
        {
            await _game.RunAsync(() =>
            {
                ShowArrival(script.Player);
                var player = Database.GetObject(script.Player);
                if (player != null)
                    BroadcastToRoom(player.Location, $"{player.Name} has connected.", player.Id);
            });
            await FlushOutbound();
        }
    }

    private readonly record struct LoginStep(string? Line, string? Screen, bool Announced, bool Royalty);

    private sealed class LoginScript
    {
        public List<LoginStep> Steps { get; } = new();
        public bool Arrive;
        public Dbref Player;
    }

    private LoginScript PlanLogin(MUSHConnection conn, string[] parts, string cmd)
    {
        var script = new LoginScript();
        switch (cmd)
        {
            case "CONNECT" or "CONN" or "CO" when parts.Length >= 2:
                PlanConnect(conn, parts, script);
                break;
            case "CREATE" or "CR" when parts.Length >= 3:
                PlanCreate(conn, parts, script);
                break;
            default:
                script.Steps.Add(new LoginStep("Valid login commands: connect <name> <password>, create <name> <password>, WHO, QUIT", null, false, false));
                break;
        }
        return script;
    }

    private void PlanConnect(MUSHConnection conn, string[] parts, LoginScript script)
    {
        string name = parts[1];
        string password = parts.Length > 2 ? parts[2] : "";
        var player = Database.FindPlayer(name);
        bool autoGuest = false;

        if (player == null && name.Equals("guest", StringComparison.OrdinalIgnoreCase))
        {
            if (!Config.Guests || !SiteLocks.Can(conn.Host, SiteLockTable.Guest))
            {
                script.Steps.Add(new LoginStep("Guest connections not allowed.", null, false, false));
                return;
            }

            int activeGuests = _connections.Values.Count(c =>
                c.PlayerId.IsValid && Database.GetObject(c.PlayerId)?.HasFlag("GUEST") == true);
            if (Config.MaxGuests > 0 && activeGuests >= Config.MaxGuests)
            {
                script.Steps.Add(new LoginStep("Sorry, all guest slots are currently in use.", null, false, false));
                return;
            }

            player = Database.ByType(ObjectType.Player)
                .FirstOrDefault(p => p.HasFlag("GUEST") && !Database.IsConnected(p.Id));
            if (player == null)
            {
                int suffix = 1;
                while (Database.FindPlayer($"Guest{suffix}") != null)
                    suffix++;
                player = Database.CreateObject($"Guest{suffix}", ObjectType.Player, Dbref.Nothing);
                player.Owner = player.Id;
                player.Home = Config.DefaultHome;
                player.Pennies = 0;
                player.AddFlag("GUEST");
                Database.MoveObject(player.Id, Config.PlayerStart);
            }
            autoGuest = true;
        }

        if (player == null)
        {
            script.Steps.Add(new LoginStep("Either that player does not exist, or has a different password.", null, false, false));
            return;
        }

        bool isGuest = player.HasFlag("GUEST");
        if (isGuest
            ? (!Config.Guests || !SiteLocks.Can(conn.Host, SiteLockTable.Guest))
            : !SiteLocks.Can(conn.Host, SiteLockTable.Connect))
        {
            script.Steps.Add(new LoginStep(
                isGuest ? "Guest connections not allowed." : "Player connections not allowed.",
                null, false, false));
            return;
        }

        if (isGuest && !autoGuest && Config.MaxGuests > 0)
        {
            int activeGuests = _connections.Values.Count(c =>
                c.PlayerId.IsValid && Database.GetObject(c.PlayerId)?.HasFlag("GUEST") == true);
            if (activeGuests >= Config.MaxGuests)
            {
                script.Steps.Add(new LoginStep("Sorry, all guest slots are currently in use.", null, false, false));
                return;
            }
        }

        if (!autoGuest && !PasswordHash.Accepts(player, password, memory: null))
        {
            script.Steps.Add(new LoginStep("Either that player does not exist, or has a different password.", null, false, false));
            return;
        }

        if (!Config.Logins && !player.IsWizard)
        {
            script.Steps.Add(new LoginStep("Logins are currently disabled.", null, false, false));
            return;
        }

        if (Config.MaxLogins > 0 && !player.IsWizard && _connections.Values.Count(c => c.PlayerId.IsValid) >= Config.MaxLogins)
        {
            script.Steps.Add(new LoginStep("The game is full; please try again later.", null, false, false));
            return;
        }

        conn.PlayerId = player.Id;
        _rooms.Place(conn, player.Location);
        Database.SetConnected(player.Id, true);
        ConnectionLog.UpdatePlayer(conn.ConnLogId, player.Id, player.Name);
        Channels.GetChannel("Public")?.AddMember(player.Id);

        if (!isGuest && Config.Paycheck > 0)
        {
            string? lastVal = player.GetAttribute("LAST")?.Value;
            DateTimeOffset lastLogin = default;
            bool parsedLast = !string.IsNullOrWhiteSpace(lastVal)
                && (DateTimeOffset.TryParseExact(lastVal.Trim(), "ddd MMM dd HH:mm:ss yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out lastLogin)
                    || DateTimeOffset.TryParse(lastVal, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out lastLogin));
            bool eligible = !parsedLast || lastLogin.UtcDateTime.Date < DateTime.UtcNow.Date;
            if (eligible)
            {
                player.Pennies += Config.Paycheck;
                script.Steps.Add(new LoginStep($"You receive your daily paycheck of {Config.Paycheck} {Database.FormatMoneyUnit(Config.Paycheck)}.", null, false, false));
            }
            player.SetAttribute("LAST", DateTime.UtcNow.ToString("ddd MMM dd HH:mm:ss yyyy", System.Globalization.CultureInfo.InvariantCulture), new Dbref(1), PennMUSH.Core.Attributes.StandardAttributes.DefaultFlags("LAST"), wizardOverride: true);
        }

        script.Steps.Add(new LoginStep($"Welcome back to {Config.MudName}, {player.Name}!", null, false, false));
        script.Steps.Add(new LoginStep(null, Config.MotdFile, false, false));
        if (player.IsRoyalty)
            script.Steps.Add(new LoginStep(null, Config.WizmotdFile, false, false));
        script.Steps.Add(new LoginStep(null, null, true, player.IsRoyalty));
        var stats = Mail.GetMailStats(player.Id);
        if (stats.UnreadCount > 0)
            script.Steps.Add(new LoginStep($"You have {stats.UnreadCount} unread mail message(s). Type '@mail' to read.", null, false, false));
        script.Arrive = true;
        script.Player = player.Id;
    }

    private void PlanCreate(MUSHConnection conn, string[] parts, LoginScript script)
    {
        string name = parts[1];
        string password = parts[2];
        bool exists = Database.FindPlayer(name) != null;
        if (exists)
        {
            script.Steps.Add(new LoginStep("That name is already in use.", null, false, false));
            return;
        }

        if (!Config.PlayerCreation || !SiteLocks.Can(conn.Host, SiteLockTable.Create))
        {
            script.Steps.Add(new LoginStep("Creation is NOT allowed", null, false, false));
            return;
        }

        if (!PlayerCreator.OkPlayerName(name, Database))
        {
            script.Steps.Add(new LoginStep("That is not a valid player name.", null, false, false));
            return;
        }

        if (Config.MaxPlayers > 0 && Database.GetAllObjects().Count(o => o.Type == ObjectType.Player) >= Config.MaxPlayers)
        {
            script.Steps.Add(new LoginStep("Too many players; creation is not allowed.", null, false, false));
            return;
        }

        var newPlayer = Database.CreateObject(name, ObjectType.Player, Dbref.Nothing);
        newPlayer.Owner = newPlayer.Id;
        newPlayer.Home = Config.DefaultHome;
        newPlayer.Pennies = Config.StartingMoney;
        newPlayer.Warnings = WarningBits.Normal;
        PasswordHash.Store(newPlayer, password);
        Database.MoveObject(newPlayer.Id, Config.PlayerStart);
        conn.PlayerId = newPlayer.Id;
        _rooms.Place(conn, newPlayer.Location);
        Database.SetConnected(newPlayer.Id, true);
        Channels.GetChannel("Public")?.AddMember(newPlayer.Id);

        script.Steps.Add(new LoginStep($"Character {name} created! Welcome to {Config.MudName}.", null, false, false));
        script.Steps.Add(new LoginStep(null, Config.NewuserFile, false, false));
        script.Steps.Add(new LoginStep(null, null, true, false));
        script.Arrive = true;
        script.Player = newPlayer.Id;
    }

    private List<string> BuildWhoLines()
    {
        var lines = new List<string>
        {
            "Player Name          Location   Connected",
            "----------------------------------------------------------------"
        };
        var active = _connections.Values.Where(c => c.PlayerId.IsValid && !c.Hidden).ToList();
        foreach (var c in active)
        {
            var p = Database.GetObject(c.PlayerId);
            if (p == null)
                continue;
            var loc = Database.GetObject(p.Location);
            string locName = loc != null ? loc.Name : p.Location.ToString();
            string duration = FormatDuration(DateTimeOffset.UtcNow - c.ConnectedAt);
            lines.Add($"{p.Name,-20} {locName,-10} {duration}");
        }
        lines.Add($"Total online: {active.Count}");
        return lines;
    }

    private async Task SendAnnouncedMotdAsync(MUSHConnection conn, bool royalty, CancellationToken ct)
    {
        foreach (string line in Motd.ConnectLines(royalty))
            await conn.SendLineAsync(line, ct);
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
                    SendToPlayer(notification.Target, notification.Message, notification.ProtocolType, notification.ProtocolPayload, notification.Prompt, notification.Speaker, notification.Spoofable, notification.EscapeHtml);
                }
                break;

            case NotificationScope.Room:
                if (notification.Target.IsValid)
                {
                    SendToRoom(notification.Target, notification.Message, speaker: notification.Speaker, spoofable: notification.Spoofable);
                }
                break;

            case NotificationScope.RoomExcept:
                if (notification.Target.IsValid)
                {
                    SendToRoom(notification.Target, notification.Message, notification.Except, notification.Speaker, notification.Spoofable);
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

    public void SendToPlayer(Dbref player, string message, string? protocolType = null, object? protocolPayload = null, bool prompt = false, Dbref speaker = default, bool spoofable = false, bool escapeHtml = false)
    {
        string line = WithNospoof(player, speaker, spoofable, message);
        foreach (var c in _connections.Values)
        {
            if (c.PlayerId != player)
                continue;

            if (protocolType != null)
            {
                if (c.IsWebSocket)
                {
                    object payloadObj = protocolPayload ?? string.Empty;
                    if (protocolPayload is string jsonText && !string.IsNullOrWhiteSpace(jsonText))
                    {
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(jsonText);
                            payloadObj = doc.RootElement.Clone();
                        }
                        catch
                        {
                            payloadObj = jsonText;
                        }
                    }

                    TrackOutbound(c.SendEnvelopeAsync(protocolType, payloadObj));
                }
                else if (c.Capabilities.SupportsGmcp && c.TelnetEnabled)
                {
                    TrackOutbound(c.SendGmcpAsync(protocolType, protocolPayload?.ToString() ?? string.Empty));
                }
                else if (!string.IsNullOrEmpty(line))
                {
                    TrackOutbound(prompt ? c.SendPromptAsync(line) : c.SendLineAsync(line, escapeHtml: escapeHtml));
                }
            }
            else
            {
                TrackOutbound(prompt ? c.SendPromptAsync(line) : c.SendLineAsync(line, escapeHtml: escapeHtml));
            }
        }
    }

    private ServerHelloPayload ApplyClientHello(MUSHConnection conn, ClientHelloPayload hello)
    {
        conn.ProtocolMode = ClientProtocolMode.MujsWebSocket;
        conn.Capabilities.IsWebSocket = true;
        conn.Capabilities.SupportsHtml = true;
        if (!string.IsNullOrWhiteSpace(hello.Client))
            conn.Capabilities.TerminalType = hello.Client;

        foreach (var cap in hello.Capabilities)
        {
            conn.ClientCapabilities.Add(cap);
            if (cap.Equals("gmcp", StringComparison.OrdinalIgnoreCase))
                conn.Capabilities.SupportsGmcp = true;
            else if (cap.Equals("rgb24", StringComparison.OrdinalIgnoreCase)
                || cap.Equals("truecolor", StringComparison.OrdinalIgnoreCase))
                conn.Capabilities.ColorMode = PennMUSH.Core.Presentation.ColorDepth.TrueColor;
            else if (cap.Equals("xterm256", StringComparison.OrdinalIgnoreCase)
                && conn.Capabilities.ColorMode < PennMUSH.Core.Presentation.ColorDepth.Color256)
                conn.Capabilities.ColorMode = PennMUSH.Core.Presentation.ColorDepth.Color256;
        }

        string token = Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData(
            _tokenSecret,
            System.Text.Encoding.UTF8.GetBytes(conn.Id + ":" + DateTimeOffset.UtcNow.ToUnixTimeSeconds())));
        conn.SessionToken = token;
        var features = new ServerFeatures(
            MaxWindowCount: 8,
            SupportedLanguages: new[] { "mushcode", "csharp", "moocode" },
            AssetBaseUrl: !string.IsNullOrEmpty(Config.MudUrl) ? Config.MudUrl : "http://localhost:5119");
        return new ServerHelloPayload("pennmush-dotnet", "1.0.0", token, features);
    }

    private string WithNospoof(Dbref listenerId, Dbref speakerId, bool spoofable, string message)
    {
        if (!spoofable || !speakerId.IsValid || !listenerId.IsValid)
            return message;
        var speaker = Database.GetObject(speakerId);
        var listener = Database.GetObject(listenerId);
        if (speaker == null || listener == null)
            return message;
        return SpoofPrefix.ForListener(Database, speaker, listener, message);
    }

    private async Task HandleProtocolEnvelopeAsync(MUSHConnection conn, string type, string? id, string json, CancellationToken ct)
    {
        switch (type.ToLowerInvariant())
        {
            case "client.hello":
                var hello = ProtocolSerializer.DeserializePayload<ClientHelloPayload>(json);
                if (hello != null)
                {
                    var serverHello = await _game.RunAsync(() => ApplyClientHello(conn, hello));
                    await conn.SendEnvelopeAsync("server.hello", serverHello, id, ct);
                    if (!string.IsNullOrEmpty(conn.PublicBaseUrl) && ScreenStylesheet.Load(GameDirectory) != null)
                    {
                        await conn.SendEnvelopeAsync(
                            "style.load",
                            new StyleLoadPayload(
                                "pennmush-screen",
                                conn.PublicBaseUrl.TrimEnd('/') + "/html/stylesheet.css",
                                "global"),
                            null,
                            ct);
                    }
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
                    var result = await _game.RunAsync(() => EditorSessions.SaveSession(save.SessionId, save.Code, Database, conn.PlayerId));
                    await conn.SendEnvelopeAsync("editor.result", result, id, ct);
                    if (result.Success)
                        await _game.RunAsync(FlushPending);
                }
                break;

            case "editor.close":
                var close = ProtocolSerializer.DeserializePayload<EditorSavePayload>(json);
                if (close != null)
                {
                    await _game.RunAsync(() => EditorSessions.CloseSession(close.SessionId));
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

    public void SendToRoom(Dbref roomRef, string message, Dbref excludePlayerId = default, Dbref speaker = default, bool spoofable = false)
    {
        foreach (var c in _rooms.In(roomRef))
        {
            if (!c.PlayerId.IsValid)
                continue;
            if (excludePlayerId.IsValid && c.PlayerId == excludePlayerId)
                continue;
            TrackOutbound(c.SendLineAsync(WithNospoof(c.PlayerId, speaker, spoofable, message)));
        }
    }

    private void Relocate(MUSHObject player, Dbref room)
    {
        foreach (var conn in _connections.Values)
        {
            if (conn.PlayerId == player.Id)
                _rooms.Place(conn, room);
        }
    }

    public void SendToAll(string message)
    {
        foreach (var c in _connections.Values)
        {
            if (c.PlayerId.IsValid)
                TrackOutbound(c.SendLineAsync(message));
        }
    }

    private void TrackOutbound(Task task)
    {
        if (task.IsCompletedSuccessfully)
            return;

        lock (_outboundLock)
        {
            _outbound = _outbound.IsCompleted
                ? task
                : Task.WhenAll(_outbound, task);
        }
    }

    private IReadOnlyList<ConnectionSnapshot> BuildConnectionSnapshots()
    {
        return _connections.Values
            .Where(c => c.PlayerId.IsValid)
            .OrderBy(c => c.Descriptor)
            .Select(c => new ConnectionSnapshot(
                PlayerId: c.PlayerId,
                Descriptor: c.Descriptor,
                Host: string.IsNullOrWhiteSpace(c.Host) ? "localhost" : c.Host,
                IpAddress: string.IsNullOrWhiteSpace(c.Host) ? "127.0.0.1" : c.Host,
                TerminalType: c.Capabilities.TerminalType,
                ConnectedAt: c.ConnectedAt,
                LastActivityAt: c.LastActivity,
                IsHidden: c.Hidden,
                BytesSent: c.BytesSent,
                BytesReceived: c.BytesReceived,
                CommandCount: c.Commands,
                SupportsTelnet: c.TelnetEnabled,
                SupportsGmcp: c.Capabilities.SupportsGmcp,
                IsSsl: c.Capabilities.IsSsl,
                IsWebSocket: c.IsWebSocket,
                SupportsHtml: c.Capabilities.SupportsHtml,
                SupportsPueblo: c.Capabilities.SupportsPueblo,
                StripAccents: c.StripAccents,
                PromptNewlines: c.PromptNewlines,
                SupportsXterm256: c.Capabilities.SupportsXterm256,
                SupportsRgb24: c.Capabilities.SupportsRgb24,
                Width: c.Capabilities.TerminalWidth,
                Height: c.Capabilities.TerminalHeight,
                ColorStyle: c.ColorStyle))
            .ToList();
    }

    private async Task FlushOutbound()
    {
        Task pending;
        lock (_outboundLock)
        {
            pending = _outbound;
            _outbound = Task.CompletedTask;
        }
        try
        {
            await pending;
        }
        catch
        {
            // A player who already disconnected does not stop the shutdown.
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
                    TrackOutbound(c.SendLineAsync(message));
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
        while (!ct.IsCancellationRequested && !_game.IsStopped)
        {
            try
            {
                await _game.RunAsync(TickOnce);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                try { Logs.Append(WizardLogKind.Err, "queue tick: " + ex.Message); }
                catch { }
            }

            try
            {
                await Task.Delay(100, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void TickOnce()
    {
        try
        {
            Cron.ProcessDueJobs(DateTimeOffset.UtcNow, Queue);
            var readyContexts = Queue.ProcessReady(DateTimeOffset.UtcNow, Dispatcher, Database, (ex, entry) =>
            {
                Logs.Append(WizardLogKind.Err, $"queue #{entry.Executor.Value} {entry.Command}: {ex.Message}");
            });
            foreach (var ctx in readyContexts)
            {
                foreach (var note in ctx.TargetedNotifications)
                    DispatchNotification(note);
            }
            _sessions.FlushDisconnects();
            if (AutosaveEnabled && DateTimeOffset.UtcNow >= _nextAutosave)
            {
                _nextAutosave = DateTimeOffset.UtcNow.AddSeconds(1);
                FlushPending();
            }
        }
        catch (Exception ex)
        {
            Logs.Append(WizardLogKind.Err, "queue tick: " + ex.Message);
        }
    }

    /// <summary>
    /// Flushes database state to storage and shuts down extensions cleanly for a hot-reboot,
    /// detaching connection references WITHOUT closing the underlying player sockets.
    /// </summary>
    public void PrepareForReboot()
    {
        _game.Run(PrepareForRebootCore);
    }

    private void PrepareForRebootCore()
    {
        try { _cts.Cancel(); } catch { }
        try { FlushPending(); } catch { }
        try { Extensions.Dispose(); } catch { }
        _connections.Clear();
        try { Storage.Dispose(); } catch { }
        _game.StopAfterCurrent();
    }

    public void Stop()
    {
        if (_game.IsStopped)
        {
            StopCore();
            return;
        }

        try
        {
            _game.Run(StopCore);
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void StopCore()
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
            return;
        try { _cts.Cancel(); } catch { }
        try { FlushPending(); } catch { }
        try { Extensions.Dispose(); } catch { }
        foreach (var conn in _connections.Values)
        {
            conn.Close();
            ConnectionLog.RecordDisconnect(conn.ConnLogId, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), "quit");
        }
        try { Storage.Dispose(); } catch { }
        _game.StopAfterCurrent();
    }
}
