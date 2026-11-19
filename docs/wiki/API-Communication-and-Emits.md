# API: Communication & Emits

Side-effect broadcast functions, no-spoof variants, formatted speech helpers, and audible bells extracted from `PennMUSH.Core/MUSHcode/Functions/Section9And18Functions.cs` (**8** functions), `ConnectionAndZoneFunctions.cs` (**8** functions), and `LateSubsystemFunctions.cs` (**1** function: `beep()`).

* Side-effect emit functions require side-effect functions to be enabled (`SideEffectsEnabled && !ctx.NoSideFX`; otherwise the function gate denies execution).
* For channel emits (`cemit()`, `nscemit()`), see [Channels & Mail](API-Channels-and-Mail); for `@emit`/`@pemit`/`@remit`/`@oemit`/`@message`/`@zemit` commands, see [Communication Commands](Commands-Communication); for WebSocket OOB frames (`client_emit()`, `oob()`), see [Client & MUjs UI](API-Client-and-MUjs-UI).

---

## Side-Effect Emits & Notifications

| Function | Signature | Notes |
|---|---|---|
| `emit` | `emit(<message>)` | `` `sideEffect` `` Emits `<message>` to caller's immediate `Location`; returns `""` |
| `lemit` | `lemit(<message>)` | `` `sideEffect` `` Emits `<message>` to caller's outermost enclosing room (`where(me)`) |
| `pemit` | `pemit(<targets>, <message>)` | `` `sideEffect` `` Sends `<message>` directly to space-separated `<targets>` |
| `prompt` | `prompt(<targets>, <message>)` | `` `sideEffect` `` Sends `<message>` with `Prompt = true` (no trailing newline prompt) |
| `remit` | `remit(<room>, <message>)` | `` `sideEffect` `` Emits `<message>` to all contents of `<room>` |
| `oemit` | `oemit(<excluded>, <message>)` | `` `sideEffect` `` Emits to room contents excluding space-separated `<excluded>` objects |
| `message` | `message(<target>, <[obj/]attr>, <msg>[, <args...>])` | `` `sideEffect` `` Formats `<msg>` through `u(<[obj/]attr>, <msg>, ...)` if present, then sends to `<target>` |
| `zemit` | `zemit(<zone>, <message>)` | `` `sideEffect` `` Emits `<message>` (`Spoofable: true`) to all rooms inside `<zone>` |
| `nszemit` | `nszemit(<zone>, <message>)` | `` `sideEffect` `` Emits `<message>` (`Spoofable: false`) to all rooms inside `<zone>` |
| `nsemit` | `nsemit(<message>)` | `` `sideEffect` `` No-spoof registration routed to `emit()` |
| `nslemit` | `nslemit(<message>)` | `` `sideEffect` `` No-spoof registration routed to `lemit()` |
| `nsoemit` | `nsoemit(<excluded>, <message>)` | `` `sideEffect` `` No-spoof registration routed to `oemit()` |
| `nspemit` | `nspemit(<targets>, <message>)` | `` `sideEffect` `` No-spoof registration routed to `pemit()` |
| `nsprompt` | `nsprompt(<targets>, <message>)` | `` `sideEffect` `` No-spoof registration routed to `prompt()` |
| `nsremit` | `nsremit(<room>, <message>)` | `` `sideEffect` `` No-spoof registration routed to `remit()` |

### Emit Behavior Notes
* **`pemit()` / `prompt()`**: Split `<targets>` on whitespace, resolving `#dbref` or `MatchObject`. Unmatched tokens are silently skipped.
* **`oemit()`**: Resolves all space-separated tokens in `<excluded>`, collecting their IDs and taking the enclosing location (`obj.Location` when valid, else `ctx.Location`), then delivers `NotificationScope.Target` to every content object not in the excluded set.
* **`message(<target>, <[obj/]attr>, <msg>[, <extra...>])`**: Returns `#-1 NO MATCH` if `<target>` fails to resolve. `<[obj/]attr>` defaults `<obj>` to `ctx.Executor` if `/` is absent; if that attribute exists on `<obj>`, it is evaluated via `Ufun` with `%0` = `<msg>` and `%1..` = `<extra...>` before delivery, otherwise raw `<msg>` is sent directly.
* **`ns*` variants**: `nszemit()` explicitly sets `Spoofable: false`; `nsemit`, `nslemit`, `nsoemit`, `nspemit`, `nsprompt`, and `nsremit` are registered in `ConnectionAndZoneFunctions.cs` as side-effect aliases directly invoking the corresponding `Section9And18Functions` handlers.

---

## Pure Formatting & Terminal Bell

| Function | Signature | Notes |
|---|---|---|
| `speak` | `speak(<speaker>, <text>[, <verb>])` | Formats speech, `:pose`, or `;semipose` as `<speaker> says, "<text>"` |
| `beep` | `beep([<count>])` | Emits `1..5` ASCII bell (`\a`) chars (`Wizard` or `Royalty` only) |

* **`speak(<speaker>, <text>[, <verb>])`**: Resolves `<speaker>` via `#dbref`/`MatchObject` (falling back to the literal `<speaker>` string if unmatched). Leading `:` formats `<name> <text>` (trimming leading space); leading `;` formats `<name><text>`; anything else uses `Feedback.SpeakSays` with `<verb>` defaulting to `Feedback.SpeakDefaultSayVerb`.
* **`beep([<count>])`**: Requires `Wizard` or `Royalty` (`#-1 PERMISSION DENIED`). `<count>` defaults to `1` and must be in `1..5` (`#-1 OUT OF RANGE`, or `#-1 ARGUMENT MUST BE INTEGER`).

```text
think [speak(me, Hello world!)]
-> Wizard says, "Hello world!"

think [speak(me, :waves happily.)]
-> Wizard waves happily.

think [null([pemit(%#, Welcome back!)])]
```

---

See also: [Communication Commands](Commands-Communication) · [Channels & Mail](API-Channels-and-Mail) · [Functions Index](Functions-Index) · [Home](Home)
