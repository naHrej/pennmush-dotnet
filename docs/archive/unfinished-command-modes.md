# Unfinished command modes

All 167 original player commands are registered. This file tracks the ones whose handler answers while the original work is still missing. Each box is a later pull request, or one piece of a pull request that shares a handler.

`command-port-order.md` is the full port status. This file is only the empty modes found in the compatibility pass on 18 Nov 2026. The comparison is original PennMUSH `src/command.c` and `src/cmds.c`. Check a box when that mode matches the original messages and effects. Leave the plain form of the command alone when this file says it already runs.

Suggested order, one heading at a time:

1. `@slave`
2. `@ps`
3. `@dump`
4. `@edit`
5. `@grep`
6. `@decompile`
7. `@list`
8. `@include` and `@hook`
9. `@search`
10. `@sockset`
11. `@sitelock`
12. `@stats`

## @slave

Handler: `PennMUSH.Core/Commands/Handlers/SlaveCommandHandler.cs`. Original: `cmd_slave`.

A bare `@slave` already says `I'm sorry, Dave, I'm afraid I can't do that.` A restart name other than `info` or `ssl` already says `No such service.`

- [ ] `@slave/restart info` stops and starts the info slave, says `Restarting info_slave daemon.`, and writes `{Name}(#{n}) restarted info_slave.` to the wizard log.
- [ ] `@slave/restart ssl` stops and starts the ssl slave, says `Restarting ssl_slave daemon.`, and writes `{Name}(#{n}) restarted ssl_slave.` to the wizard log.

## @ps

Handler: `PsCommandHandler` in `PennMUSH.Core/Commands/Handlers/QueueReportCommandHandlers.cs`. Original: `cmd_ps`.

`/all`, `/summary`, `/count`, and a queue id already run. `/quick` prints `#{pid} {command}` plus the totals. `/debug` adds the enactor, arguments, and registers when the argument is a queue id. There is still no load average.

- [x] `/quick` prints the short queue report.
- [x] `/debug` on a queue id prints that entry with its environment. Original passes the debug flag only when the argument is a queue id.

## @dump

Handler: `DumpCommandHandler` in `PennMUSH.Core/Commands/Handlers/ObjectReadWriteCommandHandlers.cs`. Original: `cmd_dump` and `do_dump`.

The command already saves. `/paranoid`, `/debug`, and `/nofork` choose that mode and its sentence. The save is still the in-process database write.

- [x] `/paranoid` selects the paranoid dump.
- [x] `/debug` selects the debug dump.
- [x] `/nofork` selects the nofork dump.

This server already saves in-process. Each switch reports with its own sentence: paranoid, debug, or saved without forking.

## @edit

Handler: `EditAttributeCommandHandler` in `PennMUSH.Core/Commands/Handlers/ObjectReadWriteCommandHandlers.cs`. Original: `cmd_edit`.

A plain edit replaces every literal match. `FIRST`, `CHECK`, `QUIET`, `REGEXP`, `NOCASE`, and `ALL` select the other forms.

- [x] `/first` replaces the first literal match.
- [x] `/regexp` edits with a regular expression. Without `/all`, that form replaces the first match.
- [x] `/all` with `/regexp` replaces every match.
- [x] `/nocase` makes a regexp edit case-insensitive.
- [x] `/check` reports the edit and leaves the attribute unchanged.
- [x] `/quiet` suppresses the report.

## @grep

Handler: `GrepCommandHandler` in `PennMUSH.Core/Commands/Handlers/ObjectReadWriteCommandHandlers.cs`. Original: `cmd_grep`.

The default search is a case-sensitive contains, and print is the default format. `LIST`, `PRINT`, `ILIST`, `IPRINT`, `REGEXP`, `WILD`, `NOCASE`, and `PARENT` select the other forms.

- [x] `/list` and `/print` use their original output.
- [x] `/ilist` and `/iprint` use the case-insensitive forms of those outputs.
- [x] `/regexp` searches with a regular expression.
- [x] `/wild` searches with a wildcard pattern.
- [x] `/nocase` controls case folding on the forms that are case-sensitive by default.
- [x] `/parent` searches attributes inherited from the parent.

## @decompile

Handler: `PennMUSH.Core/Commands/Handlers/DecompileCommandHandler.cs`. Original: `cmd_decompile` and `do_decompile`.

- [x] `/db` refers to the object by dbref. `/name` wins when both are set and refers to it by name.
- [x] `/prefix`, and an `=<prefix>` argument, starts every line with that prefix.
- [x] `/tf` uses `TFPREFIX` or `FugueEdit > `, refers to the object by dbref, and omits attribute-flag lines.
- [x] `/flags` prints the create, link, zone, parent, lock, flag, and power lines.
- [x] `/attribs` prints only attributes. A pattern after `/` does the same for the names that match.
- [x] `/skipdefaults` omits attribute-flag lines that match the standard attribute.

