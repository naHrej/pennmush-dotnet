namespace PennMUSH.Core.Tests.Commands;

using System.Collections.Generic;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class PcreateCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly Dictionary<Dbref, string> _stored = new();
    private readonly List<string> _told = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _god;
    private readonly Dbref _mortal;

    public PcreateCommandTests()
    {
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        _god = _db.CreateObject("One", ObjectType.Player, new Dbref(1)).Id;
        _mortal = _db.CreateObject("Ada", ObjectType.Player, _god).Id;
        var passwords = new CallbackPasswords((id, password) => _stored[id] = password);
        var functions = new FunctionRegistry(_db)
        {
            Passwords = passwords,
            PlayerStart = new Dbref(0),
            DefaultHome = new Dbref(0),
            StartingMoney = 25
        };
        functions.NotificationDispatcher = note => _told.Add(note.Message);
        var evaluator = new ExpressionEvaluator(functions.Dispatch, _db);
        _dispatcher = new CommandDispatcher(evaluator);
        _dispatcher.Register(new PcreateCommandHandler(passwords, new Dbref(0), new Dbref(0), 25), "@pcreate");
        _dispatcher.Register(new ThinkCommandHandler(evaluator), "think");
    }

    [Fact]
    public void Pcreate_MakesAPlayerAndStoresThePassword()
    {
        var denied = _dispatcher.Dispatch(_mortal, "@pcreate Bea=secret", _db);
        Assert.Contains("You do not have the power over body and mind!", denied.Notifications);
        Assert.Null(_db.FindPlayer("Bea"));

        var created = _dispatcher.Dispatch(_god, "@pcreate Bea=secret", _db);
        var bea = _db.FindPlayer("Bea");
        Assert.NotNull(bea);
        Assert.Contains($"New player 'Bea' ({bea!.Id}) created with password 'secret'", created.Notifications);
        Assert.Equal("secret", _stored[bea.Id]);
        Assert.Equal(ObjectType.Player, bea.Type);
        Assert.Equal(bea.Id, bea.Owner);
        Assert.Equal(new Dbref(0), bea.Location);
        Assert.Equal(25, bea.Pennies);

        var again = _dispatcher.Dispatch(_god, "@pcreate Bea=other", _db);
        Assert.Contains("Failure creating 'Bea' (name in use)", again.Notifications);

        var bad = _dispatcher.Dispatch(_god, "@pcreate Cam=sec ret", _db);
        Assert.Contains("Failure creating 'Cam' (bad password)", bad.Notifications);
        Assert.Null(_db.FindPlayer("Cam"));

        var reserved = _dispatcher.Dispatch(_god, "@pcreate me=secret", _db);
        Assert.Contains("Failure creating 'me' (bad name)", reserved.Notifications);
    }

    [Fact]
    public void PcreateFunction_ReturnsTheNewDbref()
    {
        _db.GetObject(_mortal)!.AddPower("Player_Create");

        var created = _dispatcher.Dispatch(_mortal, "think [pcreate(Bea,secret)]", _db);
        var bea = _db.FindPlayer("Bea");
        Assert.NotNull(bea);
        Assert.Contains(bea!.Id.ToString(), created.Notifications);
        Assert.Contains($"New player 'Bea' ({bea.Id}) created with password 'secret'", _told);
        Assert.Equal("secret", _stored[bea.Id]);

        var garbage = _db.CreateObject("Garbage", ObjectType.Garbage, _god);
        var reused = _dispatcher.Dispatch(_god, $"@pcreate Dot=pw,{garbage.Id}", _db);
        Assert.Equal("Dot", garbage.Name);
        Assert.Equal(ObjectType.Player, garbage.Type);
        Assert.Contains($"New player 'Dot' ({garbage.Id}) created with password 'pw'", reused.Notifications);
        Assert.Equal("pw", _stored[garbage.Id]);

        var refused = _dispatcher.Dispatch(_mortal, $"think [pcreate(Eve,pw,{garbage.Id})]", _db);
        Assert.Contains("#-1", refused.Notifications);
        Assert.Contains("Permission denied.", _told);
        Assert.Null(_db.FindPlayer("Eve"));
    }
}
