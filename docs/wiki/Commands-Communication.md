# Commands: Communication

Speech, posing, paging, emits, nospoof variants, formatted attribute messages, walls, and zone broadcasts interrogated from `PennMUSH.Core/Commands/Handlers/`.

## Quick Reference

| Command | Aliases | Key Switches | Notes |
|---|---|---|---|
| `say` | `"`, `'` | `/noeval` | Evaluates message (unless `/noeval`) and applies `SPEECHMOD` (`"`) |
| `pose` | `:`, `;`, `semipose` | `/nospace` | Evaluates via `SPEECHMOD` (`:` or `;` for no space) and emits to room |
| `whisper` | `w` | — | Private whisper to a `Player` in the same room |
| `page` | `p` | — | Private message to any `Player`, checking `LockType.Page` and `OUTPAGEFORMAT`/`PAGEFORMAT` |
| `@emit` | `\`, `@lemit`, `@nsemit`, `@nslemit` | — | Emits raw `Arguments` (via `SPEECHMOD` `|`) to executor's location (`@ns*` suppresses spoof prefix for Wizard/`Can_spoof`) |
| `@pemit` | `@prompt`, `@nspemit`, `@nsprompt` | `/silent` | Sends directly to target player/object or room; `@*prompt` omits newline unless `PROMPT_NEWLINES` |
| `@remit` | `@nsremit` | — | Sends message to a specified `Room` (`#dbref`) |
| `@oemit` | `@nsoemit` | — | Sends message to everyone in executor's room except target |
| `@message` | — | `/remit`, `/oemit` | Evaluates `<obj>/<attr>` and sends to `<target>`, room (`/remit`), or room-except (`/oemit`) |
| `@wall` | `@wizwall`, `@rwall` | — | Broadcasts `[Broadcast]` / `[Wizwall]` / `[Rwall]` (`Wizard` or `Royalty`) |
| `@zemit` | `@nszemit` | `/noisy`, `/silent` | Emits to all `Room`s belonging to `<zone>` |

---

## `NOSPOOF`, `PARANOID`, and `@ns*` Commands

Spoof-capable emits (`@emit`, `@lemit`, `@pemit`, `@prompt`, `@remit`, `@oemit`, `@zemit`, `@cemit`) pass `speaker` + `spoofable` flags to `SpoofPrefix.ForListener()`:
- If the listener (or the listener's owner) has the **`NOSPOOF`** flag, messages from another speaker are prefixed with `[<SpeakerName>:] `.
- If the listener (or owner) also has **`PARANOID`**, every message (including self-emits) is prefixed with `[<SpeakerName>(#<dbref>)] `.
- Calling an **`@ns*`** command (`@nsemit`, `@nslemit`, `@nspemit`, `@nsprompt`, `@nsremit`, `@nsoemit`, `@nszemit`) omits the spoof prefix when the speaker has `WIZARD` (or `IsGod`) or the `Can_spoof` power (`SpoofPrefix.Omits`). Otherwise the `@ns*` prefix still shows to `NOSPOOF`/`PARANOID` listeners.

---

## Speech & Private Messages

### `say[/noeval] <message>`
`Aliases: ", '`

Evaluates `<message>` (unless `/noeval` is passed), applies the executor's `SPEECHMOD` attribute (`%0` = message, `%1` = `"`), notifies executor `"You say, \"...\""`, and broadcasts `"<Name> says, \"...\""` to the room (`SayCommandHandler.cs`).
*(Shortcuts: typing `"Hello` or `'Hello` invokes `say`; leading `"` or `'` is stripped without preserving switches.)*

### `pose[/nospace] <action>` / `semipose <action>`
`Aliases: :, ;, semipose`

Applies `SPEECHMOD` (`;`, or `:` with space unless `;`, `semipose`, or `/nospace`), formats `"<Name><action>"` or `"<Name> <action>"`, and broadcasts to the executor's room (`PoseCommandHandler.cs`).

### `whisper <player>=<message>`
`Aliases: w`

Finds a `Player` in the executor's current room via `FindInContents`. Whispers privately to `<player>`, or prints `"You whisper to yourself."` when targeting `%#` (`WhisperCommandHandler`).

### `page <player>=<message>` / `page <player> <message>`
`Aliases: p`

Matches `<player>` globally (`FindPlayer`), evaluates `<player>`'s `LockType.Page`, formats outgoing text with executor's `OUTPAGEFORMAT` and incoming text with target's `PAGEFORMAT` (`%0`=message, `%1`=`"`, `%2`=`""`, `%3`=`#target`, `%4`=default line), and delivers to both (`PageCommandHandler`).

---

## Emits, Walls & Formatted Messages

### `@emit <message>` / `@lemit <message>`
`Aliases: \, @lemit, @nsemit, @nslemit`

Applies `SPEECHMOD` (`|`) and broadcasts to everyone in the executor's `Location` (`EmitCommandHandler`). `@ns*` omits `NOSPOOF`/`PARANOID` prefixes for `Wizard` / `Can_spoof`.

### `@pemit[/silent] <target>=<message>` & `@prompt <target>=<message>`
`Aliases: @prompt, @nspemit, @nsprompt`

Resolves `<target>` (`me`, `#dbref`, player, or global object). If `<target>` is a `Room`, broadcasts to the room; otherwise delivers to `<target>` (with `prompt: true` on `@prompt`/`@nsprompt` so sockets emit without a trailing newline unless `@sockset PROMPT_NEWLINES yes` is set). Prints `"Message sent to <name>."` unless `/silent` is set (`PemitCommandHandler`).

### `@remit <room>=<message>`
`Aliases: @nsremit`

Requires `<room>` to be a `#dbref` referring to a `Room`; emits `<message>` to everyone in `<room>` and confirms `"Message sent to <room>."` (`RemitCommandHandler`).

### `@oemit <exclude>=<message>`
`Aliases: @nsoemit`

Excludes `<exclude>` (`me`, `#dbref`, local content, or player) from receiving `<message>` emitted to the executor's current room (`OemitCommandHandler`).

### `@message[/<switch>] <target>=<object>/<attr>`
Evaluates `<object>/<attr>` in an `EvaluationContext(Executor, Enactor)` and delivers the result (`MessageCommandHandler` in `ObjectReadWriteCommandHandlers.cs`):

| Switch | Delivery Target |
|---|---|
| *(none)* | Directly to `<target>` |
| `/remit` | All listeners in `<target>`'s `Location` |
| `/oemit` | Everyone in `<target>`'s `Location` except `<target>` |

### `@wall <msg>` / `@wizwall <msg>` / `@rwall <msg>`
`Restrictions: Wizard or Royalty`

Broadcasts `<prefix> <Name>: <msg>` (`[Broadcast]`, `[Wizwall]`, `[Rwall]`). Note that both `@wizwall` and `@rwall` pass `wizardOnly: true` to `NotifyBroadcast`, restricting recipients to `Wizard`/`God` (`WallCommandHandler.cs`).

### `@zemit[/<switch>] <zone>=<message>`
`Aliases: @nszemit` · `Restrictions: Must control <zone> (Wizard or owner)`

Emits `<message>` to every `Room` whose `Zones` set includes `<zone>` (`ZemitCommandHandler` in `ZoneCommandHandler.cs`).

| Switch | Behavior |
|---|---|
| *(none)* | Notifies `"You zemit to <zone>."` unless the actor is standing inside a room in `<zone>` |
| `/noisy` | Always confirms `"You zemit to <zone>."` |
| `/silent` | Suppresses confirmation |
