# PennMUSH.NET Function Backward-Compatibility Gap Audit

This document compares all 18 C# function modules in `PennMUSH.Core/MUSHcode/Functions/` against the original PennMUSH C source in `C:\Repos\pennmush\src\` (`function.c` `flist[]` and `fun*.c` / subsystem `.c` files).

---

## Suggested Follow-Up PR Roadmap

To address these gaps cleanly without massive monolithic diffs, they group naturally into **6 focused follow-up PRs**:

| PR | Theme | Key High-Severity Fixes |
|---|---|---|
| **PR 1** | **Evaluator, `noParse` & Register/Capture Context** | `NULL` (`FN_REG`), `SWITCH*` (`%0`–`%9` captures), `RESWITCH*` (`$0`–`$n` captures), `REGEDIT*` (`FN_NOPARSE` + per-match eval), `REGMATCH*` (space-separated reg list + `pos:reg` + `-`), `ULAMBDA`/`DEFAULT`/`EDEFAULT`/`OBJEVAL` (`noParse` & eval context), `V`, `R`/`SETQ`/`SETR`/`LETQ`/`LISTQ`/`REGISTERS`, `FN` |
| **PR 2** | **Name Collisions, Wrong Subsystems & Swapped Signatures** | `SENT`/`RECV` (network bytes vs mail), `CEMIT` (channel emit vs OOB), `LPORTS`/`PLAYER`/`CMDS`/`MWHO*`/`INAME`/`NAMEFORMAT`, `CBUFFER` (buffer size vs recall), `MAILFSTATS` (vs `FOLDERSTATS`), `TESTLOCK` (boolexp vs named lock), `WHERE` (immediate loc vs `ROOM`), `ENTRANCES` (links vs contents), `STEP`/`MUNGE`/`SPLICE`/`LNUM`/`UNIQUE`/`TAGWRAP` arg orders, `ART`, `SUBJ`/`POSS`/`OBJ`/`APOSS` |
| **PR 3** | **Permission, Visibility & Side-Effect Gating** | `FUNCTION_SIDE_EFFECTS`/`FN_NOSIDEFX` + `@command` checks across `Section9And18Functions`, `ConnectionAndZoneFunctions`, `ChannelFunctions`, `MailFunctions`; `controls`/`Can_Examine`/`Can_Locate`/`See_All`/`Priv_Who`/`Can_Read_Attr`/`CanEvalAttr` across `DbFunctions`, `ChannelFunctions`, `MailFunctions`, `ConnectionAndZoneFunctions`, `TextFormatFunctions`, `LateSubsystemFunctions` |
| **PR 4** | **ANSI/Markup-Aware String, List & TextFormat Engine** | Markup-aware `STRLEN`/`MID`/`LEFT`/`RIGHT`/`LJUST`/`RJUST`/`CENTER`/`WRAP`/`TABLE`/`ALIGN`/`TRIM`/`SQUISH`/`REST`/`FOREACH`; `ANSI` (256/RGB/hex/`/bg`), `COLORS`, `RENDER`, `HTML`, `PUEBLO`, `OOB`, `WSHTML`, `STRIPANSI`, `BEFORE`/`AFTER`, `LPOS`, `SECURE`/`ESCAPE`, `COMP`, `EDIT`, `ACCENT` |
| **PR 5** | **List, Math, Time, Crypto & JSON Parity** | `MEMBER`/`REMOVE`/`SORT`/`SETUNION`/`SETINTER`/`SETDIFF`/`ITEMS`/`FILTER`/`MAP`/`FOLD`/`ITER`/`IBREAK`/`ITEXT`/`INUM`; `BNAND`/`BNOT`/`BAND`/`BOR`/`BXOR`/`SHL`/`SHR`/`INC`/`DEC`/`BOUND`/`VUNIT`/`REMAINDER`/`DIV`/`ROUND`/`TRUNC`/`LOG` + numeric validation; `TIMEFMT`/`ETIMEFMT`/`ETIME`/`MTIME`/`MSECS`/`UPTIME`/`TIMESTRING`/`SECSCALC`; `SSL`/`DIGEST`/`ENCRYPT`/`DECRYPT`/`URLDECODE`/`FORMDECODE`; `JSON`/`JSON_QUERY`/`JSON_MOD`/`JSON_MAP`/`WSJSON` |
| **PR 6** | **Channel, Mail, WHO/Connection & Late Subsystems** | `CRECALL`/`CBUFFERADD`/`CFLAGS`/`CLFLAGS`/`CHANNELS`/`CLOCK`/`CSTATUS`/`CTITLE`/`CWHO`; `MAIL`/`MAILLIST`/`MAILSTATS`/`MAILDSTATS`/`MAILFSTATS`/`FOLDERSTATS`/`MAILSEND`/`MALIAS`; `LWHO*`/`ZWHO`/`ZMWHO`/`TERMINFO`/`HOST*`/`IPADDR`; `BENCHMARK`/`QUOTA`/`BEEP`/`SUGGEST`/`TEXT*`/`CONFIG`/`CHECKPASS`/`ROOT`; plus `SQL`/`MAPSQL`/`ADDRLOG`/`CONNLOG`/`CONNRECORD` once backends exist |

> [!NOTE]
> **Non-Standard / Extension Functions**: `CronFunctions.cs` (`CRON_LIST`, `CRON_INFO`), `AsyncFunctions.cs` (`ASYNC`, `PARALLEL_MAP`, `BGTASK`), `ClientFunctions.cs` (`CEDIT`, `CWINDOW`, `CSTYLE`, `CAUDIO` — *excluding `CEMIT`*), and `DbFunctions.cs` (`EXITS`, `ATTRCNT`, `LZONE`, `INZONE`, `NAMEFORMAT`) are PennMUSH.NET extensions not present in original PennMUSH `function.c`.

