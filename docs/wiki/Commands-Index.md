# Commands Index

Complete A–Z command reference for **PennMUSH .NET**, extracted directly from `PennMUSH.Server/Engine/MUSHServer.cs`, `PennMUSH.Core/Commands/Handlers/*.cs`, `PennMUSH.Server/Commands/*.cs`, `game/alias.cnf`, and `game/restrict.cnf`.

* **139** handler registrations covering **201** distinct C# command names in `MUSHServer.RegisterCommands()`
* **21** default `command_alias` rules in `game/alias.cnf`, adding **3** alias-file-only names (`@gedit`, `@attrlock`, `@attrchown`) for **204** default callable command names
* **4** pre-login socket commands handled by `MUSHServer.HandleLoginAsync` (`connect`/`conn`/`co`, `create`/`cr`, `WHO`, `QUIT`)
* **13** built-in C# `AddRestriction` guards plus **21** default `restrict_command` rules in `game/restrict.cnf` (also inspectable/modifiable via `@command` and `@hook`)

---

## Domain Reference Pages

| Domain | Scope | Handlers / Names | Page |
|---|---|---|---|
| **Session & Login** | Pre-login auth, descriptors, passwords, help | 11 / 14 + 4 pre-login | [Session & Login](Commands-Session-and-Login) |
| **Movement & Interaction** | Rooms, exits, containers, items, follow | 19 / 32 | [Movement & Interaction](Commands-Movement-and-Interaction) |
| **Communication** | Speech, poses, emits, pages, walls, zones | 11 / 30 | [Communication](Commands-Communication) |
| **Channels & Mail** | Channels, channel locks, `@mail`, `@malias` | 7 / 13 | [Channels & Mail](Commands-Channels-and-Mail) |
| **Building & World** | Digging, objects, ownership, zones, search, quota | 29 / 33 | [Building & World](Commands-Building-and-World) |
| **Attributes, Flags & Locks** | Flags, powers, locks, attributes, grep/edit | 13 / 18 + 3 `alias.cnf` | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| **Softcode Flow & Queue** | Branching, loops, queue control, `@function`, `@hook` | 20 / 28 | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| **Server Admin & Extensions** | Config, schema, logs, SQL, HTTP, `@cron`, `@client`, `@plugin` | 29 / 33 | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |

---

## Prefix & Inline Command Dispatch

Before standard table lookup, the command parser handles single-character prefixes, channel shortcuts, and inline attribute assignment:

| Prefix / Pattern | Expands To / Meaning | Reference Page |
|---|---|---|
| `"<message>` / `'<message>` | `say <message>` | [Communication](Commands-Communication) |
| `:<action>` | `pose <action>` (with space) | [Communication](Commands-Communication) |
| `;<action>` | `semipose <action>` (no space) | [Communication](Commands-Communication) |
| `\<message>` | `@emit <message>` | [Communication](Commands-Communication) |
| `?<topic>` | `help <topic>` | [Session & Login](Commands-Session-and-Login) |
| `@@ <comment>` | No-op comment | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `+<channel> <message>` | Dynamic channel message dispatch (`@chat`) | [Channels & Mail](Commands-Channels-and-Mail) |
| `&<attr> <obj>=[<val>]` | Set/clear arbitrary attribute | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `@_<attr> <obj>=[<val>]` | Set/clear arbitrary attribute (`@_` prefix) | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `@<std-attr> <obj>=[<val>]` | Set/clear a standard attribute from the attribute table | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |

See [Server Configuration & Persistence](Server-Configuration-and-Persistence) for `restrict.cnf` / `alias.cnf` configuration, and [PennMUSH 1.8 Parity & Differences](PennMUSH-1.8-Parity-and-Differences) for omitted or partially implemented command switches.

---

## Complete A–Z Command Index

Sorted alphabetically by normalized command name (ignoring leading `@` and `+`), with single-symbol commands listed first.

### Symbols & Prefix Tokens

