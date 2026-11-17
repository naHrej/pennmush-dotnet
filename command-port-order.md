# Port order for the remaining commands

`unimplemented-commands.md` names the player commands that still have no registration. This file is the order to port them in. A command belongs early when the data it reads, and the handler it most resembles, already exist. Each later tier adds one foundation and then the commands that need it.

## Backwards compatibility

Softcode written for original PennMUSH has to run here as it was written. Every original command and every original function has a place in this list. That place is finished when the command or function itself is in the server and behaves as it does in PennMUSH. This server may also carry extra commands and functions. An extra path leaves the original row unfinished.

Aspace is the exception. The space engine is being redesigned in pennmush-space. This list is base PennMUSH.


The dot beside each order number is the status of that row.

| Dot | Status |
| --- | --- |
| 🟢 | Finished. The behavior described in the row is in the server. |
| 🔵 | Finished, with a caveat. The row says what is still short of original PennMUSH. There is no cyan dot in the emoji set, so this one is blue. |
| 🟡 | Unfinished. A command of this name is registered, and it is not the original command yet. |
| ⚪ | Not started. There is no gray dot in the emoji set, so this one is white. |
| 🔴 | Blocked. Another row, or a subsystem the server does not have, has to land first. |
| 🟣 | New, and not a PennMUSH command. None of the numbered rows are in this group. |

Rows 1–22 are in the server. Four of them are blue because the original command still does something that row does not. `who`, `session`, `@hide`, `@boot`, and `@poll` are in the server. `@pcreate` creates a player and stores the password until the process restarts. The white rows after them have not been started. The red rows are waiting on the row or the subsystem named in the last column. `enter`, `leave`, `get`, `drop`, `give`, `page`, and `go` test locks. `use` and `empty` are blue, and `teach` is green. `addcom`, `delcom`, `comtitle`, `comlist`, and `@clist` are in the server. The attribute setters are in the server, with the limits named in that block. `@suggest` and `@readcache` are in the server, with the limits named on those rows. `follow`, `unfollow`, `dismiss`, and `desert` are in the server, with the limits named in that block. `@quota`, `@squota`, and `@allquota` are in the server, with the limits named in that block. `@chownall` is in the server, with the limits named on that row. `@include` is in the server, with the limits named on that row. `@function` is in the server, with the limits named on that row. `@disable` and `@enable` are in the server, with the limits named on that row. `@flag` is in the server, with the limits named on that row. `@attribute` is in the server, with the limits named on that row. `@warnings` is in the server, with the limits named on that row. `@malias` is in the server, with the limits named on that row. `@config` is in the server, with the limits named on that row. `@shutdown` is in the server, with the limits named on that row. `@sitelock` is in the server, with the limits named on that row. `@sockset` is in the server, with the limits named on that row. The next blocked rows are `@sql`, `@mapsql`, `@http`, `@respond`, and `@slave`. `@password` and `@newpassword` store a `XYXXY` hash, and connect checks that hash. `@log`, `@logwipe`, `@restart`, `@assert`, `@break`, `@retry`, and `@verb` are in the server, with the limits named on those rows. `@command`, `@hook`, `@wcheck`, `buy`, `@dbck`, and `@clock` are in the server, with the limits named on those rows.

## What already exists

These are the pieces a new command can call without a new subsystem:

- Speech and movement: `say`, `pose` (`semipose`, `;`, and `/nospace` omit the space), `@emit`, `@pemit`, `@remit`, `@oemit`, `@cemit`, `whisper`, `page`, `go`, `get`, `drop`, `enter`, `leave`, `give`, `home`, `@teleport`.
- Objects: attributes (`SetAttribute`, `GetAttribute`, `ClearAttribute`, parent inheritance, a creator dbref, and attribute flags that are stored), `@chown`, `@name`, `@parent`, `@wipe`, `@clone`, `examine`. Softcode writes those attributes with `&ATTR`, `@_ATTR`, and `@` plus a standard attribute name. Those forms are in the server, with the limits named in that block.
- Locks: `@lock`, `@unlock`, `@elock`, `@ulock`, `@eunlock`, and `@uunlock`. `LockType` names Basic, Enter, Use, Page, Leave, Drop, Give, Teleport, and Speech. `EvalLock` can test a key. Movement, enter, leave, get, drop, give, and page call it. Lock names match regardless of case, so `@lock/enter` is found by the enter check. The named enter and use commands still store `Enter` and `Use`.
- Flow and queue: `@if` / `@ifelse`, `@skip` (the test reversed), `@switch` (first match unless `/all`), `@select` (always the first match), `@dolist`, `@trigger` (one evaluated result, dispatched once), `@wait`, `@notify` (wakes one waiter), `@halt`, `@allhalt`.
- World data: zones as a set of dbrefs, pennies, contents and exits as chains linked by `Next`, `GOING` set by `@destroy` and cleared by `@undestroy` / `@unrecycle`. `@purge` and `@nuke` call `ObjectDatabase.DestroyObject` on objects marked `GOING`. SQLite saves objects, attributes, flags, powers, parents, zones, pennies, and cron after a command whose name starts with `@`.
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

