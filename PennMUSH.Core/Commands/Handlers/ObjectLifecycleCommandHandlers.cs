namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Linq;
using System.Text;
using PennMUSH.Core;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Locks;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class DestroyCommandHandler : ICommandHandler
{
    private readonly Func<bool>? _liveUseQuota;
    private readonly Func<int>? _liveStartingQuota;
    private bool UseQuotaNow => _liveUseQuota?.Invoke() ?? true;
    private int StartingQuotaNow => _liveStartingQuota?.Invoke() ?? 20;

    public DestroyCommandHandler(Func<bool>? liveUseQuota = null, Func<int>? liveStartingQuota = null)
    {
        _liveUseQuota = liveUseQuota;
        _liveStartingQuota = liveStartingQuota;
    }

    public string Name => "@destroy";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify(Feedback.DestroyWhat);
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        var target = ResolveTarget(context.Arguments.Trim(), context.Executor, db);
        if (target == null)
        {
            context.Notify(Feedback.ICantFindThatObject);
            return false;
        }

        if (target.Id.Value <= 2)
        {
            context.Notify(Feedback.YouCannotDestroyFundamentalSystemObjects012);
            return false;
        }

        if (execObj == null || (!execObj.IsWizard && target.Owner != context.Executor))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        bool overrideSafe = string.Equals(context.CommandName, "@nuke", StringComparison.OrdinalIgnoreCase)
            || context.Switches.Contains("override");
        if (target.HasFlag("SAFE") && !overrideSafe)
        {
            context.Notify(Feedback.ThatObjectIsSetSafe);
            return false;
        }

        if (target.HasFlag("GOING"))
        {
            string name = target.Name;
            var id = target.Id;
            Free(db, target, UseQuotaNow, StartingQuotaNow);
            context.Notify(Feedback.HasBeenDestroyed, ("value", $"{name}"), ("value2", $"{id}"));
            return true;
        }

        target.AddFlag("GOING");
        context.Notify(Feedback.TheWreckingBallComesFor, ("value", $"{target.Name}"), ("value2", $"{target.Id}"));
        return true;
    }

    internal static void Free(ObjectDatabase db, MUSHObject target, bool useQuota = true, int startingQuota = 20)
    {
        if (target.Type == ObjectType.Garbage)
            return;
        if (target.Type != ObjectType.Player)
            QuotaRules.Refund(db, db.GetObject(target.Owner), 1, useQuota, startingQuota);
        if (target.Type == ObjectType.Exit)
        {
            if (target.Exits.IsValid)
                db.RemoveExit(target.Exits, target.Id);
        }
        else if (target.Location.IsValid)
        {
            db.MoveObject(target.Id, Dbref.Nothing);
        }

        target.RemoveFlag("GOING");
        db.DestroyObject(target.Id);
    }

    private static MUSHObject? ResolveTarget(string nameOrRef, Dbref executor, ObjectDatabase db)
    {
        if (nameOrRef.Equals("here", StringComparison.OrdinalIgnoreCase))
        {
            var exec = db.GetObject(executor);
            return exec != null && exec.Location.IsValid ? db.GetObject(exec.Location) : null;
        }
        if (nameOrRef.StartsWith('#') && int.TryParse(nameOrRef.Substring(1), out int id))
            return db.GetObject(new Dbref(id));
        return db.FindInContents(executor, nameOrRef) ?? db.FindObject(nameOrRef);
    }
}

public class PurgeCommandHandler : ICommandHandler
{
    private readonly Func<bool>? _liveUseQuota;
    private readonly Func<int>? _liveStartingQuota;
    private bool UseQuotaNow => _liveUseQuota?.Invoke() ?? true;
    private int StartingQuotaNow => _liveStartingQuota?.Invoke() ?? 20;

    public PurgeCommandHandler(Func<bool>? liveUseQuota = null, Func<int>? liveStartingQuota = null)
    {
        _liveUseQuota = liveUseQuota;
        _liveStartingQuota = liveStartingQuota;
    }

