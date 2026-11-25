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
        "max_guests", "max_players", "master_room", "system_object", "player_start", "base_room", "default_home",
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
        "player_name_len", "player_name_spaces", "only_ascii_in_names"
    };
}

public record RestrictionRule(string Restriction, string? ErrorMessage = null);
public record HelpFileDirective(string CommandName, string FilePath, bool AdminOnly);
