# Port order for the remaining commands

`unimplemented-commands.md` names the player commands that still have no registration, plus `@poll`, which is registered on the `doing` handler and only sets `DOING`. This file is the order to port them in. A command belongs early when the data it reads, and the handler it most resembles, already exist. Each later tier adds one foundation and then the commands that need it.

Tier 0 is done. `semipose`, `@select`, `@skip`, `@elock`, `@ulock`, `@eunlock`, `@uunlock`, `@undestroy`, and `@unrecycle` are registered. The next batch is tier 1, the commands that only read or write state an object already has.

## What already exists

These are the pieces a new command can call without a new subsystem:

- Speech and movement: `say`, `pose` (`semipose`, `;`, and `/nospace` omit the space), `@emit`, `@pemit`, `@remit`, `@oemit`, `@cemit`, `whisper`, `page`, `go`, `get`, `drop`, `enter`, `leave`, `give`, `home`, `@teleport`.
- Objects: attributes (`SetAttribute`, `GetAttribute`, `ClearAttribute`, parent inheritance, a creator dbref, and attribute flags that are stored), `@chown`, `@name`, `@parent`, `@wipe`, `@clone`, `examine`.
- Locks: `@lock`, `@unlock`, `@elock`, `@ulock`, `@eunlock`, and `@uunlock`. `LockType` names Basic, Enter, Use, Page, Leave, Drop, Give, and Teleport. `EvalLock` can test a key. Movement, get, drop, give, and page do not call it yet. `@lock/enter` still stores the switch text as typed, so a later check for `LockType.Enter` (`"Enter"`) misses a lock stored as `"enter"`. The named enter and use commands store the canonical type.
- Flow and queue: `@if` / `@ifelse`, `@skip` (the test reversed), `@switch` (first match unless `/all`), `@select` (always the first match), `@dolist`, `@trigger` (one evaluated result, dispatched once), `@wait`, `@notify` (wakes one waiter), `@halt`, `@allhalt`.
- World data: zones as a set of dbrefs, pennies, contents and exits as chains linked by `Next`, `GOING` set by `@destroy` and cleared by `@undestroy` / `@unrecycle`. `ObjectDatabase.DestroyObject` exists and is not called. SQLite saves objects, attributes, flags, powers, parents, zones, pennies, and cron after a command whose name starts with `@`.
- Connections: login `WHO`, an in-memory password dictionary, `DOING`. Passwords are not saved, and the God character has no password entry.
- Channels, mail, help files, and the connect texts (`motd`, wizard motd) exist in memory or on disk. Channel membership and mail are not saved.

## Tier 1 — read or write state the object already has

No new table and no new field. Persistence for attribute edits comes from the existing attribute save. This is the next batch.

| Order | Command | Depends on |
| --- | --- | --- |
| 1 | `@cpattr` | Read one attribute, write it onto each target. Copy the stored flags. |
| 2 | `@mvattr` | `@cpattr`, then clear the source. |
| 3 | `@edit` | Replace text inside one attribute. `/regexp` can wait until the plain replace works. |
| 4 | `@grep` | Read attributes on one object and print matches. |
| 5 | `@atrlock` | Set or clear `AttributeFlags.Locked` and report it. Enforcing the flag on later `@set` belongs with this command, because the flag is already stored and currently ignored. |
| 6 | `@atrchown` | Change the attribute creator. The same operation as `@chown` on one attribute. |
| 7 | `@lset` | Change flags on a lock `SetLock` already stores. The lock key stays as it is. |
| 8 | `@decompile` | Print an object's flags and attributes back as commands. `examine` already gathers that data. |
| 9 | `@moniker` | Set or clear the `MONIKER` attribute. ANSI checking waits until color codes exist; until then the value is stored as text. `%k` stays unimplemented. |
| 10 | `@message` | Evaluate one attribute and send it through `@pemit`, `@remit`, or `@oemit`. |
| 11 | `@prompt` | `@pemit` with no trailing newline. This adds a notify flag on the connection write. |
| 12 | `@lemit` | `@emit` aimed at the executor's location. |
| 13 | `@find` | Names of objects the executor controls. Optional dbref range on the right. |
| 14 | `@whereis` | A player's location. Also tell the target. |
| 15 | `brief` | `examine` with contents and the long attribute dump omitted. |
| 16 | `@drain` | The `@wait` semaphore list. `@notify` wakes one waiter; `@drain` drops them. |
| 17 | `@dump` | The existing SQLite save, on demand. |
| 18 | `@uptime` | Process start time. One timestamp recorded at startup. |
| 19 | `@poor` | Set every player's pennies. God only. Quota is not involved. |
| 20 | `@list` | Print the command table, flag table, power names, and builtin functions. Skip list types that have no table yet. |
| 21 | `@entrances` | Objects whose home or link points at a place. Type switches only filter the walk. |
| 22 | `@firstexit` | Move one exit to the front of the room's exit chain. |

