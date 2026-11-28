namespace PennMUSH.Core.Configuration;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Types;

public enum ConfigKind
{
    Text,
    Number,
    YesNo,
    Dbref
}

public sealed class ConfigOption
{
    public required string Name { get; init; }
    public required string Group { get; init; }
    public required ConfigKind Kind { get; init; }
    public required Func<MushConfiguration, string> Read { get; init; }
    public required Func<MushConfiguration, string, bool> Write { get; init; }
}

public sealed class ConfigGroup
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public bool Royalty { get; init; }
}

/// <summary>
/// The configuration options this server already stores, in the original group names.
/// Ancestor dbrefs are their own group, so @config ancestors lists them apart from the other database options.
/// </summary>
public static class ConfigCatalog
{
    public static readonly IReadOnlyList<ConfigGroup> Groups =
    [
        new() { Name = "ancestors", Description = "Default ancestor objects" },
        new() { Name = "cosmetic", Description = "Cosmetic options" },
        new() { Name = "db", Description = "Database options" },
        new() { Name = "files", Description = "Files used by the MUSH", Royalty = true },
        new() { Name = "limits", Description = "Limits and other constants" },
        new() { Name = "messages", Description = "Message files sent by the MUSH", Royalty = true },
        new() { Name = "net", Description = "Networking and connection-related options" },
    ];

    public static readonly IReadOnlyList<ConfigOption> Options = Build();

    public static bool CanSee(ConfigGroup group, bool royalty) => !group.Royalty || royalty;

