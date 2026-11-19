# PennMUSH.NET Backend Parity Plan

This document tracks the remaining backend subsystems identified in [command-port-order.md](command-port-order.md), [function-port-order.md](function-port-order.md), and [function-compatibility-gaps.md](function-compatibility-gaps.md), scoped to modern PennMUSH.NET goals.

---

## Explicitly Out of Scope (Won't Port)

The following legacy PennMUSH subsystems were intentionally excluded and will **not** be implemented:

1. **`info_slave` / `ssl_slave` (`@slave`)**: .NET handles async DNS resolution and TLS natively in-process; separate slave processes are unnecessary.
2. **Legacy Pueblo HTML Protocol**: No Pueblo handshake or 1990s Pueblo client protocol engine (`pueblo()` remains a compatibility function returning `0`/`1` based on client capability; `html()`, `tag()`, `endtag()`, `tagwrap()` remain syntax-compatible helpers). Modern rich clients use WebSockets, GMCP/OOB, and HTML color conversion.
3. **Inbound HTTP on the Telnet Port (`@respond` / `http_handler`)**: The telnet port will not parse inbound HTTP requests; any HTTP APIs belong in ASP.NET Core / dedicated HTTP endpoints.
4. **External MySQL / PostgreSQL Drivers (`@sql` / `sql()` / `mapsql()`)**: SQL support is scoped strictly to local SQLite (`sql_database`).

---

## Deferred from Function PR 6 (Pending Backends Below)

| Function(s) | Current Behavior | Blocking Backend |
|---|---|---|
| `sql()`, `mapsql()` | Hardcoded `#-1 SQL IS NOT ENABLED` (`LateSubsystemFunctions.cs`) | **Backend 1: Shared SQLite SQL Provider** |
| `sqlescape()` | Skips SQL-enabled & `Sql_Ok` permission checks (`CryptoFunctions.cs`) | **Backend 1: Shared SQLite SQL Provider** |
| `addrlog()`, `connlog()`, `connrecord()` | Stubbed to return `""` for wizards (`LateSubsystemFunctions.cs`) | **Backend 2: Persistent Connection Log DB** |
| `wshtml()`, `wsjson()` | No WebSocket-channel markup wrapper; `wsjson()` returns raw `args[0]` | **Backend 6: Descriptor Capabilities & HTML/WebSocket Output** |
| `colors()` (full table), `%k`, `align()` `(ansi)` | Static 14-color dictionary; `%k` unimplemented; `align()` discards `(ansi)` | **Backend 7: Full Color Table, `%k` & HTML Color Conversion** |
| Non-`#-1` stock wording | Inline strings in handlers/functions instead of a centralized catalog | **Backend 8: Stock Wording Catalog** |

---

## Backend Implementation Plan

### 1. Shared SQLite SQL Provider (`@sql`, `@mapsql`, `sql()`, `mapsql()`, `sqlescape()`)
- **Scope**: Local SQLite (`sql_database`) only; no MySQL or PostgreSQL.
- **Plan**:
  1. Extract a shared `ISqlService` interface in `PennMUSH.Core` backed by SQLite with `Sql_Ok` power checks.
  2. Wire `ISqlService` into both `SqlCommandHandler` (`@sql`/`@mapsql`) and `FunctionRegistry.SqlService`.
  3. Implement `sql(<query>[, <rsep>[, <csep>]])`, `mapsql(<obj/attr>, <query>[, <osep>[, <colnames>]])`, and `sqlescape()` permission/enabled checks.

### 2. Persistent Connection Log Database (`addrlog()`, `connlog()`, `connrecord()`, `@log`, `@logwipe`, `@sitelock/player`)
- **Plan**:
  1. Add an `IConnectionLogStore` backed by SQLite (recording `player`, `ip`, `host`, `connected_at`, `disconnected_at`, `disconnect_reason`).
  2. Hook login/disconnect events in `PennMUSH.Server` to record entries and update `@sitelock/player` last address.
  3. Wire `IConnectionLogStore` into `FunctionRegistry` and implement `addrlog()`, `connlog()`, and `connrecord()` query filters.

