namespace PennMUSH.Core.Tests.MUSHcode;

using System;
using Xunit;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using PennMUSH.Core.Locks;

public class FunctionCatalogTests
{
    private readonly ObjectDatabase _db;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly EvaluationContext _ctx;
    private readonly MUSHObject _god;
    private readonly MUSHObject _room;

    public FunctionCatalogTests()
    {
        _db = new ObjectDatabase();
        _room = _db.CreateObject("Test Room", ObjectType.Room, Dbref.Nothing); // #0
        _god = _db.CreateObject("God", ObjectType.Player, Dbref.Nothing);      // #1
        _god.Location = _room.Id;
        _god.AddFlag("WIZARD");

        _registry = new FunctionRegistry(_db);
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;

        _ctx = new EvaluationContext(_god.Id, _god.Id, _god.Id, _room.Id);
    }

    [Fact]
    public void ListFunctions_BasicOperations_WorkCorrectly()
    {
        Assert.Equal("apple", _evaluator.Evaluate("[first(apple banana cherry)]", _ctx));
        Assert.Equal("banana cherry", _evaluator.Evaluate("[rest(apple banana cherry)]", _ctx));
        Assert.Equal("cherry", _evaluator.Evaluate("[last(apple banana cherry)]", _ctx));
        Assert.Equal("3", _evaluator.Evaluate("[words(apple banana cherry)]", _ctx));
        Assert.Equal("2", _evaluator.Evaluate("[member(apple banana cherry,banana)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[member(apple banana cherry,orange)]", _ctx));
        Assert.Equal("cherry banana apple", _evaluator.Evaluate("[revwords(apple banana cherry)]", _ctx));
        Assert.Equal("apple banana cherry", _evaluator.Evaluate("[unique(apple banana apple cherry banana)]", _ctx));
    }

    [Fact]
    public void ListFunctions_PatternMatching_WorksCorrectly()
    {
        Assert.Equal("2", _evaluator.Evaluate("[match(foo bar baz,b*r)]", _ctx));
        Assert.Equal("2 3", _evaluator.Evaluate("[matchall(foo bar baz,b*)]", _ctx));
        Assert.Equal("bar", _evaluator.Evaluate("[grab(foo bar baz,b*r)]", _ctx));
        Assert.Equal("bar baz", _evaluator.Evaluate("[graball(foo bar baz,b*)]", _ctx));
    }

    [Fact]
    public void ListFunctions_ExtractionAndModification_WorkCorrectly()
    {
        Assert.Equal("b c", _evaluator.Evaluate("[extract(a b c d e,2,2)]", _ctx));
        Assert.Equal("a b X c d", _evaluator.Evaluate("[insert(a b c d,3,X)]", _ctx));
        Assert.Equal("a b X d", _evaluator.Evaluate("[replace(a b c d,3,X)]", _ctx));
        Assert.Equal("a b d", _evaluator.Evaluate("[ldelete(a b c d,3)]", _ctx));
        Assert.Equal("a c", _evaluator.Evaluate("[remove(a b c,b)]", _ctx));
        Assert.Equal("b d", _evaluator.Evaluate("[elements(a b c d e,2 4)]", _ctx));
    }

    [Fact]
    public void ListFunctions_SetOperationsAndGeneration_WorkCorrectly()
    {
        Assert.Equal("a b c d e", _evaluator.Evaluate("[setunion(a b c,c d e)]", _ctx));
        Assert.Equal("b c", _evaluator.Evaluate("[setinter(a b c,b c d)]", _ctx));
        Assert.Equal("a", _evaluator.Evaluate("[setdiff(a b c,b c d)]", _ctx));
        Assert.Equal("0 1 2 3 4", _evaluator.Evaluate("[lnum(5)]", _ctx));
        Assert.Equal("10 12 14", _evaluator.Evaluate("[lnum(10,14,2)]", _ctx));
        Assert.Equal("a, b, and c", _evaluator.Evaluate("[itemize(a b c)]", _ctx));
        Assert.Equal("a c e", _evaluator.Evaluate("[step(a b c d e,2)]", _ctx));
    }

