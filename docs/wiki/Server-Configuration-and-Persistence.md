# Server Configuration & Persistence

Reference for configuration files, runtime options, HTTP endpoints, text/help caching, SQLite persistence schemas, and deployment.

---

## Startup Discovery & Config Files

### Config Search Order (`MUSHServer.ResolveConfigPath`)
If no explicit `configPath` is passed to `MUSHServer`, it checks in order:
1. `<AppBase>/../../../../game/mush.cnf`
2. `./game/mush.cnf`
3. `./mush.cnf`
4. `<AppBase>/game/mush.cnf`
5. `<AppBase>/mush.cnf`

`GameDirectory` defaults to the directory containing the resolved `mush.cnf`. On first startup, `MUSHServer.EnsureDefaultConfigFiles` copies any missing `.cnf` files (`mush.cnf`, `alias.cnf`, `names.cnf`, `restrict.cnf`) from their `.dst` templates without overwriting existing `.cnf` files.

### Config Directives & Included Files (`MushConfigParser.cs`)
`mush.cnf` supports `include <file>` directives with cycle prevention. By default, `game/mush.cnf` includes:
- `game/alias.cnf` — **21 `command_alias`** rules, **18 `function_alias`** rules, `reserve_alias` entries.
- `game/restrict.cnf` — **21 `restrict_command`** rules and **1 `restrict_function`** rule (using `nobody`, `noguest`, `nofixed`, `nogagged`, `god`, `wizard`, `admin`, and optional quoted custom denial messages).
- `names_file` (`names.cnf`) & `restrict_name` — name restriction patterns.

---

## Configuration Catalog (`ConfigCatalog` & `@config`)

Inspectable via `@config`, `@config/<group>`, `@config <option>`, and `config(<option>)`; settable at runtime by Wizards with `@config/set` or toggled via `@enable` / `@disable`:

| Group | Visibility | Stored Options |
|---|---|---|
| `cosmetic` | All | `money_singular` (`"Penny"`), `money_plural` (`"Pennies"`), `tiny_trim_fun` (`No`) |
| `db` | All | `base_room` (`#0`), `default_home` (`#0`), `http_handler` (`#-1`), `master_room` (`#2`), `player_start` (`#0`), `system_object` (`#3`) |
| `files` | Royalty+ | `chat_database` (`"data/chatdb"`), `crash_database` (`"data/PANIC.db"`), `dict_file` (`""`), `input_database` (`"data/indb"`), `mail_database` (`"data/maildb"`), `output_database` (`"data/outdb"`), `sql_database` (`""`) |
| `limits` | All | `max_guests` (`10`), `max_players` (`1000`), `paycheck` (`50`), `starting_money` (`100`), `starting_quota` (`20`), `use_quota` (`Yes`) |
| `messages` | Royalty+ | `colors_file`, `connect_file`, `down_file`, `full_file`, `guest_file`, `motd_file`, `newuser_file`, `quit_file`, `register_create_file`, `wizmotd_file` |
| `net` | All | `guests` (`Yes`), `ip_addr` (`""`), `mud_name` (`"PennMUSH"`), `mud_url` (`""`), `player_creation` (`Yes`), `port` (`4201`), `ssl_port` (`4202`), `use_dns` (`Yes`), `use_ws` (`Yes`), `ws_url` (`""`) |

**`@enable` / `@disable` Toggleable Options (`MushConfiguration.TrySetSwitch`)**: `use_ws`, `use_dns`, `player_creation`, `guests`, `use_quota`, `tiny_trim_fun`.

**`@config/save` Persistence (`config_save`)**: God-only `@config/save` persists changes back to `mush.cnf` via `MushConfigWriter` only when `config_save yes` is set in `mush.cnf` (default `no`). `config_save` cannot be modified in-game via `@config/set`, `@config/save`, or `@enable`/`@disable`.

*(Other directives present in stock `game/mush.cnf` are preserved in `MushConfiguration.RawDirectives` and `Directives` for `{{directive}}` screen-token expansion.)*

---

## Screen Files & Shared `ITextFileCache` (`@readcache`)

