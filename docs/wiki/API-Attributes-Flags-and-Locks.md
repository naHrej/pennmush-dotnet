# API: Attributes, Flags & Locks

Attribute reading/searching/writing, regex attribute queries, flag/power predicates, and lock inspection/evaluation/mutation extracted from `PennMUSH.Core/MUSHcode/Functions/DbFunctions.cs` (**45** functions + 2 `alias.cnf` aliases) and `Section9And18Functions.cs` (**4** side-effect functions).

* Note that `game/alias.cnf` also defines `attrcnt` -> `nattr` *(also registered in C#)*, `attrpcnt` -> `nattrp`, and `attrlock` -> `atrlock`.
* For command-based manipulation and the full schema tables, see [Attributes, Flags & Locks Commands](Commands-Attributes-Flags-and-Locks) and [Flags, Powers, Attributes & Locks](Flags-Powers-Attributes-and-Locks).

---

## Attributes: Reading, Counting & Searching

| Function | Signature | Notes |
|---|---|---|
| `get` | `get(<obj>/<attr>)` | Reads attribute (including parent inheritance) via `CanRead`; `""` if absent/unreadable |
| `xget` | `xget(<obj>, <attr>)` | Two-argument form of `get()` |
| `hasattr` | `hasattr(<obj>/<attr> \| <obj>, <attr>)` | `1` if readable direct attribute exists, else `0` |
| `hasattrval` | `hasattrval(<obj>/<attr> \| <obj>, <attr>)` | `1` if readable direct attribute exists and is non-empty |
| `hasattrp` | `hasattrp(<obj>/<attr> \| <obj>, <attr>)` | `1` if readable attribute exists on object or parent chain |
| `hasattrpval` | `hasattrpval(<obj>/<attr> \| <obj>, <attr>)` | `1` if readable direct or inherited attribute is non-empty |
| `lattr` | `lattr(<obj>[, <pattern>])` | Sorted (`Ordinal`) space-separated readable direct attribute names (default `*`) |
| `lattrp` | `lattrp(<obj>[, <pattern>])` | Same as `lattr()` including inherited parent attributes |
| `nattr` | `nattr(<obj>)` | Count of readable direct attributes (`0` if object missing) |
| `attrcnt` | `attrcnt(<obj>)` | Alias of `nattr()` |
| `nattrp` | `nattrp(<obj>)` | Count of readable direct + inherited attributes |
| `attrpcnt` | `attrpcnt(<obj>)` | `` `alias.cnf` `` Alias of `nattrp()` |
| `xattr` | `xattr(<obj>, <pat>, <start>, <count>)` | 1-based slice of `lattr(<obj>, <pat>)` |
| `xattrp` | `xattrp(<obj>, <pat>, <start>, <count>)` | 1-based slice of `lattrp(<obj>, <pat>)` |
| `grep` | `grep(<obj>, <attrPat>, <substr>)` | Case-sensitive substring match on readable direct attrs matching `<attrPat>` |
| `pgrep` | `pgrep(<obj>, <attrPat>, <substr>)` | `grep()` including inherited parent attributes |
| `grepi` | `grepi(<obj>, <attrPat>, <substr>)` | Case-insensitive `grep()` |
| `wildgrep` | `wildgrep(<obj>, <attrPat>, <wildPat>)` | Case-sensitive wildcard (`*`/`?`) match on attribute values |
| `wildgrepi` | `wildgrepi(<obj>, <attrPat>, <wildPat>)` | Case-insensitive `wildgrep()` |
| `reglattr` | `reglattr(<obj>[/<re>][, <osep>])` | Readable direct attribute names matching `.NET Regex` (`IgnoreCase`, default `.*`) |
| `reglattrp` | `reglattrp(<obj>[/<re>][, <osep>])` | Same as `reglattr()` including parent attributes |
| `regnattr` | `regnattr(<obj>[/<re>])` | Count of `reglattr()` matches |
| `regnattrp` | `regnattrp(<obj>[/<re>])` | Count of `reglattrp()` matches |
| `regxattr` | `regxattr(<obj>[/<re>], <start>, <count>[, <osep>])` | 1-based slice of `reglattr()` matches |
| `regxattrp` | `regxattrp(<obj>[/<re>], <start>, <count>[, <osep>])` | 1-based slice of `reglattrp()` matches |
| `atrlock` | `atrlock(<obj>/<attr>[, on\|off])` | Queries (`1`/`0`) or toggles (`on`/`off`) `Locked` flag on direct attr (`Controls`) |
| `attrlock` | `attrlock(<obj>/<attr>[, on\|off])` | `` `alias.cnf` `` Alias of `atrlock()` |

### Attribute Side-Effects (`Section9And18Functions.cs`)

| Function | Signature | Notes |
|---|---|---|
| `set` | `set(<obj>, <flag\|!flag\|attr:val>)` | `` `sideEffect` `` Sets/clears a flag or writes `<attr>:<val>` (`Controls`) |
| `attrib_set` | `attrib_set(<obj>/<attr>[, <val>])` | `` `sideEffect` `` Writes or clears (`""`) an attribute via `AttributeWriter.Apply` |
| `wipe` | `wipe(<obj>[/<pattern>])` | `` `sideEffect` `` Clears attributes matching `<pattern>` (default `*`, `Controls`) |

* All `reg*attr*` helpers execute `.NET Regex` with `RegexOptions.IgnoreCase` and `100 ms` timeout (`#-1 REGEX TIMEOUT` on timeout; malformed regex returns `""`/`0`).
* `xattr*` and `regxattr*` require integer `start >= 1` and `count >= 1` (`#-1 ARGUMENT MUST BE INTEGER` / `#-1 OUT OF RANGE`).

---

## Flags & Powers (`DbFunctions.cs`)

| Function | Signature | Notes |
|---|---|---|
| `hasflag` | `hasflag(<obj>, <flag\|letter>)` | `1` if `<obj>` has flag (or type letter `P`/`R`/`T`/`E`), else `0` |
| `haspower` | `haspower(<obj>, <power>)` | `1` if `<obj>` holds `<power>`, else `0` (`#-1 NO MATCH` if obj missing) |
| `flags` | `flags(<obj>)` | Compact string: type letter (`P`/`R`/`T`/`E`/`g`) + sorted single-letter flags |
| `lflags` | `lflags(<obj>)` | Sorted space-separated full flag names |
| `powers` | `powers(<obj>)` | Sorted space-separated power names |
| `andflags` | `andflags(<obj>, <letters>)` | `1` if `<obj>` satisfies all flag/type letters (`!` negates next char) |
| `orflags` | `orflags(<obj>, <letters>)` | `1` if `<obj>` satisfies any flag/type letter (`!` negates next char) |
| `andlflags` | `andlflags(<obj>, <flagList>)` | `1` if `<obj>` satisfies all space-separated flag/type names (`!flag` negates) |
| `orlflags` | `orlflags(<obj>, <flagList>)` | `1` if `<obj>` satisfies any space-separated flag/type name (`!flag` negates) |
| `andlpowers` | `andlpowers(<obj>, <powerList>)` | `1` if `<obj>` holds all listed powers (`!power` negates) |
| `orlpowers` | `orlpowers(<obj>, <powerList>)` | `1` if `<obj>` holds any listed power (`!power` negates) |

* Note `andflags`/`orflags`/`andlflags`/`orlflags`/`andlpowers`/`orlpowers` return `#-1` (`MushError.Nothing`) if `<obj>` is missing (`"0"` on < 2 args or empty pattern).

---

## Locks (`DbFunctions.cs` + `lset`)

Reading lock details enforces `CanReadLock`: allowed when `CanSeeAll(looker)` or `Controls(looker, target)` is true, denied (`#-1` / `#-1 NO SUCH LOCK`) on `Private` or `Wizard` lock flags, and otherwise allowed only if `LockFlags.Visual` or target flag `VISUAL` is set.

| Function | Signature | Notes |
|---|---|---|
| `locks` | `locks(<obj>)` | Sorted lock types configured on `<obj>` (`#-1 NO MATCH` on 0 args) |
| `llocks` | `llocks([<obj>])` | 0 args: lists all lock type names (`Basic`..`Follow`); 1 arg: same as `locks(<obj>)` |
| `lockflags` | `lockflags([<obj>[/<type>]])` | 0 args: `ovpwln`; 1 arg: active single-letter flags on lock (`CanReadLock`) |
| `llockflags` | `llockflags([<obj>[/<type>]])` | 0 args: `owner visual private wizard locked noclone`; 1 arg: long flag names |
| `lockowner` | `lockowner(<obj>[/<type>])` | Creator `#dbref` of lock (default `<type>` = `Basic`, `CanReadLock`) |
| `lock` | `lock(<obj>[/<type>][, <newKey>])` | Reads key text (`CanReadLock`), or sets/clears lock key (`Controls` -> `#-1 PERMISSION DENIED`) |
| `elock` | `elock(<obj>[/<type>], <victim>)` | Evaluates `<obj>`'s lock (`Basic` default) against `<victim>` (`1`/`0`; checks `CanReadLock`) |
| `testlock` | `testlock(<boolexp>, <victim>)` | Parses and evaluates a raw lock boolean expression directly against `<victim>` |
| `lockfilter` | `lockfilter(<boolexp>, <list>[, <delim>])` | Filters `<delim>`-separated `#dbref` list returning elements passing `<boolexp>` |
| `lset` | `lset(<obj>[/<type>], <flags>)` | `` `sideEffect` `` Sets/clears lock flags (`visual`, `private`, `wizard`, `locked`, `noclone`, `owner`, `!` prefix) |

* Note `lock(<obj>, <newKey>)` mutates the lock when given 2 arguments even though it is not tagged `sideEffect` at registration; it enforces `Controls(looker, obj)`.
* `lset()` requires `<obj>/<type>` lock to exist (`#-1 NO SUCH LOCK`) and returns `#-1` (`MushError.Nothing`) on unrecognized flag names.

```text
think [get(me/DESCRIPTION)]
think [andlflags(me, PLAYER !GAGGED)]
think [testlock(=me|TYPE^ROOM, here)]
think [null([set(me, VA:Hello)] [lset(me/Basic, visual)])]
```

---

See also: [Attributes, Flags & Locks Commands](Commands-Attributes-Flags-and-Locks) · [Flags, Powers, Attributes & Locks](Flags-Powers-Attributes-and-Locks) · [Objects & Database](API-Objects-and-Database) · [Functions Index](Functions-Index) · [Home](Home)