    [Fact]
    public void ListFunctions_Iter_SubstitutesAndEvaluates()
    {
        Assert.Equal("ITEM:a ITEM:b ITEM:c", _evaluator.Evaluate("[iter(a b c,ITEM:##)]", _ctx));
        Assert.Equal("1:a,2:b,3:c", _evaluator.Evaluate("[iter(a b c,#@:##, ,\\,)]", _ctx));
    }

    [Fact]
    public void StringFunctions_FormattingAndPadding_WorkCorrectly()
    {
        Assert.Equal("hello     ", _evaluator.Evaluate("[ljust(hello,10)]", _ctx));
        Assert.Equal("     hello", _evaluator.Evaluate("[rjust(hello,10)]", _ctx));
        Assert.Equal("  hello   ", _evaluator.Evaluate("[center(hello,10)]", _ctx));
        Assert.Equal("Hello", _evaluator.Evaluate("[capstr(hello)]", _ctx));
        Assert.Equal("a dog", _evaluator.Evaluate("[art(dog)]", _ctx));
        Assert.Equal("an apple", _evaluator.Evaluate("[art(apple)]", _ctx));
        Assert.Equal("he", _evaluator.Evaluate("[subj(male)]", _ctx));
        Assert.Equal("her", _evaluator.Evaluate("[poss(female)]", _ctx));
        Assert.Equal("them", _evaluator.Evaluate("[obj(plural)]", _ctx));
        Assert.Equal("   ", _evaluator.Evaluate("[space(3)]", _ctx));
    }

    [Fact]
    public void StringFunctions_SearchAndReplace_WorkCorrectly()
    {
        Assert.Equal("heXXo worXd", _evaluator.Evaluate("[edit(hello world,l,X)]", _ctx));
        Assert.Equal("foo", _evaluator.Evaluate("[before(foo:bar,:)]", _ctx));
        Assert.Equal("bar", _evaluator.Evaluate("[after(foo:bar,:)]", _ctx));
        Assert.Equal("4", _evaluator.Evaluate("[lpos(abcdef,d)]", _ctx));
        Assert.Equal("65", _evaluator.Evaluate("[ord(A)]", _ctx));
        Assert.Equal("A", _evaluator.Evaluate("[chr(65)]", _ctx));
        Assert.Equal("forty-two", _evaluator.Evaluate("[spellnum(42)]", _ctx));
        Assert.Equal("1st", _evaluator.Evaluate("[ordnumber(1)]", _ctx));
        Assert.Equal("23rd", _evaluator.Evaluate("[ordnumber(23)]", _ctx));
    }

    [Fact]
    public void StringFunctions_SoundexAndEscape_WorkCorrectly()
    {
        Assert.Equal("W252", _evaluator.Evaluate("[soundex(Washington)]", _ctx));
        Assert.Equal(@"hello\[world\]", _evaluator.Evaluate(@"[secure({hello[world]})]", _ctx));
        Assert.Equal("Clean Text", _evaluator.Evaluate("[strip({\x1B[1;31mClean Text\x1B[0m})]", _ctx));
    }

    [Fact]
    public void RegexFunctions_MatchingAndEditing_WorkCorrectly()
    {
        Assert.Equal("1", _evaluator.Evaluate("[isregexp({^[0-9]+$})]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[isregexp((unclosed)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[regmatch(12345,{^[0-9]+$})]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[regmatch(abc123,{^[0-9]+$})]", _ctx));
        Assert.Equal("h*llo world", _evaluator.Evaluate("[regedit(hello world,{[aeiou]},*)]", _ctx));
        Assert.Equal("h*ll* w*rld", _evaluator.Evaluate("[regeditall(hello world,{[aeiou]},*)]", _ctx));
        Assert.Equal("apple", _evaluator.Evaluate("[regrab(apple banana cherry,^a)]", _ctx));
        Assert.Equal("banana cherry", _evaluator.Evaluate("[regraball(apple banana cherry,^b|^c)]", _ctx));
    }

