# Port order for the remaining commands

`unimplemented-commands.md` names the 75 player commands that still have no registration, plus `@poll`, which is registered on the `doing` handler and only sets `DOING`. This file is the order to port them in. A command belongs early when the data it reads, and the handler it most resembles, already exist. Each later tier adds one foundation and then the commands that need it.

The dot beside each order number is the status of that row.

| Dot | Status |
| --- | --- |
| 🟢 | Finished. The behavior described in the row is in the server. |
| 🔵 | Finished, with a caveat. The row says what is still short of original PennMUSH. There is no cyan dot in the emoji set, so this one is blue. |
| 🟡 | Unfinished. A command of this name is registered, and it is not the original command yet. |
| ⚪ | Not started. There is no gray dot in the emoji set, so this one is white. |
| 🔴 | Blocked. Another row, or a subsystem the server does not have, has to land first. |
| 🟣 | New, and not a PennMUSH command. None of the numbered rows are in this group. |

Rows 1–22 are in the server. Four of them are blue because the original command still does something that row does not. `@poll` (44) is yellow: it is registered on `doing` and only sets `DOING`. The white rows have not been started. The red rows are waiting on the row or the subsystem named in the last column. `enter`, `leave`, `get`, `drop`, `give`, `page`, and `go` test locks. `use` and `empty` are blue, and `teach` is green. The nospoof emits are blue. The next white block is `@purge`.

## What already exists

These are the pieces a new command can call without a new subsystem:

- Speech and movement: `say`, `pose` (`semipose`, `;`, and `/nospace` omit the space), `@emit`, `@pemit`, `@remit`, `@oemit`, `@cemit`, `whisper`, `page`, `go`, `get`, `drop`, `enter`, `leave`, `give`, `home`, `@teleport`.
- Objects: attributes (`SetAttribute`, `GetAttribute`, `ClearAttribute`, parent inheritance, a creator dbref, and attribute flags that are stored), `@chown`, `@name`, `@parent`, `@wipe`, `@clone`, `examine`.
- Locks: `@lock`, `@unlock`, `@elock`, `@ulock`, `@eunlock`, and `@uunlock`. `LockType` names Basic, Enter, Use, Page, Leave, Drop, Give, Teleport, and Speech. `EvalLock` can test a key. Movement, enter, leave, get, drop, give, and page call it. Lock names match regardless of case, so `@lock/enter` is found by the enter check. The named enter and use commands still store `Enter` and `Use`.
- Flow and queue: `@if` / `@ifelse`, `@skip` (the test reversed), `@switch` (first match unless `/all`), `@select` (always the first match), `@dolist`, `@trigger` (one evaluated result, dispatched once), `@wait`, `@notify` (wakes one waiter), `@halt`, `@allhalt`.
- World data: zones as a set of dbrefs, pennies, contents and exits as chains linked by `Next`, `GOING` set by `@destroy` and cleared by `@undestroy` / `@unrecycle`. `ObjectDatabase.DestroyObject` exists and is not called. SQLite saves objects, attributes, flags, powers, parents, zones, pennies, and cron after a command whose name starts with `@`.
- Connections: login `WHO`, an in-memory password dictionary, `DOING`. Passwords are not saved, and the God character has no password entry.
- Channels, mail, help files, and the connect texts (`motd`, wizard motd) exist in memory or on disk. Channel membership and mail are not saved.

## Tier 0 — aliases of handlers that already run

These shipped before the numbering above. They call a handler that already existed.

| Status | Command | What shipped |
| --- | --- | --- |
| 🟢 | `semipose` | Pose with no space before the text. `;` uses the same path. |
| 🟢 | `@select` | `@switch` that stops at the first match. It has no `/all` switch. |
| 🟢 | `@skip` | `@if` with the test inverted. |
| 🟢 | `@elock`, `@ulock`, `@eunlock`, `@uunlock` | Enter and use locks, stored as `Enter` and `Use`. |
| 🔵 | `@undestroy`, `@unrecycle` | Clears `GOING` on that one object. The original also clears a second destroy pass, undestroys the object's contents, and queues `@startup`. |

## Tier 1 — read or write state the object already has

Registered. No new table and no new field. Attribute edits are stored by the existing attribute save. `@set` refuses to change an attribute marked locked unless the setter is a wizard. `@prompt` omits the trailing newline when the target is a player.

