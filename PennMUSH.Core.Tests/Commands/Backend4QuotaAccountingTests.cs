namespace PennMUSH.Core.Tests.Commands;

using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Configuration;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Types;
using Xunit;

public class Backend4QuotaAccountingTests
{
    private readonly ObjectDatabase _db = new();
    private readonly MushConfiguration _config = new() { UseQuota = true, StartingQuota = 20 };
    private readonly FunctionRegistry _registry;
    private readonly ExpressionEvaluator _evaluator;
    private readonly CommandDispatcher _dispatcher;
    private readonly List<Notification> _notifications = new();
    private readonly MUSHObject _room;
    private readonly MUSHObject _god;
    private readonly MUSHObject _ada;
    private readonly MUSHObject _bea;

    public Backend4QuotaAccountingTests()
    {
        _room = _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1)); // #0
        _god = _db.CreateObject("One", ObjectType.Player, new Dbref(1));    // #1
        _god.Owner = _god.Id;
        _god.AddFlag("WIZARD");
        _db.MoveObject(_god.Id, _room.Id);

        _ada = _db.CreateObject("Ada", ObjectType.Player, _god.Id);         // #2
        _ada.Owner = _ada.Id;
        _db.MoveObject(_ada.Id, _room.Id);

        _bea = _db.CreateObject("Bea", ObjectType.Player, _god.Id);         // #3
        _bea.Owner = _bea.Id;
        _db.MoveObject(_bea.Id, _room.Id);

        _registry = new FunctionRegistry(_db)
        {
            NotificationDispatcher = n => _notifications.Add(n),
            UseQuota = _config.UseQuota,
            UseQuotaProvider = () => _config.UseQuota,
            StartingQuota = _config.StartingQuota,
            StartingQuotaProvider = () => _config.StartingQuota
        };
        _evaluator = new ExpressionEvaluator(_registry.Dispatch, _registry.Database);
        _registry.Evaluator = _evaluator;

        _dispatcher = new CommandDispatcher(_evaluator);
        _dispatcher.Register(new SetCommandHandler(), "@set");
        _dispatcher.Register(new PowerCommandHandler(), "@power");
        _dispatcher.Register(new EnableCommandHandler(_config, false), "@disable");
        _dispatcher.Register(new EnableCommandHandler(_config, true), "@enable");
        _dispatcher.Register(new CreateCommandHandler(() => _config.UseQuota, () => _config.StartingQuota), "@create");
        _dispatcher.Register(new OpenCommandHandler(() => _config.UseQuota, () => _config.StartingQuota), "@open");
        _dispatcher.Register(new DigCommandHandler(liveUseQuota: () => _config.UseQuota, liveStartingQuota: () => _config.StartingQuota), "@dig");
        _dispatcher.Register(new CloneCommandHandler(() => _config.UseQuota, () => _config.StartingQuota), "@clone");
        _dispatcher.Register(new DestroyCommandHandler(() => _config.UseQuota, () => _config.StartingQuota), "@destroy", "@nuke");
        _dispatcher.Register(new UndestroyCommandHandler(), "@undestroy");
        _dispatcher.Register(new PurgeCommandHandler(() => _config.UseQuota, () => _config.StartingQuota), "@purge");
        _dispatcher.Register(new ChownCommandHandler(() => _config.UseQuota, () => _config.StartingQuota), "@chown");
        _dispatcher.Register(new ChownAllCommandHandler(_config.StartingQuota, () => _config.UseQuota, () => _config.StartingQuota), "@chownall");
        _dispatcher.Register(new QuotaCommandHandler(_config.UseQuota, _config.StartingQuota, () => _config.UseQuota, () => _config.StartingQuota), "@quota");
        _dispatcher.Register(new SquotaCommandHandler(_config.UseQuota, _config.StartingQuota, () => _config.UseQuota, () => _config.StartingQuota), "@squota");
        _dispatcher.Register(new AllQuotaCommandHandler(_config.StartingQuota, _config.UseQuota, () => _config.UseQuota, () => _config.StartingQuota), "@allquota");
    }

    [Fact]
    public void FlagsAndPowers_NoQuotaQuotasAndMistrust_EnforcePermissionsAndInheritance()
    {
        var robot = _db.CreateObject("Robot", ObjectType.Thing, _ada.Id);
        _db.MoveObject(robot.Id, _room.Id);

        // Mortal cannot set wizard-only flags or powers
        Assert.Contains("Permission denied.", _dispatcher.Dispatch(_ada.Id, "@set Ada=NO_QUOTA", _db).Notifications);
        Assert.Contains("Permission denied.", _dispatcher.Dispatch(_ada.Id, "@set Ada=QUOTAS", _db).Notifications);
        Assert.Contains("Permission denied.", _dispatcher.Dispatch(_ada.Id, $"@set #{robot.Id.Value}=MISTRUST", _db).Notifications);
        Assert.Contains("Permission denied.", _dispatcher.Dispatch(_ada.Id, $"@power #{_ada.Id.Value}=No_Quota", _db).Notifications);
        Assert.Contains("Permission denied.", _dispatcher.Dispatch(_ada.Id, $"@power #{_ada.Id.Value}=Quotas", _db).Notifications);

        // Mortal cannot inspect another player's quota without QUOTAS flag/power
        var adaCtx = new EvaluationContext(_ada.Id, _ada.Id, _ada.Id, _room.Id);
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate($"[quota({_bea.Id})]", adaCtx));
        Assert.Contains("You can't look at someone else's quota.", _dispatcher.Dispatch(_ada.Id, "@quota Bea", _db).Notifications);

        // QUOTAS flag grants read access
        Assert.Contains("Set.", _dispatcher.Dispatch(_god.Id, "@set Ada=QUOTAS", _db).Notifications);
        Assert.Contains("Objects: 0   Limit: 20", _dispatcher.Dispatch(_ada.Id, "@quota Bea", _db).Notifications);
        Assert.Equal("21", _evaluator.Evaluate($"[quota({_bea.Id})]", adaCtx));

        // Clear QUOTAS flag, grant Quotas power
        _dispatcher.Dispatch(_god.Id, "@set Ada=!QUOTAS", _db);
        Assert.Equal("#-1 PERMISSION DENIED", _evaluator.Evaluate($"[quota({_bea.Id})]", adaCtx));
        _dispatcher.Dispatch(_god.Id, $"@power #{_ada.Id.Value}=Quotas", _db);
        Assert.Contains("Objects: 0   Limit: 20", _dispatcher.Dispatch(_ada.Id, "@quota Bea", _db).Notifications);
        Assert.Equal("21", _evaluator.Evaluate($"[quota({_bea.Id})]", adaCtx));

        // Exhaust Ada's RQUOTA, then grant NO_QUOTA flag
        _dispatcher.Dispatch(_god.Id, "@squota Ada=1", _db); // Ada owns Robot (1), so RQUOTA=0
        Assert.Equal("0", _ada.GetAttribute("RQUOTA")!.Value);

        Assert.Contains("Set.", _dispatcher.Dispatch(_god.Id, "@set Ada=NO_QUOTA", _db).Notifications);
        Assert.Contains("Objects: 1   Limit: UNLIMITED", _dispatcher.Dispatch(_ada.Id, "@quota", _db).Notifications);
        Assert.Equal("99999", _evaluator.Evaluate("[quota()]", adaCtx));

        // Ada and Ada's Thing can build at RQUOTA=0 without modifying RQUOTA
        int countBefore = _db.Count;
        Assert.Contains("Created:", _dispatcher.Dispatch(_ada.Id, "@create Badge", _db).Notifications.Single());
        Assert.Equal(countBefore + 1, _db.Count);
        Assert.Equal("0", _ada.GetAttribute("RQUOTA")!.Value);
        Assert.Contains("Created:", _dispatcher.Dispatch(robot.Id, "@create Gear", _db).Notifications.Single());
        Assert.Equal(countBefore + 2, _db.Count);

        // MISTRUST on Robot prevents inheriting Ada's NO_QUOTA / No_Quota
        Assert.Contains("Set.", _dispatcher.Dispatch(_god.Id, $"@set #{robot.Id.Value}=MISTRUST", _db).Notifications);
        Assert.Contains("Sorry, your building quota has run out.", _dispatcher.Dispatch(robot.Id, "@create BlockedGear", _db).Notifications);
        Assert.Equal(countBefore + 2, _db.Count);

        // Swap NO_QUOTA flag for No_Quota power
        _dispatcher.Dispatch(_god.Id, "@set Ada=!NO_QUOTA", _db);
        Assert.NotEqual("99999", _evaluator.Evaluate("[quota()]", adaCtx));
        _dispatcher.Dispatch(_god.Id, $"@power #{_ada.Id.Value}=No_Quota", _db);
        Assert.Contains("Objects: 2   Limit: UNLIMITED", _dispatcher.Dispatch(_ada.Id, "@quota", _db).Notifications);
        Assert.Equal("99999", _evaluator.Evaluate("[quota()]", adaCtx));
        Assert.Contains("Created:", _dispatcher.Dispatch(_ada.Id, "@create Token", _db).Notifications.Single());
        Assert.Equal("0", _ada.GetAttribute("RQUOTA")!.Value);

        // Still blocked through MISTRUST robot
        Assert.Contains("Sorry, your building quota has run out.", _dispatcher.Dispatch(robot.Id, "@create BlockedGear2", _db).Notifications);
    }

    [Fact]
    public void BuildingCommands_EnforceAndDeductQuotaAtomically()
    {
        _dispatcher.Dispatch(_god.Id, "@squota Ada=3", _db);
        Assert.Equal("3", _ada.GetAttribute("RQUOTA")!.Value);

        var created = _dispatcher.Dispatch(_ada.Id, "@create Lamp", _db);
        Assert.Contains("Created:", created.Notifications.Single());
        Assert.Equal("2", _ada.GetAttribute("RQUOTA")!.Value);

        var cloned = _dispatcher.Dispatch(_ada.Id, "@clone Lamp", _db);
        Assert.Contains("Cloned:", cloned.Notifications.Single());
        Assert.Equal("1", _ada.GetAttribute("RQUOTA")!.Value);

        var opened = _dispatcher.Dispatch(_ada.Id, "@open North", _db);
        Assert.Contains("Opened.", opened.Notifications);
        Assert.Equal("0", _ada.GetAttribute("RQUOTA")!.Value);

        // Exhausted: @create, @open, @clone, @dig all fail cleanly without creating objects
        int countAtZero = _db.Count;
        Assert.Contains("Sorry, your building quota has run out.", _dispatcher.Dispatch(_ada.Id, "@create Blocked", _db).Notifications);
        Assert.Contains("Sorry, your building quota has run out.", _dispatcher.Dispatch(_ada.Id, "@open BlockedExit", _db).Notifications);
        Assert.Contains("Sorry, your building quota has run out.", _dispatcher.Dispatch(_ada.Id, "@clone Lamp", _db).Notifications);
        Assert.Contains("Sorry, your building quota has run out.", _dispatcher.Dispatch(_ada.Id, "@dig BlockedRoom", _db).Notifications);
        Assert.Equal(countAtZero, _db.Count);

        // Multi-object @dig is atomic: RQUOTA=2 rejects 3-object dig without creating room/exits
        _dispatcher.Dispatch(_god.Id, "@squota Ada=+2", _db); // RQUOTA = 2
        int countBeforeDig = _db.Count;
        var failedDig = _dispatcher.Dispatch(_ada.Id, "@dig Hall=East,West", _db);
        Assert.Contains("Sorry, your building quota has run out.", failedDig.Notifications);
        Assert.Equal(countBeforeDig, _db.Count);
        Assert.Equal("2", _ada.GetAttribute("RQUOTA")!.Value);

        // 2-object dig succeeds and uses exact cost of 2
        var okDig = _dispatcher.Dispatch(_ada.Id, "@dig Hall=East", _db);
        Assert.Contains(okDig.Notifications, n => n.Contains("Hall created"));
        Assert.Equal(countBeforeDig + 2, _db.Count);
        Assert.Equal("0", _ada.GetAttribute("RQUOTA")!.Value);
    }

    [Fact]
    public void BuildingFunctionsAndQuotaFunc_EnforceQuotaAtomicallyAndTrackTotals()
    {
        _dispatcher.Dispatch(_god.Id, "@squota Ada=2", _db);
        var adaCtx = new EvaluationContext(_ada.Id, _ada.Id, _ada.Id, _room.Id);
        Assert.Equal("3", _evaluator.Evaluate("[quota()]", adaCtx));
        Assert.Equal("yes", _evaluator.Evaluate("[config(use_quota)]", adaCtx));
        Assert.Equal("20", _evaluator.Evaluate("[config(starting_quota)]", adaCtx));

        // Atomic dig() requiring 3 quota fails at RQUOTA=2 without creating anything
        int before = _db.Count;
        _notifications.Clear();
        Assert.Equal("#-1", _evaluator.Evaluate("[dig(Chamber,In,Out)]", adaCtx));
        Assert.Equal(before, _db.Count);
        Assert.Equal("2", _ada.GetAttribute("RQUOTA")!.Value);
        Assert.Contains(_notifications, n => n.Scope == NotificationScope.Self && n.Target == _ada.Id && n.Message == "Sorry, your building quota has run out.");

        // create() + open() exhaust remaining 2
        string createdRef = _evaluator.Evaluate("[create(Relic)]", adaCtx);
        Assert.True(Dbref.TryParse(createdRef, out var relicRef) && relicRef.IsValid);
        Assert.Equal("1", _ada.GetAttribute("RQUOTA")!.Value);
        Assert.Equal("3", _evaluator.Evaluate("[quota()]", adaCtx));

        string openRef = _evaluator.Evaluate("[open(Portal)]", adaCtx);
        Assert.True(Dbref.TryParse(openRef, out var portalRef) && portalRef.IsValid);
        Assert.Equal("0", _ada.GetAttribute("RQUOTA")!.Value);
        Assert.Equal("3", _evaluator.Evaluate("[quota()]", adaCtx));

        // create(), open(), dig(), clone() all fail cleanly at 0
        int countAtZero = _db.Count;
        Assert.Equal("#-1", _evaluator.Evaluate("[create(Extra)]", adaCtx));
        Assert.Equal("#-1", _evaluator.Evaluate("[open(ExtraExit)]", adaCtx));
        Assert.Equal("#-1", _evaluator.Evaluate("[dig(ExtraRoom)]", adaCtx));
        Assert.Equal("#-1", _evaluator.Evaluate($"[clone({createdRef})]", adaCtx));
        Assert.Equal(countAtZero, _db.Count);

        // Destroying via @destroy/@destroy keeps quota total (3) and frees 1 RQUOTA
        _dispatcher.Dispatch(_ada.Id, $"@destroy #{relicRef.Value}", _db);
        _dispatcher.Dispatch(_ada.Id, $"@destroy #{relicRef.Value}", _db);
        Assert.Equal("1", _ada.GetAttribute("RQUOTA")!.Value);
        Assert.Equal("3", _evaluator.Evaluate("[quota()]", adaCtx));
    }

    [Fact]
    public void DestroyAndPurge_RefundOnlyOnFinalFreeAndDoNotInflateUninitializedQuota()
    {
        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _ada.Id);
        var gem = _db.CreateObject("Gem", ObjectType.Thing, _ada.Id);
        var wand = _db.CreateObject("Wand", ObjectType.Thing, _ada.Id);

        // Ada has uninitialized RQUOTA and owns 3 objects; starting_quota = 20 -> Remaining() initializes to 17
        Assert.Null(_ada.GetAttribute("RQUOTA"));

        // Marking GOING does NOT refund; @undestroy leaves RQUOTA unchanged
        _dispatcher.Dispatch(_ada.Id, $"@destroy #{lamp.Id.Value}", _db);
        Assert.True(lamp.HasFlag("GOING"));
        Assert.Null(_ada.GetAttribute("RQUOTA"));

        _dispatcher.Dispatch(_ada.Id, $"@undestroy #{lamp.Id.Value}", _db);
        Assert.False(lamp.HasFlag("GOING"));
        Assert.Null(_ada.GetAttribute("RQUOTA"));

        // Second @destroy frees lamp and refunds +1 against initialized 17 -> 18 (not 19!)
        _dispatcher.Dispatch(_ada.Id, $"@destroy #{lamp.Id.Value}", _db);
        _dispatcher.Dispatch(_ada.Id, $"@destroy #{lamp.Id.Value}", _db);
        Assert.Equal(ObjectType.Garbage, lamp.Type);
        Assert.Equal("18", _ada.GetAttribute("RQUOTA")!.Value);

        // @purge frees multiple GOING objects and refunds +1 each
        _dispatcher.Dispatch(_ada.Id, $"@destroy #{gem.Id.Value}", _db);
        _dispatcher.Dispatch(_ada.Id, $"@destroy #{wand.Id.Value}", _db);
        Assert.Equal("18", _ada.GetAttribute("RQUOTA")!.Value);

        var purged = _dispatcher.Dispatch(_god.Id, "@purge", _db);
        Assert.Contains("Purged 2 objects.", purged.Notifications);
        Assert.Equal("20", _ada.GetAttribute("RQUOTA")!.Value);

        // God/Wizard/No_Quota owners never get RQUOTA written on destroy/purge
        var wizOrb = _db.CreateObject("WizOrb", ObjectType.Thing, _god.Id);
        wizOrb.AddFlag("GOING");
        _dispatcher.Dispatch(_god.Id, "@purge", _db);
        Assert.Null(_god.GetAttribute("RQUOTA"));
    }

    [Fact]
    public void Chown_TransfersQuotaAtomicallyEnforcesLimitsAndSupportsOverdraftForWizards()
    {
        var box = _db.CreateObject("Box", ObjectType.Thing, _ada.Id);
        // Uninitialized RQUOTA: Ada owns Box (1), Bea owns 0 -> Ada starts at 19, Bea starts at 20 -> Ada 20, Bea 19
        var okTransfer = _dispatcher.Dispatch(_ada.Id, $"@chown #{box.Id.Value}=Bea", _db);
        Assert.Contains($"Owner of Box ({box.Id}) changed to Bea.", okTransfer.Notifications);
        Assert.Equal(_bea.Id, box.Owner);
        Assert.Equal("20", _ada.GetAttribute("RQUOTA")!.Value);
        Assert.Equal("19", _bea.GetAttribute("RQUOTA")!.Value);

        // Exhaust Ada's RQUOTA and verify mortal @chown to Ada fails cleanly without mutation
        _dispatcher.Dispatch(_god.Id, "@squota Ada=0", _db);
        var deniedOwnerChown = _dispatcher.Dispatch(_bea.Id, $"@chown #{box.Id.Value}=Ada", _db);
        Assert.Contains("That player doesn't have enough quota.", deniedOwnerChown.Notifications);
        Assert.Equal(_bea.Id, box.Owner);
        Assert.Equal("0", _ada.GetAttribute("RQUOTA")!.Value);
        Assert.Equal("19", _bea.GetAttribute("RQUOTA")!.Value);

        // CHOWN_OK self-chown fails cleanly when Ada lacks quota, preserves CHOWN_OK & Owner
        box.AddFlag("CHOWN_OK");
        var deniedSelfChown = _dispatcher.Dispatch(_ada.Id, $"@chown #{box.Id.Value}=me", _db);
        Assert.Contains("Sorry, your building quota has run out.", deniedSelfChown.Notifications);
        Assert.Equal(_bea.Id, box.Owner);
        Assert.True(box.HasFlag("CHOWN_OK"));
        Assert.Equal("0", _ada.GetAttribute("RQUOTA")!.Value);
        Assert.Equal("19", _bea.GetAttribute("RQUOTA")!.Value);

        // Grant Ada 1 quota -> CHOWN_OK self-chown succeeds, clears CHOWN_OK, updates both RQUOTAs
        _dispatcher.Dispatch(_god.Id, "@squota Ada=+1", _db);
        var okSelfChown = _dispatcher.Dispatch(_ada.Id, $"@chown #{box.Id.Value}=me", _db);
        Assert.Contains($"Owner of Box ({box.Id}) changed to Ada.", okSelfChown.Notifications);
        Assert.Equal(_ada.Id, box.Owner);
        Assert.False(box.HasFlag("CHOWN_OK"));
        Assert.Equal("0", _ada.GetAttribute("RQUOTA")!.Value);
        Assert.Equal("20", _bea.GetAttribute("RQUOTA")!.Value);

        // Wizard @chown can overdraft recipient (Bea 0 -> -1, Ada 0 -> +1)
        _dispatcher.Dispatch(_god.Id, "@squota Bea=1", _db); // Bea owns 0 -> setting limit=1 sets RQUOTA=1; spend it
        _dispatcher.Dispatch(_bea.Id, "@create Pebbles", _db);
        Assert.Equal("0", _bea.GetAttribute("RQUOTA")!.Value);

        var wizChown = _dispatcher.Dispatch(_god.Id, $"@chown #{box.Id.Value}=Bea", _db);
        Assert.Contains($"Owner of Box ({box.Id}) changed to Bea.", wizChown.Notifications);
        Assert.Equal(_bea.Id, box.Owner);
        Assert.Equal("1", _ada.GetAttribute("RQUOTA")!.Value);
        Assert.Equal("-1", _bea.GetAttribute("RQUOTA")!.Value);
    }

    [Fact]
    public void DisableAndEnableUseQuota_TogglesQuotaEnforcementLive()
    {
        var adaCtx = new EvaluationContext(_ada.Id, _ada.Id, _ada.Id, _room.Id);
        _dispatcher.Dispatch(_god.Id, "@squota Ada=0", _db);
        _dispatcher.Dispatch(_god.Id, "@squota Bea=0", _db);
        Assert.Contains("Sorry, your building quota has run out.", _dispatcher.Dispatch(_ada.Id, "@create Refused", _db).Notifications);

        // Disable use_quota live
        Assert.Contains("Disabled.", _dispatcher.Dispatch(_god.Id, "@disable use_quota", _db).Notifications);
        Assert.Equal("99999", _evaluator.Evaluate("[quota()]", adaCtx));
        Assert.Equal("no", _evaluator.Evaluate("[config(use_quota)]", adaCtx));

        // Building, functions, @chown, @destroy/@purge, and @chownall ignore and preserve RQUOTA=0 while disabled
        Assert.Contains("Created:", _dispatcher.Dispatch(_ada.Id, "@create FreeThing", _db).Notifications.Single());
        var freeFuncRef = _evaluator.Evaluate("[create(FreeFuncThing)]", adaCtx);
        Assert.True(Dbref.TryParse(freeFuncRef, out var freeId) && freeId.IsValid);
        Assert.Equal("0", _ada.GetAttribute("RQUOTA")!.Value);

        var freeObj = _db.GetObject(freeId)!;
        var chownDisabled = _dispatcher.Dispatch(_ada.Id, $"@chown #{freeId.Value}=Bea", _db);
        Assert.Contains("changed to Bea.", chownDisabled.Notifications.Single());
        Assert.Equal(_bea.Id, freeObj.Owner);
        Assert.Equal("0", _ada.GetAttribute("RQUOTA")!.Value);
        Assert.Equal("0", _bea.GetAttribute("RQUOTA")!.Value);

        _dispatcher.Dispatch(_bea.Id, $"@destroy #{freeId.Value}", _db);
        _dispatcher.Dispatch(_bea.Id, $"@destroy #{freeId.Value}", _db);
        Assert.Equal(ObjectType.Garbage, freeObj.Type);
        Assert.Equal("0", _bea.GetAttribute("RQUOTA")!.Value);

        var spare = _db.CreateObject("Spare", ObjectType.Thing, _ada.Id);
        _dispatcher.Dispatch(_god.Id, "@chownall Ada=Bea", _db);
        Assert.Equal(_bea.Id, spare.Owner);
        Assert.Equal("0", _ada.GetAttribute("RQUOTA")!.Value);
        Assert.Equal("0", _bea.GetAttribute("RQUOTA")!.Value);

        // Re-enable use_quota live: enforcement resumes immediately
        Assert.Contains("Enabled.", _dispatcher.Dispatch(_god.Id, "@enable use_quota", _db).Notifications);
        Assert.Equal("yes", _evaluator.Evaluate("[config(use_quota)]", adaCtx));
        Assert.Contains("Sorry, your building quota has run out.", _dispatcher.Dispatch(_ada.Id, "@create BlockedAgain", _db).Notifications);
        Assert.Equal("#-1", _evaluator.Evaluate("[create(FuncBlockedAgain)]", adaCtx));
    }
}
