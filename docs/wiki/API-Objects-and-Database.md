# API: Objects & Database

Object naming, ownership, permissions, type validators, world-graph walkers, multi-parent/multi-zone navigation, database search, quota/followers, softcode evaluation (`u()`, `objeval()`), and building side-effect functions extracted from `PennMUSH.Core/MUSHcode/Functions/DbFunctions.cs` (**94** functions + 8 `alias.cnf` aliases), `Section9And18Functions.cs` (**7** functions), `LateSubsystemFunctions.cs` (**3** functions), and `ConnectionAndZoneFunctions.cs` (**2** functions: `zwho()`, `zmwho()`).

* `game/alias.cnf` defines `search` -> `lsearch`, `stats` -> `lstats`, and `*objects` aliases for the 6 `*things` functions (`lobjects`, `lvobjects`, `nobjects`, `nvobjects`, `xobjects`, `xvobjects`).
* `game/restrict.cnf` restricts `lstats()` with **`restrict_function lstats noguest`**.
* See [Building & World Commands](Commands-Building-and-World) and [Architecture & Concepts](Architecture-and-Concepts) for DB/DAG architecture.

---

## Identity, Names, Types & Validation

| Function | Signature | Notes |
|---|---|---|
| `name` | `name(<obj>)` | Object's `Name` (`me`/`here`/`#dbref`; `#-1 NO MATCH` if absent) |
| `fullname` | `fullname(<obj>)` | Returns object's full `Name` (`DbFunctions.FullNameFunc`) |
| `nameformat` | `nameformat(<obj>)` | Evaluates `<obj>`'s `NAMEFORMAT` attr if present, else `name(<obj>)` |
| `namelist` | `namelist(<players>[, <[obj/]errAttr>])` | Maps space-separated player tokens (`*name`, `me`) to `#dbref`s or `#-1` / callback |
| `namegrab` | `namegrab(<dbrefList>, <target>[, <delim>])` | Finds first matching object (by `#dbref`, exact name, or word prefix / exit `;` alias) |
| `namegraball` | `namegraball(<dbrefList>, <target>[, <delim>])` | Returns all matching elements in `<dbrefList>` |
| `moniker` | `moniker(<obj>)` | `<obj>`'s `MONIKER` attr if non-empty, else `obj.Name` |
| `cname` | `cname(<obj>)` | Alias of `moniker()` |
| `alias` | `alias(<obj>[, <newAlias>])` | Reads first exit alias (or `ALIAS` attr), or sets `ALIAS` attr (`Controls`) |
| `fullalias` | `fullalias(<obj>)` | All `;`-delimited exit aliases after the first `;`, or `ALIAS` attr |
| `num` | `num(<ref>)` | Resolves `me`/`here`/`#n` to `#n`, or `#-1 NO MATCH` |
| `rnum` | `rnum(<ref>)` | Numeric ID integer (`n`), or `-1` |
| `objid` | `objid(<obj>)` | Returns `#dbref:CreationTimeUnixSeconds` (`#-1 NO MATCH` if missing) |
| `owner` | `owner(<obj>)` | Owner `#dbref` |
| `type` | `type(<obj>)` | Uppercase type: `PLAYER`, `ROOM`, `THING`, `EXIT`, or `GARBAGE` |
| `hastype` | `hastype(<obj>, <type>)` | `1` if `<obj>` has `<type>` (`OrdinalIgnoreCase`), else `0` |
| `money` | `money(<obj>)` | Object's `Pennies` balance (`#-1 NO MATCH` if missing) |
| `objmem` | `objmem(<obj>)` | Estimated memory size in bytes (`64 + Name.Length + sum(32 + attrName + attrVal)`) |
| `playermem` | `playermem(<player>)` | Total `objmem()` across non-garbage objects owned by `<player>` |
| `lstats` | `lstats([<player\|all>])` | `noguest` (`restrict.cnf`) Counts `<total> <rooms> <exits> <things> <players> [<garbage>]` |
| `stats` | `stats([<player\|all>])` | `` `alias.cnf` `` Alias of `lstats()` |
| `valid` | `valid(<category>, <value>)` | Validates `name`, `attrname`, `playername`, `flag`, `power`, `function`, `locktype`, `passwd`, `qreg`, `colorname`, `ansicolor` |
| `isdbref` | `isdbref(<str>)` | `1` if valid `#n` referencing non-garbage object, else `0` |
| `isobjid` | `isobjid(<str>)` | `1` if `#n:ts` matches existing non-garbage object's `CreationTime`, else `0` |
| `isint` | `isint(<str>)` | `1` if parses as `long` integer (`InvariantCulture`), else `0` |
| `isnum` | `isnum(<str>)` | `1` if parses as finite `double` (`InvariantCulture`), else `0` |
| `isword` | `isword(<str>)` | `1` if non-empty and every character satisfies `char.IsLetter` |

