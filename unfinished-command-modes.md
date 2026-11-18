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

`/all`, `/summary`, `/count`, and a queue id already run. `/debug` and `/quick` are not read, so those calls print the normal queue list.

- [ ] `/quick` prints the short queue report.
- [ ] `/debug` on a queue id prints that entry with its environment. Original passes the debug flag only when the argument is a queue id.

## @dump

Handler: `DumpCommandHandler` in `PennMUSH.Core/Commands/Handlers/ObjectReadWriteCommandHandlers.cs`. Original: `cmd_dump` and `do_dump`.

The command already saves. `/paranoid`, `/debug`, and `/nofork` are not read, so every call is the same save.

- [ ] `/paranoid` selects the paranoid dump.
- [ ] `/debug` selects the debug dump.
- [ ] `/nofork` selects the nofork dump.

This server already saves in-process. The three switches still need the distinct original dump and the message that goes with it.

## @edit

Handler: `EditAttributeCommandHandler` in `PennMUSH.Core/Commands/Handlers/ObjectReadWriteCommandHandlers.cs`. Original: `cmd_edit`.

Every call is one literal replace. The switches are not read. Original switches are `FIRST`, `CHECK`, `QUIET`, `REGEXP`, `NOCASE`, and `ALL`.

- [ ] `/first` replaces the first literal match.
- [ ] `/regexp` edits with a regular expression. Without `/all`, that form replaces the first match.
- [ ] `/all` with `/regexp` replaces every match.
- [ ] `/nocase` makes a regexp edit case-insensitive.
- [ ] `/check` reports the edit and leaves the attribute unchanged.
- [ ] `/quiet` suppresses the report.

## @grep

Handler: `GrepCommandHandler` in `PennMUSH.Core/Commands/Handlers/ObjectReadWriteCommandHandlers.cs`. Original: `cmd_grep`.

The search is a case-insensitive contains over that object's own attributes. The switches are not read. Original switches are `LIST`, `PRINT`, `ILIST`, `IPRINT`, `REGEXP`, `WILD`, `NOCASE`, and `PARENT`.

- [ ] `/list` and `/print` use their original output.
- [ ] `/ilist` and `/iprint` use the case-insensitive forms of those outputs.
- [ ] `/regexp` searches with a regular expression.
- [ ] `/wild` searches with a wildcard pattern.
- [ ] `/nocase` controls case folding on the forms that are case-sensitive by default.
- [ ] `/parent` searches attributes inherited from the parent.

## @decompile

Handler: `DecompileCommandHandler` in `PennMUSH.Core/Commands/Handlers/ObjectReadWriteCommandHandlers.cs`. Original: `cmd_decompile`.

The output is one fixed `@set` listing of flags and non-internal attributes. The switches are not read. Original switches are `DB`, `NAME`, `PREFIX`, `TF`, `FLAGS`, `ATTRIBS`, and `SKIPDEFAULTS`.

- [ ] `/db` writes the decompile in the database form.
- [ ] `/name` limits the listing to the name.
- [ ] `/prefix` applies the requested prefix.
- [ ] `/tf` writes the true/false flag form.
- [ ] `/flags` limits the listing to flags.
- [ ] `/attribs` limits the listing to attributes.
- [ ] `/skipdefaults` omits default attributes.

## @list

Handler: `ListCommandHandler` in `PennMUSH.Core/Commands/Handlers/ObjectReadWriteCommandHandlers.cs`. Original: `cmd_list`.

`commands`, `flags`, `powers`, `functions`, `attribs`, and `motd` already list. `/lowercase` already applies to the attribute list. `/locks`, `/allocations`, `/all`, `/builtin`, and `/local` fall through to the usage line. `/lowercase` is ignored on the lists other than attributes.

- [ ] `/locks` lists locks.
- [ ] `/allocations` lists allocations.
- [ ] `/all`, `/builtin`, and `/local` choose which commands or functions are listed.
- [ ] `/lowercase` lowercases the lists that already print, not only attributes.

## @include and @hook

