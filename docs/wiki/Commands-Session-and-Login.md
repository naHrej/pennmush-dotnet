# Commands: Session & Login

Pre-login socket commands and logged-in session, authentication, help, and descriptor configuration commands interrogated from `PennMUSH.Server/Engine/MUSHServer.cs`, `PennMUSH.Core/Commands/Handlers/`, and `PennMUSH.Server/Session/ConnectionDirectory.cs`.

## Quick Reference

| Command | Aliases | Restrictions | Switches / Notes |
|---|---|---|---|
| `connect` *(pre-login)* | `conn`, `co` | SiteLock (`Connect` / `Guest`) | Authenticates an existing player before login |
| `create` *(pre-login)* | `cr` | SiteLock (`Create`) | Creates and logs in a new player |
| `WHO` *(pre-login)* | — | — | Pre-login unhidden online roster |
| `QUIT` *(pre-login)* | — | — | Sends quit screen (`txt/quit.txt` / `.html`) and disconnects |
| `who` | `session` | — | Connected players/descriptors (`session` adds byte counts for `See_All`/`Royalty`) |
| `doing` | `@doing` | — | Views or sets your `DOING` attribute |
| `@hide` | — | `Royalty` or `Hide` power | `/on`, `/yes`, `/off`, `/no` |
| `@boot` | — | `Royalty` or `Boot` power *(for others)* | `/port`, `/me`, `/silent` |
| `@poll` | — | `Wizard` or `Poll` power *(to set/clear)* | `/clear` |
| `@sockset` | — | `nogagged` | Inspects or updates descriptor options |
| `@password` | — | `noguest` | Changes your own `XYXXY` password hash |
| `@newpassword` | — | `wizard` | `/generate` |
| `@pcreate` | — | `Wizard` or `Player_Create` power | Creates player without logging in (`PlayerCreator`) |
| `score` | — | — | Displays your current pennies |
| `help` | `?` | — | Looks up help topics with `SuggestionVocabulary` fallback |

---

## Pre-Login Commands (`MUSHServer.HandleLoginAsync`)

Handled on an unauthenticated `MUSHConnection` before a player logs in (`PennMUSH.Server/Engine/MUSHServer.cs`):

### `connect <name> [<password>]`
`Aliases: conn, co` · `Checks: SiteLockTable.Connect / SiteLockTable.Guest`

Verifies `<name>` against existing `Player` objects (`PasswordHash.Accepts`). On success:
- Marks the player connected, updates `ConnectionLogStore`, greets with `MudName`, and sends `txt/motd` (plus `txt/wizmotd` if `Royalty+`) and announced `@motd` lines (`MotdBoard.ConnectLines`).
- Auto-joins the `Public` channel, reports unread `@mail` count if non-zero, triggers arrival `look`, and broadcasts `<name> has connected.` to the room.

```text
connect One secret
co Bob mypass
```

### `create <name> <password>`
`Aliases: cr` · `Checks: SiteLockTable.Create`

Creates a new `Player` when `<name>` is unused (`Owner` = self, `Home` = `default_home`, `Location` = `player_start`, `Pennies` = `starting_money`, `Warnings` = `Normal`, hashed `XYXXY`), saves `Storage`, auto-joins `Public`, sends `txt/newuser` + announced `@motd`, and looks around the starting room.

```text
create Adventurer hunter2
```

### `WHO` *(pre-login)*
Displays unhidden connected players (`Player Name`, `Location`, `Connected` duration, and `Total online: N`).

### `QUIT` *(pre-login)*
Sends `Config.QuitFile` (or `"Goodbye!"`), closes the socket, and logs the disconnect (`"quit"`) in `connlog.db`.

---

## Logged-In Session & Account Commands

### `who [<pattern>]` / `session [<pattern>]`
`Aliases: session` · `Tags: Royalty / See_All (extended columns)`

Lists active descriptors sorted by descriptor number, filtered by optional wildcard (`*`/`?`) or prefix `<pattern>` against player names and `ALIAS` attributes (`SessionCommandHandlers.cs`):
- **Mortals (`who`)**: Shows `Player Name`, `On For`, `Idle`, `D`/`H` dark/hidden mark, and `DOING` under the current `@poll` header (omitting hidden connections).
- **`Royalty` or `See_All` (`who`)**: Shows `#loc`, `On For`, `Idle`, `Cmds`, `Des`, and `Host` (`(Dark)` / `(Hide)`), including connecting/hidden sessions.
- **`session`**: With `Royalty`/`See_All`, replaces `Host` with `Sent`, `Recv`, and `Pend` byte counters. `session` is not passed through `who_mogrifier`.
- **`who_mogrifier`**: When set, `who` evaluates `WHOHEADER`, `WHOROW`, and `WHOFOOTER` on that object (one row evaluation per visible connection). `Royalty` / `See_All` use `WHOAHEADER`, `WHOAROW`, and `WHOAFOOTER`. A missing attribute keeps the hardcoded line for that slot. An empty result omits that line. The pieces are delivered as one block, so an HTML client wraps them once and a table opened in the header can be closed in the footer. See `help who_mogrifier`.