    [Fact]
    public void MathFunctions_BitwiseAndAdvanced_WorkCorrectly()
    {
        Assert.Equal("1", _evaluator.Evaluate("[band(3,5)]", _ctx));
        Assert.Equal("7", _evaluator.Evaluate("[bor(3,5)]", _ctx));
        Assert.Equal("6", _evaluator.Evaluate("[bxor(3,5)]", _ctx));
        Assert.Equal("12", _evaluator.Evaluate("[shl(3,2)]", _ctx));
        Assert.Equal("3", _evaluator.Evaluate("[shr(12,2)]", _ctx));
        Assert.Equal("2.5", _evaluator.Evaluate("[fdiv(5,2)]", _ctx));
        Assert.Equal("2", _evaluator.Evaluate("[div(5,2)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[mod(5,2)]", _ctx));
        Assert.Equal("10", _evaluator.Evaluate("[bound(5,10,20)]", _ctx));
        Assert.Equal("5", _evaluator.Evaluate("[inc(4)]", _ctx));
        Assert.Equal("3", _evaluator.Evaluate("[dec(4)]", _ctx));
        Assert.Equal("4", _evaluator.Evaluate("[sqrt(16)]", _ctx));
        Assert.Equal("8", _evaluator.Evaluate("[power(2,3)]", _ctx));
        Assert.Equal("5", _evaluator.Evaluate("[dist2d(0,0,3,4)]", _ctx));
        Assert.Equal("20", _evaluator.Evaluate("[mean(10 20 30)]", _ctx));
        Assert.Equal("20", _evaluator.Evaluate("[median(10 20 30)]", _ctx));
    }

    [Fact]
    public void TimeFunctions_Conversions_WorkCorrectly()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string secsStr = _evaluator.Evaluate("[secs()]", _ctx);
        Assert.True(long.TryParse(secsStr, out long parsed) && Math.Abs(parsed - now) <= 5);

        Assert.Equal("1d 2h 3m 4s", _evaluator.Evaluate("[secs2time(93784)]", _ctx));
        Assert.Equal("01:02:03", _evaluator.Evaluate("[etimefmt($h:$m:$s,3723)]", _ctx));
        Assert.Contains(DateTime.Now.Year.ToString(), _evaluator.Evaluate("[timefmt(%Y)]", _ctx));
    }

    [Fact]
    public void DbFunctions_IntrospectionAndQueries_WorkCorrectly()
    {
        var thing = _db.CreateObject("Widget", ObjectType.Thing, _god.Id);
        thing.Location = _room.Id;
        thing.SetAttribute("DESC", "A shiny widget.", _god.Id);
        thing.SetAttribute("COLOR", "blue", _god.Id);

        Assert.Equal("#2", _evaluator.Evaluate($"[num(#{thing.Id.Value})]", _ctx));
        Assert.Equal("2", _evaluator.Evaluate($"[rnum(#{thing.Id.Value})]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate($"[hastype(#{thing.Id.Value},thing)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate($"[hastype(#{thing.Id.Value},room)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate($"[controls(#{_god.Id.Value},#{thing.Id.Value})]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate($"[nearby(#{_god.Id.Value},#{thing.Id.Value})]", _ctx));
        Assert.Equal("A shiny widget.", _evaluator.Evaluate($"[xget(#{thing.Id.Value},desc)]", _ctx));
        Assert.Equal("default_val", _evaluator.Evaluate($"[default(#{thing.Id.Value}/nonexistent,default_val)]", _ctx));
        Assert.Equal("COLOR DESC", _evaluator.Evaluate($"[lattr(#{thing.Id.Value})]", _ctx));
        Assert.Equal("2", _evaluator.Evaluate($"[nattr(#{thing.Id.Value})]", _ctx));

        // Test lock
        thing.SetLock(LockType.Basic, new DbrefLockNode(_god.Id), _god.Id);
        Assert.Equal("1", _evaluator.Evaluate($"[testlock(#{thing.Id.Value},Basic,#{_god.Id.Value})]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate($"[testlock(#{thing.Id.Value},Basic,#999)]", _ctx));
    }
}
