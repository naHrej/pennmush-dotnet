# API: Connections & ConnLog

Connected descriptor metrics, `WHO` roster queries, terminal dimensions, TLS/password verification, and `connlog.db` history queries extracted from `PennMUSH.Core/MUSHcode/Functions/ConnectionAndZoneFunctions.cs` (**21** functions), `DbFunctions.cs` (**6** functions), `CryptoFunctions.cs` (**1** function: `ssl()`), and `LateSubsystemFunctions.cs` (**4** functions).

* See [`who`/`session`, `@hide`, `@sockset`](Commands-Session-and-Login) for session commands and [Server Configuration & Persistence](Server-Configuration-and-Persistence) for `connlog.db` schema details.
* For zone-filtered WHO functions (`zwho()`, `zmwho()`), see [Objects & Database](API-Objects-and-Database).

---

## Descriptor Telemetry & Terminal State

Functions accepting `<player|port>` match an integer descriptor first, then `#dbref` or `*player` name (hiding `DARK`/hidden descriptors unless caller satisfies `CanSeeAll` or queries their own player).

| Function | Signature | Notes |
|---|---|---|
| `conn` | `conn(<player\|port>)` | Connected duration in seconds (`-1` if disconnected/hidden) |
| `idle` | `idle(<player\|port>)` | Seconds since last activity (`-1` if disconnected/hidden) |
| `idlesecs` | `idlesecs(<player\|port>)` | Alias of `idle()` |
| `host` | `host(<player\|port>)` | Client hostname (self or `CanSeeAll`; else `#-1`) |
| `hostname` | `hostname(<player\|port>)` | Alias of `host()` |
| `ipaddr` | `ipaddr(<player\|port>)` | Client IP address (self or `CanSeeAll`; else `#-1`) |
| `terminfo` | `terminfo(<player\|port>)` | Space-separated terminal type & capabilities (self or `CanSeeAll`; else `#-1`) |
| `ports` | `ports(<player>)` | Descriptor numbers for `<player>` (self or `CanSeeAll`; else `""`) |
| `lports` | `lports([<viewer>])` | All visible descriptor numbers (includes hidden if `<viewer>`/caller has `CanSeeAll`) |
| `player` | `player(<port>)` | Connected `#dbref` for integer descriptor `<port>` (`#-1` if absent) |
| `cmds` | `cmds(<player\|port>)` | Command count processed on descriptor (`-1` if absent) |
| `sent` | `sent(<player\|port>)` | Bytes sent on descriptor (`-1` if absent or lacking self/`CanSeeAll`) |
| `recv` | `recv(<player\|port>)` | Bytes received on descriptor (`-1` if absent or lacking self/`CanSeeAll`) |
| `ssl` | `ssl([<player>])` | `1` if connection uses TLS/SSL, `0` otherwise (`#-1 NOT CONNECTED` / `#-1 PERMISSION DENIED`) |
| `doing` | `doing(<obj>)` | Value of `<obj>`'s `DOING` attribute (`#-1 NO MATCH` if object missing) |
| `hidden` | `hidden(<player>)` | `1` if player has `DARK` flag, `0` otherwise (`CanSeeAll` required; else `#-1`) |
| `height` | `height(<obj>[, <fallback>])` | Positive integer `SCREENHEIGHT` attr on `<obj>` (via parents), `<fallback>`, or `24` |
| `width` | `width(<obj>[, <fallback>])` | Positive integer `SCREENWIDTH` attr on `<obj>` (via parents), `<fallback>`, or `78` |

### Capability & Telemetry Notes
* **`terminfo()`**: Starts with `TerminalType` (or `"unknown"`), followed by any enabled flags among `pueblo`, `html`, `websocket`, `telnet`, `gmcp`, `ssl`, `prompt_newlines`, `stripaccents`, `xterm256`, `rgb24`, and `ColorStyle` if distinct.
* **`ssl([<player>])`**: Defaults `<player>` to caller (`ctx.Executor`); requires self or `CanSeeAll` (`#-1 PERMISSION DENIED`), and returns `1` if `IsSsl` is true or `TerminalType` contains `"ssl"` or `"tls"`.

