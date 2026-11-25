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
/// </summary>
public static class ConfigCatalog
{
    public static readonly IReadOnlyList<ConfigGroup> Groups =
    [
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
            Text("money_singular", "cosmetic", c => c.MoneySingular, (c, v) => c.MoneySingular = v),
            Text("money_plural", "cosmetic", c => c.MoneyPlural, (c, v) => c.MoneyPlural = v),
            YesNo("tiny_trim_fun", "cosmetic", c => c.TinyTrimFun, (c, v) => c.TinyTrimFun = v),
            Db("base_room", "db", c => c.BaseRoom, (c, v) => c.BaseRoom = v),
            Db("default_home", "db", c => c.DefaultHome, (c, v) => c.DefaultHome = v),
            Db("master_room", "db", c => c.MasterRoom, (c, v) => c.MasterRoom = v),
            Db("system_object", "db", c => c.SystemObject, (c, v) => c.SystemObject = v),
            Db("player_start", "db", c => c.PlayerStart, (c, v) => c.PlayerStart = v),
            Text("chat_database", "files", c => c.ChatDatabase, (c, v) => c.ChatDatabase = v),
            Text("sql_database", "files", c => c.SqlDatabase, (c, v) => c.SqlDatabase = v),
            Db("http_handler", "db", c => c.HttpHandler, (c, v) => c.HttpHandler = v),
            Text("crash_database", "files", c => c.CrashDatabase, (c, v) => c.CrashDatabase = v),
            Text("input_database", "files", c => c.InputDatabase, (c, v) => c.InputDatabase = v),
            Text("mail_database", "files", c => c.MailDatabase, (c, v) => c.MailDatabase = v),
            Text("output_database", "files", c => c.OutputDatabase, (c, v) => c.OutputDatabase = v),
            Number("max_guests", "limits", c => c.MaxGuests, (c, v) => c.MaxGuests = v),
            Number("max_players", "limits", c => c.MaxPlayers, (c, v) => c.MaxPlayers = v),
            Number("paycheck", "limits", c => c.Paycheck, (c, v) => c.Paycheck = v),
            Number("starting_money", "limits", c => c.StartingMoney, (c, v) => c.StartingMoney = v),
            Number("starting_quota", "limits", c => c.StartingQuota, (c, v) => c.StartingQuota = v),
            YesNo("use_quota", "limits", c => c.UseQuota, (c, v) => c.UseQuota = v),
            Text("colors_file", "messages", c => c.ColorsFile, (c, v) => c.ColorsFile = v),
            Text("connect_file", "messages", c => c.ConnectFile, (c, v) => c.ConnectFile = v),
            Text("down_file", "messages", c => c.DownFile, (c, v) => c.DownFile = v),
            Text("full_file", "messages", c => c.FullFile, (c, v) => c.FullFile = v),
            Text("guest_file", "messages", c => c.GuestFile, (c, v) => c.GuestFile = v),
            Text("motd_file", "messages", c => c.MotdFile, (c, v) => c.MotdFile = v),
            Text("newuser_file", "messages", c => c.NewuserFile, (c, v) => c.NewuserFile = v),
            Text("quit_file", "messages", c => c.QuitFile, (c, v) => c.QuitFile = v),
            Text("wizmotd_file", "messages", c => c.WizmotdFile, (c, v) => c.WizmotdFile = v),
            YesNo("guests", "net", c => c.Guests, (c, v) => c.Guests = v),
            Text("ip_addr", "net", c => c.IpAddr, (c, v) => c.IpAddr = v),
            Text("mud_name", "net", c => c.MudName, (c, v) => c.MudName = v),
            Text("mud_url", "net", c => c.MudUrl, (c, v) => c.MudUrl = v),
            YesNo("player_creation", "net", c => c.PlayerCreation, (c, v) => c.PlayerCreation = v),
            Number("port", "net", c => c.Port, (c, v) => c.Port = v),
            Number("ssl_port", "net", c => c.SslPort, (c, v) => c.SslPort = v),
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
