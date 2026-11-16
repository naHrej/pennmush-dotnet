namespace PennMUSH.Core.Attributes;

using PennMUSH.Core.Types;

public sealed class AttributeEntry
{
    public string Name { get; init; } = string.Empty;
    public AttributeFlags Flags { get; set; } = AttributeFlags.None;
    public Dbref Creator { get; set; } = Dbref.Nothing;
    public string Value { get; set; } = string.Empty;

    public AttributeEntry() { }

    public AttributeEntry(string name, string value, Dbref creator, AttributeFlags flags = AttributeFlags.None)
    {
        Name = name;
        Value = value;
        Creator = creator;
        Flags = flags;
    }

    public static bool IsValidName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
        if (name.Length > 1024)
            return false;
        if (name.StartsWith('`') || name.EndsWith('`'))
            return false;
        if (name.Contains("``"))
            return false;
        foreach (char c in name)
        {
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '`' && c != '.')
                return false;
        }
        return true;
    }
}
