namespace PennMUSH.Core.Attributes;

using PennMUSH.Core.Types;

public sealed class AttributeEntry
{
    private HashSet<string>? _customFlags;

    public string Name { get; init; } = string.Empty;
    public AttributeFlags Flags { get; set; } = AttributeFlags.None;
    public IReadOnlyCollection<string> CustomFlags => (IReadOnlyCollection<string>?)_customFlags ?? Array.Empty<string>();
    public Dbref Creator { get; set; } = Dbref.Nothing;
    public string Value { get; set; } = string.Empty;

    public AttributeEntry() { }

    public AttributeEntry(string name, string value, Dbref creator, AttributeFlags flags = AttributeFlags.None, IEnumerable<string>? customFlags = null)
    {
        Name = name;
        Value = value;
        Creator = creator;
        Flags = flags;
        SetCustomFlags(customFlags);
    }

    public bool HasCustomFlag(string flag)
        => !string.IsNullOrWhiteSpace(flag) && (_customFlags?.Contains(flag.Trim()) ?? false);

    public bool AddCustomFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag))
            return false;
        _customFlags ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return _customFlags.Add(flag.Trim().ToLowerInvariant());
    }

    public bool RemoveCustomFlag(string flag)
        => !string.IsNullOrWhiteSpace(flag) && (_customFlags?.Remove(flag.Trim()) ?? false);

    public void SetCustomFlags(IEnumerable<string>? flags)
    {
        _customFlags = null;
        if (flags == null)
            return;
        foreach (string flag in flags)
            AddCustomFlag(flag);
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
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '`' && c != '.' && c != '+' && c != '\'')
                return false;
        }
        return true;
    }
}
