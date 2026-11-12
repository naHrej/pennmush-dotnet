namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using System.Linq;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Flags;
using PennMUSH.Core.MUSHcode;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

internal static class ListenerSearch
{
    public static bool IsCommand(string? value) => TryParse(value, out _, out _);

    public static bool TryParse(string? value, out string pattern, out string action)
    {
        pattern = string.Empty;
        action = string.Empty;
        if (string.IsNullOrEmpty(value) || !value.StartsWith('$'))
            return false;
        int colon = value.IndexOf(':');
        if (colon <= 1)
            return false;
        pattern = value.Substring(1, colon - 1).Trim();
        action = value.Substring(colon + 1).Trim();
        return pattern.Length > 0;
    }

    public static List<string> MatchingAttributes(MUSHObject obj, string input)
    {
        var names = new List<string>();
        foreach (var attr in obj.Attributes)
        {
            if (TryParse(attr.Value, out string pattern, out _) && PatternMatcher.Match(pattern, input, out _))
                names.Add(attr.Name);
        }
        return names;
    }

    public static bool TryFirst(MUSHObject obj, string input, out string action, out string[] captures)
    {
        foreach (var attr in obj.Attributes)
        {
            if (TryParse(attr.Value, out string pattern, out action) && PatternMatcher.Match(pattern, input, out captures))
                return true;
        }
        action = string.Empty;
        captures = Array.Empty<string>();
        return false;
    }

    public static bool CanExamine(MUSHObject actor, MUSHObject target) =>
        actor.IsWizard || actor.Id == target.Id || target.Owner == actor.Id || target.HasFlag("VISUAL");

    public static bool Nearby(MUSHObject actor, MUSHObject target)
    {
        if (actor.Id == target.Id)
            return true;
        if (actor.Location.IsValid && actor.Location == target.Id)
            return true;
        if (target.Location.IsValid && target.Location == actor.Id)
            return true;
        return actor.Location.IsValid && actor.Location == target.Location;
    }

    public static bool Controls(MUSHObject actor, MUSHObject target) =>
        actor.IsWizard || actor.Id == target.Id || target.Owner == actor.Id || actor.HasPower("Long_Fingers");
}

public class ScanCommandHandler : ICommandHandler
{
    private readonly Dbref _masterRoom;

    public string Name => "@scan";

    public ScanCommandHandler(int masterRoom = 2)
    {
        _masterRoom = new Dbref(masterRoom);
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.Location.IsValid)
        {
            context.Notify("Sorry, you are in an invalid location.");
            return false;
        }

        string command = (context.Arguments ?? string.Empty).Trim();
        if (command.Length == 0)
        {
            context.Notify("What command do you want to scan for?");
            return false;
        }

        bool anySwitch = context.Switches.Contains("room")
            || context.Switches.Contains("self")
            || context.Switches.Contains("zone")
            || context.Switches.Contains("globals");
        bool room = !anySwitch || context.Switches.Contains("room");
        bool self = !anySwitch || context.Switches.Contains("self");
        bool zone = !anySwitch || context.Switches.Contains("zone");
        bool globals = !anySwitch || context.Switches.Contains("globals");

        if (room)
        {
            context.Notify("Matches on contents of this room:");
            foreach (var item in db.GetContents(actor.Location))
                Report(context, actor, item, command);
            var here = db.GetObject(actor.Location);
            if (here != null && ListenerSearch.MatchingAttributes(here, command).Count > 0 && ListenerSearch.CanExamine(actor, here))
            {
                var names = ListenerSearch.MatchingAttributes(here, command);
                context.Notify($"Matched here: {Label(here)}  [{names.Count}:{FormatNames(names)}]");
            }
        }

        if (self)
        {
            context.Notify("Matches on carried objects:");
            foreach (var item in db.GetContents(actor.Id))
                Report(context, actor, item, command);
            var own = ListenerSearch.MatchingAttributes(actor, command);
            if (own.Count > 0)
                context.Notify($"Matched self: {Label(actor)}  [{own.Count}:{FormatNames(own)}]");
        }

        if (zone)
            ReportZone(context, db, actor, command);

