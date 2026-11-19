namespace PennMUSH.Core.Tests.Help;

using System;
using System.IO;
using System.Linq;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Configuration;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Help;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using PennMUSH.Server.Engine;
using Xunit;

public class Backend5TextCacheTests
{
    [Fact]
    public void TextFileCache_LoadsMultiCategoryFilesAndEnforcesAdminIsolation()
    {
        string gameDir = CreateTempGameDir();
        try
        {
            Directory.CreateDirectory(Path.Combine(gameDir, "txt", "hlp"));
            Directory.CreateDirectory(Path.Combine(gameDir, "txt", "nws"));
            File.WriteAllText(Path.Combine(gameDir, "mush.cnf"), """
                help_command help txt/help.txt
                help_command news txt/news.txt
                ahelp_command ahelp txt/help.txt
                ahelp_command anews txt/news.txt
                ahelp_command staff txt/staff.txt
                dict_file txt/words.txt
                """);

            File.WriteAllText(Path.Combine(gameDir, "txt", "hlp", "topics.hlp"), """
                & help
                Public help index.
                Line two about widgets.
                & &HELP
                Secret admin help index.
                & widget
                Public widget entry.
                & &SECRET_CMD
                Wizard-only command reference.
                """);

            File.WriteAllText(Path.Combine(gameDir, "txt", "nws", "pennmush.nws"), """
                & changes
                Release notes.
                """);

            File.WriteAllText(Path.Combine(gameDir, "txt", "events.txt"), """
                & Gala
                Annual celebration tonight.
                """);

            File.WriteAllText(Path.Combine(gameDir, "txt", "rules.txt"), """
                & Conduct
                Be respectful.
                """);

            File.WriteAllText(Path.Combine(gameDir, "txt", "staff.txt"), """
                & Policy
                Internal staff guidelines.
                """);

            File.WriteAllText(Path.Combine(gameDir, "txt", "motd.txt"), "Welcome to TestMUSH!\n");
            File.WriteAllText(Path.Combine(gameDir, "txt", "wizmotd.txt"), "Wizard banner.\n");
            File.WriteAllText(Path.Combine(gameDir, "txt", "words.txt"), "aardvark\nalbatross\n");

            var config = MushConfigParser.ParseFile(Path.Combine(gameDir, "mush.cnf"));
            Assert.Equal("txt/words.txt", config.DictFile);
            Assert.Contains(config.HelpFiles, h => h.CommandName == "staff" && h.AdminOnly);

            var cache = new TextFileCache();
            cache.ConfigureFromGameDirectory(gameDir, config);

            var helpStore = new HelpStore();
            helpStore.LoadFromDirectory(Path.Combine(gameDir, "txt", "hlp"));
            Assert.Null(helpStore.Lookup("&HELP"));
            Assert.Null(helpStore.Lookup("SECRET_CMD"));
            Assert.Contains("Public help index.", helpStore.Lookup("help"));

            var db = new ObjectDatabase();
            db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));                  // #0
            var god = db.CreateObject("One", ObjectType.Player, new Dbref(1));       // #1
            god.Owner = god.Id;
            god.AddFlag("WIZARD");
            var roy = db.CreateObject("Roy", ObjectType.Player, god.Id);             // #2
            roy.Owner = roy.Id;
            roy.AddFlag("ROYALTY");
            var mortal = db.CreateObject("Mortal", ObjectType.Player, god.Id);       // #3
            mortal.Owner = mortal.Id;

            var reg = new FunctionRegistry(db, cache);
            var eval = new ExpressionEvaluator(reg.Dispatch, db);
            reg.Evaluator = eval;

            var godCtx = new EvaluationContext(god.Id, god.Id, god.Id, new Dbref(0));
            var royCtx = new EvaluationContext(roy.Id, roy.Id, roy.Id, new Dbref(0));
            var mortalCtx = new EvaluationContext(mortal.Id, mortal.Id, mortal.Id, new Dbref(0));

            // Public help excludes & & topics
            Assert.Equal("help|widget", eval.Evaluate("[textentries(help,*,|)]", mortalCtx));
            Assert.Contains("Public help index.", eval.Evaluate("[textfile(help,help)]", mortalCtx));
            Assert.Equal("#-1 NO ENTRY", eval.Evaluate("[textfile(help,secret_cmd)]", godCtx));
            Assert.Equal("#-1 NO SUCH FILE", eval.Evaluate("[textfile(bogus,help)]", mortalCtx));
            Assert.Equal("#-1 NO SUCH FILE", eval.Evaluate("[textentries(bogus,*)]", mortalCtx));
            Assert.Equal("#-1 NO SUCH FILE", eval.Evaluate("[textsearch(bogus,test)]", mortalCtx));