---

## Detailed Findings by Module

### 1. `LogicFunctions.cs`
- **`NULL`** (`LogicFunctions.cs:65` vs `function.c:647`, `funmisc.c:1285`) — **High**: Registered with `noParse: true`; original is `FN_REG` so side-effect arguments (e.g., `null(setq(0, foo))`) are evaluated. (`@@` is `FN_NOPARSE`.)
- **`SWITCH` / `SWITCHALL` / `CASE` / `CASEALL`** (`LogicFunctions.cs:46-49, 243-282` vs `funmisc.c:872-963`) — **High**: Accepts 2 args (original requires min 3); `WildOrRangeMatch` discards `PatternMatcher.Match` captures (`out _`), so `%0`–`%9` are never populated during action evaluation.
- **`RESWITCH` / `RESWITCHALL` / `RESWITCHI` / `RESWITCHALLI`** (`LogicFunctions.cs:50-53, 284-330` vs `funmisc.c:991-1101`) — **High**: Accepts 2 args (min 3 in original); only replaces `#$` and never populates `PE_REGS_REGEXP` (`$0`, `$1`, `$<name>`).
- **`R`** (`LogicFunctions.cs:62, 379-383` vs `funmisc.c:707-755`) — **Medium**: Ignores 2nd `<type>` arg (`qregisters`, `regexp`, `switch`, `iter`, `args`/`stack`) and skips `ValidQregName` validation.
- **`SETQ` / `SETR`** (`LogicFunctions.cs:60-61, 364-377` vs `funmisc.c:321-352`) — **Medium**: Silently ignores odd/insufficient args instead of returning `#-1 FUNCTION (...) EXPECTS AN EVEN NUMBER OF ARGUMENTS`; skips `ValidQregName` and register-limit checks.
- **`LETQ`** (`LogicFunctions.cs:68, 402-422` vs `funmisc.c:354-414`) — **Medium**: Returns generic `TooFewArguments` on even arg counts and skips `ValidQregName` validation.
- **`LISTQ` / `REGISTERS`** (`LogicFunctions.cs:70-71, 424-430` vs `funmisc.c:443-570`) — **Medium**: `REGISTERS` is aliased to `Listq`, missing `<types>`, `<osep>`, and type prefixes (`Q`, `R`, `S`, `T`, `A`).
- **`STEXT`** (`LogicFunctions.cs:74, 452-463` vs `funmisc.c:969-988`) — **Medium**: Defaults to `0` on 0 args and rejects `"l"`/`"L"` (`maxlev`).
- **`FN`** (`LogicFunctions.cs:75, 465-477` vs `funufun.c:37-94`) — **Medium**: Missing `<obj>/<func>` syntax and `PE_BUILTINONLY` restriction (allows `@function` softfunctions).
- **`CAND` / `NCAND` / `COR` / `NCOR` / `STRFIRSTOF` / `STRALLOF` / `ALLOF`** (`LogicFunctions.cs:27-30, 56-58`) — **Low**: Missing minimum argument count enforcement.

### 2. `MathFunctions.cs`
- **`BNAND`** (`MathFunctions.cs:57, 597-609` vs `funmath.c:1751-1760`) — **High**: Computes `~(acc & b)` over 2+ signed `long`s instead of 2-arg unsigned `a & (~b)` (bit-clear).
- **`BNOT` / `BAND` / `BOR` / `BXOR`** (`MathFunctions.cs:54-56, 58, 561-595, 611-616` vs `funmath.c:1766-1773, 2336-2415`) — **High**: Operate on signed `long` instead of unsigned 64-bit (`ulong`), accept negatives, and default non-integers to `0` instead of returning `e_uint`/`e_uints`.
- **`INC` / `DEC`** (`MathFunctions.cs:28-29, 255-273` vs `funmath.c:260-350`) — **High**: Parse as `double` and accept a 2nd `step` arg; original takes 1 arg only and increments/decrements strings ending in an integer (e.g., `inc(foo10)` -> `foo11`), else `#-1 ARGUMENT MUST END IN AN INTEGER`.
- **`BOUND`** (`MathFunctions.cs:27, 246-253` vs `funmath.c:1733-1747`) — **High**: Requires 3 args (`<max>` is optional in original) and skips `e_nums` validation.
- **`VUNIT`** (`MathFunctions.cs:76, 999-1010` vs `funmath.c:706-758`) — **High**: Returns `#-1 DIVISION BY ZERO` for zero vectors instead of a zero vector of the same dimension.
- **`REMAINDER` / `DIV`** (`MathFunctions.cs:16, 22, 119-138, 188-199` vs `funmath.c:2115-2156, 2293-2334`) — **High**: Accept floats instead of enforcing integer-only (`e_ints`).
- **`ROUND`** (`MathFunctions.cs:31, 282-289` vs `funmath.c:884-936`) — **Medium**: Ignores 3rd `<pad>` arg (strips trailing zeros).
- **`TRUNC`** (`MathFunctions.cs:34, 305-310` vs `funmath.c:353-360`) — **Medium**: Uses `double.TryParse` on full string instead of leading integer prefix (`parse_integer`).
- **`LOG`** (`MathFunctions.cs:38, 328-337` vs `funmath.c:1159-1198`) — **Medium**: Ignores `"e"` base arg and allows `0 < base <= 1` (`#-1 BASE OUT OF RANGE`).
- **`SHL` / `SHR`** (`MathFunctions.cs:59-60, 618-634` vs `funmath.c:240-257`) — **Medium**: Use signed `long` and default invalid args to `0`.
- **Missing numeric/integer validation** (`MathFunctions.cs:81-474, 534-663` vs `funmath.c`) — **Medium**: `add`, `sub`, `mul`, `modulo`, `abs`, `min`, `max`, `sign`, `floor`, `ceil`, `sqrt`, `power`, `log`, `ln`, `sin`, `cos`, `tan`, `asin`, `acos`, `atan`, `dist2d`, `dist3d`, `mean`, `median`, `stddev` treat non-numeric args as `0` or skip them instead of returning `e_num`/`e_nums`/`e_int`/`e_ints`.