| Command | Role / Related | Domain Page |
|---|---|---|
| `"` | Prefix alias for `say` | [Communication](Commands-Communication) |
| `'` | Prefix alias for `say` | [Communication](Commands-Communication) |
| `:` | Prefix alias for `pose` | [Communication](Commands-Communication) |
| `;` | Prefix alias for `pose` (`semipose`) | [Communication](Commands-Communication) |
| `\` | Prefix alias for `@emit` | [Communication](Commands-Communication) |
| `?` | Alias for `help` | [Session & Login](Commands-Session-and-Login) |
| `@@` | Built-in comment command (no-op) | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `+c` | Alias for `@chat` | [Channels & Mail](Commands-Channels-and-Mail) |
| `+<channel>` | Dynamic channel speech dispatch | [Channels & Mail](Commands-Channels-and-Mail) |
| `&<attr>` | Inline arbitrary attribute assignment | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `@_<attr>` | Inline arbitrary attribute assignment | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `@<std-attr>` | Inline standard attribute assignment | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |

### A

| Command | Role / Related | Domain Page |
|---|---|---|
| `addcom` | Subscribe / bind channel alias | [Channels & Mail](Commands-Channels-and-Mail) |
| `@allhalt` | Halt queues across all objects | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `@allquota` | Recalculate quota for all players | [Building & World](Commands-Building-and-World) |
| `@assert` | Conditional abort / break action list | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `@atrchown` | Change attribute owner (`@attrchown` in `alias.cnf`) | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `@atrlock` | Lock/unlock an attribute (`@attrlock` in `alias.cnf`) | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `@attrchown` | `game/alias.cnf` alias for `@atrchown` | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `@attribute` | Manage standard attribute definitions | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@attrlock` | `game/alias.cnf` alias for `@atrlock` | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |

### B

| Command | Role / Related | Domain Page |
|---|---|---|
| `@boot` | Disconnect player or port | [Session & Login](Commands-Session-and-Login) |
| `@break` | Conditionally break current action list | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `brief` | Brief variant of `examine` | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `buy` | Buy item from vendor / `COST` | [Movement & Interaction](Commands-Movement-and-Interaction) |

### C

| Command | Role / Related | Domain Page |
|---|---|---|
| `@cemit` | Emit to a channel | [Channels & Mail](Commands-Channels-and-Mail) |
| `@channel` | Manage and inspect chat channels | [Channels & Mail](Commands-Channels-and-Mail) |
| `@chat` | Speak on a channel | [Channels & Mail](Commands-Channels-and-Mail) |
| `@chown` | Change object owner | [Building & World](Commands-Building-and-World) |
| `@chownall` | Reassign all objects owned by a player | [Building & World](Commands-Building-and-World) |
| `@chzone` | Assign an object's zone | [Building & World](Commands-Building-and-World) |
| `@chzoneall` | Reassign zone across a player's objects | [Building & World](Commands-Building-and-World) |
| `@client` | Dispatch MUjs client UI/editor/audio directives | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@clist` | Channel directory listing | [Channels & Mail](Commands-Channels-and-Mail) |
| `@clock` | Configure channel access locks | [Channels & Mail](Commands-Channels-and-Mail) |
| `@clone` | Duplicate an existing object | [Building & World](Commands-Building-and-World) |
| `co` | Pre-login alias for `connect` | [Session & Login](Commands-Session-and-Login) |
| `@command` | Inspect, restrict, enable, disable commands | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `comlist` | List your channel aliases | [Channels & Mail](Commands-Channels-and-Mail) |
| `comtitle` | Set your channel title for an alias | [Channels & Mail](Commands-Channels-and-Mail) |
| `@config` | View/modify runtime server configuration | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `conn` | Pre-login alias for `connect` | [Session & Login](Commands-Session-and-Login) |
| `connect` | *(pre-login)* Log into an existing player | [Session & Login](Commands-Session-and-Login) |
| `@cpattr` | Copy attributes between objects/names | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `cr` | Pre-login alias for `create` | [Session & Login](Commands-Session-and-Login) |
| `create` | *(pre-login)* Create and log into a new player | [Session & Login](Commands-Session-and-Login) |
| `@create` | Create a new Thing object | [Building & World](Commands-Building-and-World) |
| `@cron` | Schedule recurring softcode cron jobs | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |

### D

| Command | Role / Related | Domain Page |
|---|---|---|
| `d` | Short alias for `drop` | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@dbck` | Run database consistency check & repair | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@decompile` | Dump softcode recreation script for object | [Building & World](Commands-Building-and-World) |
| `delcom` | Unsubscribe / remove channel alias | [Channels & Mail](Commands-Channels-and-Mail) |
| `desert` | Stop following current leader | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@destroy` | Mark object for destruction / destroy | [Building & World](Commands-Building-and-World) |
| `@dig` | Create a new room (and optional exits) | [Building & World](Commands-Building-and-World) |
| `@disable` | Disable runtime server toggle | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `dismiss` | Dismiss followers | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `doing` | View or set your `DOING` status | [Session & Login](Commands-Session-and-Login) |
| `@doing` | Alias for `doing` | [Session & Login](Commands-Session-and-Login) |
| `@dolist` | Iterate over a word list and execute actions | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `@drain` | Drain semaphore queue on an object | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `drop` | Drop carried object | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@dump` | Save database to disk | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |

### E

| Command | Role / Related | Domain Page |
|---|---|---|
| `@edit` | Search and replace text inside attributes | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `@elock` | Set `Enter` lock (`@lock/enter`) | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `@emit` | Emit text to caller's location | [Communication](Commands-Communication) |
| `empty` | Empty a container | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@enable` | Enable runtime server toggle | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `enter` | Enter a container/vehicle object | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@entrances` | List exits/objects linked to a target | [Building & World](Commands-Building-and-World) |
| `@eunlock` | Clear `Enter` lock (`@unlock/enter`) | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `ex` | Short alias for `examine` | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `examine` | Inspect object metadata, attributes, flags | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@extension` | Alias for `@plugin` | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |

### F

| Command | Role / Related | Domain Page |
|---|---|---|
| `@find` | Search database by name pattern | [Building & World](Commands-Building-and-World) |
| `@firstexit` | Promote an exit to first in room exit list | [Building & World](Commands-Building-and-World) |
| `@flag` | Inspect and manage runtime flag definitions | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `follow` | Follow a leader | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@force` | Force another object to run a command | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `@function` | Register/manage user-defined `@function`s | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |

### G

| Command | Role / Related | Domain Page |
|---|---|---|
| `g` | Short alias for `get` | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@gedit` | `game/alias.cnf` alias for `@edit` | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `get` | Pick up an object | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `give` | Give object or pennies to another target | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `go` | Move through an exit | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `goto` | Alias for `go` | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@grep` | Search attributes matching a pattern | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |

### H

| Command | Role / Related | Domain Page |
|---|---|---|
| `@halt` | Halt queued commands for caller/object | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `help` | Look up documentation topics (`?`) | [Session & Login](Commands-Session-and-Login) |
| `@hide` | Toggle `DARK`/hidden status on `WHO` | [Session & Login](Commands-Session-and-Login) |
| `home` | Return caller to `Home` | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@hook` | Attach before/after/override hooks to commands | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `@http` | Make outbound HTTP request and trigger attribute | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |

### I

| Command | Role / Related | Domain Page |
|---|---|---|
| `i` | Short alias for `inventory` | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@if` | Alias for `@ifelse` | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `@ifelse` | Execute true/false branch conditionally | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `@include` | Inline-execute an attribute's code | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `inv` | Short alias for `inventory` | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `inventory` | List carried objects and pennies | [Movement & Interaction](Commands-Movement-and-Interaction) |

### K

| Command | Role / Related | Domain Page |
|---|---|---|
| `@kick` | Force immediate processing of queue items | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |

### L

| Command | Role / Related | Domain Page |
|---|---|---|
| `l` | Short alias for `look` | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `leave` | Exit an entered object/vehicle | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@lemit` | Local-emit variant handled with `@emit` | [Communication](Commands-Communication) |
| `@link` | Link exit/object/room destination or home | [Building & World](Commands-Building-and-World) |
| `@list` | Display server tables | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@listmotd` | Alias for `@motd/list` | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@lock` | Set an object lock | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `@log` | Write/recall server logs | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@logwipe` | Wipe/rotate/trim server logs | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `look` | Inspect room, object, or exit | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@lset` | Set flags on an existing lock | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |

### M

| Command | Role / Related | Domain Page |
|---|---|---|
| `@mail` | Send, read, clear, and manage in-game mail | [Channels & Mail](Commands-Channels-and-Mail) |
| `@malias` | Create and manage `@mail` distribution lists | [Channels & Mail](Commands-Channels-and-Mail) |
| `@mapsql` | Iterate SQL result rows over an attribute | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@message` | Send parameterized formatted messages | [Communication](Commands-Communication) |
| `@moniker` | Set styled ANSI moniker (`%k`) | [Building & World](Commands-Building-and-World) |
| `@motd` | Manage connect/wizard/down/full MOTDs | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `move` | Alias for `go` | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@mvattr` | Move attribute(s) (`@cpattr` variant) | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |

