# Commands: Building & World

Room/object creation, linking, destruction, ownership, hierarchy, quotas, inspection/search, and decompilation commands interrogated from `PennMUSH.Core/Commands/Handlers/`.

## Quick Reference

| Command | Aliases | Restrictions | Notes |
|---|---|---|---|
| `@dig` | — | `noguest` *(in `restrict.cnf`)*, Quota (`1–3`) | `/teleport`, `/tel`; `<name>[=<exit_to>[,<exit_back>]]` |
| `@open` | — | `noguest`, Quota (`1`) | Opens exit in current room (`=<dest>`) |
| `@link` | — | `noguest`, Owner/Wizard | Links `<exit>=<dest>` |
| `@unlink` | — | `noguest`, Owner/Wizard | Unlinks `<exit>` (`Dbref.Nothing`) |
| `@create` | — | `noguest`, Quota (`1`) | `/silent`; creates a `Thing` |
| `@destroy` | `@recycle`, `@nuke` | `@destroy noplayer` *(in `restrict.cnf`)*, Owner/Wizard | `/override` (`@nuke` bypasses `SAFE`); marks `GOING`, frees if already `GOING` & refunds quota |
| `@purge` | — | `Wizard` | Frees all `GOING` objects `#3+` and refunds non-player quota |
| `@undestroy` | `@unrecycle` | Owner/Wizard | Clears `GOING` flag if not yet garbage |
| `@clone` | — | Quota (`1`) | Copies flags, attributes, locks & home into current room |
| `@teleport` | `@tel` | — | Teleports `me` or `<target>` to `<dest>` & triggers arrival look for players |
| `@name` | — | — | Renames `<target>=<new_name>` |
| `@moniker` | — | Owner/Wizard (`Controls`) | Sets sanitized ANSI `MONIKER` or clears on `=` |
| `@parent` | — | `noguest`, Owner/Wizard | Sets multiple parents (`<obj>=<p1> <p2>...` or `none`), cycle-checked |
| `@zone` | — | Owner/Wizard | Sets multiple zones (`<obj>=<z1> <z2>...` or `none`) |
| `@chzone` | — | `noguest`, Controls target + `Chzone` lock | `/preserve` (Wizard keeps privileges) |
| `@chzoneall` | — | `Wizard` | `/preserve`; moves all objects owned by `<player>` to `<zone>` |
| `@chown` | — | `noguest`, Owner/Wizard/`CHOWN_OK`, Quota | Transfers non-player ownership atomically with `RQUOTA` |
| `@chownall` | — | `Wizard` | `/things`, `/rooms`, `/exits`, `/preserve`; bulk ownership transfer |
| `@firstexit` | — | Controls room | Reorders `<exit>` to front of room's exit list |
| `@entrances` | — | — | `/exits`, `/things`, `/players`, `/rooms` |
| `@find` | — | — | Searches objects (`[<name>][=<low>-<high>]`) you own (or all for Wizard) |
| `@whereis` | — | — | Reports player's room; notifies target unless executor is Wizard |
| `@search` | — | — | Criteria search (`owner`, `type`, `name`, `zone`, `parent`, `flags`, `powers`, `eval`, `e*`, `elock`, `command`, `listen`) |
| `@scan` | — | — | `/room`, `/self`, `/zone`, `/globals`; lists matching `$`-commands |
| `@sweep` | — | — | `/connected`, `/here`, `/inventory`, `/exits`; lists listeners & puppets |
| `@decompile` | — | `CanExamine` (for header/flags) | `/tf`, `/db`, `/name`, `/flags`, `/attribs`, `/skipdefaults`, `=<prefix>` |
| `@quota` | — | `CanSee` (`Quotas`/`See_All`/`Royalty`) | `/all`, `/set`; views or sets `RQUOTA` building limits |
| `@squota` | — | `Wizard` | Sets player quota (`[+|-]<n>`) |
| `@allquota` | — | `God` | `/quiet`; reports or updates quotas across all players |

---

## Creating, Linking & Destroying Objects

### `@dig[/teleport] <room_name>[=<exit_to>[,<exit_back>]]`
`Aliases: /tel switch alias for /teleport` · `Restrictions: noguest (in game/restrict.cnf)`

Deducts `1` quota for the room plus `1` for each requested exit (up to `3` total via `QuotaRules.TryPay`), creates the room, optionally attaches exits to/from the executor's current location, and moves the executor in when `/teleport` (or `/tel`) is passed (`DigCommandHandler.cs`).