### 3. `TimeFunctions.cs`
- **`TIMEFMT`** (`TimeFunctions.cs:15, 93-121` vs `funtime.c:37-133`) — **High**: Uses `%`-prefixed codes instead of `$`-prefixed codes (`$Y`, `$m`, `$d`, etc.); missing 3rd `<timezone>` arg and invalid-escape error.
- **`ETIMEFMT`** (`TimeFunctions.cs:16, 123-139` vs `funtime.c:392-404, 751-1086`) — **High**: Only supports `$d/$h/$m/$s` (with forced 2-digit padding); missing `$w`, `$y`, uppercase zero-padded codes, width specifiers, `t`/`x`/`z` flags, `$$`, and `e_uint` check.
- **`ETIME`** (`TimeFunctions.cs:28, 208-209` vs `funtime.c:357-389`) — **High**: Aliased to `Secs2Time`, omitting years (`y`), weeks (`w`), 2nd `<width>` arg, and `e_int`/`e_range` errors.
- **`MTIME` / `MSECS`** (`TimeFunctions.cs:19, 27, 151-161` vs `fundb.c:1503-1529`) — **High**: Both return integer Unix seconds; `MTIME(<obj>[, <utc>])` should return a formatted `show_time` string, and both need `Can_Examine` and `!IsPlayer` permission checks.
- **`UPTIME`** (`TimeFunctions.cs:35, 310-311` vs `game.c:2280-2334`) — **High**: Takes 0 args and returns elapsed uptime instead of optional `<type>` (`upsince`, `reboot`, `save`, `nextsave`, `dbck`, `purge`, `warnings`) returning Unix timestamps/countdowns.
- **`TIME` / `CONVSECS` / `CONVTIME` / `ISDAYLIGHT`** (`TimeFunctions.cs:39-72, 141-144` vs `funtime.c:136-213, 656-730`) — **Medium**: Missing `<tz>` support (and `<secs>` in `ISDAYLIGHT`); `CONVSECS` defaults invalid/missing `<secs>` to now instead of `e_int`; day-of-month isn't 2-char space-padded.
- **`TIMESTRING`** (`TimeFunctions.cs:30, 254-271` vs `funtime.c:478-520`) — **Medium**: Missing `%2u` space-padding, intermediate-zero retention on `pad == 0`, and `%02ud` on `pad == 2`.
- **`SECSCALC` / `TIMECALC`** (`TimeFunctions.cs:31-32, 273-305` vs `funtime.c:1088-1177`) — **Medium**: Only supports `+Nd`/`-Nh` tokens, missing SQLite `strftime` modifiers (`start of month`, `+1 month`, `weekday 0`, `localtime`, `utc`).
- **`CSECS`** (`TimeFunctions.cs:25, 186-194` vs `fundb.c:1492-1500`) — **Medium**: Falls back to `ConvTime` instead of returning `#-1 NO MATCH` on missing object.
- **`STRINGSECS`** (`TimeFunctions.cs:29, 211-217` vs `funtime.c:407-475`) — **Low**: Returns `"0"` on empty and `ArgumentMustBeInteger` on invalid instead of `#-1 INVALID TIMESTRING`.

### 4. `CryptoFunctions.cs`
- **`SSL`** (`CryptoFunctions.cs:23, 183-184` vs `bsd.c:6953-6967`) — **High**: Hardcoded stub returning `"0"` instead of checking descriptor SSL state and `See_All`.
- **`DIGEST`** (`CryptoFunctions.cs:18, 77-92` vs `funcrypt.c:411-448`) — **Medium**: `digest(list)` (1 arg) fails with `TooFewArguments`; unknown digest returns `#-1` instead of `#-1 UNSUPPORTED DIGEST TYPE`.
- **`ENCRYPT` / `DECRYPT`** (`CryptoFunctions.cs:21-22, 118-181` vs `funcrypt.c:249-365`) — **Medium**: Does not drop non-ASCII-32..126 chars from text/key and does not preserve ANSI markup.
- **`URLDECODE`** (`CryptoFunctions.cs:16, 46-47` vs `funstr.c:2430-2451`) — **Medium**: Converts `+` to space and does not replace non-printable chars with `'?'`.
- **`FORMDECODE`** (`CryptoFunctions.cs:17, 49-75` vs `funstr.c:2453-2519`) — **Medium**: Deduplicates keys in list mode, converts `+` to space, splits on `=` before unescaping, and skips non-printable sanitization.
- **`HMAC` / `DECODE64` / `SQLESCAPE`** (`CryptoFunctions.cs:30-41, 97-116, 186-196`) — **Low**: Minor error-message differences, missing non-printable `'?'` sanitization in `DECODE64`, and missing SQL permission/enabled check in `SQLESCAPE`.

