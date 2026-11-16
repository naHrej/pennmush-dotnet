namespace PennMUSH.Core.Tests.Commands;

using System.IO;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Configuration;
using PennMUSH.Core.Database;
using PennMUSH.Core.Help;
using PennMUSH.Core.Types;
using Xunit;

public class SuggestAndReadCacheTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher = new();
    private readonly SuggestionVocabulary _vocabulary = new();
    private readonly HelpStore _help = new();
    private readonly string _game;
    private readonly Dbref _god;
    private readonly Dbref _mortal;

    public SuggestAndReadCacheTests()
    {
        _game = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "penn-readcache-" + Path.GetRandomFileName())).FullName;
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        var god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));
        var mortal = _db.CreateObject("Mortal", ObjectType.Player, new Dbref(1));
        mortal.Owner = mortal.Id;
        _god = god.Id;
        _mortal = mortal.Id;
        _dispatcher.Register(new SuggestCommandHandler(_vocabulary), "@suggest");
        _dispatcher.Register(new ReadCacheCommandHandler(_help, () => ScreenFileReport.Lines(_game, new MushConfiguration())), "@readcache");
        _dispatcher.Register(new HelpCommandHandler(_help, null, _vocabulary), "help");
    }

    [Fact]
    public void Suggest_AddsListsAndDeletesForAWizard()
    {
        var refused = Run(_mortal, "@suggest/add HELP=teleport");
        Assert.Equal("Your suggestion is not welcome.", Single(refused));

        var missing = Run(_god, "@suggest/add HELP");
        Assert.Equal("What did you want to add?", Single(missing));

        var added = Run(_god, "@suggest/add help=Teleport");
        Assert.Equal("Suggestion vocabulary word added.", Single(added));
        var again = Run(_god, "@suggest/add HELP=teleport");
        Assert.Equal("Unable to add word.", Single(again));

        var listed = Run(_mortal, "@suggest");
        Assert.Equal(new[] { "Vocabulary suggestion categories:", "\tHELP" }, listed.Notifications);

        var deleted = Run(_god, "@suggest/delete HELP=missing");
        Assert.Equal("Suggestion vocabulary word deleted.", Single(deleted));
        Assert.Equal("Permission denied.", Single(Run(_mortal, "@suggest/delete HELP=teleport")));
        Assert.Equal("What did you want to delete?", Single(Run(_god, "@suggest/delete HELP")));
        Assert.Equal("Suggestion vocabulary word deleted.", Single(Run(_god, "@suggest/delete HELP=teleport")));
        Assert.Equal(new[] { "Vocabulary suggestion categories:", "\tHELP" }, Run(_god, "@suggest/list").Notifications);
    }

    [Fact]
    public void Help_OffersACloseVocabularyWord()
    {
        Run(_god, "@suggest/add HELP=teleport");
        var guessed = Run(_mortal, "help teleportt");
        Assert.Equal("No help entry for 'teleportt'. Did you mean 'teleport'?", Single(guessed));

        var plain = Run(_mortal, "help nowhere");
        Assert.Equal("No entry for 'nowhere'.", Single(plain));
    }

    [Fact]
    public void ReadCache_ReportsSizesAndReloadsHelp()
    {
        Assert.Equal("Permission denied.", Single(Run(_mortal, "@readcache")));

        Directory.CreateDirectory(Path.Combine(_game, "txt"));
        File.WriteAllText(Path.Combine(_game, "txt", "connect.txt"), "hello");
        string helpDir = Path.Combine(_game, "txt", "hlp");
        Directory.CreateDirectory(helpDir);
        File.WriteAllText(Path.Combine(helpDir, "one.hlp"), "& teleport\nA jump.\n");
        _help.LoadFromDirectory(helpDir);

        var first = Run(_god, "@readcache");
        Assert.Contains("File sizes:  Index...-1  NewUser...-1  Connect...5  Guest...-1  Motd...-1  Wizmotd...-1  Quit...-1  Register...-1  Down...-1  Full...-1  Who...-1", first.Notifications);
        Assert.Contains("HTMLFile sizes:  Index...-1  NewUser...-1  Connect...-1  Guest...-1  Motd...-1  Wizmotd...-1  Quit...-1  Register...-1  Down...-1  Full...-1  Who...-1", first.Notifications);
        Assert.Equal("Help files reindexed.", first.Notifications[^1]);
        Assert.Contains("A jump.", Run(_mortal, "help teleport").Notifications.Single());

        File.WriteAllText(Path.Combine(helpDir, "one.hlp"), "& teleport\nA longer jump.\n");
        Run(_god, "@readcache");
        Assert.Contains("A longer jump.", Run(_mortal, "help teleport").Notifications.Single());
    }

    private CommandContext Run(Dbref who, string command) => _dispatcher.Dispatch(who, command, _db);

    private static string Single(CommandContext ctx) => Assert.Single(ctx.Notifications);
}
