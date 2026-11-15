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

            case "colors_file":
                config.ColorsFile = value;
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

    private static bool ParseBool(string val, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(val)) return defaultValue;
        string v = val.Trim().ToLowerInvariant();
        return v is "yes" or "true" or "1" or "on";
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