---

## Permissions, Visibility, Quota & Following

| Function | Signature | Notes |
|---|---|---|
| `controls` | `controls(<actor>, <target>)` | `1` if `actor` is `Wizard`/`God` or shares owner with `target`, else `0` |
| `nearby` | `nearby(<o1>, <o2>)` | `1` if same object, one holds the other, or both share a valid `Location` |
| `visible` | `visible(<looker>, <target>)` | `1` if `looker` is `Wizard`/`God` or `target` is not `DARK`, else `0` |
| `findable` | `findable(<looker>, <victim>)` | `1` unless `victim` or `victim.Location` is `UNFINDABLE` (override: `CanSeeAll`/`Controls`) |
| `quota` | `quota([<player>])` | *(`LateSubsystemFunctions.cs`)* Quota limit (`99999` if disabled/unlimited; `QuotaRules.CanSee`) |
| `followers` | `followers(<obj>)` | *(`LateSubsystemFunctions.cs`)* Value of `FOLLOWERS` attribute (`Controls` -> `#-1 PERMISSION DENIED`) |
| `following` | `following(<obj>)` | *(`LateSubsystemFunctions.cs`)* Value of `FOLLOWING` attribute (`Controls` -> `#-1 PERMISSION DENIED`) |

---

## Topology, Parents, Zones & Locating

| Function | Signature | Notes |
|---|---|---|
| `loc` | `loc(<obj>)` | Immediate `Location` `#dbref` (`CanLocate` -> `#-1` if denied) |
| `rloc` | `rloc(<obj>[, <levels>])` | Walks `<levels>` (default `1`) up the location chain |
| `where` | `where(<obj>)` | Immediate `Location`, or exit destination (`Home` / source room for unlinked exits) |
| `room` | `room(<obj>)` | Outermost enclosing `ObjectType.Room` `#dbref` (cycle-safe) |
| `home` | `home(<obj>)` | Object's `Home` `#dbref` (defaults to `#0` if invalid) |
| `con` | `con(<obj>)` | Head `#dbref` of `<obj>`'s contents linked list |
| `exit` | `exit(<obj>)` | Head `#dbref` of `<obj>`'s exits linked list (`#-1` if none) |
| `next` | `next(<obj>)` | Next `#dbref` in contents/exits linked list (`#-1` at end) |
| `nextdbref` | `nextdbref()` | Next dbref ID to be allocated (`db.Top`) |
| `exits` | `exits(<obj>)` | Space-separated list of exit `#dbref`s on `<obj>` |
| `lexits` | `lexits(<obj>)` | Alias of `exits()` |
| `parent` | `parent(<obj>)` | Primary parent `#dbref` |
| `lparent` | `lparent(<obj>)` | Full ordered `Parents` `#dbref` list (multi-parent DAG) |
| `children` | `children(<parent>)` | Sorted `#dbref` list of objects whose `Parents` contain `<parent>` |
| `nchildren` | `nchildren(<parent>)` | Count of `children(<parent>)` |
| `zone` | `zone(<obj>)` | Primary zone `#dbref` |
| `lzone` | `lzone(<obj>)` | Full ordered `Zones` `#dbref` list (multi-zone DAG) |
| `inzone` | `inzone(<obj>, <zone>)` | `1` if `<obj>.InZone(<zone>)`, else `0` |
| `zwho` | `zwho(<zone>)` | *(`ConnectionAndZoneFunctions.cs`)* Online `#dbref`s in `<zone>` (checks `Zone` lock unless `CanSeeAll`) |
| `zmwho` | `zmwho(<zone>)` | *(`ConnectionAndZoneFunctions.cs`)* Mortal `zwho()`: omits hidden players, skips `Zone` lock |
| `entrances` | `entrances([<target>[, <types>[, <lo>[, <hi>]]]])` | Sorted objects (`E`/`P`/`R`/`T`/`A`, default `A`) linked/homed to `<target>` in `#lo..#hi` |
| `locate` | `locate(<looker>, <what>, <flags>)` | Resolves object relative to `<looker>` (`*amhinecpxXPRTEN` flags -> `#dbref`, `#-1`, `#-2`) |
| `pmatch` | `pmatch(<spec>)` | Matches online/offline player by `#dbref`, `me`, `*name`, exact or unique prefix (`#-1`/`#-2`) |

### Contents & Exits Walkers (`L*` / `N*` / `LV*` / `NV*` / `X*` / `XV*`)

