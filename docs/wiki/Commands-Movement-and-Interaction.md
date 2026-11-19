# Commands: Movement & Interaction

Movement, room inspection, object manipulation, container, shop/vendor, follower, and `$`-command dispatch commands interrogated from `PennMUSH.Core/Commands/Handlers/`.

## Quick Reference

| Command | Aliases | Restrictions | Locks / Attributes Used |
|---|---|---|---|
| `look` | `l`, `read` | — | `Basic`, `DESCRIBE`, `ODESCRIBE`, `IDESCRIBE`, `OIDESCRIBE`, `DESCFORMAT`, `IDESCFORMAT`, `NAMEFORMAT`, `CONFORMAT`, `EXITFORMAT`, `SUCCESS`/`OSUCCESS`/`FAILURE`/`OFAILURE` |
| `examine` | `ex`, `x`, `brief` | — | `AttributePermissions.CanRead` (`brief` omits contents, exits, attributes & locks) |
| `go` | `goto`, `move` | — | Room `Leave`, exit `Basic` (or fallback `LOCK` attr); `FAIL`, `SUCC`, `OSUCC`, `DROP`, `ODROP`; brings followers |
| `home` | — | `nofixed` *(in `restrict.cnf`)* | Moves directly to `Home` (`#0` fallback) |
| `enter` | — | — | Target `Enter`; brings followers |
| `leave` | — | — | Container `Leave`; brings followers |
| `get` | `take`, `g` | — | `Thing` `Basic` lock |
| `drop` | `throw`, `d` | — | `Thing` `Drop`, room `Drop` |
| `inventory` | `i`, `inv` | — | `INVFORMAT` (`%0` = carried dbrefs, `%1` = `|`-joined names) |
| `give` | — | — | `Thing` `Give` lock *(or pennies transfer to nearby/online player)* |
| `use` | — | — | `Thing` `Use` lock; `USE`, `OUSE`, `FAILURE`, `AUSE` (fallback `RUNOUT`) |
| `empty` | — | — | Container `Enter` (unless controlled), `Basic` & `Drop` locks on contents / room |
| `teach` | — | — | Room `Speech` lock; announces & queues command for executor |
| `buy` | — | `nogagged` | Vendor `Pay` lock, `PRICELIST`, `BUY`, `OBUY`, `ABUY` |
| `follow` | — | — | Leader `Follow` lock; `FOLLOW`, `OFOLLOW`, `` FOLLOW_LOCK`FAILURE ``, `FOLLOWING`/`FOLLOWERS` |
| `unfollow` | — | — | `UNFOLLOW`, clears `FOLLOWING`/`FOLLOWERS` |
| `dismiss` | — | — | Clears one or all followers |
| `desert` | — | — | Drops both follower and leader links |
| `with` | — | — | `/room`; executes first matching `$pattern:action` on target/zone/master room |

---

## Inspection Commands

### `look [<target>]`
`Aliases: l, read`

Displays the current room or a named object/room (`LookCommandHandler.cs`):
- Checks `LIGHT` / `DARK` / `OPAQUE` flags (ignored by `Wizard`), evaluating `NAMEFORMAT`, `DESCFORMAT`, `IDESCFORMAT`, `CONFORMAT`, and `EXITFORMAT` via `FormatAttributes.Evaluate()` with `%!` set to the host object.
- When looking at a `Room`, evaluates `Basic` lock to emit `SUCCESS`/`OSUCCESS` vs `FAILURE`/`OFAILURE` (`TERSE` skips `DESCRIBE` and personal `SUCCESS`/`FAILURE` during automatic movement/connect looks).
- If `<target>` is a held/nearby `Thing`/`Player`, shows `DESCRIBE`, fires `ODESCRIBE`, and lists contents unless flagged `OPAQUE`.

```text
look
look here
read sign
```

### `examine [<object>]` / `brief [<object>]`
`Aliases: ex, x, brief`

Inspects `<object>` (defaults to `here`). Reports name, dbref, type, owner, pennies, flags, powers, warning mask (`WarningBits.Unparse`), location, and home. `examine`/`ex`/`x` also show contents, exits (for rooms), readable attributes (filtered by `AttributePermissions.CanRead`) with their flags, and locks. `brief` omits those four detail blocks (`SpatialInteractionCommandHandlers.cs`).

```text
examine me
brief #2
```

### `inventory`
`Aliases: i, inv`

Lists objects in your inventory, delegating to `INVFORMAT` (`%0` = space-separated dbrefs, `%1` = `|`-separated names) if present on `%#` (`InventoryCommandHandler.cs`).

```text
inventory
```

---

## Movement & Followers

### `go <exit>` / `<exit_name>`
`Aliases: goto, move` *(implicit via `CommandDispatcher` when `<input>` matches an exit in the current room)*