        if (globals && actor.Location != _masterRoom && actor.Zone != _masterRoom && LocationZone(db, actor) != _masterRoom)
        {
            context.Notify("Matches on objects in the Master Room:");
            foreach (var item in db.GetContents(_masterRoom))
                Report(context, actor, item, command);
        }

        return true;
    }

    private static void ReportZone(CommandContext context, ObjectDatabase db, MUSHObject actor, string command)
    {
        var locationZone = LocationZone(db, actor);
        if (locationZone.IsValid)
            ReportOneZone(context, db, actor, command, locationZone, personal: false);
        if (actor.Zone.IsValid && actor.Zone != locationZone)
            ReportOneZone(context, db, actor, command, actor.Zone, personal: true);
    }

    private static void ReportOneZone(CommandContext context, ObjectDatabase db, MUSHObject actor, string command, Dbref zoneId, bool personal)
    {
        var zone = db.GetObject(zoneId);
        if (zone == null || (zone.Type == ObjectType.Room && actor.Location == zone.Id))
            return;
        if (zone.Type == ObjectType.Room)
        {
            context.Notify(personal
                ? "Matches on personal zone master room:"
                : "Matches on zone master room of location:");
            foreach (var item in db.GetContents(zone.Id))
                Report(context, actor, item, command);
            return;
        }

        var names = ListenerSearch.MatchingAttributes(zone, command);
        if (names.Count == 0 || !ListenerSearch.CanExamine(actor, zone))
            return;
        context.Notify(personal
            ? $"Matched personal zone: {Label(zone)}  [{names.Count}:{FormatNames(names)}]"
            : $"Matched zone of location: {Label(zone)}  [{names.Count}:{FormatNames(names)}]");
    }

    private static Dbref LocationZone(ObjectDatabase db, MUSHObject actor)
    {
        var room = db.GetObject(actor.Location);
        return room?.Zone ?? Dbref.Nothing;
    }

    private static void Report(CommandContext context, MUSHObject actor, MUSHObject target, string command)
    {
        if (!ListenerSearch.CanExamine(actor, target))
            return;
        var names = ListenerSearch.MatchingAttributes(target, command);
        if (names.Count == 0)
            return;
        context.Notify($"{Label(target)}  [{names.Count}:{FormatNames(names)}]");
    }

    private static string FormatNames(List<string> names) => " " + string.Join(" ", names);

    private static string Label(MUSHObject obj) => $"{obj.Name}(#{obj.Id.Value})";
}