### `@open <exit_name>[=<dest>]` & `@link <exit>=<dest>` & `@unlink <exit>`
- **`@open`** (`noguest`): Pays `1` quota, attaches a new `Exit` to the current room, and optionally links it to `<dest>` (`OpenCommandHandler.cs`).
- **`@link`** (`noguest`, Owner/Wizard): Links an `Exit` to a valid destination dbref (`LinkCommandHandler.cs`).
- **`@unlink`** (`noguest`, Owner/Wizard): Unlinks `<exit>` back to `Dbref.Nothing` (`UnlinkCommandHandler.cs`).

### `@create[/silent] <name>`
`Restrictions: noguest (in game/restrict.cnf)`

Pays `1` quota and creates a `Thing` (defaults name to `"Thing"` if empty); `/silent` suppresses creation confirmation (`CreateCommandHandler.cs`).

### `@destroy[/override] <target>` / `@recycle` / `@nuke`
`Aliases: @recycle, @nuke` · `Restrictions: @destroy noplayer (in game/restrict.cnf)`

Marks `<target>` (cannot destroy `#0`, `#1`, or `#2`) with `GOING`, or immediately frees it (`DestroyCommandHandler.Free`) and refunds `1` quota for non-players if `GOING` is already set. Non-safe check requires `@nuke` or `/override` when `SAFE` is set (`ObjectLifecycleCommandHandlers.cs`).

### `@purge` & `@undestroy <target>`
- **`@purge`** (**Wizard**): Permanently frees every `#3+` object marked `GOING` and refunds quota (`PurgeCommandHandler`).
- **`@undestroy`** (`Alias: @unrecycle`, Owner/Wizard): Clears `GOING` on a queued-for-destruction non-garbage object (`UndestroyCommandHandler`).

### `@clone <target>`
Pays `1` quota, refuses players (`"You cannot clone players."`), duplicates `Name`, `Type`, `Home`, `Flags`, `Attributes`, and `Locks` into executor's current location (`CloneCommandHandler`).

---

## Ownership, Hierarchy, Zones & Naming

### `@chown <object>=<player>` & `@chownall[/<switches>] <old_owner>[=<new_owner>]`
- **`@chown`** (`noguest`, Owner/Wizard, or self-chown of `CHOWN_OK` object): Atomically transfers non-player quota via `QuotaRules.TryTransfer` (Wizards allow overdraft) and clears `CHOWN_OK` when self-chowned (`ChownCommandHandler`).
- **`@chownall`** (**Wizard**, `ChownAllCommandHandler.cs`): Bulk-transfers owned `Thing`/`Room`/`Exit` objects (filtered by `/things`, `/rooms`, `/exits`), adjusts `RQUOTA` delta, and resets `WIZARD`/`ROYALTY`/`TRUST`/powers while setting `HALT` unless `/preserve` is passed (even with `/preserve`, strips `WIZARD` when recipient is non-Wizard).

### `@teleport <target>[=<dest>]` & `@name <target>=<new_name>` & `@firstexit <exit>`
- **`@teleport`** (`Alias: @tel`): Moves `<target>` (or `me` when no `=` given) to `<dest>` and triggers `LookKind.Automatic` for players (`TeleportCommandHandler.cs`).
- **`@name`**: Updates `<target>.Name` (`NameCommandHandler.cs`).
- **`@firstexit`**: Re-attaches `<exit>` to the head of its source room's exit linked list (`FirstExitCommandHandler`).

### `@moniker <object>[=<colored_text>]`
Controls `<object>` (Owner/Wizard). Without `=`, displays current `MONIKER` (or `"No moniker set."`). With empty RHS, clears `MONIKER`. Otherwise evaluates `<colored_text>`, strips non-SGR/MUjs/underline/blink codes via `AnsiColorTable.SanitizeMoniker`, requires at least one remaining ANSI sequence, and stores `MONIKER` unless locked (`MonikerCommandHandler`).

### `@parent <object>[=<p1> <p2>...|none]`
`Restrictions: noguest`

Sets one or more ordered parent dbrefs (`ParentCommandHandler.cs`), checking every edge via `MUSHObject.WouldCauseParentCycle()`. Empty RHS or `none` clears all parents.

