# Rearchitecture notes

Ideas for the PennMUSH .NET server after the feature work landed as separate pull requests. This is a design note. Items 1 through 4 below are in the code. Items 5 and 6 are not.

The game mailbox is `PennMUSH.Server/Engine/GameMailbox.cs`. Commands, login, the queue tick, reboot, and shutdown run there. Socket reads and the actual sends stay on the connection. `@` commands no longer rewrite the database. `SqliteStore.Flush` writes dirty objects, and when channels, mail, soft functions, sitelocks, or cron have changed, those tables, in one transaction. The game thread flushes about once a second. `@dump`, the editor save, reboot, and shutdown flush immediately. Destroyed objects stay in the file as Garbage, which is what `DestroyObject` already does in memory.

The server is a memory-resident world, which is the right shape. Players, rooms, and attributes already live in `ObjectDatabase` and are looked up by dbref in constant time. The work below keeps that model. It does not move the game onto a query per command.

## What the separate pull requests left behind

Each backend added a store or a pipeline next to the ones that already existed.

- The world is saved by `SqliteStore`. Channels, mail, soft functions, and sitelocks are saved by `RuntimeStateStore`. Connection history is a third file, `connlog.db`. `@dump` is the only player command that writes all of them (`MUSHServer.SaveAll`).
- Commands, wording, and registration grew in three places: a handler under `PennMUSH.Core/Commands/Handlers`, a member of the `Feedback` catalog, and a `Dispatcher.Register` line inside `MUSHServer.RegisterCommands`.
- Presentation grew as extra branches on `MUSHConnection`: telnet, HTML, accent stripping, prefix and suffix, and MUjs envelopes. The double prefix and suffix fix on main is the kind of bug that path produces.
- Login, passwords, screens, notifications, the queue tick, and reboot all still live on `MUSHServer`, which is now about 1,100 lines.

## Do this first

### 1. One game thread

`HandleInputAsync` runs on whatever connection accepted the line. Telnet and WebSocket sessions do this independently. `QueueTickLoopAsync` also calls `Dispatcher` and `Database` every 100ms from a background task. `ObjectDatabase` and `MUSHObject` do not lock their lists or attribute dictionaries.

Two connected players, or one player and a cron tick, can interleave a move, an attribute write, and a save. That is a correctness bug that will show up as a rare crash or a torn object long before it shows up as slowness.

Post each input line and each queue tick onto one game mailbox. Keep socket reads and writes off that thread. `TrackOutbound` can stay as it is: the game thread builds the text, and the socket send stays async. PennMUSH works this way, and it makes every later cache safe without sprinkling locks through the handlers.

### 2. Stop rewriting the database on every `@` command

`HandleInputAsync` calls `Storage.Save` whenever `CommandName` starts with `@`. `SqliteStore.Save` opens a new connection, deletes every row in `objects`, `attributes`, `flags`, `powers`, `parents`, `zones`, and `cron_jobs`, then inserts the whole world again. Each attribute gets its own `SqliteCommand`. A quiet `@emit` pays for that, and so does `@pemit`.

`RuntimeState.Save` is the same kind of full rewrite, and the `@` path does not call it. A channel change or a sitelock survives `@dump`, reboot, and shutdown, and is lost on a crash. The world rewrite and the runtime-state rewrite can also land as two transactions, so a crash between them splits the two stores.

Replace both with one writer:

- Keep a long-lived SQLite connection and prepared statements.
- Mark an object, a cron job, or a runtime record dirty when it changes.
- Flush the dirty set on a short interval, on `@dump`, and on shutdown, in one transaction.
- Write inserts and updates for the dirty rows. Delete a row only when the object is destroyed.

`@dump` then means "flush now," which is what the command already claims. The `@` prefix check goes away, so commands that do not mutate anything stop forcing a save, and commands that mutate runtime state stop skipping one.

### 3. Indexes for the scans that walk every object

`GetObject` is an array index. `FindPlayer`, `FindObject`, login, and `create` are not. They call `GetAllObjects()` or `FirstOrDefault` over the whole list. The same walk is in `lsearch`, `children`, quota, `@search`, `@find`, `@stats`, and zone commands.

Maintain maps that update when a name, owner, type, or zone changes:

- Player name to dbref, for login and `*name`.
- Object name to dbrefs, for the global step of `MatchObject`.
- Type, owner, and zone to dbrefs, for the commands and functions that filter the world.

`@search` and `lsearch` keep a full scan. They mean "consider everything." `@stats` does too, because `GOING` is counted as garbage rather than the object's type. Login, `children()`, quota, `@find` for a non-wizard, chown, zone, and the player loops use the maps.

