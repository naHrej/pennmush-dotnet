# Commands: Server Admin & Extensions

Server configuration, runtime schema management, database maintenance, logging, HTTP/SQL side-effects, shutdown/hot-reboot, and PennMUSH .NET extension commands interrogated from `PennMUSH.Core/Commands/Handlers/`, `PennMUSH.Server/Commands/RebootCommandHandler.cs`, and `PennMUSH.Server/Engine/MUSHServer.cs`.

## Quick Reference

| Command | Aliases | Restrictions | Switches / Notes |
|---|---|---|---|
| `@config` | — | `Royalty` (`files`/`messages`), `Wizard` (`/set`), `God` (`/save`) | `/set`, `/save`, `/lowercase` |
| `@enable` | — | `Wizard`, `nogagged` | Enables `use_ws`, `use_dns`, `player_creation`, `guests`, `use_quota` |
| `@disable` | — | `Wizard` | Disables `use_ws`, `use_dns`, `player_creation`, `guests`, `use_quota` |
| `@flag` | — | `/debug` `Wizard`; mutating switches `God`; queries any | `/list`, `/decompile`, `/add`, `/delete`, `/alias`, `/restrict`, `/disable`, `/enable`, `/letter`, `/type`, `/debug` |
| `@attribute` | — | Mutating switches `Wizard`; queries/`/decompile` any | `/access`, `/decompile`, `/delete`, `/rename`, `/limit`, `/enum`, `/retroactive` |
| `@list` | — | `/allocations` (`Wizard`) | `commands`, `flags`, `powers`, `functions`, `attribs`, `motd`, `locks`, `allocations`, `/lowercase`, `/builtin`, `/local` |
| `@stats` | — | Memory switches `Wizard` | `/tables`, `/chunks`, `/regions`, `/paging`, `/freespace`, `/flags` |
| `@dbck` | — | `Wizard` | Repairs dangling refs & warns on zone/room anomalies |
| `@dump` | — | `Wizard` | Saves database (`/paranoid`, `/debug`, `/nofork`) |
| `@uptime` | — | — | Process start time & elapsed uptime |
| `@version` | — | — | Running server version (`ServerVersion.Text`) |
| `@poor` | — | `God` | Resets every `Player`'s `Pennies` to `<amount>` |
| `@motd` | `@listmotd`, `@wizmotd`, `@rejectmotd` | `Connect`: `Wizard`/`Announce`; `/wizard`/`/down`/`/full`: `Royalty`; `@wizmotd`/`@rejectmotd`: `Wizard` | `/list`, `/clear`, `/wizard`, `/down`, `/full` |
| `@readcache` | — | `Wizard` | Reloads `HelpStore`, `ITextFileCache`, syncs `SuggestionVocabulary` |
| `@suggest` | — | `Wizard` (`/add`, `/delete`); list any | `/add`, `/delete` |
| `@sitelock` | — | `Wizard` | `/ban`, `/register`, `/check`, `/remove`, `/player`, `/name` |
| `@log` | — | `Wizard`, `nogagged` | `/check`, `/cmd`, `/conn`, `/err`, `/trace`, `/wiz`, `/recall` |
| `@logwipe` | — | `God`, `nogagged` + `WizardLog.WipePassword` | `/check`, `/cmd`, `/conn`, `/err`, `/trace`, `/wiz`, `/wipe`, `/rotate`, `/trim` |
| `@sql` | — | `Wizard` or `Sql_Ok` power | Executes SQL query |
| `@mapsql` | — | `Controls` or `LINK_OK` *(not targeting God unless God)* | `/spoof`, `/colnames`, `/notify` |
| `@http` | — | `Wizard` or `Can_HTTP` power, `nogagged`, `noguest` | `/post`, `/put`, `/delete` |
| `@respond` | — | `nogagged` | `/type`, `/header` |
| `@slave` | — | `Wizard` | `/restart` (compatibility stub) |
| `@restart` | — | `nogagged` (`/all`: `Wizard` or `Halt` power) | `/all`; halts queues & runs `STARTUP` on non-`HALT` objects |
| `@shutdown` | — | `Wizard` (`/panic`: `God`) | `/reboot`, `/panic` |
| `@reboot` | — | `Wizard` | Hot reboot preserving connections & simulation state |
| `@cron` | — | `Wizard`, `God`, or `ROYALTY` | `/add` (`/set`), `/del` (`/delete`), `/pause`, `/resume`, `/run`, `/list` |
| `@client` | — | — | `/edit`, `/window`, `/style`, `/audio`, `/sound` |
| `@plugin` | `@extension` | `Wizard` | `/list`, `/load`, `/unload`, `/reload` |

