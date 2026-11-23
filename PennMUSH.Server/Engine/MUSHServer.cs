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
            GameDirectory = Path.GetDirectoryName(Path.GetFullPath(resolvedConfig)) ?? Directory.GetCurrentDirectory();
        }
        else
        {
            Config = new MushConfiguration();
            GameDirectory = Directory.GetCurrentDirectory();
        }

        Database = new ObjectDatabase();
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
        Functions.DefaultHome = Config.DefaultHome;
        Functions.StartingMoney = Config.StartingMoney;
        Functions.UseQuota = Config.UseQuota;
        Functions.UseQuotaProvider = () => Config.UseQuota;
        Functions.StartingQuota = Config.StartingQuota;
        Functions.StartingQuotaProvider = () => Config.StartingQuota;
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

    private static string? ResolveConfigPath(string? configPath)
    {
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            return Path.GetFullPath(configPath);
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
        Dispatcher.Register(new SayCommandHandler(Evaluator), "say", "\"", "'");
        Dispatcher.Register(new PoseCommandHandler(Evaluator), "pose", ":", ";", "semipose");
        _roomLook = new LookCommandHandler(Evaluator);
        Dispatcher.Register(_roomLook, "look", "l", "read");
        Func<Dbref, bool> online = id => _connections.Values.Any(conn => conn.PlayerId == id);
        Dispatcher.Register(new GoCommandHandler(_roomLook, Evaluator, Dispatcher, online), "go", "goto", "move");
        Dispatcher.Register(new DigCommandHandler(_roomLook, () => Config.UseQuota, () => Config.StartingQuota), "@dig");
        Dispatcher.Register(new OpenCommandHandler(() => Config.UseQuota, () => Config.StartingQuota), "@open");
        Dispatcher.Register(new LinkCommandHandler(), "@link");
        Dispatcher.Register(new UnlinkCommandHandler(), "@unlink");
        Dispatcher.Register(new CreateCommandHandler(() => Config.UseQuota, () => Config.StartingQuota), "@create");
        Dispatcher.Register(new SetCommandHandler(), "@set");
        Dispatcher.Register(new PowerCommandHandler(), "@power");
        Dispatcher.Register(new NameCommandHandler(), "@name");
        Dispatcher.Register(new ParentCommandHandler(), "@parent");
        Dispatcher.Register(new ZoneCommandHandler(), "@zone");
        Dispatcher.Register(new ChzoneCommandHandler(), "@chzone");
        Dispatcher.Register(new ChzoneAllCommandHandler(), "@chzoneall");
        Dispatcher.Register(new ZemitCommandHandler(), "@zemit", "@nszemit");
        Dispatcher.Register(new ScanCommandHandler(Config.MasterRoom.Value), "@scan");
        Dispatcher.Register(new SweepCommandHandler(), "@sweep");
        Dispatcher.Register(new WithCommandHandler(Dispatcher, Evaluator, Config.MasterRoom.Value), "with");
        Dispatcher.Register(new SearchCommandHandler(Evaluator), "@search");
        Dispatcher.Register(new WaitCommandHandler(Queue), "@wait", "@notify", "@drain");
        Dispatcher.Register(new HaltCommandHandler(Queue), "@halt");
        Dispatcher.Register(new AllHaltCommandHandler(Queue), "@allhalt");
        Dispatcher.Register(new PsCommandHandler(Queue), "@ps");
        Dispatcher.Register(new KickCommandHandler(Queue, Dispatcher), "@kick");
        Dispatcher.Register(new ThinkCommandHandler(Evaluator), "think", "th");
        Dispatcher.Register(new TeleportCommandHandler(_roomLook), "@teleport", "@tel");
        Dispatcher.Register(new GetCommandHandler(), "get", "take", "g");
        Dispatcher.Register(new DropCommandHandler(), "drop", "throw", "d");
        Dispatcher.Register(new InventoryCommandHandler(Evaluator), "inventory", "i", "inv");
        Dispatcher.Register(new ChannelCommandHandler(Channels), "@channel");
        Dispatcher.Register(new ClockCommandHandler(Channels), "@clock");
        Dispatcher.Register(new ComCommandHandler(Channels), "addcom", "delcom", "comtitle", "comlist", "@clist");
        Dispatcher.Register(new ChatCommandHandler(Channels, Evaluator), "@chat", "+c");
        Dispatcher.Register(new CemitCommandHandler(Channels, Evaluator), "@cemit", "@nscemit");
        Dispatcher.Register(new MailCommandHandler(Mail, MailAliases), "@mail");
        Dispatcher.Register(new MaliasCommandHandler(MailAliases), "@malias");
        Dispatcher.Register(new CronCommandHandler(Cron, Queue), "@cron");
        Dispatcher.Register(new HelpCommandHandler(Help, PlayerReceivesHtml, Suggestions), "help", "?");
        Dispatcher.Register(new EmitCommandHandler(Evaluator), "@emit", "\\", "@lemit", "@nsemit", "@nslemit");
        Dispatcher.Register(new PemitCommandHandler(), "@pemit", "@prompt", "@nspemit", "@nsprompt");
        Dispatcher.Register(new RemitCommandHandler(), "@remit", "@nsremit");
        Dispatcher.Register(new OemitCommandHandler(), "@oemit", "@nsoemit");
        Dispatcher.Register(new WallCommandHandler(), "@wall", "@wizwall", "@rwall");
        Dispatcher.Register(new WhisperCommandHandler(), "whisper", "w");
        Dispatcher.Register(new PageCommandHandler(Evaluator), "page", "p");

        // Spatial & Interaction
        Dispatcher.Register(new HomeCommandHandler(_roomLook), "home");
        Dispatcher.Register(new EnterCommandHandler(_roomLook, Dispatcher, online), "enter");
        Dispatcher.Register(new LeaveCommandHandler(_roomLook, Dispatcher, online), "leave");
        Dispatcher.Register(new FollowCommandHandler(online), "follow");
        Dispatcher.Register(new UnfollowCommandHandler(), "unfollow");
        Dispatcher.Register(new DismissCommandHandler(), "dismiss");
        Dispatcher.Register(new DesertCommandHandler(), "desert");
        Dispatcher.Register(new GiveCommandHandler(), "give");
        Dispatcher.Register(new UseCommandHandler(Queue, Evaluator), "use");
        Dispatcher.Register(new EmptyCommandHandler(), "empty");
        Dispatcher.Register(new TeachCommandHandler(Queue), "teach");
        Dispatcher.Register(new ExamineCommandHandler(), "examine", "ex", "x", "brief");

        // Scripting & Flow Control
        Dispatcher.Register(new SwitchCommandHandler(Dispatcher, Evaluator), "@switch", "@sw", "@select");
        Dispatcher.Register(new DolistCommandHandler(Dispatcher, Evaluator), "@dolist");
        Dispatcher.Register(new TriggerCommandHandler(Dispatcher, Evaluator), "@trigger", "@tr");
        Dispatcher.Register(new IncludeCommandHandler(Dispatcher, Evaluator), "@include");
        Dispatcher.Register(new ActionListCommandHandler(Dispatcher, "@assert", Evaluator, Queue), "@assert");
        Dispatcher.Register(new ActionListCommandHandler(Dispatcher, "@break", Evaluator, Queue), "@break");
        Dispatcher.Register(new ActionListCommandHandler(Dispatcher, "@retry", Evaluator, Queue), "@retry");
        Dispatcher.Register(new VerbCommandHandler(Dispatcher, Evaluator), "@verb");
        Dispatcher.Register(new LogCommandHandler(Logs), "@log");
        Dispatcher.Register(new LogWipeCommandHandler(Logs), "@logwipe");
        Dispatcher.Register(new RestartCommandHandler(Queue), "@restart");
        Dispatcher.Register(new SqlCommandHandler(Sql), "@sql");
        Dispatcher.Register(new MapSqlCommandHandler(Sql, Queue), "@mapsql");
        Dispatcher.Register(new HttpCommandHandler(Web, Queue), "@http");
        Dispatcher.Register(new RespondCommandHandler(Http), "@respond");
        Dispatcher.Register(new SlaveCommandHandler(), "@slave");
        Dispatcher.Register(new FunctionCommandHandler(Functions), "@function");
        Dispatcher.Register(new EnableCommandHandler(Config, false), "@disable");
        Dispatcher.Register(new EnableCommandHandler(Config, true), "@enable");
        Dispatcher.Register(new ConfigCommandHandler(Config), "@config");
        Dispatcher.Register(new FlagCommandHandler(), "@flag");
        Dispatcher.Register(new AttributeCommandHandler(), "@attribute");
        Dispatcher.Register(new CommandCommandHandler(Dispatcher), "@command");
        Dispatcher.Register(new HookCommandHandler(Dispatcher), "@hook");
        Dispatcher.Register(new WcheckCommandHandler(), "@wcheck");
        Dispatcher.Register(new BuyCommandHandler(Evaluator, Queue), "buy");
        Dispatcher.Register(new DbckCommandHandler(), "@dbck");
        Dispatcher.Register(new WarningsCommandHandler(), "@warnings");
        Dispatcher.Register(new ForceCommandHandler(Dispatcher), "@force");
        Dispatcher.Register(new IfElseCommandHandler(Dispatcher, Evaluator), "@ifelse", "@if", "@skip");
        Dispatcher.Register(new CommentCommandHandler(), "@@");

        // Object Lifecycle & Admin
        Dispatcher.Register(new DestroyCommandHandler(() => Config.UseQuota, () => Config.StartingQuota), "@destroy", "@recycle", "@nuke");
        Dispatcher.Register(new PurgeCommandHandler(() => Config.UseQuota, () => Config.StartingQuota), "@purge");
        Dispatcher.Register(new CopyAttributeCommandHandler(), "@cpattr", "@mvattr");
        Dispatcher.Register(new EditAttributeCommandHandler(), "@edit");
        Dispatcher.Register(new GrepCommandHandler(), "@grep");
        Dispatcher.Register(new AttributeLockCommandHandler(), "@atrlock");
        Dispatcher.Register(new AttributeChownCommandHandler(), "@atrchown");
        Dispatcher.Register(new LockFlagCommandHandler(), "@lset");
        Dispatcher.Register(new DecompileCommandHandler(), "@decompile");
        Dispatcher.Register(new MonikerCommandHandler(Evaluator), "@moniker");
        Dispatcher.Register(new MessageCommandHandler(Evaluator), "@message");
        Dispatcher.Register(new FindCommandHandler(), "@find");
        Dispatcher.Register(new WhereIsCommandHandler(), "@whereis");
        Dispatcher.Register(new EntrancesCommandHandler(), "@entrances");
        Dispatcher.Register(new FirstExitCommandHandler(), "@firstexit");
        Dispatcher.Register(new UptimeCommandHandler(), "@uptime");
        Dispatcher.Register(new PoorCommandHandler(), "@poor");
        Dispatcher.Register(new QuotaCommandHandler(Config.UseQuota, Config.StartingQuota, () => Config.UseQuota, () => Config.StartingQuota), "@quota");
        Dispatcher.Register(new SquotaCommandHandler(Config.UseQuota, Config.StartingQuota, () => Config.UseQuota, () => Config.StartingQuota), "@squota");
        Dispatcher.Register(new AllQuotaCommandHandler(Config.StartingQuota, Config.UseQuota, () => Config.UseQuota, () => Config.StartingQuota), "@allquota");
        Dispatcher.Register(new ListCommandHandler(Dispatcher, Functions, Motd), "@list");
        Dispatcher.Register(new MotdCommandHandler(Motd), "@motd", "@listmotd", "@wizmotd", "@rejectmotd");
        Dispatcher.Register(new SuggestCommandHandler(Suggestions), "@suggest");
        Dispatcher.Register(
            new ReadCacheCommandHandler(Help, () => ScreenFileReport.Lines(GameDirectory, Config), TextCache, SyncSuggestionsFromCache),
            "@readcache");
        Dispatcher.Register(new DumpCommandHandler(_ => SaveAll()), "@dump");
        Dispatcher.Register(new UndestroyCommandHandler(), "@undestroy", "@unrecycle");
        Dispatcher.Register(new CloneCommandHandler(() => Config.UseQuota, () => Config.StartingQuota), "@clone");
        Dispatcher.Register(new ChownCommandHandler(() => Config.UseQuota, () => Config.StartingQuota), "@chown");
        Dispatcher.Register(new ChownAllCommandHandler(Config.StartingQuota, () => Config.UseQuota, () => Config.StartingQuota), "@chownall");
        Dispatcher.Register(new WipeCommandHandler(), "@wipe");
        Dispatcher.Register(new LockCommandHandler(), "@lock", "@elock", "@ulock");
        Dispatcher.Register(new UnlockCommandHandler(), "@unlock", "@eunlock", "@uunlock");
        Dispatcher.Register(new StatsCommandHandler(), "@stats");
        Dispatcher.Register(new ScoreCommandHandler(), "score");
        Dispatcher.Register(new DoingCommandHandler(), "doing", "@doing");
        Dispatcher.Register(new WhoCommandHandler(_sessions), "who", "session");
        Dispatcher.Register(new HideCommandHandler(_sessions), "@hide");
        Dispatcher.Register(new BootCommandHandler(_sessions), "@boot");
        Dispatcher.Register(new SocksetCommandHandler(_sessions), "@sockset");
        Dispatcher.Register(new PollCommandHandler(_sessions), "@poll");
        Dispatcher.Register(new PcreateCommandHandler(Functions.Passwords, Config.DefaultHome, Config.PlayerStart, Config.StartingMoney), "@pcreate");
        Dispatcher.Register(new PasswordCommandHandler(), "@password");
        Dispatcher.Register(new NewPasswordCommandHandler(Logs), "@newpassword");
        Dispatcher.Register(new VersionCommandHandler(), "@version");
        Dispatcher.Register(new ClientCommandHandler(EditorSessions), "@client");
        Dispatcher.Register(new PluginCommandHandler(Extensions), "@plugin", "@extension");
        Dispatcher.Register(new RebootCommandHandler(() => OnRebootRequested?.Invoke()), "@reboot");
        Dispatcher.Register(new ShutdownCommandHandler(
            SaveAll,
            () => _shutdownRequested = true,
            () => _quietRebootRequested = true), "@shutdown");
        Dispatcher.Register(new SiteLockCommandHandler(SiteLocks, ConnectionLog, new NameLockFile(Path.Combine(GameDirectory, "names.cnf"))), "@sitelock");

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

        Dispatcher.AddRestriction("buy", "nogagged", null);

        foreach (var (cmd, rules) in Config.CommandRestrictions)
        {
            foreach (var rule in rules)
            {
                Dispatcher.AddRestriction(cmd, rule.Restriction, rule.ErrorMessage);
            }
        }

        Dispatcher.AddRestriction("@malias", "nogagged", null);
        Dispatcher.AddRestriction("@sockset", "nogagged", null);
        Dispatcher.AddRestriction("@log", "nogagged", null);
        Dispatcher.AddRestriction("@password", "noguest", null);
        Dispatcher.AddRestriction("@newpassword", "wizard", null);
        Dispatcher.AddRestriction("@logwipe", "nogagged", null);
        Dispatcher.AddRestriction("@restart", "nogagged", null);
        Dispatcher.AddRestriction("@http", "nogagged", null);
        Dispatcher.AddRestriction("@http", "noguest", null);
        Dispatcher.AddRestriction("@respond", "nogagged", null);
        Dispatcher.AddRestriction("@slave", "wizard", null);
        Dispatcher.AddRestriction("@retry", "nogagged", null);
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
        conn.ConnLogId = ConnectionLog.RecordConnect(
            conn.PlayerId,
            conn.PlayerId.IsValid ? Database.GetObject(conn.PlayerId)?.Name : null,
            host,
            host,
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
        string trimmed = input.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        conn.NoteInput(trimmed.Length);

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

        var outcome = await _game.RunAsync(() => DispatchCommand(conn, trimmed));
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

    private CommandOutcome DispatchCommand(MUSHConnection conn, string trimmed)
    {
        _sessions.SourceDescriptor = conn.Descriptor;
        try
        {
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

        if (player == null)
        {
            script.Steps.Add(new LoginStep("Either that player does not exist, or has a different password.", null, false, false));
            return;
        }

        if (player.HasFlag("GUEST")
            ? !SiteLocks.Can(conn.Host, SiteLockTable.Guest)
            : !SiteLocks.Can(conn.Host, SiteLockTable.Connect))
        {
            script.Steps.Add(new LoginStep(
                player.HasFlag("GUEST") ? "Guest connections not allowed." : "Player connections not allowed.",
                null, false, false));
            return;
        }

        if (!PasswordHash.Accepts(player, password, memory: null))
        {
            script.Steps.Add(new LoginStep("Either that player does not exist, or has a different password.", null, false, false));
            return;
        }

        conn.PlayerId = player.Id;
        _rooms.Place(conn, player.Location);
        Database.SetConnected(player.Id, true);
        ConnectionLog.UpdatePlayer(conn.ConnLogId, player.Id, player.Name);
        Channels.GetChannel("Public")?.AddMember(player.Id);

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

        if (!SiteLocks.Can(conn.Host, SiteLockTable.Create))
        {
            script.Steps.Add(new LoginStep("Creation is NOT allowed", null, false, false));
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
            AssetBaseUrl: !string.IsNullOrEmpty(Config.MudUrl) ? Config.MudUrl : "http://localhost:5000");
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
