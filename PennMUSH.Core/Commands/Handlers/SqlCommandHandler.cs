namespace PennMUSH.Core.Commands.Handlers;

using System;
using System.Collections.Generic;
using System.Globalization;
using PennMUSH.Core.Database;
using PennMUSH.Core.Entities;
using PennMUSH.Core.Queue;
using PennMUSH.Core.Types;

public class SqlCommandHandler : ICommandHandler
{
    private readonly SqlDatabase _sql;

    public string Name => "@sql";

    public SqlCommandHandler(SqlDatabase sql) => _sql = sql;

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        var actor = db.GetObject(context.Executor);
        if (actor == null || !(actor.IsWizard || actor.HasPower("Sql_Ok")))
        {
            context.Notify("Permission denied.");
            return false;
        }

        if (!_sql.IsConnected)
        {
            context.Notify("No SQL database connection.");
            return false;
        }

        if (string.IsNullOrEmpty(context.Arguments))
        {
            context.Notify("What do you want to query?");
            return false;
        }

        SqlQueryResult result = _sql.Query(context.Arguments);
        Report(context, result);
        return result.Error == null && result.Connected;
    }

    internal static void Report(CommandContext context, SqlQueryResult result)
    {
        if (!result.Connected)
        {
            context.Notify("No SQL database connection.");
            return;
        }

        if (result.Error != null)
        {
            context.Notify($"SQL: Error: {result.Error}");
            return;
        }

        if (result.AffectedRows >= 0 && result.Fields.Count == 0)
        {
            context.Notify($"SQL: {result.AffectedRows} rows affected.");
            return;
        }

        int rowNumber = 1;
        if (result.Fields.Count == 0)
        {
            foreach (string?[] _ in result.Rows)
                context.Notify($"Row {rowNumber++}: NULL");
            return;
        }

        foreach (string?[] row in result.Rows)
        {
            for (int i = 0; i < result.Fields.Count; i++)
            {
                string? cell = i < row.Length ? row[i] : null;
                string shown = string.IsNullOrEmpty(cell) ? "NULL" : Sanitize(cell);
                context.Notify($"Row {rowNumber}, Field {result.Fields[i]}: {shown}");
            }

            rowNumber++;
        }
    }

    internal static string Sanitize(string value)
    {
        var kept = new char[value.Length];
        int n = 0;
        foreach (char ch in value)
        {
            if (ch is '\n' or '\t' || (ch >= ' ' && ch <= '~'))
                kept[n++] = ch;
        }

        return new string(kept, 0, n);
    }
}

public class MapSqlCommandHandler : ICommandHandler
{
    private const int MaxArguments = 30;
    private readonly SqlDatabase _sql;
    private readonly ActionQueue _queue;

    public string Name => "@mapsql";

    public MapSqlCommandHandler(SqlDatabase sql, ActionQueue queue)
    {
        _sql = sql;
        _queue = queue;
    }

    public bool Execute(CommandContext context, ObjectDatabase db)
    {
        if (string.IsNullOrEmpty(context.RightArg))
        {
            context.Notify("What do you want to query?");
            return false;
        }

        if (!CopyAttributeCommandHandler.TrySplitAttr(context.LeftArg, out string objectName, out string attribute))
        {
            context.Notify("I need to know what attribute to trigger.");
            return false;
        }

        var actor = db.GetObject(context.Executor);
        var thing = db.MatchObject(context.Executor, objectName);
        if (actor == null || thing == null)
        {
            context.Notify("I can't see that here.");
            return false;
        }

        bool spoof = context.Switches.Contains("spoof");
        if (!CopyAttributeCommandHandler.Controls(actor, thing)
            && (spoof || thing.Owner != actor.Id || !thing.HasFlag("LINK_OK")))
        {
            context.Notify("Permission denied.");
            return false;
        }

        if (thing.IsGod && !actor.IsGod)
        {
            context.Notify("You can't trigger God!");
            return false;
        }

        var stored = thing.GetAttribute(attribute, db);
        if (stored == null || string.IsNullOrEmpty(stored.Value))
            return true;

        SqlQueryResult result = _sql.Query(context.RightArg);
        if (!result.Connected)
        {
            context.Notify("No SQL database connection.");
            return false;
        }

        if (result.Error != null || (result.AffectedRows >= 0 && result.Fields.Count == 0))
        {
            SqlCommandHandler.Report(context, result);
            return result.Error == null;
        }

        string command = StripListenPrefix(stored.Value);
        int fields = Math.Min(result.Fields.Count, MaxArguments - 1);
        Dbref enactor = spoof ? context.Enactor : context.Executor;
        if (context.Switches.Contains("colnames"))
            Queue(thing.Id, enactor, command, 0, result.Fields, names: true, fields);

        int rowNumber = 1;
        foreach (string?[] row in result.Rows)
        {
            Queue(thing.Id, enactor, command, rowNumber, result.Fields, names: false, fields, row);
            rowNumber++;
        }

        if (context.Switches.Contains("notify"))
            _queue.Enqueue(context.Executor, context.Executor, "@notify me");
        return true;
    }

    private void Queue(Dbref thing, Dbref enactor, string command, int rowNumber, IReadOnlyList<string> fields, bool names, int count, string?[]? row = null)
    {
        var args = new string[count + 1];
        var registers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        args[0] = rowNumber.ToString(CultureInfo.InvariantCulture);
        for (int i = 0; i < count; i++)
        {
            string value = names
                ? fields[i]
                : SqlCommandHandler.Sanitize(row != null && i < row.Length ? row[i] ?? string.Empty : string.Empty);
            if (!names && string.IsNullOrEmpty(value))
                value = string.Empty;
            args[i + 1] = value;
            if (!IsStrictInteger(fields[i]))
                registers[fields[i]] = value;
        }

        _queue.Enqueue(thing, enactor, command, args: args, registers: registers);
    }

    private static bool IsStrictInteger(string text)
    {
        if (text.Length == 0)
            return false;
        int start = text[0] == '-' ? 1 : 0;
        if (start == text.Length)
            return false;
        for (int i = start; i < text.Length; i++)
        {
            if (!char.IsDigit(text[i]))
                return false;
        }

        return true;
    }

    private static string StripListenPrefix(string value)
    {
        if (value.Length == 0 || (value[0] != '$' && value[0] != '^'))
            return value;
        for (int i = 1; i < value.Length; i++)
        {
            if (value[i] == '\\' && i + 1 < value.Length)
            {
                i++;
                continue;
            }

            if (value[i] == ':')
                return value[(i + 1)..];
        }

        return value;
    }
}
