# Functions Index

Complete A–Z MUSHcode function reference for **PennMUSH .NET**, extracted directly from all **18** C# modules under `PennMUSH.Core/MUSHcode/Functions/`, `game/alias.cnf`, and `game/restrict.cnf`.

* **567** distinct built-in C# function registrations (**40** `noParse`, **29** `sideEffect`)
* **18** default `function_alias` rules in `game/alias.cnf` (**11** alias-only names + **7** already registered in C#) = **578** default callable function names
* **1** default config restriction in `game/restrict.cnf` (`restrict_function lstats noguest`)

---

## Domain Reference Pages

| Domain | Scope | Count | Page |
|---|---|---|---|
| **String & Formatting** | String indexing, ANSI padding/align, editing, grammar, phonetics, colors, Pueblo HTML | 74 | [String & Formatting](API-String-and-Formatting) |
| **Lists & Iteration** | 1-based word lists, sorting, set ops, `iter()`/`parse()`, combinators, random sampling | 53 | [Lists & Iteration](API-Lists-and-Iteration) |
| **Logic, Control & Registers** | Booleans, `cand`/`cor`, `if`/`cond`, `switch`/`reswitch`, Q-registers, `fn()` dispatch | 50 | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| **Math & Numbers** | Arithmetic, roots/logs, trig angles, 64-bit bitwise, `lmath()`, `baseconv()`, vectors | 67 | [Math & Numbers](API-Math-and-Numbers) |
| **Regex & Wildcards** | `.NET Regex` matching (`100 ms` timeout), `regedit*()`/`regsub()`, list/attr regex helpers | 18 | [Regex & Wildcards](API-Regex-and-Wildcards) |
| **Time & Scheduling** | Local/UTC clocks, `$`-code `timefmt()`/`etimefmt()`, durations, `cron_list()`/`cron_info()` | 27 | [Time & Scheduling](API-Time-and-Scheduling) |
| **JSON & Crypto** | `json*()` builder/query/mod/map, Base64, URL/form decode, `digest()`, `hmac()`, ciphers | 15 | [JSON & Crypto](API-JSON-and-Crypto) |
| **Objects & Database** | Names, types, topology, multi-parent/zone DAGs, `L*`/`N*`/`V*`/`X*` walkers, `lsearch()`, `u*()`, building | 106 + 8 `alias.cnf` | [Objects & Database](API-Objects-and-Database) |
| **Attributes, Flags & Locks** | Attribute read/search/write, `reg*attr*()`, flags, powers, lock inspection & evaluation | 49 + 2 `alias.cnf` | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| **Connections & ConnLog** | Descriptors, `WHO` rosters, terminal capabilities, `ssl()`, `checkpass()`, `connlog.db` queries | 32 | [Connections & ConnLog](API-Connections-and-ConnLog) |
| **Channels & Mail** | Channel buffers/flags/locks/emits (`cemit`/`nscemit`) and `@mail`/`@malias` inspection & send | 29 | [Channels & Mail](API-Channels-and-Mail) |
| **Communication & Emits** | Side-effect emits (`emit`, `pemit`, `remit`, `oemit`, `zemit`, `ns*`), `speak()`, `beep()` | 17 | [Communication & Emits](API-Communication-and-Emits) |
| **SQL, HTTP & System** | SQL queries/escaping, `config()`, PID queue inspection, `ITextFileCache`, `suggest()`, `benchmark()` | 19 + 1 `alias.cnf` | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| **Async & Background Tasks** | `async()` worker threads, `parallel_map()`, direct `bgtask()` queue submission | 3 | [Async & Background Tasks](API-Async-and-Background-Tasks) |
| **Client & MUjs UI** | WebSocket/MUjs UI sessions (`cedit`, `cwindow`, `cstyle`, `caudio`, `client_emit`), `oob()`, `wshtml()`, `wsjson()` | 8 | [Client & MUjs UI](API-Client-and-MUjs-UI) |

See [MUSHcode Language Guide](MUSHcode-Language-Guide) for evaluation order, bracket/brace escaping, `noParse` semantics, and `MushError` return codes, and [Server Configuration & Persistence](Server-Configuration-and-Persistence) for `alias.cnf` / `restrict.cnf` configuration.

---

## Complete A–Z Function Index

### Symbols

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `@@` | `@@([<comment...>])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |

### A

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `abs` | `abs(<num>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `accent` | `accent(<text>, <template>)` | — | [String & Formatting](API-String-and-Formatting) |
| `accname` | `accname(<obj>)` | — | [String & Formatting](API-String-and-Formatting) |
| `acos` | `acos(<val>[, <unit>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `add` | `add(<n1>[, <n2>...])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `addrlog` | `addrlog([count, ]<mode>, <pat>[, <sep>])` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `after` | `after(<str>[, <search>])` | — | [String & Formatting](API-String-and-Formatting) |
| `alias` | `alias(<obj>[, <newAlias>])` | — | [Objects & Database](API-Objects-and-Database) |
| `align` | `align(<spec>, <c1>, ...[, <fill>[, <colSep>[, <rowSep>]]])` | — | [String & Formatting](API-String-and-Formatting) |
| `allof` | `allof(<expr1>[, <expr2>...], <sep>)` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `alphamax` | `alphamax(<s1>[, <s2>...])` | — | [String & Formatting](API-String-and-Formatting) |
| `alphamin` | `alphamin(<s1>[, <s2>...])` | — | [String & Formatting](API-String-and-Formatting) |
| `and` | `and(<b1>[, <b2>...])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `andflags` | `andflags(<obj>, <letters>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `andlflags` | `andlflags(<obj>, <flagList>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `andlpowers` | `andlpowers(<obj>, <powerList>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `ansi` | `ansi(<spec>, <text>)` | — | [String & Formatting](API-String-and-Formatting) |
| `aposs` | `aposs(<obj>)` | — | [String & Formatting](API-String-and-Formatting) |
| `art` | `art(<word>)` | — | [String & Formatting](API-String-and-Formatting) |
| `asin` | `asin(<val>[, <unit>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `async` | `async(<code>[, <callbackCmd>])` | — | [Async & Background Tasks](API-Async-and-Background-Tasks) |
| `atan` | `atan(<val>[, <unit>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `atan2` | `atan2(<y>, <x>[, <unit>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `atrlock` | `atrlock(<obj>/<attr>[, on\|off])` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `attrcnt` | `attrcnt(<obj>)` | also `alias.cnf` | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `attrib_set` | `attrib_set(<obj>/<attr>[, <val>])` | `sideEffect` | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `attrlock` | `attrlock(<obj>/<attr>[, on\|off])` | `alias.cnf` -> `atrlock()` | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `attrpcnt` | `attrpcnt(<obj>)` | `alias.cnf` -> `nattrp()` | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `avg` | `avg(<list1>[, <list2>...])` | — | [Math & Numbers](API-Math-and-Numbers) |

### B

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `band` | `band(<u1>, <u2>[, <u3>...])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `baseconv` | `baseconv(<num>, <from>, <to>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `beep` | `beep([<count>])` | — | [Communication & Emits](API-Communication-and-Emits) |
| `before` | `before(<str>[, <search>])` | — | [String & Formatting](API-String-and-Formatting) |
| `benchmark` | `benchmark(<expr>, <count>[, <target>])` | `noParse` | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `bgtask` | `bgtask(<command>)` | — | [Async & Background Tasks](API-Async-and-Background-Tasks) |
| `bnand` | `bnand(<u1>, <u2>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `bnot` | `bnot(<u>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `bor` | `bor(<u1>, <u2>[, <u3>...])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `bound` | `bound(<val>, <min>[, <max>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `brackets` | `brackets(<str>)` | — | [String & Formatting](API-String-and-Formatting) |
| `bxor` | `bxor(<u1>, <u2>[, <u3>...])` | — | [Math & Numbers](API-Math-and-Numbers) |

### C

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `cand` | `cand([<expr1>...])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `capstr` | `capstr(<str>)` | — | [String & Formatting](API-String-and-Formatting) |
| `case` | `case(<target>, <str1>, <act1>[, ...][, <def>])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `caseall` | `caseall(<target>, <str1>, <act1>[, ...][, <def>])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `cat` | `cat([<s1>...])` | — | [String & Formatting](API-String-and-Formatting) |
| `caudio` | `caudio(<playerDbref>, <url>[, <vol>[, <loop?>]])` | — | [Client & MUjs UI](API-Client-and-MUjs-UI) |
| `cbuffer` | `cbuffer(<channel>)` | — | [Channels & Mail](API-Channels-and-Mail) |
| `cbufferadd` | `cbufferadd(<channel>, <text>)` | `sideEffect` | [Channels & Mail](API-Channels-and-Mail) |
| `cdesc` | `cdesc(<channel>)` | — | [Channels & Mail](API-Channels-and-Mail) |
| `cedit` | `cedit(<playerDbref>, <targetDbref>, <attr>)` | — | [Client & MUjs UI](API-Client-and-MUjs-UI) |
| `ceil` | `ceil(<num>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `cemit` | `cemit(<channel>, <message>)` | `sideEffect` | [Channels & Mail](API-Channels-and-Mail) |
| `center` | `center(<str>, <width>[, <lfill>[, <rfill>]])` | — | [String & Formatting](API-String-and-Formatting) |
| `cflags` | `cflags(<channel>[, <player>])` | — | [Channels & Mail](API-Channels-and-Mail) |
| `channels` | `channels([<player>][, <sep>])` | — | [Channels & Mail](API-Channels-and-Mail) |
| `checkpass` | `checkpass(<player>, <password>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `children` | `children(<parent>)` | — | [Objects & Database](API-Objects-and-Database) |
| `chr` | `chr(<code>)` | — | [String & Formatting](API-String-and-Formatting) |
| `clflags` | `clflags(<channel>[, <player>])` | — | [Channels & Mail](API-Channels-and-Mail) |
| `client_emit` | `client_emit(<playerDbref>, <type>, <json>)` | — | [Client & MUjs UI](API-Client-and-MUjs-UI) |
| `clock` | `clock(<channel>[/<type>][, <key>])` | — | [Channels & Mail](API-Channels-and-Mail) |
| `clone` | `clone(<obj>[, <newName>])` | `sideEffect` | [Objects & Database](API-Objects-and-Database) |
| `cmds` | `cmds(<player\|port>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `cmogrifier` | `cmogrifier(<channel>)` | — | [Channels & Mail](API-Channels-and-Mail) |
| `cmsgs` | `cmsgs(<channel>)` | — | [Channels & Mail](API-Channels-and-Mail) |
| `cname` | `cname(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `colors` | `colors([<pattern\|spec>[, <formats>]])` | — | [String & Formatting](API-String-and-Formatting) |
| `columns` | `columns(<list>, <numCols>[, <width>[, <delim>]])` | — | [String & Formatting](API-String-and-Formatting) |
| `comp` | `comp(<a, b>[, <type>])` | — | [String & Formatting](API-String-and-Formatting) |
| `con` | `con(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `cond` | `cond(<c1>, <e1>[, <c2>, <e2>...][, <def>])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `condall` | `condall(<c1>, <e1>[, <c2>, <e2>...][, <def>])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `config` | `config([<option>])` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `conn` | `conn(<player\|port>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `connlog` | `connlog([count, ][<mode>[, <after>[, <before>[, <spec>[, <sep>]]]]])` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `connrecord` | `connrecord(<id>[, <sep>])` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `controls` | `controls(<actor>, <target>)` | — | [Objects & Database](API-Objects-and-Database) |
| `convsecs` | `convsecs([<secs>])` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `convtime` | `convtime(<timestring>)` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `convutcsecs` | `convutcsecs([<secs>])` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `convutctime` | `convutctime(<timestring>)` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `cor` | `cor([<expr1>...])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `cos` | `cos(<angle>[, <unit>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `cowner` | `cowner(<channel>)` | — | [Channels & Mail](API-Channels-and-Mail) |
| `create` | `create(<name>)` | `sideEffect` | [Objects & Database](API-Objects-and-Database) |
| `crecall` | `crecall(<channel>[, <n>[, <start>[, <delim>[, <stamp?>]]]])` | — | [Channels & Mail](API-Channels-and-Mail) |
| `cron_info` | `cron_info(<id>[, <field>])` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `cron_list` | `cron_list([<pattern>])` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `csecs` | `csecs(<obj\|timestring>)` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `cstatus` | `cstatus(<channel>, <player>)` | — | [Channels & Mail](API-Channels-and-Mail) |
| `cstyle` | `cstyle(<playerDbref>, <url>)` | — | [Client & MUjs UI](API-Client-and-MUjs-UI) |
| `ctime` | `ctime(<obj>[, <utc?>])` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `ctitle` | `ctitle(<channel>, <player>)` | — | [Channels & Mail](API-Channels-and-Mail) |
| `ctu` | `ctu(<angle>, <from>, <to>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `cusers` | `cusers(<channel>)` | — | [Channels & Mail](API-Channels-and-Mail) |
| `cwho` | `cwho(<channel>[, <status>[, <skipGagged?>]])` | — | [Channels & Mail](API-Channels-and-Mail) |
| `cwindow` | `cwindow(<playerDbref>, <id>, <target>, <action>, <html>[, <cssUrl>])` | — | [Client & MUjs UI](API-Client-and-MUjs-UI) |

### D

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `dec` | `dec(<int\|prefixInt>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `decode64` | `decode64(<base64>)` | — | [JSON & Crypto](API-JSON-and-Crypto) |
| `decompose` | `decompose(<str>)` | — | [String & Formatting](API-String-and-Formatting) |
| `decrypt` | `decrypt(<text>, <key>[, <base64?>])` | — | [JSON & Crypto](API-JSON-and-Crypto) |
| `default` | `default(<[obj/]attr1>[, ...], <default>)` | `noParse` | [Objects & Database](API-Objects-and-Database) |
| `delete` | `delete(<str>, <pos>, <len>)` | — | [String & Formatting](API-String-and-Formatting) |
| `die` | `die(<n>, <sides>[, <showAll?>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `dig` | `dig(<name>[, <exitTo>[, <exitBack>]])` | `sideEffect` | [Objects & Database](API-Objects-and-Database) |
| `digest` | `digest(<algo>, <string>)` / `digest(list)` | — | [JSON & Crypto](API-JSON-and-Crypto) |
| `dist2d` | `dist2d(<x1>, <y1>, <x2>, <y2>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `dist3d` | `dist3d(<x1>, <y1>, <z1>, <x2>, <y2>, <z2>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `div` | `div(<i1>, <i2>[, <i3>...])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `doing` | `doing(<obj>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `dynhelp` | `dynhelp(<file>, <topic>)` | `alias.cnf` -> `textfile()` | [SQL, HTTP & System](API-SQL-HTTP-and-System) |

### E

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `e` | `e([<x>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `edefault` | `edefault(<[obj/]attr>, <default>)` | `noParse` | [Objects & Database](API-Objects-and-Database) |
| `edit` | `edit(<str>, <s1>, <r1>[, <s2>, <r2>...])` | — | [String & Formatting](API-String-and-Formatting) |
| `element` | `element(<list>, <pattern>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `elements` | `elements(<list>, <posList>[, <delim>[, <osep>]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `elist` | `elist(<list>[, <conj>[, <delim>[, <osep>[, <punc>]]]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `elock` | `elock(<obj>[/<type>], <victim>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `emit` | `emit(<message>)` | `sideEffect` | [Communication & Emits](API-Communication-and-Emits) |
| `encode64` | `encode64(<string>)` | — | [JSON & Crypto](API-JSON-and-Crypto) |
| `encrypt` | `encrypt(<text>, <key>[, <base64?>])` | — | [JSON & Crypto](API-JSON-and-Crypto) |
| `endtag` | `endtag(<name>)` | — | [String & Formatting](API-String-and-Formatting) |
| `entrances` | `entrances([<target>[, <types>[, <lo>[, <hi>]]]])` | — | [Objects & Database](API-Objects-and-Database) |
| `eq` | `eq(<n1>, <n2>[, <n3>...])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `escape` | `escape(<str>)` | — | [String & Formatting](API-String-and-Formatting) |
| `etime` | `etime(<secs>[, <maxWidth>])` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `etimefmt` | `etimefmt(<format>, <secs>)` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `eval` | `eval(<obj>, <attr>)` | — | [Objects & Database](API-Objects-and-Database) |
| `exit` | `exit(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `exits` | `exits(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `exp` | `exp([<x>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `extract` | `extract(<list>, <first>, <len>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |

### F

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `fdiv` | `fdiv(<n1>, <n2>[, <n3>...])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `filter` | `filter(<[obj/]attr>, <list>[, <delim>[, <osep>[, <args...>]]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `filterbool` | `filterbool(<[obj/]attr>, <list>[, <delim>[, <osep>[, <args...>]]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `findable` | `findable(<looker>, <victim>)` | — | [Objects & Database](API-Objects-and-Database) |
| `first` | `first(<list>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `firstof` | `firstof(<expr1>[, <expr2>...])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `flags` | `flags(<obj>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `flip` | `flip(<str>)` | — | [String & Formatting](API-String-and-Formatting) |
| `floor` | `floor(<num>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `floordiv` | `floordiv(<i1>, <i2>[, <i3>...])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `fmod` | `fmod(<n1>, <n2>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `fn` | `fn(<[obj/]name>[, <args...>])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `fold` | `fold(<[obj/]attr>, <list>[, <init>[, <delim>]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `folderstats` | `folderstats([<player>][, <folder>])` | — | [Channels & Mail](API-Channels-and-Mail) |
| `followers` | `followers(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `following` | `following(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `foreach` | `foreach(<[obj/]attr>, <str>[, <start>[, <end>]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `formdecode` | `formdecode(<query>[, <key>[, <sep>]])` | — | [JSON & Crypto](API-JSON-and-Crypto) |
| `fraction` | `fraction(<num>[, <mixed?>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `fullalias` | `fullalias(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `fullname` | `fullname(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `functions` | `functions([all\|builtin\|local])` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |

### G

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `get` | `get(<obj>/<attr>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `get_eval` | `get_eval(<obj>/<attr>)` | — | [Objects & Database](API-Objects-and-Database) |
| `getpids` | `getpids([<object>])` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `grab` | `grab(<list>, <pattern>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `graball` | `graball(<list>, <pattern>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `grep` | `grep(<obj>, <attrPat>, <substr>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `grepi` | `grepi(<obj>, <attrPat>, <substr>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `gt` | `gt(<n1>, <n2>[, <n3>...])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `gte` | `gte(<n1>, <n2>[, <n3>...])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |

### H

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `hasattr` | `hasattr(<obj>/<attr> \| <obj>, <attr>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `hasattrp` | `hasattrp(<obj>/<attr> \| <obj>, <attr>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `hasattrpval` | `hasattrpval(<obj>/<attr> \| <obj>, <attr>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `hasattrval` | `hasattrval(<obj>/<attr> \| <obj>, <attr>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `hasflag` | `hasflag(<obj>, <flag\|letter>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `haspower` | `haspower(<obj>, <power>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `hastype` | `hastype(<obj>, <type>)` | — | [Objects & Database](API-Objects-and-Database) |
| `height` | `height(<obj>[, <fallback>])` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `hidden` | `hidden(<player>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `hmac` | `hmac(<algo>, <key>, <text>[, <encoding>])` | — | [JSON & Crypto](API-JSON-and-Crypto) |
| `home` | `home(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `host` | `host(<player\|port>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `hostname` | `hostname(<player\|port>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `html` | `html(<text>)` | — | [String & Formatting](API-String-and-Formatting) |

### I

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `ibreak` | `ibreak()` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `idle` | `idle(<player\|port>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `idlesecs` | `idlesecs(<player\|port>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `if` | `if(<cond>, <true>[, <false>])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `ifelse` | `ifelse(<cond>, <true>[, <false>])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `ifeq` | `ifeq(<s1>, <s2>, <true>[, <false>])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `ilev` | `ilev()` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `iname` | `iname([<level\|l>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `inc` | `inc(<int\|prefixInt>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `index` | `index(<list>, <delim>, <start>, <len>)` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `insert` | `insert(<list>, <pos>, <word>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `inum` | `inum([<level\|l>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `inzone` | `inzone(<obj>, <zone>)` | — | [Objects & Database](API-Objects-and-Database) |
| `ipaddr` | `ipaddr(<player\|port>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `isdaylight` | `isdaylight()` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `isdbref` | `isdbref(<str>)` | — | [Objects & Database](API-Objects-and-Database) |
| `isint` | `isint(<str>)` | — | [Objects & Database](API-Objects-and-Database) |
| `isjson` | `isjson(<string>)` | — | [JSON & Crypto](API-JSON-and-Crypto) |
| `isnum` | `isnum(<str>)` | — | [Objects & Database](API-Objects-and-Database) |
| `isobjid` | `isobjid(<str>)` | — | [Objects & Database](API-Objects-and-Database) |
| `isregexp` | `isregexp(<pattern>)` | — | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `isword` | `isword(<str>)` | — | [Objects & Database](API-Objects-and-Database) |
| `itemize` | `itemize(<list>[, <delim>[, <conj>[, <punc>]]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `items` | `items(<list>, <delim>)` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `iter` | `iter(<list>, <pattern>[, <delim>[, <osep>]])` | `noParse` | [Lists & Iteration](API-Lists-and-Iteration) |
| `itext` | `itext([<level\|l>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |

### J

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `json` | `json(<type>[, <arg...>])` | — | [JSON & Crypto](API-JSON-and-Crypto) |
| `json_map` | `json_map(<[obj/]attr>, <json>[, <osep>[, <args...>]])` | — | [JSON & Crypto](API-JSON-and-Crypto) |
| `json_mod` | `json_mod(<json>, <op>, <path>[, <value>])` | — | [JSON & Crypto](API-JSON-and-Crypto) |
| `json_query` | `json_query(<json>[, <op>[, <path...>]])` | — | [JSON & Crypto](API-JSON-and-Crypto) |

### L

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `lalign` | `lalign(<spec>, <list>[, <delim>[, <fill>[, <colSep>[, <rowSep>]]]])` | — | [String & Formatting](API-String-and-Formatting) |
| `last` | `last(<list>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `lattr` | `lattr(<obj>[, <pattern>])` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `lattrp` | `lattrp(<obj>[, <pattern>])` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `lcon` | `lcon(<obj>[, <mode>])` | — | [Objects & Database](API-Objects-and-Database) |
| `lcstr` | `lcstr(<str>)` | — | [String & Formatting](API-String-and-Formatting) |
| `lcstr2` | `lcstr2(<str>)` | — | [String & Formatting](API-String-and-Formatting) |
| `ldelete` | `ldelete(<list>, <posList>[, <delim>[, <osep>]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `left` | `left(<str>, <len>)` | — | [String & Formatting](API-String-and-Formatting) |
| `lemit` | `lemit(<message>)` | `sideEffect` | [Communication & Emits](API-Communication-and-Emits) |
| `letq` | `letq([<r1>, <v1>, ... ,]<body>)` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `lexits` | `lexits(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `lflags` | `lflags(<obj>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `link` | `link(<obj>, <dest>)` | `sideEffect` | [Objects & Database](API-Objects-and-Database) |
| `linsert` | `linsert(<list>, <pos>, <word>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `list` | `list(<type>[, <which>])` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `listq` | `listq([<pattern>])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `lit` | `lit([<expr>])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `ljust` | `ljust(<str>, <width>[, <fill>[, <trunc?>]])` | — | [String & Formatting](API-String-and-Formatting) |
| `llockflags` | `llockflags([<obj>[/<type>]])` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `llocks` | `llocks([<obj>])` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `lmath` | `lmath(<op>, <list>[, <sep>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `ln` | `ln(<val>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `lnum` | `lnum(<count>)` / `lnum(<start>, <end>[, <osep>[, <step>]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `lobjects` | `lobjects(<obj>)` | `alias.cnf` -> `lthings()` | [Objects & Database](API-Objects-and-Database) |
| `loc` | `loc(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `localize` | `localize(<body>)` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `locate` | `locate(<looker>, <what>, <flags>)` | — | [Objects & Database](API-Objects-and-Database) |
| `lock` | `lock(<obj>[/<type>][, <newKey>])` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `lockfilter` | `lockfilter(<boolexp>, <list>[, <delim>])` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `lockflags` | `lockflags([<obj>[/<type>]])` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `lockowner` | `lockowner(<obj>[/<type>])` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `locks` | `locks(<obj>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `log` | `log(<val>[, <base\|e>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `lparent` | `lparent(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `lpids` | `lpids([<object>])` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `lplayers` | `lplayers(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `lports` | `lports([<viewer>])` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `lpos` | `lpos(<str>, <char>)` | — | [String & Formatting](API-String-and-Formatting) |
| `lreplace` | `lreplace(<list>, <posList>, <word>[, <delim>[, <osep>]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `lsearch` | `lsearch(<who>[, <class>, <val>...])` | — | [Objects & Database](API-Objects-and-Database) |
| `lsearchr` | `lsearchr(<who>[, <class>, <val>...])` | — | [Objects & Database](API-Objects-and-Database) |
| `lset` | `lset(<obj>[/<type>], <flags>)` | `sideEffect` | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `lstats` | `lstats([<player\|all>])` | `noguest` | [Objects & Database](API-Objects-and-Database) |
| `lt` | `lt(<n1>, <n2>[, <n3>...])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `lte` | `lte(<n1>, <n2>[, <n3>...])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `lthings` | `lthings(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `lvcon` | `lvcon(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `lvexits` | `lvexits(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `lvobjects` | `lvobjects(<obj>)` | `alias.cnf` -> `lvthings()` | [Objects & Database](API-Objects-and-Database) |
| `lvplayers` | `lvplayers(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `lvthings` | `lvthings(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `lwho` | `lwho()` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `lwhoid` | `lwhoid()` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `lzone` | `lzone(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |

### M

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `mail` | `mail([<player> \| [<player>, ]<[f:]n>])` | — | [Channels & Mail](API-Channels-and-Mail) |
| `maildstats` | `maildstats([<player>])` | — | [Channels & Mail](API-Channels-and-Mail) |
| `mailfrom` | `mailfrom(<[player, ][folder:]num>)` | — | [Channels & Mail](API-Channels-and-Mail) |
| `mailfstats` | `mailfstats([<player>])` | — | [Channels & Mail](API-Channels-and-Mail) |
| `maillist` | `maillist([<player>][, <[folder:]filter>])` | — | [Channels & Mail](API-Channels-and-Mail) |
| `mailsend` | `mailsend(<recipients>, <[subject/]body>)` | `sideEffect` | [Channels & Mail](API-Channels-and-Mail) |
| `mailstats` | `mailstats([<player>])` | — | [Channels & Mail](API-Channels-and-Mail) |
| `mailstatus` | `mailstatus(<[player, ][folder:]num>)` | — | [Channels & Mail](API-Channels-and-Mail) |
| `mailsubject` | `mailsubject(<[player, ][folder:]num>)` | — | [Channels & Mail](API-Channels-and-Mail) |
| `mailtime` | `mailtime(<[player, ][folder:]num>)` | — | [Channels & Mail](API-Channels-and-Mail) |
| `malias` | `malias([<name>[, <sep>]])` | — | [Channels & Mail](API-Channels-and-Mail) |
| `map` | `map(<[obj/]attr>, <list>[, <delim>[, <osep>]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `mapsql` | `mapsql(<[obj/]attr>, <query>[, <osep>[, <colNames?>]])` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `match` | `match(<list>, <pattern>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `matchall` | `matchall(<list>, <pattern>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `max` | `max(<n1>[, <n2>...])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `mean` | `mean(<list1>[, <list2>...])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `median` | `median(<list1>[, <list2>...])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `member` | `member(<list>, <word>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `merge` | `merge(<s1>, <s2>, <matchChars>)` | — | [String & Formatting](API-String-and-Formatting) |
| `message` | `message(<target>, <[obj/]attr>, <msg>[, <args...>])` | `sideEffect` | [Communication & Emits](API-Communication-and-Emits) |
| `mid` | `mid(<str>, <first>, <len>)` | — | [String & Formatting](API-String-and-Formatting) |
| `min` | `min(<n1>[, <n2>...])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `mix` | `mix(<[obj/]attr>, <l1>, <l2>[, <l3>...][, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `mod` | `mod(<i1>, <i2>[, <i3>...])` | also `alias.cnf` | [Math & Numbers](API-Math-and-Numbers) |
| `modulo` | `modulo(<i1>, <i2>[, <i3>...])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `modulus` | `modulus(<i1>, <i2>[, <i3>...])` | also `alias.cnf` | [Math & Numbers](API-Math-and-Numbers) |
| `money` | `money(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `moniker` | `moniker(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `msecs` | `msecs(<obj>)` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `mtime` | `mtime(<obj>)` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `mudname` | `mudname()` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `mudurl` | `mudurl()` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `mul` | `mul(<n1>[, <n2>...])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `munge` | `munge(<[obj/]attr>, <l1>, <l2>[, <delim>[, <osep>]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `mwho` | `mwho()` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `mwhoid` | `mwhoid()` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |

### N

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `name` | `name(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `nameformat` | `nameformat(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `namegrab` | `namegrab(<dbrefList>, <target>[, <delim>])` | — | [Objects & Database](API-Objects-and-Database) |
| `namegraball` | `namegraball(<dbrefList>, <target>[, <delim>])` | — | [Objects & Database](API-Objects-and-Database) |
| `namelist` | `namelist(<players>[, <[obj/]errAttr>])` | — | [Objects & Database](API-Objects-and-Database) |
| `nand` | `nand([<b1>...])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `nattr` | `nattr(<obj>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `nattrp` | `nattrp(<obj>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `ncand` | `ncand([<expr1>...])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `nchildren` | `nchildren(<parent>)` | — | [Objects & Database](API-Objects-and-Database) |
| `ncon` | `ncon(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `ncond` | `ncond(<c1>, <e1>[, <c2>, <e2>...][, <def>])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `ncondall` | `ncondall(<c1>, <e1>[, <c2>, <e2>...][, <def>])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `ncor` | `ncor([<expr1>...])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `nearby` | `nearby(<o1>, <o2>)` | — | [Objects & Database](API-Objects-and-Database) |
| `neq` | `neq(<n1>, <n2>[, <n3>...])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `nexits` | `nexits(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `next` | `next(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `nextdbref` | `nextdbref()` | — | [Objects & Database](API-Objects-and-Database) |
| `nlsearch` | `nlsearch(<who>[, <class>, <val>...])` | — | [Objects & Database](API-Objects-and-Database) |
| `nmwho` | `nmwho()` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `nobjects` | `nobjects(<obj>)` | `alias.cnf` -> `nthings()` | [Objects & Database](API-Objects-and-Database) |
| `nor` | `nor([<b1>...])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `not` | `not([<bool>])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `nplayers` | `nplayers(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `nscemit` | `nscemit(<channel>, <message>)` | `sideEffect` | [Channels & Mail](API-Channels-and-Mail) |
| `nsearch` | `nsearch(<who>[, <class>, <val>...])` | — | [Objects & Database](API-Objects-and-Database) |
| `nsemit` | `nsemit(<message>)` | `sideEffect` | [Communication & Emits](API-Communication-and-Emits) |
| `nslemit` | `nslemit(<message>)` | `sideEffect` | [Communication & Emits](API-Communication-and-Emits) |
| `nsoemit` | `nsoemit(<excluded>, <message>)` | `sideEffect` | [Communication & Emits](API-Communication-and-Emits) |
| `nspemit` | `nspemit(<targets>, <message>)` | `sideEffect` | [Communication & Emits](API-Communication-and-Emits) |
| `nsprompt` | `nsprompt(<targets>, <message>)` | `sideEffect` | [Communication & Emits](API-Communication-and-Emits) |
| `nsremit` | `nsremit(<room>, <message>)` | `sideEffect` | [Communication & Emits](API-Communication-and-Emits) |
| `nszemit` | `nszemit(<zone>, <message>)` | `sideEffect` | [Communication & Emits](API-Communication-and-Emits) |
| `nthings` | `nthings(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `null` | `null([<expr...>])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `num` | `num(<ref>)` | — | [Objects & Database](API-Objects-and-Database) |
| `numversion` | `numversion()` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `nvcon` | `nvcon(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `nvexits` | `nvexits(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `nvobjects` | `nvobjects(<obj>)` | `alias.cnf` -> `nvthings()` | [Objects & Database](API-Objects-and-Database) |
| `nvplayers` | `nvplayers(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `nvthings` | `nvthings(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `nwho` | `nwho()` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |

### O

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `obj` | `obj(<obj>)` | — | [String & Formatting](API-String-and-Formatting) |
| `objeval` | `objeval(<obj>, <expr>)` | `noParse` | [Objects & Database](API-Objects-and-Database) |
| `objid` | `objid(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `objmem` | `objmem(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `oemit` | `oemit(<excluded>, <message>)` | `sideEffect` | [Communication & Emits](API-Communication-and-Emits) |
| `oob` | `oob(<targets>, <package>[, <json>])` | — | [Client & MUjs UI](API-Client-and-MUjs-UI) |
| `open` | `open(<name>[, <dest>[, <srcRoom>]])` | `sideEffect` | [Objects & Database](API-Objects-and-Database) |
| `or` | `or([<b1>...])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `ord` | `ord(<str>)` | — | [String & Formatting](API-String-and-Formatting) |
| `ordinal` | `ordinal(<int>)` | — | [String & Formatting](API-String-and-Formatting) |
| `ordnumber` | `ordnumber(<int>)` | — | [String & Formatting](API-String-and-Formatting) |
| `orflags` | `orflags(<obj>, <letters>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `orlflags` | `orlflags(<obj>, <flagList>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `orlpowers` | `orlpowers(<obj>, <powerList>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `owner` | `owner(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |

### P

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `parallel_map` | `parallel_map(<list>, <delimiter>, <ufun>)` | — | [Async & Background Tasks](API-Async-and-Background-Tasks) |
| `parent` | `parent(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `parse` | `parse(<list>, <pattern>[, <delim>[, <osep>]])` | `noParse`, also `alias.cnf` | [Lists & Iteration](API-Lists-and-Iteration) |
| `pcreate` | `pcreate(<name>, <pass>[, <dbref>])` | — | [Objects & Database](API-Objects-and-Database) |
| `pemit` | `pemit(<targets>, <message>)` | `sideEffect` | [Communication & Emits](API-Communication-and-Emits) |
| `pfun` | `pfun(<attr>[, <args...>])` | — | [Objects & Database](API-Objects-and-Database) |
| `pgrep` | `pgrep(<obj>, <attrPat>, <substr>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `pi` | `pi()` | — | [Math & Numbers](API-Math-and-Numbers) |
| `pickrand` | `pickrand(<list>[, <delim>])` | also `alias.cnf` | [Lists & Iteration](API-Lists-and-Iteration) |
| `pidinfo` | `pidinfo(<pid>[, <fields>[, <sep>]])` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `player` | `player(<port>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `playermem` | `playermem(<player>)` | — | [Objects & Database](API-Objects-and-Database) |
| `pmatch` | `pmatch(<spec>)` | — | [Objects & Database](API-Objects-and-Database) |
| `poll` | `poll()` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `ports` | `ports(<player>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `pos` | `pos(<needle>, <haystack>)` | — | [String & Formatting](API-String-and-Formatting) |
| `poss` | `poss(<obj>)` | — | [String & Formatting](API-String-and-Formatting) |
| `power` | `power(<base>, <exp>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `powers` | `powers(<obj>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `prompt` | `prompt(<targets>, <message>)` | `sideEffect` | [Communication & Emits](API-Communication-and-Emits) |
| `pueblo` | `pueblo([<player>])` | — | [String & Formatting](API-String-and-Formatting) |

### Q

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `quota` | `quota([<player>])` | — | [Objects & Database](API-Objects-and-Database) |

### R

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `r` | `r(<name>[, <type>])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `rand` | `rand([<bound> \| <low>, <high>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `randextract` | `randextract(<list>[, <count>[, <delim>[, <mode>[, <osep>]]]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `randword` | `randword(<list>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `recv` | `recv(<player\|port>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `regedit` | `regedit(<string>, <regexp1>, <repl1>[, <regexp2>, <repl2>...])` | `noParse` | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `regeditall` | `regeditall(<string>, <regexp1>, <repl1>[, <regexp2>, <repl2>...])` | `noParse` | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `regeditalli` | `regeditalli(<string>, <regexp1>, <repl1>[, <regexp2>, <repl2>...])` | `noParse` | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `regediti` | `regediti(<string>, <regexp1>, <repl1>[, <regexp2>, <repl2>...])` | `noParse` | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `registers` | `registers([<pat>[, <types>[, <osep>]]])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `reglattr` | `reglattr(<obj>[/<re>][, <osep>])` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `reglattrp` | `reglattrp(<obj>[/<re>][, <osep>])` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `reglmatch` | `reglmatch(<list>, <regexp>[, <delim>])` | — | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `reglmatchall` | `reglmatchall(<list>, <regexp>[, <delim>[, <osep>]])` | — | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `reglmatchalli` | `reglmatchalli(<list>, <regexp>[, <delim>[, <osep>]])` | — | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `reglmatchi` | `reglmatchi(<list>, <regexp>[, <delim>])` | — | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `regmatch` | `regmatch(<string>, <regexp>[, <reglist...>])` | — | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `regmatchi` | `regmatchi(<string>, <regexp>[, <reglist...>])` | — | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `regnattr` | `regnattr(<obj>[/<re>])` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `regnattrp` | `regnattrp(<obj>[/<re>])` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `regrab` | `regrab(<list>, <regexp>[, <delim>])` | — | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `regraball` | `regraball(<list>, <regexp>[, <delim>[, <osep>]])` | — | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `regraballi` | `regraballi(<list>, <regexp>[, <delim>[, <osep>]])` | — | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `regrabi` | `regrabi(<list>, <regexp>[, <delim>])` | — | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `regrep` | `regrep(<obj>, <attrPattern>, <regexp>)` | — | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `regrepi` | `regrepi(<obj>, <attrPattern>, <regexp>)` | — | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `regsub` | `regsub(<string>, <regexp1>, <repl1>[, <regexp2>, <repl2>...])` | `noParse` | [Regex & Wildcards](API-Regex-and-Wildcards) |
| `regxattr` | `regxattr(<obj>[/<re>], <start>, <count>[, <osep>])` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `regxattrp` | `regxattrp(<obj>[/<re>], <start>, <count>[, <osep>])` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `remainder` | `remainder(<i1>, <i2>[, <i3>...])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `remit` | `remit(<room>, <message>)` | `sideEffect` | [Communication & Emits](API-Communication-and-Emits) |
| `remove` | `remove(<list>, <words>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `render` | `render(<text>[, <flags>])` | — | [String & Formatting](API-String-and-Formatting) |
| `repeat` | `repeat(<str>, <count>)` | — | [String & Formatting](API-String-and-Formatting) |
| `replace` | `replace(<list>, <posList>, <word>[, <delim>[, <osep>]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `rest` | `rest(<list>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `restarts` | `restarts()` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `restarttime` | `restarttime()` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `reswitch` | `reswitch(<target>, <re1>, <act1>[, ...][, <def>])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `reswitchall` | `reswitchall(<target>, <re1>, <act1>[, ...][, <def>])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `reswitchalli` | `reswitchalli(<target>, <re1>, <act1>[, ...][, <def>])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `reswitchi` | `reswitchi(<target>, <re1>, <act1>[, ...][, <def>])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `reverse` | `reverse(<str>)` | — | [String & Formatting](API-String-and-Formatting) |
| `revwords` | `revwords(<list>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `right` | `right(<str>, <len>)` | — | [String & Formatting](API-String-and-Formatting) |
| `rjust` | `rjust(<str>, <width>[, <fill>[, <trunc?>]])` | — | [String & Formatting](API-String-and-Formatting) |
| `rloc` | `rloc(<obj>[, <levels>])` | — | [Objects & Database](API-Objects-and-Database) |
| `rnum` | `rnum(<ref>)` | — | [Objects & Database](API-Objects-and-Database) |
| `room` | `room(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `root` | `root(<x>, <n>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `round` | `round(<num>, <digits>[, <pad?>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `rpos` | `rpos(<haystack>, <needle>)` | — | [String & Formatting](API-String-and-Formatting) |

### S

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `s` | `s(<expr>)` | — | [Objects & Database](API-Objects-and-Database) |
| `scan` | `scan([<looker>, ]<commandStr>)` | — | [Objects & Database](API-Objects-and-Database) |
| `scramble` | `scramble(<str>)` | — | [String & Formatting](API-String-and-Formatting) |
| `search` | `search(<who>[, <class>, <val>...])` | `alias.cnf` -> `lsearch()` | [Objects & Database](API-Objects-and-Database) |
| `secs` | `secs()` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `secs2time` | `secs2time(<secs>)` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `secscalc` | `secscalc(<base\|now>[, <Â±delta>...])` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `secure` | `secure(<str>)` | — | [String & Formatting](API-String-and-Formatting) |
| `sent` | `sent(<player\|port>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `set` | `set(<obj>, <flag\|!flag\|attr:val>)` | `sideEffect` | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `setdiff` | `setdiff(<l1>, <l2>[, <delim>[, <sortType>[, <osep>]]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `setinter` | `setinter(<l1>, <l2>[, <delim>[, <sortType>[, <osep>]]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `setq` | `setq(<reg1>, <val1>[, <reg2>, <val2>...])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `setr` | `setr(<reg1>, <val1>[, <reg2>, <val2>...])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `setsymdiff` | `setsymdiff(<l1>, <l2>[, <delim>[, <sortType\|osep>[, <osep>]]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `setunion` | `setunion(<l1>, <l2>[, <delim>[, <sortType>[, <osep>]]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `sha0` | `sha0(<string>)` | — | [JSON & Crypto](API-JSON-and-Crypto) |
| `shl` | `shl(<u>, <bits>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `shr` | `shr(<u>, <bits>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `shuffle` | `shuffle(<list>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `sign` | `sign(<num>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `sin` | `sin(<angle>[, <unit>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `slev` | `slev()` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `sort` | `sort(<list>[, <sortType>[, <delim>[, <osep>]]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `sortby` | `sortby(<[obj/]attr>, <list>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `sortkey` | `sortkey(<[obj/]attr>, <list>[, <sortType>[, <delim>[, <osep>]]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `soundex` | `soundex(<word>[, <type>])` | — | [String & Formatting](API-String-and-Formatting) |
| `soundlike` | `soundlike(<w1>, <w2>[, <type>])` | also `alias.cnf` | [String & Formatting](API-String-and-Formatting) |
| `soundslike` | `soundslike(<w1>, <w2>[, <type>])` | — | [String & Formatting](API-String-and-Formatting) |
| `space` | `space(<count>)` | — | [String & Formatting](API-String-and-Formatting) |
| `speak` | `speak(<speaker>, <text>[, <verb>])` | — | [Communication & Emits](API-Communication-and-Emits) |
| `spellnum` | `spellnum(<int>)` | — | [String & Formatting](API-String-and-Formatting) |
| `splice` | `splice(<l1>, <l2>, <word>[, <delim>[, <osep>]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `sql` | `sql(<query>[, <rowSep>[, <colSep>]])` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `sqlescape` | `sqlescape(<string>)` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `sqrt` | `sqrt(<num>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `squish` | `squish(<str>[, <sep>])` | — | [String & Formatting](API-String-and-Formatting) |
| `ssl` | `ssl([<player>])` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `starttime` | `starttime()` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `stats` | `stats([<player\|all>])` | `alias.cnf` -> `lstats()` | [Objects & Database](API-Objects-and-Database) |
| `stddev` | `stddev(<list1>[, <list2>...])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `step` | `step(<[obj/]attr>, <list>, <size>[, <delim>[, <osep>]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `stext` | `stext(<level\|l>)` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `strallof` | `strallof(<expr1>[, <expr2>...], <sep>)` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `strcat` | `strcat([<s1>...])` | — | [String & Formatting](API-String-and-Formatting) |
| `strdelete` | `strdelete(<str>, <pos>, <len>)` | — | [String & Formatting](API-String-and-Formatting) |
| `strfirstof` | `strfirstof(<expr1>[, <expr2>...])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `stringsecs` | `stringsecs(<duration>)` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `strinsert` | `strinsert(<str>, <pos>, <ins>)` | — | [String & Formatting](API-String-and-Formatting) |
| `strip` | `strip(<str>)` | — | [String & Formatting](API-String-and-Formatting) |
| `stripaccents` | `stripaccents(<str>)` | — | [String & Formatting](API-String-and-Formatting) |
| `stripansi` | `stripansi(<str>)` | — | [String & Formatting](API-String-and-Formatting) |
| `strlen` | `strlen([<str>])` | — | [String & Formatting](API-String-and-Formatting) |
| `strmatch` | `strmatch(<str>, <pattern>[, <reglist>])` | — | [String & Formatting](API-String-and-Formatting) |
| `strreplace` | `strreplace(<str>, <pos>, <len>, <ins>)` | — | [String & Formatting](API-String-and-Formatting) |
| `sub` | `sub(<n1>[, <n2>...])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `subj` | `subj(<obj>)` | — | [String & Formatting](API-String-and-Formatting) |
| `suggest` | `suggest(<category>, <word>[, <delim>[, <limit>]])` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `switch` | `switch(<target>, <pat1>, <act1>[, ...][, <def>])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `switchall` | `switchall(<target>, <pat1>, <act1>[, ...][, <def>])` | `noParse` | [Logic, Control & Registers](API-Logic-Control-and-Registers) |

### T

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `t` | `t([<expr>])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `table` | `table(<list>[, <fieldW>[, <lineW>[, <delim>[, <osep>]]]])` | — | [String & Formatting](API-String-and-Formatting) |
| `tag` | `tag(<name>[, <attr>...])` | — | [String & Formatting](API-String-and-Formatting) |
| `tagwrap` | `tagwrap(<name>[, <attr>...], <inner>)` | — | [String & Formatting](API-String-and-Formatting) |
| `tan` | `tan(<angle>[, <unit>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `tel` | `tel(<victim>, <dest>)` | `sideEffect` | [Objects & Database](API-Objects-and-Database) |
| `terminfo` | `terminfo(<player\|port>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `testlock` | `testlock(<boolexp>, <victim>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `textentries` | `textentries(<file>, <pattern>[, <sep>])` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `textfile` | `textfile(<file>, <topic>)` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `textsearch` | `textsearch(<file>, <needle>[, <sep>])` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `time` | `time()` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `timecalc` | `timecalc(<base\|now>[, <Â±delta>...])` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `timefmt` | `timefmt(<format>[, <secs>])` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `timestring` | `timestring(<secs>[, <pad>])` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `tr` | `tr(<str>, <from>, <to>)` | — | [String & Formatting](API-String-and-Formatting) |
| `trim` | `trim(<str>[, <chars>[, <style>]])` | — | [String & Formatting](API-String-and-Formatting) |
| `trimpenn` | `trimpenn(<str>[, <chars>[, <style>]])` | — | [String & Formatting](API-String-and-Formatting) |
| `trimtiny` | `trimtiny(<str>[, <style>[, <chars>]])` | — | [String & Formatting](API-String-and-Formatting) |
| `trunc` | `trunc(<str>)` | — | [Math & Numbers](API-Math-and-Numbers) |
| `type` | `type(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `tzoffset` | `tzoffset()` | — | [Time & Scheduling](API-Time-and-Scheduling) |

### U

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `u` | `u(<obj/attr \| #lambda/expr>[, <args...>])` | — | [Objects & Database](API-Objects-and-Database) |
| `ucstr` | `ucstr(<str>)` | — | [String & Formatting](API-String-and-Formatting) |
| `ucstr2` | `ucstr2(<str>)` | — | [String & Formatting](API-String-and-Formatting) |
| `udefault` | `udefault(<[obj/]attr>, <default>[, <args...>])` | `noParse` | [Objects & Database](API-Objects-and-Database) |
| `ufun` | `ufun(<obj/attr \| #lambda/expr>[, <args...>])` | — | [Objects & Database](API-Objects-and-Database) |
| `ulambda` | `ulambda(<#lambda/expr \| obj/attr>[, <args...>])` | — | [Objects & Database](API-Objects-and-Database) |
| `uldefault` | `uldefault(<[obj/]attr>, <default>[, <args...>])` | `noParse` | [Objects & Database](API-Objects-and-Database) |
| `ulocal` | `ulocal(<obj/attr \| #lambda/expr>[, <args...>])` | — | [Objects & Database](API-Objects-and-Database) |
| `unique` | `unique(<list>[, <sortType>[, <delim>[, <osep>]]])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `unsetq` | `unsetq([<patterns>])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `uptime` | `uptime()` | — | [Time & Scheduling](API-Time-and-Scheduling) |
| `urldecode` | `urldecode(<string>)` | — | [JSON & Crypto](API-JSON-and-Crypto) |
| `urlencode` | `urlencode(<string>)` | — | [JSON & Crypto](API-JSON-and-Crypto) |
| `utctime` | `utctime()` | — | [Time & Scheduling](API-Time-and-Scheduling) |

### V

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `v` | `v(<attr \| regChar \| argIdx>)` | — | [Objects & Database](API-Objects-and-Database) |
| `vadd` | `vadd(<v1>, <v2>[, <sep>[, <osep>]])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `val` | `val(<str>)` | also `alias.cnf` | [Math & Numbers](API-Math-and-Numbers) |
| `valid` | `valid(<category>, <value>)` | — | [Objects & Database](API-Objects-and-Database) |
| `vcross` | `vcross(<v1>, <v2>[, <sep>[, <osep>]])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `vdim` | `vdim(<v>[, <sep>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `vdot` | `vdot(<v1>, <v2>[, <sep>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `version` | `version()` | — | [SQL, HTTP & System](API-SQL-HTTP-and-System) |
| `visible` | `visible(<looker>, <target>)` | — | [Objects & Database](API-Objects-and-Database) |
| `vmag` | `vmag(<v>[, <sep>])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `vmax` | `vmax(<v1>, <v2>[, <sep>[, <osep>]])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `vmin` | `vmin(<v1>, <v2>[, <sep>[, <osep>]])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `vmul` | `vmul(<v1>, <v2>[, <sep>[, <osep>]])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `vsub` | `vsub(<v1>, <v2>[, <sep>[, <osep>]])` | — | [Math & Numbers](API-Math-and-Numbers) |
| `vunit` | `vunit(<v>[, <sep>[, <osep>]])` | — | [Math & Numbers](API-Math-and-Numbers) |

### W

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `where` | `where(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `width` | `width(<obj>[, <fallback>])` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `wildgrep` | `wildgrep(<obj>, <attrPat>, <wildPat>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `wildgrepi` | `wildgrepi(<obj>, <attrPat>, <wildPat>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `wipe` | `wipe(<obj>[/<pattern>])` | `sideEffect` | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `wordpos` | `wordpos(<str>, <charPos>[, <sep>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `words` | `words(<list>[, <delim>])` | — | [Lists & Iteration](API-Lists-and-Iteration) |
| `wrap` | `wrap(<str>, <width>[, <width1st>[, <lineSep>]])` | — | [String & Formatting](API-String-and-Formatting) |
| `wshtml` | `wshtml(<html>[, <fallback>])` | — | [Client & MUjs UI](API-Client-and-MUjs-UI) |
| `wsjson` | `wsjson(<json>[, <fallback>])` | — | [Client & MUjs UI](API-Client-and-MUjs-UI) |

### X

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `xattr` | `xattr(<obj>, <pat>, <start>, <count>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `xattrp` | `xattrp(<obj>, <pat>, <start>, <count>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `xcon` | `xcon(<obj>, <start>, <count>)` | — | [Objects & Database](API-Objects-and-Database) |
| `xexits` | `xexits(<obj>, <start>, <count>)` | — | [Objects & Database](API-Objects-and-Database) |
| `xget` | `xget(<obj>, <attr>)` | — | [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks) |
| `xmwho` | `xmwho(<start>, <count>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `xmwhoid` | `xmwhoid(<start>, <count>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `xobjects` | `xobjects(<obj>, <start>, <count>)` | `alias.cnf` -> `xthings()` | [Objects & Database](API-Objects-and-Database) |
| `xor` | `xor([<b1>...])` | — | [Logic, Control & Registers](API-Logic-Control-and-Registers) |
| `xplayers` | `xplayers(<obj>, <start>, <count>)` | — | [Objects & Database](API-Objects-and-Database) |
| `xthings` | `xthings(<obj>, <start>, <count>)` | — | [Objects & Database](API-Objects-and-Database) |
| `xvcon` | `xvcon(<obj>, <start>, <count>)` | — | [Objects & Database](API-Objects-and-Database) |
| `xvexits` | `xvexits(<obj>, <start>, <count>)` | — | [Objects & Database](API-Objects-and-Database) |
| `xvobjects` | `xvobjects(<obj>, <start>, <count>)` | `alias.cnf` -> `xvthings()` | [Objects & Database](API-Objects-and-Database) |
| `xvplayers` | `xvplayers(<obj>, <start>, <count>)` | — | [Objects & Database](API-Objects-and-Database) |
| `xvthings` | `xvthings(<obj>, <start>, <count>)` | — | [Objects & Database](API-Objects-and-Database) |
| `xwho` | `xwho(<start>, <count>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |
| `xwhoid` | `xwhoid(<start>, <count>)` | — | [Connections & ConnLog](API-Connections-and-ConnLog) |

### Z

| Function | Signature | Tags | Domain Page |
|---|---|---|---|
| `zemit` | `zemit(<zone>, <message>)` | `sideEffect` | [Communication & Emits](API-Communication-and-Emits) |
| `zfun` | `zfun(<attr>[, <args...>])` | — | [Objects & Database](API-Objects-and-Database) |
| `zmwho` | `zmwho(<zone>)` | — | [Objects & Database](API-Objects-and-Database) |
| `zone` | `zone(<obj>)` | — | [Objects & Database](API-Objects-and-Database) |
| `zwho` | `zwho(<zone>)` | — | [Objects & Database](API-Objects-and-Database) |

---

Back to [Home](Home)
