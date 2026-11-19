namespace PennMUSH.Core.Communication.Mail;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public sealed class MailAlias
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Dbref> Members { get; } = new();
    public int NameFlags { get; set; }
    public int MemberFlags { get; set; }
    public Dbref Owner { get; set; }
}

/// <summary>
/// The mail-alias table original PennMUSH keeps beside the mailbox.
/// Names are stored without the leading '+'.
/// </summary>
public sealed class MailAliasTable
{
    public const char Token = '+';
    public const int Members = 0x1;
    public const int Admin = 0x2;
    public const int OwnerBit = 0x4;
    public const int MaxBatch = 100;

    private readonly List<MailAlias> _aliases = new();

    public int Count => _aliases.Count;
    public int Capacity { get; private set; }
    public IReadOnlyList<MailAlias> Aliases => _aliases;

    public void Clear()
    {
        _aliases.Clear();
        Capacity = 0;
    }

    public void ImportAlias(MailAlias alias)
    {
        _aliases.Add(alias);
        NoteAdded();
    }

    public void NoteAdded()
    {
        if (Capacity == 0)
            Capacity = 3;
        else if (_aliases.Count > Capacity)
            Capacity += 3;
    }

    public MailAlias? Find(MUSHObject viewer, string alias)
    {
        if (string.IsNullOrEmpty(alias) || alias[0] != Token)
            return null;
        string name = alias[1..];
        foreach (var item in _aliases)
        {
            if (!item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                continue;
            if (CanSeeName(viewer, item))
                return item;
        }
        return null;
    }

    public MailAlias? FindAny(string alias)
    {
        if (string.IsNullOrEmpty(alias) || alias[0] != Token)
            return null;
        string name = alias[1..];
        foreach (var item in _aliases)
        {
            if (item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return item;
        }
        return null;
    }

    public IEnumerable<MailAlias> Visible(MUSHObject viewer)
    {
        foreach (var item in _aliases)
        {
            if (item.Owner == viewer.Id
                || item.NameFlags == 0
                || ((item.NameFlags & Admin) != 0 && viewer.IsRoyalty)
                || ((item.NameFlags & Members) != 0 && IsMember(item, viewer.Id)))
                yield return item;
        }
    }

    public static bool CanSeeName(MUSHObject viewer, MailAlias alias) =>
        alias.Owner == viewer.Id
        || alias.NameFlags == 0
        || viewer.IsRoyalty
        || ((alias.NameFlags & Members) != 0 && IsMember(alias, viewer.Id));

    public static bool CanSeeMembers(MUSHObject viewer, MailAlias alias) =>
        alias.Owner == viewer.Id
        || alias.MemberFlags == 0
        || viewer.IsRoyalty
        || ((alias.MemberFlags & Members) != 0 && IsMember(alias, viewer.Id));

    public static int MemberIndex(MailAlias alias, Dbref player)
    {
        for (int i = 0; i < alias.Members.Count; i++)
        {
            if (alias.Members[i] == player)
                return i + 1;
        }
        return 0;
    }

    public static bool IsMember(MailAlias alias, Dbref player) => MemberIndex(alias, player) != 0;

    public void RemoveAt(int index)
    {
        int last = _aliases.Count - 1;
        _aliases[index] = _aliases[last];
        _aliases.RemoveAt(last);
    }

    public int IndexOf(MailAlias alias) => _aliases.IndexOf(alias);

    public MailAlias Add(MailAlias alias)
    {
        _aliases.Add(alias);
        NoteAdded();
        return alias;
    }

    public static bool LegalName(string alias, out string error)
    {
        error = "";
        if (string.IsNullOrEmpty(alias) || alias[0] != Token)
        {
            error = $"MAIL: All Mail aliases must begin with '{Token}'.";
            return false;
        }
        const string good = "`$_-.'";
        for (int i = 1; i < alias.Length; i++)
        {
            char ch = alias[i];
            if (char.IsLetterOrDigit(ch) || good.Contains(ch))
                continue;
            error = "MAIL: Invalid character in mail alias.";
            return false;
        }
        return true;
    }

    public static string ShortPrivs(int nameFlags, int memberFlags)
    {
        var privs = "--  -- ".ToCharArray();
        Apply(privs, 0, nameFlags);
        Apply(privs, 4, memberFlags);
        return new string(privs);
    }

    public static int ParsePrivs(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int yes = 0;
        int no = 0;
        foreach (string raw in words)
        {
            bool negate = raw.StartsWith('!');
            string word = negate ? raw[1..] : raw;
            if (word.Length == 0)
                continue;
            int bits = word.Length == 1 ? LetterBits(word) : 0;
            if (bits == 0)
                bits = PrefixBits(word);
            if (bits == 0 && words.Length == 1)
                bits = Letters(word);
            if (negate)
                no |= bits;
            else
                yes |= bits;
        }
        return yes & ~no;
    }

    public static string PrivsText(int flags)
    {
        var names = new List<string>();
        if ((flags & Admin) != 0)
            names.Add("Admin");
        if ((flags & Members) != 0)
            names.Add("Members");
        if ((flags & OwnerBit) != 0)
            names.Add("Owner");
        return string.Join(' ', names);
    }

    public static MUSHObject? ResolvePlayer(ObjectDatabase db, MUSHObject actor, string token)
    {
        if (token.Equals("me", StringComparison.OrdinalIgnoreCase))
            return actor.Type == ObjectType.Player ? actor : null;
        if (token.StartsWith('#') && int.TryParse(token.AsSpan(1), out int id))
        {
            var byId = db.GetObject(new Dbref(id));
            return byId != null && byId.Type == ObjectType.Player ? byId : null;
        }

        MUSHObject? match = null;
        for (int i = 0; i < db.Count; i++)
        {
            var obj = db.GetObject(new Dbref(i));
            if (obj == null || obj.Type != ObjectType.Player)
                continue;
            if (NameMatches(obj.Name, token))
            {
                match = obj;
                break;
            }
        }
        return match;
    }

    public static string ShownName(MUSHObject obj)
    {
        int semi = obj.Name.IndexOf(';');
        return semi < 0 ? obj.Name : obj.Name[..semi];
    }

    public static string Unparse(MUSHObject obj) => $"{ShownName(obj)}(#{obj.Id.Value})";

    public static IEnumerable<string> Tokens(string list)
    {
        int i = 0;
        while (i < list.Length)
        {
            while (i < list.Length && list[i] == ' ')
                i++;
            if (i >= list.Length)
                yield break;
            if (list[i] == '"')
            {
                i++;
                int start = i;
                while (i < list.Length && list[i] != '"')
                    i++;
                yield return list[start..i];
                if (i < list.Length && list[i] == '"')
                    i++;
            }
            else
            {
                int start = i;
                while (i < list.Length && list[i] != ' ')
                    i++;
                yield return list[start..i];
            }
        }
    }

    private static void Apply(char[] privs, int at, int flags)
    {
        if (flags == 0)
        {
            privs[at] = 'E';
            return;
        }
        if ((flags & Members) != 0)
            privs[at] = 'M';
        if ((flags & Admin) != 0)
            privs[at + 1] = 'A';
        if (privs[at] == '-' && privs[at + 1] == '-')
            privs[at + 1] = 'O';
    }

    private static int LetterBits(string word) => word[0] switch
    {
        'A' or 'a' => Admin,
        'M' or 'm' => Members,
        'O' or 'o' => OwnerBit,
        _ => 0
    };

    private static int Letters(string word)
    {
        int bits = 0;
        foreach (char ch in word)
            bits |= LetterBits(ch.ToString());
        return bits;
    }

    private static int PrefixBits(string word)
    {
        if ("Admin".StartsWith(word, StringComparison.OrdinalIgnoreCase))
            return Admin;
        if ("Members".StartsWith(word, StringComparison.OrdinalIgnoreCase))
            return Members;
        if ("Owner".StartsWith(word, StringComparison.OrdinalIgnoreCase))
            return OwnerBit;
        return 0;
    }

    private static bool NameMatches(string stored, string token)
    {
        foreach (string part in stored.Split(';'))
        {
            if (part.Equals(token, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
