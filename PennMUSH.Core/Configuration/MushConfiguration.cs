namespace PennMUSH.Core.Configuration;

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using PennMUSH.Core.Types;

public class MushConfiguration
{
    // Essentials
    public string MudName { get; set; } = "PennMUSH";
    public string MudUrl { get; set; } = string.Empty;
    public int Port { get; set; } = 4201;
    public int SslPort { get; set; } = 4202;
    public string IpAddr { get; set; } = string.Empty;
    public bool UseWs { get; set; } = true;
    public string WsUrl { get; set; } = string.Empty;
    public bool UseDns { get; set; } = true;
    public bool PlayerCreation { get; set; } = true;
    public bool Guests { get; set; } = true;
    public int MaxGuests { get; set; } = 10;
    public int MaxPlayers { get; set; } = 1000;

    // Special Dbrefs
    public Dbref MasterRoom { get; set; } = new(2);
    public Dbref SystemObject { get; set; } = new(3);
    public Dbref SpeechMogrifier { get; set; } = Dbref.Nothing;
    public Dbref PlayerStart { get; set; } = new(0);
    public Dbref BaseRoom { get; set; } = new(0);
    public Dbref DefaultHome { get; set; } = new(0);

    // Database Paths
    public string InputDatabase { get; set; } = "data/indb";
    public string OutputDatabase { get; set; } = "data/outdb";
    public string CrashDatabase { get; set; } = "data/PANIC.db";
    public string MailDatabase { get; set; } = "data/maildb";
    public string ChatDatabase { get; set; } = "data/chatdb";
    public string SqlDatabase { get; set; } = string.Empty;
    public Dbref HttpHandler { get; set; } = Dbref.Nothing;

    // Text File Paths (relative to game/ directory)
    public string ConnectFile { get; set; } = "txt/connect.txt";
    public string MotdFile { get; set; } = "txt/motd.txt";
    public string WizmotdFile { get; set; } = "txt/wizmotd.txt";
    public string NewuserFile { get; set; } = "txt/newuser.txt";
    public string QuitFile { get; set; } = "txt/quit.txt";
    public string DownFile { get; set; } = "txt/down.txt";
    public string FullFile { get; set; } = "txt/full.txt";
    public string GuestFile { get; set; } = "txt/guest.txt";
    public string RegisterFile { get; set; } = "txt/register.txt";
    public string ColorsFile { get; set; } = "txt/colors.json";
    public string DictFile { get; set; } = string.Empty;

    // Economy & Names
    public string MoneySingular { get; set; } = "Penny";
    public string MoneyPlural { get; set; } = "Pennies";
    public int StartingMoney { get; set; } = 100;
    public bool UseQuota { get; set; } = true;
    public bool TinyTrimFun { get; set; } = false;
    public bool UnsafeEvalFunctions { get; set; } = false;
    public bool ConfigSave { get; set; } = false;
    public string? ConfigFilePath { get; set; }
    public int StartingQuota { get; set; } = 20;
    public int Paycheck { get; set; } = 50;

    // Phase 2: Evaluation & Security
    public bool SaferUfun { get; set; } = true;
    public bool FunctionSideEffects { get; set; } = true;
    public int FunctionInvocationLimit { get; set; } = 25000;
    public int FunctionRecursionLimit { get; set; } = 50;
    public int CallLimit { get; set; } = 100;
    public bool TinyBooleans { get; set; } = false;
    public bool TinyMath { get; set; } = false;
    public bool NullEqZero { get; set; } = false;
    public bool ReadRemoteDesc { get; set; } = false;
    public bool EmptyAttrs { get; set; } = true;

    // Phase 2: Economy & Limits
    public int ObjectCost { get; set; } = 10;
    public int RoomCost { get; set; } = 10;
    public int ExitCost { get; set; } = 1;
    public int LinkCost { get; set; } = 1;
    public int FindCost { get; set; } = 100;
    public int QueueCost { get; set; } = 10;
    public int QuotaCost { get; set; } = 1;
    public int MaxPennies { get; set; } = 100000;
    public int MaxAliases { get; set; } = 3;
    public int MaxAttrsPerObj { get; set; } = 2048;
    public int MaxChannels { get; set; } = 200;
    public int MaxPlayerChans { get; set; } = 0;
    public int MaxDbref { get; set; } = 0;
    public int MaxDepth { get; set; } = 10;
    public int MaxParents { get; set; } = 10;
    public int MaxNamedQregs { get; set; } = 50;
    public int PlayerQueueLimit { get; set; } = 100;
    public int QueueChunk { get; set; } = 3;
    public int MailLimit { get; set; } = 300;

