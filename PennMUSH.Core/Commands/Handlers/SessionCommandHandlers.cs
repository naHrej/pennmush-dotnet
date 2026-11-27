namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Session;
using PennMUSH.Core.Types;
using PennMUSH.Core.MUSHcode;

public class WhoCommandHandler : ICommandHandler
{
    private readonly ISessionDirectory _sessions;
    private readonly Func<DateTimeOffset> _now;
    private readonly ExpressionEvaluator? _evaluator;

    public string Name => "who";

    public WhoCommandHandler(ISessionDirectory sessions, Func<DateTimeOffset>? now = null, ExpressionEvaluator? evaluator = null)
    {
        _sessions = sessions;
        _now = now ?? (() => DateTimeOffset.UtcNow);
        _evaluator = evaluator;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        bool session = context.CommandName.Equals("session", StringComparison.OrdinalIgnoreCase);
        var lines = SessionReport.Lines(db, _sessions, actor, session, context.Arguments, _now(), _evaluator).ToList();
        if (lines.Count > 0)
            context.NotifyBlock(string.Join("\n", lines));
        return true;
    }
}

public class HideCommandHandler : ICommandHandler
{
    private readonly ISessionDirectory _sessions;

    public string Name => "@hide";

    public HideCommandHandler(ISessionDirectory sessions) => _sessions = sessions;

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;
        if (!SessionAccess.CanHide(actor))
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        bool? forced = HideChoice(context);
        string target = context.Arguments.Trim();
        if (int.TryParse(target, out int descriptor))
            return HideDescriptor(context, actor, descriptor, forced);

        var player = string.IsNullOrEmpty(target)
            ? actor
            : SessionAccess.FindPlayer(db, context.Executor, target);
        if (player == null || player.Type != ObjectType.Player)
        {
            context.Notify(Feedback.ICantFindThatPlayer);
            return false;
        }
        if (player.Id != actor.Id && !actor.IsWizard)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        var sessions = _sessions.List().Where(session => session.LoggedIn && session.PlayerId == player.Id).ToList();
        if (sessions.Count == 0)
        {
            context.Notify(Feedback.ThatPlayerIsNotOnline);
            return false;
        }

        bool hide = forced ?? sessions.Any(session => !session.Hidden);
        foreach (var session in sessions)
            _sessions.SetHidden(session.Descriptor, hide);
        context.Notify(HideMessage(actor, player, hide));
        return true;
    }

    private bool HideDescriptor(CommandContext context, MUSHObject actor, int descriptor, bool? forced)
    {
        var session = _sessions.List().FirstOrDefault(item => item.Descriptor == descriptor);
        if (session == null)
        {
            context.Notify(actor.IsWizard || actor.HasPower("See_All")
                ? Feedback.CouldntFindThatDescriptor
                : Feedback.PermissionDenied);
            return false;
        }
        if (!session.LoggedIn)
        {
            context.Notify(Feedback.NooneIsConnectedToThatDescriptor);
            return false;
        }
        if (session.PlayerId != actor.Id && !actor.IsWizard)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        bool hide = forced ?? !session.Hidden;
        _sessions.SetHidden(descriptor, hide);
        context.Notify(hide ? Feedback.ConnectionHidden : Feedback.ConnectionUnhidden);
        return true;
    }

    private static bool? HideChoice(CommandContext context)
    {
        if (context.Switches.Contains("on") || context.Switches.Contains("yes"))
            return true;
        if (context.Switches.Contains("off") || context.Switches.Contains("no"))
            return false;
        return null;
    }

    private static string HideMessage(MUSHObject actor, MUSHObject player, bool hide)
    {
        if (player.Id == actor.Id)
            return hide ? "You no longer appear on the WHO list." : "You now appear on the WHO list.";
        return hide
            ? $"{player.Name} no longer appears on the WHO list."
            : $"{player.Name} now appears on the WHO list.";
    }
}

public class BootCommandHandler : ICommandHandler
{
    private readonly ISessionDirectory _sessions;
    private readonly Func<DateTimeOffset> _now;

    public string Name => "@boot";

    public BootCommandHandler(ISessionDirectory sessions, Func<DateTimeOffset>? now = null)
    {
        _sessions = sessions;
        _now = now ?? (() => DateTimeOffset.UtcNow);
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        if (context.Switches.Contains("port"))
            return BootPort(context, db, actor);

        bool self = context.Switches.Contains("me") || context.Arguments.Trim().Equals("me", StringComparison.OrdinalIgnoreCase);
        MUSHObject? victim = actor;
        if (!self)
        {
            victim = SessionAccess.FindPlayer(db, context.Executor, context.Arguments.Trim());
            if (victim == null || victim.Type != ObjectType.Player)
            {
                context.Notify(Feedback.NoSuchConnectedPlayer);
                return false;
            }
            if (victim.Id == actor.Id)
                self = true;
        }

        if (victim!.IsGod && !actor.IsGod)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }
        if (!self && !SessionAccess.CanBoot(actor))
        {
            context.Notify(Feedback.YouCantBootOtherPeople);
            return false;
        }