public class SweepCommandHandler : ICommandHandler
{
    public string Name => "@sweep";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !actor.Location.IsValid || db.GetObject(actor.Location) == null)
        {
            context.Notify("Sorry, you are in an invalid location.");
            return false;
        }

        if (!TryMode(context, out string mode))
        {
            context.Notify("Invalid parameter.");
            return false;
        }

        bool connected = mode == "connected";
        bool here = mode == "here";
        bool inventory = mode == "inventory";
        bool exits = mode == "exits";
        var room = db.GetObject(actor.Location)!;

        if (!inventory && !exits)
        {
            context.Notify("Listening in ROOM:");
            ReportPlace(context, db, room, connected, thisRoom: true);
            foreach (var item in db.GetContents(room.Id))
                ReportPlace(context, db, item, connected, thisRoom: false);
        }

        if (!connected && !inventory && room.Type == ObjectType.Room)
        {
            context.Notify("Listening EXITS:");
            if (room.HasFlag("AUDIBLE"))
            {
                foreach (var exit in db.GetExits(room.Id))
                {
                    if (exit.HasFlag("AUDIBLE"))
                        context.Notify($"{exit.Name} [broadcasting].");
                }
            }
        }

        if (!here && !exits)
        {
            context.Notify("Listening in your INVENTORY:");
            foreach (var item in db.GetContents(actor.Id))
                ReportPlace(context, db, item, connected, thisRoom: false);
        }

        return true;
    }

    private static bool TryMode(CommandContext context, out string mode)
    {
        foreach (var name in new[] { "connected", "here", "inventory", "exits" })
        {
            if (context.Switches.Contains(name))
            {
                mode = name;
                return true;
            }
        }

        string word = (context.Arguments ?? string.Empty).Trim();
        if (word.Length == 0)
        {
            mode = string.Empty;
            return true;
        }

        foreach (var name in new[] { "connected", "here", "inventory", "exits" })
        {
            if (word.StartsWith(name, StringComparison.OrdinalIgnoreCase))
            {
                mode = name;
                return true;
            }
        }

        mode = string.Empty;
        return false;
    }

    private static void ReportPlace(CommandContext context, ObjectDatabase db, MUSHObject obj, bool connectedOnly, bool thisRoom)
    {
        if (connectedOnly)
        {
            if (!ConnectedListener(db, obj))
                return;
            if (obj.Type == ObjectType.Player)
                context.Notify($"{obj.Name} is listening.");
            else
            {
                var owner = db.GetObject(obj.Owner);
                context.Notify($"{obj.Name} [owner: {owner?.Name ?? "???"}] is listening.");
            }
            return;
        }

        bool hears = obj.Type == ObjectType.Player || obj.HasFlag("PUPPET");
        if (hears)
        {
            string where = thisRoom ? " (this room)" : string.Empty;
            string online = db.IsConnected(obj.Id) ? " (connected)" : string.Empty;
            context.Notify($"{obj.Name}{where} [speech].{online}");
        }
        if (obj.Attributes.Any(attr => ListenerSearch.IsCommand(attr.Value)))
            context.Notify($"{obj.Name}{(thisRoom ? " (this room)" : string.Empty)} [commands].");
        if (obj.HasFlag("AUDIBLE"))
            context.Notify($"{obj.Name}{(thisRoom ? " (this room)" : string.Empty)} [broadcasting].");
    }

    private static bool ConnectedListener(ObjectDatabase db, MUSHObject obj)
    {
        if (obj.Type == ObjectType.Player)
            return db.IsConnected(obj.Id);
        return obj.HasFlag("PUPPET") && obj.Owner.IsValid && db.IsConnected(obj.Owner);
    }
}

public class WithCommandHandler : ICommandHandler
{
    private readonly CommandDispatcher _dispatcher;
    private readonly ExpressionEvaluator? _evaluator;
    private readonly Dbref _masterRoom;

    public string Name => "with";

    public WithCommandHandler(CommandDispatcher dispatcher, ExpressionEvaluator? evaluator = null, int masterRoom = 2)
    {
        _dispatcher = dispatcher;
        _evaluator = evaluator;
        _masterRoom = new Dbref(masterRoom);
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || string.IsNullOrWhiteSpace(context.LeftArg) || string.IsNullOrWhiteSpace(context.RightArg))
        {
            context.Notify("Usage: with <object>=<command>");
            return false;
        }

        var target = Find(db, actor, context.LeftArg.Trim());
        if (target == null)
        {
            context.Notify("I don't see that here.");
            return false;
        }

        bool roomSwitch = context.Switches.Contains("room");
        if (!ListenerSearch.Nearby(actor, target) && !ListenerSearch.Controls(actor, target))
        {
            if (roomSwitch)
            {
                if (target.Id != _masterRoom && target.Id != actor.Zone)
                {
                    context.Notify("I don't see that here.");
                    return false;
                }
                if (target.Id == actor.Zone && target.Type != ObjectType.Room)
                {
                    context.Notify("Make room! Make room!");
                    return false;
                }
            }
            else if (target.Id != actor.Zone || target.Type == ObjectType.Room)
            {
                context.Notify("I don't see that here.");
                return false;
            }
        }

        string command = context.RightArg.Trim();
        if (!roomSwitch)
        {
            if (!ListenerSearch.TryFirst(target, command, out string action, out var captures))
            {
                context.Notify("No matching command.");
                return false;
            }
            Run(context, db, target, action, captures);
            return true;
        }

        if (target.Type != ObjectType.Room && target.Id != actor.Location)
        {
            context.Notify("Make room! Make room!");
            return false;
        }