| Order | Command | Depends on |
| --- | --- | --- |
| 🟢 1 | `@cpattr` | Read one attribute, write it onto each target. Copy the stored flags. |
| 🟢 2 | `@mvattr` | `@cpattr`, then clear the source. |
| 🔵 3 | `@edit` | Replace text inside one attribute. `/regexp` can wait until the plain replace works. |
| 🟢 4 | `@grep` | Read attributes on one object and print matches. |
| 🟢 5 | `@atrlock` | Set or clear `AttributeFlags.Locked` and report it. A later `@set` leaves the attribute unchanged unless the setter is a wizard. |
| 🟢 6 | `@atrchown` | Change the attribute creator. The same operation as `@chown` on one attribute. |
| 🟢 7 | `@lset` | Change flags on a lock `SetLock` already stores. The lock key stays as it is. |
| 🟢 8 | `@decompile` | Print an object's flags and attributes back as commands. `examine` already gathers that data. |
| 🔵 9 | `@moniker` | Set or clear the `MONIKER` attribute. ANSI checking waits until color codes exist; until then the value is stored as text. `%k` stays unimplemented. |
| 🟢 10 | `@message` | Evaluate one attribute and send it through `@pemit`, `@remit`, or `@oemit`. |
| 🔵 11 | `@prompt` | `@pemit` with no trailing newline for a direct player target. A room target still uses the normal room send. |
| 🟢 12 | `@lemit` | `@emit` aimed at the executor's location. |
| 🟢 13 | `@find` | Names of objects the executor controls. Optional dbref range on the right. |
| 🟢 14 | `@whereis` | A player's location. Also tell the target. |
| 🟢 15 | `brief` | `examine` with contents and the long attribute dump omitted. |
| 🟢 16 | `@drain` | The `@wait` semaphore list. `@notify` wakes one waiter; `@drain` drops them. |
| 🟢 17 | `@dump` | The existing SQLite save, on demand. |
| 🟢 18 | `@uptime` | Process start time. One timestamp recorded at startup. |
| 🟢 19 | `@poor` | Set every player's pennies. God only. Quota is not involved. |
| 🔵 20 | `@list` | Print the command table, flag table, power names, and builtin functions. Skip list types that have no table yet. |
| 🟢 21 | `@entrances` | Objects whose home or link points at a place. Type switches only filter the walk. |
| 🟢 22 | `@firstexit` | Move one exit to the front of the room's exit chain. |

## Tier 2 — one behavior added to a system that already runs

Each block starts with the foundation. The commands under it come after that foundation, in the listed order.

### Locks actually get tested

`EvalLock` is what `enter`, `leave`, `get`, `drop`, `give`, `page`, and `go` test. A lock command stores the key those commands read.

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 23 | `use` | Use lock, then `USE` and `OUSE`. `AUSE` is queued when it is set; otherwise `RUNOUT` is queued. A failed lock does not queue either. There is no action charge, so `RUNOUT` stands in for an unpaid action. |
| 🔵 24 | `empty` | Each thing inside follows the get and drop locks. A locked thing stays. Sticky, dropto, and drop-in are not checked, and dark contents are not hidden. |
| 🟢 25 | `teach` | Speech lock, a room line of the form "types --> …", then queue the taught text. |

### Nospoof on the emit path

`@emit`, `@oemit`, `@remit`, `@pemit`, `@lemit`, `@prompt`, and `@cemit` add `[Name:] ` when the speaker lacks `Can_spoof`. The `@ns` commands add that prefix even when the speaker has the power. A listener's `NOSPOOF` flag does not change the text.

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 26 | `@nsemit`, `@nsoemit`, `@nsremit`, `@nspemit`, `@nslemit`, `@nsprompt`, `@nscemit` | The matching emit with the speaker prefix forced. The listener's `NOSPOOF` flag is not consulted, and the prefix has no dbref. |
| 🔴 27 | `@nszemit` | `@zemit`, below. |

### Zones get a player-facing change and a broadcast

`@zone` already stores zone dbrefs. A walk of "objects in this zone" is the missing helper.

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 28 | `@chzone` | Control the object, and control the zone or pass its chzone lock. A non-player loses wizard, royalty, trust, and powers unless a wizard uses `/preserve`. No chzone failure attributes. A zone with no zone lock is locked to its owner. |
| 🟢 29 | `@chzoneall` | `@chzone` for each object a player owns. Wizards only. |
| 🟢 30 | `@zemit` | Emit to every room in the zone. You must control the zone. |
| 🟢 31 | `@nszemit` | `@zemit` with the speaker prefix forced. |