### 5. `RegexFunctions.cs`
- **`REGMATCH` / `REGMATCHI`** (`RegexFunctions.cs:16-17, 36-79` vs `funlist.c:2838-2970`) — **High**: Expects variadic register args instead of splitting space-separated `args[2]`; missing `pos:reg`, `name:reg`, and `-`; fails to clear registers on no-match; returns `"0"` instead of `#-1 REGEXP ERROR: ...` on invalid regex.
- **`REGEDIT` / `REGEDITI` / `REGEDITALL` / `REGEDITALLI`** (`RegexFunctions.cs:18-21, 81-118` vs `funlist.c:2582-2836`) — **High**: Registered without `noParse: true`, so replacement expressions are not evaluated per match with `$0`/`$1`/`$<name>` context.
- **`REGREP` / `REGREPI`** (`RegexFunctions.cs:27-28, 189-230` vs `fundb.c:2342-2372`) — **Medium**: Empty pattern returns `""` instead of `#-1 INVALID GREP PATTERN`; missing attribute visibility/examine checks.
- **`REGRAB*` / `REGLMATCH*`** (`RegexFunctions.cs:23-26, 29-32, 120-187` vs `funlist.c:2975-3056`) — **Low**: Return `""` instead of `#-1 REGEXP ERROR: ...` on invalid regex.

### 6. `StringFunctions.cs`
- **Cross-Cutting ANSI/Markup Awareness** — **High**: String length, slicing, padding, and wrapping operate on raw `string` characters instead of markup-aware `ansi_string` (`\x1B[...m` sequences are counted and split).
- **`MID`** (`StringFunctions.cs:98-110` vs `funstr.c:266-295`) — **High**: Returns `""` on non-integers or `len <= 0` and clamps `start < 0` to `0` (original returns `e_ints`, `e_range` for `pos < 0`, and extracts backwards for `len < 0`).
- **`COMP`** (`StringFunctions.cs:167-174` vs `funstr.c:451-507`) — **High**: Ignores 3rd `type` arg (`A`, `I`, `N`, `F`, `D`).
- **`LJUST` / `RJUST` / `CENTER`** (`StringFunctions.cs:193-226` vs `funstr.c:911-1120`) — **High**: Always truncate when `len >= width` (instead of requiring 4th truthy arg in `LJUST`/`RJUST`, never truncating in `CENTER`), ignore multi-char fill strings and `CENTER` 4th arg, and don't return `e_uint` on invalid width.
- **`BEFORE` / `AFTER`** (`StringFunctions.cs:228-248` vs `funlist.c:1882-1925`) — **High**: Case-insensitive (original is case-sensitive) and don't default empty `args[1]` to `" "`.
- **`LPOS`** (`StringFunctions.cs:250-259` vs `funstr.c:522-538`) — **High**: Returns a single 1-based case-insensitive substring index instead of space-separated 0-based indices of `args[1][0]`.
- **`ART`** (`StringFunctions.cs:292-300` vs `funstr.c:168-182`) — **High**: Returns `"a " + word` / `"an " + word` instead of just `"a"` / `"an"`.
- **`SUBJ` / `POSS` / `OBJ` / `APOSS`** (`StringFunctions.cs:302-363` vs `funstr.c:185-234`) — **High**: Treat `args[0]` as raw gender string instead of looking up a DB object (`match_thing` -> `#-1 NO MATCH`) and reading its `SEX` attribute.
- **`TABLE`** (`StringFunctions.cs:493-520` vs `funlist.c:2440-2578`) — **High**: Requires 2+ args (default width is 10), lacks `<`/`>`/`-` alignment prefixes, treats `args[4]` (`osep`) as a padding char, and omits column separator.
- **`WRAP`** (`StringFunctions.cs:522-562` vs `funstr.c:1627-1704`) — **High**: Treats `args[2]`/`args[3]` as indents instead of `<width1st>`/`<linesep>`, collapses spaces/newlines, and doesn't hyphenate long words.
- **`SECURE`** (`StringFunctions.cs:571-584` vs `funstr.c:1223-1236`) — **High**: Escapes with `\` instead of replacing `escaped_chars` (`% [ ] { } ( ) , ; $ ^ \`) with spaces.
- **`ESCAPE`** (`StringFunctions.cs:586-599` vs `funstr.c:1239-1251`) — **Medium**: Missing leading `\` and misses `$`, `^`, `\`.
- **`EDIT`** (`StringFunctions.cs:176-191` vs `funstr.c:1518-1559`) — **Medium**: Treats `"$"` (append), `"^"` (prepend), and empty `search` literally.
- **`REPEAT`** (`StringFunctions.cs:154-165` vs `funstr.c:838-893`) — **Medium**: Returns `""` instead of `e_int` / `#-1 ARGUMENT MUST BE NON-NEGATIVE INTEGER` and caps at `1000`.
- **`CHR` / `ORD`** (`StringFunctions.cs:272-283` vs `funstr.c:1431-1464`) — **Medium**: `CHR` allows >255 and unprintable chars; `ORD` allows multi-char strings and unprintable chars.
- **`SPELLNUM` / `ORDINAL`** (`StringFunctions.cs:384-423, 992-1025` vs `funmath.c:1609-1750`) — **Medium**: `SPELLNUM` lacks decimals/errors; both stop at billions instead of trillions.
- **`ALIGN` / `LALIGN`** (`StringFunctions.cs:1044-1367` vs `funstr.c:1967-2170`) — **Medium**: Discards `(ansi)` color specs and rejects `0`-width columns.
- **`SPACE` / `ALPHAMAX` / `ALPHAMIN` / `STRIPACCENTS`** — **Low**: Minor error/locale/2nd-arg differences.