        var sessions = _sessions.List().Where(session => session.LoggedIn && session.PlayerId == victim.Id).ToList();
        if (self)
        {
            var kept = sessions.OrderByDescending(session => session.LastActivity).FirstOrDefault();
            sessions = sessions
                .Where(session => kept == null || session.Descriptor != kept.Descriptor)
                .Where(session => _now() - session.LastActivity > TimeSpan.FromSeconds(60))
                .ToList();
        }

        if (sessions.Count == 0)
        {
            context.Notify(self
                ? "None of your connections are idle. If you want to quit, use QUIT."
                : "That player is not online.");
            return false;
        }

        if (!self && !context.Switches.Contains("silent"))
            context.NotifyTarget(victim.Id, Feedback.YouArePolitelyShownToTheDoor.Text());
        foreach (var session in sessions)
            _sessions.Disconnect(session.Descriptor);

        if (self)
        {
            context.Notify(sessions.Count == 1
                ? "You boot an idle self."
                : $"You boot {sessions.Count} idle selves.");
        }
        else
        {
            context.Notify(Feedback.YouBootedOff, ("value", $"{victim.Name}"));
        }
        return true;
    }

    private bool BootPort(CommandContext context, ObjectDatabase db, MUSHObject actor)
    {
        if (!int.TryParse(context.Arguments.Trim(), out int descriptor))
        {
            context.Notify(Feedback.InvalidPort);
            return false;
        }

        var session = _sessions.List().FirstOrDefault(item => item.Descriptor == descriptor);
        if (session == null || (!SessionAccess.CanBoot(actor) && (!session.LoggedIn || session.PlayerId != actor.Id)))
        {
            context.Notify(SessionAccess.CanBoot(actor)
                ? "There is noone connected on that descriptor."
                : "You can't boot other people!");
            return false;
        }
        if (_sessions.SourceDescriptor == descriptor)
        {
            context.Notify(Feedback.IfYouWantToQuitUseQuit);
            return false;
        }

        var victim = session.LoggedIn ? db.GetObject(session.PlayerId) : null;
        if (victim != null && victim.IsGod && !actor.IsGod)
        {
            context.Notify(Feedback.PermissionDenied);
            return false;
        }

        if (victim != null)
        {
            if (!context.Switches.Contains("silent"))
                context.NotifyTarget(victim.Id, Feedback.YouArePolitelyShownToTheDoor.Text());
            context.Notify(victim.Id == actor.Id
                ? "You boot a duplicate self."
                : $"You booted {victim.Name} off!");
        }
        else
        {
            context.Notify(Feedback.YouBootedUnconnectedPort, ("value", $"{descriptor}"));
        }

        _sessions.Disconnect(descriptor);
        return true;
    }
}

public class PollCommandHandler : ICommandHandler
{
    public const int MaxLength = 40;
    private readonly ISessionDirectory _sessions;

    public string Name => "@poll";

    public PollCommandHandler(ISessionDirectory sessions) => _sessions = sessions;

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
            return false;

        bool clear = context.Switches.Contains("clear");
        if (string.IsNullOrWhiteSpace(context.Arguments) && !clear)
        {
            context.Notify(Feedback.TheCurrentPollIs, ("value", $"{_sessions.Poll}"));
            return true;
        }
        if (!SessionAccess.CanChangePoll(actor))
        {
            context.Notify(Feedback.WhoDoYouThinkYouAreGallup);
            return false;
        }
        if (clear)
        {
            _sessions.Poll = "Doing";
            context.Notify(Feedback.PollReset);
            return true;
        }

        string cleaned = SessionReport.Clean(context.Arguments.Trim());
        int lost = Math.Max(0, cleaned.Length - MaxLength);
        if (lost > 0)
            cleaned = cleaned[..MaxLength];
        _sessions.Poll = cleaned;
        context.Notify(lost > 0
            ? $"Poll set to '{cleaned}'. {lost} characters lost."
            : $"Poll set to: {cleaned}");
        return true;
    }
}

internal static class SessionAccess
{
    public static bool SeesConnections(MUSHObject actor) =>
        actor.IsRoyalty || actor.HasPower("See_All");

    public static bool CanHide(MUSHObject actor) =>
        actor.IsRoyalty || actor.HasPower("Hide");

    public static bool CanBoot(MUSHObject actor) =>
        actor.IsRoyalty || actor.HasPower("Boot");

