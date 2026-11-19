# PennMUSH.NET Backend Parity Plan

This document tracks the missing or in-memory-only backend subsystems identified in [command-port-order.md](command-port-order.md), [function-port-order.md](function-port-order.md), and [function-compatibility-gaps.md](function-compatibility-gaps.md), along with what was explicitly deferred from **PR 6** until these backends exist.

---

## Explicitly Left Out of PR 6 (Blocked on Missing Backends)

| Function(s) | Current Behavior | Blocking Backend |
|---|---|---|
| `sql()`, `mapsql()` | Hardcoded `#-1 SQL IS NOT ENABLED` (`LateSubsystemFunctions.cs`) | **Backend 1: Shared SQL Provider** |
| `sqlescape()` | Skips SQL-enabled & `Sql_Ok` permission checks (`CryptoFunctions.cs`) | **Backend 1: Shared SQL Provider** |
| `addrlog()`, `connlog()`, `connrecord()` | Stubbed to return `""` for wizards (`LateSubsystemFunctions.cs`) | **Backend 2: Persistent Connection Log DB** |
| `wshtml()`, `wsjson()` | No WebSocket-channel markup wrapper; `wsjson()` returns raw `args[0]` | **Backend 6: Descriptor & WebSocket/HTTP Layer** |
| `colors()` (SQLite table), `%k`, `align()` `(ansi)` | Static 14-color dictionary; `%k` unimplemented; `align()` discards `(ansi)` | **Backend 7: Full Color Table & Markup Engine** |
| Non-`#-1` stock wording | Inline strings in handlers/functions instead of a centralized catalog | **Backend 8: Stock Wording Catalog** |

---

## Backend Implementation Plan

### 1. Shared SQL Provider (`@sql`, `@mapsql`, `sql()`, `mapsql()`, `sqlescape()`)
- **Gap**: `@sql` and `@mapsql` (`command-port-order.md` rows 85–86) only query a local SQLite `sql_database` file (no MySQL/PostgreSQL), and `FunctionRegistry` has no SQL service wired to `LateSubsystemFunctions.cs` (`sql()`, `mapsql()`) or `CryptoFunctions.cs` (`sqlescape()`).
- **Plan**:
  1. Extract a shared `ISqlService` interface in `PennMUSH.Core` (supporting `sql_database` SQLite and optional MySQL/PostgreSQL providers) with `Sql_Ok` power checks.
  2. Wire `ISqlService` into both `SqlCommandHandler` (`@sql`/`@mapsql`) and `FunctionRegistry.SqlService`.
  3. Implement `sql(<query>[, <rsep>[, <csep>]])`, `mapsql(<obj/attr>, <query>[, <osep>[, <colnames>]])`, and `sqlescape()` permission/enabled checks.

### 2. Persistent Connection Log Database (`addrlog()`, `connlog()`, `connrecord()`, `@log`, `@logwipe`, `@sitelock/player`)
- **Gap**: `command-port-order.md` rows 80 & 82 and `function-port-order.md` §19 note there is no persistent `connlog.db` recording every login, disconnect, and IP address. `@log`/`@logwipe` only recall lines written since process start, `@sitelock/player` has no stored last address, and `addrlog()`/`connlog()`/`connrecord()` are stubbed.
- **Plan**:
  1. Add an `IConnectionLogStore` backed by SQLite (recording `player`, `ip`, `host`, `connected_at`, `disconnected_at`, `disconnect_reason`).
  2. Hook login/disconnect events in `PennMUSH.Server` to record entries and update `@sitelock/player` last address.
  3. Wire `IConnectionLogStore` into `FunctionRegistry` and implement `addrlog()`, `connlog()`, and `connrecord()` query filters.

### 3. SQLite Persistence for Runtime Tables
- **Gap**: `command-port-order.md` (lines 34–35, rows 45–48, 64–67, 69–72, 75, 80, 83) notes the following tables exist in memory only and reset on restart:
  - **Channels & `@clock`**: `ChannelRegistry` membership, `addcom`/`delcom` aliases, `comtitle`, and channel locks.
  - **Mail & `@malias`**: `MailStore` messages/folders and `MailAliasTable`.
  - **Editable Tables & Config**: `@function`, `@command`, `@flag`, `@attribute`, `@sitelock`, `@motd`/`@wizmotd`/`@rejectmotd`, `@suggest` vocabulary, and `@config/save` / `@disable` / `@enable` writing `mush.cnf`.
  - **Passwords (`checkpass()`)**: `IPlayerPasswords` only exposes `Set(Dbref, string)` (`checkpass()` relies on an external `PasswordVerifier` delegate rather than verifying the `XYXXY` `2:sha512:` hash directly).