### 7. `ListFunctions.cs`
- **Delimiter Validation & `REST`** (`ListFunctions.cs:70-95` vs `funlist.c:1238-1264`) — **High**: Most list functions don't reject multi-char delimiters (`#-1 SEPARATOR MUST BE ONE CHARACTER`); `REST` collapses multiple spaces.
- **`MEMBER`** (`ListFunctions.cs:174-189` vs `funlist.c:1852-1879`) — **High**: Case-insensitive instead of case-sensitive; doesn't reject `args[1]` containing `sep`.
- **`ITEMS`** (`ListFunctions.cs:224-229` vs `funlist.c:1631-1651`) — **High**: Uses `SplitList` instead of counting `delim + 1` (returns `"0"` for `""` instead of `"1"`, and collapses spaces).
- **`REMOVE`** (`ListFunctions.cs:316-329` vs `funlist.c:1585-1628`) — **High**: Treats `args[1]` as 1 word (instead of a word list) and compares case-insensitively.
- **`UNIQUE`** (`ListFunctions.cs:340-356` vs `funlist.c:941-994`) — **High**: Signature is `unique(<list>[, <sort_type>[, <delim>[, <osep>]]])` (adjacent dedup, case-sensitive default); C# puts `delim` in `args[1]` and does global case-insensitive dedup.
- **`SORT`** (`ListFunctions.cs:372-394` vs `funlist.c:688-713`) — **High**: Missing auto-detection, `a` (case-sensitive), `f` (float), `d` (dbref), and 4th arg `osep`.
- **`SETUNION` / `SETINTER` / `SETDIFF`** (`ListFunctions.cs:415-470` vs `funlist.c:815-939`) — **High**: Don't sort output, ignore `sort_type` and `osep`, and always compare case-insensitively.
- **`LNUM`** (`ListFunctions.cs:472-512` vs `funlist.c:999-1106`) — **High**: Swapped `args[2]` (`osep`) and `args[3]` (`step`), `int`-only, and returns `""` on `lnum(-1)`/errors.
- **`FILTER`** (`ListFunctions.cs:618-638` vs `funlist.c:580-640`) — **High**: Uses truthy check instead of `== "1"` and drops extra args `args[4..]` (`%1..%9`).
- **`SPLICE`** (`ListFunctions.cs:750-766` vs `funlist.c:1973-2023`) — **High**: Splices entire `list2` into first match instead of element-by-element replacement across equal-length lists.
- **`STEP`** (`ListFunctions.cs:768-784` vs `funlist.c:2216-2293`) — **High**: Missing `<attr>` ufun (`step(<attr>, <list>, <step>[, <delim>[, <osep>]])`).
- **`MUNGE`** (`ListFunctions.cs:786-803` vs `funlist.c:227-332`) — **High**: Calls `<attr>` per element instead of once on the entire `list1` to reorder `list2`.
- **`MATCHALL` / `GRABALL` / `REVWORDS` / `SHUFFLE` / `SORTBY`** (`ListFunctions.cs:129-145, 164-172, 331-338, 358-370, 396-413`) — **Medium**: Missing optional `osep` output-separator arg.
- **`EXTRACT` / `INDEX` / `ELEMENTS`** (`ListFunctions.cs:191-249`) — **Medium**: Missing negative indices/counts, 1-2 arg `EXTRACT`, and space-preserving `INDEX`.
- **`ITER` / `PARSE` / `IBREAK` / `ITEXT` / `INUM` / `INAME`** (`ListFunctions.cs:514-591`) — **Medium**: `ITER` defaults `outsep` to `inDelim` instead of `" "`; `IBREAK` ignores `<level>`; `ITEXT`/`INUM` reject `"l"`; `INAME` differs from `fundb.c:1868`.
- **`MAP` / `FOLD` / `FOREACH` / `RANDEXTRACT`** (`ListFunctions.cs:600-616, 672-733, 973-1022`) — **Medium**: `MAP` omits `%1` (1-based index); `FOLD` omits `%2` (0-based count) and skips ufun on 1-element list with no base; `FOREACH` allows multi-char markers and resets `%1`; `RANDEXTRACT` ignores invalid mode error.

### 8. `JsonFunctions.cs`
- **`JSON`** (`JsonFunctions.cs:51-85` vs `funjson.c:546-683`) — **High**: Doesn't strip ANSI, accepts `"bool"`, uses `IsTruthy` for boolean, reformats numbers, wraps invalid JSON strings in `array`/`object` instead of returning `#-1 INVALID VALUE`, and drops odd trailing keys.
- **`JSON_QUERY`** (`JsonFunctions.cs:136-178` vs `funjson.c:78-274`) — **High**: Missing 1-arg default (`type`), missing `"extract"`, unquotes/coerces `get` results instead of returning JSON, splits paths on `.`, and skips error returns on primitive traversal / `unescape` non-string.
- **`JSON_MOD`** (`JsonFunctions.cs:180-237` vs `funjson.c:276-409`) — **High**: Missing `"patch"` and `"sort"`, accepts non-JSON `args[3]`, lacks JSONPath `[0]`/`$[#]` syntax, and errors on nonexistent `replace` paths.
- **`JSON_MAP`** (`JsonFunctions.cs:239-266` vs `funjson.c:411-544`) — **High**: Passes `%0=value, %1=key` instead of `%0=type, %1=value, %2=key, %3..%9=extra args`, and skips scalar/null roots.
- **`WSJSON`** (`JsonFunctions.cs:268-269` vs `websock.c:648-681`) — **High**: Stub returning `args[0]` verbatim.
- **`ISJSON`** (`JsonFunctions.cs:22-34`) — **Low**: Returns `"0"` on 0 args.