    public string Name => "@purge";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.IsWizard)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        int count = 0;
        foreach (var obj in db.GetAllObjects().ToList())
        {
            if (obj.Id.Value <= 2 || obj.Type == ObjectType.Garbage || !obj.HasFlag("GOING"))
                continue;
            DestroyCommandHandler.Free(db, obj, UseQuotaNow, StartingQuotaNow);
            count++;
        }

        if (count == 0)
            context.Notify(Feedback.NothingToPurge);
        else
            context.Notify(Feedback.PurgedObjects, ("count", $"{count}"), ("plural", count == 1 ? "" : "s"));
        return true;
    }
}

public class UndestroyCommandHandler : ICommandHandler
{
    public string Name => "@undestroy";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify(Feedback.UndestroyWhat);
            return false;
        }

        var actor = db.GetObject(context.Executor);
        var target = ResolveTarget(context.Arguments.Trim(), context.Executor, db);
        if (target == null)
        {
            context.Notify(Feedback.ICantFindThatObject);
            return false;
        }

        if (actor == null || (!actor.IsWizard && target.Owner != context.Executor))
        {
            context.Notify(Feedback.AlasYourEffortsOfMercyAreInVain);
            return false;
        }

        if (target.Type == ObjectType.Garbage || !target.RemoveFlag("GOING"))
        {
            context.Notify(Feedback.ThatCantBeUndestroyed);
            return false;
        }

        string spared = $"{target.Name} ({target.Id}) has been spared from destruction.";
        var owner = db.GetObject(target.Owner);
        if (owner != null && owner.Id != context.Executor)
        {
            context.NotifyTarget(owner.Id, Feedback.YourValue.Text(("value", $"{spared}")));
            context.Notify(Feedback.ValueSValue, ("value", $"{owner.Name}"), ("value2", $"{spared}"));
        }
        else
        {
            context.Notify(Feedback.YourValue, ("value", $"{spared}"));
        }

        return true;
    }

    private static MUSHObject? ResolveTarget(string nameOrRef, Dbref executor, ObjectDatabase db)
    {
        if (nameOrRef.Equals("here", StringComparison.OrdinalIgnoreCase))
        {
            var exec = db.GetObject(executor);
            return exec != null && exec.Location.IsValid ? db.GetObject(exec.Location) : null;
        }
        if (nameOrRef.StartsWith('#') && int.TryParse(nameOrRef.Substring(1), out int id))
            return db.GetObject(new Dbref(id));
        return db.FindInContents(executor, nameOrRef) ?? db.FindObject(nameOrRef);
    }
}

public class CloneCommandHandler : ICommandHandler
{
    private readonly Func<bool>? _liveUseQuota;
    private readonly Func<int>? _liveStartingQuota;
    private bool UseQuotaNow => _liveUseQuota?.Invoke() ?? true;
    private int StartingQuotaNow => _liveStartingQuota?.Invoke() ?? 20;

    public CloneCommandHandler(Func<bool>? liveUseQuota = null, Func<int>? liveStartingQuota = null)
    {
        _liveUseQuota = liveUseQuota;
        _liveStartingQuota = liveStartingQuota;
    }

    public string Name => "@clone";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify(Feedback.CloneWhat);
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        if (execObj == null || !execObj.Location.IsValid)
        {
            context.Notify(Feedback.YouAreNowhere);
            return false;
        }

        string targetSpec = string.IsNullOrWhiteSpace(context.LeftArg) ? context.Arguments.Trim() : context.LeftArg.Trim();
        var target = db.FindInContents(context.Executor, targetSpec) ??
                     db.FindInContents(execObj.Location, targetSpec) ??
                     db.MatchObject(context.Executor, targetSpec);

        if (target == null)
        {
            context.Notify(Feedback.ICantFindThatToClone);
            return false;
        }

        if (target.Type == ObjectType.Player)
        {
            context.Notify(Feedback.YouCannotClonePlayers);
            return false;
        }

        if (!QuotaRules.TryPay(db, execObj, 1, UseQuotaNow, StartingQuotaNow))
        {
            context.Notify(Feedback.SorryYourBuildingQuotaHasRunOut);
            return false;
        }