    public static ConfigOption? Find(string name)
    {
        foreach (var option in Options)
        {
            if (option.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return option;
        }
        return null;
    }

    public static bool TrySet(MushConfiguration config, string name, string value)
    {
        var option = Find(name);
        if (option == null || !option.Write(config, value))
            return false;
        config.Directives[option.Name] = option.Kind == ConfigKind.YesNo
            ? (option.Read(config) == "Yes" ? "yes" : "no")
            : value;
        return true;
    }

    private static List<ConfigOption> Build()
    {
        return
        [
            YesNo("ansi_names", "cosmetic", c => c.AnsiNames, (c, v) => c.AnsiNames = v),
            YesNo("comma_exit_list", "cosmetic", c => c.CommaExitList, (c, v) => c.CommaExitList = v),
            YesNo("count_all", "cosmetic", c => c.CountAll, (c, v) => c.CountAll = v),
            YesNo("ex_public_attribs", "cosmetic", c => c.ExPublicAttribs, (c, v) => c.ExPublicAttribs = v),
            YesNo("flags_on_examine", "cosmetic", c => c.FlagsOnExamine, (c, v) => c.FlagsOnExamine = v),
            Number("float_precision", "cosmetic", c => c.FloatPrecision, (c, v) => c.FloatPrecision = v),
            Text("monikers", "cosmetic", c => c.Monikers, (c, v) => c.Monikers = v),
            Text("money_singular", "cosmetic", c => c.MoneySingular, (c, v) => c.MoneySingular = v),
            Text("money_plural", "cosmetic", c => c.MoneyPlural, (c, v) => c.MoneyPlural = v),
            YesNo("only_ascii_in_names", "cosmetic", c => c.OnlyAsciiInNames, (c, v) => c.OnlyAsciiInNames = v),
            YesNo("player_name_spaces", "cosmetic", c => c.PlayerNameSpaces, (c, v) => c.PlayerNameSpaces = v),
            YesNo("read_remote_desc", "cosmetic", c => c.ReadRemoteDesc, (c, v) => c.ReadRemoteDesc = v),
            YesNo("tiny_booleans", "cosmetic", c => c.TinyBooleans, (c, v) => c.TinyBooleans = v),
            YesNo("tiny_math", "cosmetic", c => c.TinyMath, (c, v) => c.TinyMath = v),
            YesNo("tiny_trim_fun", "cosmetic", c => c.TinyTrimFun, (c, v) => c.TinyTrimFun = v),
            YesNo("empty_attrs", "db", c => c.EmptyAttrs, (c, v) => c.EmptyAttrs = v),
            YesNo("null_eq_zero", "cosmetic", c => c.NullEqZero, (c, v) => c.NullEqZero = v),
            Db("ancestor_exit", "ancestors", c => c.AncestorExit, (c, v) => c.AncestorExit = v),
            Db("ancestor_player", "ancestors", c => c.AncestorPlayer, (c, v) => c.AncestorPlayer = v),
            Db("ancestor_room", "ancestors", c => c.AncestorRoom, (c, v) => c.AncestorRoom = v),
            Db("ancestor_thing", "ancestors", c => c.AncestorThing, (c, v) => c.AncestorThing = v),
            Db("base_room", "db", c => c.BaseRoom, (c, v) => c.BaseRoom = v),
            Db("default_home", "db", c => c.DefaultHome, (c, v) => c.DefaultHome = v),
            Db("event_handler", "db", c => c.EventHandler, (c, v) => c.EventHandler = v),
            Db("master_room", "db", c => c.MasterRoom, (c, v) => c.MasterRoom = v),
            Db("speech_mogrifier", "db", c => c.SpeechMogrifier, (c, v) => c.SpeechMogrifier = v),
            Db("who_mogrifier", "db", c => c.WhoMogrifier, (c, v) => c.WhoMogrifier = v),
            Db("system_object", "db", c => c.SystemObject, (c, v) => c.SystemObject = v),
            Db("player_start", "db", c => c.PlayerStart, (c, v) => c.PlayerStart = v),
            Text("access_file", "files", c => c.AccessFile, (c, v) => c.AccessFile = v),
            Text("chat_database", "files", c => c.ChatDatabase, (c, v) => c.ChatDatabase = v),
            Text("checkpt_log", "files", c => c.CheckptLog, (c, v) => c.CheckptLog = v),
            Text("command_log", "files", c => c.CommandLog, (c, v) => c.CommandLog = v),
            Text("connect_log", "files", c => c.ConnectLog, (c, v) => c.ConnectLog = v),
            Text("connlog_db", "files", c => c.ConnlogDb, (c, v) => c.ConnlogDb = v),
            Text("dict_file", "files", c => c.DictFile, (c, v) => c.DictFile = v),
            Text("error_log", "files", c => c.ErrorLog, (c, v) => c.ErrorLog = v),
            Text("sql_database", "files", c => c.SqlDatabase, (c, v) => c.SqlDatabase = v),
            Text("sql_host", "files", c => c.SqlHost, (c, v) => c.SqlHost = v),
            Text("sql_password", "files", c => c.SqlPassword, (c, v) => c.SqlPassword = v),
            Text("sql_platform", "files", c => c.SqlPlatform, (c, v) => c.SqlPlatform = v),
            Text("sql_username", "files", c => c.SqlUsername, (c, v) => c.SqlUsername = v),
            Text("ssl_ca_dir", "files", c => c.SslCaDir, (c, v) => c.SslCaDir = v),
            Text("ssl_ca_file", "files", c => c.SslCaFile, (c, v) => c.SslCaFile = v),
            Text("ssl_certificate_file", "files", c => c.SslCertificateFile, (c, v) => c.SslCertificateFile = v),
            Text("ssl_private_key_file", "files", c => c.SslPrivateKeyFile, (c, v) => c.SslPrivateKeyFile = v),
            Text("trace_log", "files", c => c.TraceLog, (c, v) => c.TraceLog = v),
            Text("wizard_log", "files", c => c.WizardLog, (c, v) => c.WizardLog = v),
            Db("http_handler", "db", c => c.HttpHandler, (c, v) => c.HttpHandler = v),
            Text("crash_database", "files", c => c.CrashDatabase, (c, v) => c.CrashDatabase = v),
            Text("input_database", "files", c => c.InputDatabase, (c, v) => c.InputDatabase = v),
            Text("mail_database", "files", c => c.MailDatabase, (c, v) => c.MailDatabase = v),
            Text("output_database", "files", c => c.OutputDatabase, (c, v) => c.OutputDatabase = v),
            YesNo("adestroy", "limits", c => c.Adestroy, (c, v) => c.Adestroy = v),
            YesNo("amail", "limits", c => c.Amail, (c, v) => c.Amail = v),
            Number("call_limit", "limits", c => c.CallLimit, (c, v) => c.CallLimit = v),
            Number("chan_cost", "limits", c => c.ChanCost, (c, v) => c.ChanCost = v),
            Number("chan_title_len", "limits", c => c.ChanTitleLen, (c, v) => c.ChanTitleLen = v),
            Text("channel_flags", "limits", c => c.ChannelFlags, (c, v) => c.ChannelFlags = v),
            YesNo("chat_strip_quote", "limits", c => c.ChatStripQuote, (c, v) => c.ChatStripQuote = v),
            Number("connect_fail_limit", "limits", c => c.ConnectFailLimit, (c, v) => c.ConnectFailLimit = v),
            YesNo("destroy_possessions", "limits", c => c.DestroyPossessions, (c, v) => c.DestroyPossessions = v),
            Number("exit_cost", "limits", c => c.ExitCost, (c, v) => c.ExitCost = v),
            Text("exit_flags", "limits", c => c.ExitFlags, (c, v) => c.ExitFlags = v),
            YesNo("exits_connect_rooms", "limits", c => c.ExitsConnectRooms, (c, v) => c.ExitsConnectRooms = v),
            Number("find_cost", "limits", c => c.FindCost, (c, v) => c.FindCost = v),
            YesNo("full_invis", "limits", c => c.FullInvis, (c, v) => c.FullInvis = v),
            Number("function_invocation_limit", "limits", c => c.FunctionInvocationLimit, (c, v) => c.FunctionInvocationLimit = v),
            Number("function_recursion_limit", "limits", c => c.FunctionRecursionLimit, (c, v) => c.FunctionRecursionLimit = v),
            YesNo("function_side_effects", "limits", c => c.FunctionSideEffects, (c, v) => c.FunctionSideEffects = v),
            Number("idle_timeout", "limits", c => c.IdleTimeout, (c, v) => c.IdleTimeout = v),
            Number("link_cost", "limits", c => c.LinkCost, (c, v) => c.LinkCost = v),
            YesNo("link_to_object", "limits", c => c.LinkToObject, (c, v) => c.LinkToObject = v),
            Number("mail_limit", "limits", c => c.MailLimit, (c, v) => c.MailLimit = v),
            Number("max_aliases", "limits", c => c.MaxAliases, (c, v) => c.MaxAliases = v),
            Number("max_attrs_per_obj", "limits", c => c.MaxAttrsPerObj, (c, v) => c.MaxAttrsPerObj = v),
            Number("max_channels", "limits", c => c.MaxChannels, (c, v) => c.MaxChannels = v),
            Number("max_dbref", "limits", c => c.MaxDbref, (c, v) => c.MaxDbref = v),
            Number("max_depth", "limits", c => c.MaxDepth, (c, v) => c.MaxDepth = v),
            Number("max_guests", "limits", c => c.MaxGuests, (c, v) => c.MaxGuests = v),
            Number("max_logins", "limits", c => c.MaxLogins, (c, v) => c.MaxLogins = v),
            Number("max_named_qregs", "limits", c => c.MaxNamedQregs, (c, v) => c.MaxNamedQregs = v),
            Number("max_parents", "limits", c => c.MaxParents, (c, v) => c.MaxParents = v),
            Number("max_pennies", "limits", c => c.MaxPennies, (c, v) => c.MaxPennies = v),
            Number("max_player_chans", "limits", c => c.MaxPlayerChans, (c, v) => c.MaxPlayerChans = v),
            Number("max_players", "limits", c => c.MaxPlayers, (c, v) => c.MaxPlayers = v),
            YesNo("noisy_cemit", "limits", c => c.NoisyCemit, (c, v) => c.NoisyCemit = v),
            YesNo("noisy_whisper", "limits", c => c.NoisyWhisper, (c, v) => c.NoisyWhisper = v),
            Number("object_cost", "limits", c => c.ObjectCost, (c, v) => c.ObjectCost = v),
            YesNo("owner_queues", "limits", c => c.OwnerQueues, (c, v) => c.OwnerQueues = v),
            Number("paycheck", "limits", c => c.Paycheck, (c, v) => c.Paycheck = v),
            YesNo("player_ahear", "limits", c => c.PlayerAhear, (c, v) => c.PlayerAhear = v),
            Text("player_flags", "limits", c => c.PlayerFlags, (c, v) => c.PlayerFlags = v),
            YesNo("player_listen", "limits", c => c.PlayerListen, (c, v) => c.PlayerListen = v),
            Number("player_name_len", "limits", c => c.PlayerNameLen, (c, v) => c.PlayerNameLen = v),
            Number("player_queue_limit", "limits", c => c.PlayerQueueLimit, (c, v) => c.PlayerQueueLimit = v),
            YesNo("possessive_get", "limits", c => c.PossessiveGet, (c, v) => c.PossessiveGet = v),
            YesNo("possessive_get_d", "limits", c => c.PossessiveGetD, (c, v) => c.PossessiveGetD = v),
            Number("queue_chunk", "limits", c => c.QueueChunk, (c, v) => c.QueueChunk = v),
            Number("queue_cost", "limits", c => c.QueueCost, (c, v) => c.QueueCost = v),
            Number("quota_cost", "limits", c => c.QuotaCost, (c, v) => c.QuotaCost = v),
            YesNo("really_safe", "limits", c => c.ReallySafe, (c, v) => c.ReallySafe = v),
            YesNo("room_connects", "limits", c => c.RoomConnects, (c, v) => c.RoomConnects = v),
            Number("room_cost", "limits", c => c.RoomCost, (c, v) => c.RoomCost = v),
            Text("room_flags", "limits", c => c.RoomFlags, (c, v) => c.RoomFlags = v),
            YesNo("safer_ufun", "limits", c => c.SaferUfun, (c, v) => c.SaferUfun = v),
            YesNo("silent_pemit", "limits", c => c.SilentPemit, (c, v) => c.SilentPemit = v),
            Number("starting_money", "limits", c => c.StartingMoney, (c, v) => c.StartingMoney = v),
            Number("starting_quota", "limits", c => c.StartingQuota, (c, v) => c.StartingQuota = v),
            YesNo("startups", "limits", c => c.Startups, (c, v) => c.Startups = v),
            Text("thing_flags", "limits", c => c.ThingFlags, (c, v) => c.ThingFlags = v),
            Number("unconnected_idle_timeout", "limits", c => c.UnconnectedIdleTimeout, (c, v) => c.UnconnectedIdleTimeout = v),
            YesNo("unsafe_eval_functions", "limits", c => c.UnsafeEvalFunctions, (c, v) => c.UnsafeEvalFunctions = v),
            YesNo("use_muxcomm", "limits", c => c.UseMuxcomm, (c, v) => c.UseMuxcomm = v),
            Number("dbck_interval", "limits", c => c.DbckInterval, (c, v) => c.DbckInterval = v),
            Number("dump_interval", "limits", c => c.DumpInterval, (c, v) => c.DumpInterval = v),
            Number("http_per_second", "limits", c => c.HttpPerSecond, (c, v) => c.HttpPerSecond = v),
            YesNo("log_commands", "limits", c => c.LogCommands, (c, v) => c.LogCommands = v),
            YesNo("log_forces", "limits", c => c.LogForces, (c, v) => c.LogForces = v),
            Number("log_max_size", "limits", c => c.LogMaxSize, (c, v) => c.LogMaxSize = v),
            Text("log_size_policy", "limits", c => c.LogSizePolicy, (c, v) => c.LogSizePolicy = v),
            Text("log_wipe_passwd", "limits", c => c.LogWipePasswd, (c, v) => c.LogWipePasswd = v),
            Number("purge_interval", "limits", c => c.PurgeInterval, (c, v) => c.PurgeInterval = v),
            YesNo("use_connlog", "limits", c => c.UseConnlog, (c, v) => c.UseConnlog = v),
            Number("warn_interval", "limits", c => c.WarnInterval, (c, v) => c.WarnInterval = v),
            YesNo("use_quota", "limits", c => c.UseQuota, (c, v) => c.UseQuota = v),
            Number("whisper_loudness", "limits", c => c.WhisperLoudness, (c, v) => c.WhisperLoudness = v),
            YesNo("wiz_noaenter", "limits", c => c.WizNoAenter, (c, v) => c.WizNoAenter = v),
            YesNo("zone_control_zmp_only", "limits", c => c.ZoneControlZmpOnly, (c, v) => c.ZoneControlZmpOnly = v),
            Text("colors_file", "messages", c => c.ColorsFile, (c, v) => c.ColorsFile = v),
            Text("connect_file", "messages", c => c.ConnectFile, (c, v) => c.ConnectFile = v),
            Text("connect_html_file", "messages", c => c.ConnectHtmlFile, (c, v) => c.ConnectHtmlFile = v),
            Text("down_file", "messages", c => c.DownFile, (c, v) => c.DownFile = v),
            Text("down_html_file", "messages", c => c.DownHtmlFile, (c, v) => c.DownHtmlFile = v),
            Text("full_file", "messages", c => c.FullFile, (c, v) => c.FullFile = v),
            Text("full_html_file", "messages", c => c.FullHtmlFile, (c, v) => c.FullHtmlFile = v),
            Text("guest_file", "messages", c => c.GuestFile, (c, v) => c.GuestFile = v),
            Text("guest_html_file", "messages", c => c.GuestHtmlFile, (c, v) => c.GuestHtmlFile = v),
            Text("motd_file", "messages", c => c.MotdFile, (c, v) => c.MotdFile = v),
            Text("motd_html_file", "messages", c => c.MotdHtmlFile, (c, v) => c.MotdHtmlFile = v),
            Text("newuser_file", "messages", c => c.NewuserFile, (c, v) => c.NewuserFile = v),
            Text("newuser_html_file", "messages", c => c.NewuserHtmlFile, (c, v) => c.NewuserHtmlFile = v),
            Text("quit_file", "messages", c => c.QuitFile, (c, v) => c.QuitFile = v),
            Text("quit_html_file", "messages", c => c.QuitHtmlFile, (c, v) => c.QuitHtmlFile = v),
            Text("register_create_file", "messages", c => c.RegisterFile, (c, v) => c.RegisterFile = v),
            Text("register_create_html_file", "messages", c => c.RegisterHtmlFile, (c, v) => c.RegisterHtmlFile = v),
            Text("wizmotd_file", "messages", c => c.WizmotdFile, (c, v) => c.WizmotdFile = v),
            Text("wizmotd_html_file", "messages", c => c.WizmotdHtmlFile, (c, v) => c.WizmotdHtmlFile = v),
            YesNo("guests", "net", c => c.Guests, (c, v) => c.Guests = v),
            Text("ip_addr", "net", c => c.IpAddr, (c, v) => c.IpAddr = v),
            YesNo("logins", "net", c => c.Logins, (c, v) => c.Logins = v),
            Text("mud_name", "net", c => c.MudName, (c, v) => c.MudName = v),
            Text("mud_url", "net", c => c.MudUrl, (c, v) => c.MudUrl = v),
            YesNo("player_creation", "net", c => c.PlayerCreation, (c, v) => c.PlayerCreation = v),
            Number("port", "net", c => c.Port, (c, v) => c.Port = v),
            YesNo("pueblo", "net", c => c.Pueblo, (c, v) => c.Pueblo = v),
            Text("ssl_ip_addr", "net", c => c.SslIpAddr, (c, v) => c.SslIpAddr = v),
            Number("ssl_port", "net", c => c.SslPort, (c, v) => c.SslPort = v),
            YesNo("ssl_require_client_cert", "net", c => c.SslRequireClientCert, (c, v) => c.SslRequireClientCert = v),
            YesNo("use_dns", "net", c => c.UseDns, (c, v) => c.UseDns = v),
            YesNo("use_ws", "net", c => c.UseWs, (c, v) => c.UseWs = v),
            Text("ws_url", "net", c => c.WsUrl, (c, v) => c.WsUrl = v),
        ];
    }

    private static ConfigOption Text(string name, string group, Func<MushConfiguration, string> read, Action<MushConfiguration, string> write)
        => new()
        {
            Name = name,
            Group = group,
            Kind = ConfigKind.Text,
            Read = read,
            Write = (config, value) => { write(config, value); return true; }
        };

    private static ConfigOption Number(string name, string group, Func<MushConfiguration, int> read, Action<MushConfiguration, int> write)
        => new()
        {
            Name = name,
            Group = group,
            Kind = ConfigKind.Number,
            Read = config => read(config).ToString(),
            Write = (config, value) =>
            {
                if (!int.TryParse(value, out int number))
                    return false;
                write(config, number);
                return true;
            }
        };

    private static ConfigOption YesNo(string name, string group, Func<MushConfiguration, bool> read, Action<MushConfiguration, bool> write)
        => new()
        {
            Name = name,
            Group = group,
            Kind = ConfigKind.YesNo,
            Read = config => read(config) ? "Yes" : "No",
            Write = (config, value) =>
            {
                if (!TryBool(value, out bool enabled))
                    return false;
                write(config, enabled);
                return true;
            }
        };

    private static ConfigOption Db(string name, string group, Func<MushConfiguration, Dbref> read, Action<MushConfiguration, Dbref> write)
        => new()
        {
            Name = name,
            Group = group,
            Kind = ConfigKind.Dbref,
            Read = config => "#" + read(config).Value,
            Write = (config, value) =>
            {
                string text = value.StartsWith('#') ? value[1..] : value;
                if (!int.TryParse(text, out int number))
                    return false;
                write(config, new Dbref(number));
                return true;
            }
        };

    private static bool TryBool(string value, out bool enabled)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "yes":
            case "on":
            case "true":
            case "1":
                enabled = true;
                return true;
            case "no":
            case "off":
            case "false":
            case "0":
                enabled = false;
                return true;
            default:
                enabled = false;
                return false;
        }
    }
}
