namespace PennMUSH.Core.Tests.Flags;

using System.IO;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Configuration;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Flags;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class FlagRenameTests
{
    [Fact]
    public void FlagRename_UpdatesCanonicalName_RejectsOldNameInSetAndHasFlag_AndPreservesInternals()
    {
        var db = new ObjectDatabase();
        var god = WorldBootstrapper.GenerateBaseWorld(db);
        var alice = db.CreateObject("Alice", ObjectType.Player, god.Id);

        var functions = new FunctionRegistry(db);
        var evaluator = new ExpressionEvaluator(functions.Dispatch, db);
        var dispatcher = new CommandDispatcher(evaluator);
        dispatcher.Register(new FlagCommandHandler(), "@flag");
        dispatcher.Register(new SetCommandHandler(), "@set");
        dispatcher.Register(new ExamineCommandHandler(), "examine", "ex");

        var myopicDef = FlagTable.FindFlag("MYOPIC")!;
        try
        {
            // Rename MYOPIC -> SHORTSIGHTED via @flag/rename
            var ren = dispatcher.Dispatch(god.Id, "@flag/rename MYOPIC=SHORTSIGHTED", db);
            Assert.Contains("Flag 'MYOPIC' renamed to 'SHORTSIGHTED'.", ren.Notifications);

            // Old name is no longer recognized by @set or hasflag()
            var oldSet = dispatcher.Dispatch(god.Id, $"@set #{alice.Id.Value}=MYOPIC", db);
            Assert.Contains("I don't recognize that flag.", oldSet.Notifications);
            Assert.False(alice.HasFlag("MYOPIC"));

            var godCtx = new EvaluationContext(god.Id, god.Id, god.Id, god.Location);
            Assert.Equal("0", evaluator.Evaluate($"[hasflag({alice.Id},MYOPIC)]", godCtx));

            // New name works for @set, hasflag(), lflags(), and examine, and internal HasFlag keeps working
            var newSet = dispatcher.Dispatch(god.Id, $"@set #{alice.Id.Value}=SHORTSIGHTED", db);
            Assert.Contains("Set.", newSet.Notifications);
            Assert.True(alice.HasFlag("MYOPIC")); // internal C# check still works
            Assert.Equal("1", evaluator.Evaluate($"[hasflag({alice.Id},SHORTSIGHTED)]", godCtx));
            Assert.Contains("SHORTSIGHTED", evaluator.Evaluate($"[lflags({alice.Id})]", godCtx));
            Assert.DoesNotContain("MYOPIC", evaluator.Evaluate($"[lflags({alice.Id})]", godCtx));

            var ex = dispatcher.Dispatch(god.Id, $"examine #{alice.Id.Value}", db);
            Assert.Contains(ex.Notifications, n => n.Contains("SHORTSIGHTED") && !n.Contains("MYOPIC"));

            // Underlying _flags data structure still stores InternalName ("MYOPIC")
            Assert.Contains("MYOPIC", alice.Flags);
        }
        finally
        {
            FlagTable.RenameFlag(myopicDef, "MYOPIC");
        }
    }

    [Fact]
    public void FlagRename_ParsesFromMushCnf()
    {
        string tmp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmp, "flag_rename ROYALTY NOBLE\n");
            var cfg = MushConfigParser.ParseFile(tmp);
            Assert.Equal("NOBLE", cfg.FlagRenames["ROYALTY"]);
        }
        finally
        {
            File.Delete(tmp);
        }
    }
}