### Listeners and nearby commands

`$` command listeners already match on the room, its contents, inventory, and the player.

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 32 | `@scan` | Lists the $-commands that match the text, in the room, its contents, what you carry, yourself, the zone, or the master room. Speech still does not run zone or master-room commands. |
| 🔵 33 | `@sweep` | Lists listeners, puppets, and connected players in the room, inventory, or exits. Hearing is players and puppets, and a player is connected after login. |
| 🔵 34 | `with` | Runs the first matching $-command on a nearby object, one you control, or your zone object. `with/room` runs every match in a room, including your zone room and the master room. `/noeval` is ignored. |
| 🔵 35 | `@search` | Walks the database and keeps objects by owner, type, name, zone, parent, flags, and powers. No eval, lock, command, or listen search. |

### Destroy finishes what `@destroy` starts

`@destroy` sets `GOING` and leaves the object in the database. `@undestroy` clears that flag. Freeing the object is still separate.

| Order | Command | Depends on |
| --- | --- | --- |
| ⚪ 36 | `@purge` | Call `DestroyObject` on objects already marked `GOING`. |
| ⚪ 37 | `@nuke` | `@destroy` that frees an object immediately when it is already `GOING`. |

`@dbck` stays in tier 4. A consistency pass is misleading while locks, mail, channels, and passwords are still unsaved.

### The queue can be seen and nudged

| Order | Command | Depends on |
| --- | --- | --- |
| ⚪ 38 | `@ps` | List `ActionQueue` entries. |
| ⚪ 39 | `@kick` | Run the next N queued commands on the wizard's demand. |

`@assert`, `@break`, `@retry`, and `@verb` need an action list that can stop or restart. `@trigger` today runs a single dispatched command.

### Who is connected

Login already builds a `WHO` line. The in-game command is that list plus `DOING`.

| Order | Command | Depends on |
| --- | --- | --- |
| ⚪ 40 | `who` | The connection list, with `DOING`. |
| ⚪ 41 | `session` | `who`, plus host and idle when the connection records them. |
| 🔴 42 | `@hide` | A hidden bit on the connection that `who` and `session` skip. |
| ⚪ 43 | `@boot` | Drop one connection. The `Boot` power is already named. |
| 🟡 44 | `@poll` | Send one line to every connection and keep a reply slot per connection. This replaces the current `@poll` registration on `doing`. `doing` stays the `DOING` command. |

### Channel names that map onto `@channel`

`ChannelRegistry` already joins, leaves, sets a title, and lists members.

| Order | Command | Depends on |
| --- | --- | --- |
| ⚪ 45 | `addcom`, `delcom` | Join and leave, and remember the player's channel alias. |
| ⚪ 46 | `comtitle` | The membership title `@channel` already stores. |
| ⚪ 47 | `comlist`, `@clist` | The channel list. `/full` is the longer listing. |

`@clock` and `@restart` stay later. `@clock` is channel locks. `@restart` reloads channel runtime state. It does not reboot the process. `@reboot` remains the hot reload.

### Text files the server already reads on connect

| Order | Command | Depends on |
| --- | --- | --- |
| ⚪ 48 | `@motd`, `@listmotd`, `@wizmotd`, `@rejectmotd` | The connect and wizard text files already sent at login. |
| ⚪ 49 | `@suggest` | The same file read and write, for the suggestion file. |
| ⚪ 50 | `@readcache` | Reload help files and those connect texts. |

## Tier 3 — one new field or table, then the commands on it

### Followers

A follower list on the object, updated from `go`, `@teleport`, `enter`, and `leave`.

| Order | Command | Depends on |
| --- | --- | --- |
| ⚪ 51 | `follow` | Add a follower, or list them when no name is given. |
| 🔴 52 | `unfollow` | Leave one leader, or everyone when no name is given. |
| 🔴 53 | `dismiss` | Remove people following you. |
| 🔴 54 | `desert` | Clear both directions. |

### Quota

A quota number on the player, saved with the object.

| Order | Command | Depends on |
| --- | --- | --- |
| ⚪ 55 | `@quota` | Show the count and the limit. `/set` writes one player's limit. |
| 🔴 56 | `@squota` | `@quota/set`. |
| 🔴 57 | `@allquota` | `@quota` for every player. |
| 🔴 58 | `@chownall` | `@chown` across one player's things, rooms, and exits, then adjust both players' quotas. |