        string cloneName = string.IsNullOrWhiteSpace(context.RightArg) ? target.Name : context.RightArg.Trim();
        var clone = db.CreateObject(cloneName, target.Type, context.Executor);
        db.MoveObject(clone.Id, execObj.Location);
        clone.Home = target.Home.IsValid ? target.Home : execObj.Location;

        bool preserve = context.Switches.Contains("preserve") && execObj.IsWizard;
        // Copy flags (strip privileged flags unless /preserve by a wizard)
        foreach (var flag in target.Flags)
        {
            if (!preserve && (flag.Equals("WIZARD", StringComparison.OrdinalIgnoreCase) || flag.Equals("ROYALTY", StringComparison.OrdinalIgnoreCase)))
                continue;
            clone.AddFlag(flag);
        }

        // Copy attributes
        foreach (var attr in target.Attributes)
            clone.SetAttribute(attr.Name, attr.Value, context.Executor, attr.Flags);

        // Copy locks
        foreach (var l in target.Locks)
            clone.SetLock(l.Type, l.Key, context.Executor, l.Flags);

        context.Notify(Feedback.ClonedValueValue, ("value", $"{clone.Name}"), ("value2", $"{clone.Id}"));
        context.NotifyExcept(execObj.Location, context.Executor, Feedback.ValueCreatesValue.Text(("value", $"{execObj.Name}"), ("value2", $"{clone.Name}")));
        return true;
    }
}

public class ChownCommandHandler : ICommandHandler
{
    private readonly Func<bool>? _liveUseQuota;
    private readonly Func<int>? _liveStartingQuota;
    private bool UseQuotaNow => _liveUseQuota?.Invoke() ?? true;
    private int StartingQuotaNow => _liveStartingQuota?.Invoke() ?? 20;

    public ChownCommandHandler(Func<bool>? liveUseQuota = null, Func<int>? liveStartingQuota = null)
    {
        _liveUseQuota = liveUseQuota;
        _liveStartingQuota = liveStartingQuota;
    }

    public string Name => "@chown";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify(Feedback.UsageChownObjectPlayer);
            return false;
        }

        var execObj = db.GetObject(context.Executor);
        var target = db.MatchObject(context.Executor, context.LeftArg.Trim());
        if (target == null)
        {
            context.Notify(Feedback.ICantFindThatObject);
            return false;
        }

        string ownerSpec = context.RightArg.Trim();
        MUSHObject? newOwner;
        if (ownerSpec.Equals("me", StringComparison.OrdinalIgnoreCase))
        {
            newOwner = execObj != null && execObj.Type == ObjectType.Player ? execObj : null;
        }
        else
        {
            if (ownerSpec.StartsWith('*') && ownerSpec.Length > 1)
                ownerSpec = ownerSpec.Substring(1);
            newOwner = db.FindPlayer(ownerSpec);
        }

        if (newOwner == null)
        {
            context.Notify(Feedback.ICantFindThatPlayer);
            return false;
        }

        bool chownOkSelf = target.HasFlag("CHOWN_OK") && newOwner.Id == context.Executor;
        if (execObj == null || target.Type == ObjectType.Player || (!execObj.IsWizard && target.Owner != context.Executor && !chownOkSelf))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        if (!QuotaRules.TryTransfer(db, target, newOwner, allowOverdraft: execObj.IsWizard, UseQuotaNow, StartingQuotaNow))
        {
            context.Notify(newOwner.Id == context.Executor
                ? Feedback.SorryYourBuildingQuotaHasRunOut
                : Feedback.ThatPlayerDoesntHaveEnoughQuota);
            return false;
        }

        if (chownOkSelf)
            target.RemoveFlag("CHOWN_OK");

        context.Notify(Feedback.OwnerOfChangedTo, ("value", $"{target.Name}"), ("value2", $"{target.Id}"), ("value3", $"{newOwner.Name}"));
        return true;
    }
}

