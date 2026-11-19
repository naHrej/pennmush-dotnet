namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;
using Xunit;

public class VersionCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher = new();
    private readonly Dbref _ada;

    public VersionCommandTests()
    {
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        ada.Owner = ada.Id;
        _ada = ada.Id;
        _dispatcher.Register(new VersionCommandHandler(), "@version");
    }

    [Fact]
    public void Version_ReportsTheReleaseBakedFromGitHistory()
    {
        var result = _dispatcher.Dispatch(_ada, "@version", _db);

        Assert.Equal($"PennMUSH .NET {ServerVersion.Text}", result.Notifications[0]);
        Assert.Equal(0, ServerVersion.Major);
        Assert.Equal(1, ServerVersion.Minor);
        Assert.True(ServerVersion.Build > 0);
        string release = ServerVersion.Commit.Length == 0
            ? $"{ServerVersion.Major}.{ServerVersion.Minor}.{ServerVersion.Build}"
            : $"{ServerVersion.Major}.{ServerVersion.Minor}.{ServerVersion.Build}+g{ServerVersion.Commit}";
        Assert.Equal(ServerVersion.Text, release);
        Assert.Contains("Feature-parity reimplementation of PennMUSH 1.8+.", result.Notifications);
    }
}