### `@zone`, `@chzone[/preserve]`, `@chzoneall[/preserve]`
- **`@zone <obj>[=<z1> <z2>...|none]`**: Sets the object's `Zones` set (Owner/Wizard) (`ZoneCommandHandler.cs`).
- **`@chzone[/preserve] <obj>=<zone|none>`** (`noguest`): Controls `<obj>`, checks `ZoneOperations.WouldLoop()`, requires controlling `<zone>` or passing `LockType.Chzone`, resets `WIZARD`/`ROYALTY`/`TRUST` & powers on non-players unless `Wizard` + `/preserve`, and ensures default `Zone` lock (`ChzoneCommandHandler`).
- **`@chzoneall[/preserve] <owner>=<zone|none>`** (**Wizard**): Applies zone move across all objects owned by `<owner>` (`ChzoneAllCommandHandler`).

---

## Quotas, Searching & Decompilation

### `@quota`, `@squota`, `@allquota`
Backed by `QuotaRules` (`QuotaCommandHandlers.cs`) and `RQUOTA` (`MDark | Locked`):
- **`@quota [<player>]`**: Displays `<owned>` / `<limit>` (or `Unlimited` when `!use_quota`, `Royalty+`, `NO_QUOTA` flag, or `No_Quota` power). Requires `CanSee` (`Quotas`/`See_All`/self).
- **`@quota/all`** & **`@allquota[/quiet] [<limit>]`** (**God only**): Reports or sets quotas across all `Player` objects.
- **`@quota/set`** & **`@squota <player>=[+|-]<amount>`** (**Wizard**): Sets absolute quota limit or relative delta (`+N`/`-N`), clamped at `owned`.

### `@find`, `@whereis`, `@entrances`, `@search`, `@scan`, `@sweep`
- **`@find [<name>][=<low>-<high>]`**: Lists objects matching substring and optional dbref range owned by caller (all objects for Wizard) (`FindCommandHandler`).
- **`@whereis <player>`**: Reports `<player>`'s location room; notifies `<player>` `"<actor> is looking for you."` unless executor is Wizard or targeting self (`WhereIsCommandHandler`).
- **`@entrances[/exits|/things|/players|/rooms] [<target>]`**: Lists objects whose `Home` equals `<target>` or exits whose destination equals `<target>` (`EntrancesCommandHandler`).
- **`@search [<owner>] [<min>] [<max>][ <class>=<val>, ...]`** (`SearchCommandHandler`): Filters by `owner` (`all` or player), dbref range, `type`, `name`, `zone`, `parent`, `flags`, `lflags`, `powers`, `exits`/`things`/`objects`/`rooms`/`players`, `mindb`/`maxdb`/`start`/`count`, `elock` (`LockParser`), `command` (`$`-commands), `listen` (`LISTEN` / `^`-listeners), and evaluator expressions `eval`, `eplayer`, `eroom`, `eexit`, `ething`/`eobject` (`##` substituted with `#<id>`). (`eval`/`e*` require an `ExpressionEvaluator`, otherwise returning `"That search is not available."`.)
- **`@scan[/room][/self][/zone][/globals] <text>`**: Lists `$pattern:` attributes matching `<text>` across room/contents, carried inventory/self, personal/location zone, and `master_room` (`ScanCommandHandler`).
- **`@sweep[/connected|/here|/inventory|/exits]`**: Reports players, `PUPPET`s, `$`-command objects, and `AUDIBLE` exits/rooms listening nearby (`SweepCommandHandler`).

### `@decompile[/<switches>] <target>[/<pattern>][=<prefix>]`
Reconstructs MUSHcode creation, link/home, zone, parent, lock (`@lock` + `@lset`), flag, power, and attribute statements for `<target>` (`DecompileCommandHandler.cs`).

| Switch / Option | Behavior |
|---|---|
| `/db`, `/name` | Refers to `<target>` by dbref (or by name; `/name` overrides `/db`) |
| `/tf` | FugueEdit mode: prepends `TFPREFIX` (default `"FugueEdit > "`), uses dbref, omits attribute-flag lines |
| `/flags` | Outputs object header/create/link/zone/parent/locks/flags/powers |
| `/attribs` | Outputs attributes only (or match `/<pattern>` on target) |
| `/skipdefaults` | Omits `@set` flag lines that match `StandardAttributes.DefaultFlags` |
| `/prefix` or `=<prefix>` | Prepends text after `=` to every output line (`MONIKER` uses `@moniker`; multiline/tab/leading-trailing-space values use `[space()]/%r/%t` decomposition) |
