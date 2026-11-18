namespace PennMUSH.Core.Database;

using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

/// <summary>
/// The database <c>@sql</c> and <c>@mapsql</c> query. It is a SQLite file
/// named by <c>sql_database</c>, separate from the game database.
/// </summary>
public sealed class SqlDatabase : IDisposable
{
    private SqliteConnection? _connection;

    public bool IsConnected => _connection != null;

    public string? LastError { get; private set; }

    public void OpenFile(string path)
    {
        Close();
        var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        _connection = connection;
        LastError = null;
    }

    public SqlQueryResult Query(string sql)
    {
        if (_connection == null)
            return SqlQueryResult.NotConnected();

        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            using var reader = command.ExecuteReader();
            if (reader.FieldCount == 0)
                return SqlQueryResult.Affected(reader.RecordsAffected);

            var fields = new List<string>(reader.FieldCount);
            for (int i = 0; i < reader.FieldCount; i++)
                fields.Add(reader.GetName(i));

            var rows = new List<string?[]>();
            while (reader.Read())
            {
                var row = new string?[fields.Count];
                for (int i = 0; i < fields.Count; i++)
                    row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i)?.ToString();
                rows.Add(row);
            }

            return SqlQueryResult.Selected(fields, rows);
        }
        catch (SqliteException ex)
        {
            LastError = ex.Message;
            return SqlQueryResult.Failed(ex.Message);
        }
    }

    public void Close()
    {
        if (_connection != null)
        {
            SqliteConnection.ClearPool(_connection);
            _connection.Dispose();
            _connection = null;
        }
    }

    public void Dispose() => Close();
}

public sealed class SqlQueryResult
{
    public bool Connected { get; private init; }
    public string? Error { get; private init; }
    public int AffectedRows { get; private init; } = -1;
    public IReadOnlyList<string> Fields { get; private init; } = Array.Empty<string>();
    public IReadOnlyList<string?[]> Rows { get; private init; } = Array.Empty<string?[]>();
    public bool HasRows => Fields.Count > 0 || Rows.Count > 0;

    public static SqlQueryResult NotConnected() => new() { Connected = false };

    public static SqlQueryResult Failed(string error) => new() { Connected = true, Error = error };

    public static SqlQueryResult Affected(int rows) => new() { Connected = true, AffectedRows = Math.Max(0, rows) };

    public static SqlQueryResult Selected(IReadOnlyList<string> fields, IReadOnlyList<string?[]> rows) =>
        new() { Connected = true, Fields = fields, Rows = rows, AffectedRows = -1 };
}
