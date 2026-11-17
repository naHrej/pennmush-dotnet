namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Communication.Mail;
using PennMUSH.Core.Database;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class MaliasCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly MailStore _mail = new();
    private readonly MailAliasTable _aliases = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _god;
    private readonly Dbref _ada;
    private readonly Dbref _bea;

    public MaliasCommandTests()
    {
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.Owner = god.Id;
        god.AddFlag("WIZARD");
        _god = god.Id;
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        ada.Owner = ada.Id;
        _ada = ada.Id;
        var bea = _db.CreateObject("Bea", ObjectType.Player, new Dbref(1));
        bea.Owner = bea.Id;
        _bea = bea.Id;
        var evaluator = new ExpressionEvaluator(database: _db);
        _dispatcher = new CommandDispatcher(evaluator);
        _dispatcher.Register(new MaliasCommandHandler(_aliases), "@malias");
        _dispatcher.Register(new MailCommandHandler(_mail, _aliases), "@mail");
    }

    [Fact]
    public void CreateListAndSendToTheAlias()
    {
        var bad = _dispatcher.Dispatch(_ada, "@malias crew=Ada", _db);
        Assert.Contains("MAIL: All Mail aliases must begin with '+'.", bad.Notifications);

        var created = _dispatcher.Dispatch(_ada, "@malias +crew=me Bea", _db);
        Assert.Contains($"MAIL: {MailAliasTable.Unparse(_db.GetObject(_ada)!)} added to alias +crew", created.Notifications);
        Assert.Contains($"MAIL: {MailAliasTable.Unparse(_db.GetObject(_bea)!)} added to alias +crew", created.Notifications);
        Assert.Contains("MAIL: Alias set '+crew' defined.", created.Notifications);

        var again = _dispatcher.Dispatch(_ada, "@malias +crew=Ada", _db);
        Assert.Contains("MAIL: Mail Alias '+crew' already exists.", again.Notifications);

        var listed = _dispatcher.Dispatch(_ada, "@malias", _db);
        string row = "+" + "crew".PadRight(12) + " " + "crew".PadRight(35) + " " + "M-  -O " + " " + "Ada".PadRight(15);
        Assert.Contains(row, listed.Notifications);
        Assert.Contains("*****  End of Mail Aliases *****", listed.Notifications);

        var members = _dispatcher.Dispatch(_ada, "@malias +crew", _db);
        Assert.Contains("MAIL: Alias +crew: Ada, Bea", members.Notifications);

        var sent = _dispatcher.Dispatch(_ada, "@mail +crew=Hello/There", _db);
        Assert.Contains("Mail sent to Ada (Subject: Hello).", sent.Notifications);
        Assert.Contains("Mail sent to Bea (Subject: Hello).", sent.Notifications);
        Assert.Contains(_mail.GetMessages(_bea), message => message.Subject == "Hello" && message.Body == "There");
    }

    [Fact]
    public void OwnerEditsAndGodNukes()
    {
        _dispatcher.Dispatch(_ada, "@malias +crew=Ada Bea", _db);

        var denied = _dispatcher.Dispatch(_bea, "@malias/destroy +crew", _db);
        Assert.Contains("MAIL: Permission denied!", denied.Notifications);
        Assert.Equal(1, _aliases.Count);

        var described = _dispatcher.Dispatch(_ada, "@malias/describe +crew=Bridge crew", _db);
        Assert.Contains("MAIL: Description changed.", described.Notifications);

        var stats = _dispatcher.Dispatch(_god, "@malias/stats", _db);
        Assert.Contains("MAIL: Number of mail aliases defined: 1", stats.Notifications);
        Assert.Contains("MAIL: Allocated slots 3", stats.Notifications);

        var mortal = _dispatcher.Dispatch(_ada, "@malias/nuke", _db);
        Assert.Contains("MAIL: Only god can do that!", mortal.Notifications);

        var nuked = _dispatcher.Dispatch(_god, "@malias/nuke", _db);
        Assert.Contains("MAIL: All mail aliases destroyed!", nuked.Notifications);
        Assert.Equal(0, _aliases.Count);
    }
}
