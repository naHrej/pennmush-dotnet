# Unimplemented PennMUSH commands

Original PennMUSH defines 167 player commands in `src/command.c`. 67 of those names are registered in `PennMUSH.Server/Engine/MUSHServer.cs`. 100 names have no registration and are listed below. `@poll` is included too: it is registered on the `doing` handler and only sets the `DOING` attribute, so the original poll command is still missing.

`@notify` is implemented. It wakes a `@wait` semaphore. `semipose` and `;` pose without a space. `@select` stops at the first match. `@skip` reverses `@if`. `@elock`, `@ulock`, `@eunlock`, and `@uunlock` set the enter and use locks. `@undestroy` and `@unrecycle` clear `GOING`. `@zone` sets zones. `@chzone` and `@chzoneall` are not registered. `@reboot` exists. `@restart` does not.

Default aliases of missing commands are also absent: `@gedit` for `@edit`, `@attrlock` for `@atrlock`, and `@attrchown` for `@atrchown`.

## Building and movement

- `buy`

- `brief`

- `desert`

- `dismiss`

- `empty`

- `follow`

- `unfollow`

- `use`

- `with`


- `@sweep`

- `@entrances`

- `@whereis`

- `@scan`

- `@find`

- `@firstexit`

- `@search`

## Objects, attributes, and locks

- `@attribute`

- `@atrlock`

- `@atrchown`

- `@cpattr`

- `@mvattr`

- `@edit`

- `@chownall`

- `@flag`

- `@decompile`

- `@verb`

- `@moniker`

- `@lset`





## Softcode

- `@assert`

- `@break`


- `@include`

- `@retry`


- `@drain`

- `@message`

- `@hook`

- `@function`

- `@grep`

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

- `@lemit`

- `@zemit`

- `@prompt`

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

- `@poor`

- `@suggest`

- `@poll`

## Wizard and server

- `@command`

- `@config`

- `@list`

- `@dbck`

- `@dump`

- `@readcache`

- `@shutdown`

- `@restart`

- `@slave`

- `@uptime`

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
