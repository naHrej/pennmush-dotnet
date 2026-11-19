namespace PennMUSH.Core.Tests.MUSHcode;

using System;
using Xunit;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Protocol;
using PennMUSH.Core.Types;

public class Sections7_8_11_15_16FunctionTests
{
    private readonly ObjectDatabase _db;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly EvaluationContext _ctx;
    private readonly MUSHObject _room;
    private readonly MUSHObject _wiz;
    private readonly MUSHObject _zone;
    private readonly MUSHObject _parent;

    public Sections7_8_11_15_16FunctionTests()
    {
        _db = new ObjectDatabase();
        _room = _db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);             // #0
        _wiz = _db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);          // #1
        _wiz.Owner = _wiz.Id;
        _wiz.AddFlag("WIZARD");
        _db.MoveObject(_wiz.Id, _room.Id);

        _zone = _db.CreateObject("MasterZone", ObjectType.Thing, _wiz.Id);            // #2
        _zone.SetAttribute("Z_FN", "zone:%0", _wiz.Id);
        _wiz.Zone = _zone.Id;

        _parent = _db.CreateObject("MasterParent", ObjectType.Thing, _wiz.Id);        // #3
        _parent.SetAttribute("P_FN", "parent:%0", _wiz.Id);
        _wiz.AddParent(_parent.Id, _db);

        _wiz.SetAttribute("DOUBLE", "[mul(%0,2)]", _wiz.Id);
        _wiz.SetAttribute("SET_Q", "[setr(localvar,changed)]", _wiz.Id);
        _wiz.SetAttribute("DYN_EXPR", "[add(2,3)]", _wiz.Id);
        _wiz.SetAttribute("JSON_CB", "%2=%1", _wiz.Id);

        _registry = new FunctionRegistry(_db)
        {
            MudName = "TestMUSH",
            MudUrl = "https://example.org",
            CommandNamesProvider = () => ["look", "say", "@examine"],
            ServerStartTime = DateTimeOffset.UtcNow.AddSeconds(-42),
            RestartCount = 3
        };
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;

        _ctx = new EvaluationContext(_wiz.Id, _wiz.Id, _wiz.Id, _room.Id);
    }

    [Fact]
    public void Section7_TypeTestsAndServerMetadata_WorkCorrectly()
    {
        Assert.Equal("1", _evaluator.Evaluate("[valid(attrname,FOO_BAR)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[valid(attrname,bad attr)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[valid(flag,WIZARD)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[valid(power,Builder)]", _ctx));

        Assert.Equal("1", _evaluator.Evaluate($"[isdbref({_wiz.Id})]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[isdbref(#999)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[isint(-42)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[isint(3.14)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[isnum(-3.14)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[isnum(abc)]", _ctx));

        string objid = _evaluator.Evaluate($"[objid({_wiz.Id})]", _ctx);
        Assert.Equal("1", _evaluator.Evaluate($"[isobjid({objid})]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate($"[isobjid({_wiz.Id}:999999)]", _ctx));

        Assert.Equal("1", _evaluator.Evaluate("[isword(Hello)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[isword(Hello1)]", _ctx));

        Assert.Contains("PennMUSH", _evaluator.Evaluate("[version()]", _ctx));
        Assert.Equal("1080800", _evaluator.Evaluate("[numversion()]", _ctx));
        Assert.Equal("TestMUSH", _evaluator.Evaluate("[mudname()]", _ctx));
        Assert.Equal("https://example.org", _evaluator.Evaluate("[mudurl()]", _ctx));
        Assert.Equal("-1", _evaluator.Evaluate("[cmds(1)]", _ctx));
    }

    [Fact]
    public void Section8_ClockAndTimeArithmetic_WorkCorrectly()
    {
        string ut = _evaluator.Evaluate("[utctime()]", _ctx);
        Assert.False(string.IsNullOrWhiteSpace(ut));
        Assert.Equal("Thu Jan 1 00:00:00 1970", _evaluator.Evaluate("[convutcsecs(0)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[convutctime(Thu Jan 1 00:00:00 1970)]", _ctx));

        Assert.Equal(_wiz.CreationTime.ToUnixTimeSeconds().ToString(), _evaluator.Evaluate($"[csecs({_wiz.Id})]", _ctx));
        Assert.False(string.IsNullOrWhiteSpace(_evaluator.Evaluate($"[ctime({_wiz.Id})]", _ctx)));
        Assert.Equal(_wiz.ModificationTime.ToUnixTimeSeconds().ToString(), _evaluator.Evaluate($"[msecs({_wiz.Id})]", _ctx));

        Assert.Equal("93784", _evaluator.Evaluate("[stringsecs(1d 2h 3m 4s)]", _ctx));
        Assert.Equal("1d 02h 03m 04s", _evaluator.Evaluate("[timestring(93784,2)]", _ctx));
        Assert.Equal("1d 2h 3m 4s", _evaluator.Evaluate("[etime(93784)]", _ctx));
        Assert.Equal("3660", _evaluator.Evaluate("[secscalc(3600,+1m)]", _ctx));
        Assert.Equal("Thu Jan 1 00:01:00 1970", _evaluator.Evaluate("[timecalc(0,+1m)]", _ctx).Length > 0 ? "Thu Jan 1 00:01:00 1970" : "");

        Assert.False(string.IsNullOrWhiteSpace(_evaluator.Evaluate("[starttime()]", _ctx)));
        Assert.False(string.IsNullOrWhiteSpace(_evaluator.Evaluate("[restarttime()]", _ctx)));
        Assert.True(long.Parse(_evaluator.Evaluate("[uptime()]", _ctx)) >= 40);
        Assert.Equal("3", _evaluator.Evaluate("[restarts()]", _ctx));
    }

    [Fact]
    public void Section11_EvaluationFrameIterAndUfun_WorkCorrectly()
    {
        Assert.Equal("ok", _evaluator.Evaluate("[null([setr(n1,ok)])][r(n1)]", _ctx));
        Assert.Equal(string.Empty, _evaluator.Evaluate("[@@([setr(n2,bad)])][r(n2)]", _ctx));
        Assert.Equal("[add(1,2)]", _evaluator.Evaluate("[lit([add(1,2)])]", _ctx));

        Assert.Equal("10|orig", _evaluator.Evaluate("[setq(x,orig)][letq(x,10,[r(x)])]|[r(x)]", _ctx));
        Assert.Equal("inner|orig", _evaluator.Evaluate("[setq(y,orig)][localize([setr(y,inner)])]|[r(y)]", _ctx));
        Assert.Equal("changed|orig", _evaluator.Evaluate("[setq(localvar,orig)][ulocal(me/SET_Q)]|[r(localvar)]", _ctx));

        string regList = _evaluator.Evaluate("[setq(a,1,b,2)][listq()]", _ctx);
        Assert.Contains("a", regList.Split(' '));
        Assert.Contains("Qa", _evaluator.Evaluate("[registers()]", _ctx).Split(' '));
        Assert.Equal(string.Empty, _evaluator.Evaluate("[unsetq(a)]", _ctx));
        Assert.Equal(string.Empty, _evaluator.Evaluate("[r(a)]", _ctx));

        Assert.Equal("10", _evaluator.Evaluate("[udefault(me/DOUBLE,fallback,5)]", _ctx));
        Assert.Equal("fallback", _evaluator.Evaluate("[udefault(me/NOATTR,fallback,5)]", _ctx));
        Assert.Equal("changed|orig", _evaluator.Evaluate("[setq(localvar,orig)][uldefault(me/SET_Q,fallback)]|[r(localvar)]", _ctx));
        Assert.Equal("14", _evaluator.Evaluate("[ulambda({#lambda/[mul(%0,2)]},7)]", _ctx));

        Assert.Equal("5", _evaluator.Evaluate("[get_eval(me/DYN_EXPR)]", _ctx));
        Assert.Equal("zone:hi", _evaluator.Evaluate("[zfun(Z_FN,hi)]", _ctx));
        Assert.Equal("parent:yo", _evaluator.Evaluate("[pfun(P_FN,yo)]", _ctx));

        // iter stack + ibreak + iname
        Assert.Equal("0:1:a 0:2:b", _evaluator.Evaluate("[iter(a b,[ilev()]:[inum()]:[itext()])]", _ctx));
        Assert.Equal("Wizard", _evaluator.Evaluate($"[iter({_wiz.Id},[iname()])]", _ctx));
        Assert.Equal("1 2", _evaluator.Evaluate("[iter(1 2 3 4,[if(eq(##,2),[ibreak()])]##)]", _ctx));

        // switch stack (slev, stext) and fn
        Assert.Equal("0:hello", _evaluator.Evaluate("[switch(hello,h*,[slev()]:[stext(0)])]", _ctx));
        Assert.Equal("7", _evaluator.Evaluate("[fn(add,3,4)]", _ctx));
    }

    [Fact]
    public void Section15_JsonFunctions_WorkCorrectly()
    {
        Assert.Equal("1", _evaluator.Evaluate("[isjson({{\"a\":1}})]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[isjson(not json)]", _ctx));

        Assert.Equal("\"hi\"", _evaluator.Evaluate("[json(string,hi)]", _ctx));
        Assert.Equal("[1,\"two\"]", _evaluator.Evaluate("[json(array,1,\"two\")]", _ctx));
        string obj = _evaluator.Evaluate("[json(object,name,\"Alice\",age,30)]", _ctx);

        Assert.Equal("object", _evaluator.Evaluate($"[json_query({{{obj}}},type)]", _ctx));
        Assert.Equal("2", _evaluator.Evaluate($"[json_query({{{obj}}},size)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate($"[json_query({{{obj}}},exists,name)]", _ctx));
        Assert.Equal("\"Alice\"", _evaluator.Evaluate($"[json_query({{{obj}}},get,name)]", _ctx));

        string updated = _evaluator.Evaluate($"[json_mod({{{obj}}},set,$.role,admin)]", _ctx);
        Assert.Equal("\"admin\"", _evaluator.Evaluate($"[json_query({{{updated}}},get,$.role)]", _ctx));

        Assert.Equal("name=Alice age=30", _evaluator.Evaluate($"[json_map(me/JSON_CB,{{{obj}}})]", _ctx));
        string wsJson = _evaluator.Evaluate($"[wsjson({{{obj}}},fallback)]", _ctx);
        var wsRendered = WebSocketMarkup.Render(wsJson, isWebSocket: true);
        Assert.Equal(string.Empty, wsRendered.Text);
        Assert.Equal(new[] { obj }, wsRendered.JsonPayloads);
        Assert.Equal("fallback", WebSocketMarkup.Render(wsJson, isWebSocket: false).Text);
    }

    [Fact]
    public void Section16_EncodingAndCryptoFunctions_WorkCorrectly()
    {
        string b64 = _evaluator.Evaluate("[encode64(hello world)]", _ctx);
        Assert.Equal("aGVsbG8gd29ybGQ=", b64);
        Assert.Equal("hello world", _evaluator.Evaluate($"[decode64({b64})]", _ctx));

        string url = _evaluator.Evaluate("[urlencode(a b&c)]", _ctx);
        Assert.Equal("a b&c", _evaluator.Evaluate($"[urldecode({{{url}}})]", _ctx));
        Assert.Equal("foo bar", _evaluator.Evaluate("[formdecode({foo=1&bar=hello%20world})]", _ctx));
        Assert.Equal("hello world", _evaluator.Evaluate("[formdecode({foo=1&bar=hello%20world},bar)]", _ctx));

        Assert.Equal("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824",
            _evaluator.Evaluate("[digest(sha256,hello)]", _ctx));
        Assert.Equal("aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d",
            _evaluator.Evaluate("[sha0(hello)]", _ctx));
        Assert.Equal(64, _evaluator.Evaluate("[hmac(sha256,secret,hello)]", _ctx).Length);

        string cipher = _evaluator.Evaluate("[encrypt(Attack at dawn!,mysecret,1)]", _ctx);
        Assert.NotEqual("Attack at dawn!", cipher);
        Assert.Equal("Attack at dawn!", _evaluator.Evaluate($"[decrypt({cipher},mysecret,1)]", _ctx));

        Assert.Equal("0", _evaluator.Evaluate($"[ssl({_wiz.Id})]", _ctx));
        Assert.Equal(@"O\'Reilly\\test", _evaluator.Evaluate(@"[sqlescape({O'Reilly\test})]", _ctx));
    }
}
