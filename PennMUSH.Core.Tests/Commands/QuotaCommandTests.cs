namespace PennMUSH.Core.Tests.Commands;

using PennMUSH.Core.Commands;
using PennMUSH.Core.Commands.Handlers;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;
using Xunit;

public class QuotaCommandTests
{
    private readonly ObjectDatabase _db = new();
    private readonly CommandDispatcher _dispatcher;
    private readonly Dbref _god;
    private readonly Dbref _ada;

    public QuotaCommandTests()
    {
        _db.CreateObject("Limbo", ObjectType.Room, new Dbref(1));
        _god = _db.CreateObject("One", ObjectType.Player, new Dbref(1)).Id;
        var ada = _db.CreateObject("Ada", ObjectType.Player, new Dbref(1));
        ada.Owner = ada.Id;
        _ada = ada.Id;
        _dispatcher = new CommandDispatcher();
        _dispatcher.Register(new QuotaCommandHandler(), "@quota");
        _dispatcher.Register(new SquotaCommandHandler(), "@squota");
        _dispatcher.Register(new AllQuotaCommandHandler(), "@allquota");
    }

    [Fact]
    public void Quota_ShowsOwnedObjectsAndTheLimit()
    {
        var first = _dispatcher.Dispatch(_ada, "@quota", _db);
        Assert.Contains("Objects: 0   Limit: 20", first.Notifications);
        Assert.Equal("20", _db.GetObject(_ada)!.GetAttribute("RQUOTA")!.Value);

        var lamp = _db.CreateObject("Lamp", ObjectType.Thing, _ada);
        var second = _dispatcher.Dispatch(_ada, "@quota me", _db);
        Assert.Contains("Objects: 1   Limit: 21", second.Notifications);

        _db.DestroyObject(lamp.Id);
        var third = _dispatcher.Dispatch(_ada, "@quota", _db);
        Assert.Contains("Objects: 0   Limit: 20", third.Notifications);

        var other = _dispatcher.Dispatch(_ada, "@quota One", _db);
        Assert.Contains("You can't look at someone else's quota.", other.Notifications);

        var missing = _dispatcher.Dispatch(_ada, "@quota Nobody", _db);
        Assert.Contains("No such player.", missing.Notifications);

        var denied = _dispatcher.Dispatch(_ada, "@squota me=30", _db);
        Assert.Contains("Only wizards may change a quota.", denied.Notifications);
    }

    [Fact]
    public void Squota_SetsAndAdjustsOnePlayersLimit()
    {
        var set = _dispatcher.Dispatch(_god, "@quota/set Ada=8", _db);
        Assert.Contains("Objects: 0   Limit: 8", set.Notifications);
        Assert.Equal("8", _db.GetObject(_ada)!.GetAttribute("RQUOTA")!.Value);

        var plus = _dispatcher.Dispatch(_god, "@squota Ada=+3", _db);
        Assert.Contains("Objects: 0   Limit: 11", plus.Notifications);
        Assert.Equal("11", _db.GetObject(_ada)!.GetAttribute("RQUOTA")!.Value);

        _db.CreateObject("Lamp", ObjectType.Thing, _ada);
        var floor = _dispatcher.Dispatch(_god, "@squota Ada=-100", _db);
        Assert.Contains("Objects: 1   Limit: 1", floor.Notifications);
        Assert.Equal("0", _db.GetObject(_ada)!.GetAttribute("RQUOTA")!.Value);

        var blank = _dispatcher.Dispatch(_god, "@quota/set Ada", _db);
        Assert.Contains("Objects: 1   Limit: 1", blank.Notifications);
        Assert.Contains("What do you want to set the quota to?", blank.Notifications);

        var own = _dispatcher.Dispatch(_god, "@quota", _db);
        Assert.Contains("Objects: 1   Limit: UNLIMITED", own.Notifications);
        Assert.Null(_db.GetObject(_god)!.GetAttribute("RQUOTA"));
    }

    [Fact]
    public void AllQuota_ListsRemainingQuotaAndGodSetsEveryMortal()
    {
        var mortal = _dispatcher.Dispatch(_ada, "@allquota 4", _db);
        Assert.Contains("Who do you think you are, GOD?", mortal.Notifications);

        _db.GetObject(_ada)!.AddFlag("WIZARD");
        var wizard = _dispatcher.Dispatch(_ada, "@quota/all 4", _db);
        Assert.Contains("Who do you think you are, GOD?", wizard.Notifications);
        _db.GetObject(_ada)!.RemoveFlag("WIZARD");

        _db.CreateObject("Lamp", ObjectType.Thing, _ada);
        var listed = _dispatcher.Dispatch(_god, "@allquota", _db);
        Assert.Contains("One: Objects: 1   Limit: UNLIMITED", listed.Notifications);
        Assert.Contains("Ada: Objects: 1   Limit: 19", listed.Notifications);
        Assert.Contains("Quotas not changed.", listed.Notifications);
        Assert.Equal("19", _db.GetObject(_ada)!.GetAttribute("RQUOTA")!.Value);

        var changed = _dispatcher.Dispatch(_god, "@allquota/quiet 4", _db);
        Assert.DoesNotContain("Ada: Objects: 1   Limit: 19", changed.Notifications);
        Assert.Contains("All quotas changed to 4.", changed.Notifications);
        Assert.Equal("3", _db.GetObject(_ada)!.GetAttribute("RQUOTA")!.Value);
        Assert.Null(_db.GetObject(_god)!.GetAttribute("RQUOTA"));

        var word = _dispatcher.Dispatch(_god, "@allquota lots", _db);
        Assert.Contains("You can only set quotas to a number.", word.Notifications);
        var negative = _dispatcher.Dispatch(_god, "@allquota -2", _db);
        Assert.Contains("You can only set quotas to a positive number.", negative.Notifications);
    }

    [Fact]
    public void RoyaltyCanLookAndDisabledQuotasStayUnlimited()
    {
        var roy = _db.CreateObject("Bea", ObjectType.Player, new Dbref(1));
        roy.Owner = roy.Id;
        roy.AddFlag("ROYALTY");

        var looked = _dispatcher.Dispatch(roy.Id, "@quota Ada", _db);
        Assert.Contains("Objects: 0   Limit: 20", looked.Notifications);

        var own = _dispatcher.Dispatch(roy.Id, "@quota", _db);
        Assert.Contains("Objects: 0   Limit: UNLIMITED", own.Notifications);

        var cam = _db.CreateObject("Cam", ObjectType.Player, new Dbref(1));
        cam.Owner = cam.Id;
        var off = new CommandDispatcher();
        off.Register(new QuotaCommandHandler(useQuota: false), "@quota");
        var disabled = off.Dispatch(cam.Id, "@quota", _db);
        Assert.Contains("Objects: 0   Limit: UNLIMITED", disabled.Notifications);
        Assert.Null(cam.GetAttribute("RQUOTA"));
    }
}
