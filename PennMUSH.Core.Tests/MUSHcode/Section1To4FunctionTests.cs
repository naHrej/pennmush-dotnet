namespace PennMUSH.Core.Tests.MUSHcode;

using System;
using System.Linq;
using Xunit;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class Section1To4FunctionTests
{
    private readonly ObjectDatabase _db;
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly EvaluationContext _ctx;
    private readonly MUSHObject _room;
    private readonly MUSHObject _wiz;
    private readonly MUSHObject _alice;
    private readonly MUSHObject _bob;
    private readonly MUSHObject _sword;
    private readonly MUSHObject _shield;

    public Section1To4FunctionTests()
    {
        _db = new ObjectDatabase();
        _room = _db.CreateObject("Grand Hall", ObjectType.Room, Dbref.Nothing);         // #0
        _wiz = _db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);          // #1
        _wiz.Location = _room.Id;
        _wiz.Owner = _wiz.Id;
        _wiz.AddFlag("WIZARD");

        _alice = _db.CreateObject("Alice", ObjectType.Player, _wiz.Id);               // #2
        _alice.Location = _room.Id;

        _bob = _db.CreateObject("Robert Bob", ObjectType.Player, _wiz.Id);            // #3
        _bob.Location = _room.Id;

        _sword = _db.CreateObject("Steel Longsword", ObjectType.Thing, _wiz.Id);      // #4
        _sword.Location = _room.Id;

        _shield = _db.CreateObject("Oak Shield", ObjectType.Thing, _wiz.Id);          // #5
        _shield.Location = _room.Id;

        _wiz.SetAttribute("DOUBLE", "[mul(%0,2)]", _wiz.Id);
        _wiz.SetAttribute("SUM2", "[add(%0,%1)]", _wiz.Id);
        _wiz.SetAttribute("MIX2", "%0-%1-%2", _wiz.Id);
        _wiz.SetAttribute("BADPLAYER", "BAD(%0,%1)", _wiz.Id);
        _wiz.SetAttribute("SORTKEY_LEN", "[strlen(%0)]", _wiz.Id);
        _wiz.SetAttribute("ALPHA", "Foo Bar 123", _wiz.Id);
        _wiz.SetAttribute("BETA", "baz QUX", _wiz.Id);
        _wiz.SetAttribute("GAMMA", "Other Text", _wiz.Id);

        _registry = new FunctionRegistry(_db);
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;

        _registry.SaveSoft(new SoftFunction
        {
            Name = "MYLOCALFN",
            Thing = _wiz.Id,
            Attribute = "DOUBLE",
            MinArgs = 1,
            MaxArgs = 1
        });

        _ctx = new EvaluationContext(_wiz.Id, _wiz.Id, _wiz.Id, _room.Id);
    }

    [Fact]
    public void Section1_AliasesAndMovedNames_WorkCorrectly()
    {
        Assert.Equal("olleh", _evaluator.Evaluate("[flip(hello)]", _ctx));
        Assert.Equal("olleh", _evaluator.Evaluate("[reverse(hello)]", _ctx));
        Assert.Equal("a b X c d", _evaluator.Evaluate("[linsert(a b c d,3,X)]", _ctx));
        Assert.Equal("a b X c", _evaluator.Evaluate("[linsert(a b c,-2,X)]", _ctx));
        Assert.Equal("a X c d", _evaluator.Evaluate("[lreplace(a b c d,2,X)]", _ctx));
        Assert.Equal("a|X|c|X", _evaluator.Evaluate("[lreplace(a b c d,2 -1,X, ,|)]", _ctx));
        Assert.Equal("a d", _evaluator.Evaluate("[ldelete(a b c d,2 3)]", _ctx));
        Assert.Equal("a;c", _evaluator.Evaluate("[ldelete(a b c d,2 -1, ,;)]", _ctx));
        Assert.Equal("2", _evaluator.Evaluate("[element(foo bar baz,b*r)]", _ctx));
        Assert.Equal("ONE:one TWO:two", _evaluator.Evaluate("[parse(one two,ucstr(##):##)]", _ctx));
        // Verify parse evaluates its body lazily using ##
        Assert.Equal("2 4 6", _evaluator.Evaluate("[parse(1 2 3,[mul(##,2)])]", _ctx));
        Assert.Equal("20", _evaluator.Evaluate("[avg(10,20,30)]", _ctx));
        Assert.Equal("3", _evaluator.Evaluate("[val(3.99)]", _ctx));
        Assert.Equal("2.718282", _evaluator.Evaluate("[exp(1)]", _ctx));
        Assert.Equal("hlo", _evaluator.Evaluate("[delete(hello,1,2)]", _ctx));
    }

    [Fact]
    public void Section1_NameList_Functions_List_WorkCorrectly()
    {
        Assert.Equal("#1 #2 #-1", _evaluator.Evaluate("[namelist(*Wizard Alice nobody)]", _ctx));
        Assert.Equal("#2 BAD(nobody,#-1)", _evaluator.Evaluate("[namelist(Alice nobody,me/BADPLAYER)]", _ctx));
        Assert.Equal("#-1 NO SUCH ATTRIBUTE", _evaluator.Evaluate("[namelist(Alice,me/NOATTR)]", _ctx));

        string builtins = _evaluator.Evaluate("[functions(builtin)]", _ctx);
        Assert.Contains("FLIP", builtins.Split(' '));
        Assert.DoesNotContain("MYLOCALFN", builtins.Split(' '));

        string locals = _evaluator.Evaluate("[functions(local)]", _ctx);
        Assert.Equal("MYLOCALFN", locals);

        string allFns = _evaluator.Evaluate("[functions()]", _ctx);
        Assert.Contains("MYLOCALFN", allFns.Split(' '));
        Assert.Contains("FLIP", allFns.Split(' '));

        Assert.Equal("#-1", _evaluator.Evaluate("[functions(bogus)]", _ctx));

        Assert.Equal("MYLOCALFN", _evaluator.Evaluate("[list(local)]", _ctx));
        Assert.Equal("MYLOCALFN", _evaluator.Evaluate("[list(@functions)]", _ctx));
        Assert.Equal("MYLOCALFN", _evaluator.Evaluate("[list(functions,local)]", _ctx));
        Assert.Contains("DESCRIBE", _evaluator.Evaluate("[list(attribs)]", _ctx).Split(' '));
        Assert.Contains("WIZARD", _evaluator.Evaluate("[list(flags)]", _ctx).Split(' '));
        Assert.Contains("Builder", _evaluator.Evaluate("[list(powers)]", _ctx).Split(' '));
        Assert.Contains("Basic", _evaluator.Evaluate("[list(locks)]", _ctx).Split(' '));
        Assert.Equal("#-1", _evaluator.Evaluate("[list(unknowncat)]", _ctx));
    }

    [Fact]
    public void Section2_BooleanAndComparisonFunctions_WorkCorrectly()
    {
        Assert.Equal("1", _evaluator.Evaluate("[and(1,2,yes)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[and(1,0,yes)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[or(0,0,yes)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[or(0,0,0)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[not(0)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[not(1)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[xor(0,1,0)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[xor(1,1,0)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[nand(1,0,1)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[nand(1,1,1)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[nor(0,0,0)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[nor(0,1,0)]", _ctx));

        // Short-circuit checks using setr side effects
        Assert.Equal("0|", _evaluator.Evaluate("[cand(0,[setr(sc1,ran)])]|[r(sc1)]", _ctx));
        Assert.Equal("1|", _evaluator.Evaluate("[ncand(0,[setr(sc2,ran)])]|[r(sc2)]", _ctx));
        Assert.Equal("1|", _evaluator.Evaluate("[cor(1,[setr(sc3,ran)])]|[r(sc3)]", _ctx));
        Assert.Equal("0|", _evaluator.Evaluate("[ncor(1,[setr(sc4,ran)])]|[r(sc4)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[ncand(1,1)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[ncor(0,0)]", _ctx));

        // Comparisons with 2+ numbers
        Assert.Equal("1", _evaluator.Evaluate("[eq(5,5.0,5)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[eq(5,6)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[neq(5,6)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[neq(5,5,5)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[gt(10,5,1)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[gt(10,10,1)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[gte(10,10,1)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[lt(1,5,10)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[lte(1,5,5)]", _ctx));
        Assert.Equal("#-1 COMPARISON REQUIRES 2 OR MORE NUMBERS", _evaluator.Evaluate("[gt(5)]", _ctx));
        Assert.Equal("#-1 ARGUMENTS MUST BE NUMBERS", _evaluator.Evaluate("[lt(5,abc)]", _ctx));
    }

    [Fact]
    public void Section2_ConditionalsSwitchAndSelection_WorkCorrectly()
    {
        Assert.Equal("a|", _evaluator.Evaluate("[ifelse(1,a,[setr(ife1,bad)])]|[r(ife1)]", _ctx));
        Assert.Equal("second", _evaluator.Evaluate("[cond(0,first,1,second,default)]", _ctx));
        Assert.Equal("firstsecond", _evaluator.Evaluate("[condall(1,first,1,second,0,third,default)]", _ctx));
        Assert.Equal("first", _evaluator.Evaluate("[ncond(0,first,1,second)]", _ctx));
        Assert.Equal("firstthird", _evaluator.Evaluate("[ncondall(0,first,1,second,0,third)]", _ctx));

        // switch/case with #$ and ranges
        Assert.Equal("val:50", _evaluator.Evaluate("[switch(50,<10,low,<100,val:#$,high)]", _ctx));
        Assert.Equal("match1match2", _evaluator.Evaluate("[switchall(cat,c*,match1,*t,match2,none)]", _ctx));
        Assert.Equal("exact", _evaluator.Evaluate("[case(a*b,xyz,miss,a*b,exact,def)]", _ctx));
        Assert.Equal("one two ", _evaluator.Evaluate("[caseall(x,x,one ,y,skip,x,two ,def)]", _ctx));

        // reswitch forms (regex pattern containing [] must be braced in MUSH expressions)
        Assert.Equal("matched:123", _evaluator.Evaluate("[reswitch(123,^a,alpha,{^[0-9]+$},matched:#$,def)]", _ctx));
        Assert.Equal("12", _evaluator.Evaluate("[reswitchall(abc,a,1,^x,nope,c$,2,def)]", _ctx));
        Assert.Equal("hit", _evaluator.Evaluate("[reswitchi(ABC,^abc$,hit,miss)]", _ctx));
        Assert.Equal("AB", _evaluator.Evaluate("[reswitchalli(AbC,a,A,z,Z,c$,B)]", _ctx));

        // firstof / allof
        Assert.Equal("2|", _evaluator.Evaluate("[firstof(0,2,[setr(fo1,bad)],def)]|[r(fo1)]", _ctx));
        Assert.Equal("2,3", _evaluator.Evaluate("[allof(0,2,0,3,\\,)]", _ctx));
        Assert.Equal("hi", _evaluator.Evaluate("[strfirstof(,hi,there)]", _ctx));
        Assert.Equal("a|0|c", _evaluator.Evaluate("[strallof(a,,0,,c,|)]", _ctx));
    }

    [Fact]
    public void Section3_StringFunctions_WorkCorrectly()
    {
        Assert.Equal("4", _evaluator.Evaluate("[pos(def,abcdefdef)]", _ctx));
        Assert.Equal("#-1", _evaluator.Evaluate("[pos(xyz,abcdef)]", _ctx));
        Assert.Equal("hel", _evaluator.Evaluate("[left(hello,3)]", _ctx));
        Assert.Equal("llo", _evaluator.Evaluate("[right(hello,3)]", _ctx));
        Assert.Equal("a b c", _evaluator.Evaluate("[squish({  a   b   c  })]", _ctx));
        Assert.Equal("a-b-c", _evaluator.Evaluate("[squish(--a--b---c--,-)]", _ctx));
        Assert.Equal("2 1 2 2 1 1", _evaluator.Evaluate(@"[brackets({[[()(){}]})]", _ctx));

        Assert.Equal("hi  |12| mid ", _evaluator.Evaluate("[align(4 >2 -5,hi,12,mid, ,|)]", _ctx));
        Assert.Equal("hi  | 12", _evaluator.Evaluate("[lalign(4 >3,hi|12,|, ,|)]", _ctx));
        Assert.Equal("abc", _evaluator.Evaluate("[lcstr2(ABC)]", _ctx));
        Assert.Equal("ABC", _evaluator.Evaluate("[ucstr2(abc)]", _ctx));

        Assert.Equal("uryyb", _evaluator.Evaluate("[tr(hello,a-z,n-za-m)]", _ctx));
        Assert.Equal("#-1 STRING LENGTHS MUST BE EQUAL", _evaluator.Evaluate("[tr(abc,ab,xyz)]", _ctx));

        Assert.Equal("heXYZllo", _evaluator.Evaluate("[strinsert(hello,2,XYZ)]", _ctx));
        Assert.Equal("hlo", _evaluator.Evaluate("[strdelete(hello,1,2)]", _ctx));
        Assert.Equal("heXYo", _evaluator.Evaluate("[strreplace(hello,2,2,XY)]", _ctx));

        Assert.Equal("Cafe naive", _evaluator.Evaluate("[stripaccents(Café naïve)]", _ctx));
        Assert.Equal(@"a b\[c\]%r", _evaluator.Evaluate("[decompose({a b[c]\n})]", _ctx));

        Assert.Equal("abc", _evaluator.Evaluate("[trimpenn(xxabcxx,x)]", _ctx));
        Assert.Equal("abcxx", _evaluator.Evaluate("[trimpenn(xxabcxx,x,l)]", _ctx));
        Assert.Equal("xxabc", _evaluator.Evaluate("[trimtiny(xxabcxx,r,x)]", _ctx));

        Assert.Equal("1", _evaluator.Evaluate("[strmatch(foo bar baz,foo * *,q1 q2)]", _ctx));
        Assert.Equal("bar|baz", _evaluator.Evaluate("[r(q1)]|[r(q2)]", _ctx));

        Assert.Equal("1", _evaluator.Evaluate("[soundslike(Smith,Smythe)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[soundlike(Robert,Rupert)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[soundslike(Apple,Zebra)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[soundslike(Phone,Fone,phone)]", _ctx));
        Assert.Equal("#-1 UNKNOWN HASH TYPE", _evaluator.Evaluate("[soundex(Test,bogus)]", _ctx));
        Assert.Equal("fifth", _evaluator.Evaluate("[ordinal(5)]", _ctx));
        Assert.Equal("twenty-second", _evaluator.Evaluate("[ordinal(22)]", _ctx));
        Assert.Equal("abcdef", _evaluator.Evaluate("[merge(ab__ef,xxcdxx,_)]", _ctx));
    }

    [Fact]
    public void Section3_ListAndDbFunctions_WorkCorrectly()
    {
        Assert.Equal("a d", _evaluator.Evaluate("[setsymdiff(a b c,b c d)]", _ctx));
        Assert.Equal("a bbb cccc", _evaluator.Evaluate("[sortkey(me/SORTKEY_LEN,cccc a bbb)]", _ctx));
        Assert.Equal("3", _evaluator.Evaluate("[wordpos(hello world foo bar,13)]", _ctx));
        Assert.Contains(_evaluator.Evaluate("[randword(red green blue)]", _ctx), new[] { "red", "green", "blue" });
        Assert.Contains(_evaluator.Evaluate("[pickrand(red green blue)]", _ctx), new[] { "red", "green", "blue" });
        Assert.Equal("3", _evaluator.Evaluate("[words([randextract(a b c d e,3)])]", _ctx));
        Assert.Equal("11 22", _evaluator.Evaluate("[mix(me/SUM2,1 2,10 20)]", _ctx));
        Assert.Equal("1-a-x:2-b-y", _evaluator.Evaluate("[mix(me/MIX2,1:2,a:b,x:y,:)]", _ctx));
        Assert.Equal("apples, bananas, and cherries", _evaluator.Evaluate("[elist(apples bananas cherries)]", _ctx));

        string refs = $"{_wiz.Id} {_alice.Id} {_bob.Id} {_sword.Id} {_shield.Id}";
        Assert.Equal(_alice.Id.ToString(), _evaluator.Evaluate($"[namegrab({refs},Alice)]", _ctx));
        Assert.Equal(_bob.Id.ToString(), _evaluator.Evaluate($"[namegrab({refs},Bob)]", _ctx));
        Assert.Equal($"{_sword.Id}|{_shield.Id}", _evaluator.Evaluate($"[namegraball({refs.Replace(' ', '|')},S,|)]", _ctx));

        Assert.Equal("assigned", _evaluator.Evaluate("[setr(sr1,assigned)]", _ctx));
        Assert.Equal("assigned", _evaluator.Evaluate("[r(sr1)]", _ctx));
    }

    [Fact]
    public void Section3_MathAndVectorFunctions_WorkCorrectly()
    {
        // Multi-arg existing functions
        Assert.Equal("2", _evaluator.Evaluate("[sub(10,5,3)]", _ctx));
        Assert.Equal("2", _evaluator.Evaluate("[div(20,5,2)]", _ctx));
        Assert.Equal("2.5", _evaluator.Evaluate("[fdiv(20,4,2)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[band(7,3,5)]", _ctx));
        Assert.Equal("7", _evaluator.Evaluate("[bor(1,2,4)]", _ctx));
        Assert.Equal("6", _evaluator.Evaluate("[bxor(1,3,4)]", _ctx));

        // Modulo vs Remainder vs FloorDiv vs FMod
        Assert.Equal("-4", _evaluator.Evaluate("[floordiv(-13,4)]", _ctx));
        Assert.Equal("3", _evaluator.Evaluate("[modulo(-13,4)]", _ctx));
        Assert.Equal("-3", _evaluator.Evaluate("[mod(13,-4)]", _ctx));
        Assert.Equal("-1", _evaluator.Evaluate("[modulus(-13,-4)]", _ctx));
        Assert.Equal("-1", _evaluator.Evaluate("[remainder(-13,4)]", _ctx));
        Assert.Equal("1.5", _evaluator.Evaluate("[fmod(5.5,2)]", _ctx));

        // Constants, trig & angle units
        Assert.StartsWith("3.14159", _evaluator.Evaluate("[pi()]", _ctx));
        Assert.StartsWith("2.71828", _evaluator.Evaluate("[e()]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[round([sin(90,d)],6)]", _ctx));
        Assert.Equal("0", _evaluator.Evaluate("[round([cos(100,g)],6)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[round([tan(45,d)],6)]", _ctx));
        Assert.Equal("90", _evaluator.Evaluate("[round([asin(1,d)],6)]", _ctx));
        Assert.Equal("90", _evaluator.Evaluate("[round([acos(0,d)],6)]", _ctx));
        Assert.Equal("45", _evaluator.Evaluate("[round([atan(1,d)],6)]", _ctx));
        Assert.Equal("45", _evaluator.Evaluate("[round([atan2(1,1,d)],6)]", _ctx));
        Assert.Equal("180", _evaluator.Evaluate("[round([ctu(200,g,d)],6)]", _ctx));
        Assert.Equal("#-1 INVALID ANGLE TYPE", _evaluator.Evaluate("[ctu(90,x,d)]", _ctx));

        // Fraction, BaseConv, BNand, LMath, Rand, Die
        Assert.Equal("3/4", _evaluator.Evaluate("[fraction(0.75)]", _ctx));
        Assert.Equal("1 1/4", _evaluator.Evaluate("[fraction(1.25,1)]", _ctx));
        Assert.Equal("ff", _evaluator.Evaluate("[baseconv(255,10,16)]", _ctx));
        Assert.Equal("-ff", _evaluator.Evaluate("[baseconv(-255,10,16)]", _ctx));
        Assert.Equal("255", _evaluator.Evaluate("[baseconv(FF,16,10)]", _ctx));
        Assert.Equal("#-1 FROM BASE OUT OF RANGE", _evaluator.Evaluate("[baseconv(1,1,10)]", _ctx));
        Assert.Equal("#-1 TO BASE OUT OF RANGE", _evaluator.Evaluate("[baseconv(1,10,65)]", _ctx));
        Assert.Equal("#-1 MALFORMED NUMBER", _evaluator.Evaluate("[baseconv(9,8,10)]", _ctx));
        Assert.Equal("4", _evaluator.Evaluate("[bnand(6,2)]", _ctx));

        Assert.Equal("15", _evaluator.Evaluate("[lmath(add,1 2 3 4 5)]", _ctx));
        Assert.Equal("3", _evaluator.Evaluate("[lmath(mean,2|4,|)]", _ctx));
        Assert.Equal("1", _evaluator.Evaluate("[lmath(lt,1 2 3)]", _ctx));
        Assert.Equal("#-1 UNKNOWN OPERATION", _evaluator.Evaluate("[lmath(bogus,1 2)]", _ctx));

        double r0 = double.Parse(_evaluator.Evaluate("[rand()]", _ctx), System.Globalization.CultureInfo.InvariantCulture);
        Assert.InRange(r0, 0.0, 1.0);
        int r = int.Parse(_evaluator.Evaluate("[rand(5,10)]", _ctx));
        Assert.InRange(r, 5, 10);
        int die = int.Parse(_evaluator.Evaluate("[die(3,6)]", _ctx));
        Assert.InRange(die, 3, 18);
        var rolls = _evaluator.Evaluate("[die(4,6,1)]", _ctx).Split(' ').Select(int.Parse).ToArray();
        Assert.Equal(4, rolls.Length);
        Assert.All(rolls, v => Assert.InRange(v, 1, 6));

        // Vectors
        Assert.Equal("5 7 9", _evaluator.Evaluate("[vadd(1 2 3,4 5 6)]", _ctx));
        Assert.Equal("3 3 3", _evaluator.Evaluate("[vsub(4 5 6,1 2 3)]", _ctx));
        Assert.Equal("2 4 6", _evaluator.Evaluate("[vmul(2,1 2 3)]", _ctx));
        Assert.Equal("4 10 18", _evaluator.Evaluate("[vmul(1 2 3,4 5 6)]", _ctx));
        Assert.Equal("32", _evaluator.Evaluate("[vdot(1 2 3,4 5 6)]", _ctx));
        Assert.Equal("-3 6 -3", _evaluator.Evaluate("[vcross(1 2 3,4 5 6)]", _ctx));
        Assert.Equal("5", _evaluator.Evaluate("[vmag(3 4)]", _ctx));
        Assert.Equal("3", _evaluator.Evaluate("[vdim(10 20 30)]", _ctx));
        Assert.Equal("0.6 0.8", _evaluator.Evaluate("[vunit(3 4)]", _ctx));
        Assert.Equal("4 5 6", _evaluator.Evaluate("[vmax(1 5 3,4 2 6)]", _ctx));
        Assert.Equal("1 2 3", _evaluator.Evaluate("[vmin(1 5 3,4 2 6)]", _ctx));
        Assert.Equal("#-1 VECTORS MUST BE SAME DIMENSIONS", _evaluator.Evaluate("[vadd(1 2,1 2 3)]", _ctx));
        Assert.Equal("#-1 VECTORS MUST BE THREE-DIMENSIONAL", _evaluator.Evaluate("[vcross(1 2,3 4)]", _ctx));
    }

    [Fact]
    public void Section4_RegexFunctions_WorkCorrectly()
    {
        Assert.Equal("Hxllo World", _evaluator.Evaluate("[regediti(Hello World,e,x)]", _ctx));
        Assert.Equal("HxllX WXrld", _evaluator.Evaluate("[regeditalli(Hello World,E,x,o,X)]", _ctx));
        Assert.Equal("Hello", _evaluator.Evaluate("[regrabi(Hello world,^hello)]", _ctx));
        Assert.Equal("Apple|apricot", _evaluator.Evaluate("[regraballi(Apple banana apricot,^a, ,|)]", _ctx));

        Assert.Equal("ALPHA", _evaluator.Evaluate($"[regrep({_wiz.Id},*,Bar)]", _ctx));
        Assert.Equal("ALPHA BETA", _evaluator.Evaluate($"[regrepi({_wiz.Id},*,foo|qux)]", _ctx));

        Assert.Equal("3", _evaluator.Evaluate("[reglmatch(one two Test four,^Test)]", _ctx));
        Assert.Equal("3", _evaluator.Evaluate("[reglmatchi(one two test four,^TEST$)]", _ctx));
        Assert.Equal(string.Empty, _evaluator.Evaluate("[reglmatch(one two three,zzz)]", _ctx));
        Assert.Equal("1|3", _evaluator.Evaluate("[reglmatchall(apple banana apricot,^a, ,|)]", _ctx));
        Assert.Equal("1 3", _evaluator.Evaluate("[reglmatchalli(Apple banana apricot,^a)]", _ctx));
    }
}
