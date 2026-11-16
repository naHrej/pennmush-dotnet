# Unimplemented PennMUSH commands

Original PennMUSH defines 167 player commands in `src/command.c`. 142 of those names are registered in `PennMUSH.Server/Engine/MUSHServer.cs`. 25 names have no registration and are listed below.

`@notify` wakes one `@wait` semaphore, and `@drain` drops every waiter on that semaphore. `semipose` and `;` pose without a space. `@select` stops at the first match. `@skip` reverses `@if`. `@elock`, `@ulock`, `@eunlock`, and `@uunlock` set the enter and use locks. `@undestroy` and `@unrecycle` clear `GOING`. `@cpattr` copies an attribute, including its stored flags, and `@mvattr` copies it and then clears the source. `@edit` replaces text in one attribute. `@grep` prints an object's own attributes whose name or value contains the search text. `@atrlock` sets or clears the locked attribute flag, and a later `@set` leaves a locked attribute unchanged unless the setter is a wizard. `@atrchown` changes an attribute's creator. `@lset` changes the flags on an existing lock and leaves the key as it is. `@decompile` prints an object's flags and attributes as `@set` commands. `@moniker` stores or clears the `MONIKER` attribute as plain text. `@message` evaluates an attribute and sends it with pemit, remit, or oemit. `@prompt` sends a pemit with no trailing newline. `@lemit` emits to the player's location. `@find` lists objects the player controls. `@whereis` reports a player's location and, unless the seeker is a wizard, tells that player. `brief` examines an object without its contents or attribute list. `@dump` saves the database. `@uptime` reports when the process started. `@poor` sets every player's pennies. `@list` prints the command, flag, power, and function tables. `@entrances` lists objects whose home or exit destination is a place. `@firstexit` moves an exit to the front of a room's exit list. `use` shows `USE`, tells the room `OUSE`, and queues `AUSE`, or `RUNOUT` when `AUSE` is unset. `empty` moves the things a container holds, skipping one whose get or drop lock fails. `teach` tells the room and queues the taught command. `@nsemit` and the other `@ns` emits leave the speaker prefix off for every listener when the speaker is a wizard or has `Can_spoof`. A normal emit shows `[Name:] ` only to a listener with `NOSPOOF`, and `[Name(#dbref)] ` when that listener is also `PARANOID`. `@chzone` moves an object into a zone when you control it and pass the zone's chzone lock. `@chzoneall` does that for every object a player owns. `@zemit` speaks to the rooms in a zone, and `@nszemit` leaves that message unprefixed for a wizard or someone with `Can_spoof`. `enter`, `leave`, `get`, `drop`, `give`, `page`, and `go` refuse when the matching lock fails. `use` shows `USE`, tells the room `OUSE`, and queues `AUSE`, or `RUNOUT` when `AUSE` is unset. `empty` moves the things a container holds, skipping one whose get or drop lock fails. `teach` tells the room and queues the taught command. `enter`, `leave`, `get`, `drop`, `give`, `page`, and `go` refuse when the matching lock fails. `@zone` sets zones. `@reboot` exists. `@restart` does not. `@scan` reports the $-commands that match the text you give it. `@sweep` lists listeners, puppets, and connected players nearby. `with` runs an object's $-command. `@search` lists objects by owner, type, name, zone, parent, flags, and powers. `@purge` frees every object marked `GOING`. `@nuke` frees one object that is already marked. `@ps` lists the queue. `@kick` runs the next queued commands. `who` lists who is connected. `session` adds byte counts for someone who can see every connection. `@hide` takes a connection off that list. `@boot` drops a connection. `@poll` sets the heading on `who`. `@pcreate` creates a player and remembers the password until the process restarts. `pcreate()` returns that player's dbref. `addcom` and `delcom` join and leave a channel and remember an alias. `comtitle` sets the title for that alias. `comlist` lists aliases, and `@clist` lists channels. `@motd` sets the connect announcement. `@listmotd` lists the announcements. `@wizmotd` and `@rejectmotd` set the wizard and down announcements, and only a wizard can use those two names. `@suggest` adds, deletes, or lists suggestion vocabulary. `@readcache` reports connect-file sizes and reloads help, and only a wizard can use it. `follow`, `unfollow`, `dismiss`, and `desert` keep the follower lists. `go`, `enter`, and `leave` bring followers who are in the same place. `@include` runs the commands stored in an attribute. `@function` adds a global soft function and can alias, restrict, enable, disable, and delete it. `@disable` and `@enable` turn a boolean configuration option off or on. `@flag` creates and edits flag definitions. `@attribute` edits the attribute definition table.

`@edit`, `@atrlock`, and `@atrchown` are registered. Their original aliases `@gedit`, `@attrlock`, and `@attrchown` are not.

## Building and movement

- `buy`

## Objects, attributes, and locks




- `@verb`

## Softcode

- `@assert`

- `@break`

- `@retry`

- `@hook`


## Speech that bypasses formatting










## Channels

`@channel`, `@chat`, and `@cemit` are already registered.

- `@malias`

## Players and session

`doing` is already registered.

- `@password`

- `@newpassword`

## Quota, money, and polls




## Wizard and server

- `@command`

- `@config`

- `@dbck`

- `@shutdown`

- `@restart`

- `@slave`

- `@wcheck`

- `@log`

- `@logwipe`

- `@sitelock`

- `@sql`

- `@mapsql`

- `@http`

- `@sockset`

- `@clock`

- `@respond`



## Zones



## Percent-substitution

`say` and `think` run text through the evaluator. These codes are substituted:

- `%%`, `%r`, `%t`, and `%b`
- `%#`, `%!`, `%@`, and `%l`
- `%0` through `%9`, and `%q`
- `%n` / `%N` — the enactor's name. An uppercase code capitalizes the first character.
- `%s`, `%o`, `%p`, and `%a` — the enactor's subjective, objective, possessive, and absolute possessive pronouns. Gender comes from the enactor's `SEX` attribute: male, female, plural, or neuter when it is absent.
- `%:` — the enactor's dbref and creation time, the same pair `objid()` returns
- `%+` — how many arguments were passed in
- `%?` — the evaluation's invocation count and recursion depth
- `%v`, `%w`, and `%x` plus one character — the executor's attribute of that name. `%va` reads `VA`. An uppercase first letter capitalizes the attribute value.

A code the evaluator does not recognize is left in the text. These original codes are still not substituted:

### Enactor presentation

- `%k` / `%K` — enactor's moniker, the name with its ANSI formatting
- `%~` — enactor's accented name

### Command and attribute context

- `%c` / `%C` — the raw command line
- `%u` / `%U` — the evaluated command line
- `%=` — the name of the attribute being evaluated
- `% ` — a percent sign followed by a space, which original PennMUSH turns into a single space

### Iteration stacks

- `%i0` through `%i9`, and `%il` — the current `iter()` text at that depth. `%il` is the innermost level.
- `%$0` through `%$9`, and `%$l` — the current `switch()` text at that depth. `%$l` is the innermost level.