---

## Configuration, Schema & Introspection

### `@config [<group|option>[=<value>]]`
`Switches: /set, /save, /lowercase` · `Source: ConfigCommandHandler.cs`

Views or updates runtime configuration options documented in [Server Configuration & Persistence](Server-Configuration-and-Persistence):
- Bare `@config`, `@config <group>`, or `@config <option>` displays configuration groups/settings; accessing the `files` and `messages` groups requires `Royalty`.
- `@config/set <option>=<value>` requires `Wizard`.
- `@config/save` requires `God` and currently prints `Option set but not saved (Saves disabled.)`.

```text
@config
@config/lowercase
@config/set player_start=#2
```

### `@enable <option>` / `@disable <option>`
`Requires: Wizard (and !GAGGED for @enable)` · `Source: EnableCommandHandler.cs`

Toggles boolean runtime server features on (`@enable`) or off (`@disable`): `use_ws`, `use_dns`, `player_creation`, `guests`, and `use_quota`.

```text
@disable player_creation
@enable use_ws
```

### `@flag[/<switch>] ...`
`Switches: /list, /decompile, /add, /delete, /alias, /restrict, /disable, /enable, /letter, /type, /debug` · `Source: FlagCommandHandler.cs`

Inspects or modifies the server flag table (see [Flags, Powers, Attributes & Locks](Flags-Powers-Attributes-and-Locks)):
- Querying flags, `/list`, and `/decompile` are available to any caller. Does not enforce a `GAGGED` check.
- `/debug` requires `Wizard`.
- All mutating switches (`/add`, `/delete`, `/alias`, `/restrict`, `/disable`, `/enable`, `/letter`, `/type`) require `God`.

### `@attribute[/<switch>] [/retroactive] ...`
`Switches: /access, /decompile, /delete, /rename, /limit, /enum, /retroactive` · `Source: AttributeCommandHandler.cs`

Inspects or modifies standard attribute definitions and access/validation metadata (see [Flags, Powers, Attributes & Locks](Flags-Powers-Attributes-and-Locks)):
- Queries and `/decompile` are available to any caller.
- All mutating switches (`/access`, `/delete`, `/rename`, `/limit`, `/enum`, plus the `/retroactive` modifier) require `Wizard`.

### `@list` / `@stats`
`Source: ObjectReadWriteCommandHandlers.cs, ObjectLifecycleCommandHandlers.cs`

- **`@list[/lowercase][/builtin|/local] <table|type>`**: Inspects runtime `commands`, `flags`, `powers`, `functions`, `attribs`, `motd`, `locks` (`LockType.Names`), and `allocations` (`Wizard`: `GC.GetTotalMemory` + `Environment.WorkingSet`). Supports type as switch (e.g., `@list/locks`) or argument, `/lowercase` across tables, and `/builtin` / `/local` filters on `commands` and `functions`.
- **`@stats[/tables|/chunks|/regions|/paging|/freespace|/flags]`**: Prints total and per-type object counts (`Player`, `Room`, `Exit`, `Thing`, `Garbage`). `/flags` prints per-flag object counts; memory switches (`/tables`, `/chunks`, `/regions`, `/paging`, `/freespace`) require `Wizard` and report .NET GC/process memory statistics.

### `@uptime` / `@version`
`Source: ObjectReadWriteCommandHandlers.cs, ObjectLifecycleCommandHandlers.cs`

- `@uptime`: Displays the current OS process start timestamp (`yyyy-MM-dd HH:mm:ss zzz`) and elapsed uptime (`Xh Ym`, `Xm Ys`, or `Xs`).
- `@version`: Displays the git-stamped release string (`PennMUSH .NET <ServerVersion.Text>`, e.g., `0.1.<build>+g<commit>`).

---

## Database Maintenance, Economy & Caches

### `@dbck`
`Requires: Wizard` · `Source: DbckCommandHandler.cs`

Runs an integrity check and repair pass across the in-memory `ObjectDatabase`:
- Repairs dangling zone, parent, owner, `Next`, creator, location, and exit references.
- Warns object owners on circular zone chains, zone objects missing a `Zone` lock, and disconnected non-`FLOATING` rooms.