        bool any = false;
        foreach (var item in db.GetContents(target.Id))
        {
            if (!ListenerSearch.TryFirst(item, command, out string action, out var captures))
                continue;
            any = true;
            Run(context, db, item, action, captures);
        }
        if (!any)
            context.Notify("No matching command.");
        return any;
    }

    private void Run(CommandContext context, ObjectDatabase db, MUSHObject listener, string action, string[] captures)
    {
        string evaluated = action;
        if (_evaluator != null)
        {
            var evalCtx = new EvaluationContext(listener.Id, context.Executor, location: listener.Location, args: captures);
            evaluated = _evaluator.Evaluate(action, evalCtx);
        }
        if (string.IsNullOrWhiteSpace(evaluated))
            return;
        if (evaluated.StartsWith('@') || evaluated.StartsWith('"') || evaluated.StartsWith(':') || evaluated.StartsWith(';'))
        {
            var result = _dispatcher.Dispatch(listener.Id, evaluated, db);
            foreach (var note in result.TargetedNotifications)
                context.TargetedNotifications.Add(note);
            foreach (var note in result.Notifications)
                context.Notifications.Add(note);
            return;
        }
        context.Notify(evaluated);
    }

    private static MUSHObject? Find(ObjectDatabase db, MUSHObject actor, string name)
    {
        if (name.Equals("me", StringComparison.OrdinalIgnoreCase))
            return actor;
        if (name.Equals("here", StringComparison.OrdinalIgnoreCase))
            return actor.Location.IsValid ? db.GetObject(actor.Location) : null;
        if (name.StartsWith('#') && int.TryParse(name.Substring(1), out int id))
            return db.GetObject(new Dbref(id));
        return db.MatchObject(actor.Id, name);
    }
}