- **Connect / MOTD / HTML Screens** (`GameScreenFiles.cs`): WebSocket connections check `game/html/<name>.html` before falling back to `game/txt/<name>.txt`. Screen files expand `{{directive}}` placeholders (HTML-encoded for `.html` WebSocket clients).
- **`ITextFileCache` / `TextFileCache`** (`PennMUSH.Core/Help/TextFileCache.cs`): Loads disk categories from `help_command` / `ahelp_command` directives (defaulting `help`, `news`, admin-gated `ahelp`, `anews`, plus optional categories like `events`) and supports `textfile()` / `textentries()` lookups with admin-only gating.
- **`@readcache`** (`Wizard`): Reloads `HelpStore`, `TextFileCache`, `SuggestionVocabulary` (syncing non-admin cached categories plus `dict_file` into `WORDS`), and reports byte sizes via `ScreenFileReport`.

---

## SQLite Persistence Architecture

PennMUSH .NET stores all durable state across up to three SQLite databases (using `Microsoft.Data.Sqlite`):

### 1. Core World & Runtime DB (`mush.db`)
Managed by `SqliteStore` (**7 tables**) and `RuntimeStateStore` (**9 tables**) = **16 tables total**:
- **`SqliteStore`**: `objects` (incl. `warnings`), `attributes`, `flags`, `powers`, `parents` (with `priority`), `zones`, `cron_jobs`.
- **`RuntimeStateStore`**: `channels`, `channel_members`, `channel_locks`, `mail_messages`, `mail_active_folders`, `mail_aliases`, `mail_alias_members`, `soft_functions`, `sitelocks`.

Persisted explicitly by `@dump`, `@reboot`, `POST /reboot`, `POST editor.save`, and `Stop()` / `@shutdown`.

### 2. Connection Log DB (`connlog.db`)
Managed by `ConnectionLogStore` (`GameDirectory/connlog.db`):
- Table `connlog` (`id`, `dbref`, `name`, `ipaddr`, `hostname`, `conn`, `disconn`, `reason`, `ssl`, `websocket`, indexed on `dbref` and `conn`).
- Recorded on socket connect, updated on `connect`/`create` login, closed on disconnect/shutdown, and queried by `connlog()`, `connrecord()`, `addrlog()`, and `@sitelock/player`.

### 3. Optional SQLite SQL DB (`sql_database`)
Managed by `SqlDatabase` (`PennMUSH.Core/Database/SqlDatabase.cs`):
- Opened at startup when `sql_database` is set to a non-empty path in `mush.cnf` (relative to `GameDirectory` or rooted).
- Powers `@sql`, `@mapsql`, `sql()`, `mapsql()`, and `sqlescape()`.

---

## HTTP Server, Dynamic `@http` & Hot-Reboot

Kestrel endpoints in `PennMUSH.Server/Program.cs` (default `http://0.0.0.0:5119` / `https://0.0.0.0:7069` from `launchSettings.json`) + `TelnetListenerService` on TCP port `4201`:

- `GET /health` — JSON status (`status`, `version`, `objects`, `connectedSessions`, `channels`).
- `POST /reboot` — Triggers zero-disconnect `MUSHServerHost.Reboot()`.
- `GET /html/stylesheet.css` — Serves screen stylesheet CSS.
- `/ws`, `/game`, `/api` — WebSocket endpoints (see [MUjs Protocol Reference](MUjs-Protocol-Reference)).
- **Fallback Softcode HTTP (`HttpChannel` / `http_handler`)**: Unmatched HTTP routes invoke attribute `<METHOD>` (`GET`, `POST`, etc.) on the configured `http_handler` player object, passing path (`%0`), body (`%1`), and headers in `%q<HEADERS>` / `%q<HDR.*>`. The handler shapes headers/status with `@respond` and emits response body lines.

---

## Self-Hosted CI/CD Deployment

`.github/workflows/deploy-local.yml` automates local deployment on pushes to `main`:
1. Fetches and pulls `main` on the `[self-hosted, pennmush-dotnet]` runner.
2. Stops running `PennMUSH.Server.exe` / `dotnet *PennMUSH.Server*` processes to release binary locks.
3. Rebuilds `PennMUSH.Server` (and `PennMUSH.SpaceEngine` if present alongside).
4. Relaunches `PennMUSH.Server` and checks `http://localhost:5119/health`.
