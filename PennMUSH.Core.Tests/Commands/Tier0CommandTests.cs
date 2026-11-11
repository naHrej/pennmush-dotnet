namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Types;
using Xunit;

public class Tier0CommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher = new();
    private readonly Dbref _player;

    public Tier0CommandTests()
    {
        _player = _db.CreateObject("Wizard", ObjectType.Player, new Dbref(1)).Id;
        _dispatcher.Register(new PoseCommandHandler(), "pose", ";", "semipose");
        _dispatcher.Register(new SayCommandHandler(), "say");
        _dispatcher.Register(new SwitchCommandHandler(_dispatcher), "@switch", "@select");
        _dispatcher.Register(new IfElseCommandHandler(_dispatcher), "@if", "@skip");
        _dispatcher.Register(new LockCommandHandler(), "@lock", "@elock", "@ulock");
        _dispatcher.Register(new UnlockCommandHandler(), "@unlock", "@eunlock", "@uunlock");
        _dispatcher.Register(new DestroyCommandHandler(), "@destroy");
        _dispatcher.Register(new UndestroyCommandHandler(), "@undestroy", "@unrecycle");
    }

    [Fact]
    public void Semipose_JoinsTheNameWithoutASpace()
    {
        var posed = _dispatcher.Dispatch(_player, "pose smiles.", _db);
        var semi = _dispatcher.Dispatch(_player, "semipose smiles.", _db);

        Assert.Equal("Wizard smiles.", posed.Notifications[0]);
        Assert.Equal("Wizardsmiles.", semi.Notifications[0]);
    }

    [Fact]
    public void Select_StopsAfterTheFirstMatch()
    {
        var all = _dispatcher.Dispatch(_player, "@switch/all red=red,say one,red,say two", _db);
        var select = _dispatcher.Dispatch(_player, "@select/all red=red,say one,red,say two", _db);

        Assert.Equal(2, all.Notifications.Count);
        Assert.Equal("You say, \"one\"", select.Notifications.Single());
    }

    [Fact]
    public void Skip_ReversesTheIfBranches()
    {
        var skipped = _dispatcher.Dispatch(_player, "@skip 0=say spared,say marked", _db);
        var kept = _dispatcher.Dispatch(_player, "@skip 1=say spared,say marked", _db);
        var ordinary = _dispatcher.Dispatch(_player, "@if 0=say spared,say marked", _db);

        Assert.Equal("You say, \"spared\"", skipped.Notifications.Single());
        Assert.Equal("You say, \"marked\"", kept.Notifications.Single());
        Assert.Equal("You say, \"marked\"", ordinary.Notifications.Single());
    }

    [Fact]
    public void EnterAndUseLocks_UseTheCanonicalLockTypes()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _player);

        _dispatcher.Dispatch(_player, $"@elock #{lamp.Id.Value}=#{_player.Value}", _db);
        _dispatcher.Dispatch(_player, $"@ulock #{lamp.Id.Value}=#{_player.Value}", _db);

        Assert.NotNull(lamp.GetLock(LockType.Enter));
        Assert.NotNull(lamp.GetLock(LockType.Use));
        Assert.Null(lamp.GetLock("Default"));

        _dispatcher.Dispatch(_player, $"@eunlock #{lamp.Id.Value}", _db);
        Assert.Null(lamp.GetLock(LockType.Enter));
        Assert.NotNull(lamp.GetLock(LockType.Use));

        _dispatcher.Dispatch(_player, $"@uunlock #{lamp.Id.Value}", _db);
        Assert.Null(lamp.GetLock(LockType.Use));

        _dispatcher.Dispatch(_player, $"@lock #{lamp.Id.Value}=#{_player.Value}", _db);
        Assert.NotNull(lamp.GetLock("Default"));
    }

    [Fact]
    public void Undestroy_ClearsGoingForTheOwner()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _player);
        lamp.AddFlag("GOING");

        var spared = _dispatcher.Dispatch(_player, $"@undestroy #{lamp.Id.Value}", _db);

        Assert.False(lamp.HasFlag("GOING"));
        Assert.Contains("spared from destruction", spared.Notifications.Single());
    }

    [Fact]
    public void Unrecycle_IsUndestroy()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _player);
        lamp.AddFlag("GOING");

        _dispatcher.Dispatch(_player, $"@unrecycle #{lamp.Id.Value}", _db);

        Assert.False(lamp.HasFlag("GOING"));
    }

    [Fact]
    public void Undestroy_RefusesAnObjectThatIsNotGoing()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _player);

        var result = _dispatcher.Dispatch(_player, $"@undestroy #{lamp.Id.Value}", _db);

        Assert.Equal("That can't be undestroyed.", result.Notifications.Single());
    }

    [Fact]
    public void Undestroy_RefusesSomeoneElsesObject()
    {
        var owner = _db.CreateObject("Owner", ObjectType.Player, new Dbref(1));
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, owner.Id);
        lamp.AddFlag("GOING");
        var mortal = _db.CreateObject("Mortal", ObjectType.Player, new Dbref(1));

        var result = _dispatcher.Dispatch(mortal.Id, $"@undestroy #{lamp.Id.Value}", _db);

        Assert.Equal("Alas, your efforts of mercy are in vain.", result.Notifications.Single());
        Assert.True(lamp.HasFlag("GOING"));
    }
}