`@emit`, `@oemit`, `@remit`, `@pemit`, `@lemit`, `@prompt`, `@cemit`, and `@zemit` keep the typed text. A listener with `NOSPOOF` sees `[Name:] ` on someone else's emit. `PARANOID` changes that tag to `[Name(#dbref)] ` and also shows it on the listener's own emit. A wizard, or a speaker with `Can_spoof`, leaves the prefix off an `@ns` command for every listener.

| Order | Command | Depends on |
| --- | --- | --- |
| 🟢 26 | `@nsemit`, `@nsoemit`, `@nsremit`, `@nspemit`, `@nslemit`, `@nsprompt`, `@nscemit` | A wizard or someone with `Can_spoof` omits the prefix for every listener. Anyone else follows the listener rule: `[Name:] ` for `NOSPOOF`, and `[Name(#dbref)] ` when that listener is also `PARANOID`. |
| 🟢 27 | `@nszemit` | The same command as row 31. |

### Zones get a player-facing change and a broadcast

`@zone` already stores zone dbrefs. A walk of "objects in this zone" is the missing helper.

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 28 | `@chzone` | Control the object, and control the zone or pass its chzone lock. A non-player loses wizard, royalty, trust, and powers unless a wizard uses `/preserve`. No chzone failure attributes. A zone with no zone lock is locked to its owner. |
| 🟢 29 | `@chzoneall` | `@chzone` for each object a player owns. Wizards only. |
| 🟢 30 | `@zemit` | Emit to every room in the zone. You must control the zone. |
| 🟢 31 | `@nszemit` | `@zemit` that omits the prefix for every listener when the speaker is a wizard or has `Can_spoof`. |

### Listeners and nearby commands

`$` command listeners already match on the room, its contents, inventory, and the player.

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 32 | `@scan` | Lists the $-commands that match the text, in the room, its contents, what you carry, yourself, the zone, or the master room. Speech still does not run zone or master-room commands. |
| 🔵 33 | `@sweep` | Lists listeners, puppets, and connected players in the room, inventory, or exits. Hearing is players and puppets, and a player is connected after login. |
| 🔵 34 | `with` | Runs the first matching $-command on a nearby object, one you control, or your zone object. `with/room` runs every match in a room, including your zone room and the master room. `/noeval` is ignored. |
| 🔵 35 | `@search` | Walks the database and keeps objects by owner, type, name, zone, parent, flags, and powers. No eval, lock, command, or listen search. |

### Destroy finishes what `@destroy` starts

`@destroy` sets `GOING`. `@purge` frees every such object, and `@nuke` frees one object that is already `GOING`.

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 36 | `@purge` | Wizards free every `GOING` object except #0, #1, and #2. One purge destroys them. There is no `GOING_TWICE` clock, `@adestroy`, possession sweep, queue drain, or quota refund. |
| 🔵 37 | `@nuke` | `@destroy` that frees an object already marked `GOING`. `@nuke` and `@destroy/override` can mark a `SAFE` object. A connected player can still be destroyed. |

`@dbck` is in the server, with the limits named on that row.

### The queue can be seen and nudged

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 38 | `@ps` | Lists your queue. `/all` lists every entry for a wizard, `/summary` and `/count` shorten the report, and a pid shows one entry. No load average and no `/debug` environment. |
| 🔵 39 | `@kick` | A wizard runs the next N queued commands immediately, including waits whose time has not come. Commands waiting on a semaphore stay queued. |

