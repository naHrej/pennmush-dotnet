namespace PennMUSH.Core.Database;

using System;
using PennMUSH.Core.Types;

public interface IPlayerPasswords
{
    void Set(Dbref player, string password);
}

public sealed class CallbackPasswords : IPlayerPasswords
{
    private readonly Action<Dbref, string> _set;

    public CallbackPasswords(Action<Dbref, string> set) => _set = set;

    public void Set(Dbref player, string password) => _set(player, password);
}