A value with a newline, a tab, or a leading or trailing space is written with `@set` and decomposed. `MONIKER` is written with `@moniker`. ANSI markup stays as stored text.

## @list

Handler: `ListCommandHandler` in `PennMUSH.Core/Commands/Handlers/ObjectReadWriteCommandHandlers.cs`. Original: `cmd_list`.

`commands`, `flags`, `powers`, `functions`, `attribs`, `motd`, `locks`, and `allocations` list. `/all` is the default. `/builtin` and `/local` limit commands and functions. `/lowercase` lowercases every list. `/allocations` is wizard-only and reports this process.

- [x] `/locks` lists locks.
- [x] `/allocations` lists allocations.
- [x] `/all`, `/builtin`, and `/local` choose which commands or functions are listed.
- [x] `/lowercase` lowercases the lists that already print, not only attributes.

## @include and @hook

Handlers: `PennMUSH.Core/Commands/Handlers/IncludeCommandHandler.cs` and `PennMUSH.Core/Commands/Handlers/HookCommandHandler.cs`. Original: `cmd_include`, and the same queue flags in `cmd_hook`.

`@include` already runs the attribute. `/nobreak` already keeps a break inside the attribute from stopping the caller. `/localize` keeps the caller's registers. `/clearregs` starts the included commands with empty registers and leaves the caller unchanged.

`@hook` already stores before, after, ignore, override, and extend. `/localize` and `/clearregs` are accepted on override and extend, and they control registers when that hook runs.

Original maps `/localize` to `QUEUE_PRESERVE_QREG` and `/clearregs` to `QUEUE_CLEAR_QREG`.

- [x] `@include/localize` preserves the caller's registers around the included commands.
- [x] `@include/clearregs` clears registers for the included commands.
- [x] `@hook/localize` preserves registers when that hook runs.
- [x] `@hook/clearregs` clears registers when that hook runs.

## @search

Handler: `SearchCommandHandler` in `PennMUSH.Core/Commands/Handlers/NearbyCommandHandlers.cs`. Original: `cmd_search`.

Owner, type, name, zone, parent, flags, and powers already search. `eval`, `elock`, `command`, `listen`, `eplayer`, `eroom`, `eexit`, `ething`, and `eobject` search as well. `##` in an eval expression is that object's dbref.

- [x] `eval` searches by an evaluated expression.
- [x] `elock` searches by a lock.
- [x] `command` searches by a `$` command.
- [x] `listen` searches by a listen pattern.
- [x] `eplayer`, `eroom`, `eexit`, `ething`, and `eobject` search by the evaluated type forms.

## @sockset

Handler: `PennMUSH.Core/Commands/Handlers/SocksetCommandHandler.cs`. Storage: `PennMUSH.Server/Session/ConnectionDirectory.cs` and `MUSHConnection`.

Width, height, color, Pueblo, and telnet already update the connection. `OUTPUTPREFIX` and `OUTPUTSUFFIX` wrap each line. `STRIPACCENTS` strips accents on the way out. `PROMPT_NEWLINES` sends a newline after a prompt. `NOQUOTA` skips that connection's queue limit.

- [x] `OUTPUTPREFIX` is sent before each block of output.
- [x] `OUTPUTSUFFIX` is sent after each block of output.
- [x] `STRIPACCENTS` strips accents on the way out.
- [x] `PROMPT_NEWLINES` applies to prompts.
- [x] `NOQUOTA` exempts that connection from quota charges.

## @sitelock

Handler: `PennMUSH.Core/Commands/Handlers/SiteLockCommandHandler.cs`. Table: `PennMUSH.Core/Access/SiteLockTable.cs`.

`/ban`, `/register`, `/check`, `/remove`, and a bare `@sitelock` already keep an in-memory table. The table is still cleared on process restart. That save is a separate gap in `command-port-order.md`.

- [x] `/name` opens the names file and can lock or delete a name.
- [x] `/player` bans the player's last address from the connection log.

## @stats

Handler: `StatsCommandHandler` in `PennMUSH.Core/Commands/Handlers/ObjectLifecycleCommandHandlers.cs`. Original: `cmd_stats`.

A plain `@stats` prints object counts. `/tables`, `/chunks`, `/regions`, `/paging`, and `/freespace` report this process. `/flags` counts flags. The memory reports are wizard-only.

- [x] `/tables` prints the memory-table report.
- [x] `/chunks` prints the chunk summary, and `/chunks/regions` prints the region breakdown.
- [x] `/regions` prints the region report.
- [x] `/paging` prints the paging report.
- [x] `/freespace` prints the freespace report.
- [x] `/flags` prints the flag report.

The original chunk reports describe the C allocator. These switches report this process under the same switch names, and `/flags` counts flags.
