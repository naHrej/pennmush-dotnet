# PennMUSH 1.8 Parity & Differences

This page replaces legacy porting checklists with a codebase-verified ledger against `PennMUSH.Core` and `PennMUSH.Server` on `main`.

---

## 1. PennMUSH.NET Extensions & Modernizations

Features implemented in PennMUSH .NET that extend or modernize stock PennMUSH 1.8:

- **DAG Multi-Parent & Multi-Zone Support**: `MUSHObject` allows multiple ordered parents (`@parent <obj>=<p1> <p2>...`, breadth-first lookup with cycle detection) and multiple zones (`@zone <obj>=<z1> <z2>...`), plus extra helpers `lzone()`, `inzone()`, `exits()`, `attrcnt()`, and `nameformat()`.
- **Managed Background & Async Softcode (`AsyncFunctions.cs`)**: `async()`, `parallel_map()`, `bgtask()` (marked `[Extension]` in API docs).
- **Persistent Cron Scheduling (`CronCommandHandler.cs`, `CronFunctions.cs`)**: `@cron` command persisting 5-field schedules in SQLite `cron_jobs`, inspectable via `cron_list()` and `cron_info()`.
- **MUjs Client UI & Framing Extensions**:
  - `@client` (`/edit`, `/window`, `/style`, `/audio`/`/sound`) and functions `cedit()`, `cwindow()`, `cstyle()`, `caudio()`, `client_emit()`.
  - `wshtml()` / `wsjson()` for WebSocket inline HTML & out-of-band JSON envelopes with Telnet fallbacks.
- **Zero-Disconnect Hot-Reboot & C# Plugins**: `@reboot`, HTTP `POST /reboot`, and collectible `ExtensionLoadContext` plugins managed via `@plugin` / `@extension`.
- **SQLite Persistence & Catalogs**: Single-binary managed persistence across `mush.db` (`SqliteStore` + `RuntimeStateStore`), `connlog.db`, and optional SQLite `sql_database`, backed by centralized `Feedback` (701 templates) and `MushError` (91 templates) catalogs.

---

## 2. Completed Parity Summary

