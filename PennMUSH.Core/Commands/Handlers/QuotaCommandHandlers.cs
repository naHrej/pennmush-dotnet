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
    private readonly Func<int>? _liveStartingQuota;
    private bool UseQuotaNow => _liveUseQuota?.Invoke() ?? _useQuota;
    private int StartingQuotaNow => _liveStartingQuota?.Invoke() ?? _startingQuota;

    public QuotaCommandHandler(
        bool useQuota = true,
        int startingQuota = 20,
        Func<bool>? liveUseQuota = null,
        Func<int>? liveStartingQuota = null)
    {
        _useQuota = useQuota;
        _startingQuota = startingQuota;
        _liveUseQuota = liveUseQuota;
        _liveStartingQuota = liveStartingQuota;
    }

    public string Name => "@quota";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (context.Switches.Contains("all"))
            return QuotaRules.All(context, db, StartingQuotaNow, context.LeftArg, UseQuotaNow);
        if (context.Switches.Contains("set"))
            return QuotaRules.Set(context, db, UseQuotaNow, StartingQuotaNow);
        return QuotaRules.Show(context, db, UseQuotaNow, StartingQuotaNow);
    }
}

public class SquotaCommandHandler : ICommandHandler
{
    private readonly bool _useQuota;
    private readonly Func<bool>? _liveUseQuota;
    private readonly int _startingQuota;
    private readonly Func<int>? _liveStartingQuota;
    private bool UseQuotaNow => _liveUseQuota?.Invoke() ?? _useQuota;
    private int StartingQuotaNow => _liveStartingQuota?.Invoke() ?? _startingQuota;

    public SquotaCommandHandler(
        bool useQuota = true,
        int startingQuota = 20,
        Func<bool>? liveUseQuota = null,
        Func<int>? liveStartingQuota = null)
    {
        _useQuota = useQuota;
        _startingQuota = startingQuota;
        _liveUseQuota = liveUseQuota;
        _liveStartingQuota = liveStartingQuota;
    }

    public string Name => "@squota";

    public bool Execute(CommandContext context, ObjectDatabase db)
        => QuotaRules.Set(context, db, UseQuotaNow, StartingQuotaNow);
}

public class AllQuotaCommandHandler : ICommandHandler
{
    private readonly int _startingQuota;
    private readonly bool _useQuota;
    private readonly Func<bool>? _liveUseQuota;
    private readonly Func<int>? _liveStartingQuota;
    private bool UseQuotaNow => _liveUseQuota?.Invoke() ?? _useQuota;
    private int StartingQuotaNow => _liveStartingQuota?.Invoke() ?? _startingQuota;

    public AllQuotaCommandHandler(
        int startingQuota = 20,
        bool useQuota = true,
        Func<bool>? liveUseQuota = null,
        Func<int>? liveStartingQuota = null)
    {
        _startingQuota = startingQuota;
        _useQuota = useQuota;
        _liveUseQuota = liveUseQuota;
        _liveStartingQuota = liveStartingQuota;
    }

    public string Name => "@allquota";

    public bool Execute(CommandContext context, ObjectDatabase db)
        => QuotaRules.All(context, db, StartingQuotaNow, context.LeftArg, UseQuotaNow);
}

public static class QuotaRules
{
    private static readonly Dbref God = new(1);
    private static readonly object QuotaSync = new();

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
        if (IsUnlimited(who, db) || !useQuota)
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
        if (IsUnlimited(who, db) || !useQuota)
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

    public static bool All(CommandContext context, ObjectDatabase db, int startingQuota, string requested, bool useQuota = true)
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
        foreach (var who in db.ByType(ObjectType.Player))
        {
            int owned = Owned(db, who.Id);
            if (IsUnlimited(who, db) || !useQuota)
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
        if (token.Length == 0 || token.Equals("me", StringComparison.OrdinalIgnoreCase))
        {
            var account = ResolveAccount(db, actor);
            if (account == null)
            {
                who = actor;
                return false;
            }
            who = account;
            return true;
        }

        if (token.StartsWith('*') && token.Length > 1)
            token = token.Substring(1);
        var found = db.FindPlayer(token);
        if (found == null || found.Type != ObjectType.Player)
        {
            who = actor;
            return false;
        }
        who = found;
        return true;
    }

    public static MUSHObject? ResolveAccount(ObjectDatabase db, MUSHObject? obj)
    {
        var curr = obj;
        for (int hop = 0; hop < 8 && curr != null; hop++)
        {
            if (curr.Type == ObjectType.Player)
                return curr;
            if (!curr.Owner.IsValid || curr.Owner == curr.Id)
                return null;
            curr = db.GetObject(curr.Owner);
        }
        return null;
    }