public class WipeCommandHandler : ICommandHandler
{
    public string Name => "@wipe";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify(Feedback.UsageWipeObjectPattern);
            return false;
        }

        string raw = context.Arguments.Trim();
        string objStr = raw;
        string pattern = "*";

        int slash = raw.IndexOf('/');
        if (slash != -1)
        {
            objStr = raw.Substring(0, slash).Trim();
            pattern = raw.Substring(slash + 1).Trim();
        }

        var target = db.MatchObject(context.Executor, objStr);
        if (target == null)
        {
            context.Notify(Feedback.ICantFindThatObject);
            return false;
        }

        var actor = db.GetObject(context.Executor);
        if (actor == null || (!actor.IsWizard && actor.Id != target.Owner))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        bool hasWildcard = pattern.Contains('*') || pattern.Contains('?');
        var toWipe = target.Attributes
            .Where(a => hasWildcard
                ? PatternMatcher.Match(pattern, a.Name, out _)
                : (slash == -1
                    || a.Name.Equals(pattern, StringComparison.OrdinalIgnoreCase)
                    || a.Name.StartsWith(pattern + "`", StringComparison.OrdinalIgnoreCase)))
            .Select(a => a.Name)
            .ToList();

        foreach (var attr in toWipe)
        {
            target.ClearAttribute(attr);
        }

        context.Notify($"{toWipe.Count} attributes wiped.");
        return true;
    }
}

public class LockCommandHandler : ICommandHandler
{
    public string Name => "@lock";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify(Feedback.UsageLockTypeObjectKey);
            return false;
        }

        var target = db.MatchObject(context.Executor, context.LeftArg.Trim());
        if (target == null)
        {
            context.Notify(Feedback.ICantFindThatObject);
            return false;
        }

        string lockType = LockCommands.LockCommandType(context);
        var lockNode = LockParser.Parse(context.RightArg.Trim());

        target.SetLock(lockType, lockNode, context.Executor);
        context.Notify(Feedback.LockedOn, ("value", $"{lockType}"), ("value2", $"{target.Name}"));
        return true;
    }
}

public class UnlockCommandHandler : ICommandHandler
{
    public string Name => "@unlock";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            context.Notify(Feedback.UsageUnlockTypeObject);
            return false;
        }

        var target = db.MatchObject(context.Executor, context.Arguments.Trim());
        if (target == null)
        {
            context.Notify(Feedback.ICantFindThatObject);
            return false;
        }

        string lockType = LockCommands.LockCommandType(context);
        if (target.ClearLock(lockType))
        {
            context.Notify(Feedback.UnlockedOn, ("value", $"{lockType}"), ("value2", $"{target.Name}"));
            return true;
        }

        context.Notify(Feedback.NoSuchLockSet, ("lock", $"{lockType}"));
        return false;
    }
}

public class StatsCommandHandler : ICommandHandler
{
    public string Name => "@stats";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (context.Switches.Contains("flags"))
        {
            context.Notify(FlagReport(db));
            return true;
        }

        if (context.Switches.Contains("tables") || context.Switches.Contains("chunks")
            || context.Switches.Contains("regions") || context.Switches.Contains("paging")
            || context.Switches.Contains("freespace"))
        {
            var actor = db.GetObject(context.Executor);
            if (actor == null || !actor.IsWizard)
            {
                context.Notify(Feedback.PermissionDenied);
                return false;
            }

            context.Notify(MemoryReport(context.Switches));
            return true;
        }

        int players = 0, rooms = 0, exits = 0, things = 0, garbage = 0;
        foreach (var obj in db.GetAllObjects())
        {
            if (obj.HasFlag("GOING")) { garbage++; continue; }
            switch (obj.Type)
            {
                case ObjectType.Player: players++; break;
                case ObjectType.Room: rooms++; break;
                case ObjectType.Exit: exits++; break;
                case ObjectType.Thing: things++; break;
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Total objects: {db.Count}");
        sb.AppendLine($"  Players: {players,-6} Rooms: {rooms,-6} Exits: {exits,-6} Things: {things,-6} Garbage: {garbage}");
        context.Notify(sb.ToString().TrimEnd());
        return true;
    }

    private static string FlagReport(ObjectDatabase db)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var obj in db.GetAllObjects())
        {
            foreach (string flag in obj.Flags)
            {
                string display = PennMUSH.Core.Flags.FlagTable.DisplayName(flag);
                counts[display] = counts.TryGetValue(display, out int count) ? count + 1 : 1;
            }
        }