public class SearchCommandHandler : ICommandHandler
{
    private static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase)
    {
        "type", "name", "zone", "parent", "flags", "lflags", "powers",
        "exits", "things", "objects", "rooms", "players",
        "mindb", "maxdb", "start", "count"
    };

    private static readonly HashSet<string> Unsupported = new(StringComparer.OrdinalIgnoreCase)
    {
        "eval", "elock", "command", "listen", "eplayer", "eroom", "eexit", "ething", "eobject"
    };

    public string Name => "@search";

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null)
        {
            context.Notify("Permission denied.");
            return false;
        }

        if (!TryParse(context, out string ownerToken, out int? minDb, out int? maxDb, out var criteria, out string? error))
        {
            context.Notify(error ?? "I don't understand that search.");
            return false;
        }

        MUSHObject? owner = null;
        bool all = ownerToken.Equals("all", StringComparison.OrdinalIgnoreCase);
        if (!all)
        {
            owner = ownerToken.Equals("me", StringComparison.OrdinalIgnoreCase)
                ? actor
                : FindPlayer(db, ownerToken);
            if (owner == null || owner.Type != ObjectType.Player)
            {
                context.Notify("I can't find that player.");
                return false;
            }
        }

        bool playersArePublic = criteria.Any(pair =>
            pair.Key.Equals("type", StringComparison.OrdinalIgnoreCase) && IsPlayerType(pair.Value)
            || pair.Key.Equals("players", StringComparison.OrdinalIgnoreCase));
        int? criterionMin = Value(criteria, "mindb");
        int? criterionMax = Value(criteria, "maxdb");

        var hits = new List<MUSHObject>();
        foreach (var obj in db.GetAllObjects().OrderBy(obj => obj.Id.Value))
        {
            if (minDb.HasValue && obj.Id.Value < minDb.Value)
                continue;
            if (maxDb.HasValue && obj.Id.Value > maxDb.Value)
                continue;
            if (criterionMin.HasValue && obj.Id.Value < criterionMin.Value)
                continue;
            if (criterionMax.HasValue && obj.Id.Value > criterionMax.Value)
                continue;
            if (owner != null && obj.Owner != owner.Id)
                continue;
            if (!Matches(obj, criteria))
                continue;
            bool showPlayers = playersArePublic && obj.Type == ObjectType.Player;
            if (!actor.IsWizard && !showPlayers && !ListenerSearch.CanExamine(actor, obj))
                continue;
            hits.Add(obj);
        }

        int start = Value(criteria, "start") ?? 1;
        int count = Value(criteria, "count") ?? hits.Count;
        if (start < 1)
            start = 1;
        hits = hits.Skip(start - 1).Take(Math.Max(0, count)).ToList();
        if (hits.Count == 0)
        {
            context.Notify("Nothing found.");
            return true;
        }

        ReportGroup(context, "ROOMS:", hits.Where(obj => obj.Type == ObjectType.Room));
        ReportGroup(context, "EXITS:", hits.Where(obj => obj.Type == ObjectType.Exit));
        ReportGroup(context, "THINGS:", hits.Where(obj => obj.Type == ObjectType.Thing));
        ReportGroup(context, "PLAYERS:", hits.Where(obj => obj.Type == ObjectType.Player));
        return true;
    }

    private static bool TryParse(
        CommandContext context,
        out string owner,
        out int? minDb,
        out int? maxDb,
        out List<(string Key, string Value)> criteria,
        out string? error)
    {
        owner = "me";
        minDb = null;
        maxDb = null;
        criteria = new List<(string, string)>();
        error = null;
        var words = (context.LeftArg ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var pieces = string.IsNullOrWhiteSpace(context.RightArg)
            ? Array.Empty<string>()
            : context.RightArg.Split(',');

        var numbers = new List<int>();
        if (words.Length > 0 && IsClass(words[^1]) && pieces.Length > 0)
        {
            if (words.Length >= 2)
                owner = words[0];
            criteria.Add((words[^1], pieces[0].Trim()));
            for (int i = 1; i < words.Length - 1; i++)
            {
                if (!int.TryParse(words[i], out int number))
                {
                    error = "I don't understand that search.";
                    return false;
                }
                numbers.Add(number);
            }
            for (int i = 1; i < pieces.Length; i++)
            {
                int eq = pieces[i].IndexOf('=');
                if (eq <= 0)
                {
                    error = "I don't understand that search.";
                    return false;
                }
                criteria.Add((pieces[i].Substring(0, eq).Trim(), pieces[i].Substring(eq + 1).Trim()));
            }
        }
        else if (words.Length > 0)
        {
            owner = words[0];
            for (int i = 1; i < words.Length; i++)
            {
                if (!int.TryParse(words[i], out int number))
                {
                    error = "I don't understand that search.";
                    return false;
                }
                numbers.Add(number);
            }
        }

        if (numbers.Count > 2)
        {
            error = "I don't understand that search.";
            return false;
        }
        if (numbers.Count > 0)
            minDb = numbers[0];
        if (numbers.Count > 1)
            maxDb = numbers[1];

        foreach (var pair in criteria)
        {
            if (Unsupported.Contains(pair.Key))
            {
                error = "That search is not available.";
                return false;
            }
            if (!Supported.Contains(pair.Key))
            {
                error = "I don't understand that search.";
                return false;
            }
        }
        return true;
    }

    private static bool IsClass(string word) => Supported.Contains(word) || Unsupported.Contains(word);

    private static MUSHObject? FindPlayer(ObjectDatabase db, string name)
    {
        if (name.StartsWith('#') && int.TryParse(name.Substring(1), out int id))
            return db.GetObject(new Dbref(id));
        return db.FindPlayer(name);
    }

    private static bool Matches(MUSHObject obj, List<(string Key, string Value)> criteria)
    {
        string? type = null;
        foreach (var pair in criteria)
        {
            if (pair.Key.Equals("type", StringComparison.OrdinalIgnoreCase))
                type = pair.Value;
        }
        if (type != null && !TypeMatches(obj.Type, type))
            return false;

        foreach (var pair in criteria)
        {
            if (pair.Key.Equals("name", StringComparison.OrdinalIgnoreCase) && !obj.Name.StartsWith(pair.Value, StringComparison.OrdinalIgnoreCase))
                return false;
            if (pair.Key.Equals("zone", StringComparison.OrdinalIgnoreCase) && !InParsedZone(obj, pair.Value))
                return false;
            if (pair.Key.Equals("parent", StringComparison.OrdinalIgnoreCase) && !HasParsedParent(obj, pair.Value))
                return false;
            if (pair.Key.Equals("flags", StringComparison.OrdinalIgnoreCase) && !HasFlagLetters(obj, pair.Value))
                return false;
            if (pair.Key.Equals("lflags", StringComparison.OrdinalIgnoreCase) && !HasFlagNames(obj, pair.Value))
                return false;
            if (pair.Key.Equals("powers", StringComparison.OrdinalIgnoreCase) && !HasPowers(obj, pair.Value))
                return false;
            if (pair.Key.Equals("exits", StringComparison.OrdinalIgnoreCase) && !NamedType(obj, ObjectType.Exit, pair.Value))
                return false;
            if ((pair.Key.Equals("things", StringComparison.OrdinalIgnoreCase) || pair.Key.Equals("objects", StringComparison.OrdinalIgnoreCase))
                && !NamedType(obj, ObjectType.Thing, pair.Value))
                return false;
            if (pair.Key.Equals("rooms", StringComparison.OrdinalIgnoreCase) && !NamedType(obj, ObjectType.Room, pair.Value))
                return false;
            if (pair.Key.Equals("players", StringComparison.OrdinalIgnoreCase) && !NamedType(obj, ObjectType.Player, pair.Value))
                return false;
            if ((pair.Key.Equals("mindb", StringComparison.OrdinalIgnoreCase) || pair.Key.Equals("maxdb", StringComparison.OrdinalIgnoreCase)
                || pair.Key.Equals("start", StringComparison.OrdinalIgnoreCase) || pair.Key.Equals("count", StringComparison.OrdinalIgnoreCase))
                && !int.TryParse(pair.Value, out _))
                return false;
        }
        return true;
    }

    private static bool TypeMatches(ObjectType type, string value)
    {
        if (value.Equals("thing", StringComparison.OrdinalIgnoreCase) || value.Equals("object", StringComparison.OrdinalIgnoreCase))
            return type == ObjectType.Thing;
        if (value.Equals("room", StringComparison.OrdinalIgnoreCase))
            return type == ObjectType.Room;
        if (value.Equals("exit", StringComparison.OrdinalIgnoreCase))
            return type == ObjectType.Exit;
        if (value.Equals("player", StringComparison.OrdinalIgnoreCase))
            return type == ObjectType.Player;
        return false;
    }

    private static bool IsPlayerType(string value) =>
        value.Equals("player", StringComparison.OrdinalIgnoreCase);

    private static bool NamedType(MUSHObject obj, ObjectType type, string prefix) =>
        obj.Type == type && obj.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    private static bool InParsedZone(MUSHObject obj, string value) =>
        TryDbref(value, out var id) && obj.InZone(id);

    private static bool HasParsedParent(MUSHObject obj, string value) =>
        TryDbref(value, out var id) && obj.Parents.Contains(id);

    private static bool TryDbref(string value, out Dbref id)
    {
        string text = value.StartsWith('#') ? value.Substring(1) : value;
        if (int.TryParse(text, out int number))
        {
            id = new Dbref(number);
            return true;
        }
        id = Dbref.Nothing;
        return false;
    }

    private static bool HasFlagLetters(MUSHObject obj, string letters)
    {
        if (letters.Length == 0)
            return false;
        foreach (char letter in letters)
        {
            var flag = FlagTable.FindFlagByLetter(letter, obj.Type);
            if (flag == null || !obj.HasFlag(flag.Name))
                return false;
        }
        return true;
    }

    private static bool HasFlagNames(MUSHObject obj, string names)
    {
        var parts = names.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 && parts.All(obj.HasFlag);
    }

    private static bool HasPowers(MUSHObject obj, string names)
    {
        var parts = names.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 && parts.All(obj.HasPower);
    }

    private static int? Value(List<(string Key, string Value)> criteria, string key)
    {
        for (int i = criteria.Count - 1; i >= 0; i--)
        {
            if (criteria[i].Key.Equals(key, StringComparison.OrdinalIgnoreCase) && int.TryParse(criteria[i].Value, out int number))
                return number;
        }
        return null;
    }

    private static void ReportGroup(CommandContext context, string header, IEnumerable<MUSHObject> objects)
    {
        var list = objects.ToList();
        if (list.Count == 0)
            return;
        context.Notify(header);
        foreach (var obj in list)
            context.Notify($"{obj.Name} (#{obj.Id.Value})");
    }
}