- **Plan**:
  1. Add SQLite tables/serializers for Channels + ChannelMembers + ChannelLocks, Mail + MailAliases, SoftFunctions + CommandOverrides + CustomFlags + CustomAttributes + SitelockRules + MotdState + SuggestVocabulary.
  2. Implement `Verify(Dbref, string)` on `IPlayerPasswords` reading `XYXXY` (`2:sha512:`) so `checkpass()` works out of the box without custom wiring.
  3. Add `mush.cnf` writer support for `@config/save` and `@disable`/`@enable` (gated behind a config flag).

### 4. Quota Accounting & Flags (`@quota`, `@squota`, `@allquota`, `quota()`)
- **Gap**: `command-port-order.md` (lines 207–218) notes `NO_QUOTA` and `QUOTAS` are not in the flag table, and `create`/`clone`/`@destroy`/`@chown` do not decrement/increment `RQUOTA` (only `@chownall` adjusts it).
- **Plan**:
  1. Add `NO_QUOTA` / `NoQuota` flag/power definitions and `use_quota` config gating.
  2. Update `CreateFunc`/`@create`, `OpenFunc`/`@open`, `DigFunc`/`@dig`, `CloneFunc`/`@clone`, `@destroy`/`@purge`, and `@chown` to check and adjust `RQUOTA` atomically.

### 5. On-Disk Text / Help / Suggest Cache Wiring (`textentries()`, `textfile()`, `textsearch()`, `suggest()`, `@readcache`)
- **Gap**: `LateSubsystemFunctions.cs` relies on optional `TextEntriesProvider` and `TextFileEntryProvider` delegates on `FunctionRegistry` instead of reading directly from the server's `txt/` and `hlp/` index managed by `@readcache`, and `@suggest` keeps its vocabulary in memory rather than a persistent file.
- **Plan**:
  1. Expose a shared `ITextFileCache` (populated at startup and reloaded by `@readcache`) for `help`, `news`, `events`, `rules`, `motd`, etc., including `admin`-only file flags.
  2. Wire `ITextFileCache` into `FunctionRegistry` by default so `textentries()`, `textfile()`, and `textsearch()` work automatically in-game, and persist `@suggest` vocabulary to disk.

### 6. Descriptor Capabilities, Inbound HTTP & WebSocket Layer (`@http`, `@respond`, `@sockset`, `@slave`, `wshtml()`, `wsjson()`, `terminfo()`)
- **Gap**: `command-port-order.md` (rows 81, 87–89) notes:
  - `info_slave` and `ssl_slave` are not separate processes (`@slave`).
  - The telnet port does not parse inbound HTTP requests (`@respond` has no `HTTP` event).
  - `@sockset` stores prefix, suffix, accent stripping, prompt newlines, and `noquota` on descriptors without applying them to output.
  - `ConnectionSnapshot` lacks structured capability flags (`pueblo`, `gmcp`, `ssl`, `websocket`, `stripaccents`, `prompt_newlines`, `xterm256`) and `wshtml()`/`wsjson()` lack WebSocket-channel wrapping.
- **Plan**:
  1. Add structured capability flags to `ConnectionSnapshot` / `Session` and wire `@sockset` output transforms (prefix, suffix, accent stripping, prompt newlines).
  2. Add WebSocket-channel framing in `PennMUSH.Server` so `wshtml()`/`wsjson()` wrap output when on a WebSocket descriptor and fall back to `args[1]` otherwise.
  3. Add optional HTTP listener routing to the `http_handler` player object for `@respond`.

### 7. Full Color Table, `%k` & `align()` `(ansi)` Specs
- **Gap**: `command-port-order.md` (row 9) and `function-port-order.md` (§14) note `%k` is unimplemented, `@moniker` does not validate ANSI codes, `colors()` has a static 14-color dictionary instead of PennMUSH's full X11/CSS `named_colors` table, and `align()`/`lalign()` parse and discard `(ansi)` specs.
- **Plan**:
  1. Populate the full PennMUSH `named_colors` table in `TextFormatFunctions.cs`.
  2. Implement `%k` color-code substitution in `ExpressionEvaluator.cs`, ANSI validation in `@moniker`, and `(ansi)` application in `StringFunctions.Align`.

### 8. Centralized Non-Error Stock Wording Catalog
- **Gap**: Both `command-port-order.md` and `function-port-order.md` defer centralizing non-`#-1` player-facing sentences until all commands and functions are ported.
- **Plan**:
  1. Audit all inline player-facing strings across `PennMUSH.Core/Commands/` and `PennMUSH.Core/MUSHcode/Functions/` and move them into a catalog beside `MushError.cs`.
