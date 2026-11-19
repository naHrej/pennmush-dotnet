# Flags, Powers, Attributes & Locks

Complete tables interrogated from `PennMUSH.Core/Flags/FlagTable.cs`, `PennMUSH.Core/Attributes/`, and `PennMUSH.Core/Locks/`.

---

## Built-in Object Flags (`FlagTable`)

All **45** built-in flags registered in `FlagTable.cs` (`ObjectType.None` applies to all object types). Additional flags and aliases can be managed at runtime with `@flag`.

| Flag | Letter | Allowed Types | Set Privileges | Clear Privileges |
|---|---|---|---|---|
| `CHOWN_OK` | `C` | All | Any | Any |
| `DARK` | `D` | All | Any | Any |
| `GOING` | `G` | All | Internal | Internal |
| `HAVEN` | `H` | `Player` | Any | Any |
| `TRUST` | `I` | All | Inherit | Inherit |
| `LINK_OK` | `L` | All | Any | Any |
| `OPAQUE` | `O` | All | Any | Any |
| `QUIET` | `Q` | All | Any | Any |
| `STICKY` | `S` | All | Any | Any |
| `UNFINDABLE` | `U` | All | Any | Any |
| `VISUAL` | `V` | All | Any | Any |
| `WIZARD` | `W` | All | Wizard, Inherit, Log | Wizard, Inherit |
| `SAFE` | `X` | All | Any | Any |
| `AUDIBLE` | `a` | All | Any | Any |
| `DEBUG` | `b` | All | Any | Any |
| `NO_WARN` | `w` | All | Any | Any |
| `ENTER_OK` | `e` | All | Any | Any |
| `HALT` | `h` | All | Any | Any |
| `NO_COMMAND` | `n` | All | Any | Any |
| `LIGHT` | `l` | All | Any | Any |
| `ROYALTY` | `r` | All | Royalty, Inherit, Log | Royalty, Inherit |
| `TRANSPARENT` | `t` | All | Any | Any |
| `VERBOSE` | `v` | All | Any | Any |
| `ANSI` | `A` | `Player` | Any | Any |
| `COLOR` | `C` | `Player` | Any | Any |
| `MONITOR` | `M` | All | Any | Any |
| `NOSPOOF` | `"` | `Player` | Any, Odark | Any, Odark |
| `PARANOID` | none | `Player` | Any, Odark | Any, Odark |
| `SHARED` | `Z` | `Player` | Any | Any |
| `GAGGED` | `g` | `Player` | Wizard | Wizard |
| `MYOPIC` | `m` | `Player` | Any | Any |
| `TERSE` | `x` | All | Any | Any |
| `FIXED` | `F` | `Player` | Wizard | Wizard |
| `SUSPECT` | `s` | `Player` | Wizard, Mdark, Log | Wizard, Mdark |
| `DESTROY_OK` | `d` | `Thing` | Any | Any |
| `PUPPET` | `p` | `Thing` | Any | Any |
| `NO_LEAVE` | `N` | `Thing` | Any | Any |
| `ABODE` | `A` | `Room` | Any | Any |
| `FLOATING` | `F` | `Room` | Any | Any |
| `JUMP_OK` | `J` | `Room` | Any | Any |
| `NO_TEL` | `N` | `Room` | Any | Any |
| `CLOUDY` | `x` | `Exit` | Any | Any |
| `MISTRUST` | `m` | `Thing`, `Room`, `Exit` | Wizard | Wizard |
| `NO_QUOTA` | none | All | Wizard, Log | Wizard |
| `QUOTAS` | none | All | Wizard, Log | Wizard |

**Built-in Flag Aliases (13)**: `INHERIT` → `TRUST`, `TRACE` → `DEBUG`, `NOWARN` → `NO_WARN`, `NOCOMMAND` → `NO_COMMAND`, `LISTENER` / `WATCHER` → `MONITOR`, `ZONE` → `SHARED`, `COLOUR` → `COLOR`, `DEST_OK` → `DESTROY_OK`, `TEL_OK` / `TELOK` / `TEL-OK` → `JUMP_OK`, `NOQUOTA` → `NO_QUOTA`.

---

## Built-in Powers (`FlagTable`)

All **31** registered powers require `Wizard` privilege to grant or revoke (`Immortal` omits `FlagPrivileges.Log`):

`Announce`, `Boot`, `Builder`, `Chat_Privs`, `Functions`, `Guest`, `Halt`, `Hide`, `Idle`, `Immortal`, `Link_Anywhere`, `Login`, `Long_Fingers`, `No_Pay`, `No_Quota`, `Open_Anywhere`, `Pemit_All`, `Player_Create`, `Poll`, `Queue`, `Quotas`, `Search`, `See_All`, `See_Queue`, `Tport_Anything`, `Tport_Anywhere`, `Can_spoof`, `Can_HTTP`, `Sql_Ok`, `Pueblo_Send`, `Send_OOB`.

**Power Aliases (4)**: `@wall` / `wall` → `Announce`, `Can_nspemit` → `Can_spoof`, `noquota` → `No_Quota`.

---

## Attributes & Attribute Flags

