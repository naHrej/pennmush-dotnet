# API: Channels & Mail

Chat channel inspection, broadcasts, channel locks, in-game mail queries/sending, and mail aliases extracted from `PennMUSH.Core/MUSHcode/Functions/ChannelFunctions.cs` (**16** functions), `MailFunctions.cs` (**12** functions), and `ConnectionAndZoneFunctions.cs` (**1** function: `nscemit()`).

* See [`@channel`, `@clock`, `@chat`, `@mail`, `@malias`](Commands-Channels-and-Mail) for interactive commands and `Flags, Powers, Attributes & Locks` ([link](Flags-Powers-Attributes-and-Locks)) for channel/mail flags.

---

## Channel Functions (`ChannelFunctions.cs` + `nscemit`)

| Function | Signature | Notes |
|---|---|---|
| `channels` | `channels([<player>][, <sep>])` | Lists all channels or `<player>`'s channels (1-char arg acts as global `<sep>`) |
| `cusers` | `cusers(<channel>)` | Member count on `<channel>` (`#-1 NO CHANNEL GIVEN` if empty) |
| `cwho` | `cwho(<channel>[, <status>[, <skipGagged?>]])` | `#dbref` list (`<status>`: `on` [default], `off` [hidden], `all`) |
| `cdesc` | `cdesc(<channel>)` | Channel description string |
| `cowner` | `cowner(<channel>)` | Owner `#dbref` of `<channel>` |
| `cmsgs` | `cmsgs(<channel>)` | Total message count sent on `<channel>` |
| `cmogrifier` | `cmogrifier(<channel>)` | Mogrifier `#dbref` if valid, else `#-1` |
| `cbuffer` | `cbuffer(<channel>)` | Buffer capacity (`BufferCapacity`) of `<channel>` |
| `cbufferadd` | `cbufferadd(<channel>, <text>)` | `` `sideEffect` `` Appends `<text>` directly to channel recall buffer; returns `""` |
| `crecall` | `crecall(<channel>[, <n>[, <start>[, <delim>[, <stamp?>]]]])` | Recalls up to `<n>` (default `10`, `0` = all) buffered messages from 1-based `<start>` |
| `cemit` | `cemit(<channel>, <message>)` | `` `sideEffect` `` Spoofable broadcast to `<channel>`; returns `""` |
| `nscemit` | `nscemit(<channel>, <message>)` | `` `sideEffect` `` *(`ConnectionAndZoneFunctions.cs`)* Non-spoofable (`Spoofable: false`) `cemit()` |
| `cflags` | `cflags(<channel>[, <player>])` | Short channel flags (`A D O P Q`) or member flags (`G H`) |
| `clflags` | `clflags(<channel>[, <player>])` | Long flag names (`Admin Disabled Open Player Quiet` / `Gag Hide`) |
| `clock` | `clock(<channel>[/<type>][, <key>])` | Reads or updates (`join`/`speak`/`mod`/`see`/`hide`) channel lock (default `join`) |
| `cstatus` | `cstatus(<channel>, <player>)` | Member status: `On`, `Gag`, or `Off` |
| `ctitle` | `ctitle(<channel>, <player>)` | Member's `@comtitle` on `<channel>` (or `""`) |

### Channel Notes & Errors
- Most channel queries return `#-1` (`MushError.Nothing`) on missing arguments and `#-1 NO MATCH` if `<channel>` (matched via `MatchChannel`) is not found.
- **`clock(<channel>[/<type>][, <key>])`**: Valid `<type>` values are `join`, `speak`, `mod`, `see`, and `hide` (`#-1 NO SUCH LOCK TYPE` otherwise). Passing `<key>` requires side effects enabled (`SideEffectsEnabled && !NoSideFX`) and either `Wizard` or channel ownership (`#-1 PERMISSION DENIED` otherwise); clearing `<key>` passes empty string to unset the lock. Note that `clock()` is not flagged `sideEffect: true` at registration, so read calls work with side effects disabled.
- **`cflags()` / `clflags()`**: With 2 arguments, returns `#-1 NOT ON CHANNEL` if `<player>` is not subscribed.
- **`cwho()`**: When `<status>` is `on`, hidden members are omitted unless caller satisfies `CanPrivWho`. `<status>` values outside `on`/`off`/`all` return `#-1 INVALID ARGUMENT`.
- **`cemit()` vs `client_emit()`**: Channel emits use `cemit()`, whereas WebSocket OOB frames were separated onto `client_emit()` in [Client & MUjs UI](API-Client-and-MUjs-UI).

