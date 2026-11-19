# Commands: Attributes, Flags & Locks

Commands that set or inspect attributes, flags, powers, locks, and topology warnings, interrogated from `PennMUSH.Core/Attributes/AttributeSetCommandHandler.cs` and `PennMUSH.Core/Commands/Handlers/`.

## Quick Reference

| Command | Aliases *(+ `alias.cnf`)* | Restrictions | Switches / Notes |
|---|---|---|---|
| `&<attr>`, `@_<attr>`, `@<std-attr>` | — | Target controls check; `ATTRIB_SET noguest` *(in `restrict.cnf`)* | Inline attribute set or clear (`=<empty>`) |
| `@set` | — | `noguest` *(in `restrict.cnf`)* | `<obj>=[!]<flag>` or `<obj>=<attr>:<val>` |
| `@power` | — | `Wizard`, `logargs` *(in `restrict.cnf`)* | `<obj>=[!]<power>` |
| `@wipe` | — | `noguest` *(in `restrict.cnf`)* | `<obj>[/<pattern>]` |
| `@cpattr` | `@mvattr` | `noguest` *(in `restrict.cnf`)*, Controls src & dest | Copies (or moves) value + flags to one or more targets |
| `@edit` | `@gedit` *(via `alias.cnf`)* | `noguest` *(in `restrict.cnf`)*, Controls target | `/first`, `/regexp`, `/all`, `/nocase`, `/check`, `/quiet` |
| `@grep` | — | — | `/list`, `/print`, `/ilist`, `/iprint`, `/regexp`, `/wild`, `/nocase`, `/parent` |
| `@atrlock` | `@attrlock` *(via `alias.cnf`)* | Controls target | `<obj>/<attr>[=on\|off]` |
| `@atrchown` | `@attrchown` *(via `alias.cnf`)* | `noguest` *(in `restrict.cnf`)*, Controls target | `<obj>/<attr>=<owner>` (fails if attr `Locked` unless Wizard) |
| `@lock` | `@elock`, `@ulock` | `noguest` *(in `restrict.cnf`)* | `@lock[/<type>] <obj>=<key>` (default `"Default"` lock) |
| `@unlock` | `@eunlock`, `@uunlock` | `noguest` *(in `restrict.cnf`)* | `@unlock[/<type>] <obj>` |
| `@lset` | — | Controls target | `<obj>/<lock>=[!]<visual\|private\|wizard\|locked\|noclone\|owner>` |
| `@warnings` | — | Controls target, not Garbage/`GOING` | `<obj>=[!]<warning ...>` (`WarningBits.Parse`) |
| `@wcheck` | — | `/all` (`Wizard`), `/me`, `<obj>` (`See_All`/owner) | `/all`, `/me` |

---

## Inline Attribute Assignment (`AttributeSetCommandHandler`)

Unregistered commands starting with `&`, `@_`, or `@<standard-attr>` are intercepted by `AttributeSetCommandHandler.TryExecute()`:
- **`&<ATTR> <obj>[=<value>]`** — Sets `<ATTR>` on controlled `<obj>`, or clears it if `=` has an empty RHS.
- **`@_<ATTR> <obj>[=<value>]`** — Identical custom-attribute setter prefix (`TryReadName`).
- **`@<STD_ATTR> <obj>[=<value>]`** — Resolves standard attribute abbreviations or prefixes via `StandardAttributes.TryResolve` (e.g., `@desc me=Hello`, `@su exit=You step through.`).