### Name Rules (`AttributeEntry.IsValidName`)
- Maximum **1024** characters, non-empty.
- Permitted characters: alphanumeric, `_`, `-`, `.`, and `` ` `` (for attribute trees like `PARENT`CHILD`), except names cannot start or end with `` ` `` or contain ` `` `.
- Attribute setting syntax:
  - `&<attr> <obj>[=<val>]` — sets or clears a custom attribute.
  - `@_<attr> <obj>[=<val>]` — alternate syntax for custom attributes (`AttributeSetCommandHandler.TryReadName`).
  - `@<std-attr> <obj>[=<val>]` — resolves standard attributes by exact or unique prefix match via `StandardAttributes.TryResolve` (e.g., `@desc me=Hi`, `@su me=...`, `@asucc ...`).

### Attribute Flags (`AttributeFlags`)
`Quiet`, `Internal`, `Wizard`, `Locked`, `NoProg`, `MDark`, `Private`, `NoCopy`, `Visual`, `Regexp`, `Case`, `Safe`, `Root`, `RLimit`, `Enum`, `Command`, `Listen`, `NoDump`, `PrefixMatch`, `Veiled`, `Debug`, `Nearby`, `Public`, `Anon`, `NoName`, `NoSpace`, `MHear`, `AHear`, `NoDebug`.

### Standard Attribute Highlights (`StandardAttributes.cs`)
- **Presentation & Formatters**: `DESCRIBE` (`DESC`), `IDESCRIBE` (`IDESC`), `DESCFORMAT`, `IDESCFORMAT`, `CONFORMAT`, `EXITFORMAT`, `INVFORMAT`, `NAMEFORMAT`, `CHATFORMAT`, `PAGEFORMAT`, `OUTPAGEFORMAT`, `MONIKER`, `NAMEACCENT`, `DOING`, `SEX`, `TZ`.
- **Action / Emit Triads**: `SUCCESS`/`OSUCCESS`/`ASUCCESS`, `FAILURE`/`OFAILURE`/`AFAILURE`, `DROP`/`ODROP`/`ADROP`, `ENTER`/`OENTER`/`OXENTER`/`AENTER`, `LEAVE`/`OLEAVE`/`OXLEAVE`/`ALEAVE`, `TPORT`/`OTPORT`/`OXTPORT`/`ATPORT`, `USE`/`OUSE`/`AUSE`/`RUNOUT`, `GIVE`/`OGIVE`/`AGIVE`, `RECEIVE`/`ORECEIVE`/`ARECEIVE`, `MOVE`/`OMOVE`/`OXMOVE`/`AMOVE`, `BUY`/`OBUY`/`ABUY`, `PAYMENT`/`OPAYMENT`/`APAYMENT`, `FOLLOW`/`OFOLLOW`/`AFOLLOW`, `UNFOLLOW`/`OUNFOLLOW`/`AUNFOLLOW`, `ZENTER`/`OZENTER`/`AZENTER`, `ZLEAVE`/`OZLEAVE`/`AZLEAVE`, plus lock-failure messages (`EFAIL`/`OEFAIL`/`AEFAIL`, `LFAIL`/`OLFAIL`/`ALFAIL`, `UFAIL`/`OUFAIL`/`AUFAIL`).
- **System / Internal / Locked**: `XYXXY` (`Internal`, `Locked`, `Wizard`, password hash), `RQUOTA` (`MDark`, `Locked`, remaining building quota), `LASTIP` / `LASTSITE` / `LAST` / `LASTLOGOUT` / `LASTFAILED` / `LASTPAGED` / `REGISTERED_EMAIL`, `QUEUE`, `SEMAPHORE`, `FOLLOWERS`/`FOLLOWING`, `MAIL*` folder/filter metadata.
- **Quick-Reference Attributes (`%va–%vz`, `%wa–%wz`, `%xa–%xz`)**: **78** built-in 2-letter attributes (`VA–VZ`, `WA–WZ`, `XA–XZ`).

---

## Locks (`PennMUSH.Core/Locks/`)

### Standard Lock Types (`LockType.cs`)
All **13** built-in lock types (case-insensitive string keys stored per object):
`Basic`, `Enter`, `Use`, `Page`, `Leave`, `Drop`, `Give`, `Teleport`, `Speech`, `Chzone`, `Zone`, `Follow`, `Pay`.

Shortcuts: `@elock`/`@eunlock` operate on `Enter`; `@ulock`/`@uunlock` operate on `Use`; `@lock/<type>` / `@unlock/<type>` target any lock type.

### Lock Flags (`LockFlags.cs`)
Modified with `@lset <obj>/<lock>=[!]<flag>`:
`Visual` (`0x1`), `Private` (`0x2`), `Wizard` (`0x4`), `Locked` (`0x8`), `NoClone` (`0x10`), `Owner` (`0x100`).

### Lock Expression Grammar (`LockParser.cs`)
`LockParser.Parse()` supports Boolean expressions over dbrefs (`#n`, `=#n`, numeric `n`, or `-n`):
- Logical OR: `<expr> | <expr>`
- Logical AND: `<expr> & <expr>`
- Negation: `!<expr>`
- Grouping: `( <expr> )`
- Empty input evaluates to constant `*UNLOCKED*` (`ConstantLockNode(true)`).
> **Parity Note**: `LockParser.cs` evaluates dbref literals and Boolean combinators (`&`, `|`, `!`, `(...)`); unparsed non-dbref identifiers currently default to `ConstantLockNode(true)`.