### `@dump[/paranoid|/debug|/nofork]`
`Requires: Wizard` · `Source: ObjectReadWriteCommandHandlers.cs`

Triggers a manual database save, forwarding `"paranoid"`, `"debug"`, `"nofork"`, or `"normal"` mode to the save callback and reporting the corresponding completion feedback.

### `@poor <amount>`
`Requires: God` · `Source: ObjectReadWriteCommandHandlers.cs`

Sets `Pennies = <amount>` on every `Player` object in `ObjectDatabase`. `<amount>` must be a non-negative integer (`>= 0`).

```text
@poor 100
```

### `@motd[/list|/clear|/wizard|/down|/full] [<text>]`
`Aliases: @listmotd, @wizmotd, @rejectmotd` · `Source: MotdCommandHandler.cs`

Views, sets, or clears server message-of-the-day banners (`MotdBoard`):
- `/list` (or `@listmotd`) displays active MOTD entries.
- `/clear` clears the targeted MOTD banner.
- Updating the player `Connect` MOTD requires `Wizard` or the `Announce` power.
- `/wizard`, `/down`, and `/full` require `Royalty`; invoking via the `@wizmotd` or `@rejectmotd` alias prefixes requires `Wizard`.

### `@readcache`
`Requires: Wizard` · `Source: ReadCacheCommandHandler.cs`

Prints the `ScreenFileReport`, reloads `HelpStore` and `ITextFileCache`, and resynchronizes `SuggestionVocabulary` from loaded help/command/function entries.

### `@suggest[/add|/delete] ...`
`Source: SuggestCommandHandler.cs`

Manages spellcheck/typo suggestion vocabulary categories used by command and help lookups:
- Bare `@suggest` lists suggestion categories for any caller.
- `/add` and `/delete` require `Wizard`.

### `@sitelock[/ban|/register|/check|/remove|/player|/name] ...`
`Requires: Wizard` · `Source: SiteLockCommandHandler.cs`

Inspects and persists IP/host access rules in `RuntimeStateStore.sitelocks`:
- Supports `/ban`, `/register`, `/check`, `/remove`, and `/player` (cross-referencing `connlog.db`).
- `/name <name>` and `/name !<name>` lock or remove reserved names in `game/names.cnf` (`NameLockFile`).

---

## Logging, SQL & HTTP Side-Effects

### `@log[/check|/cmd|/conn|/err|/trace|/wiz][/recall] ...`
`Requires: Wizard, nogagged` · `Source: LogCommandHandler.cs`

Writes to or recalls recent lines (`/recall`) from the `check`, `cmd`, `conn`, `err`, `trace`, or `wiz` server log streams.

### `@logwipe[/check|/cmd|/conn|/err|/trace|/wiz][/wipe|/rotate|/trim] <password>`
`Requires: God, nogagged` · `Source: LogCommandHandler.cs`

Wipes (`/wipe`), rotates (`/rotate`), or trims (`/trim`) the selected log stream after verifying `<password>` against `WizardLog.WipePassword`.

### `@sql <query>`
`Requires: Wizard or Sql_Ok power` · `Source: SqlCommandHandler.cs`

Executes `<query>` against the configured external SQL connection and outputs formatted results to the caller.

### `@mapsql[/spoof|/colnames|/notify] <object>/<attribute>=<query>`
`Requires: Controls or LINK_OK on <object> (cannot target God unless executor is God)` · `Source: SqlCommandHandler.cs`

Runs `<query>` and evaluates `<object>/<attribute>` for each returned row:
- Passes row column values in `%0`–`%29` and named Q-registers `%q<col>`.
- Supports `/spoof`, `/colnames`, and `/notify`.

### `@http[/post|/put|/delete] <object>/<attribute>=<url>[,<body>]`
`Requires: Wizard or Can_HTTP power, nogagged, noguest` · `Source: HttpCommandHandler.cs`

Performs an outbound HTTP request (`GET` by default, or `/post`, `/put`, `/delete`) and enqueues/evaluates `<object>/<attribute>` with the response:
- Reads optional request metadata from `%q<content-type>` and `%q<userpass>`.
- Passes response body in `%0` and sets `%q<status>` and `%q<content-type>`.

### `@respond[/type|/header] <value>[=<value>]`
`Requires: nogagged` · `Source: HttpCommandHandler.cs`