Visibility variants (`*V*`) filter out `DARK` objects unless caller satisfies `CanSeeAll` or `Controls`. Slice variants (`X*` / `XV*`) take `(<loc>, <start>, <count>)` (`start`/`count >= 1`, else `#-1 OUT OF RANGE` / `#-1 ARGUMENT MUST BE INTEGER`). All walkers enforce `CanExamine(looker, loc) || looker.Location == loc.Id || ctx.Enactor == loc.Id`.

| Function | Signature | Notes |
|---|---|---|
| `lcon` | `lcon(<obj>[, <mode>])` | All contents (or `<mode>`: `player`, `object`/`thing`, `connect`, `puppet`, `listen`) |
| `lplayers` | `lplayers(<obj>)` | Players inside `<obj>` |
| `lthings` | `lthings(<obj>)` | Things inside `<obj>` |
| `lobjects` | `lobjects(<obj>)` | `` `alias.cnf` `` Alias of `lthings()` |
| `ncon` | `ncon(<obj>)` | Count of Players + Things in `<obj>` |
| `nexits` | `nexits(<obj>)` | Count of exits on `<obj>` |
| `nplayers` | `nplayers(<obj>)` | Count of players in `<obj>` |
| `nthings` | `nthings(<obj>)` | Count of things in `<obj>` |
| `nobjects` | `nobjects(<obj>)` | `` `alias.cnf` `` Alias of `nthings()` |
| `lvcon` | `lvcon(<obj>)` | Visible Players + Things in `<obj>` |
| `lvexits` | `lvexits(<obj>)` | Visible exits on `<obj>` |
| `lvplayers` | `lvplayers(<obj>)` | Visible players in `<obj>` |
| `lvthings` | `lvthings(<obj>)` | Visible things in `<obj>` |
| `lvobjects` | `lvobjects(<obj>)` | `` `alias.cnf` `` Alias of `lvthings()` |
| `nvcon` | `nvcon(<obj>)` | Count of visible Players + Things in `<obj>` |
| `nvexits` | `nvexits(<obj>)` | Count of visible exits on `<obj>` |
| `nvplayers` | `nvplayers(<obj>)` | Count of visible players in `<obj>` |
| `nvthings` | `nvthings(<obj>)` | Count of visible things in `<obj>` |
| `nvobjects` | `nvobjects(<obj>)` | `` `alias.cnf` `` Alias of `nvthings()` |
| `xcon` | `xcon(<obj>, <start>, <count>)` | 1-based slice of `lcon(<obj>)` |
| `xexits` | `xexits(<obj>, <start>, <count>)` | 1-based slice of `exits(<obj>)` |
| `xplayers` | `xplayers(<obj>, <start>, <count>)` | 1-based slice of `lplayers(<obj>)` |
| `xthings` | `xthings(<obj>, <start>, <count>)` | 1-based slice of `lthings(<obj>)` |
| `xobjects` | `xobjects(<obj>, <start>, <count>)` | `` `alias.cnf` `` Alias of `xthings()` |
| `xvcon` | `xvcon(<obj>, <start>, <count>)` | 1-based slice of `lvcon(<obj>)` |
| `xvexits` | `xvexits(<obj>, <start>, <count>)` | 1-based slice of `lvexits(<obj>)` |
| `xvplayers` | `xvplayers(<obj>, <start>, <count>)` | 1-based slice of `lvplayers(<obj>)` |
| `xvthings` | `xvthings(<obj>, <start>, <count>)` | 1-based slice of `lvthings(<obj>)` |
| `xvobjects` | `xvobjects(<obj>, <start>, <count>)` | `` `alias.cnf` `` Alias of `xvthings()` |

---

## Database Search & Command Scan

| Function | Signature | Notes |
|---|---|---|
| `lsearch` | `lsearch(<who>[, <class>, <val>...])` | Queries DB objects (`all`/`me`/player, `mindb`/`maxdb`/`start`/`count`, `eval`/`e*`/`elock`, `type`, `name`, ...) |
| `search` | `search(<who>[, <class>, <val>...])` | `` `alias.cnf` `` Alias of `lsearch()` |
| `lsearchr` | `lsearchr(<who>[, <class>, <val>...])` | Reversed dbref order (`lsearch()` from highest dbref down) |
| `nlsearch` | `nlsearch(<who>[, <class>, <val>...])` | Count of matches from `lsearch()` |
| `nsearch` | `nsearch(<who>[, <class>, <val>...])` | Alias of `nlsearch()` |
| `scan` | `scan([<looker>, ]<commandStr>)` | *(`Section9And18Functions.cs`)* Lists `<#dbref>/<ATTR>` `$command:` matches around `<looker>` |

