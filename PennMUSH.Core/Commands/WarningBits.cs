namespace PennMUSH.Core.Commands;

using System;
using System.Text;

/// <summary>
/// The warning names and bit values from original PennMUSH <c>warn_tab.h</c>.
/// </summary>
public static class WarningBits
{
    public const uint None = 0;
    public const uint ExitOneway = 0x1;
    public const uint ExitMultiple = 0x2;
    public const uint ExitMsgs = 0x4;
    public const uint ExitDesc = 0x8;
    public const uint ExitUnlinked = 0x10;
    public const uint ThingMsgs = 0x100;
    public const uint ThingDesc = 0x200;
    public const uint RoomDesc = 0x1000;
    public const uint PlayerDesc = 0x10000;
    public const uint LockProbs = 0x100000;

    public const uint Serious = ExitUnlinked | ThingDesc | RoomDesc | PlayerDesc | LockProbs;
    public const uint Normal = Serious | ExitOneway | ExitMultiple | ExitMsgs;
    public const uint Extra = Normal | ThingMsgs;
    public const uint All = Extra | ExitDesc;

    private static readonly (string Name, uint Flag)[] Checklist =
    [
        ("none", None),
        ("exit-unlinked", ExitUnlinked),
        ("thing-desc", ThingDesc),
        ("room-desc", RoomDesc),
        ("my-desc", PlayerDesc),
        ("exit-oneway", ExitOneway),
        ("exit-multiple", ExitMultiple),
        ("exit-msgs", ExitMsgs),
        ("thing-msgs", ThingMsgs),
        ("exit-desc", ExitDesc),
        ("lock-checks", LockProbs),
        ("serious", Serious),
        ("normal", Normal),
        ("extra", Extra),
        ("all", All),
    ];

    /// <summary>
    /// Turns a space-separated warning list into a bitmask.
    /// An empty list is none. A list whose last name is unknown is every bit,
    /// which is what the original unsigned return of -1 stores.
    /// </summary>
    public static uint Parse(string? warnings, Action<string>? unknown)
    {
        if (string.IsNullOrEmpty(warnings))
            return None;

        string[] tokens = warnings.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
            return uint.MaxValue;

        uint flags = None;
        uint negate = None;
        bool found = false;
        foreach (string raw in tokens)
        {
            found = false;
            string name = raw;
            bool negated = false;
            if (name.StartsWith('!'))
            {
                negated = true;
                name = name[1..];
            }

            foreach ((string itemName, uint flag) in Checklist)
            {
                if (name.Equals(itemName, StringComparison.OrdinalIgnoreCase))
                {
                    if (negated)
                        negate |= flag;
                    else
                        flags |= flag;
                    found = true;
                }
            }

            if (!found)
                unknown?.Invoke(name);
        }

        if (!found)
            return uint.MaxValue;
        return flags & ~negate;
    }

    /// <summary>
    /// Renders a bitmask as warning names, largest group first, with a trailing space.
    /// </summary>
    public static string Unparse(uint warns)
    {
        var text = new StringBuilder();
        for (int index = Checklist.Length - 1; warns != 0 && index >= 0; index--)
        {
            uint flag = Checklist[index].Flag;
            if ((flag & ~warns) == 0)
            {
                text.Append(Checklist[index].Name);
                text.Append(' ');
                warns &= ~flag;
            }
        }
        return text.ToString();
    }
}
