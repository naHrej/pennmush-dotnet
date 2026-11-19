# Commands: Softcode, Flow & Queue

Expression evaluation, conditionals, loops, action lists, queues/semaphores, user functions, command hooks, and command table administration interrogated from `PennMUSH.Core/Commands/Handlers/`.

## Quick Reference

| Command | Aliases | Restrictions | Switches / Notes |
|---|---|---|---|
| `think` | `th` | — | Evaluates `<expr>` and outputs directly to executor |
| `@switch` | `@sw`, `@select` | — | `/first` (default), `/all` (`@select` always stops on first match) |
| `@ifelse` | `@if`, `@skip` | — | `<cond>=<true>[,<false>]` (`@skip` inverts `<cond>`) |
| `@dolist` | — | — | Iterates whitespace-delimited items substituting `##`, `#@`, `%0` |
| `@trigger` | `@tr` | — | `<obj>/<attr>[=<a0>,<a1>...]` evaluates attribute & dispatches |
| `@include` | — | Cannot include `#1` God unless God | `/nobreak`, `/localize`, `/clearregs` |
| `@force` | — | Own/controlled or `Wizard` | `<target>=<command>` dispatches synchronously as `<target>` |
| `@verb` | — | Permission check (`Allows`) | Multi-arg verb message/attribute runner (`WHAT`..`ACTION`, `%0–%9`) |
| `@assert` | — | — | `/queued`; stops active `@include` frame if false & runs RHS |
| `@break` | — | — | `/queued`; stops active `@include` frame if true & runs RHS |
| `@retry` | — | `nogagged` *(in `MUSHServer.cs`)* | Updates `%0–%9` & restarts current `@include` loop while true |
| `@wait` | `@notify`, `@drain` | — | Queues timed or semaphore commands; notifies 1 or drains all waiters |
| `@halt` | — | Own/controlled (`Wizard` for `all` or others) | Cancels queued commands for target or `all` |
| `@allhalt` | — | `Wizard` | Halts every entry in `ActionQueue` |
| `@ps` | — | `/all` requires `Wizard` | `/all`, `/summary`, `/count`, `/quick`, `/debug`, `<pid>` |
| `@kick` | — | `Wizard` | Executes next `<count>` ready non-semaphore queue entries immediately |
| `@@` | — | — | No-op comment command |
| `@function` | — | `Wizard` / `Functions` power; `Partial Switches` | `/delete`, `/enable`, `/disable`, `/restrict`, `/alias` (`/clone`, `/restore` stubbed) |
| `@hook` | — | `Wizard` or `HOOK` power | `/before`, `/after`, `/ignore`, `/override`, `/extend`, `/igswitch`, `/inplace`, `/inline`, `/nobreak`, `/localize`, `/clearregs`, `/list` |
| `@command` | — | Lookups any; writes `Wizard`; `/delete` `God` | `/add`, `/alias`, `/clone`, `/delete`, `/on`, `/enable`, `/off`, `/disable`, `/restrict`, `/quiet`, parse switches |

---

## Softcode Evaluation & Flow Control

### `think <expression>` & `@@ [<comment>]`
- **`think`** (`Aliases: th`, `ThinkCommandHandler.cs`): Evaluates `<expression>` in `EvaluationContext(Executor, Enactor)` and sends the result straight to executor.
- **`@@`**: No-op comment (`CommentCommandHandler`).

### `@switch[/first|/all] <expr>=<pat1>,<act1>[,...,<default>]`
`Aliases: @sw, @select`

Evaluates `<expr>`, then tests wildcard patterns via `PatternMatcher.Match` (`SwitchCommandHandler` in `FlowControlCommandHandlers.cs`):
- Stops after the first matching pattern by default (or with `/first` or when invoked as `@select`).
- Pass `/all` to evaluate and execute all matching branches.
- An odd trailing argument runs only when no pattern matched.

### `@ifelse <cond>=<true>[,<false>]`
`Aliases: @if, @skip`

Evaluates `<cond>`; truthy when non-whitespace, not `"0"`, and not starting with `#-`. `@skip` reverses the truth value (`IfElseCommandHandler`).

### `@dolist <list>=<template>`
Evaluates `<list>`, splits on spaces, and dispatches `<template>` synchronously for each item with `##` (element), `#@` (1-based index), and `%0` substituted (`DolistCommandHandler`).

### `@trigger <obj>/<attr>[=<a0>,<a1>...]`
`Aliases: @tr`

Evaluates `<obj>/<attr>` on `<obj>` (`%!` = `<obj>`, `%#` = executor, `%0–%9` from `,`-split RHS) and dispatches the resulting command if non-whitespace (`TriggerCommandHandler`).

### `@force <target>=<command>`
Dispatches `<command>` immediately as `<target>` (`ForceCommandHandler`; requires controlling `<target>` or `Wizard`).

### `@include[/nobreak|/localize|/clearregs] <obj>/<attr>[=<args>]`

Reads `<obj>/<attr>` (stripped of leading `$pattern:` / `^pattern:` prefixes if present), splits commands on `;`, substitutes `%0–%9`, and executes each command in an `ActionFrame` (`RecursionLimit = 50`, `IncludeCommandHandler.cs`):
- `/nobreak` isolates nested `@break` / `@assert` frames from halting the caller's frame.
- `/localize` preserves outer Q-registers (`context.Registers`) around execution (starts with copies and does not write mutations back).
- `/clearregs` begins with empty Q-registers and isolates outer Q-registers from changes.