    // Phase 2: Login & Player Naming
    public bool Logins { get; set; } = true;
    public int MaxLogins { get; set; } = 120;
    public int ConnectFailLimit { get; set; } = 10;
    public int IdleTimeout { get; set; } = 0;
    public int UnconnectedIdleTimeout { get; set; } = 300;
    public int PlayerNameLen { get; set; } = 15;
    public bool PlayerNameSpaces { get; set; } = false;
    public bool OnlyAsciiInNames { get; set; } = true;

    // Phase 3: World, Command & Display Behavior
    public bool ExitsConnectRooms { get; set; } = false;
    public bool LinkToObject { get; set; } = true;
    public bool OwnerQueues { get; set; } = false;
    public bool PossessiveGet { get; set; } = true;
    public bool PossessiveGetD { get; set; } = false;
    public bool ReallySafe { get; set; } = true;
    public bool DestroyPossessions { get; set; } = true;
    public bool Adestroy { get; set; } = false;
    public bool Amail { get; set; } = false;
    public bool Startups { get; set; } = true;
    public bool RoomConnects { get; set; } = true;
    public bool PlayerListen { get; set; } = true;
    public bool PlayerAhear { get; set; } = true;
    public bool NoisyWhisper { get; set; } = false;
    public bool FullInvis { get; set; } = false;
    public bool WizNoAenter { get; set; } = false;
    public bool SilentPemit { get; set; } = false;
    public int WhisperLoudness { get; set; } = 100;
    public bool ZoneControlZmpOnly { get; set; } = true;
    public bool ExPublicAttribs { get; set; } = true;
    public bool FlagsOnExamine { get; set; } = true;
    public int FloatPrecision { get; set; } = 6;
    public bool CommaExitList { get; set; } = true;
    public bool AnsiNames { get; set; } = true;
    public bool CountAll { get; set; } = false;
    public string Monikers { get; set; } = string.Empty;

    // Phase 3: Chat, Ancestors & Default Flags
    public int ChanCost { get; set; } = 1000;
    public int ChanTitleLen { get; set; } = 80;
    public bool NoisyCemit { get; set; } = false;
    public bool ChatStripQuote { get; set; } = true;
    public bool UseMuxcomm { get; set; } = false;
    public string ChannelFlags { get; set; } = "player";
    public Dbref AncestorRoom { get; set; } = Dbref.Nothing;
    public Dbref AncestorThing { get; set; } = Dbref.Nothing;
    public Dbref AncestorExit { get; set; } = Dbref.Nothing;
    public Dbref AncestorPlayer { get; set; } = Dbref.Nothing;
    public string PlayerFlags { get; set; } = "enter_ok ansi no_command";
    public string RoomFlags { get; set; } = "no_command";
    public string ExitFlags { get; set; } = string.Empty;
    public string ThingFlags { get; set; } = "no_command";

    // Phase 4: Intervals, Events, Logs, HTML Screens, Connlog, SSL, SQL, MSSP
    public int HttpPerSecond { get; set; } = 3;
    public Dbref EventHandler { get; set; } = Dbref.Nothing;
    public string AccessFile { get; set; } = "access.cnf";
    public int DumpInterval { get; set; } = 3600;
    public int DbckInterval { get; set; } = 599;
    public int PurgeInterval { get; set; } = 601;
    public int WarnInterval { get; set; } = 3600;
    public string ConnectHtmlFile { get; set; } = "txt/connect.html";
    public string MotdHtmlFile { get; set; } = "txt/motd.html";
    public string WizmotdHtmlFile { get; set; } = "txt/wizmotd.html";
    public string NewuserHtmlFile { get; set; } = "txt/newuser.html";
    public string QuitHtmlFile { get; set; } = "txt/quit.html";
    public string DownHtmlFile { get; set; } = "txt/down.html";
    public string FullHtmlFile { get; set; } = "txt/full.html";
    public string GuestHtmlFile { get; set; } = "txt/guest.html";
    public string RegisterHtmlFile { get; set; } = "txt/register.html";
    public bool UseConnlog { get; set; } = true;
    public string ConnlogDb { get; set; } = "connlog.db";
    public string ErrorLog { get; set; } = "log/netmush.log";
    public string ConnectLog { get; set; } = "log/connect.log";
    public string WizardLog { get; set; } = "log/wizard.log";
    public string CheckptLog { get; set; } = "log/checkpt.log";
    public string TraceLog { get; set; } = "log/trace.log";
    public string CommandLog { get; set; } = "log/command.log";
    public bool LogCommands { get; set; } = false;
    public bool LogForces { get; set; } = false;
    public string LogWipePasswd { get; set; } = "zap!";
    public int LogMaxSize { get; set; } = 100;
    public string LogSizePolicy { get; set; } = "trim";
    public bool Pueblo { get; set; } = false;
    public string SqlPlatform { get; set; } = "disabled";
    public string SqlHost { get; set; } = "127.0.0.1";
    public string SqlUsername { get; set; } = "mush";
    public string SqlPassword { get; set; } = "mush";
    public string SslIpAddr { get; set; } = string.Empty;
    public string SslCertificateFile { get; set; } = "server-cert.crt";
    public string SslPrivateKeyFile { get; set; } = "server-key.crt";
    public string SslCaFile { get; set; } = string.Empty;
    public string SslCaDir { get; set; } = string.Empty;
    public bool SslRequireClientCert { get; set; } = false;
    public Dictionary<string, string> MsspFields { get; } = new(StringComparer.OrdinalIgnoreCase);