Moves `%#` through an exit in `Location` (matched by `;`-delimited exit alias or `#dbref`, `GoCommandHandler.cs`):
1. Verifies the destination object exists, evaluates the current room's `Leave` lock, and checks the exit's `Basic` lock (falling back to the legacy `LOCK` attribute if no `Basic` lock is set).
2. On lock failure, evaluates `FAIL` (or notifies `"You can't go that way."`).
3. On success, evaluates `SUCC`, `OSUCC` (default `"<name> leaves."`), moves `%#`, announces `ODROP` (default `"<name> has arrived."`), notifies `DROP`, shows automatic `look`, and brings eligible followers (`FollowerMovement.Bring`).

```text
go north
north
```

### `home`
`Restrictions: nofixed (in game/restrict.cnf)`

Teleports `%#` to its `Home` dbref (default `#0` if invalid), notifies departure/arrival, and shows automatic `look`.

### `enter <object>` & `leave`
- **`enter <object>`**: Finds `<object>` in the current room, checks `LockType.Enter`, moves `%#` inside it, shows automatic `look`, and brings followers (`EnterCommandHandler`).
- **`leave`**: Verifies `%#` is inside a non-`Room` container whose own `Location` is valid, checks `LockType.Leave`, moves `%#` out to the container's location, shows automatic `look`, and brings followers (`LeaveCommandHandler`).

```text
enter pod
leave
```

### `follow [<target>]`, `unfollow [<target>]`, `dismiss [<follower>]`, `desert [<who>]`
Manages follower graphs stored on `FOLLOWING` and `FOLLOWERS` attributes (`FollowCommandHandlers.cs`, `FollowerList.cs`):
- **`follow`**: With no argument, lists who you follow and who follows you. With `<target>`, checks visibility (`DARK`/room) and online status for players, evaluates `LockType.Follow` (failing with `` FOLLOW_LOCK`FAILURE ``), updates `FOLLOWING`/`FOLLOWERS`, and fires `FOLLOW`/`OFOLLOW`. Followers in the same room follow along automatically on `go`, `enter`, and `leave`.
- **`unfollow`**: Stops following `<target>` (or clears all leaders if no argument), firing `UNFOLLOW`.
- **`dismiss`**: Stops `<follower>` (or all followers) from following you.
- **`desert`**: Breaks both directions with `<who>` (or clears everyone you lead or follow).

---

## Object & World Interaction

### `get <thing>` & `drop <thing>`
- **`get`** (`Aliases: take, g`): Picks up a `Thing` in the current room if `LockType.Basic` passes (`GetCommandHandler.cs`).
- **`drop`** (`Aliases: throw, d`): Drops a held object into the current room if both the object's `LockType.Drop` and the room's `LockType.Drop` pass (`DropCommandHandler.cs`).

### `give <player>=<amount|object>`
1. **Pennies** (integer right side > `0`): Deducts pennies from non-Wizards (requires sufficient `Pennies`) and adds them to `<player>` (matched in room or globally via `FindPlayer`), notifying giver, recipient, and room.
2. **Object**: Transfers a held object if its `LockType.Give` passes (`SpatialInteractionCommandHandlers.cs`).

```text
give Alice=50
give Bob=Keycard
```

### `use <thing>`
Matches a `Thing` in room or inventory, evaluates `LockType.Use` (showing `FAILURE` on denial), prints `USE` (or `"Used."`), broadcasts `OUSE`, and queues `AUSE` (or `RUNOUT` if `AUSE` is unset) (`UseCommandHandler`).

### `empty <container|me>`
Moves `Thing`s out of `<container>` (requires controlling it or passing `LockType.Enter`):
- `empty me` drops held items failing neither item nor room `Drop` lock into the current room.
- Held container transfers items passing `Basic` lock into `%#`'s inventory.
- Ground container transfers items passing item `Basic` + `Drop` and room `Drop` locks into the current room.

### `teach <command>`
Checks the room's `LockType.Speech` lock, announces `"<name> types <command>."` to everyone in the room, and enqueues `<command>` on `ActionQueue` for `%#` (`TeachCommandHandler`).

### `buy <item>[ from <vendor>][ for <price>]`
`Restrictions: nogagged`

Purchases `<item>` from a nearby `Player`/`Thing` with a `PRICELIST` attribute (`ITEM:cost`, ranges `min-max`, open `min+`, or comma-separated prices; spaces in `<item>` normalize to `_`). Checks `LockType.Pay`, deducts pennies (free for Wizards), transfers payment to `<vendor>`, evaluates `BUY` and `OBUY` (passing `%0` = item, `%1` = price), and queues `ABUY` unless vendor is a non-player flagged `HALT` (`BuyCommandHandler.cs`).

```text
buy potion
buy health potion from Alchemist for 25
```

### `with[/room] <object>=<command>`
Matches `<target>` locally (nearby, controlled, zone, or with `/room` also `master_room` or zone room), locates the first matching `$<pattern>:<action>` attribute (or scans room contents with `/room`), and evaluates/dispatches the matched action on `<target>` (`WithCommandHandler` in `NearbyCommandHandlers.cs`).

| Switch | Behavior |
|---|---|
| *(none)* | Runs the first matching `$`-command directly on `<object>` |
| `/room` | Scans objects inside `<object>` (or current room / zone room / `master_room`) |

```text
with terminal=status
with/room #2=+who
```