`@assert`, `@break`, and `@retry` stop or rerun the list `@include` is running. `@verb` runs one object's attribute as another object. `@trigger` today runs a single dispatched command.

### Who is connected

Login already builds a `WHO` line. The in-game command is that list plus `DOING`.

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 40 | `who` | Lists each connection. A mortal sees the name, time on, idle time, and the raw `DOING` text. A wizard, royalty, or someone with `See_All` also sees the location, command count, descriptor, and host, including hidden and not-yet-logged-in connections. The poll is the first line of that list, and the last column heading of the mortal list. `DOING` is not evaluated. |
| 🔵 41 | `session` | The privileged list, with bytes sent and received instead of the host. Pending output is always 0. A mortal gets the `who` list. |
| 🔵 42 | `@hide` | Hides one descriptor or every connection of a player. Royalty, a wizard, or the Hide power can hide their own. Only a wizard can hide someone else. No idle autohide. |
| 🔵 43 | `@boot` | Drops another player's connections. Royalty, a wizard, or the Boot power can boot someone else. `@boot/me` drops your idle connections and keeps the active one. `@boot/port` drops one descriptor. God cannot be booted. No boot log. |
| 🔵 44 | `@poll` | Sets the heading on `who`. A wizard or the Poll power can change it. `doing` still sets the `DOING` attribute. |

### Channel names that map onto `@channel`

`ChannelRegistry` already joins, leaves, sets a title, and lists members.

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 45 | `addcom`, `delcom` | Joins or leaves and remembers the alias. The alias is kept in memory with the channel list, so a restart drops both. There is no join lock and no join announcement. |
| 🔵 46 | `comtitle` | Sets, shows, or clears the title stored for that alias. Titles are not saved. |
| 🔵 47 | `comlist`, `@clist` | `comlist` lists your aliases. `@clist` lists channels, and `/full` adds the message count and the flags this server stores. |

`@restart` stays later. `@clock` is in the server, with the limits named on that row. `@restart` reloads channel runtime state. It does not reboot the process. `@reboot` remains the hot reload.

### Text files the server already reads on connect

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 48 | `@motd`, `@listmotd`, `@wizmotd`, `@rejectmotd` | One handler stores the connect, wizard, down, and full announcements. They are shown after the connect files and cleared on restart. |

### Attribute setters softcode already writes

These forms are parsed before the command table, so they are absent from the 167 names in `unimplemented-commands.md`. `@set` toggles flags, and this server also accepts `@set object=ATTR:value`. The original forms are in the server.

