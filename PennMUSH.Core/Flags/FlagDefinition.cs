namespace PennMUSH.Core.Flags;

using PennMUSH.Core.Types;

public class FlagDefinition
{
    private string _name = string.Empty;
    private string? _internalName;

    public string Name
    {
        get => _name;
        set
        {
            if (_internalName == null && !string.IsNullOrEmpty(value))
                _internalName = value;
            _name = value;
        }
    }

    public string InternalName
    {
        get => _internalName ?? _name;
        init => _internalName = value;
    }
    public char Letter { get; set; } = '\0';
    public ObjectType TargetType { get; set; } = ObjectType.None; // ObjectType.None means any type
    public FlagPrivileges Permissions { get; set; } = FlagPrivileges.Any;
    public FlagPrivileges NegatePermissions { get; set; } = FlagPrivileges.Any;
    public bool IsPower { get; init; } = false;
}