```text
think [channels(me)]
think [cwho(Public, on, 1)]
think [crecall(Public, 5, 1, %r, 1)]
```

---

## Mail & Mail Alias Functions (`MailFunctions.cs`)

| Function | Signature | Notes |
|---|---|---|
| `mail` | `mail([<player> \| [<player>, ]<[f:]n>])` | Message count, `total unread cleared` triple, or message body |
| `maillist` | `maillist([<player>][, <[folder:]filter>])` | Space-separated `folder:num` list matching filter (`all`, `unread`, `read`, `cleared`, `tagged`, `urgent`, `n`, `lo-hi`) |
| `mailfrom` | `mailfrom(<[player, ][folder:]num>)` | Sender `#dbref` of specified message |
| `mailsubject` | `mailsubject(<[player, ][folder:]num>)` | Subject line of specified message |
| `mailtime` | `mailtime(<[player, ][folder:]num>)` | Local send timestamp (`ddd MMM d HH:mm:ss yyyy`) |
| `mailstatus` | `mailstatus(<[player, ][folder:]num>)` | 4-char status flags (`N`/`-`, `C`/`-`, `U`/`-`, `T`/`-`) |
| `mailsend` | `mailsend(<recipients>, <[subject/]body>)` | `` `sideEffect` `` Sends mail to space-separated players; returns `""` or `#-1 NO MATCH` |
| `mailstats` | `mailstats([<player>])` | `<sentTotal> <recTotal>` |
| `maildstats` | `maildstats([<player>])` | `<sentTotal> <sentUnread> <sentCleared> <recTotal> <recUnread> <recCleared>` |
| `mailfstats` | `mailfstats([<player>])` | 8 fields adding byte counts: `<sTot> <sUnr> <sClr> <sBytes> <rTot> <rUnr> <rClr> <rBytes>` |
| `folderstats` | `folderstats([<player>][, <folder>])` | `<total> <unread> <cleared>` for current or specified folder (`#-1 FOLDER MUST BE AN INTEGER`) |
| `malias` | `malias([<name>[, <sep>]])` | Lists visible `+<alias>` names (1-char arg acts as global `<sep>`) or expands `<name>` member `#dbref`s |

### Mail Permissions & Behavior Notes
- `mail()`, `maillist()`, `mailstats()`, `maildstats()`, and `folderstats()` enforce `CanAccessMail` (`target == Executor` or `Controls(actor, target)` -> `#-1 PERMISSION DENIED`). Note `mailfstats()` and `mailfrom`/`mailsubject`/`mailtime`/`mailstatus` do not check `CanAccessMail` in the current implementation.
- **`mailsend(<recipients>, <[subject/]body>)`**: `<recipients>` splits on spaces (`#dbref` or player name). If `<body`> contains `/` (`slash > 0 && slash < Length - 1`), the prefix before the first `/` becomes the subject; otherwise `Feedback.MailNoSubject` is used. Returns `#-1 TOO FEW ARGUMENTS` if < 2 args or `#-1 NO MATCH` if no recipients matched a `Player` object.
- **`mailstatus()`**: Each slot shows its letter when active or `-` when unset: `N` (unread), `C` (cleared), `U` (urgent), `T` (tagged). Missing messages return `#-1 OUT OF RANGE`.

```text
think [mail(me)]
think [maillist(0:unread)]
think [null([mailsend(*Bob, Meeting/Starting in 5 minutes.)])]
```

---

See also: [Channels & Mail Commands](Commands-Channels-and-Mail) · [Flags, Powers, Attributes & Locks](Flags-Powers-Attributes-and-Locks) · [Functions Index](Functions-Index) · [Home](Home)