    // Aliases & Restrictions
    public Dictionary<string, string> CommandAliases { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> FunctionAliases { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ReservedAliases { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<RestrictionRule>> CommandRestrictions { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<RestrictionRule>> FunctionRestrictions { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> RestrictedNames { get; } = new();
    public List<HelpFileDirective> HelpFiles { get; } = new();

    // Raw directives for unmodeled config directives
    public Dictionary<string, string> RawDirectives { get; } = new(StringComparer.OrdinalIgnoreCase);

    // Every mush.cnf directive and the text written after it. Screen files
    // substitute {{directive}} from this table.
    public Dictionary<string, string> Directives { get; } = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Regex ScreenTokens = new(@"\{\{\s*([A-Za-z0-9_]+)\s*\}\}", RegexOptions.Compiled);

    public string ExpandScreenText(string text, bool html)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf("{{", StringComparison.Ordinal) < 0)
            return text;

        return ScreenTokens.Replace(text, match =>
        {
            if (!Directives.TryGetValue(match.Groups[1].Value, out string? value))
                return match.Value;
            return html ? WebUtility.HtmlEncode(value) : value;
        });
    }

    public bool TrySetSwitch(string name, bool enabled, out string failure)
    {
        failure = "";
        if (string.IsNullOrWhiteSpace(name))
        {
            failure = "No such option.";
            return false;
        }

        string key = name.Trim().ToLowerInvariant();
        switch (key)
        {
            case "use_ws":
                UseWs = enabled;
                break;
            case "use_dns":
                UseDns = enabled;
                break;
            case "player_creation":
                PlayerCreation = enabled;
                break;
            case "guests":
                Guests = enabled;
                break;
            case "use_quota":
                UseQuota = enabled;
                break;
            case "tiny_trim_fun":
                TinyTrimFun = enabled;
                break;
            case "unsafe_eval_functions":
                UnsafeEvalFunctions = enabled;
                break;
            case "safer_ufun":
                SaferUfun = enabled;
                break;
            case "function_side_effects":
                FunctionSideEffects = enabled;
                break;
            case "tiny_booleans":
                TinyBooleans = enabled;
                break;
            case "tiny_math":
                TinyMath = enabled;
                break;
            case "null_eq_zero":
                NullEqZero = enabled;
                break;
            case "read_remote_desc":
                ReadRemoteDesc = enabled;
                break;
            case "empty_attrs":
                EmptyAttrs = enabled;
                break;
            case "logins":
                Logins = enabled;
                break;
            case "player_name_spaces":
                PlayerNameSpaces = enabled;
                break;
            case "only_ascii_in_names":
                OnlyAsciiInNames = enabled;
                break;
            case "exits_connect_rooms":
                ExitsConnectRooms = enabled;
                break;
            case "link_to_object":
                LinkToObject = enabled;
                break;
            case "owner_queues":
                OwnerQueues = enabled;
                break;
            case "possessive_get":
                PossessiveGet = enabled;
                break;
            case "possessive_get_d":
                PossessiveGetD = enabled;
                break;
            case "really_safe":
                ReallySafe = enabled;
                break;
            case "destroy_possessions":
                DestroyPossessions = enabled;
                break;
            case "adestroy":
                Adestroy = enabled;
                break;
            case "amail":
                Amail = enabled;
                break;
            case "startups":
                Startups = enabled;
                break;
            case "room_connects":
                RoomConnects = enabled;
                break;
            case "player_listen":
                PlayerListen = enabled;
                break;
            case "player_ahear":
                PlayerAhear = enabled;
                break;
            case "noisy_whisper":
                NoisyWhisper = enabled;
                break;
            case "full_invis":
                FullInvis = enabled;
                break;
            case "wiz_noaenter":
                WizNoAenter = enabled;
                break;
            case "silent_pemit":
                SilentPemit = enabled;
                break;
            case "zone_control_zmp_only":
                ZoneControlZmpOnly = enabled;
                break;
            case "ex_public_attribs":
                ExPublicAttribs = enabled;
                break;
            case "flags_on_examine":
                FlagsOnExamine = enabled;
                break;
            case "comma_exit_list":
                CommaExitList = enabled;
                break;
            case "ansi_names":
                AnsiNames = enabled;
                break;
            case "count_all":
                CountAll = enabled;
                break;
            case "noisy_cemit":
                NoisyCemit = enabled;
                break;
            case "chat_strip_quote":
                ChatStripQuote = enabled;
                break;
            case "use_muxcomm":
                UseMuxcomm = enabled;
                break;
            case "use_connlog":
                UseConnlog = enabled;
                break;
            case "log_commands":
                LogCommands = enabled;
                break;
            case "log_forces":
                LogForces = enabled;
                break;
            case "pueblo":
                Pueblo = enabled;
                break;
            case "ssl_require_client_cert":
                SslRequireClientCert = enabled;
                break;
            default:
                failure = KnownDirectives.Contains(key)
                    ? "That isn't an on/off option."
                    : "No such option.";
                return false;
        }

        Directives[key] = enabled ? "yes" : "no";
        return true;
    }

    private static readonly HashSet<string> KnownDirectives = new(StringComparer.OrdinalIgnoreCase)
    {
        "include", "command_alias", "function_alias", "reserve_alias", "restrict_command",
        "restrict_function", "restrict_name", "names_file", "mud_name", "mud_url", "port",
        "ssl_port", "ip_addr", "use_ws", "ws_url", "use_dns", "player_creation", "guests",
        "max_guests", "max_players", "master_room", "system_object", "speech_mogrifier", "player_start", "base_room", "default_home",
        "input_database", "output_database", "crash_database", "mail_database", "chat_database",
        "sql_database", "http_handler",
        "connect_file", "motd_file", "wizmotd_file", "newuser_file", "quit_file", "down_file",
        "full_file", "guest_file", "register_create_file", "colors_file", "dict_file",
        "help_command", "ahelp_command", "money_singular", "money_plural",
        "starting_money", "paycheck", "use_quota", "starting_quota", "tiny_trim_fun", "unsafe_eval_functions", "config_save",
        "safer_ufun", "function_side_effects", "function_invocation_limit", "function_recursion_limit", "call_limit",
        "tiny_booleans", "tiny_math", "null_eq_zero", "read_remote_desc", "empty_attrs",
        "object_cost", "room_cost", "exit_cost", "link_cost", "find_cost", "queue_cost", "quota_cost",
        "max_pennies", "max_aliases", "max_attrs_per_obj", "max_channels", "max_player_chans", "max_dbref",
        "max_depth", "max_parents", "max_named_qregs", "player_queue_limit", "queue_chunk", "mail_limit",
        "logins", "max_logins", "connect_fail_limit", "idle_timeout", "unconnected_idle_timeout",
        "player_name_len", "player_name_spaces", "only_ascii_in_names",
        "exits_connect_rooms", "link_to_object", "owner_queues", "possessive_get", "possessive_get_d",
        "really_safe", "destroy_possessions", "adestroy", "amail", "startups", "room_connects",
        "player_listen", "player_ahear", "noisy_whisper", "full_invis", "wiz_noaenter", "silent_pemit",
        "whisper_loudness", "zone_control_zmp_only", "ex_public_attribs", "flags_on_examine",
        "float_precision", "comma_exit_list", "ansi_names", "count_all", "monikers",
        "chan_cost", "chan_title_len", "noisy_cemit", "chat_strip_quote", "use_muxcomm", "channel_flags",
        "ancestor_room", "ancestor_thing", "ancestor_exit", "ancestor_player",
        "player_flags", "room_flags", "exit_flags", "thing_flags",
        "http_per_second", "event_handler", "access_file", "dump_interval", "dbck_interval",
        "purge_interval", "warn_interval", "connect_html_file", "motd_html_file", "wizmotd_html_file",
        "newuser_html_file", "quit_html_file", "down_html_file", "full_html_file", "guest_html_file",
        "register_create_html_file", "use_connlog", "connlog_db", "error_log", "connect_log",
        "wizard_log", "checkpt_log", "trace_log", "command_log", "log_commands", "log_forces",
        "log_wipe_passwd", "log_max_size", "log_size_policy", "pueblo", "sql_platform", "sql_host",
        "sql_username", "sql_password", "ssl_ip_addr", "ssl_certificate_file", "ssl_private_key_file",
        "ssl_ca_file", "ssl_ca_dir", "ssl_require_client_cert", "mssp"
    };
}

public record RestrictionRule(string Restriction, string? ErrorMessage = null);
public record HelpFileDirective(string CommandName, string FilePath, bool AdminOnly);
