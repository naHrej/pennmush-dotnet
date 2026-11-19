namespace PennMUSH.Core.Tests.MUSHcode;

using System;
using System.IO;
using System.Linq;
using Xunit;
using PennMUSH.Core.Access;
using PennMUSH.Core.Communication.Channels;
using PennMUSH.Core.Communication.Mail;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Locks;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;

public class Backend3RuntimePersistenceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _connStr;

    public Backend3RuntimePersistenceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"pennmush_rt_{Guid.NewGuid():N}.db");
        _connStr = $"Data Source={_dbPath}";
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact]
    public void RuntimeStateStore_RoundTripsChannelsMailAliasesSoftFunctionsAndSiteLocks()
    {
        var store = new RuntimeStateStore(_connStr);

        var channels1 = new ChannelRegistry();
        channels1.CreateChannel("Staff", new Dbref(1), ChannelFlags.Admin | ChannelFlags.Quiet, "Staff chat");
        var pub = channels1.GetChannel("Staff")!;
        pub.BufferCapacity = 25;
        pub.AddMember(new Dbref(1), "Admin");
        pub.GetMember(new Dbref(1))!.IsHidden = true;
        pub.SetLock("join", "#1", LockParser.Parse("#1")!);

        var mail1 = new MailStore();
        mail1.SendMail(new Dbref(1), new Dbref(2), "Welcome", "Hello world", urgent: true);
        mail1.SetCurrentFolder(new Dbref(2), 3);

        var aliases1 = new MailAliasTable();
        var alias = new MailAlias { Name = "devs", Owner = new Dbref(1), Description = "Developers" };
        alias.Members.Add(new Dbref(1));
        alias.Members.Add(new Dbref(2));
        aliases1.ImportAlias(alias);

        var funcs1 = new FunctionRegistry();
        var sf = new SoftFunction { Name = "MY_FN", Thing = new Dbref(1), Attribute = "FN_BODY", MinArgs = 1, MaxArgs = 3 };
        FunctionRestrictions.Apply(sf.Gate, "Wizard");
        funcs1.SaveSoft(sf);

        var siteLocks1 = new SiteLockTable();
        siteLocks1.Add("192.168.1.*", 0, SiteLockTable.Default);

        store.Save(channels1, mail1, aliases1, funcs1, siteLocks1);

        // Load into fresh instances
        var channels2 = new ChannelRegistry();
        var mail2 = new MailStore();
        var aliases2 = new MailAliasTable();
        var funcs2 = new FunctionRegistry();
        var siteLocks2 = new SiteLockTable();

        store.Load(channels2, mail2, aliases2, funcs2, siteLocks2);

        var loadedCh = channels2.GetChannel("Staff");
        Assert.NotNull(loadedCh);
        Assert.Equal(25, loadedCh!.BufferCapacity);
        Assert.True(loadedCh.GetMember(new Dbref(1))!.IsHidden);
        Assert.Equal("#1", loadedCh.GetLockText("join"));

        Assert.Equal(3, mail2.GetCurrentFolder(new Dbref(2)));
        var msgs = mail2.GetMessages(new Dbref(2), 0);
        Assert.Single(msgs);
        Assert.Equal("Welcome", msgs[0].Subject);
        Assert.True(msgs[0].IsUrgent);

        Assert.Single(aliases2.Aliases);
        Assert.Equal(2, aliases2.Aliases[0].Members.Count);

        var loadedSf = funcs2.FindSoft("MY_FN");
        Assert.NotNull(loadedSf);
        Assert.Contains("Wizard", loadedSf!.Gate.Restrictions);

        Assert.False(siteLocks2.Can("192.168.1.50", SiteLockTable.Connect));
    }

    [Fact]
    public void CheckPass_VerifiesXyxxyHashDirectly()
    {
        var db = new ObjectDatabase();
        var room = db.CreateObject("Hall", ObjectType.Room, Dbref.Nothing);         // #0
        var wiz = db.CreateObject("Wizard", ObjectType.Player, Dbref.Nothing);      // #1
        wiz.Owner = wiz.Id;
        wiz.AddFlag("WIZARD");
        db.MoveObject(wiz.Id, room.Id);

        var alice = db.CreateObject("Alice", ObjectType.Player, wiz.Id);            // #2
        alice.Owner = alice.Id;
        PasswordHash.Store(alice, "hunter2");
        db.MoveObject(alice.Id, room.Id);

        var reg = new FunctionRegistry(db);
        var eval = new ExpressionEvaluator(reg.Dispatch, reg.Database);
        reg.Evaluator = eval;
        var wizCtx = new EvaluationContext(wiz.Id, wiz.Id, wiz.Id, room.Id);

        Assert.Equal("1", eval.Evaluate($"[checkpass({alice.Id},hunter2)]", wizCtx));
        Assert.Equal("0", eval.Evaluate($"[checkpass({alice.Id},wrong)]", wizCtx));
    }
}