*(Calls `AttributeWriter.Apply`, enforcing `Safe`, `Locked`, and `` ` `` branch hierarchy rules.)*

```text
&DATA_CACHE here=ready
@_HELPER me=think %0
@desc lamp=A polished brass lantern.
```

---

## Attributes, Flags & Powers

### `@set <target>=[!]<flag>|<attr>:<value>`
`Restrictions: noguest (in game/restrict.cnf)`

Two forms dispatched in `SetCommandHandler.cs`:
1. **Attribute form** (when RHS contains `:`): Splits `<attr>:<value>` (requiring Owner or Wizard). Branch attributes containing `` ` `` route through `AttributeWriter.Apply`; others call `target.SetAttribute(...)` with Wizard override.
2. **Flag form** (no `:` in RHS): Resolves `<flag>` via `FlagTable.FindFlag` (supporting aliases) and checks `FlagTable.CanSetFlag`; prefixes with `!` to clear.

```text
@set box=DARK
@set box=!DARK
@set box=DESCRIBE:A wooden crate.
```

### `@power <target>=[!]<power>`
`Restrictions: Wizard; @power logargs (in game/restrict.cnf)`

Grants or revokes (`!`) a power from `FlagTable.FindPower` (`PowerCommandHandler.cs`).

### `@wipe <target>[/<pattern>]`
`Restrictions: noguest (in game/restrict.cnf)`

Matches attributes on `<target>` by wildcard pattern (default `*`) via `PatternMatcher.Match` and clears each via `ClearAttribute` (`WipeCommandHandler` in `ObjectLifecycleCommandHandlers.cs`). Note: `FindObject` is used to locate `<target>`, and `ClearAttribute` skips `Safe` or non-Wizard `Locked` attributes.

### `@cpattr <src>/<attr>=<dest1>[/<attr1>][, <dest2>...]` & `@mvattr`
`Aliases: @mvattr` · `Restrictions: noguest (in game/restrict.cnf); must control source and all destinations`

Copies `<src>/<attr>` value and `AttributeFlags` to one or more comma-separated destination objects/attributes (`CopyAttributeCommandHandler.cs`). When invoked as **`@mvattr`**, clears the source attribute afterward (unless `Locked` without Wizard).

### `@edit[/first|/regexp|/all|/nocase|/check|/quiet] <object>/<attribute>=<old>,<new>`
`Aliases: @gedit (in game/alias.cnf)` · `Restrictions: noguest (in game/restrict.cnf)`

Must control `<object>`. By default replaces **every literal** occurrence (`StringComparison.Ordinal`) of `<old>` with `<new>` inside `<attribute>` (`EditAttributeCommandHandler`):
- `/first`: Replaces only the first literal occurrence.
- `/regexp`: Treats `<old>` as a .NET regular expression (first match by default, or all matches with `/all`; `/nocase` adds `RegexOptions.IgnoreCase`; invalid patterns print `Invalid Regular Expression.`).
- `/check`: Previews the change (`Would edit to: <updated>`) without writing `<attribute>`.
- `/quiet`: Suppresses status notifications (`Edited.`, `Nothing to change.`, `Would edit to: ...`).

### `@grep[/list|/print|/ilist|/iprint|/regexp|/wild|/nocase|/parent] <object>=<pattern>`

Searches attribute names and values on `<object>` (`GrepCommandHandler`):
- Default / `/print` prints `<ATTR>: <Value>` lines; `/list` prints matching attribute names only (`Sorted`).
- `/nocase`, `/ilist`, and `/iprint` enable case-insensitive matching (`/ilist` sets `/list` + nocase; `/iprint` sets `/print` + nocase).
- `/regexp` matches with `.NET Regex` (`Invalid Regular Expression.` on bad pattern); `/wild` matches using MUSH wildcard patterns (`PatternMatcher`).
- `/parent` walks `<object>`'s multi-parent DAG (`Parents`) without duplicating overridden attribute names.

### `@atrlock <object>/<attr>[=on|off]`
`Aliases: @attrlock (in game/alias.cnf)` · `Restrictions: Must control <object>`

Queries or updates `AttributeFlags.Locked` on `<object>/<attr>` (`AttributeLockCommandHandler`). Locked attributes cannot be modified by non-Wizards via `@set`/`@cpattr`/`@edit`.

### `@atrchown <object>/<attr>=<new_owner>`
`Aliases: @attrchown (in game/alias.cnf)` · `Restrictions: Must control <object>`

Changes `AttributeEntry.Creator` on `<object>/<attr>` unless `AttributeFlags.Locked` is set and executor is not Wizard (`AttributeChownCommandHandler`).

---

## Locks & Warnings

### `@lock[/<type>] <object>=<key>` & `@unlock[/<type>] <object>`
`Aliases: @elock/@eunlock (Enter lock), @ulock/@uunlock (Use lock)` · `Restrictions: noguest (in game/restrict.cnf)`

- **`@lock`** parses `<key>` using `LockParser.Parse` (dbref keys `#n`, `=#n`, `n` and Boolean operators `&`, `|`, `!`, `(...)`) and stores it under `<type>` (or `"Default"` when invoked as bare `@lock` without a switch; `"Enter"` for `@elock`; `"Use"` for `@ulock`) (`LockCommandHandler`, `LockCommands.LockCommandType`).
- **`@unlock`** clears that lock type (`UnlockCommandHandler`).

```text
@lock/Use Console=#10 | #15
@elock Shuttle=#10
@unlock/Use Console
```

### `@lset <object>/<lock>[=[!]<flag ...>]`
`Restrictions: Must control <object>`

Inspects or toggles `LockFlags` on an existing lock without modifying its key (`LockFlagCommandHandler`). Recognized lock flags: `visual`, `private`, `wizard`, `locked`, `noclone`, `owner`.

### `@warnings <object>=[!]<warning ...>` & `@wcheck`
- **`@warnings`** (`Controls`, non-Garbage/`GOING`): Parses space-separated warning bits via `WarningBits.Parse`: `none`, `exit-unlinked`, `thing-desc`, `room-desc`, `my-desc`, `exit-oneway`, `exit-multiple`, `exit-msgs`, `thing-msgs`, `exit-desc`, `lock-checks`, `serious`, `normal`, `extra`, `all` (prefix `!` to clear) (`WarningsCommandHandler.cs`).
- **`@wcheck[/all|/me] [<object>]`** (`WcheckCommandHandler.cs`):
  - `/all` (**Wizard**): Scans all non-garbage objects whose owners are currently online and lack `NOWARN`.
  - `/me`: Scans all non-garbage objects owned by executor.
  - `<object>` (requires `See_All`/`Royalty` or matching owner): Audits descriptions (`DESCRIBE`), lock targets referring to garbage, unlocked missing `SUCC`/`OSUCC`/`DROP`/`ODROP`, locked missing `FAIL`, unlinked exits, and oneway/multiple return exits.