Configures the HTTP response sent back for inbound Web/HTTP softcode handlers:
- Bare `@respond <code text>`: sets HTTP status (`<3 digits> <text>`, under `40` characters).
- `@respond/type <mime>`: sets response `Content-Type`.
- `@respond/header <name>=<value>`: adds a response header (forbids overriding `content-length`).

---

## Process Lifecycle & Compatibility Stubs

### `@slave[/restart]`
`Requires: Wizard` · `Source: SlaveCommandHandler.cs`

Legacy PennMUSH external DNS/ident slave stub. `@slave/restart` prints `No such service.`; bare `@slave` prints `I'm sorry, Dave...`.

### `@restart[/all] [<object>]`
`Requires: nogagged (/all: Wizard or Halt power)` · `Source: RestartCommandHandler.cs`

Halts queued commands on `<object>` (or all objects with `/all`) and enqueues their `STARTUP` attribute unless flagged `HALT`.

### `@shutdown[/reboot|/panic]`
`Requires: Wizard (/panic: God)` · `Source: ShutdownCommandHandler.cs`

Saves the database and shuts down or reboots the server:
- `/reboot` and `/panic` are matched both in command switches and as `/reboot` / `/panic` argument tokens.
- Saves the database before stopping (`GameErrorDatabaseSaveFailed` wizard broadcast on save error), broadcasts `GameShutdownBy` using the actor's owner name (plus `EmergencyShutdownShutdownPanic` on `/panic`), and halts the server process.
- With `/reboot`, aborts and broadcasts `GameRebootFailed` if saving fails; otherwise broadcasts `GAME: Reboot w/o disconnect by <name>, please wait.` and triggers reboot.

```text
@shutdown
@shutdown/panic
@shutdown/reboot
```

---

## PennMUSH .NET Extensions

Commands beyond traditional PennMUSH 1.8 for zero-downtime restarts, built-in cron scheduling, rich client UI frames, and runtime `.NET 10` plugins.

### `@reboot`
`Requires: Wizard` · `Source: PennMUSH.Server/Commands/RebootCommandHandler.cs`

Initiates a hot reboot preserving live TCP/WebSocket player connections and world simulation state (printing `InitiatingHotReboot...` and invoking the server reboot delegate after a `200 ms` delay). See [Architecture & Concepts](Architecture-and-Concepts).

### `@cron[/<switch>] ...`
`Requires: Wizard, God, or ROYALTY` · `Source: CronCommandHandler.cs`

Manages recurring MUSHcode schedules via `CronScheduler` and `ActionQueue`:
- `@cron` or `@cron/list`: lists configured jobs (`ID`, cron expression, `ACTIVE`/`PAUSED`, target `#dbref`, next UTC run time or `---`).
- `@cron/add <id>=<expr>,<cmd_or_attr>` (alias `/set`): registers or updates a job; if `<cmd_or_attr>` starts with `#` and contains `/` (`<#dbref>/<ATTR>`), resolves `<#dbref>` as target and schedules `@trigger <#dbref>/<ATTR>`.
- `@cron/del <id>` (`/delete`): deletes job `<id>`.
- `@cron/pause <id>` / `@cron/resume <id>`: pauses or resumes job `<id>` and reports its next UTC execution time.
- `@cron/run <id>`: immediately enqueues the job onto `ActionQueue` and updates `LastRun`.

```text
@cron/add backup=0 4 * * *,@dump
@cron/add tick=*/5 * * * *,#10/VA
@cron/pause tick
@cron/run tick
@cron/list
```

### `@client[/edit|/window|/style|/audio|/sound] ...`
`Source: ClientCommandHandler.cs`

Dispatches MUjs client protocol frames for rich web-client code editors (`/edit`), dockable panels (`/window`), styling (`/style`), and audio playback (`/audio`, `/sound`). See [MUjs Protocol Reference](MUjs-Protocol-Reference).

### `@plugin[/list|/load|/unload|/reload] [<path|name>]`
`Alias: @extension` · `Requires: Wizard` · `Source: PluginCommandHandler.cs`

Lists, loads, unloads, and hot-reloads `.NET 10` `PennMUSH.Extensions` assemblies at runtime without restarting the server. See [Plugin Development Guide](Plugin-Development-Guide).

---

Back to [Commands Index](Commands-Index) · [Home](Home)
