# API: Time & Scheduling

Clocks, timestamp conversion, duration formatting/arithmetic, server uptime metrics, and cron job inspection functions extracted from `PennMUSH.Core/MUSHcode/Functions/TimeFunctions.cs` (**25** functions) and `PennMUSH.Core/MUSHcode/Functions/CronFunctions.cs` (**2** functions).

> [!NOTE]
> `time()`, `convsecs()`, `convtime()`, `timefmt()`, `isdaylight()`, `tzoffset()`, `ctime()`, `starttime()`, and `restarttime()` format/evaluate in the server's **local timezone**, while `secs()`, `utctime()`, `convutcsecs()`, and `convutctime()` operate in **UTC**. Format strings for `timefmt()` and `etimefmt()` use `$`-prefixed codes rather than `%`.

---

## Current Clocks & Server Counters

| Function | Signature | Notes |
|---|---|---|
| `time` | `time()` | Current local time formatted `ddd MMM d HH:mm:ss yyyy` |
| `utctime` | `utctime()` | Current UTC time formatted `ddd MMM d HH:mm:ss yyyy` |
| `secs` | `secs()` | Current Unix epoch timestamp in UTC seconds |
| `isdaylight` | `isdaylight()` | Returns `1` if local time is in Daylight Saving Time, else `0` |
| `tzoffset` | `tzoffset()` | Current local UTC offset in seconds |
| `starttime` | `starttime()` | Local formatted timestamp when the server started |
| `restarttime` | `restarttime()` | Alias of `starttime()` (`reg.ServerStartTime`) |
| `uptime` | `uptime()` | Elapsed seconds since server start (`>= 0`) |
| `restarts` | `restarts()` | Number of server restarts recorded in `FunctionRegistry.RestartCount` |

---

## Conversions & Object Timestamps

| Function | Signature | Notes |
|---|---|---|
| `convsecs` | `convsecs([<secs>])` | Converts Unix `<secs>` (default: now) to local `ddd MMM d HH:mm:ss yyyy` |
| `convutcsecs` | `convutcsecs([<secs>])` | Converts Unix `<secs>` (default: now) to UTC `ddd MMM d HH:mm:ss yyyy` |
| `convtime` | `convtime(<timestring>)` | Parses date/time string in local context to Unix seconds, or `-1` on failure |
| `convutctime` | `convutctime(<timestring>)` | Parses UTC date/time string (`ddd MMM d HH:mm:ss yyyy` or ISO) to Unix seconds, or `-1` |
| `csecs` | `csecs(<obj\|timestring>)` | Object `CreationTime` in Unix seconds, or falls back to `convtime()` |
| `ctime` | `ctime(<obj>[, <utc?>])` | Object `CreationTime` formatted locally (or UTC if `<utc?>` is truthy); `#-1 NO MATCH` if invalid |
| `mtime` | `mtime(<obj>)` | Object `ModificationTime` in Unix seconds, or `-1` on missing/invalid object |
| `msecs` | `msecs(<obj>)` | Alias of `mtime(<obj>)` |

---

## Formatting & Duration Arithmetic

| Function | Signature | Notes |
|---|---|---|
| `timefmt` | `timefmt(<format>[, <secs>])` | Formats local time using `$` codes (`<secs>` defaults to now) |
| `etimefmt` | `etimefmt(<format>, <secs>)` | Formats unsigned elapsed `<secs>` using `$` duration codes |
| `secs2time` | `secs2time(<secs>)` | Compact duration omitting zero units (e.g. `2h 5s`, `0s` if `<= 0`) |
| `timestring` | `timestring(<secs>[, <pad>])` | Duration with optional zero-padding (`<pad>`: `0`, `1`, `>= 2`) |
| `etime` | `etime(<secs>[, <maxWidth>])` | Elapsed years/weeks/days/hours/mins/secs (`y w d h m s`), trimmed to fit `<maxWidth>` |
| `stringsecs` | `stringsecs(<duration>)` | Parses `Ny Nw Nd Nh Nm Ns` duration tokens into total seconds (`0` if empty) |
| `secscalc` | `secscalc(<base\|now>[, <±delta>...])` | Computes Unix seconds by adding/subtracting duration expressions |
| `timecalc` | `timecalc(<base\|now>[, <±delta>...])` | Same as `secscalc()`, formatted via `convsecs()` |