---

## Softcode Invocation & Evaluation (`DbFunctions.cs`)

`U`, `UFUN`, `ULOCAL`, and `ULAMBDA` all support `#lambda/<expr>` anonymous inline evaluation in addition to `<[obj/]attr>` references, enforcing cycle detection (`#-1 RECURSION CYCLE DETECTED`) and `GetReadableAttribute`.

| Function | Signature | Notes |
|---|---|---|
| `u` | `u(<obj/attr \| #lambda/expr>[, <args...>])` | Evaluates attribute in `<obj>`'s context with `%0..` bound to `<args...>` |
| `ufun` | `ufun(<obj/attr \| #lambda/expr>[, <args...>])` | Alias of `u()` |
| `ulocal` | `ulocal(<obj/attr \| #lambda/expr>[, <args...>])` | Like `u()`, saving and restoring caller Q-registers around execution |
| `ulambda` | `ulambda(<#lambda/expr \| obj/attr>[, <args...>])` | Prioritizes `#lambda/<body>` dispatch, otherwise delegates to `u()` |
| `udefault` | `udefault(<[obj/]attr>, <default>[, <args...>])` | `` `noParse` `` Calls `u()` if readable attr exists; otherwise evaluates `<default>` |
| `uldefault` | `uldefault(<[obj/]attr>, <default>[, <args...>])` | `` `noParse` `` Like `udefault()`, preserving Q-registers across the `u()` call |
| `default` | `default(<[obj/]attr1>[, ...], <default>)` | `` `noParse` `` Returns raw value of first readable attribute found, or evaluates `<default>` |
| `edefault` | `edefault(<[obj/]attr>, <default>)` | `` `noParse` `` Evaluates readable attribute (empty `%0..`), or evaluates `<default>` |
| `eval` | `eval(<obj>, <attr>)` | Evaluates readable `<obj>/<attr>` in `<obj>`'s context (`#-1 NO MATCH` if missing) |
| `get_eval` | `get_eval(<obj>/<attr>)` | Evaluates readable `<obj>/<attr>` (`""` if missing) |
| `objeval` | `objeval(<obj>, <expr>)` | `` `noParse` `` Evaluates `<expr>` with `Executor` = `<obj>` (if `Controls`), keeping caller `%0..` |
| `v` | `v(<attr \| regChar \| argIdx>)` | Retrieves `%#`, `%@`, `%!`, `%l`, `%n`, `%0..`, or direct attr on caller (`Executor`) |
| `s` | `s(<expr>)` | Evaluates `<expr>` in current context |
| `zfun` | `zfun(<attr>[, <args...>])` | Invokes `u(<zone(me)>/<attr>[, <args...>])` |
| `pfun` | `pfun(<attr>[, <args...>])` | Invokes `u(<parent(me)>/<attr>[, <args...>])` |

---

## World-Building Side-Effects (`DbFunctions.cs` & `Section9And18Functions.cs`)

| Function | Signature | Notes |
|---|---|---|
| `pcreate` | `pcreate(<name>, <pass>[, <dbref>])` | Creates a `Player` via `PlayerCreator.Create`; returns `#dbref` or `#-1` *(note: not tagged `sideEffect`)* |
| `create` | `create(<name>)` | `` `sideEffect` `` Creates Thing (pays `1` quota), moves to caller, returns `#dbref` |
| `open` | `open(<name>[, <dest>[, <srcRoom>]])` | `` `sideEffect` `` Opens Exit (pays `1` quota), attaches & links destination, returns `#dbref` |
| `link` | `link(<obj>, <dest>)` | `` `sideEffect` `` Links Exit destination or sets `Home` on non-exit (`Controls`); returns `""` |
| `tel` | `tel(<victim>, <dest>)` | `` `sideEffect` `` Teleports non-room `<victim>` into `<dest>` (`Controls`/`Tel_*`/`JUMP_OK`); returns `""` |
| `dig` | `dig(<name>[, <exitTo>[, <exitBack>]])` | `` `sideEffect` `` Digs Room (+ optional return/entry exits, `1..3` quota), returns `#dbref` |
| `clone` | `clone(<obj>[, <newName>])` | `` `sideEffect` `` Clones non-Player `<obj>` (`CanExamine`, copies flags & attrs without `NoCopy`) |

```text
think [u(me/FORMAT_NAME, Alice)]
think [lsearch(me, type, room, mindb, 0)]
think [lstats(all)]
```

---

See also: [Building & World Commands](Commands-Building-and-World) · [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) · [Architecture & Concepts](Architecture-and-Concepts) · [Functions Index](Functions-Index) · [Home](Home)