### 9. `TextFormatFunctions.cs`
- **`ANSI`** (`TextFormatFunctions.cs:29-69` vs `markup.c:199-247`) — **High**: Only supports legacy 16-color single-char codes; missing 256-color (`xtermN`), hex/RGB, named colors, `/bg`, `+`, invalid-code error, and empty-text short-circuit.
- **`COLORS`** (`TextFormatFunctions.cs:71-77` vs `markup.c:260-360+`) — **High**: Hardcoded 8-color stub; missing full color table, pattern matching, and 2-arg format conversion (`hex`, `16color`, `256color`, `rgb`, etc.).
- **`RENDER`** (`TextFormatFunctions.cs:129-140` vs `funstr.c:2380-2414`) — **High**: Missing `noaccents`, `markup`, multi-flag space-separated `args[1]`, `Can_Nspemit` check for `ansi`, and `#-1 INVALID SECOND ARGUMENT` (accepts non-standard `"pueblo"`).
- **`HTML`** (`TextFormatFunctions.cs:142-148` vs `markup.c:473`) — **High**: Returns `args[0]` raw instead of wrapping in internal HTML tag markers.
- **`TAGWRAP`** (`TextFormatFunctions.cs:162-169` vs `markup.c:505-515`) — **High**: Swapped arg order for 3-arg calls (`tagwrap(<tag>, <attrs>, <text>)` in original).
- **`TAG` / `ENDTAG` / `TAGWRAP` permissions** (`TextFormatFunctions.cs:150-169`) — **Medium**: Missing `Can_Send_OOB` / allowed-tag / attribute validation.
- **`PUEBLO`** (`TextFormatFunctions.cs:171` vs `bsd.c:6941-6951`) — **High**: Hardcoded stub returning `"0"`.
- **`OOB`** (`TextFormatFunctions.cs:173-191` vs `bsd.c:3165-3221`) — **High**: Only takes 1 target, skips JSON validation on `args[2]`, skips `Can_Send_OOB` check, and returns `""` instead of sent count.
- **`WSHTML`** (`TextFormatFunctions.cs:193-203` vs `websock.c:648-687`) — **High**: Treats `args[1]` as target dbref instead of fallback text and returns raw `args[0]`.
- **`ACCENT`** (`TextFormatFunctions.cs:79-114` vs `funstr.c:1466-1473`) — **Medium**: Doesn't require equal lengths (`#-1 STRING LENGTHS MUST BE EQUAL`) and misses several Nordic/punctuation mappings.
- **`STRIPANSI`** (`TextFormatFunctions.cs:15` vs `markup.c:169-174`) — **Medium**: Only strips ANSI escapes, not HTML/WS markup tags.
- **`ACCNAME`** (`TextFormatFunctions.cs:116-127`) — **Low**: Returns `#-1 NO MATCH` instead of `#-1 I CAN'T SEE THAT HERE`.

### 10. `DbFunctions.cs`
- **Permission & Visibility Checks (Cross-Cutting)** — **High**: `LOC`, `CON`, `EXITS`, `LEXITS`, `LCON`, `LPLAYERS`, `LTHINGS`, `PARENT`, `LPARENT`, `ZONE`, `HOME`, `WHERE`, `ROOM`, `RLOC`, `CONTROLS`, `NEARBY`, `VISIBLE`, `EVAL`, `OBJEVAL`, `GET_EVAL`, `HASFLAG`/`FLAGS`/`LFLAGS`/`POWERS`, `ENTRANCES`, `LOCATE`, `HIDDEN`, `LSTATS`, `LSEARCH*` are missing `Can_Examine`, `Can_Locate`, `controls`, `See_All`, `CanEvalAttr`, `Can_Read_Attr`, `Search_All`, or `sees_flag` checks.
- **Object Matching vs `ParseDbref`** — **High**: `NAME`, `NUM`, `GET`, `XGET`, `GET_EVAL`, `HASATTR*`, `LOC`, `OWNER` use `ParseDbref` instead of `match_thing` (failing on object names / `*player`).
- **Wrong Signatures / Semantics**:
  - **`NAME`** (`DbFunctions.cs:225-231`) — **High**: Includes `;aliases` on exits and ignores 2-arg rename.
  - **`NAMELIST`** (`DbFunctions.cs:804-858`) — **High**: Inserts `ufun` return value into output list instead of `#-1`, lacks `visible_short_page` and `#-2`.
  - **`OWNER`** (`DbFunctions.cs:315-321`) — **High**: Missing `obj/attr` and 2-3 arg `@chown` side-effect.
  - **`U` / `UFUN` / `ULOCAL` / `ULAMBDA`** (`DbFunctions.cs:331-366, 1971-1982, 2033-2064`) — **High**: Require `/` (breaking `u(attr, ...)`), skip `CanRead`/`CanEvalAttr`, and `ULAMBDA` is wrongly `noParse: true`.
  - **`DEFAULT` / `EDEFAULT` / `OBJEVAL`** (`DbFunctions.cs:673-715, 1984-2031`) — **High**: Missing `noParse: true`; `DEFAULT` limited to 2 args; `EDEFAULT` doesn't evaluate found attr; `OBJEVAL` has no `controls()` check.
  - **`V`** (`DbFunctions.cs:717-725`) — **High**: Only looks up attributes; doesn't handle `0-9`, stack indices, `#`, `@`, `!`, `n`, `l`, `c`.
  - **`LATTR*` / `NATTR*` / `XATTR*`** (`DbFunctions.cs:743-779`) — **High**: Wrong arg positions (`obj/pattern` vs separate arg, missing `osep`).
  - **`TESTLOCK`** (`DbFunctions.cs:791-802`) — **High**: Tests a named lock on target (`elock`) instead of parsing `args[0]` as a boolexp.
  - **`WHERE`** (`DbFunctions.cs:574-591`) — **High**: Walks to outermost room (like `ROOM`) instead of immediate location / exit `Source`.
  - **`RNUM`** (`DbFunctions.cs:537-542`) — **High**: Takes 1 arg and returns int instead of `rnum(place, name)` dbref.
  - **`ENTRANCES`** (`DbFunctions.cs:1353-1383`) — **High**: Checks `Location` (contents) instead of linked destinations/homes/droptos.
  - **`PLAYER`** (`DbFunctions.cs:1509-1517`) — **High**: Resolves player name/dbref instead of `player(port)`.
  - **`CMDS`** (`DbFunctions.cs:1962-1969`) — **High**: Lists all commands (0 args) instead of `cmds(port)` connection command count.
  - **`ZFUN` / `PFUN`** (`DbFunctions.cs:2074-2098`) — **High**: `ZFUN` returns `""` instead of `#-1 INVALID ZONE`; `PFUN` runs as `parent` instead of `ctx.Executor` and exposes `Private`/`Internal` attrs.