            // Multi-line wildcard and substring textsearch() across newlines
            Assert.Equal("help", eval.Evaluate("[textsearch(help,*Public help*widgets.*)]", mortalCtx));
            Assert.Equal("help", eval.Evaluate("[textsearch(help,widgets)]", mortalCtx));

            // News, events, rules, motd
            Assert.Equal("changes", eval.Evaluate("[textentries(news,*)]", mortalCtx));
            Assert.Equal("Release notes.", eval.Evaluate("[textfile(news,changes)]", mortalCtx));
            Assert.Equal("Annual celebration tonight.", eval.Evaluate("[textfile(events,gala)]", mortalCtx));
            Assert.Equal("Be respectful.", eval.Evaluate("[textfile(rules,conduct)]", mortalCtx));
            Assert.Equal("Welcome to TestMUSH!", eval.Evaluate("[textfile(motd,motd)]", mortalCtx));

            // Admin-only & & topics and dedicated admin files enforce PermissionDenied for mortals
            Assert.Equal("#-1 PERMISSION DENIED", eval.Evaluate("[textfile(ahelp,help)]", mortalCtx));
            Assert.Equal("#-1 PERMISSION DENIED", eval.Evaluate("[textentries(ahelp,*)]", mortalCtx));
            Assert.Equal("#-1 PERMISSION DENIED", eval.Evaluate("[textsearch(ahelp,secret)]", mortalCtx));
            Assert.Equal("#-1 PERMISSION DENIED", eval.Evaluate("[textfile(anews,changes)]", mortalCtx));
            Assert.Equal("#-1 PERMISSION DENIED", eval.Evaluate("[textfile(staff,policy)]", mortalCtx));
            Assert.Equal("#-1 PERMISSION DENIED", eval.Evaluate("[textfile(wizmotd,wizmotd)]", mortalCtx));