All major parity waves (PRs #1–#54) have landed on `main`:
- **Commands**: All 167 stock PennMUSH command names (plus 4 host/extension commands = **201** distinct C# command names/aliases across **139** handler registrations, plus **3** extra `alias.cnf` command aliases `@gedit`, `@attrlock`, `@attrchown` = **204** under default config).
- **Functions**: **567** distinct built-in MUSHcode function registrations across 18 modules (**40** `noParse`, **29** `sideEffect`), plus **18** `function_alias` entries in `game/alias.cnf` (**578** callable names under default config).
- **Backends 1–8 (PRs #47–#54)**:
  - Shared SQLite SQL DB (`@sql`, `@mapsql`, `sql()`, `mapsql()`, `sqlescape()`).
  - Persistent `connlog.db` (`connlog()`, `connrecord()`, `addrlog()`, `@sitelock/player`).
  - SQLite `RuntimeStateStore` (channels, mail, mail aliases, `@function` softcode, sitelocks) & `checkpass()` `XYXXY` fallback.
  - Atomic `RQUOTA` quota accounting (`QUOTAS`, `NO_QUOTA`, `No_Quota` power, `use_quota`).
  - Shared `ITextFileCache`, admin-gated text files, `suggest()`/`dict_file` caching, `@readcache`.
  - Descriptor capabilities, `@sockset` transforms (`OUTPUTPREFIX`, `OUTPUTSUFFIX`, `STRIPACCENTS`, `PROMPT_NEWLINES`, `NOQUOTA`, `COLORSTYLE`), `wshtml()`/`wsjson()` channel framing.
  - Full 927-color `colors.json`, `%k`/`%K`, `@moniker` ANSI sanitization, ANSI-aware `align()`/`wrap()`, smart `AnsiToHtmlTranslator`.
  - Centralized `Feedback` and `MushError` catalogs with 0 remaining inline `"#-1` error literals in `Functions/*.cs`.

---

## 3. Verified Remaining Partial Command Switches & Modes

Audited directly against each C# handler:

| Command | Works Today | Missing / Ignored Switches & Modes |
|---|---|---|
| `@slave` | Bare `@slave` replies `"I'm sorry, Dave..."`; `@slave/restart` replies `"No such service."` | Stock `info` / `ssl` daemon restarts do not run external daemons (see Section 5) |
| `@ps` | Plain `@ps`, `/all`, `/summary`, `/count`, and `<pid>` inspect queues | `/quick` and `/debug` switches are ignored |
| `@dump` | Saves DB (`SqliteStore` + `RuntimeStateStore`) | `/paranoid`, `/debug`, `/nofork` switches are ignored (all perform standard save) |
| `@edit` | Literal substring replacement in an attribute | `/first`, `/regexp`, `/all`, `/nocase`, `/check`, `/quiet` switches are not read |
| `@grep` | Case-insensitive substring search on an object's own attributes | `/list`, `/print`, `/ilist`, `/iprint`, `/regexp`, `/wild`, `/nocase`, `/parent` switches are not read |
| `@list` | `commands`, `flags`, `powers`, `functions`, `attribs` (with `/lowercase`), `motd` | `/locks`, `/allocations`, `/all`, `/builtin`, `/local` fall through to usage; `/lowercase` only applies to `attribs` |
| `@include` & `@hook` | `@include` + `/nobreak`; `@hook` stores/runs `/before`, `/after`, `/ignore`, `/override`, `/extend` + `/inplace`/`/inline` | `@include/localize`, `@include/clearregs` are ignored; `@hook` stores `Localize`/`ClearRegs` flags on `CommandHook` display text, but hook execution does not localize/clear Q-registers |
| `@search` | Owner, dbref range, `type`, `name`, `zone`, `parent`, `flags`, `lflags`, `powers`, `exits`/`things`/`objects`/`rooms`/`players`, `mindb`/`maxdb`/`start`/`count` | `eval`, `elock`, `command`, `listen`, `eplayer`, `eroom`, `eexit`, `ething`, `eobject` return `"That search is not available."` |
| `@sitelock` | `/ban`, `/register`, `/check`, `/remove`, `/player` (via `connlog.db`), bare `@sitelock`, persisted in SQLite `sitelocks` | `/name` reports it cannot open/lock/delete the names file |
| `@stats` | Prints total, player, room, exit, thing, and garbage counts | `/tables`, `/chunks`, `/regions`, `/paging`, `/freespace`, `/flags` switches are ignored |
| Alias-file-only commands | `@gedit`, `@attrlock`, `@attrchown` work when `game/alias.cnf` is loaded | Not registered directly in C# `MUSHServer.RegisterCommands()` if running without `alias.cnf` |
| Lock expressions | Boolean `&`, `|`, `!`, `(...)` over dbref keys (`#n`, `=#n`, `n`) | Stock attribute/flag/eval/owner/indirect lock atoms default to unlocked (`ConstantLockNode(true)`) in `LockParser.cs` |

---

## 4. Unimplemented `%`-Substitutions

`ExpressionEvaluator.cs` preserves unrecognized `%` codes verbatim. Not yet implemented:
- `%~` (accented name), `%c` / `%C` (raw command), `%u` / `%U` (evaluated command), `%=` (active attribute name), `% ` (percent-space)
- `%i0`–`%i9`, `%il` (iteration stack — use `itext()` / `ilev()` instead)
- `%$0`–`%$9`, `%$l` (`switch()` stack — use `stext()` / `slev()` instead)

---

## 5. Intentionally Omitted Legacy C Mechanisms

- External `info_slave` / `ssl_slave` child daemon processes (`TelnetListenerService` and `MushServer` run in-process on .NET async sockets/DNS).
- POSIX `fork()` dump processes (`SqliteStore` writes atomically in-process).
- C memory/chunk allocator statistics (`/chunks`, `/regions`, `/paging` in stock PennMUSH `@stats`).
- MySQL / PostgreSQL native database drivers (superseded by managed embedded SQLite via `Microsoft.Data.Sqlite`).