    public static bool CanSee(MUSHObject actor, MUSHObject who)
        => actor.IsRoyalty
            || actor.HasPower("Quotas")
            || actor.HasFlag("QUOTAS")
            || actor.HasPower("See_All")
            || actor.IsWizard
            || who.Id == actor.Id
            || (actor.Type != ObjectType.Player && who.Id == actor.Owner);

    public static bool IsUnlimited(MUSHObject who, ObjectDatabase db)
    {
        if (who.IsRoyalty || who.HasPower("No_Quota") || who.HasFlag("NO_QUOTA"))
            return true;
        if (who.Type == ObjectType.Player)
            return false;
        if (who.HasFlag("MISTRUST"))
            return false;
        var owner = ResolveAccount(db, who);
        return owner != null && (owner.IsRoyalty || owner.HasPower("No_Quota") || owner.HasFlag("NO_QUOTA"));
    }

    public static int Owned(ObjectDatabase db, Dbref who)
    {
        int owned = 0;
        foreach (var obj in db.ByOwner(who))
        {
            if (obj.Id != who)
                owned++;
        }
        return owned;
    }

    public static int PeekRemaining(ObjectDatabase db, MUSHObject who, int startingQuota)
    {
        var entry = who.GetAttribute("RQUOTA", db);
        if (entry != null)
            return int.TryParse(entry.Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int stored) ? stored : 0;

        return Math.Max(0, startingQuota - Owned(db, who.Id));
    }

    public static int Remaining(ObjectDatabase db, MUSHObject who, int startingQuota)
    {
        lock (QuotaSync)
        {
            var entry = who.GetAttribute("RQUOTA");
            if (entry != null)
                return int.TryParse(entry.Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int stored) ? stored : 0;

            int owned = Owned(db, who.Id);
            int remaining = Math.Max(0, startingQuota - owned);
            Store(who, remaining);
            return remaining;
        }
    }

    public static bool TryPay(ObjectDatabase db, MUSHObject? actor, int cost, bool useQuota, int startingQuota)
    {
        if (cost <= 0 || !useQuota || actor == null || IsUnlimited(actor, db))
            return true;

        var account = ResolveAccount(db, actor);
        if (account == null)
            return true;

        lock (QuotaSync)
        {
            int remaining = Remaining(db, account, startingQuota);
            if (remaining < cost)
                return false;

            Store(account, remaining - cost);
            return true;
        }
    }

    public static void Refund(ObjectDatabase db, MUSHObject? owner, int amount, bool useQuota, int startingQuota)
    {
        if (amount <= 0 || !useQuota || owner == null)
            return;

        var account = ResolveAccount(db, owner);
        if (account == null || IsUnlimited(account, db))
            return;

        lock (QuotaSync)
        {
            int remaining = Remaining(db, account, startingQuota);
            Store(account, remaining + amount);
        }
    }

    public static bool TryTransfer(ObjectDatabase db, MUSHObject target, MUSHObject newOwner, bool allowOverdraft, bool useQuota, int startingQuota)
    {
        if (target.Owner == newOwner.Id)
            return true;

        if (!useQuota)
        {
            target.Owner = newOwner.Id;
            return true;
        }

        var oldAccount = ResolveAccount(db, db.GetObject(target.Owner));
        var newAccount = ResolveAccount(db, newOwner);
        if (oldAccount != null && newAccount != null && oldAccount.Id == newAccount.Id)
        {
            target.Owner = newOwner.Id;
            return true;
        }

        lock (QuotaSync)
        {
            bool oldLimited = oldAccount != null && !IsUnlimited(oldAccount, db);
            bool newLimited = newAccount != null && !IsUnlimited(newAccount, db);

            int oldRemaining = oldLimited ? Remaining(db, oldAccount!, startingQuota) : 0;
            int newRemaining = newLimited ? Remaining(db, newAccount!, startingQuota) : 0;

            if (newLimited && !allowOverdraft && newRemaining < 1)
                return false;

            if (oldLimited)
                Store(oldAccount!, oldRemaining + 1);
            if (newLimited)
                Store(newAccount!, newRemaining - 1);

            target.Owner = newOwner.Id;
            return true;
        }
    }

    public static void Adjust(ObjectDatabase db, MUSHObject who, int payment, int startingQuota)
    {
        lock (QuotaSync)
        {
            Store(who, Remaining(db, who, startingQuota) + payment);
        }
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
