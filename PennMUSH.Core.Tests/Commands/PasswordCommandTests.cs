namespace PennMUSH.Core.Tests.Commands;

using System.IO;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Logging;
using PennMUSH.Core.Types;
using Xunit;

public class PasswordCommandTests : IDisposable
{
    private readonly string _directory;
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly WizardLog _log;
    private readonly Dbref _god;
    private readonly Dbref _ada;

    public PasswordCommandTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "penn-password-" + Guid.NewGuid().ToString("N"));
        _log = new WizardLog(_directory, () => new DateTime(2026, 11, 17, 11, 3, 0));
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        god.Owner = god.Id;
        god.AddFlag("WIZARD");
        _god = god.Id;
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        ada.Owner = ada.Id;
        PasswordHash.Store(ada, "secret");
        _ada = ada.Id;
        _dispatcher = new CommandDispatcher();
        _dispatcher.Register(new PasswordCommandHandler(), "@password");
        _dispatcher.Register(new NewPasswordCommandHandler(_log), "@newpassword");
        _dispatcher.AddRestriction("@password", "noguest", null);
        _dispatcher.AddRestriction("@newpassword", "wizard", null);
    }

    [Fact]
    public void APlayerChangesAMatchingPassword()
    {
        var wrong = _dispatcher.Dispatch(_ada, "@password nope=better", _db);
        Assert.Contains("The old password that you entered was incorrect.", wrong.Notifications);
        Assert.True(PasswordHash.Matches(_db.GetObject(_ada)!.GetAttribute(PasswordHash.AttributeName)!.Value, "secret"));

        var bad = _dispatcher.Dispatch(_ada, "@password secret=bad word", _db);
        Assert.Contains("Bad new password.", bad.Notifications);

        var changed = _dispatcher.Dispatch(_ada, "@password secret=better", _db);
        Assert.Contains("You have changed your password.", changed.Notifications);
        string saved = _db.GetObject(_ada)!.GetAttribute(PasswordHash.AttributeName)!.Value;
        Assert.StartsWith("2:sha512:", saved);
        Assert.True(PasswordHash.Matches(saved, "better"));
        Assert.False(PasswordHash.Matches(saved, "secret"));
    }

    [Fact]
    public void AMissingPasswordAcceptsAnyOldPassword()
    {
        var open = _db.CreateObject("Bea", ObjectType.Player, new Dbref(1));
        open.Owner = open.Id;
        var changed = _dispatcher.Dispatch(open.Id, "@password anything=fresh", _db);
        Assert.Contains("You have changed your password.", changed.Notifications);
        Assert.True(PasswordHash.Accepts(open, "fresh", memory: null));
    }

    [Fact]
    public void AWizardResetsAnotherPlayersPassword()
    {
        var denied = _dispatcher.Dispatch(_ada, "@newpassword One=nope", _db);
        Assert.Contains("Permission denied.", denied.Notifications);

        var missing = _dispatcher.Dispatch(_god, "@newpassword Nobody=fine", _db);
        Assert.Contains("No such player.", missing.Notifications);

        var changed = _dispatcher.Dispatch(_god, "@newpassword Ada=fresh", _db);
        Assert.Contains("Password for Ada changed.", changed.Notifications);
        Assert.Contains(changed.TargetedNotifications, note => note.Target == _ada && note.Message == "Your password has been changed by One.");
        Assert.True(PasswordHash.Accepts(_db.GetObject(_ada)!, "fresh", memory: null));
        Assert.Contains("*** NEWPASSWORD ***", File.ReadAllText(Path.Combine(_directory, "wizard.log")));
    }

    [Fact]
    public void GenerateRevealsTheNewPasswordAndGodIsProtected()
    {
        var blocked = _dispatcher.Dispatch(_ada, "@newpassword One=nope", _db);
        _db.GetObject(_ada)!.AddFlag("WIZARD");
        blocked = _dispatcher.Dispatch(_ada, "@newpassword One=nope", _db);
        Assert.Contains("You cannot change that player's password.", blocked.Notifications);

        var generated = _dispatcher.Dispatch(_god, "@newpassword/generate Ada", _db);
        string line = Assert.Single(generated.Notifications, note => note.StartsWith("Password for Ada changed to "));
        string password = line["Password for Ada changed to ".Length..].TrimEnd('.');
        Assert.InRange(password.Length, 7, 12);
        Assert.True(PasswordHash.Accepts(_db.GetObject(_ada)!, password, memory: null));
    }

    [Fact]
    public void AnEmptyWizardPasswordClearsTheCheck()
    {
        var cleared = _dispatcher.Dispatch(_god, "@newpassword Ada=", _db);
        Assert.Contains("Password for Ada changed.", cleared.Notifications);
        Assert.True(PasswordHash.Accepts(_db.GetObject(_ada)!, "", memory: null));
        Assert.False(PasswordHash.Accepts(_db.GetObject(_ada)!, "secret", memory: null));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