## Tier 2 — one behavior added to a system that already runs

Each block starts with the foundation. The commands under it come after that foundation, in the listed order.

### Locks actually get tested

`EvalLock` exists. `enter`, `leave`, `get`, `drop`, `give`, `page`, and `go` need to call it for the matching `LockType`. Until they do, a lock command only stores a key.

| Order | Command | Depends on |
| --- | --- | --- |
| 23 | `use` | Use lock, then the `USE` / `OUSE` / `AUSE` attributes, or `RUNOUT` when the lock fails. |
| 24 | `empty` | For each visible content, the same path as `get` and `drop`. |
| 25 | `teach` | Speech lock, a room line of the form "types --> …", then queue the taught text. |

### Nospoof on the emit path

`@emit` and the other room and player emits send the text unchanged. Nospoof means those emits gain a speaker prefix when the player lacks the spoof power. `Can_spoof` is already a power name and is not checked.

| Order | Command | Depends on |
| --- | --- | --- |
| 26 | `@nsemit`, `@nsoemit`, `@nsremit`, `@nspemit`, `@nslemit`, `@nsprompt`, `@nscemit` | The matching emit handler with nospoof forced. `@nsprompt` also needs `@prompt`. `@nslemit` also needs `@lemit`. |
| 27 | `@nszemit` | `@zemit`, below. |

### Zones get a player-facing change and a broadcast

`@zone` already stores zone dbrefs. A walk of "objects in this zone" is the missing helper.

| Order | Command | Depends on |
| --- | --- | --- |
| 28 | `@chzone` | Set one object's zone through that helper, after the chzone lock. |
| 29 | `@chzoneall` | `@chzone` for each object a player owns. |
| 30 | `@zemit` | Emit to objects that share a zone. |
| 31 | `@nszemit` | `@zemit` with nospoof forced. |

### Listeners and nearby commands

`$` command listeners already match on the room, its contents, inventory, and the player.

| Order | Command | Depends on |
| --- | --- | --- |
| 32 | `@scan` | Report those listeners. Switches only choose which set is scanned. |
| 33 | `@sweep` | Report listeners, puppets, and connected players in the room, inventory, or exits. |
| 34 | `with` | Run the command against one nearby object's listeners. `/room` uses the zone and the master room. |
| 35 | `@search` | The same database walk as `@entrances`, plus the right-hand criteria. |

### Destroy finishes what `@destroy` starts

`@destroy` sets `GOING` and leaves the object in the database. `@undestroy` clears that flag. Freeing the object is still separate.

| Order | Command | Depends on |
| --- | --- | --- |
| 36 | `@purge` | Call `DestroyObject` on objects already marked `GOING`. |
| 37 | `@nuke` | `@destroy` that frees an object immediately when it is already `GOING`. |

`@dbck` stays in tier 4. A consistency pass is misleading while locks, mail, channels, and passwords are still unsaved.

### The queue can be seen and nudged

| Order | Command | Depends on |
| --- | --- | --- |
| 38 | `@ps` | List `ActionQueue` entries. |
| 39 | `@kick` | Run the next N queued commands on the wizard's demand. |

`@assert`, `@break`, `@retry`, and `@verb` need an action list that can stop or restart. `@trigger` today runs a single dispatched command.

### Who is connected

Login already builds a `WHO` line. The in-game command is that list plus `DOING`.

| Order | Command | Depends on |
| --- | --- | --- |
| 40 | `who` | The connection list, with `DOING`. |
| 41 | `session` | `who`, plus host and idle when the connection records them. |
| 42 | `@hide` | A hidden bit on the connection that `who` and `session` skip. |
| 43 | `@boot` | Drop one connection. The `Boot` power is already named. |
| 44 | `@poll` | Send one line to every connection and keep a reply slot per connection. This replaces the current `@poll` registration on `doing`. `doing` stays the `DOING` command. |

### Channel names that map onto `@channel`

`ChannelRegistry` already joins, leaves, sets a title, and lists members.

| Order | Command | Depends on |
| --- | --- | --- |
| 45 | `addcom`, `delcom` | Join and leave, and remember the player's channel alias. |
| 46 | `comtitle` | The membership title `@channel` already stores. |
| 47 | `comlist`, `@clist` | The channel list. `/full` is the longer listing. |

`@clock` and `@restart` stay later. `@clock` is channel locks. `@restart` reloads channel runtime state. It does not reboot the process. `@reboot` remains the hot reload.

### Text files the server already reads on connect

| Order | Command | Depends on |
| --- | --- | --- |
| 48 | `@motd`, `@listmotd`, `@wizmotd`, `@rejectmotd` | The connect and wizard text files already sent at login. |
| 49 | `@suggest` | The same file read and write, for the suggestion file. |
| 50 | `@readcache` | Reload help files and those connect texts. |

