namespace PennMUSH.Core.Commands.Handlers;

using System.Globalization;
using PennMUSH.Core.Attributes;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Types;

public class QuotaCommandHandler : ICommandHandler
{
    private readonly bool _useQuota;
    private readonly Func<bool>? _liveUseQuota;
    private readonly int _startingQuota;
    private bool UseQuotaNow => _liveUseQuota?.Invoke() ?? _useQuota;

    public QuotaCommandHandler(bool useQuota = true, int startingQuota = 20, Func<bool>? liveUseQuota = null)
    {
        _useQuota = useQuota;
        _startingQuota = startingQuota;
        _liveUseQuota = liveUseQuota;
    }

    public string Name => "@quota";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (context.Switches.Contains("all"))
            return QuotaRules.All(context, db, _startingQuota, context.LeftArg);
        if (context.Switches.Contains("set"))
            return QuotaRules.Set(context, db, UseQuotaNow, _startingQuota);
        return QuotaRules.Show(context, db, UseQuotaNow, _startingQuota);
    }
}

public class SquotaCommandHandler : ICommandHandler
{
    private readonly bool _useQuota;
    private readonly Func<bool>? _liveUseQuota;
    private readonly int _startingQuota;
    private bool UseQuotaNow => _liveUseQuota?.Invoke() ?? _useQuota;

    public SquotaCommandHandler(bool useQuota = true, int startingQuota = 20, Func<bool>? liveUseQuota = null)
    {
        _useQuota = useQuota;
        _startingQuota = startingQuota;
        _liveUseQuota = liveUseQuota;
    }

    public string Name => "@squota";

    public bool Execute(CommandContext context, ObjectDatabase db)
        => QuotaRules.Set(context, db, UseQuotaNow, _startingQuota);
}

public class AllQuotaCommandHandler : ICommandHandler
{
    private readonly int _startingQuota;

    public AllQuotaCommandHandler(int startingQuota = 20)
    {
        _startingQuota = startingQuota;
    }

    public string Name => "@allquota";

    public bool Execute(CommandContext context, ObjectDatabase db)
        => QuotaRules.All(context, db, _startingQuota, context.LeftArg);
}

internal static class QuotaRules
{
    private static readonly Dbref God = new(1);

    public static bool Show(CommandContext context, ObjectDatabase db, bool useQuota, int startingQuota)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;
        if (!TryResolve(db, actor, context.LeftArg, out var who))
        {
            context.Notify(Feedback.NoSuchPlayer);
            return false;
        }
        if (!CanSee(actor, who))
        {
            context.Notify(Feedback.YouCantLookAtSomeoneElsesQuota);
            return false;
        }

        int owned = Owned(db, who.Id);
        if (Unlimited(who, db) || !useQuota)
        {
            context.Notify(Feedback.ObjectsLimitUnlimited, ("value", $"{owned}"));
            return true;
        }

