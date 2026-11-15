namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class FunctionCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly FunctionRegistry _functions;
    private readonly ExpressionEvaluator _evaluator;
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _ada;
    private readonly Dbref _bea;
    private readonly Dbref _god;
    private readonly Dbref _limbo;

    public FunctionCommandTests()
    {
        var limbo = _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        _limbo = limbo.Id;
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.Owner = god.Id;
        _god = god.Id;
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        ada.Owner = ada.Id;
        _ada = ada.Id;
        var bea = _db.CreateObject("Bea", ObjectType.Player, new Dbref(1));
        bea.Owner = bea.Id;
        _bea = bea.Id;

        _functions = new FunctionRegistry(_db);
        _evaluator = new ExpressionEvaluator(_functions.Dispatch, _db);
        _functions.Evaluator = _evaluator;
        _dispatcher = new CommandDispatcher(_evaluator);
        _dispatcher.Register(new FunctionCommandHandler(_functions), "@function");
    }

    [Fact]
    public void Add_RequiresTheFunctionsPrivilegeAndThenCallsTheAttribute()
    {
        var denied = _dispatcher.Dispatch(_ada, "@function GREET=me/HELLO", _db);
        Assert.Contains("Permission denied.", denied.Notifications);

        _db.GetObject(_ada)!.AddPower("Functions");
        _db.GetObject(_ada)!.SetAttribute("HELLO", "Hi %0", _ada);
        var added = _dispatcher.Dispatch(_ada, "@function GREET=me/HELLO", _db);
        Assert.Contains("Function added.", added.Notifications);

        Assert.Equal("Hi Ada", Call(_ada, "[greet(Ada)]"));
    }

    [Fact]
    public void List_ShowsNamesToEveryoneAndLocationsToThePrivileged()
    {
        _db.GetObject(_god)!.SetAttribute("HELLO", "Hi", _god);
        _dispatcher.Dispatch(_god, "@function GREET=me/HELLO", _db);

        var names = _dispatcher.Dispatch(_ada, "@function", _db);
        Assert.Contains("User functions: GREET", names.Notifications);
        Assert.DoesNotContain(names.Notifications, note => note.Contains("Dbref"));

        var full = _dispatcher.Dispatch(_god, "@function", _db);
        Assert.Contains("Function Name                   Dbref #    Attrib", full.Notifications);
        Assert.Contains($"{"GREET",-32} {_god.Value,6}    HELLO", full.Notifications);

        _functions.RemoveSoft("GREET");
        var none = _dispatcher.Dispatch(_ada, "@function", _db);
        Assert.Contains("No global user-defined functions exist.", none.Notifications);
    }

    [Fact]
    public void Add_RejectsABadNameABuiltinAndAMissingObject()
    {
        var bad = _dispatcher.Dispatch(_god, "@function 123=me/HELLO", _db);
        Assert.Contains("Invalid function name.", bad.Notifications);

        var builtin = _dispatcher.Dispatch(_god, "@function add=me/HELLO", _db);
        Assert.Contains("You cannot change that built-in function.", builtin.Notifications);

        var missing = _dispatcher.Dispatch(_god, "@function GREET=#99/HELLO", _db);
        Assert.Contains("I can't see that here.", missing.Notifications);

        var slash = _dispatcher.Dispatch(_god, "@function GREET=me", _db);
        Assert.Contains("#-1 INVALID SECOND ARGUMENT", slash.Notifications);

        Assert.Contains("Permission denied.", _dispatcher.Dispatch(_ada, "@function/delete GREET", _db).Notifications);
        _db.GetObject(_ada)!.AddPower("Functions");
        _db.GetObject(_god)!.SetAttribute("HELLO", "Hi", _god);
        _dispatcher.Dispatch(_god, "@function GREET=me/HELLO", _db);
        Assert.Contains("You can't delete that @function.", _dispatcher.Dispatch(_ada, "@function/delete GREET", _db).Notifications);
    }

    [Fact]
    public void UpdateReportAliasRestrictAndDelete_FollowTheCommand()
    {
        _db.GetObject(_ada)!.SetAttribute("HELLO", "Hi %0", _ada);
        _dispatcher.Dispatch(_god, $"@function GREET=#{_ada.Value}/HELLO,1,1", _db);
        var updated = _dispatcher.Dispatch(_god, $"@function GREET=Ada/HELLO,1,2", _db);
        Assert.Contains("Function updated.", updated.Notifications);

        var report = _dispatcher.Dispatch(_god, "@function GREET", _db);
        Assert.Contains(report.Notifications, note =>
            note.Contains("Name      : GREET() (Enabled @function)")
            && note.Contains($"Location  : #{_ada.Value}/HELLO")
            && note.Contains("Arguments : 1 to 2"));

        var mortal = _dispatcher.Dispatch(_ada, "@function GREET", _db);
        Assert.Contains(mortal.Notifications, note => note.Contains("Enabled @function") && !note.Contains("Location"));

        Assert.Equal("#-1 FUNCTION (GREET) EXPECTS 1 OR 2 ARGUMENTS BUT GOT 0", Call(_ada, "[greet()]"));
        Assert.Equal("Hi Bea", Call(_ada, "[greet(Bea)]"));

        Assert.Contains("Alias added.", _dispatcher.Dispatch(_god, "@function/alias add=PLUS", _db).Notifications);
        Assert.Equal("3", Call(_ada, "[plus(1,2)]"));

        Assert.Contains("You cannot alias @functions.", _dispatcher.Dispatch(_god, "@function/alias GREET=HI", _db).Notifications);
        Assert.Contains("There's already a function with that name.", _dispatcher.Dispatch(_god, "@function/alias add=GREET", _db).Notifications);

        Assert.Contains(_dispatcher.Dispatch(_god, "@function/restrict GREET=Wizard", _db).Notifications, note => note.Contains("Restrictions modified."));
        Assert.Equal("#-1 PERMISSION DENIED", Call(_ada, "[greet(Bea)]"));
        Assert.Equal("Hi Bea", Call(_god, "[greet(Bea)]"));

        Assert.Contains("Disabled.", _dispatcher.Dispatch(_god, "@function/disable GREET", _db).Notifications);
        Assert.Equal("#-1 FUNCTION DISABLED", Call(_god, "[greet(Bea)]"));
        Assert.Contains("You can't disable aliases.", _dispatcher.Dispatch(_god, "@function/disable PLUS", _db).Notifications);

        Assert.Contains("Enabled.", _dispatcher.Dispatch(_god, "@function/enable GREET", _db).Notifications);
        Assert.Equal("Hi Bea", Call(_god, "[greet(Bea)]"));

        Assert.Contains("Function deleted.", _dispatcher.Dispatch(_god, "@function/delete GREET", _db).Notifications);
        Assert.Contains("No such function.", _dispatcher.Dispatch(_god, "@function GREET", _db).Notifications);
        Assert.Contains("Function alias deleted.", _dispatcher.Dispatch(_god, "@function/delete PLUS", _db).Notifications);
        Assert.Equal("#-1 FUNCTION (PLUS) NOT FOUND", Call(_ada, "[plus(1,2)]"));
    }

    [Fact]
    public void Restrict_HonorsGodGagAndFixedAndLeavesOtherNamesOnTheReport()
    {
        _db.GetObject(_ada)!.SetAttribute("HELLO", "Hi", _ada);
        _dispatcher.Dispatch(_god, "@function GREET=Ada/HELLO", _db);
        _dispatcher.Dispatch(_god, "@function/restrict GREET=God NoGagged NoFixed LogArgs", _db);

        var report = _dispatcher.Dispatch(_god, "@function GREET", _db);
        Assert.Contains(report.Notifications, note => note.Contains("Flags     : NoGagged, NoFixed, God, LogArgs"));

        Assert.Equal("#-1 PERMISSION DENIED", Call(_ada, "[greet()]"));
        Assert.Equal("Hi", Call(_god, "[greet()]"));

        _dispatcher.Dispatch(_god, "@function/restrict GREET=!God NoGagged", _db);
        _db.GetObject(_ada)!.AddFlag("GAGGED");
        Assert.Equal("#-1 PERMISSION DENIED", Call(_ada, "[greet()]"));
        _db.GetObject(_ada)!.RemoveFlag("GAGGED");
        _db.GetObject(_ada)!.AddFlag("FIXED");
        Assert.Equal("#-1 PERMISSION DENIED", Call(_ada, "[greet()]"));
    }

    private string Call(Dbref who, string expression)
        => _evaluator.Evaluate(expression, new EvaluationContext(who, who, who, _limbo));
}