    public static bool CanChangePoll(MUSHObject actor) =>
        actor.IsWizard || actor.HasPower("Poll");

    public static MUSHObject? FindPlayer(ObjectDatabase db, Dbref actor, string name)
    {
        if (name.Equals("me", StringComparison.OrdinalIgnoreCase))
            return db.GetObject(actor);
        return db.FindPlayer(name);
    }
}

internal static class SessionReport
{
    public static IEnumerable<string> Lines(
        ObjectDatabase db,
        ISessionDirectory sessions,
        MUSHObject actor,
        bool sessionCommand,
        string pattern,
        DateTimeOffset now,
        ExpressionEvaluator? evaluator = null)
    {
        bool privileged = SessionAccess.SeesConnections(actor);
        var all = sessions.List().OrderBy(item => item.Descriptor).ToList();
        int count = all.Count(item => item.LoggedIn && (privileged || !item.Hidden));
        string filter = pattern.Trim();
        bool mogrify = !sessionCommand;
        string sentence = count switch
        {
            0 => "There are no players connected.",
            1 => "There is one player connected.",
            _ => $"There are {count} players connected."
        };

        if (!privileged)
        {
            foreach (var line in Section(db, evaluator, actor, mogrify, "WHOHEADER",
                [Header(sessions.Poll)],
                sessions.Poll, count.ToString()))
                yield return line;
            foreach (var item in all)
            {
                if (!item.LoggedIn || item.Hidden || !Matches(db, item, filter))
                    continue;
                var player = db.GetObject(item.PlayerId);
                foreach (var line in Section(db, evaluator, actor, mogrify, "WHOROW",
                    [MortalRow(db, item, now)],
                    player?.Name ?? "Unknown",
                    PlayerRef(db, item),
                    Elapsed(now - item.ConnectedAt),
                    Elapsed(now - item.LastActivity),
                    Doing(player),
                    PublicMark(db, item),
                    Seconds(now - item.ConnectedAt),
                    Seconds(now - item.LastActivity)))
                    yield return line;
            }
        }
        else if (sessionCommand)
        {
            yield return sessions.Poll;
            yield return $"{"Player Name",-16} {"Loc #",6} {"On For",9} {"Idle",5} {"Cmds",5} {"Des",4} {"Sent",7} {"Recv",7} {"Pend",7}";
            foreach (var item in all)
            {
                if (!Matches(db, item, filter))
                    continue;
                yield return SessionRow(db, item, now);
            }
        }
        else
        {
            foreach (var line in Section(db, evaluator, actor, mogrify, "WHOAHEADER",
                [sessions.Poll, $"{"Player Name",-16} {"Loc #",6} {"On For",9} {"Idle",5} {"Cmds",5} {"Des",4} Host"],
                sessions.Poll, count.ToString(), all.Count.ToString()))
                yield return line;
            foreach (var item in all)
            {
                if (!Matches(db, item, filter))
                    continue;
                foreach (var line in Section(db, evaluator, actor, mogrify, "WHOAROW",
                    [AdminRow(db, item, now)],
                    RowName(db, item),
                    PlayerRef(db, item),
                    Place(db, item),
                    Elapsed(now - item.ConnectedAt),
                    Elapsed(now - item.LastActivity),
                    item.Commands.ToString(),
                    item.Descriptor.ToString(),
                    item.Host,
                    AdminStatus(db, item),
                    Seconds(now - item.ConnectedAt)))
                    yield return line;
            }
        }

        string footer = privileged ? "WHOAFOOTER" : "WHOFOOTER";
        foreach (var line in Section(db, evaluator, actor, mogrify, footer, [sentence], count.ToString(), sentence))
            yield return line;
    }

    private static IEnumerable<string> Section(
        ObjectDatabase db,
        ExpressionEvaluator? evaluator,
        MUSHObject actor,
        bool mogrify,
        string attribute,
        string[] fallback,
        params string[] args)
    {
        if (!mogrify || !db.WhoMogrifierId.IsValid)
            return fallback;

        var mogrifier = db.GetObject(db.WhoMogrifierId);
        if (mogrifier == null)
            return fallback;

        string? custom = FormatAttributes.Evaluate(
            db, evaluator, mogrifier, actor.Id, actor.Id, attribute, args);
        if (custom == null)
            return fallback;
        if (string.IsNullOrWhiteSpace(custom))
            return [];

        return custom.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').Where(line => line.Length > 0);
    }

    public static string Clean(string text)
    {
        var chars = text.Select(ch => ch is '\r' or '\n' or '\t' ? ' ' : ch).ToArray();
        return new string(chars);
    }

    private static string Header(string poll) =>
        $"{"Player Name",-16} {"On For",10} {"Idle",6}  {poll}";

    private static string MortalRow(ObjectDatabase db, SessionInfo item, DateTimeOffset now)
    {
        var player = db.GetObject(item.PlayerId);
        string name = player?.Name ?? "Unknown";
        char mark = player != null && player.HasFlag("DARK") ? 'D' : item.Hidden ? 'H' : ' ';
        string doing = Doing(player);
        return $"{name,-16} {Elapsed(now - item.ConnectedAt),10} {Elapsed(now - item.LastActivity),6} {mark} {doing}";
    }

    private static string AdminRow(ObjectDatabase db, SessionInfo item, DateTimeOffset now)
    {
        string name = RowName(db, item);
        string host = HostNote(db, item);
        return $"{name,-16} {Place(db, item),6} {Elapsed(now - item.ConnectedAt),9} {Elapsed(now - item.LastActivity),5} {item.Commands,5} {item.Descriptor,4} {host}";
    }

    private static string SessionRow(ObjectDatabase db, SessionInfo item, DateTimeOffset now)
    {
        string name = RowName(db, item);
        return $"{name,-16} {Place(db, item),6} {Elapsed(now - item.ConnectedAt),9} {Elapsed(now - item.LastActivity),5} {item.Commands,5} {item.Descriptor,4} {item.BytesSent,7} {item.BytesReceived,7} {item.Pending,7}";
    }

    private static string RowName(ObjectDatabase db, SessionInfo item)
    {
        if (!item.LoggedIn)
            return Feedback.Connecting.Text();
        return db.GetObject(item.PlayerId)?.Name ?? "Unknown";
    }

    private static string Place(ObjectDatabase db, SessionInfo item)
    {
        if (!item.LoggedIn)
            return MushError.Nothing.Text();
        var player = db.GetObject(item.PlayerId);
        return player != null && player.Location.IsValid ? "#" + player.Location.Value : MushError.Nothing.Text();
    }

    private static string HostNote(ObjectDatabase db, SessionInfo item)
    {
        if (!item.LoggedIn)
            return item.Host;
        var player = db.GetObject(item.PlayerId);
        if (player != null && player.HasFlag("DARK"))
            return item.Host + " (Dark)";
        if (item.Hidden)
            return item.Host + " (Hide)";
        return item.Host;
    }

    private static string Doing(MUSHObject? player)
    {
        string value = player?.GetAttribute("DOING")?.Value ?? "";
        return Clean(value);
    }

    private static string PlayerRef(ObjectDatabase db, SessionInfo item)
    {
        if (!item.LoggedIn)
            return MushError.Nothing.Text();
        return db.GetObject(item.PlayerId)?.Id.ToString() ?? MushError.Nothing.Text();
    }

    private static string PublicMark(ObjectDatabase db, SessionInfo item)
    {
        var player = db.GetObject(item.PlayerId);
        if (player != null && player.HasFlag("DARK"))
            return "D";
        if (item.Hidden)
            return "H";
        return string.Empty;
    }

    private static string AdminStatus(ObjectDatabase db, SessionInfo item)
    {
        if (!item.LoggedIn)
            return "Connecting";
        var player = db.GetObject(item.PlayerId);
        if (player != null && player.HasFlag("DARK"))
            return "Dark";
        if (item.Hidden)
            return "Hide";
        return string.Empty;
    }

    private static string Seconds(TimeSpan span)
    {
        if (span < TimeSpan.Zero)
            span = TimeSpan.Zero;
        return ((long)span.TotalSeconds).ToString();
    }

    private static string Elapsed(TimeSpan span)
    {
        if (span < TimeSpan.Zero)
            span = TimeSpan.Zero;
        if (span.TotalDays >= 1)
            return $"{(int)span.TotalDays}d {span.Hours:00}h";
        if (span.TotalHours >= 1)
            return $"{(int)span.TotalHours}h {span.Minutes:00}m";
        if (span.TotalMinutes >= 1)
            return $"{(int)span.TotalMinutes}m {span.Seconds:00}s";
        return $"{span.Seconds}s";
    }

    private static bool Matches(ObjectDatabase db, SessionInfo item, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return true;
        if (!item.LoggedIn)
            return false;
        var player = db.GetObject(item.PlayerId);
        if (player == null)
            return false;
        if (pattern.Contains('*') || pattern.Contains('?'))
        {
            if (Wild(pattern, player.Name))
                return true;
            string alias = player.GetAttribute("ALIAS")?.Value ?? "";
            return alias.Split(';').Any(part => Wild(pattern, part.Trim()));
        }
        return player.Name.StartsWith(pattern, StringComparison.OrdinalIgnoreCase);
    }

    private static bool Wild(string pattern, string text)
    {
        string body = Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".");
        return Regex.IsMatch(text, "^" + body + "$", RegexOptions.IgnoreCase);
    }
}