```text
who
who W*
session
```

### `doing [<text>]`
`Aliases: @doing`

Without arguments, prints your current `DOING` attribute (`"Your doing is: ..."` or `"You have no doing set."`). With text, sets `DOING` on `%#` (`Action`/`@doing` strips nothing beyond `.Trim()`, while `who` flattens `\r`/`\n`/`\t` to spaces when rendering).

```text
doing Building the docs wiki
```

### `@hide[/<switch>] [<player|descriptor>]`
`Restrictions: Royalty or Hide power (Wizard to hide other players/descriptors)`

Toggles whether a connection appears on mortal `WHO` lists (`HideCommandHandler.cs`).

| Switch | Behavior |
|---|---|
| *(none)* | Toggles hidden state (`!Hidden`) |
| `/on`, `/yes` | Forces connection hidden |
| `/off`, `/no` | Forces connection visible |

```text
@hide
@hide/on
@hide/off #4
```

### `@boot[/<switch>] <player|descriptor|me>`
`Restrictions: Royalty or Boot power for other players; non-God cannot boot God`

Queues descriptor disconnection (`BootCommandHandler.cs`), flushed on the server tick loop (`FlushDisconnects()`).

| Switch | Behavior |
|---|---|
| *(none)* | Boots all connections belonging to `<player>` (or idle duplicate sessions >60s old if target is `me`) |
| `/me` | Boots your own duplicate connections that have been idle >60s (preserving your least-idle descriptor) |
| `/port` | Boots a single numeric `<descriptor>` (cannot boot the invoking source descriptor; advises `QUIT`) |
| `/silent` | Suppresses `"You are politely shown to the door."` to the target |

```text
@boot/me
@boot TroubleMaker
@boot/port/silent 7
```

### `@poll[/clear] [<text>]`
`Restrictions: Viewer = any; set/clear = Wizard or Poll power`

Without arguments, displays `"The current poll is: <poll>"`. Wizards or players with `Poll` set the `WHO` column header (up to `MaxLength = 40` characters, cleaning `\r`/`\n`/`\t` to spaces), or reset it to `"Doing"` with `/clear`.

```text
@poll What is your favorite zone?
@poll/clear
```

### `@sockset [<player|descriptor>=][<option> <value> ...]`
`Restrictions: nogagged; Wizard required to inspect non-owned with See_All or modify another player's session; subject must be Wizard for NOQUOTA`

Inspects or configures per-connection presentation and quota flags (`SocksetCommandHandler.cs` & `ConnectionDirectory.cs`):
- Supported options: `OUTPUTPREFIX`, `OUTPUTSUFFIX`, `PUEBLO`, `TELNET`, `WIDTH`, `HEIGHT`, `TERMINALTYPE`, `COLORSTYLE`/`COLOURSTYLE` (`auto`, `plain`/`none`, `hilite`/`highlight`, `16color`, `xterm256`/`256`, `rgb24`/`truecolor`), `PROMPT_NEWLINES`, `STRIPACCENTS`/`NOACCENTS`, `NOQUOTA`.
- See [Colors, Markup & Presentation](Colors-Markup-and-Presentation) for exact option effects.

```text
@sockset
@sockset COLORSTYLE rgb24 STRIPACCENTS yes
@sockset OUTPUTPREFIX >>>
```

### `@password <old>=<new>`
`Restrictions: noguest; Player only`

Verifies `<old>` against your stored hash (`PasswordHash.Accepts`), validates `<new>` (`PasswordHash.Ok`), and stores the new `XYXXY` hash (`PasswordCommandHandler.cs`).

```text
@password oldpass=newpass123
```

### `@newpassword[/generate] <player>[=<new>]`
`Restrictions: wizard (cannot target God unless executor is God)`

Resets `<player>`'s password (generating a random password if `/generate` is given), notifies `<player>` if online, and appends an entry to `WizardLogKind.Wiz` (`PasswordCommandHandler.cs`).

```text
@newpassword Bob=temp1234
@newpassword/generate Bob
```

### `@pcreate <name>=<password>[,<dbref>]`
`Restrictions: Wizard or Player_Create power (checked inside PlayerCreator)`

Creates a new player object (`PlayerCreator.Create`) without connecting them, supporting optional garbage `<dbref>` reuse when supplied after `,` (`PcreateCommandHandler.cs`).

```text
@pcreate BuilderOne=secretpass
```

### `score`
Reports your current penny balance (`execObj.Pennies`) (`ObjectLifecycleCommandHandlers.cs`).

```text
score
```

### `help [<topic>]`
`Aliases: ?`

Queries `HelpStore` for `<topic>` (default `"help"`). When no entry matches, checks `SuggestionVocabulary.Closest("HELP", topic)` (`@suggest`) to offer `"Did you mean ...?"`. For WebSocket/HTML clients (`PlayerReceivesHtml`), wraps plain text output in `<div style="white-space:pre-wrap">...</div>` with non-breaking spaces (`HelpCommandHandler.cs`).

```text
help @dig
? commands
```