## Tier 3 — one new field or table, then the commands on it

### Followers

A follower list on the object, updated from `go`, `@teleport`, `enter`, and `leave`.

| Order | Command | Depends on |
| --- | --- | --- |
| 51 | `follow` | Add a follower, or list them when no name is given. |
| 52 | `unfollow` | Leave one leader, or everyone when no name is given. |
| 53 | `dismiss` | Remove people following you. |
| 54 | `desert` | Clear both directions. |

### Quota

A quota number on the player, saved with the object.

| Order | Command | Depends on |
| --- | --- | --- |
| 55 | `@quota` | Show the count and the limit. `/set` writes one player's limit. |
| 56 | `@squota` | `@quota/set`. |
| 57 | `@allquota` | `@quota` for every player. |
| 58 | `@chownall` | `@chown` across one player's things, rooms, and exits, then adjust both players' quotas. |

### Action lists

A multi-line attribute must run as a sequence of commands in one queue frame. `@include` is that runner.

| Order | Command | Depends on |
| --- | --- | --- |
| 59 | `@include` | Run one attribute inplace as that list. |
| 60 | `@assert` | A false check sets the break flag. |
| 61 | `@break` | A true check sets the same flag. |
| 62 | `@retry` | A true check reruns the frame. The right-hand side replaces `%0` through `%9`. |
| 63 | `@verb` | The verb string is visible to the action list. |

### Passwords that survive a restart

| Order | Command | Depends on |
| --- | --- | --- |
| 64 | `@password` | Check the old password, write a new hash. |
| 65 | `@newpassword` | Wizard write of that hash. |
| 66 | `@pcreate` | The existing player-creation path, plus a password. |

### Commands and functions as editable tables

| Order | Command | Depends on |
| --- | --- | --- |
| 67 | `@function` | Add, alias, restrict, enable, disable, and delete a global soft function. |
| 68 | `@disable`, `@enable` | An enabled bit on a command. |
| 69 | `@command` | Add, alias, delete, and restrict a command. |
| 70 | `@hook` | Before, after, ignore, override, and extend slots around dispatch. |

### Definitions, warnings, mail aliases, config, and vendors

| Order | Command | Depends on |
| --- | --- | --- |
| 71 | `@flag` | Create and edit flag definitions. Builtin flags already exist. |
| 72 | `@attribute` | An attribute-definition table. Per-object attributes already exist. |
| 73 | `@warnings` | A warning bitset stored on the object. |
| 74 | `@wcheck` | Those bits, then a topology check of what the player owns. |
| 75 | `@malias` | A mail-alias table beside `MailStore`. This is not a channel command. |
| 76 | `@config` | Read the options `MushConfiguration` already parsed. `/set` changes memory only. |
| 77 | `buy` | A vendor's `PRICELIST` attribute, a penny transfer through the same path as `give`, and the Pay lock from tier 2. |

## Tier 4 — wizard tools that need the earlier model to stay still

| Order | Command | Depends on |
| --- | --- | --- |
| 78 | `@dbck` | A full consistency walk: locations, homes, zones, owners, and `GOING` objects. |
| 79 | `@shutdown` | Save, then stop the process. The existing `@reboot` command remains the hot engine reload. |
| 80 | `@sitelock` | A ban table checked by login. |
| 81 | `@sockset` | Options on one connected descriptor. |
| 82 | `@log`, `@logwipe` | A wizard log file. |
| 83 | `@clock` | Join, speak, see, hide, and modify locks on a channel. |
| 84 | `@restart` | Rebuild channel runtime state. This is not a server restart. |

## Tier 5 — subsystems the process does not have

| Order | Command | Subsystem |
| --- | --- | --- |
| 85 | `@sql` | A SQL connection for wizards. |
| 86 | `@mapsql` | `@sql`, plus one attribute call per row. |
| 87 | `@http` | An outbound HTTP client. |
| 88 | `@respond` | A pending inbound HTTP request to answer. |
| 89 | `@slave` | An external info or SSL slave process. |

## Commands that wait on a foundation

| Command | Wait for |
| --- | --- |
| `@assert`, `@break`, `@retry`, `@verb` | `@include` and the action-list frame. |
| `follow`, `unfollow`, `dismiss`, `desert` | The follower list and the movement hook. |
| `buy` | Pay-lock checks and penny transfer. |
| `@chownall` | Quota. |
| `@nszemit` | `@zemit`. |
| `@nsprompt`, `@nslemit` | `@prompt` and `@lemit`, then nospoof. |
| `@poll` | Connection replies. `doing` stays the `DOING` command. |
| `@restart` | Channel reload. It does not replace `@reboot` or `@shutdown`. |
| `@clock` | Channel locks. It is not a time command. |
| `@malias` | Mail aliases. It is not `comlist`. |
| `@sql`, `@mapsql`, `@http`, `@respond`, `@slave` | Tier 5. |
