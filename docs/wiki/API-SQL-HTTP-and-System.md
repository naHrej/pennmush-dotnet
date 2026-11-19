# API: SQL, HTTP & System

SQL queries/escaping, configuration lookups, server/version introspection, queue PID inspection, help/text cache search, typo suggestions, and benchmarking extracted from `PennMUSH.Core/MUSHcode/Functions/LateSubsystemFunctions.cs` (**9** functions + `dynhelp` alias), `DbFunctions.cs` (**6** functions), `Section9And18Functions.cs` (**3** functions), and `CryptoFunctions.cs` (**1** function: `sqlescape()`).

> [!NOTE]
> Outbound HTTP requests and inbound web response headers are provided as commands ([`@http`](Commands-Server-Admin-and-Extensions) and [`@respond`](Commands-Server-Admin-and-Extensions)) rather than softcode functions.

---

## SQL Side-Effects & Escaping

| Function | Signature | Notes |
|---|---|---|
| `sql` | `sql(<query>[, <rowSep>[, <colSep>]])` | Executes `<query>` (`Wizard` or `Sql_Ok` power) |
| `mapsql` | `mapsql(<[obj/]attr>, <query>[, <osep>[, <colNames?>]])` | Maps SQL result rows through `Ufun` and sets `%q<col>` registers |
| `sqlescape` | `sqlescape(<string>)` | *(`CryptoFunctions.cs`)* Backslash-escapes `\ ' " \0 \n \r` (`Wizard` or `Sql_Ok`) |

* **`sql()` & `mapsql()`**: Require `Wizard` or `Sql_Ok` (`#-1 PERMISSION DENIED`), an active SQL connection (`#-1 SQL IS NOT ENABLED`), and non-blank `<query>` (`#-1 TOO FEW ARGUMENTS`); database errors return `#-1 SQL ERROR: <msg>`. `<rowSep>`, `<colSep>`, and `<osep>` default to `" "`.
* **`mapsql()`**: Invokes `Ufun` per row with `%0` = `1`-based row number and `%1..` = column values (also stored into Q-registers `%q<colName>` for valid non-numeric column names). Passing truthy `<colNames?>` emits an extra initial header call with `%0` = `0` and `%1..` = column names.

---

## Configuration, Introspection & Benchmarking

| Function | Signature | Notes |
|---|---|---|
| `config` | `config([<option>])` | Returns setting string (no-arg lists `mud_name mud_url starting_money`) |
| `mudname` | `mudname()` | Configured `mud_name` from `FunctionRegistry.MudName` |
| `mudurl` | `mudurl()` | Configured `mud_url` from `FunctionRegistry.MudUrl` |
| `version` | `version()` | Returns `"PennMUSH.NET 1.8.8p0"` |
| `numversion` | `numversion()` | Numeric compatibility constant (`1080800`) |
| `poll` | `poll()` | Current server `@poll` question banner |
| `functions` | `functions([all\|builtin\|local])` | Sorted uppercase function names (`all` default; `#-1` on unknown filter) |
| `list` | `list(<type>[, <which>])` | Prefix-matched table names (`functions`, `@functions`, `attribs`, `flags`, `powers`, `locks`) |
| `benchmark` | `benchmark(<expr>, <count>[, <target>])` | `` `noParse` `` Runs `<expr>` `1..10000` times and reports `avg`/`min`/`max` µs |

* **`config([<option>])`**: Delegates to `ConfigProvider(key)` when wired, falling back to `mud_name`, `mud_url`, `starting_money`, `use_quota` (`yes`/`no`), and `starting_quota` (`#-1` on unknown key).
* **`benchmark(<expr>, <count>[, <target>])`**: Clamps `<count>` at `10,000` (`<= 0` -> `#-1 ARGUMENT MUST BE POSITIVE INTEGER`). When `<target>` is supplied, sends `Feedback.BenchmarkReport` to `<target>` via `NotificationDispatcher` and returns the last evaluation result of `<expr>`; otherwise returns the benchmark report string directly.

---

## Action Queue PID Inspection (`Section9And18Functions.cs`)

| Function | Signature | Notes |
|---|---|---|
| `pidinfo` | `pidinfo(<pid>[, <fields>[, <sep>]])` | Queries `<fields>` (`executor`, `enactor`, `command`, `semaphore`, `time`) for `<pid>` |
| `getpids` | `getpids([<object>])` | Space-separated queued PIDs (all entries, or filtered by `Executor == <object>`) |
| `lpids` | `lpids([<object>])` | Alias behavior to `getpids()` over `ActionQueue.Snapshot()` |

* **`pidinfo()`**: `<fields>` defaults to `"executor"` (space-separated field names); `<sep>` defaults to `" "`. `time` returns remaining delay in whole seconds (`>= 0`). Non-integer `<pid>` returns `#-1 ARGUMENT MUST BE INTEGER`; missing `<pid>` returns `#-1 NO MATCH`.

---

## Text Caches & Suggestions

| Function | Signature | Notes |
|---|---|---|
| `textentries` | `textentries(<file>, <pattern>[, <sep>])` | Lists topic keys in `<file>` matching wildcard `<pattern>` |
| `textfile` | `textfile(<file>, <topic>)` | Returns topic body (`allowPrefix: true`; `#-1 NO SUCH FILE` / `#-1 NO ENTRY`) |
| `dynhelp` | `dynhelp(<file>, <topic>)` | `` `alias.cnf` `` Alias of `textfile()` |
| `textsearch` | `textsearch(<file>, <needle>[, <sep>])` | Searches entry contents in `<file>` (wildcard if `*`/`?`, else case-insensitive substring) |
| `suggest` | `suggest(<category>, <word>[, <delim>[, <limit>]])` | Fuzzy suggestions (Levenshtein `<= 2` / prefix, default `<limit>` = `20`) |

* **`textentries` / `textfile` / `textsearch`**: Query `ITextFileCache` (or wired test providers) with `<sep>` defaulting to `" "`. Categories marked `AdminOnly` return `#-1 PERMISSION DENIED` unless caller is `Royalty+` (`obj.IsRoyalty`).
* **`suggest()`**: Searches built-in `functions`, `commands`, `attributes`/`attribs`, `flags`, `powers`, plus readable `ITextFileCache` categories and `@suggest` custom vocabulary (`SuggestionVocabulary`).

```text
think [version()] ([numversion()])
think [benchmark([add(1, 2)], 100)]
think [suggest(functions, strl)]
```

---

See also: [Server Admin Commands (`@config`, `@sql`, `@http`, `@readcache`)](Commands-Server-Admin-and-Extensions) · [Queue Commands (`@ps`, `@wait`)](Commands-Softcode-Flow-and-Queue) · [Functions Index](Functions-Index) · [Home](Home)
