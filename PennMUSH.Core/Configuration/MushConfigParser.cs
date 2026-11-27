namespace PennMUSH.Core.Configuration;

using System;
using System.Collections.Generic;
using System.IO;
using PennMUSH.Core.Types;

public static class MushConfigParser
{
    public static MushConfiguration ParseFile(string filePath)
    {
        var config = new MushConfiguration();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ParseInternal(filePath, config, visited);
        return config;
    }

    public static void ParseInternal(string filePath, MushConfiguration config, HashSet<string> visited)
    {
        string fullPath = Path.GetFullPath(filePath);
        if (!visited.Add(fullPath) || !File.Exists(fullPath))
        {
            return;
        }

        string directory = Path.GetDirectoryName(fullPath) ?? "";
        string[] lines = File.ReadAllLines(fullPath);

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            int firstSpace = line.IndexOfAny(new[] { ' ', '\t' });
            string directive;
            string value;

            if (firstSpace == -1)
            {
                directive = line.ToLowerInvariant();
                value = string.Empty;
            }
            else
            {
                directive = line.Substring(0, firstSpace).Trim().ToLowerInvariant();
                value = line.Substring(firstSpace + 1).Trim();
            }

            ProcessDirective(directive, value, directory, config, visited);
        }
    }

    private static void ProcessDirective(string directive, string value, string baseDir, MushConfiguration config, HashSet<string> visited)
    {
        config.Directives[directive] = value;

        switch (directive)
        {
            case "include":
                if (!string.IsNullOrEmpty(value))
                {
                    string target = Path.Combine(baseDir, value);
                    ParseInternal(target, config, visited);
                }
                break;

            case "command_alias":
                ParseAlias(value, config.CommandAliases);
                break;

            case "function_alias":
                ParseAlias(value, config.FunctionAliases);
                break;

            case "flag_rename":
                var renameTokens = value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (renameTokens.Length >= 2)
                    config.FlagRenames[renameTokens[0]] = renameTokens[1];
                break;

            case "reserve_alias":
                if (!string.IsNullOrEmpty(value))
                {
                    config.ReservedAliases.Add(value.Trim());
                }
                break;

            case "restrict_command":
                ParseRestriction(value, config.CommandRestrictions);
                break;

            case "restrict_function":
                ParseRestriction(value, config.FunctionRestrictions);
                break;

            case "restrict_name":
                if (!string.IsNullOrEmpty(value))
                {
                    config.RestrictedNames.Add(value.Trim());
                }
                break;

            case "names_file":
                if (!string.IsNullOrEmpty(value))
                {
                    string namesTarget = Path.Combine(baseDir, value);
                    if (File.Exists(namesTarget))
                    {
                        ParseInternal(namesTarget, config, visited);
                    }
                }
                break;

            case "mud_name":
                config.MudName = value;
                break;

            case "mud_url":
                config.MudUrl = value;
                break;

            case "port":
                if (int.TryParse(value, out int port)) config.Port = port;
                break;

            case "ssl_port":
                if (int.TryParse(value, out int sslPort)) config.SslPort = sslPort;
                break;

            case "ip_addr":
                config.IpAddr = value;
                break;

            case "use_ws":
                config.UseWs = ParseBool(value, true);
                break;

            case "ws_url":
                config.WsUrl = value;
                break;

            case "use_dns":
                config.UseDns = ParseBool(value, true);
                break;

            case "player_creation":
                config.PlayerCreation = ParseBool(value, true);
                break;

            case "guests":
                config.Guests = ParseBool(value, true);
                break;

            case "max_guests":
                if (int.TryParse(value, out int maxGuests)) config.MaxGuests = maxGuests;
                break;

            case "max_players":
                if (int.TryParse(value, out int maxPlayers)) config.MaxPlayers = maxPlayers;
                break;

            case "master_room":
                config.MasterRoom = ParseDbref(value, new Dbref(2));
                break;

            case "system_object":
                config.SystemObject = ParseDbref(value, new Dbref(3));
                break;

            case "speech_mogrifier":
                config.SpeechMogrifier = ParseDbref(value, Dbref.Nothing);
                break;

            case "who_mogrifier":
                config.WhoMogrifier = ParseDbref(value, Dbref.Nothing);
                break;

            case "player_start":
                config.PlayerStart = ParseDbref(value, new Dbref(0));
                break;

            case "base_room":
                config.BaseRoom = ParseDbref(value, new Dbref(0));
                break;

            case "default_home":
                config.DefaultHome = ParseDbref(value, new Dbref(0));
                break;

            case "input_database":
                config.InputDatabase = value;
                break;

            case "output_database":
                config.OutputDatabase = value;
                break;

            case "crash_database":
                config.CrashDatabase = value;
                break;

            case "mail_database":
                config.MailDatabase = value;
                break;

            case "chat_database":
                config.ChatDatabase = value;
                break;

            case "sql_database":
                config.SqlDatabase = value;
                break;

            case "http_handler":
                config.HttpHandler = ParseDbref(value, Dbref.Nothing);
                break;

            case "connect_file":
                config.ConnectFile = value;
                break;

            case "motd_file":
                config.MotdFile = value;
                break;

            case "wizmotd_file":
                config.WizmotdFile = value;
                break;

            case "newuser_file":
                config.NewuserFile = value;
                break;

            case "quit_file":
                config.QuitFile = value;
                break;

            case "down_file":
                config.DownFile = value;
                break;

            case "full_file":
                config.FullFile = value;
                break;

            case "guest_file":
                config.GuestFile = value;
                break;

            case "register_create_file":
                config.RegisterFile = value;
                break;

            case "colors_file":
                config.ColorsFile = value;
                break;

            case "dict_file":
                config.DictFile = value;
                break;

            case "help_command":
                ParseHelpFile(value, adminOnly: false, config.HelpFiles);
                break;

            case "ahelp_command":
                ParseHelpFile(value, adminOnly: true, config.HelpFiles);
                break;

            case "money_singular":
                config.MoneySingular = value;
                break;

            case "money_plural":
                config.MoneyPlural = value;
                break;

            case "starting_money":
                if (int.TryParse(value, out int money)) config.StartingMoney = money;
                break;

            case "paycheck":
                if (int.TryParse(value, out int paycheck)) config.Paycheck = paycheck;
                break;

            case "use_quota":
                config.UseQuota = ParseBool(value, true);
                break;

            case "starting_quota":
                if (int.TryParse(value, out int startingQuota)) config.StartingQuota = startingQuota;
                break;

            case "tiny_trim_fun":
                config.TinyTrimFun = ParseBool(value, false);
                break;

            case "unsafe_eval_functions":
                config.UnsafeEvalFunctions = ParseBool(value, false);
                break;

            case "safer_ufun":
                config.SaferUfun = ParseBool(value, true);
                break;

            case "function_side_effects":
                config.FunctionSideEffects = ParseBool(value, true);
                break;

            case "function_invocation_limit":
                if (int.TryParse(value, out int invLimit)) config.FunctionInvocationLimit = invLimit;
                break;

            case "function_recursion_limit":
                if (int.TryParse(value, out int recLimit)) config.FunctionRecursionLimit = recLimit;
                break;

            case "call_limit":
                if (int.TryParse(value, out int callLimit)) config.CallLimit = callLimit;
                break;

            case "tiny_booleans":
                config.TinyBooleans = ParseBool(value, false);
                break;

            case "tiny_math":
                config.TinyMath = ParseBool(value, false);
                break;

            case "null_eq_zero":
                config.NullEqZero = ParseBool(value, false);
                break;

            case "read_remote_desc":
                config.ReadRemoteDesc = ParseBool(value, false);
                break;

            case "empty_attrs":
                config.EmptyAttrs = ParseBool(value, true);
                break;

            case "object_cost":
                if (int.TryParse(value, out int objCost)) config.ObjectCost = objCost;
                break;

            case "room_cost":
                if (int.TryParse(value, out int roomCost)) config.RoomCost = roomCost;
                break;

            case "exit_cost":
                if (int.TryParse(value, out int exitCost)) config.ExitCost = exitCost;
                break;

            case "link_cost":
                if (int.TryParse(value, out int linkCost)) config.LinkCost = linkCost;
                break;

            case "find_cost":
                if (int.TryParse(value, out int findCost)) config.FindCost = findCost;
                break;

            case "queue_cost":
                if (int.TryParse(value, out int queueCost)) config.QueueCost = queueCost;
                break;

            case "quota_cost":
                if (int.TryParse(value, out int quotaCost)) config.QuotaCost = quotaCost;
                break;

            case "max_pennies":
                if (int.TryParse(value, out int maxPennies)) config.MaxPennies = maxPennies;
                break;

            case "max_aliases":
                if (int.TryParse(value, out int maxAliases)) config.MaxAliases = maxAliases;
                break;

            case "max_attrs_per_obj":
                if (int.TryParse(value, out int maxAttrs)) config.MaxAttrsPerObj = maxAttrs;
                break;

            case "max_channels":
                if (int.TryParse(value, out int maxChannels)) config.MaxChannels = maxChannels;
                break;

            case "max_player_chans":
                if (int.TryParse(value, out int maxPlayerChans)) config.MaxPlayerChans = maxPlayerChans;
                break;

            case "max_dbref":
                if (int.TryParse(value, out int maxDbref)) config.MaxDbref = maxDbref;
                break;

            case "max_depth":
                if (int.TryParse(value, out int maxDepth)) config.MaxDepth = maxDepth;
                break;

            case "max_parents":
                if (int.TryParse(value, out int maxParents)) config.MaxParents = maxParents;
                break;

            case "max_named_qregs":
                if (int.TryParse(value, out int maxQregs)) config.MaxNamedQregs = maxQregs;
                break;

            case "player_queue_limit":
                if (int.TryParse(value, out int pQueueLimit)) config.PlayerQueueLimit = pQueueLimit;
                break;

            case "queue_chunk":
                if (int.TryParse(value, out int queueChunk)) config.QueueChunk = queueChunk;
                break;

            case "mail_limit":
                if (int.TryParse(value, out int mailLimit)) config.MailLimit = mailLimit;
                break;

            case "logins":
                config.Logins = ParseBool(value, true);
                break;

            case "max_logins":
                if (int.TryParse(value, out int maxLogins)) config.MaxLogins = maxLogins;
                break;

            case "connect_fail_limit":
                if (int.TryParse(value, out int failLimit)) config.ConnectFailLimit = failLimit;
                break;

            case "idle_timeout":
                config.IdleTimeout = ParseDurationSeconds(value, 0);
                break;

            case "unconnected_idle_timeout":
                config.UnconnectedIdleTimeout = ParseDurationSeconds(value, 300);
                break;

            case "player_name_len":
                if (int.TryParse(value, out int nameLen)) config.PlayerNameLen = nameLen;
                break;

            case "player_name_spaces":
                config.PlayerNameSpaces = ParseBool(value, false);
                break;

            case "only_ascii_in_names":
                config.OnlyAsciiInNames = ParseBool(value, true);
                break;

            case "exits_connect_rooms":
                config.ExitsConnectRooms = ParseBool(value, false);
                break;

            case "link_to_object":
                config.LinkToObject = ParseBool(value, true);
                break;

            case "owner_queues":
                config.OwnerQueues = ParseBool(value, false);
                break;

            case "possessive_get":
                config.PossessiveGet = ParseBool(value, true);
                break;

            case "possessive_get_d":
                config.PossessiveGetD = ParseBool(value, false);
                break;

            case "really_safe":
                config.ReallySafe = ParseBool(value, true);
                break;

            case "destroy_possessions":
                config.DestroyPossessions = ParseBool(value, true);
                break;

            case "adestroy":
                config.Adestroy = ParseBool(value, false);
                break;

            case "amail":
                config.Amail = ParseBool(value, false);
                break;

            case "startups":
                config.Startups = ParseBool(value, true);
                break;

            case "room_connects":
                config.RoomConnects = ParseBool(value, true);
                break;

            case "player_listen":
                config.PlayerListen = ParseBool(value, true);
                break;

            case "player_ahear":
                config.PlayerAhear = ParseBool(value, true);
                break;

            case "noisy_whisper":
                config.NoisyWhisper = ParseBool(value, false);
                break;

            case "full_invis":
                config.FullInvis = ParseBool(value, false);
                break;

            case "wiz_noaenter":
                config.WizNoAenter = ParseBool(value, false);
                break;

            case "silent_pemit":
                config.SilentPemit = ParseBool(value, false);
                break;

            case "whisper_loudness":
                if (int.TryParse(value, out int whisperLoud)) config.WhisperLoudness = whisperLoud;
                break;

            case "zone_control_zmp_only":
                config.ZoneControlZmpOnly = ParseBool(value, true);
                break;

            case "ex_public_attribs":
                config.ExPublicAttribs = ParseBool(value, true);
                break;

            case "flags_on_examine":
                config.FlagsOnExamine = ParseBool(value, true);
                break;

            case "float_precision":
                if (int.TryParse(value, out int floatPrec)) config.FloatPrecision = floatPrec;
                break;

            case "comma_exit_list":
                config.CommaExitList = ParseBool(value, true);
                break;

            case "ansi_names":
                config.AnsiNames = ParseBool(value, true);
                break;

            case "count_all":
                config.CountAll = ParseBool(value, false);
                break;

            case "monikers":
                config.Monikers = value;
                break;

            case "chan_cost":
                if (int.TryParse(value, out int chanCost)) config.ChanCost = chanCost;
                break;

            case "chan_title_len":
                if (int.TryParse(value, out int chanTitleLen)) config.ChanTitleLen = chanTitleLen;
                break;

            case "noisy_cemit":
                config.NoisyCemit = ParseBool(value, false);
                break;

            case "chat_strip_quote":
                config.ChatStripQuote = ParseBool(value, true);
                break;

            case "use_muxcomm":
                config.UseMuxcomm = ParseBool(value, false);
                break;

            case "channel_flags":
                config.ChannelFlags = value;
                break;

            case "ancestor_room":
                config.AncestorRoom = ParseDbref(value, Dbref.Nothing);
                break;

            case "ancestor_thing":
                config.AncestorThing = ParseDbref(value, Dbref.Nothing);
                break;

            case "ancestor_exit":
                config.AncestorExit = ParseDbref(value, Dbref.Nothing);
                break;

            case "ancestor_player":
                config.AncestorPlayer = ParseDbref(value, Dbref.Nothing);
                break;

            case "player_flags":
                config.PlayerFlags = AppendFlagList(config.PlayerFlags, value);
                break;

            case "room_flags":
                config.RoomFlags = AppendFlagList(config.RoomFlags, value);
                break;

            case "exit_flags":
                config.ExitFlags = AppendFlagList(config.ExitFlags, value);
                break;

            case "thing_flags":
                config.ThingFlags = AppendFlagList(config.ThingFlags, value);
                break;

            case "http_per_second":
                if (int.TryParse(value, out int httpRate)) config.HttpPerSecond = httpRate;
                break;

            case "event_handler":
                config.EventHandler = ParseDbref(value, Dbref.Nothing);
                break;

            case "access_file":
                config.AccessFile = value;
                break;

            case "dump_interval":
                config.DumpInterval = ParseDurationSeconds(value, 3600);
                break;

            case "dbck_interval":
                config.DbckInterval = ParseDurationSeconds(value, 599);
                break;

            case "purge_interval":
                config.PurgeInterval = ParseDurationSeconds(value, 601);
                break;

            case "warn_interval":
                config.WarnInterval = ParseDurationSeconds(value, 3600);
                break;

            case "connect_html_file":
                config.ConnectHtmlFile = value;
                break;

            case "motd_html_file":
                config.MotdHtmlFile = value;
                break;

            case "wizmotd_html_file":
                config.WizmotdHtmlFile = value;
                break;

            case "newuser_html_file":
                config.NewuserHtmlFile = value;
                break;

            case "quit_html_file":
                config.QuitHtmlFile = value;
                break;

            case "down_html_file":
                config.DownHtmlFile = value;
                break;

            case "full_html_file":
                config.FullHtmlFile = value;
                break;

            case "guest_html_file":
                config.GuestHtmlFile = value;
                break;

            case "register_create_html_file":
                config.RegisterHtmlFile = value;
                break;

            case "use_connlog":
                config.UseConnlog = ParseBool(value, true);
                break;

            case "connlog_db":
                config.ConnlogDb = value;
                break;

            case "error_log":
                config.ErrorLog = value;
                break;

            case "connect_log":
                config.ConnectLog = value;
                break;

            case "wizard_log":
                config.WizardLog = value;
                break;

            case "checkpt_log":
                config.CheckptLog = value;
                break;

            case "trace_log":
                config.TraceLog = value;
                break;

            case "command_log":
                config.CommandLog = value;
                break;

            case "log_commands":
                config.LogCommands = ParseBool(value, false);
                break;

            case "log_forces":
                config.LogForces = ParseBool(value, false);
                break;

            case "log_wipe_passwd":
                config.LogWipePasswd = value;
                break;

            case "log_max_size":
                if (int.TryParse(value, out int logMax)) config.LogMaxSize = logMax;
                break;

            case "log_size_policy":
                config.LogSizePolicy = value;
                break;

            case "pueblo":
                config.Pueblo = ParseBool(value, false);
                break;

            case "sql_platform":
                config.SqlPlatform = value;
                break;

            case "sql_host":
                config.SqlHost = value;
                break;

            case "sql_username":
                config.SqlUsername = value;
                break;

            case "sql_password":
                config.SqlPassword = value;
                break;

            case "ssl_ip_addr":
                config.SslIpAddr = value;
                break;

            case "ssl_certificate_file":
                config.SslCertificateFile = value;
                break;

            case "ssl_private_key_file":
                config.SslPrivateKeyFile = value;
                break;

            case "ssl_ca_file":
                config.SslCaFile = value;
                break;

            case "ssl_ca_dir":
                config.SslCaDir = value;
                break;

            case "ssl_require_client_cert":
                config.SslRequireClientCert = ParseBool(value, false);
                break;

            case "mssp":
                int slashIdx = value.IndexOf('/');
                if (slashIdx > 0)
                    config.MsspFields[value[..slashIdx].Trim()] = value[(slashIdx + 1)..].Trim();
                break;

            case "config_save":
                config.ConfigSave = ParseBool(value, false);
                break;

            default:
                config.RawDirectives[directive] = value;
                break;
        }
    }

    private static void ParseAlias(string value, Dictionary<string, string> target)
    {
        var tokens = value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length >= 2)
        {
            string original = tokens[0];
            string alias = tokens[1];
            target[alias] = original;
        }
    }

    private static void ParseHelpFile(string value, bool adminOnly, List<HelpFileDirective> target)
    {
        var tokens = value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length >= 2)
        {
            target.Add(new HelpFileDirective(tokens[0], tokens[1], adminOnly));
        }
    }

    private static void ParseRestriction(string value, Dictionary<string, List<RestrictionRule>> target)
    {
        string? errorMessage = null;
        string cleanValue = value;
        int quoteIdx = value.IndexOf('"');
        if (quoteIdx != -1)
        {
            cleanValue = value.Substring(0, quoteIdx).Trim();
            errorMessage = value.Substring(quoteIdx + 1).Trim();
        }

        var tokens = cleanValue.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length >= 2)
        {
            string item = tokens[0];
            if (!target.TryGetValue(item, out var list))
            {
                list = new List<RestrictionRule>();
                target[item] = list;
            }

            for (int i = 1; i < tokens.Length; i++)
            {
                list.Add(new RestrictionRule(tokens[i], errorMessage));
            }
        }
    }

    private static string AppendFlagList(string existing, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return existing;
        return string.IsNullOrWhiteSpace(existing) ? value.Trim() : $"{existing} {value.Trim()}";
    }

    private static bool ParseBool(string val, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(val)) return defaultValue;
        string v = val.Trim().ToLowerInvariant();
        return v is "yes" or "true" or "1" or "on";
    }

    private static int ParseDurationSeconds(string val, int fallback)
    {
        if (string.IsNullOrWhiteSpace(val)) return fallback;
        string s = val.Trim().ToLowerInvariant();
        if (int.TryParse(s, out int plain)) return plain;

        int total = 0;
        int num = 0;
        bool hasDigit = false;
        foreach (char c in s)
        {
            if (char.IsDigit(c))
            {
                num = num * 10 + (c - '0');
                hasDigit = true;
            }
            else if (hasDigit)
            {
                total += c switch
                {
                    'd' => num * 86400,
                    'h' => num * 3600,
                    'm' => num * 60,
                    's' => num,
                    _ => 0
                };
                num = 0;
                hasDigit = false;
            }
        }
        return total + (hasDigit ? num : 0);
    }

    private static Dbref ParseDbref(string val, Dbref fallback)
    {
        if (string.IsNullOrWhiteSpace(val)) return fallback;
        string s = val.Trim();
        if (s.StartsWith('#') && int.TryParse(s.Substring(1), out int num))
        {
            return new Dbref(num);
        }
        if (int.TryParse(s, out int n))
        {
            return new Dbref(n);
        }
        return fallback;
    }
}