            // Wizards and Royalty can read admin categories
            Assert.Equal("HELP|SECRET_CMD", eval.Evaluate("[textentries(ahelp,*,|)]", godCtx));
            Assert.Equal("Secret admin help index.", eval.Evaluate("[textfile(ahelp,help)]", godCtx));
            Assert.Equal("Wizard-only command reference.", eval.Evaluate("[textfile(ahelp,secret_cmd)]", royCtx));
            Assert.Equal("SECRET_CMD", eval.Evaluate("[textsearch(ahelp,Wizard-only)]", godCtx));
            Assert.Equal(string.Empty, eval.Evaluate("[textentries(anews,*)]", godCtx));
            Assert.Equal("Internal staff guidelines.", eval.Evaluate("[textfile(staff,policy)]", royCtx));
            Assert.Equal("Wizard banner.", eval.Evaluate("[textfile(wizmotd,wizmotd)]", godCtx));
        }
        finally
        {
            Directory.Delete(gameDir, recursive: true);
        }
    }

    [Fact]
    public void ReadCache_ReloadsTextCacheNewOptionalFilesAndSuggestionsWhilePreservingManualWords()
    {
        string gameDir = CreateTempGameDir();
        try
        {
            string helpDir = Path.Combine(gameDir, "txt", "hlp");
            Directory.CreateDirectory(helpDir);
            File.WriteAllText(Path.Combine(helpDir, "core.hlp"), """
                & alpha
                Initial alpha body.
                """);
            File.WriteAllText(Path.Combine(gameDir, "txt", "motd.txt"), "Old MOTD.\n");

            var config = new MushConfiguration();
            var helpStore = new HelpStore();
            helpStore.LoadFromDirectory(helpDir);

            var cache = new TextFileCache();
            cache.ConfigureFromGameDirectory(gameDir, config);

            var suggestions = new SuggestionVocabulary();
            void Sync()
            {
                foreach (string cat in cache.Categories)
                {
                    if (!cache.IsAdminOnly(cat))
                        suggestions.ReplaceCachedCategory(cat, cache.GetEntries(cat, isAdmin: false));
                }
            }
            Sync();

            var db = new ObjectDatabase();
            db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
            var god = db.CreateObject("One", ObjectType.Player, new Dbref(1));
            god.Owner = god.Id;
            var mortal = db.CreateObject("Mortal", ObjectType.Player, god.Id);
            mortal.Owner = mortal.Id;

            var reg = new FunctionRegistry(db, cache) { Suggestions = suggestions };
            var eval = new ExpressionEvaluator(reg.Dispatch, db);
            reg.Evaluator = eval;

            var dispatcher = new CommandDispatcher(eval);
            dispatcher.Register(new ReadCacheCommandHandler(helpStore, () => ScreenFileReport.Lines(gameDir, config), cache, Sync), "@readcache");
            dispatcher.Register(new SuggestCommandHandler(suggestions), "@suggest");
            dispatcher.Register(new HelpCommandHandler(helpStore, null, suggestions), "help");

            var mortalCtx = new EvaluationContext(mortal.Id, mortal.Id, mortal.Id, new Dbref(0));

            // Add manual suggestion word
            dispatcher.Dispatch(god.Id, "@suggest/add custom=zephyr", db);
            Assert.Equal("#-1 NO SUCH FILE", eval.Evaluate("[textentries(events,*)]", mortalCtx));
            Assert.Equal("#-1 NO SUCH FILE", eval.Evaluate("[textentries(rules,*)]", mortalCtx));

            // Modify help, update motd, and create events.txt + rules.txt after startup
            File.WriteAllText(Path.Combine(helpDir, "core.hlp"), """
                & alpha
                Updated alpha body.
                & nebula
                Deep space cloud.
                """);
            File.WriteAllText(Path.Combine(gameDir, "txt", "motd.txt"), "Fresh MOTD!\n");
            File.WriteAllText(Path.Combine(gameDir, "txt", "events.txt"), "& Summit\nWizard summit.\n");
            File.WriteAllText(Path.Combine(gameDir, "txt", "rules.txt"), "& FairPlay\nNo cheating.\n");

            var readcacheResult = dispatcher.Dispatch(god.Id, "@readcache", db);
            Assert.Contains("Help files reindexed.", readcacheResult.Notifications);

            Assert.Equal("Updated alpha body.", eval.Evaluate("[textfile(help,alpha)]", mortalCtx));
            Assert.Equal("alpha|nebula", eval.Evaluate("[textentries(help,*,|)]", mortalCtx));
            Assert.Equal("nebula", eval.Evaluate("[textsearch(help,*space cloud*)]", mortalCtx));
            Assert.Equal("Fresh MOTD!", eval.Evaluate("[textfile(motd,motd)]", mortalCtx));
            Assert.Equal("Wizard summit.", eval.Evaluate("[textfile(events,summit)]", mortalCtx));
            Assert.Equal("No cheating.", eval.Evaluate("[textfile(rules,fairplay)]", mortalCtx));
            Assert.Equal("Updated alpha body.", dispatcher.Dispatch(mortal.Id, "help alpha", db).Notifications.Single());

            // Suggest picks up newly cached topics and keeps manual @suggest/add entries
            Assert.Contains("nebula", eval.Evaluate("[suggest(help,nebulaa)]", mortalCtx).Split(' '), StringComparer.OrdinalIgnoreCase);
            Assert.Equal("zephyr", eval.Evaluate("[suggest(custom,zephyrr)]", mortalCtx));
        }
        finally
        {
            Directory.Delete(gameDir, recursive: true);
        }
    }

    [Fact]
    public void SuggestFunc_SearchesBuiltinsCommandsDictAndRespectsAdminIsolation()
    {
        string gameDir = CreateTempGameDir();
        try
        {
            Directory.CreateDirectory(Path.Combine(gameDir, "txt", "hlp"));
            File.WriteAllText(Path.Combine(gameDir, "txt", "hlp", "help.hlp"), """
                & teleport
                Teleport help.
                & &HIDDEN_ADMIN_TOPIC
                Secret topic.
                """);
            File.WriteAllText(Path.Combine(gameDir, "txt", "words.txt"), "aardvark\naardwolf\n");

            var config = new MushConfiguration();
            var cache = new TextFileCache();
            cache.ConfigureFromGameDirectory(gameDir, config);

            var suggestions = new SuggestionVocabulary();
            suggestions.LoadCachedWordFile("WORDS", Path.Combine(gameDir, "txt", "words.txt"));
            suggestions.ReplaceCachedCategory("help", cache.GetEntries("help", isAdmin: false));
            suggestions.Add("custom", "starship");

            var db = new ObjectDatabase();
            db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
            var god = db.CreateObject("One", ObjectType.Player, new Dbref(1));
            god.Owner = god.Id;
            var mortal = db.CreateObject("Mortal", ObjectType.Player, god.Id);
            mortal.Owner = mortal.Id;

            var dispatcher = new CommandDispatcher();
            dispatcher.Register(new SuggestCommandHandler(suggestions), "@suggest");
            dispatcher.Register(new ReadCacheCommandHandler(new HelpStore(), () => Array.Empty<string>(), cache), "@readcache");

            var reg = new FunctionRegistry(db, cache)
            {
                Suggestions = suggestions,
                CommandNamesProvider = () => dispatcher.Table.PrimaryNames
            };
            var eval = new ExpressionEvaluator(reg.Dispatch, db);
            reg.Evaluator = eval;

            var mortalCtx = new EvaluationContext(mortal.Id, mortal.Id, mortal.Id, new Dbref(0));
            var godCtx = new EvaluationContext(god.Id, god.Id, god.Id, new Dbref(0));

            Assert.Contains("LWHO", eval.Evaluate("[suggest(functions,lwh)]", mortalCtx));
            Assert.Contains("@readcache", eval.Evaluate("[suggest(commands,@readcach)]", mortalCtx));
            Assert.Contains("DESCRIBE", eval.Evaluate("[suggest(attributes,describ)]", mortalCtx));
            Assert.Contains("WIZARD", eval.Evaluate("[suggest(flags,wizar)]", mortalCtx));
            Assert.Contains("Builder", eval.Evaluate("[suggest(powers,builde)]", mortalCtx));
            Assert.Equal("starship", eval.Evaluate("[suggest(custom,starshp)]", mortalCtx));
            Assert.Contains("aardvark", eval.Evaluate("[suggest(words,ardvark)]", mortalCtx).Split(' '), StringComparer.OrdinalIgnoreCase);
            Assert.Equal("teleport", eval.Evaluate("[suggest(help,teleportt)]", mortalCtx));

            // Admin-only ahelp topics are hidden from mortals but available to wizards/royalty
            Assert.Equal(string.Empty, eval.Evaluate("[suggest(ahelp,hidden_admin_topi)]", mortalCtx));
            Assert.Equal("HIDDEN_ADMIN_TOPIC", eval.Evaluate("[suggest(ahelp,hidden_admin_topi)]", godCtx));
        }
        finally
        {
            Directory.Delete(gameDir, recursive: true);
        }
    }

    [Fact]
    public void MushServer_WiresTextCacheSuggestionsAndCommandsByDefault()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"backend5_server_{Guid.NewGuid():N}.db");
        try
        {
            var server = new MUSHServer(dbPath);
            var mortal = server.Database.CreateObject("Mortal", ObjectType.Player, new Dbref(1));
            mortal.Owner = mortal.Id;
            var room = server.Database.GetObject(new Dbref(0))!;
            server.Database.MoveObject(mortal.Id, room.Id);

            var godCtx = new EvaluationContext(new Dbref(1), new Dbref(1), new Dbref(1), room.Id);
            var mortalCtx = new EvaluationContext(mortal.Id, mortal.Id, mortal.Id, room.Id);

            try
            {
                string lnHelp = server.Evaluator.Evaluate(@"[textfile(help, ln\(\))]", mortalCtx);
                Assert.Contains("natural log", lnHelp, StringComparison.OrdinalIgnoreCase);

                string whoEntries = server.Evaluator.Evaluate("[textentries(help, ?who(), |)]", mortalCtx);
                Assert.Contains("CWHO()", whoEntries.Split('|'));
                Assert.Contains("LWHO()", whoEntries.Split('|'));

                string logSearch = server.Evaluator.Evaluate("[textsearch(help, *natural log*)]", mortalCtx);
                Assert.Contains("LN()", logSearch.Split(' '));

                string newsEntries = server.Evaluator.Evaluate("[textentries(news, *)]", mortalCtx);
                Assert.Contains("code", newsEntries.Split(' '));
                Assert.Contains("changes", newsEntries.Split(' '));

                string motd = server.Evaluator.Evaluate("[textfile(motd, motd)]", mortalCtx);
                Assert.Contains("Welcome to PennMUSH!", motd);

                Assert.Equal("#-1 PERMISSION DENIED", server.Evaluator.Evaluate("[textfile(ahelp, help)]", mortalCtx));
                Assert.Equal("This is the AHELP index.", server.Evaluator.Evaluate("[textfile(ahelp, help)]", godCtx));

                Assert.Equal("#-1 PERMISSION DENIED", server.Evaluator.Evaluate("[textfile(wizmotd, wizmotd)]", mortalCtx));
                Assert.Contains("friendly neighborhood god", server.Evaluator.Evaluate("[textfile(wizmotd, wizmotd)]", godCtx));

                Assert.Contains("@readcache", server.Evaluator.Evaluate("[suggest(commands, @readcach)]", mortalCtx).Split(' '));
            }
            finally
            {
                server.Stop();
            }
        }
        finally
        {
            foreach (string suffix in new[] { "", "-shm", "-wal" })
            {
                string candidate = dbPath + suffix;
                if (File.Exists(candidate))
                {
                    try { File.Delete(candidate); } catch { }
                }
            }
        }
    }

    private static string CreateTempGameDir()
        => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "penn-backend5-" + Guid.NewGuid().ToString("N"))).FullName;
}