### `timefmt()` Escape Codes
Leaves `%` literal and replaces `$` sequences on local time:
- `$$`: literal `$`
- `$Y` / `$y`: 4-digit / 2-digit year
- `$m` / `$B` / `$b`: month `01`–`12` / full name / 3-letter abbreviation
- `$d`: day of month `01`–`31`
- `$H` / `$I`: 24-hour (`00`–`23`) / 12-hour (`01`–`12`)
- `$M` / `$S`: minute (`00`–`59`) / second (`00`–`59`)
- `$p`: AM/PM designator
- `$a` / `$A`: abbreviated / full weekday name

### `etimefmt()` Escape Codes
Uses fixed constants (`1y = 31536000s`, `1w = 604800s`, `1d = 86400s`, `1h = 3600s`, `1m = 60s`). Uppercase codes are zero-padded to 2 digits (`D2`); lowercase codes are unpadded. Requires `<secs>` to be an unsigned integer (`#-1 ARGUMENT MUST BE AN UNSIGNED INTEGER`, `#-1 TOO FEW ARGUMENTS` if < 2 args):
- `$$`: literal `$`
- `$Y` / `$y`: years
- `$W` / `$w`: weeks
- `$D` / `$d`: days
- `$H` / `$h`: hours
- `$M` / `$m`: minutes
- `$S` / `$s`: seconds

### Duration Helpers & Edge Cases
- **`timestring(<secs>[, <pad>])`**: With `<pad> == 0` (default), behaves like `secs2time()`. With `<pad> == 1`, outputs all 4 components `Xd Xh Xm Xs` (`<secs> < 0` clamped to `0`). With `<pad> >= 2`, zero-pads hours, minutes, and seconds (`Xd XXh XXm XXs`). Non-integer `<secs>` returns `#-1 ARGUMENT MUST BE INTEGER`.
- **`etime(<secs>[, <maxWidth>])`**: Negative `<secs>` returns `#-1 OUT OF RANGE`; non-integer returns `#-1 ARGUMENT MUST BE INTEGER`. When `<maxWidth> > 0`, drops smallest trailing units while length exceeds `<maxWidth>` (keeping at least the largest unit).
- **`stringsecs(<duration>)`**: Parses space-separated `<num><unit>` tokens (`y`, `w`, `d`, `h`, `m`, `s`; bare integer = seconds). Unknown unit or non-numeric token returns `#-1 ARGUMENT MUST BE INTEGER`.
- **`secscalc()` / `timecalc()`**: First arg accepts `"now"`, Unix seconds, or a date string; subsequent args accept signed duration strings (`+1d`, `-2h 30m`). Invalid inputs return `#-1` (`MushError.Nothing`).

```text
think [timefmt($Y-$m-$d $H:$M:$S)]
think [etimefmt($D days $H:$M:$S, 93784)]
-> 01 days 02:03:04

think [secscalc(now, +1w, -1d)]
```

---

## Cron Inspection (`CronFunctions.cs`)

| Function | Signature | Notes |
|---|---|---|
| `cron_list` | `cron_list([<pattern>])` | Space-separated list of job IDs matching wildcard `<pattern>` (default `*`) |
| `cron_info` | `cron_info(<id>[, <field>])` | Inspects a scheduled job; missing job returns `#-1 NO MATCH` |

> [!IMPORTANT]
> There are **no** `cron_add()` or `cron_del()` softcode functions in PennMUSH .NET. Creating, deleting, pausing, resuming, and manually triggering cron jobs is done via [`@cron`](Commands-Server-Admin-and-Extensions).

Supported `<field>` values for `cron_info(<id>[, <field>])` (default `schedule`; unrecognized fields also return `job.Expression`):
- `schedule` / `expr`: cron expression
- `command` / `cmd`: scheduled command string
- `target`: target `#dbref`
- `creator`: creator `#dbref`
- `nextrun` / `next`: next run time in Unix epoch seconds
- `lastrun` / `last`: last run time in Unix epoch seconds (or `-1` if never run)
- `paused`: `1` if paused, `0` otherwise

---

See also: [`@cron`](Commands-Server-Admin-and-Extensions) · [Functions Index](Functions-Index) · [Home](Home)