`ObjectIndex` keeps the maps and the room exit-alias map. Ids stay sorted, and the lowest dbref wins a shared name. The alias map is rebuilt from the exit list when an exit in that room is renamed, linked, or removed. Load rebuilds the maps after every object, parent, and zone is in memory. `MUSHObject.Quiet` is per thread, so one database load does not freeze index updates on another.

## Do this next

### 4. Cache `$` listeners and inherited attributes

An unmatched command checks the room, its contents, the player's inventory, and the player. Each of those objects keeps the `$` and `^` patterns parsed off its own attributes. The command walk and `@listen` / `@search listen` read that list. Setting or clearing an attribute rebuilds it.

`GetAttributeWithSource` still walks the parent list in the same order: the object's own attribute, then each ancestor's own attribute, skipping `PRIVATE`. A hit or a miss is remembered on the child. The child's cache is dropped when its attributes or its parent list change. A change on a parent drops the cache of every descendant. The walk order is unchanged.

### 5. Split `MUSHServer` along the jobs it already does

`MUSHServer` constructs the world, registers every command, runs login, owns the in-memory password dictionary, sends screens, routes notifications, ticks the queue, and saves. Tests can build a `CommandDispatcher` without it, but production behavior keeps leaking back into this class because that is where the last pull request had to wire a constructor argument.

Four types match the code that is already there:

- **World services.** Database, queue, cron, channels, mail, functions. This is what handlers need.
- **Session service.** Connections, login, connect screens, and who is in a room. `SendToRoom` uses a room-to-sessions list, updated on connect, disconnect, and when a player's location changes. Login screens and the rest of the session work still live on `MUSHServer`.
- **Persistence.** The writer from item 2. Handlers stop calling `Storage.Save` themselves.
- **Command modules.** Each area registers its own handlers and restrictions. `RegisterCommands` becomes a list of modules instead of a method that grows with every command.

`Feedback` can stay a catalog. The gain is not splitting the enum. The gain is that a new command does not have to edit the server shell to exist.

Passwords are the hash on `XYXXY`. Login reads that attribute. A player with no hash, including the starting God, still connects with any password.

### 6. One outbound record

`SendLineAsync`, `SendRawAsync`, `SendMarkupAsync`, and `SendPromptAsync` each decide accent stripping, HTML, newlines, prefix, suffix, and whether the socket is a MUjs WebSocket. A new presentation change has to be threaded through all of them.

Build one record per line: plain text, optional HTML, and optional protocol payload. Apply prefix, suffix, and accent policy once, when the record is created. The socket writer only sends bytes or one JSON envelope. Telnet negotiation stays on the telnet listener, where it already belongs.

## Later, if softcode or the database gets large

These are real, and they are not the current ceiling. The save and the full-table scans will dominate first.

- `ExpressionEvaluator` allocates a `StringBuilder` and a substring per bracket and brace. A reused buffer per evaluation, or a small compiled form for attributes that are evaluated often (`@acommand`, descriptions, `$` actions), matters once those run on every speech line.
- `@edit`, `@grep`, and the regex functions compile a `Regex` on every call. A cache keyed by pattern and options, with the timeout already used, removes that from inner loops.
- `SqliteStore.Save` builds a new command per attribute even after the writer is incremental. Prepared statements are part of item 2, not a separate project.
- The queue tick swallows every exception. Once the game thread exists, a tick failure should be logged with the command and the dbref. Silent `catch` blocks are how a bad `@wait` disappears.

## What not to change

- Do not replace the in-memory dbref array with SQLite reads on the command path. Loading the world at startup and serving it from memory is the design that matches the game.
- Do not merge `connlog.db` into the world file just to have one file. It is an append-only log with its own lifetime. It only needs to stay off the `@` save path, which it already is.
- Do not split `Feedback` into per-command files as a cleanup project. The catalog is doing its job. Touch it when a command module is being introduced for another reason.

## Suggested order

1. Game mailbox, so the world is no longer touched from two threads. Done.
2. Dirty-set save that includes runtime state, and removal of the save-on-every-`@` rule. Done.
3. Player, name, type, owner, and zone indexes, plus the room exit-alias map. Done.
4. Listener list and inherited-attribute cache. Done.
5. Split session, persistence, and command registration out of `MUSHServer`, and collapse outbound to one record. The room list and the single password store are in. The class split and the outbound record are not.
6. Evaluation buffers and a regex cache when a profile says they matter.

Items 1 and 2 change behavior under load and across a crash. They should land with tests that run two sessions against one database and a test that kills the process after a channel change and a single attribute edit. The rest can follow one area at a time.