### `@assert[/queued] <cond>[=<actionlist>]`, `@break[/queued] <cond>[=<actionlist>]`, `@retry <cond>[=<args>]`
Action-list flow controllers operating on the active `@include` / `@verb` `ActionFrame` (`ActionListCommandHandler.cs`):
- **`@assert`**: When `<cond>` is false (`!MushBoolean.IsTrue`), marks `frame.Break = true` and runs/queues `<actionlist>`.
- **`@break`**: When `<cond>` is true, marks `frame.Break = true` and runs/queues `<actionlist>`.
- **`@retry`** (`nogagged` in `MUSHServer.cs`): When `<cond>` is true, optionally replaces `frame.Args` (`%0–%9`) and sets `frame.Retry = true` to rerun the current `@include` loop.

### `@verb <victim>=<actor>,<what>,<pdef>,<others>,<odef>,<action>[,<a0>...]`
Evaluates `<what>`/`<others>`/`<action>` attributes on `<victim>` as `<actor>`, emits formatted notifications (using fallback text `<pdef>` / `<actor.Name> <odef>` if attributes are absent, skipping others when `<actor>` is `DARK`), and runs `<action>` with `%0–%9` mapped from arguments 6+ (`VerbCommandHandler`).

---

## Queue & Semaphore Management

### `@wait <secs|sem>=<command>`, `@notify <sem>`, `@drain <sem>`
Handled by `WaitCommandHandler.cs` against `ActionQueue`:
- **`@wait <seconds>=<cmd>`**: Enqueues `<cmd>` with delay `TimeSpan.FromSeconds(seconds)`.
- **`@wait <semaphore>=<cmd>`**: Enqueues `<cmd>` blocked on case-insensitive `<semaphore>`.
- **`@notify <semaphore>`**: Releases one matching waiting entry to run immediately (`ActionQueue.NotifySemaphore`).
- **`@drain <semaphore>`**: Drops all queued entries waiting on `<semaphore>` (`ActionQueue.DrainSemaphore`).

### `@halt [<dbref|all>]`, `@allhalt`, `@ps`, `@kick`
- **`@halt`** (`HaltCommandHandler.cs`): Clears executor's (or controlled target's) queued entries (`@halt all` requires `Wizard`).
- **`@allhalt`** (**Wizard**, `AllHaltCommandHandler.cs`): Clears all entries across `ActionQueue`.
- **`@ps[/all|/summary|/count|/quick|/debug] [<pid>]`** (`PsCommandHandler`): Inspects own queue, a specific `<pid>`, `/all` (**Wizard**), `/summary`, or `/count`. `/quick` outputs compact `#<pid> <command>` lines without the header; `/debug <pid>` adds `Enactor`, `Args`, and saved `%`-registers (`Registers`).
- **`@kick [<count>]`** (**Wizard**, `KickCommandHandler`): Immediately runs up to `<count>` (default `1`) non-semaphore queue entries ordered by scheduled time & PID.

---

## Meta-Commands (`@function`, `@hook`, `@command`)

### `@function[/<switch>] [<name>[=<obj>/<attr>[,<min>,<max>[,<flags>]]]]`
`Restrictions: View/inspect = any; add/update/delete = Wizard or Functions power; enable/disable/restrict/alias = Wizard`

Defines and configures persistent global softcode functions (`soft_functions` in `RuntimeStateStore`, `FunctionCommandHandler.cs`):
- Bare `@function`: lists defined user functions (names only for mortals; dbref/attr table for `Wizard`/`Functions`).
- `@function <name>`: displays enabled state, restriction flags, location, and arg bounds (`MinArgs..MaxArgs`, clamped `0..30`).
- `@function <name>=<obj>/<attr>[,...]`: registers or updates a softcode function (`MaxArgs` default `10`).
- Switches: `/delete`, `/enable`, `/disable`, `/restrict <flags>`, `/alias <builtin>=<alias>`. (`/clone` and `/restore` currently report `"That's not a builtin function."`.)

### `@hook/<slot>[/<mode>] <command>[=<obj>[/<attr>]]`
`Restrictions: Wizard or HOOK power`

Attaches or clears command hooks on built-in or user-added commands (disallowed on `@password` and `@newpassword`; stored in memory in `CommandCatalog`, `HookCommandHandler.cs`):
- **Slots**: `/before`, `/after`, `/ignore`, `/override`, `/extend` (alias `/igswitch`).
- **Modes** (for `/override` & `/extend`): `/inplace`, `/inline`, `/nobreak`, `/localize`, `/clearregs` (`/localize` restores caller Q-registers after hook evaluation; `/clearregs` evaluates with cleared Q-registers and leaves caller registers untouched via `CommandCatalog.ApplyRegisters`).
- **`@hook/list [<command>]`**: Lists all hooked commands or hook details for `<command>`.

### `@command[/<switch>] <command>[=<rhs>]`
`Restrictions: Query any; /add, /alias, /clone, /on, /off, /restrict Wizard; /delete God`

Inspects and modifies the runtime `CommandDispatcher` / `CommandCatalog` table (`CommandCommandHandler.cs`):
- Switches: `/add` (creates a `StubCommandHandler`; captures `/eqsplit`, `/lsargs`, `/rsargs`, `/noparse`, `/rsnoparse` parse flags), `/alias <cmd>=<alias>`, `/clone <orig>=<clone>`, `/delete <user_cmd>`, `/on` / `/enable`, `/off` / `/disable` (cannot disable `@command`), `/restrict <rules>`, `/quiet`.