- **Medium/Low in `DbFunctions.cs`**: `NAMEFORMAT` (vs `INAME`), `NAMEGRAB*`, `FUNCTIONS`/`LIST`, `HASATTR*` (`"0"` vs `#-1`), `RLOC` (`levels == 0`, cap 20), `HOME` (exit/room), `HASTYPE`, `MONEY`, `DOING`/`HEIGHT`/`WIDTH` (attrs vs descriptor), `ANDFLAGS`/`ORFLAGS`/etc. (unknown flag error), `LOCATE` switches, `LSEARCH*` (`##` via `PE_REGS`), `VALID`, `ISDBREF` (`#dbref:ctime`), `FULLNAME`, `LOCKS`, `GREP*`, `ISINT`/`ISNUM`, `VERSION`/`NUMVERSION`.

### 11. `Section9And18Functions.cs`
- **Side-Effect Gate & `@command` Checks** (`Section9And18Functions.cs:53-324`) — **High**: None check `FUNCTION_SIDE_EFFECTS`, `FN_NOSIDEFX`, or `@command` permissions (`CREATE`, `OPEN`, `DIG`, `CLONE`, `TEL`, `EMIT`, `PEMIT`, `REMIT`, `OEMIT`, `LEMIT`, `MESSAGE` have zero permission checks).
- **`MESSAGE`** (`Section9And18Functions.cs:115-133`) — **High**: Passes `args[2]` (default message) as `%0`, shifting `args[3..]` to `%1..`, and lacks 14th-arg switches.
- **`CREATE` / `OPEN` / `DIG` / `CLONE` / `TEL`** (`Section9And18Functions.cs:228-254, 270-324`) — **High**: Ignore `cost`, `newdbref`, `preserve`, `silent`, `inside`; no money/quota/control/lock checks; `CLONE` doesn't copy locks, parent, zone, powers, or home.
- **`PIDINFO`** (`Section9And18Functions.cs:341-366` vs `cque.c:1738`) — **High**: Wrong default field (`"executor"` vs `"queue player time object attribute command"`), missing fields, returns `0` for `time`, no `LookQueue`/`controls` check.
- **`GETPIDS` / `LPIDS`** (`Section9And18Functions.cs:368-392` vs `cque.c:1825`) — **High**: `GETPIDS` filters by `Executor` instead of semaphore `obj/attr`; `LPIDS` ignores `type` and `"all"` and lacks permission check.
- **`PEMIT` / `OEMIT` / `SET` / `ATTRIB_SET` / `LINK` / `SPEAK` / `SCAN`** — **Medium**: Missing port lists (`PEMIT`), `room/obj` (`OEMIT`), `set(obj/attr, flag)`, `attrib_set(obj/attr)` clear, `LINK` `preserve`/`LINK_TO`/bool return, `SPEAK` `&Name`/args 4-7/`; text`, `SCAN` `zone`/`globals`/`types`/`NO_COMMAND`.

### 12. `ClientFunctions.cs`
- **`CEMIT`** (`ClientFunctions.cs:124-142` vs `extchat.c:3445`, `function.c:393`) — **High**: Collides with PennMUSH's built-in `cemit(<channel>, <message>[, <noisy>])` channel-emit function (implemented here as an OOB `CEMIT(player, type, json)` client function).

