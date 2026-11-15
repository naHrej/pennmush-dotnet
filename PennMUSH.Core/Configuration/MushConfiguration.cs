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
    public Dbref PlayerStart { get; set; } = new(0);
    public Dbref BaseRoom { get; set; } = new(0);
    public Dbref DefaultHome { get; set; } = new(0);

    // Database Paths
    public string InputDatabase { get; set; } = "data/indb";
    public string OutputDatabase { get; set; } = "data/outdb";
    public string CrashDatabase { get; set; } = "data/PANIC.db";
    public string MailDatabase { get; set; } = "data/maildb";
    public string ChatDatabase { get; set; } = "data/chatdb";

    // Text File Paths (relative to game/ directory)
    public string ConnectFile { get; set; } = "txt/connect.txt";
    public string MotdFile { get; set; } = "txt/motd.txt";
    public string WizmotdFile { get; set; } = "txt/wizmotd.txt";
    public string NewuserFile { get; set; } = "txt/newuser.txt";
    public string QuitFile { get; set; } = "txt/quit.txt";
    public string DownFile { get; set; } = "txt/down.txt";
    public string FullFile { get; set; } = "txt/full.txt";
    public string GuestFile { get; set; } = "txt/guest.txt";
    public string ColorsFile { get; set; } = "txt/colors.json";

    // Economy & Names
    public string MoneySingular { get; set; } = "Penny";
    public string MoneyPlural { get; set; } = "Pennies";
    public int StartingMoney { get; set; } = 100;
    public bool UseQuota { get; set; } = true;
    public int StartingQuota { get; set; } = 20;
    public int Paycheck { get; set; } = 50;

    // Aliases & Restrictions
    public Dictionary<string, string> CommandAliases { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> FunctionAliases { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ReservedAliases { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<RestrictionRule>> CommandRestrictions { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<RestrictionRule>> FunctionRestrictions { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> RestrictedNames { get; } = new();

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
}

public record RestrictionRule(string Restriction, string? ErrorMessage = null);