### 3. SQLite Persistence for Core Game & Player State
- **Scope**: Persist core game/player state to SQLite; keep `@motd`, `@config`, `@command`, `@flag`, `@attribute`, and `@suggest` in-memory or config-driven (no `mush.cnf` writeback).
- **Plan**:
  1. Add SQLite tables/serializers for **Channels** (channels, members, aliases, titles, `@clock` locks), **Mail** (messages, folders, `@malias` aliases), **SoftFunctions** (`@function`), and **Sitelock** rules.
  2. Implement `Verify(Dbref, string)` on `IPlayerPasswords` reading `XYXXY` (`2:sha512:`) so `checkpass()` works out of the box without custom wiring.

### 4. Quota Accounting & Flags (`@quota`, `@squota`, `@allquota`, `quota()`)
- **Scope**: Full `RQUOTA` accounting + `No_Quota` flag/power gated behind `use_quota` config.
- **Plan**:
  1. Add `NO_QUOTA` / `No_Quota` flag/power definitions and `use_quota` config gating.
  2. Update `CreateFunc`/`@create`, `OpenFunc`/`@open`, `DigFunc`/`@dig`, `CloneFunc`/`@clone`, `@destroy`/`@purge`, and `@chown` to check and adjust `RQUOTA` atomically when `use_quota` is enabled.

### 5. On-Disk Text / Help / Suggest Cache Wiring (`textentries()`, `textfile()`, `textsearch()`, `suggest()`, `@readcache`)
- **Plan**:
  1. Expose a shared `ITextFileCache` (populated at startup and reloaded by `@readcache`) for `help`, `news`, `events`, `rules`, `motd`, etc., including `admin`-only file flags.
  2. Wire `ITextFileCache` into `FunctionRegistry` by default so `textentries()`, `textfile()`, and `textsearch()` work automatically in-game.

### 6. Descriptor Capabilities, `@sockset` & WebSocket/HTML Output (`@sockset`, `wshtml()`, `wsjson()`, `terminfo()`)
- **Scope**: Clean WebSocket OOB/JSON and HTML client capability detection without legacy Pueblo handshakes.
- **Plan**:
  1. Add structured capability flags to `ConnectionSnapshot` / `Session` (`gmcp`, `ssl`, `websocket`, `html`, `stripaccents`, `prompt_newlines`, `xterm256`, `rgb24`).
  2. Wire `@sockset` output transforms (prefix, suffix, accent stripping, prompt newlines) into the server output pipeline.
  3. Implement WebSocket-channel framing in `PennMUSH.Server` so `wshtml()` / `wsjson()` wrap output for WebSocket descriptors and fall back to `args[1]` otherwise.

### 7. Full Color Table, `%k`, `align()` `(ansi)` & Smart HTML Color Conversion
- **Plan**:
  1. Populate the full PennMUSH X11/CSS `named_colors` table in `TextFormatFunctions.cs`.
  2. Implement `%k` color-code substitution in `ExpressionEvaluator.cs`, ANSI validation in `@moniker`, and `(ansi)` column color application in `StringFunctions.Align`.
  3. **Smart HTML Client Color Conversion**: Add an ANSI/markup-to-HTML converter in the output pipeline that detects HTML-capable clients (via `ConnectionSnapshot` / WebSocket capabilities) and converts ANSI SGR sequences (16-color, `xterm256`, and 24-bit `#rrggbb` RGB) into full semantic HTML (`<span style="color:...;background-color:...">` / formatting tags) with proper HTML escaping.

### 8. Centralized Non-Error Stock Wording Catalog
- **Plan**:
  1. Audit all inline non-`#-1` player-facing strings across `PennMUSH.Core/Commands/` and `PennMUSH.Core/MUSHcode/Functions/` and centralize them into a catalog beside `MushError.cs`.
