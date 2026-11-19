namespace PennMUSH.Core.Tests.MUSHcode;

using System;
using System.IO;
using Xunit;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class Backend1SqlFunctionTests : IDisposable
{
    private readonly string _sqlPath;
    private readonly SqlDatabase _sql;
    private readonly ObjectDatabase _db;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly EvaluationContext _wizCtx;
    private readonly EvaluationContext _mortalCtx;
    private readonly EvaluationContext _sqlOkCtx;

    public Backend1SqlFunctionTests()
    {
        _sqlPath = Path.Combine(Path.GetTempPath(), $"pennmush_sql_fn_{Guid.NewGuid():N}.db");
        _sql = new SqlDatabase();
        _sql.OpenFile(_sqlPath);

        _db = new ObjectDatabase();
        var room = _db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);         // #0
        var wiz = _db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);       // #1
        wiz.Owner = wiz.Id;
        wiz.AddFlag("WIZARD");
        _db.MoveObject(wiz.Id, room.Id);

        var mortal = _db.CreateObject("Mortal", ObjectType.Player, wiz.Id);           // #2
        mortal.Owner = mortal.Id;
        _db.MoveObject(mortal.Id, room.Id);

        var sqlUser = _db.CreateObject("SqlUser", ObjectType.Player, wiz.Id);         // #3
        sqlUser.Owner = sqlUser.Id;
        sqlUser.AddPower("Sql_Ok");
        _db.MoveObject(sqlUser.Id, room.Id);

        wiz.SetAttribute("ROW_FMT", "%0:%1=%2(%q<score>)", wiz.Id);

        _registry = new FunctionRegistry(_db)
        {
            SqlDatabase = _sql
        };
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;

        _wizCtx = new EvaluationContext(wiz.Id, wiz.Id, wiz.Id, room.Id);
        _mortalCtx = new EvaluationContext(mortal.Id, mortal.Id, mortal.Id, room.Id);
        _sqlOkCtx = new EvaluationContext(sqlUser.Id, sqlUser.Id, sqlUser.Id, room.Id);
    }

    public void Dispose()
    {
        _sql.Dispose();
        if (File.Exists(_sqlPath))
            File.Delete(_sqlPath);
    }

    [Fact]
    public void SqlAndMapsql_EnforcePermissionsAndExecuteQueries()
    {
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate("[sql(SELECT 1)]", _mortalCtx));
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate("[mapsql(me/ROW_FMT,SELECT 1)]", _mortalCtx));
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate("[sqlescape(a'b)]", _mortalCtx));

        Assert.Equal(string.Empty, _evaluator.Evaluate("[sql({CREATE TABLE users (name TEXT, score INT)})]", _wizCtx));
        Assert.Equal(string.Empty, _evaluator.Evaluate("[sql({INSERT INTO users VALUES ('Alice', 10), ('Bob', 25)})]", _wizCtx));

        Assert.Equal("Alice:10|Bob:25", _evaluator.Evaluate("[sql({SELECT name, score FROM users ORDER BY score},|,:)]", _sqlOkCtx));
        Assert.StartsWith("#-1 SQL ERROR:", _evaluator.Evaluate("[sql(SELECT * FROM nonexistent_table)]", _wizCtx));

        Assert.Equal("0:name=score()|1:Alice=10(10)|2:Bob=25(25)",
            _evaluator.Evaluate("[mapsql(#1/ROW_FMT,{SELECT name, score FROM users ORDER BY score},|,1)]", _wizCtx));
    }
}