### Action lists

A multi-line attribute must run as a sequence of commands in one queue frame. `@include` is that runner.

| Order | Command | Depends on |
| --- | --- | --- |
| ⚪ 59 | `@include` | Run one attribute inplace as that list. |
| 🔴 60 | `@assert` | A false check sets the break flag. |
| 🔴 61 | `@break` | A true check sets the same flag. |
| 🔴 62 | `@retry` | A true check reruns the frame. The right-hand side replaces `%0` through `%9`. |
| 🔴 63 | `@verb` | The verb string is visible to the action list. |

### Passwords that survive a restart

| Order | Command | Depends on |
| --- | --- | --- |
| 🔴 64 | `@password` | Check the old password, write a new hash. |
| 🔴 65 | `@newpassword` | Wizard write of that hash. |
| 🔴 66 | `@pcreate` | The existing player-creation path, plus a password. |

### Commands and functions as editable tables

| Order | Command | Depends on |
| --- | --- | --- |
| ⚪ 67 | `@function` | Add, alias, restrict, enable, disable, and delete a global soft function. |
| ⚪ 68 | `@disable`, `@enable` | An enabled bit on a command. |
| 🔴 69 | `@command` | Add, alias, delete, and restrict a command. |
| 🔴 70 | `@hook` | Before, after, ignore, override, and extend slots around dispatch. |

### Definitions, warnings, mail aliases, config, and vendors

| Order | Command | Depends on |
| --- | --- | --- |
| ⚪ 71 | `@flag` | Create and edit flag definitions. Builtin flags already exist. |
| ⚪ 72 | `@attribute` | An attribute-definition table. Per-object attributes already exist. |
| ⚪ 73 | `@warnings` | A warning bitset stored on the object. |
| 🔴 74 | `@wcheck` | Those bits, then a topology check of what the player owns. |
| ⚪ 75 | `@malias` | A mail-alias table beside `MailStore`. This is not a channel command. |
| ⚪ 76 | `@config` | Read the options `MushConfiguration` already parsed. `/set` changes memory only. |
| 🔴 77 | `buy` | A vendor's `PRICELIST` attribute, a penny transfer through the same path as `give`, and the Pay lock from tier 2. |

## Tier 4 — wizard tools that need the earlier model to stay still

| Order | Command | Depends on |
| --- | --- | --- |
| 🔴 78 | `@dbck` | A full consistency walk: locations, homes, zones, owners, and `GOING` objects. |
| ⚪ 79 | `@shutdown` | Save, then stop the process. The existing `@reboot` command remains the hot engine reload. |
| ⚪ 80 | `@sitelock` | A ban table checked by login. |
| ⚪ 81 | `@sockset` | Options on one connected descriptor. |
| ⚪ 82 | `@log`, `@logwipe` | A wizard log file. |
| 🔴 83 | `@clock` | Join, speak, see, hide, and modify locks on a channel. |
| ⚪ 84 | `@restart` | Rebuild channel runtime state. This is not a server restart. |

## Tier 5 — subsystems the process does not have

| Order | Command | Subsystem |
| --- | --- | --- |
| 🔴 85 | `@sql` | A SQL connection for wizards. |
| 🔴 86 | `@mapsql` | `@sql`, plus one attribute call per row. |
| 🔴 87 | `@http` | An outbound HTTP client. |
| 🔴 88 | `@respond` | A pending inbound HTTP request to answer. |
| 🔴 89 | `@slave` | An external info or SSL slave process. |

## Commands that wait on a foundation

| Command | Wait for |
| --- | --- |
| `@assert`, `@break`, `@retry`, `@verb` | `@include` and the action-list frame. |
| `follow`, `unfollow`, `dismiss`, `desert` | The follower list and the movement hook. |
| `buy` | Pay-lock checks and penny transfer. |
| `@chownall` | Quota. |
| `@poll` | Connection replies. `doing` stays the `DOING` command. |
| `@restart` | Channel reload. It does not replace `@reboot` or `@shutdown`. |
| `@clock` | Channel locks. It is not a time command. |
| `@malias` | Mail aliases. It is not `comlist`. |
| `@sql`, `@mapsql`, `@http`, `@respond`, `@slave` | Tier 5. |
