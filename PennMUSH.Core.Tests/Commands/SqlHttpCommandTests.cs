namespace PennMUSH.Core.Tests.Commands;

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Http;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;
using Xunit;

public class SqlHttpCommandTests : IDisposable
{
    private readonly string _file;
    private readonly SqlDatabase _sql = new();
    private readonly ObjectDatabase _db = new();
    private readonly ActionQueue _queue = new();
    private readonly HttpChannel _http = new();
    private readonly HttpFetch _fetch = new();
    private readonly CommandDispatcher _dispatcher = new();
    private readonly Dbref _god;
    private readonly Dbref _ada;

    public SqlHttpCommandTests()
    {
        _file = Path.Combine(Path.GetTempPath(), "penn-sql-" + Guid.NewGuid().ToString("N") + ".db");
        _sql.OpenFile(_file);
        _sql.Query("create table sample(name text, score integer)");
        _sql.Query("insert into sample(name, score) values ('Ada', 3)");
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.Owner = god.Id;
        god.AddFlag("WIZARD");
        _god = god.Id;
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        ada.Owner = ada.Id;
        _ada = ada.Id;
        _dispatcher.Register(new SqlCommandHandler(_sql), "@sql");
        _dispatcher.Register(new MapSqlCommandHandler(_sql, _queue), "@mapsql");
        _dispatcher.Register(new HttpCommandHandler(_fetch, _queue), "@http");
        _dispatcher.Register(new RespondCommandHandler(_http), "@respond");
        _dispatcher.Register(new SlaveCommandHandler(), "@slave");
        _dispatcher.Register(new PemitCommandHandler(), "@pemit");
        _dispatcher.AddRestriction("@http", "nogagged", null);
        _dispatcher.AddRestriction("@slave", "wizard", null);
    }

    [Fact]
    public void SqlPrintsRowsAndAMortalNeedsThePower()
    {
        var denied = _dispatcher.Dispatch(_ada, "@sql select name from sample", _db);
        Assert.Contains("Permission denied.", denied.Notifications);

        _db.GetObject(_ada)!.AddPower("Sql_Ok");
        var rows = _dispatcher.Dispatch(_ada, "@sql select name, score from sample", _db);
        Assert.Contains("Row 1, Field name: Ada", rows.Notifications);
        Assert.Contains("Row 1, Field score: 3", rows.Notifications);

        var changed = _dispatcher.Dispatch(_god, "@sql insert into sample(name, score) values ('Bea', 4)", _db);
        Assert.Contains("SQL: 1 rows affected.", changed.Notifications);

        var broken = _dispatcher.Dispatch(_god, "@sql select", _db);
        Assert.Contains(broken.Notifications, line => line.StartsWith("SQL: Error:"));
    }

    [Fact]
    public void MapSqlQueuesOneCommandPerRow()
    {
        _db.GetObject(_ada)!.SetAttribute("ROW", "@pemit me=%0 %1 %q<name>", _ada);
        var missing = _dispatcher.Dispatch(_god, "@mapsql Ada", _db);
        Assert.Contains("What do you want to query?", missing.Notifications);

        var queued = _dispatcher.Dispatch(_god, "@mapsql/colnames/notify Ada/ROW=select name, score from sample", _db);
        Assert.Empty(queued.Notifications);
        var entries = _queue.Snapshot();
        Assert.Equal(3, entries.Count);
        Assert.Equal(new[] { "0", "name", "score" }, entries[0].Arguments);
        Assert.Equal(new[] { "1", "Ada", "3" }, entries[1].Arguments);
        Assert.Equal("Ada", entries[1].Registers["name"]);
        Assert.Equal("@notify me", entries[2].Command);
        Assert.Equal(_ada, entries[0].Executor);
    }

    [Fact]
    public void HttpQueuesTheBodyAndRespondFillsTheRequest()
    {
        _fetch.Transport = (request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("Basic", request.Headers.Authorization?.Scheme);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("fetched")
            };
            response.Content.Headers.TryAddWithoutValidation("Content-Type", "text/plain");
            return Task.FromResult(response);
        };
        _db.GetObject(_god)!.SetAttribute("GOT", "@pemit me=%0", _god);
        var context = new CommandContext
        {
            Executor = _god,
            Enactor = _god,
            CommandName = "@http",
            LeftArg = "me/GOT",
            RightArg = "https://example.test/a,payload",
            Switches = new(StringComparer.OrdinalIgnoreCase) { "post" }
        };
        context.Registers["userpass"] = "ada:secret";
        var handler = new HttpCommandHandler(_fetch, _queue);
        Assert.True(handler.Execute(context, _db));
        var entry = Assert.Single(_queue.Snapshot());
        Assert.Equal("fetched", Assert.Single(entry.Arguments));
        Assert.Equal("200", entry.Registers["status"]);

        var idle = _dispatcher.Dispatch(_god, "@respond 204 No Content", _db);
        Assert.Contains("Invalid http_handler, it should be a player.", idle.Notifications);
        Assert.Contains("(HTTP): HTTP/1.1 204 No Content", idle.Notifications);

        _http.Handler = _god;
        _db.GetObject(_god)!.SetAttribute("GET", "@respond 201 Created\n@respond/type text/html\n@pemit me=Hello %0", _god);
        var exchange = _http.Handle(
            _dispatcher,
            new ExpressionEvaluator(),
            _db,
            "GET",
            "/quest",
            "",
            new Dictionary<string, string>());
        Assert.True(exchange.Handled);
        Assert.Equal(201, exchange.Reply.StatusCode);
        Assert.Equal("text/html", exchange.Reply.MediaType);
        Assert.Contains("Hello /quest", exchange.Reply.Body.ToString());
    }

    [Fact]
    public void SlaveRefusesWithoutADaemon()
    {
        var mortal = _dispatcher.Dispatch(_ada, "@slave", _db);
        Assert.Contains("Permission denied.", mortal.Notifications);
        var bare = _dispatcher.Dispatch(_god, "@slave", _db);
        Assert.Contains("I'm sorry, Dave, I'm afraid I can't do that.", bare.Notifications);
        var restart = _dispatcher.Dispatch(_god, "@slave/restart info", _db);
        Assert.Contains("No such service.", restart.Notifications);
    }

    public void Dispose()
    {
        _sql.Dispose();
        if (File.Exists(_file))
            File.Delete(_file);
    }
}