        if (counts.Count == 0)
            return "Flags: (none)";
        return "Flags:\n" + string.Join("\n", counts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => $"{pair.Key}: {pair.Value}"));
    }

    private static string MemoryReport(IReadOnlySet<string> switches)
    {
        var info = GC.GetGCMemoryInfo();
        long heap = GC.GetTotalMemory(false);
        if (switches.Contains("freespace"))
            return $"Freespace: {info.TotalAvailableMemoryBytes} bytes available";
        if (switches.Contains("paging"))
        {
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            return $"Paging: working set {process.WorkingSet64} bytes, paged {process.PagedMemorySize64} bytes";
        }
        if (switches.Contains("regions") || (switches.Contains("chunks") && switches.Contains("regions")))
        {
            return string.Join("\n", new[]
            {
                "Regions:",
                $"Gen0 collections: {GC.CollectionCount(0)}",
                $"Gen1 collections: {GC.CollectionCount(1)}",
                $"Gen2 collections: {GC.CollectionCount(2)}",
                $"Heap: {heap} bytes"
            });
        }
        if (switches.Contains("chunks"))
            return $"Chunks: heap {heap} bytes, fragmentable {info.FragmentedBytes} bytes";
        return string.Join("\n", new[]
        {
            "Tables:",
            $"Heap: {heap} bytes",
            $"High memory load threshold: {info.HighMemoryLoadThresholdBytes} bytes",
            $"Total available: {info.TotalAvailableMemoryBytes} bytes"
        });
    }
}

public class ScoreCommandHandler : ICommandHandler
{
    public string Name => "score";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var execObj = db.GetObject(context.Executor);
        if (execObj == null) return false;

        context.Notify($"You have {execObj.Pennies} {db.FormatMoneyUnit(execObj.Pennies)}.");
        return true;
    }
}

public class DoingCommandHandler : ICommandHandler
{
    public string Name => "doing";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var execObj = db.GetObject(context.Executor);
        if (execObj == null) return false;

        if (string.IsNullOrWhiteSpace(context.Arguments))
        {
            var doing = execObj.GetAttribute("DOING");
            if (doing != null)
                context.Notify(Feedback.YourDoingIs, ("value", doing.Value));
            else
                context.Notify(Feedback.YouHaveNoDoingSet);
            return true;
        }

        execObj.SetAttribute("DOING", context.Arguments.Trim(), context.Executor);
        context.Notify(Feedback.DoingSetTo, ("value", $"{context.Arguments.Trim()}"));
        return true;
    }
}

public class VersionCommandHandler : ICommandHandler
{
    public string Name => "@version";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        context.Notify(Feedback.PennMUSHNetVersion, ("version", ServerVersion.Text));
        context.Notify(Feedback.ModernVirtualWorldEngineWithC3DagInheritanceAsyncSoftcodeAndCronScheduler);
        context.Notify(Feedback.FeatureParityReimplementationOfPennMUSH18);
        return true;
    }
}

internal static class LockCommands
{
    public static string LockCommandType(CommandContext context)
    {
        if (context.CommandName.Equals("@elock", StringComparison.OrdinalIgnoreCase)
            || context.CommandName.Equals("@eunlock", StringComparison.OrdinalIgnoreCase))
            return LockType.Enter;
        if (context.CommandName.Equals("@ulock", StringComparison.OrdinalIgnoreCase)
            || context.CommandName.Equals("@uunlock", StringComparison.OrdinalIgnoreCase))
            return LockType.Use;
        return context.Switches.FirstOrDefault() ?? LockType.Basic;
    }
}