### N

| Command | Role / Related | Domain Page |
|---|---|---|
| `@name` | Rename an object | [Building & World](Commands-Building-and-World) |
| `@newpassword` | Admin password reset for another player | [Session & Login](Commands-Session-and-Login) |
| `@notify` | Notify/decrement object semaphore queue | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `@nscemit` | No-spoof channel emit | [Channels & Mail](Commands-Channels-and-Mail) |
| `@nsemit` | No-spoof `@emit` | [Communication](Commands-Communication) |
| `@nslemit` | No-spoof `@lemit` | [Communication](Commands-Communication) |
| `@nsoemit` | No-spoof `@oemit` | [Communication](Commands-Communication) |
| `@nspemit` | No-spoof `@pemit` | [Communication](Commands-Communication) |
| `@nsprompt` | No-spoof `@prompt` | [Communication](Commands-Communication) |
| `@nsremit` | No-spoof `@remit` | [Communication](Commands-Communication) |
| `@nszemit` | No-spoof `@zemit` | [Communication](Commands-Communication) |
| `@nuke` | Immediate destroy variant (`@destroy`) | [Building & World](Commands-Building-and-World) |

### O

| Command | Role / Related | Domain Page |
|---|---|---|
| `@oemit` | Emit to everyone in a room except target(s) | [Communication](Commands-Communication) |
| `@open` | Open a new exit | [Building & World](Commands-Building-and-World) |

### P