        int remaining = Remaining(db, who, startingQuota);
        context.Notify(Feedback.ObjectsLimit, ("value", $"{owned}"), ("value2", $"{owned + remaining}"));
        return true;
    }

    public static bool Set(CommandContext context, ObjectDatabase db, bool useQuota, int startingQuota)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;
        if (!TryResolve(db, actor, context.LeftArg, out var who))
        {
            context.Notify(Feedback.NoSuchPlayer);
            return false;
        }
        if (!actor.IsWizard)
        {
            context.Notify(Feedback.OnlyWizardsMayChangeAQuota);
            return false;
        }
        if (!CanSee(actor, who))
        {
            context.Notify(Feedback.YouCantLookAtSomeoneElsesQuota);
            return false;
        }

        int owned = Owned(db, who.Id);
        if (Unlimited(who, db) || !useQuota)
        {
            context.Notify(Feedback.ObjectsLimitUnlimited, ("value", $"{owned}"));
            return true;
        }

        if (string.IsNullOrEmpty(context.RightArg.Trim()))
        {
            int current = Remaining(db, who, startingQuota);
            context.Notify(Feedback.ObjectsLimit, ("value", $"{owned}"), ("value2", $"{owned + current}"));
            context.Notify(Feedback.WhatDoYouWantToSetTheQuotaTo);
            return false;
        }

        string amount = context.RightArg.Trim();
        int remaining = Remaining(db, who, startingQuota);
        bool adjust = amount[0] == '+' || amount[0] == '-';
        int limit = adjust ? owned + remaining + Atoi(amount) : Atoi(amount);
        if (limit < owned)
            limit = owned;
        Store(who, limit - owned);
        context.Notify(Feedback.ObjectsLimit, ("value", $"{owned}"), ("value2", $"{limit}"));
        return true;
    }

    public static bool All(CommandContext context, ObjectDatabase db, int startingQuota, string requested)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.IsGod)
        {
            context.Notify(Feedback.WhoDoYouThinkYouAreGod);
            return false;
        }

        string arg = requested.Trim();
        int limit = -1;
        if (arg.Length > 0)
        {
            if (!int.TryParse(arg, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out limit))
            {
                context.Notify(Feedback.YouCanOnlySetQuotasToANumber);
                return false;
            }
            if (limit < 0)
            {
                context.Notify(Feedback.YouCanOnlySetQuotasToAPositiveNumber);
                return false;
            }
        }

        bool quiet = context.Switches.Contains("quiet");
        foreach (var who in db.GetAllObjects())
        {
            if (who.Type != ObjectType.Player)
                continue;
            int owned = Owned(db, who.Id);
            if (Unlimited(who, db))
            {
                if (!quiet)
                    context.Notify(Feedback.ObjectsLimitUnlimited2, ("value", $"{who.Name}"), ("value2", $"{owned}"));
                continue;
            }
            if (!quiet)
            {
                int oldRemaining = Remaining(db, who, startingQuota);
                context.Notify(Feedback.ObjectsLimit2, ("value", $"{who.Name}"), ("value2", $"{owned}"), ("value3", $"{oldRemaining}"));
            }
            if (limit != -1)
                Store(who, limit <= owned ? 0 : limit - owned);
        }

        if (limit == -1)
            context.Notify(Feedback.QuotasNotChanged);
        else
            context.Notify(Feedback.AllQuotasChangedTo, ("value", $"{limit}"));
        return true;
    }

    private static bool TryResolve(ObjectDatabase db, MUSHObject actor, string name, out MUSHObject who)
    {
        string token = name.Trim();
        if (token.Length == 0 || token.Equals("me", System.StringComparison.OrdinalIgnoreCase))
        {
            var owner = db.GetObject(actor.Owner);
            if (owner == null || owner.Type != ObjectType.Player)
            {
                who = actor;
                return false;
            }
            who = owner;
            return true;
        }

        var found = db.FindPlayer(token);
        if (found == null || found.Type != ObjectType.Player)
        {
            who = actor;
            return false;
        }
        who = found;
        return true;
    }

    private static bool CanSee(MUSHObject actor, MUSHObject who)
        => actor.IsRoyalty
            || actor.HasPower("QUOTAS")
            || actor.HasPower("SEE_ALL")
            || actor.IsWizard
            || who.Id == actor.Id
            || who.Owner == actor.Owner;

    private static bool Unlimited(MUSHObject who, ObjectDatabase db)
    {
        if (who.IsRoyalty || who.HasPower("NO_QUOTA"))
            return true;
        var owner = db.GetObject(who.Owner);
        if (owner == null)
            return false;
        if (owner.IsRoyalty)
            return true;
        return !who.HasFlag("MISTRUST") && owner.HasPower("NO_QUOTA");
    }

    private static int Owned(ObjectDatabase db, Dbref who)
    {
        int owned = -1;
        foreach (var obj in db.GetAllObjects())
        {
            if (obj.Type == ObjectType.Garbage)
                continue;
            if (obj.Owner == who)
                owned++;
        }
        return owned;
    }

    private static int Remaining(ObjectDatabase db, MUSHObject who, int startingQuota)
    {
        var entry = who.GetAttribute("RQUOTA");
        if (entry != null)
            return int.TryParse(entry.Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int stored) ? stored : 0;

        int owned = Owned(db, who.Id);
        int remaining = owned <= startingQuota ? startingQuota - owned : owned;
        Store(who, remaining);
        return remaining;
    }

    public static void Adjust(ObjectDatabase db, MUSHObject who, int payment, int startingQuota)
    {
        Store(who, Remaining(db, who, startingQuota) + payment);
    }

    private static void Store(MUSHObject who, int remaining)
    {
        who.SetAttribute(
            "RQUOTA",
            remaining.ToString(CultureInfo.InvariantCulture),
            God,
            AttributeFlags.MDark | AttributeFlags.Locked,
            wizardOverride: true);
    }

    private static int Atoi(string text)
    {
        int i = 0;
        while (i < text.Length && char.IsWhiteSpace(text[i]))
            i++;
        int sign = 1;
        if (i < text.Length && (text[i] == '+' || text[i] == '-'))
        {
            if (text[i] == '-')
                sign = -1;
            i++;
        }
        int value = 0;
        while (i < text.Length && char.IsDigit(text[i]))
        {
            value = value * 10 + (text[i] - '0');
            i++;
        }
        return sign * value;
    }
}
