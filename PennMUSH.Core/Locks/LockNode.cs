namespace PennMUSH.Core.Locks;

using PennMUSH.Core.Database;
using PennMUSH.Core.Types;

public abstract record LockNode
{
    public abstract bool Evaluate(Dbref player, Dbref target, ObjectDatabase db);
    public abstract string Unparse();
}

public sealed record ConstantLockNode(bool Value) : LockNode
{
    public override bool Evaluate(Dbref player, Dbref target, ObjectDatabase db) => Value;
    public override string Unparse() => Value ? "#TRUE" : "#FALSE";
}

public sealed record DbrefLockNode(Dbref Target) : LockNode
{
    public override bool Evaluate(Dbref player, Dbref target, ObjectDatabase db) => player == Target;
    public override string Unparse() => Target.ToString();
}

public sealed record NotLockNode(LockNode Child) : LockNode
{
    public override bool Evaluate(Dbref player, Dbref target, ObjectDatabase db) => !Child.Evaluate(player, target, db);
    public override string Unparse() => Child is AndLockNode or OrLockNode ? $"!({Child.Unparse()})" : $"!{Child.Unparse()}";
}

public sealed record AndLockNode(LockNode Left, LockNode Right) : LockNode
{
    public override bool Evaluate(Dbref player, Dbref target, ObjectDatabase db) => Left.Evaluate(player, target, db) && Right.Evaluate(player, target, db);
    public override string Unparse()
    {
        string l = Left is OrLockNode ? $"({Left.Unparse()})" : Left.Unparse();
        string r = Right is OrLockNode ? $"({Right.Unparse()})" : Right.Unparse();
        return $"{l}&{r}";
    }
}

public sealed record OrLockNode(LockNode Left, LockNode Right) : LockNode
{
    public override bool Evaluate(Dbref player, Dbref target, ObjectDatabase db) => Left.Evaluate(player, target, db) || Right.Evaluate(player, target, db);
    public override string Unparse() => $"{Left.Unparse()}|{Right.Unparse()}";
}
