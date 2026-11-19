namespace PennMUSH.Core.Tests.Commands;

using System;
using System.Collections.Generic;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.MUSHcode.Functions;
using PennMUSH.Core.Types;
using Xunit;

[CollectionDefinition("FeedbackTemplates", DisableParallelization = true)]
public sealed class FeedbackTemplatesCollection { }

[Collection("FeedbackTemplates")]
public class Backend8WordingCatalogTests
{
    [Fact]
    public void GetTemplate_AndGetAllTemplates_ReflectRuntimeOverrides()
    {
        try
        {
            var all = FeedbackText.GetAllTemplates();
            Assert.Equal(Enum.GetValues<Feedback>().Length, all.Count);
            Assert.Equal("Command disabled.", FeedbackText.GetTemplate(Feedback.CommandDisabled));
            Assert.Equal("What do you want to @decompile?", FeedbackText.GetTemplate(Feedback.WhatDoYouWantToDecompile));

            FeedbackText.SetTemplate(Feedback.CommandDisabled, "Sorry, that command is disabled.");
            Assert.Equal("Sorry, that command is disabled.", FeedbackText.GetTemplate(Feedback.CommandDisabled));
            Assert.Equal("Sorry, that command is disabled.", FeedbackText.GetAllTemplates()[Feedback.CommandDisabled]);
        }
        finally
        {
            FeedbackText.ResetTemplates();
        }

        Assert.Equal("Command disabled.", FeedbackText.GetTemplate(Feedback.CommandDisabled));
    }

    [Fact]
    public void CommandHandlers_RespectRuntimeFeedbackOverrides()
    {
        var db = new ObjectDatabase();
        var room = db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);
        var wiz = db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);
        wiz.Owner = wiz.Id;
        wiz.AddFlag("WIZARD");
        db.MoveObject(wiz.Id, room.Id);

        var alice = db.CreateObject("Alice", ObjectType.Player, wiz.Id);
        alice.Owner = alice.Id;
        db.MoveObject(alice.Id, room.Id);

        var lamp = db.CreateObject("Lamp", ObjectType.Thing, wiz.Id);
        var zone = db.CreateObject("ZoneObj", ObjectType.Thing, wiz.Id);

        var dispatcher = new CommandDispatcher();
        dispatcher.Register(new DecompileCommandHandler(), "@decompile");
        dispatcher.Register(new MonikerCommandHandler(), "@moniker");
        dispatcher.Register(new ChzoneCommandHandler(), "@chzone");
        dispatcher.Register(new PoorCommandHandler(), "@poor");
        dispatcher.AddRestriction("@poor", "nobody");

        try
        {
            FeedbackText.SetTemplate(Feedback.CommandDisabled, "OVERRIDE: disabled");
            FeedbackText.SetTemplate(Feedback.WhatDoYouWantToDecompile, "OVERRIDE: decompile what?");
            FeedbackText.SetTemplate(Feedback.NoMonikerSet, "OVERRIDE: no moniker");
            FeedbackText.SetTemplate(Feedback.YouDontControlThat, "OVERRIDE: not yours");

            Assert.Equal("OVERRIDE: disabled", dispatcher.Dispatch(alice.Id, "@poor 0", db).Notifications[0]);
            Assert.Equal("OVERRIDE: decompile what?", dispatcher.Dispatch(alice.Id, "@decompile", db).Notifications[0]);
            Assert.Equal("OVERRIDE: no moniker", dispatcher.Dispatch(wiz.Id, $"@moniker #{alice.Id.Value}", db).Notifications[0]);
            Assert.Equal("OVERRIDE: not yours", dispatcher.Dispatch(alice.Id, $"@chzone #{lamp.Id.Value}=#{zone.Id.Value}", db).Notifications[0]);
        }
        finally
        {
            FeedbackText.ResetTemplates();
        }
    }

    [Fact]
    public void Functions_UseFeedbackCatalog_AndParameterizedMushErrors()
    {
        var db = new ObjectDatabase();
        var room = db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);
        var wiz = db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);
        wiz.Owner = wiz.Id;
        wiz.AddFlag("WIZARD");
        db.MoveObject(wiz.Id, room.Id);

        var lamp = db.CreateObject("Lamp", ObjectType.Thing, wiz.Id);
        lamp.SetAttribute("DESC", "glow", wiz.Id);

        var notes = new List<Notification>();
        var registry = new FunctionRegistry(db)
        {
            NotificationDispatcher = notes.Add
        };
        var evaluator = new ExpressionEvaluator(registry.Dispatch, db);
        registry.Evaluator = evaluator;
        var ctx = new EvaluationContext(wiz.Id, wiz.Id, wiz.Id, room.Id);

        // Parameterized MushError formatting
        Assert.Equal("#-1 SQL ERROR: bad sql", MushError.SqlError.Text("error", "bad sql"));
        Assert.Equal("#-1 REGEXP ERROR: bad rx", MushError.RegexpError.Text("error", "bad rx"));
        Assert.Equal("#-1 FUNCTION (SETQ) EXPECTS AN EVEN NUMBER OF ARGUMENTS", MushError.FunctionExpectsEvenArguments.Text("function", "SETQ"));
        Assert.Equal("#-1 FUNCTION (LETQ) EXPECTS AN ODD NUMBER OF ARGUMENTS", MushError.FunctionExpectsOddArguments.Text("function", "LETQ"));

        // Function overrides
        try
        {
            FeedbackText.SetTemplate(Feedback.ClientCodeEditorOpened, "ED={{value}}");
            FeedbackText.SetTemplate(Feedback.BenchmarkReport, "BENCH={{avg}}/{{min}}/{{max}}");
            FeedbackText.SetTemplate(Feedback.SpeakDefaultSayVerb, "declares,");
            FeedbackText.SetTemplate(Feedback.SpeakSays, "{{speaker}} {{verb}} {{text}}");

            Assert.Equal("1", evaluator.Evaluate($"[cedit({wiz.Id},{lamp.Id},DESC)]", ctx));
            Assert.Contains(notes, n => n.Message == "ED=Editing Lamp/DESC");

            string bench = evaluator.Evaluate("[benchmark(add(1,2),3)]", ctx);
            Assert.StartsWith("BENCH=", bench);

            string spoken = evaluator.Evaluate($"[speak({wiz.Id},hello)]", ctx);
            Assert.Equal("Wizard declares, hello", spoken);
        }
        finally
        {
            FeedbackText.ResetTemplates();
        }
    }
}
