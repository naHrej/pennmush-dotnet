namespace PennMUSH.Core.Flags;

using PennMUSH.Core.Types;

public class FlagDefinition
{
    public string Name { get; init; } = string.Empty;
    public char Letter { get; set; } = '\0';
    public ObjectType TargetType { get; set; } = ObjectType.None; // ObjectType.None means any type
    public FlagPrivileges Permissions { get; set; } = FlagPrivileges.Any;
    public FlagPrivileges NegatePermissions { get; set; } = FlagPrivileges.Any;
    public bool IsPower { get; init; } = false;
}