---

## Connected Player Rosters (`WHO` Family)

| Function | Signature | Notes |
|---|---|---|
| `lwho` | `lwho()` | Distinct connected `#dbref`s (hidden visible only with `CanSeeAll`) |
| `lwhoid` | `lwhoid()` | Distinct connected `#dbref:CreationTime` objids |
| `nwho` | `nwho()` | Count of visible connected players |
| `xwho` | `xwho(<start>, <count>)` | 1-based slice of `lwho()` (`#-1 OUT OF RANGE` if `start`/`count < 1`) |
| `xwhoid` | `xwhoid(<start>, <count>)` | 1-based slice of `lwhoid()` |
| `mwho` | `mwho()` | Mortal `lwho()`: excludes hidden players even for Wizards |
| `mwhoid` | `mwhoid()` | Mortal `lwhoid()`: excludes hidden players even for Wizards |
| `nmwho` | `nmwho()` | Mortal connected player count (excludes hidden players) |
| `xmwho` | `xmwho(<start>, <count>)` | 1-based slice of `mwho()` |
| `xmwhoid` | `xmwhoid(<start>, <count>)` | 1-based slice of `mwhoid()` |

* Note that `xwho*()` and `xmwho*()` take `<start>, <count>` as `args[0], args[1]` directly (passing < 2 or non-integer arguments returns `#-1 ARGUMENT MUST BE INTEGER`).

---

## Passwords & Connection History (`LateSubsystemFunctions.cs`)

| Function | Signature | Notes |
|---|---|---|
| `checkpass` | `checkpass(<player>, <password>)` | Verifies `<player>` password (`1`/`0`; **Wizard only**) |
| `addrlog` | `addrlog([count, ]<mode>, <pat>[, <sep>])` | Queries unique IP/host entries in `connlog.db` (`CanSeeAll`) |
| `connlog` | `connlog([count, ][<mode>[, <after>[, <before>[, <spec>[, <sep>]]]]])` | Queries session record IDs in `connlog.db` (`CanSeeAll`) |
| `connrecord` | `connrecord(<id>[, <sep>])` | Returns 9 fields for `connlog.db` record `<id>` (`CanSeeAll`) |

### History Details
- **`checkpass()`**: Returns `#-1 PERMISSION DENIED` unless caller is `Wizard`, `#-1 TOO FEW ARGUMENTS` if < 2 args, and `#-1 NO SUCH PLAYER` if `<player>` does not resolve to a `Player`.
- **`addrlog()` / `connlog()` / `connrecord()`**: Require `CanSeeAll` (`#-1 PERMISSION DENIED`) and return `""` when `connlog.db` is closed.
  - Prefixing `count` as the first argument to `addrlog()` or `connlog()` returns the integer match count instead of entries/IDs.
  - `addrlog()` joins `<Ip> <Host>` pairs with `<sep>` (default `"|"`).
  - `connlog()` defaults `<mode>` = `"all"` and `<sep>` = `" "`.
  - `connrecord(<id>[, <sep>])` joins fields with `<sep>` (default `" "`): `#<Dbref>`, `Name`, `IpAddress`, `Hostname`, `ConnectedAt`, `DisconnectedAt`, `Reason`, `Ssl` (`1`/`0`), `WebSocket` (`1`/`0`) (`#-1 NO SUCH RECORD` if missing).

```text
think [terminfo(me)]
think [xwho(1, 5)]
think [connrecord([first([connlog(all)])])]
```

---

See also: [Session & Login Commands](Commands-Session-and-Login) · [Server Configuration & Persistence](Server-Configuration-and-Persistence) · [Functions Index](Functions-Index) · [Home](Home)
