namespace PennMUSH.Core.Tests.MUSHcode;

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using PennMUSH.Core.Access;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class Backend2ConnectionLogTests : IDisposable
{
    private readonly string _logPath;
    private readonly ConnectionLogStore _connLog;
    private readonly ObjectDatabase _db;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly EvaluationContext _wizCtx;
    private readonly EvaluationContext _mortalCtx;
    private readonly MUSHObject _wiz;
    private readonly MUSHObject _alice;
    private readonly long _rec1;
    private readonly long _rec2;

    public Backend2ConnectionLogTests()
    {
        _logPath = Path.Combine(Path.GetTempPath(), $"pennmush_connlog_{Guid.NewGuid():N}.db");
        _connLog = new ConnectionLogStore();
        _connLog.OpenFile(_logPath);

        _db = new ObjectDatabase();
        var room = _db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);         // #0
        _wiz = _db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);          // #1
        _wiz.Owner = _wiz.Id;
        _wiz.AddFlag("WIZARD");
        _db.MoveObject(_wiz.Id, room.Id);

        _alice = _db.CreateObject("Alice", ObjectType.Player, _wiz.Id);               // #2
        _alice.Owner = _alice.Id;
        _db.MoveObject(_alice.Id, room.Id);

        _rec1 = _connLog.RecordConnect(_wiz.Id, _wiz.Name, "127.0.0.1", "localhost", 1000, ssl: true, websocket: false);
        _rec2 = _connLog.RecordConnect(Dbref.Nothing, null, "10.0.0.42", "alice.example.com", 2000, ssl: false, websocket: true);
        _connLog.UpdatePlayer(_rec2, _alice.Id, _alice.Name);
        _connLog.RecordDisconnect(_rec2, 2500, "quit");

        _registry = new FunctionRegistry(_db)
        {
            ConnectionLog = _connLog
        };
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;

        _wizCtx = new EvaluationContext(_wiz.Id, _wiz.Id, _wiz.Id, room.Id);
        _mortalCtx = new EvaluationContext(_alice.Id, _alice.Id, _alice.Id, room.Id);
    }

    public void Dispose()
    {
        _connLog.Dispose();
        if (File.Exists(_logPath))
            File.Delete(_logPath);
    }

    [Fact]
    public void ConnLogConnRecordAndAddrLog_QueryAndEnforcePermissions()
    {
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate("[connlog(all)]", _mortalCtx));
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate("[addrlog(ip,*)]", _mortalCtx));
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate($"[connrecord({_rec1})]", _mortalCtx));

        Assert.Equal($"{_rec1} {_rec2}", _evaluator.Evaluate("[connlog(all)]", _wizCtx));
        Assert.Equal($"{_rec1}", _evaluator.Evaluate("[connlog(online)]", _wizCtx));
        Assert.Equal($"{_rec2}", _evaluator.Evaluate($"[connlog(player,,,{_alice.Id})]", _wizCtx));
        Assert.Equal("2", _evaluator.Evaluate("[connlog(count,all)]", _wizCtx));

        Assert.Equal("#1 Wizard 127.0.0.1 localhost 1000 -1 - 1 0", _evaluator.Evaluate($"[connrecord({_rec1})]", _wizCtx));
        Assert.Equal("#2 Alice 10.0.0.42 alice.example.com 2000 2500 quit 0 1", _evaluator.Evaluate($"[connrecord({_rec2})]", _wizCtx));
        Assert.Equal("#-1 NO SUCH RECORD", _evaluator.Evaluate("[connrecord(9999)]", _wizCtx));

        Assert.Equal("10.0.0.42 alice.example.com", _evaluator.Evaluate("[addrlog(ip,10.0.*)]", _wizCtx));
        Assert.Equal("1", _evaluator.Evaluate("[addrlog(count,hostname,*.example.com)]", _wizCtx));
    }

    [Fact]
    public void SitelockPlayer_BansLastKnownIpFromConnectionLog()
    {
        var table = new SiteLockTable();
        var handler = new SiteLockCommandHandler(table, _connLog);
        var ctx = new CommandContext
        {
            Executor = _wiz.Id,
            Enactor = _wiz.Id,
            CommandName = "@sitelock",
            Switches = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "player" },
            Arguments = "Alice",
            LeftArg = "Alice"
        };

        Assert.True(handler.Execute(ctx, _db));
        var (rule, _) = table.Check("10.0.0.42");
        Assert.NotNull(rule);
        Assert.Contains(ctx.Notifications, n => n.Contains("10.0.0.42"));
    }
}
