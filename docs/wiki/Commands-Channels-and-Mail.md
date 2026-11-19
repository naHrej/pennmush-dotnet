# Commands: Channels & Mail

Channel management, chat aliases, channel locks, and threaded `@mail` / `@malias` commands interrogated from `PennMUSH.Core/Commands/Handlers/`.

## Quick Reference

| Command | Aliases | Restrictions | Key Switches |
|---|---|---|---|
| `@channel` | — | `Wizard` *(create)*, owner/Wizard *(delete)* | `/add`/`/create`, `/delete`/`/remove`, `/join`/`/on`, `/leave`/`/off`, `/who`, `/title`, `/recall`, `/list` |
| `@clock` | — | Channel `CanModify` (owner, Wizard, or `mod` lock) | `/join`, `/speak`, `/mod`, `/see`, `/hide` |
| `@chat` | `+c` *(+ `+<channel>` prefix)* | `speak` lock & membership *(Wizard bypasses membership)* | — |
| `@cemit` | `@nscemit` | Channel owner or `Wizard` | `/noisy` (`@nscemit` suppresses spoof prefix for Wizard/`Can_spoof`) |
| `addcom` | — | Valid alias (`1..15` chars, no `` ` ``) | Maps personal chat alias to a channel & auto-joins |
| `delcom` | — | — | Removes alias and leaves channel if it was your last alias for it |
| `comtitle` | — | Must be on channel | Views or sets/clears channel title (max `80` chars) |
| `comlist` | — | — | Lists your channel aliases, statuses, and titles |
| `@clist` | — | — | `/full` (lists channels, counts, flags `P`/`Q`/`A`/`D`, status) |
| `@mail` | — | — | `/read`, `/clear`, `/unclear`, `/purge`, `/folder`, `/status`, `/urgent` |
| `@malias` | — | `nogagged` *(Wizard/Royalty/God for `/chown`, `/stats`, `/all`, `/nuke`)* | `/list`, `/all`, `/members`/`/who`, `/create`, `/set`, `/destroy`, `/add`, `/remove`, `/describe`/`/desc`, `/rename`, `/stats`/`/stat`, `/chown`, `/useflag`, `/seeflag`, `/nuke` |

---

## Channel Commands

### `@channel[/<switch>] <args>`
Manages server-wide channels persisted in `RuntimeStateStore` (`channels`, `channel_members`, `channel_locks` tables) (`ChannelCommandHandler.cs`):

| Switch | Syntax | Notes |
|---|---|---|
| *(none)* or `/list` | `@channel` | Lists visible channels with owner, member count, `[Joined]`/`[Off]` |
| `/add`, `/create` | `@channel/add <name>` | **Wizard only** |
| `/delete`, `/remove` | `@channel/delete <name>` | Owner or Wizard |
| `/join`, `/on` | `@channel/join <name>` | Checks `see` and `join` locks (Wizards warn and join anyway) |
| `/leave`, `/off` | `@channel/leave <name>` | Removes you from channel |
| `/who` | `@channel/who <name>` | Lists visible members and `[Title]` (respects `hide` lock unless self/owner/Wizard) |
| `/title` | `@channel/title <name>=<title>` | Sets your member title |
| `/recall` | `@channel/recall <name>[=<count>]` | Shows last `<count>` buffered messages (default `20`) |

### `@clock/<type> <channel>[=<key>]`
Requires `ChannelAccess.CanModify` (owner, Wizard, or passes `mod` lock). Sets or clears a channel lock expression (`ClockCommandHandler.cs`). Note `Understands()` restricts lock keys to numeric dbrefs and Boolean combinators (`#`, `&`, `|`, `!`, `(...)` — alphabetic keys are rejected with `"I don't understand that key."`).

| Switch | Lock Controlled |
|---|---|
| `/join` | Joinlock |
| `/speak` | Speaklock |
| `/mod` | Modlock |
| `/see` | Seelock |
| `/hide` | Hidelock |

```text
@clock/join Staff=#1 | #5
@clock/join Staff=
```

### `@chat <channel>=<message>` / `+<channel> <message>`
`Aliases: +c` *(plus `+<channel> <msg>` and `+<channel>/<switch>` prefix dispatch in `CommandDispatcher.cs`)*

Broadcasts to `<channel>` if visible, speaker passes `speak` lock, and speaker is a member (or `Wizard`). Prefixing `<message>` with `:` or `;` sends a pose/semipose; delivery formats through listeners' `CHATFORMAT` attribute (`FormatAttributes.DeliverChannel`: `%0`=token, `%1`=channel, `%2`=message, `%3`=sender, `%4`=title, `%5`=formatted, `%6`=`"says"`, `%7`=noise) (`ChatCommandHandler.cs`).

### `@cemit[/noisy] <channel>=<message>`
`Aliases: @nscemit` · `Restrictions: Channel owner or Wizard`

Emits raw `<message>` to `<channel>` (prefix-free by default; `/noisy` emits noisily). `@nscemit` suppresses `NOSPOOF`/`PARANOID` prefixes for `Wizard` / `Can_spoof` (`CemitCommandHandler.cs`).

### `addcom`, `delcom`, `comtitle`, `comlist`, `@clist`
Registered together on `ComCommandHandler.cs`:
- **`addcom <alias>=<channel>`**: Validates `<alias>` (`1..15` chars, alphanumeric/`_`/`-`/`.`, no `` ` ``), joins `<channel>` via prefix/partial `MatchChannel`, and records the alias.
- **`delcom <alias>`**: Deletes `<alias>`; if no remaining aliases point to that channel, removes you from the channel.
- **`comtitle <alias>[=<title>]`**: Displays, sets (max `80` chars, no control/non-space whitespace), or clears (`=` empty) your member title.
- **`comlist`**: Prints your alias table (`Alias`, `Channel`, `Status`, `Title`).
- **`@clist[/full] [<prefix>]`**: Lists channels matching `<prefix>` (`Name`, `Users`, `Status`, plus `Msgs` and `Flags` `P`/`Q`/`A`/`D` with `/full`).

---

## Mail & Mail Alias Commands

### `@mail[/<switch>] [<target>=<subject>/<body|message>]`
Per-player foldered mail (`0..15`), persisted in `RuntimeStateStore` (`mail_messages`, `mail_active_folders`) (`MailCommandHandler.cs`):

| Syntax | Behavior |
|---|---|
| `@mail` | Lists messages in current folder (`[U]` Unread, `[C]` Cleared, `!` Urgent) |
| `@mail <player|+malias>=[<subj>/]<body>` | Sends mail to player or `@malias` (`+name`); optional `@mail/urgent` flag sets urgent `!` |
| `@mail/read <num>` | Displays message and marks read |
| `@mail/clear <num>` / `@mail/unclear <num>` | Toggles cleared flag for `/purge` |
| `@mail/purge` | Permanently deletes all cleared messages |
| `@mail/folder [<0..15>]` | Reports or switches active folder (`0..15`) |
| `@mail/status` | Reports total, unread, and cleared counts |

### `@malias[/<switch>] [<args>]`
`Restrictions: nogagged` (`MUSHServer.cs`); `Player` only to create; `Royalty+` for `/all` & `/stats`; `Wizard` for `/chown`; `God` for `/nuke` (`MaliasCommandHandler.cs`).

Manages `+`-prefixed mailing lists (`MaxBatch = 100` members per operation, persisted in `mail_aliases` / `mail_alias_members`):

| Syntax / Switch | Behavior |
|---|---|
| `@malias` or `/list` | Lists visible mail aliases |
| `/all` | Lists all aliases (`Royalty+`, else falls back to `/list`) |
| `@malias <+alias>` or `/members` / `/who` | Shows alias members (gated by `CanSeeMembers`) |
| `@malias <+alias>=<list>` or `/create` | Creates new alias (`OwnerBit \| Members` use flags, `OwnerBit` see flags) |
| `/set <+alias>=<list>` | Replaces alias member list |
| `/add <+alias>=<list>`, `/remove <+alias>=<list>` | Adds or removes players |
| `/describe` or `/desc <+alias>=<text>` | Changes alias description |
| `/rename <+alias>=<+new>` | Renames alias |
| `/useflag <+alias>=<flags>`, `/seeflag <+alias>=<flags>` | Sets use/visibility flags |
| `/chown <+alias>=<player>` | **Wizard** transfers ownership |
| `/stats` / `/stat` | **Royalty+** prints alias count and capacity |
| `/destroy <+alias>` | Destroys alias |
| `/nuke` | **God only** clears every mail alias |