Handlers: `PennMUSH.Core/Commands/Handlers/IncludeCommandHandler.cs` and `PennMUSH.Core/Commands/Handlers/HookCommandHandler.cs`. Original: `cmd_include`, and the same queue flags in `cmd_hook`.

`@include` already runs the attribute. `/nobreak` already keeps a break inside the attribute from stopping the caller. `/localize` and `/clearregs` are not read.

`@hook` already stores before, after, ignore, override, and extend. `/localize` and `/clearregs` are saved as words on the hook's displayed switch list. Running the hook does not change registers.

Original maps `/localize` to `QUEUE_PRESERVE_QREG` and `/clearregs` to `QUEUE_CLEAR_QREG`.

- [ ] `@include/localize` preserves the caller's registers around the included commands.
- [ ] `@include/clearregs` clears registers for the included commands.
- [ ] `@hook/localize` preserves registers when that hook runs.
- [ ] `@hook/clearregs` clears registers when that hook runs.

## @search

Handler: `SearchCommandHandler` in `PennMUSH.Core/Commands/Handlers/NearbyCommandHandlers.cs`. Original: `cmd_search`.

Owner, type, name, zone, parent, flags, and powers already search. These criteria answer `That search is not available.`: `eval`, `elock`, `command`, `listen`, `eplayer`, `eroom`, `eexit`, `ething`, `eobject`.

- [ ] `eval` searches by an evaluated expression.
- [ ] `elock` searches by a lock.
- [ ] `command` searches by a `$` command.
- [ ] `listen` searches by a listen pattern.
- [ ] `eplayer`, `eroom`, `eexit`, `ething`, and `eobject` search by the evaluated type forms.

## @sockset

Handler: `PennMUSH.Core/Commands/Handlers/SocksetCommandHandler.cs`. Storage: `PennMUSH.Server/Session/ConnectionDirectory.cs` and `MUSHConnection`.

Width, height, color, Pueblo, and telnet already update the connection. `OUTPUTPREFIX`, `OUTPUTSUFFIX`, `STRIPACCENTS`, `PROMPT_NEWLINES`, and `NOQUOTA` are stored and shown by a bare `@sockset`. The output path never reads them.

- [ ] `OUTPUTPREFIX` is sent before each block of output.
- [ ] `OUTPUTSUFFIX` is sent after each block of output.
- [ ] `STRIPACCENTS` strips accents on the way out.
- [ ] `PROMPT_NEWLINES` applies to prompts.
- [ ] `NOQUOTA` exempts that connection from quota charges.

## @sitelock

Handler: `PennMUSH.Core/Commands/Handlers/SiteLockCommandHandler.cs`. Table: `PennMUSH.Core/Access/SiteLockTable.cs`.

`/ban`, `/register`, `/check`, `/remove`, and a bare `@sitelock` already keep an in-memory table. The table is still cleared on process restart. That save is a separate gap in `command-port-order.md`.

- [ ] `/name` opens the names file and can lock or delete a name. Today every call says it cannot open, lock, or delete the file.
- [ ] `/player` bans the player's last address. Today every call says there is no known address, because connect does not store `LASTIP` or `LASTSITE`.

## @stats

Handler: `StatsCommandHandler` in `PennMUSH.Core/Commands/Handlers/ObjectLifecycleCommandHandlers.cs`. Original: `cmd_stats`.

A plain `@stats` already prints object counts. `/tables`, `/chunks`, `/regions`, `/paging`, `/freespace`, and `/flags` are not read, so those calls print the same object counts. Original sends `/tables` to the memory-table report, `/chunks` and `/regions` to the chunk reports, `/paging` and `/freespace` to the paging and freespace reports, and `/flags` to the flag report.

- [ ] `/tables` prints the memory-table report.
- [ ] `/chunks` prints the chunk summary, and `/chunks/regions` prints the region breakdown.
- [ ] `/regions` prints the region report.
- [ ] `/paging` prints the paging report.
- [ ] `/freespace` prints the freespace report.
- [ ] `/flags` prints the flag report.

The original chunk reports describe the C allocator. These switches need a report for this process under the same switch names, with `/flags` counting flags.
