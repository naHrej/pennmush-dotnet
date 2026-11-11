# Unimplemented PennMUSH commands

Original PennMUSH defines 167 player commands in `src/command.c`. 89 of those names are registered in `PennMUSH.Server/Engine/MUSHServer.cs`. 78 names have no registration and are listed below. `@poll` is included too: it is registered on the `doing` handler and only sets the `DOING` attribute, so the original poll command is still missing.

`@notify` wakes one `@wait` semaphore, and `@drain` drops every waiter on that semaphore. `semipose` and `;` pose without a space. `@select` stops at the first match. `@skip` reverses `@if`. `@elock`, `@ulock`, `@eunlock`, and `@uunlock` set the enter and use locks. `@undestroy` and `@unrecycle` clear `GOING`. `@cpattr` copies an attribute, including its stored flags, and `@mvattr` copies it and then clears the source. `@edit` replaces text in one attribute. `@grep` prints an object's own attributes whose name or value contains the search text. `@atrlock` sets or clears the locked attribute flag, and a later `@set` leaves a locked attribute unchanged unless the setter is a wizard. `@atrchown` changes an attribute's creator. `@lset` changes the flags on an existing lock and leaves the key as it is. `@decompile` prints an object's flags and attributes as `@set` commands. `@moniker` stores or clears the `MONIKER` attribute as plain text. `@message` evaluates an attribute and sends it with pemit, remit, or oemit. `@prompt` sends a pemit with no trailing newline. `@lemit` emits to the player's location. `@find` lists objects the player controls. `@whereis` reports a player's location and, unless the seeker is a wizard, tells that player. `brief` examines an object without its contents or attribute list. `@dump` saves the database. `@uptime` reports when the process started. `@poor` sets every player's pennies. `@list` prints the command, flag, power, and function tables. `@entrances` lists objects whose home or exit destination is a place. `@firstexit` moves an exit to the front of a room's exit list. `@zone` sets zones. `@chzone` and `@chzoneall` are not registered. `@reboot` exists. `@restart` does not.

`@edit`, `@atrlock`, and `@atrchown` are registered. Their original aliases `@gedit`, `@attrlock`, and `@attrchown` are not.

## Building and movement

- `buy`

- `desert`

- `dismiss`

- `empty`

- `follow`

- `unfollow`

- `use`

- `with`

- `@sweep`

- `@scan`

- `@search`

## Objects, attributes, and locks

- `@attribute`

- `@chownall`

- `@flag`

- `@verb`

## Softcode

- `@assert`

- `@break`

- `@include`

- `@retry`

- `@hook`

- `@function`

- `teach`

## Speech that bypasses formatting

- `@nscemit`

- `@nsemit`

- `@nslemit`

- `@nsoemit`

- `@nspemit`

- `@nsprompt`

- `@nsremit`

- `@nszemit`

- `@zemit`

## Channels

`@channel`, `@chat`, and `@cemit` are already registered.

- `addcom`

- `delcom`

- `@clist`

- `comtitle`

- `comlist`

- `@malias`

## Players and session

`doing` is already registered.

- `who`

- `session`

- `@password`

- `@newpassword`

- `@pcreate`

- `@boot`

- `@hide`

## Quota, money, and polls

- `@quota`

- `@allquota`

- `@squota`

- `@suggest`

- `@poll`

## Wizard and server

- `@command`

- `@config`

- `@dbck`

- `@readcache`

- `@shutdown`

- `@restart`

- `@slave`

- `@kick`

- `@purge`

- `@nuke`

- `@ps`

- `@warnings`

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

- `@disable`

- `@enable`

## MOTD

- `@motd`

- `@listmotd`

- `@wizmotd`

- `@rejectmotd`

## Zones

- `@chzone`

- `@chzoneall`

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