| Command | Role / Related | Domain Page |
|---|---|---|
| `p` | Short alias for `page` | [Communication](Commands-Communication) |
| `page` | Send remote private message to player(s) | [Communication](Commands-Communication) |
| `@parent` | Set or clear object parent (`%L`/inheritance) | [Building & World](Commands-Building-and-World) |
| `@password` | Change your own password | [Session & Login](Commands-Session-and-Login) |
| `@pcreate` | Wizard command to create a player object | [Session & Login](Commands-Session-and-Login) |
| `@pemit` | Send text directly to target object(s) | [Communication](Commands-Communication) |
| `@plugin` | Manage `.NET 10` runtime plugins | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@poll` | View or set the server `WHO` poll banner | [Session & Login](Commands-Session-and-Login) |
| `@poor` | Set all players' pennies | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `pose` | Emit action pose to current room | [Communication](Commands-Communication) |
| `@power` | Grant or revoke powers on an object | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `@prompt` | Send text without trailing newline prompt | [Communication](Commands-Communication) |
| `@ps` | Inspect queued actions and semaphores | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `@purge` | Permanently purge `GOING` objects | [Building & World](Commands-Building-and-World) |

### Q

| Command | Role / Related | Domain Page |
|---|---|---|
| `QUIT` | *(pre-login)* Disconnect socket before login | [Session & Login](Commands-Session-and-Login) |
| `@quota` | Inspect object quota usage | [Building & World](Commands-Building-and-World) |

### R

| Command | Role / Related | Domain Page |
|---|---|---|
| `read` | Alias for `look` | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@readcache` | Reload help/screen caches & vocabulary | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@reboot` | Hot reboot preserving connections & state | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@recycle` | Alias for `@destroy` | [Building & World](Commands-Building-and-World) |
| `@rejectmotd` | Alias prefix for `@motd/down` (`Wizard`) | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@remit` | Emit text to a specified room's contents | [Communication](Commands-Communication) |
| `@respond` | Set HTTP status/headers for web softcode | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@restart` | Halt queues & re-run `STARTUP` | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@retry` | Re-run/retry an attribute evaluation | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `@rwall` | Alias for `@wizwall` | [Communication](Commands-Communication) |

### S

| Command | Role / Related | Domain Page |
|---|---|---|
| `say` | Speak aloud to current room | [Communication](Commands-Communication) |
| `@scan` | Scan objects for matching `$command` patterns | [Building & World](Commands-Building-and-World) |
| `score` | Display your current pennies | [Session & Login](Commands-Session-and-Login) |
| `@search` | Query database objects by criteria | [Building & World](Commands-Building-and-World) |
| `@select` | Alias / first-match variant of `@switch` | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `semipose` | Pose without inserting a space (`;`) | [Communication](Commands-Communication) |
| `session` | Alias / byte-count view of `who` | [Session & Login](Commands-Session-and-Login) |
| `@set` | Set flags or attribute values | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `@shutdown` | Save database and stop/reboot server | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@sitelock` | Manage banned/registration IP/host rules | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@skip` | Conditional skip/guard (`@ifelse` family) | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `@slave` | Legacy DNS/ident slave stub | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@sockset` | View/configure descriptor options | [Session & Login](Commands-Session-and-Login) |
| `@sql` | Run SQL query against external DB | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@squota` | Set player quota ceiling | [Building & World](Commands-Building-and-World) |
| `@stats` | Display database object statistics | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@suggest` | Manage typo suggestion vocabulary | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@sw` | Short alias for `@switch` | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `@sweep` | Inspect room for listening objects/players | [Building & World](Commands-Building-and-World) |
| `@switch` | Pattern-match branch over action lists | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |

### T

| Command | Role / Related | Domain Page |
|---|---|---|
| `take` | Alias for `get` | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `teach` | Execute/teach commands to a target | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@tel` | Short alias for `@teleport` | [Building & World](Commands-Building-and-World) |
| `@teleport` | Move an object to another destination | [Building & World](Commands-Building-and-World) |
| `th` | Short alias for `think` | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `think` | Evaluate expression and show result to caller | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `throw` | Alias for `drop` | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@tr` | Short alias for `@trigger` | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `@trigger` | Enqueue an object attribute's action list | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |

### U

| Command | Role / Related | Domain Page |
|---|---|---|
| `@ulock` | Set `Use` lock (`@lock/use`) | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `@undestroy` | Cancel pending `GOING` destruction | [Building & World](Commands-Building-and-World) |
| `unfollow` | Stop following a leader | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@unlink` | Clear exit destination / dropto | [Building & World](Commands-Building-and-World) |
| `@unlock` | Remove an object lock | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `@unrecycle` | Alias for `@undestroy` | [Building & World](Commands-Building-and-World) |
| `@uptime` | Show process start time and elapsed uptime | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `use` | Use an object and trigger `USE`/`OUSE`/`AUSE` | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@uunlock` | Clear `Use` lock (`@unlock/use`) | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |

### V

| Command | Role / Related | Domain Page |
|---|---|---|
| `@verb` | Invoke custom verb attribute set (`@verb`) | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `@version` | Display server version | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |

### W

| Command | Role / Related | Domain Page |
|---|---|---|
| `w` | Short alias for `whisper` | [Communication](Commands-Communication) |
| `@wait` | Delay or semaphore-gate an action list | [Softcode Flow & Queue](Commands-Softcode-Flow-and-Queue) |
| `@wall` | Broadcast message server-wide | [Communication](Commands-Communication) |
| `@warnings` | Configure topology/building warning flags | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `@wcheck` | Run topology warning checks | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `@whereis` | Locate an online/findable player | [Building & World](Commands-Building-and-World) |
| `whisper` | Send private speech within current room | [Communication](Commands-Communication) |
| `who` | Show connected players/descriptors | [Session & Login](Commands-Session-and-Login) |
| `WHO` | *(pre-login)* Show unhidden connected roster | [Session & Login](Commands-Session-and-Login) |
| `@wipe` | Erase matching attributes from an object | [Attributes, Flags & Locks](Commands-Attributes-Flags-and-Locks) |
| `with` | Speak/run command as if with an object | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@wizmotd` | Alias prefix for `@motd/wizard` (`Wizard`) | [Server Admin & Extensions](Commands-Server-Admin-and-Extensions) |
| `@wizwall` | Broadcast message to Royalty+ / Wizards | [Communication](Commands-Communication) |

### X–Z

| Command | Role / Related | Domain Page |
|---|---|---|
| `x` | Short alias for `examine` | [Movement & Interaction](Commands-Movement-and-Interaction) |
| `@zemit` | Emit text across all rooms in a zone | [Communication](Commands-Communication) |
| `@zone` | Alias / zone inspection or assignment (`@chzone`) | [Building & World](Commands-Building-and-World) |

---

Back to [Home](Home)