### 13. `ChannelFunctions.cs`
- **`CBUFFER`** (`ChannelFunctions.cs:14, 34-43` vs `extchat.c:2311-2345`) — **High**: Aliased to `CRECALL` instead of returning recall buffer capacity (`0` if none).
- **`CRECALL`** (`ChannelFunctions.cs:24, 34-43` vs `extchat.c:3461-3576`) — **High**: Missing timestrings, `0` = all, `<start>`, `<delim>` (default `' '`, C# hardcodes `"\n"`), `<showstamp>`, default `10` (C# uses `20`), and permission/`#-1 NO RECALL BUFFER` checks.
- **`CFLAGS` / `CLFLAGS`** (`ChannelFunctions.cs:17-18, 63-87` vs `extchat.c:2255-2308`) — **High**: Ignore 2nd `<object>` arg (per-user `GQH` flags), skip `Chan_Can_See`, and only map 5 channel flags.
- **`CLOCK`** (`ChannelFunctions.cs:20, 107-117` vs `extchat.c:3377-3442`) — **High**: Read-only (ignores 2nd `<newlock>` arg), lacks lock-type validation and `Chan_Can_Decomp`.
- **`CSTATUS`** (`ChannelFunctions.cs:25, 141-151` vs `extchat.c:2463-2511`) — **High**: Returns `"Gagged"` instead of `"Gag"`, ignores connected/hidden status and permissions.
- **`CBUFFERADD` / `CHANNELS` / `CTITLE` / `CWHO` / `CDESC` / `CMSGS` / `CUSERS`** — **Medium/Low**: Missing `spoof` arg (`CBUFFERADD`), 1-arg delimiter & object name matching (`CHANNELS`), `skip_gagged`/`"off"`/`Priv_Who` (`CWHO`), and `Chan_Can_See` / `Can_Examine` permission checks.

### 14. `MailFunctions.cs`
- **`SENT` / `RECV`** (`MailFunctions.cs:28-29, 192-213` vs `bsd.c:6918-6937`) — **High**: Implemented as mail message counts instead of descriptor **network bytes** sent/received.
- **`MAILSTATS` / `MAILDSTATS` / `MAILFSTATS`** (`MailFunctions.cs:23-25, 147-176` vs `extmail.c:2204-2355`) — **High**: `MAILFSTATS` is wrongly aliased to `FOLDERSTATS` instead of 8-field full stats; `MAILSTATS` returns 1 number instead of 2 (`sent rec`); `MAILDSTATS` returns 3 numbers instead of 6; all lack `controls()` and wizard `""` all-DB mode.
- **`MAIL`** (`MailFunctions.cs:16, 61-83` vs `extmail.c:2104-2146`) — **High**: 0-arg `mail()` returns 3 numbers instead of 1 total count; lacks `controls()` checks.
- **`MAILLIST`** (`MailFunctions.cs:17, 85-92` vs `extmail.c:807-851`) — **High**: Ignores 2nd `<msglist>` arg, message-list filters/ranges, and `controls()`.
- **`FOLDERSTATS` / `MAILFROM` / `MAILSUBJECT` / `MAILTIME` / `MAILSTATUS` / `MAILSEND` / `MALIAS`** — **Medium**: Missing `<player>` arg in `FOLDERSTATS`, `controls()` checks, `+malias`/forwarding/perm checks in `MAILSEND`, and delimiter/perm checks in `MALIAS`.

### 15. `ConnectionAndZoneFunctions.cs`
- **`LPORTS`** (`ConnectionAndZoneFunctions.cs:45, 155-162` vs `bsd.c:7067-7122`) — **High**: Implemented as `<player>` port+type list instead of listing all MUSH descriptors (`lports([<viewer>[, <status>]])`).
- **`MWHO` / `MWHOID` / `NMWHO` / `XMWHO` / `XMWHOID`** (`ConnectionAndZoneFunctions.cs:53-57, 174-183` vs `bsd.c:6435, 6495, 6527`) — **High**: Filter by caller's room instead of returning **Mortal WHO** (all non-hidden connected players).
- **`HOST` / `HOSTNAME` / `IPADDR`** (`ConnectionAndZoneFunctions.cs:40-42, 142-147` vs `bsd.c:6885-6904`) — **High**: Missing `d->player == executor || See_All(executor)` check.
- **`LWHO*` / `ZWHO` / `ZMWHO` / `TERMINFO` / `PORTS` / `CONN` / `IDLE*` / `ZEMIT` / `NS*`** — **Medium**: Missing `<viewer>`/`<status>` args, multi-descriptor entries (`.Distinct()`), `Zone_Lock` checks, full `TERMINFO` capability flags + perm check, `Priv_Who` checks, `Can_Nspemit`, and `NS*` variants bypassing `Spoofable: false`.

### 16. `LateSubsystemFunctions.cs`
- **`BENCHMARK`** (`LateSubsystemFunctions.cs:27, 109-126` vs `funmisc.c:1482-1564`) — **High**: Returns bare total microseconds instead of `"Average: ... Min: ... Max: ..."` and ignores `args[2]` (`<sendto>`).
- **`QUOTA`** (`LateSubsystemFunctions.cs:16, 36-44` vs `wiz.c:1864-1897`) — **High**: Returns raw `RQUOTA` (default `"100"`) without adding owned object count, lacks `NoQuota` (`"99999"`), player check, and permission check.
- **`ADDRLOG` / `CONNLOG` / `CONNRECORD`** (`LateSubsystemFunctions.cs:23-25, 89-95`) — **High (Stubbed)**: Ignore all args and return `""` for wizards.
- **`SQL` / `MAPSQL`** (`LateSubsystemFunctions.cs:21-22`) — **High (Stubbed)**: Hardcoded `#-1 SQL IS NOT ENABLED`.
- **`BEEP` / `SUGGEST` / `TEXTFILE` / `TEXTENTRIES` / `TEXTSEARCH` / `FOLLOWERS` / `FOLLOWING` / `CONFIG` / `CHECKPASS` / `ROOT`** — **Medium/Low**: Missing Royalty access & out-of-range errors (`BEEP`), `<delim>`/`<limit>` (`SUGGEST`), file/admin checks + wildcard topic listing (`TEXT*`), `controls` + noparent (`FOLLOWERS`/`FOLLOWING`), full config table + perms (`CONFIG`), player check (`CHECKPASS`), and `IMAGINARY NUMBER` (`ROOT`).
