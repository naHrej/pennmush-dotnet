namespace PennMUSH.Core.Attributes;

public enum AttributeErrorCode
{
    Okay = 0,
    Error = -1,
    Safe = -2,
    BadName = -3,
    TooMany = -4,
    Tree = -5,
    NotFound = -6,
    Locked = -7
}
