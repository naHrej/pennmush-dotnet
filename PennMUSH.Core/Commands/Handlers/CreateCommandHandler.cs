namespace PennMUSH.Core.Commands.Handlers;

using System;
using PennMUSH.Core.Database;
using PennMUSH.Core.Types;

public class CreateCommandHandler : ICommandHandler
{
    private readonly Func<bool>? _liveUseQuota;
    private readonly Func<int>? _liveStartingQuota;
    private bool UseQuotaNow => _liveUseQuota?.Invoke() ?? true;
    private int StartingQuotaNow => _liveStartingQuota?.Invoke() ?? 20;

    public CreateCommandHandler(Func<bool>? liveUseQuota = null, Func<int>? liveStartingQuota = null)
    {
        _liveUseQuota = liveUseQuota;
        _liveStartingQuota = liveStartingQuota;
    }

    public string Name => "@create";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (!QuotaRules.TryPay(db, db.GetObject(context.Executor), 1, UseQuotaNow, StartingQuotaNow))
        {
            context.Notify(Feedback.SorryYourBuildingQuotaHasRunOut);
            return false;
        }

        string name = string.IsNullOrWhiteSpace(context.LeftArg) ? "Thing" : context.LeftArg;
        var obj = db.CreateObject(name, ObjectType.Thing, context.Executor);

        if (!context.Switches.Contains("silent"))
            context.Notify(Feedback.CreatedValueValue, ("value", $"{obj.Name}"), ("value2", $"{obj.Id}"));

        return true;
    }
}