A dot is one character in a single attribute name. A grave accent separates a branch from the name under it. Setting KEEP`MID`LEAF creates KEEP and KEEP`MID when they are missing, marks each as a branch, and stores a single space on the new branch. A branch that still has a name under it cannot be cleared. Clearing the last name under a branch leaves the branch and removes the branch mark. A wizard, locked, safe, or no-dump flag on a branch blocks a change to a name under it.

| Status | Form | What has to happen |
| --- | --- | --- |
| 🔵 | `&ATTR object=value` | Sets or clears that attribute. No `=` clears it. An empty value clears it. The report is `Name(#dbref)/ATTR - Set.` or `Cleared.` A quiet attribute stays silent. |
| 🔵 | `@_ATTR object=value` | The same write. The stored name is the text after `@_`, uppercased. |
| 🔵 | `@attr object=value` | Sets a standard attribute when no real command has that name. `@desc` and `@describe` store `DESCRIBE` with that attribute's standard flags. `@va` through `@vz`, `@wa` through `@wz`, and `@xa` through `@xz` store those exact names. A unique prefix of a prefix-matching attribute matches. `@name` stays the name command. |

Player alias checks, forward-list checks, LISTEN ear announcements, and enum or limit checks are still absent from this write. A name may use letters, digits, `_`, `-`, `.`, and a grave accent. The original name table allows more punctuation. `get()`, `lattr()`, and `examine` still do not apply a branch's restrictions to the names under it.

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 49 | `@suggest` | `/add category=word` and `/delete category=word` change the suggestion vocabulary. A wizard adds or deletes. Anyone listing, including `/list` and a bare `@suggest`, sees the categories. `help` uses the HELP category for a close match. The vocabulary is kept in memory, so a restart clears it, and the match is a short edit distance. |
| 🔵 50 | `@readcache` | A wizard reports the byte size of each connect text, for the text files and the HTML files, then reloads help. The report ends with "Help files reindexed." There is no file-watch restart. |

## Tier 3 — one new field or table, then the commands on it

### Followers

A follower list on the object. `go`, `enter`, and `leave` bring along followers who are in the same place. `@teleport` does not.

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 51 | `follow` | Add a follower, or list both directions when no name is given. A follow lock can refuse. |
| 🔵 52 | `unfollow` | Leave one leader, or everyone when no name is given. |
| 🔵 53 | `dismiss` | Remove people following you. |
| 🔵 54 | `desert` | Clear both directions. |

`FOLLOWERS` and `FOLLOWING` store the dbrefs. `FOLLOW`, `OFOLLOW`, `UNFOLLOW`, and `OUNFOLLOW` are sent as stored text. Their action attributes are not queued. A player is visible to `follow`, and is brought along, only while that player is connected. A thing is brought along whenever it is in the same place.

### Quota

A quota number on the player, saved with the object.

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 55 | `@quota` | Show the count and the limit. `/set` writes one player's limit. |
| 🔵 56 | `@squota` | `@quota/set`. |
| 🔵 57 | `@allquota` | `@quota` for every player. God only. |
| 🔵 58 | `@chownall` | `@chown` across one player's things, rooms, and exits, then adjust both players' quotas. |

`RQUOTA` stores the objects still available. The line from `@quota` adds the owned count to that remainder. Wizard, royalty, and God are unlimited, and their limit cannot be set. `NO_QUOTA` and `QUOTAS` are not flag-table entries. Creating, cloning, destroying, and `@chown` leave the stored remainder unchanged. `@allquota` prints each remaining quota, and only God can run it.

`@chownall` is wizard-only. It gives away that player's things, rooms, and exits. `/things`, `/rooms`, and `/exits` narrow the set. `/preserve` keeps wizard, royalty, trust, and powers, except a wizard object given to a non-wizard loses WIZARD. The old owner gains one unit of remaining quota per object and the new owner loses one. Zones are replaced with the new owner's zones, with no circular-zone check. The object's queue is not halted. A player name matches anywhere, and `me` means the enactor.

### Action lists

A multi-line attribute must run as a sequence of commands in one queue frame. `@include` is that runner.

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 59 | `@include` | Run one attribute inplace as that list. |
| 🔵 60 | `@assert` | A false check stops the included list. Its right-hand side runs first. `/queued` puts that side on the queue. |
| 🔵 61 | `@break` | A true check stops the included list. Its right-hand side runs first. `/queued` puts that side on the queue. |
| 🔵 62 | `@retry` | A true check reruns the list. The right-hand side replaces `%0` through `%9`. A gagged player cannot retry. A list stops after 50 retries. |
| 🔵 63 | `@verb` | One object speaks and acts as another. The victim's attribute is evaluated, the room hears the others message, and the action attribute runs as the victim with the actor as enactor. |

`@include` runs the commands in one attribute immediately. `;` and a newline separate commands, and braces keep a `;` inside one command. A `$` or `^` prefix is skipped through the first colon. The right-hand side fills `%0` through `%9`, and the commands run as the player who typed `@include`. An attribute on a parent is included. `/localize` and `/clearregs` are accepted and do not change registers. `/nobreak` keeps a break inside the included attribute from stopping the caller. An attribute is included when it can be named, with no read check. A caller's `%0` through `%9` are not passed along when the command has no `=`. An ambiguous name is not reported.

### Passwords that survive a restart

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 64 | `@password` | A player checks the old password and stores `2:sha512:` on `XYXXY`. A bad new password is empty, contains a space, or contains `=`. A guest is refused. There is no guest flag yet, so that check waits until one exists. |
| 🔵 65 | `@newpassword` | A wizard stores that hash. `/generate` makes 7 to 12 letters and digits and shows them. An empty password is stored. God cannot be changed by another wizard. The wizard log records `*** NEWPASSWORD ***`. |
| 🔵 66 | `@pcreate` | Creates the player and stores the password hash on `XYXXY`. Connect checks that hash after a restart. No quota charge. `pcreate()` returns the dbref and sends the same messages. A dbref argument reuses a garbage object and is wizard-only. |

### Commands and functions as editable tables

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 67 | `@function` | Adds, lists, reports, updates, aliases, restricts, enables, disables, and deletes a global soft function. Calling it runs the named attribute. The table is not saved. `/clone` and `/restore` do not copy or restore a builtin. `/preserve` and `/builtin` are ignored. A builtin cannot be deleted. A call enforces Nobody, Wizard, God, Admin, NoGagged, and NoFixed. Other restriction names appear on the report and leave the call unchanged. NoGuest is stored, and the flag table has no GUEST flag. A builtin report leaves out the argument count. |
| 🔵 68 | `@disable`, `@enable` | A wizard turns a boolean configuration option on or off. The options are use_ws, use_dns, player_creation, guests, and use_quota. The new value stays in memory. `@quota` and `@squota` read use_quota on each command. A gagged wizard can disable an option and cannot enable one. Other stored options report that they are not on/off switches. An unknown name reports that there is no such option. The change is not written to mush.cnf or the wizard log. |
| 🔵 69 | `@command` | A wizard adds a stub command, aliases it, clones it, and can enable, disable, or restrict it. God deletes a command added this way. A disabled command is skipped. `@command` stays enabled. Restrictions are nobody, noguest, nofixed, nogagged, god, wizard, and admin. The table stays in memory. |
| 🔵 70 | `@hook` | A wizard, or someone with the Hook power, sets before, after, ignore, override, and extend. Ignore skips the command when the attribute is false. Override and extend run a `$` pattern in place of the command. Before and after evaluate the attribute. `/inline` and `/inplace` are stored, and the hook still runs immediately. `@password` and `@newpassword` cannot be hooked. |

### Definitions, warnings, mail aliases, config, and vendors

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 71 | `@flag` | God creates and edits flag definitions. List, info, add, alias, restrict, type, letter, enable, disable, delete, and decompile use the original messages. A new flag can be set with `@set`. The definition stays in memory and is not saved. `/debug` reports the flag count and bit positions and omits the flag cache. `inherit`, `log`, and `event` are stored on the definition and do not change `@set`. |
| 🔵 72 | `@attribute` | A wizard edits the attribute definition table: access, limit, enum, rename, delete, info, and decompile. Flags chosen here are used the next time the attribute is set. /limit and /enum reject or rewrite the value. /retroactive updates copies already stored. The edited table stays in memory. Rename does not rewrite values stored under the old name. A prefixmatch flag added here is recorded, and prefix lookup still uses the built-in prefix list. |
| 🔵 73 | `@warnings` | A warning bitset stored on the object. Names, groups, and `!` negation match the original list, and a new player starts on normal. Examine shows `Warnings checked:`. The bits are saved in the sqlite database, and an original dump's warnings field is loaded. The matcher returns the first object, so two matches do not produce the ambiguous message. |
| 🔵 74 | `@wcheck` | Checks the warning bits on an object the player owns. `/me` checks everything that player owns, and `/all` is wizard-only. Missing descriptions, unlinked exits, one-way and multiple exits, success and failure messages, and a lock that names a missing object are reported. A global return exit is not counted. |
| 🔵 75 | `@malias` | A mail-alias table beside `MailStore`. Names start with `+`. Create, list, members, add, remove, set, describe, rename, chown, useflag, seeflag, stats, destroy, and nuke use the original messages. `@mail +alias=subject/body` sends to the members. The table stays in memory. A sender who cannot see the members gets one confirmation, and the messages are still sent under that sender's name. `malias()` is not registered. |
| 🔵 76 | `@config` | Lists the options this server already stores, in the original groups that contain one. A mortal does not see the files or messages groups. `/set` changes the value in memory and the directive text used by screen files. `/save` is God only and reports that saves are disabled. The command does not write `mush.cnf`, does not enforce the original integer maxima, and does not set aliases, restrictions, or `include`. |
| 🔵 77 | `buy` | Reads a nearby vendor's `PRICELIST`. A price, a range, or a minimum moves pennies the way `give` does. The Pay lock can refuse. `BUY` and `OBUY` are evaluated when they are set. `ABUY` is queued as that vendor's command list, with the buyer as the enactor, the item in `%0`, and the price in `%1`. A halted thing does not queue it. A gagged player cannot buy. |

## Tier 4 — wizard tools that need the earlier model to stay still

| Order | Command | Depends on |
| --- | --- | --- |
| 🔵 78 | `@dbck` | A wizard repairs invalid owners, homes, locations, zones, parents, exit destinations, and attribute creators. A mortal hears `Silly mortal, chicks are for kids!` Owners hear about a circular zone, a zone with no zone lock, and a disconnected room. The check does not write the wizard log. |
| 🔵 79 | `@shutdown` | A wizard saves and then stops the process. Everyone sees `GAME: Shutdown by` the owner's name. `/panic` is God only and adds `EMERGENCY SHUTDOWN: @shutdown/panic`. The save is the normal database. `/reboot` reloads the engine without dropping sockets and says `GAME: Reboot finished.` It does not re-exec the process, and `@reboot` keeps its own messages. A switch typed after a space, such as `@shutdown /reboot`, counts too. `/paranoid` uses the same save. A failed save still stops, except `/reboot`, which cancels and says `GAME: Reboot failed.` |
| 🔵 80 | `@sitelock` | A wizard keeps an in-memory ban table. `/ban` refuses connect and create from a matching host, and the player hears `Player connections not allowed.` A plain `@sitelock` refuses create and still allows connect. `/register` refuses create the same way. `/check` and a bare `@sitelock` list the rules. `/remove` deletes by pattern or rule number. The table is not written to the access file, so a process restart clears it. A reload keeps the same table. `/player` has no stored last address. `/name` does not edit a names file. Access options after `=` are not parsed. A connection with no recorded host is not checked. |
| 🔵 81 | `@sockset` | Shows and sets options on one connected descriptor: prefix, suffix, Pueblo, telnet, width, height, terminal type, accents, color style, prompt newlines, and noquota. Width, height, color, Pueblo, and telnet update that connection. Prefix, suffix, accent stripping, prompt newlines, and noquota are stored and are not applied to output. Setting Pueblo does not send the Pueblo handshake. The options last for the connection. |
| 🔵 82 | `@log`, `@logwipe` | A wizard writes `Logged.` and a line on one of six files in `game/log`. `/recall` prints `Begin log recall.`, the lines written since this process started, and `End log recall.` God wipes a file with password `zap!` and hears `Log wiped.` A wrong password hears `Wrong password.` `/rotate` keeps one uncompressed copy named `.1`. `/trim` keeps the last tenth of the lines. The password is not read from `mush.cnf`. Recall does not reread the file. These commands do not log every command or connection. |
| 🔵 83 | `@clock` | Sets or clears the join, speak, see, hide, and modify locks on a channel. A failed join lock says `Permission to join denied.` A wizard joins anyway. A failed speak lock refuses the message. A failed see lock hides the channel. A member who passes the hide lock is left off `who`. A key is dbrefs combined with `&`, `|`, and `!`. The locks stay in memory. |
| 🔵 84 | `@restart` | Halts queues and queues each object's own `STARTUP`. A bare `@restart` says `Halted:`. Naming an object says `Restarting:`. `/all` needs a wizard or the Halt power, and every player hears `Your objects are being globally restarted by` the name. A switch after a space counts. `STARTUP` waits on the queue. Halting does not refund queue pennies. This does not reboot the process. |

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
| `@poll` | Connection replies. `doing` stays the `DOING` command. |
| `@malias` | Mail aliases. It is not `comlist`. |
| `@sql`, `@mapsql`, `@http`, `@respond`, `@slave` | Tier 5. |

## Player-facing wording, after the sections above

The port keeps each original sentence until the command rows in this file and the function sections in `function-port-order.md` are in the server. Many of those sentences refuse an action and leave out what was required. `You don't look like God.` and `You don't have enough magic for that.` are two of them. On `@flag`, both mean the edit is limited to player #1, and a wizard who is not #1 receives the same refusal. Other commands already use sentences of this kind, and the remaining rows will add more.

`#-1` results already have one home. `PennMUSH.Core/MUSHcode/MushError.cs` names each result and holds its text. After the command rows and function sections above are ported, give the other stock sentences that same treatment. One catalog lists them. A handler names the message, and the words a player sees come from the catalog. That catalog is where the sentences are read together and where their wording is changed. The catalog waits until those commands and functions exist, so the sentences they add are included.
