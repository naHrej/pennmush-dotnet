# Port order for the built-in softcode functions

Original PennMUSH lists its built-in functions in `src/function.c` as `flist`. That table has 528 identifier names. `@@` is also in the table and is not an identifier. `ansigen` and `pe_regs_dump` are compiled only into debug builds, so they are not part of this port.

## Backwards compatibility

Softcode written for original PennMUSH has to run here as it was written. Every original command and every original function has a place in this list. That place is finished when the command or function itself is in the server and behaves as it does in PennMUSH. This server may also carry extra commands and functions. An extra path leaves the original row unfinished.

Aspace is the exception. The space engine is being redesigned in pennmush-space. This list is base PennMUSH.


`PennMUSH.Core/MUSHcode/FunctionRegistry.cs` and the `PennMUSH.Core/MUSHcode/Functions/*` modules now register 387 names (192 prior baseline + 109 added in Sections 1–4 + 68 added in Section 5 + 18 added in Section 6). 351 of the 528 original `flist` identifier names are now registered, and 177 original identifier names remain in Sections 7–19.

A function belongs early when it only calls code that already runs. Later sections add one foundation and then the functions that need it. Player commands stay in `command-port-order.md`. The attribute setters, `@suggest`, `@readcache`, and the follower commands are in the server. The command rows are in the server.

## What already runs

Sections 1–4 are now implemented alongside the prior baseline:

- Math & Vectors: `abs`, `add`, `sub`, `mul`, `div`, `floordiv`, `fdiv`, `mod`, `modulo`, `modulus`, `fmod`, `remainder`, `sign`, `min`, `max`, `bound`, `inc`, `dec`, `sqrt`, `power`, `log`, `ln`, `pi`, `e`, `exp`, `round`, `floor`, `ceil`, `trunc`, `val`, `mean`, `avg`, `median`, `stddev`, `dist2d`, `dist3d`, `sin`, `cos`, `tan`, `asin`, `acos`, `atan`, `atan2`, `ctu`, `fraction`, `lmath`, `baseconv`, `rand`, `die`, `shl`, `shr`, `band`, `bor`, `bnot`, `bxor`, `bnand`, `vadd`, `vsub`, `vmul`, `vdot`, `vcross`, `vmag`, `vdim`, `vunit`, `vmax`, `vmin`.
- Strings: `after`, `before`, `pos`, `lpos`, `rpos`, `left`, `right`, `cat`, `strcat`, `capstr`, `center`, `edit`, `escape`, `extract`, `mid`, `trim`, `trimpenn`, `trimtiny`, `lcstr`, `lcstr2`, `ucstr`, `ucstr2`, `strlen`, `repeat`, `space`, `squish`, `brackets`, `align`, `lalign`, `tr`, `strinsert`, `strdelete`, `delete`, `strreplace`, `stripaccents`, `decompose`, `strmatch`, `soundex`, `soundslike`, `soundlike`, `merge`, `flip`, `reverse`, `chr`, `ord`, `spellnum`, `ordnumber`, `ordinal`, `wrap`, `ljust`, `rjust`, `revwords`, `strip`, `secure`, `comp`.
- Lists: `iter`, `parse`, `map`, `filter`, `fold`, `first`, `rest`, `last`, `elements`, `index`, `items`, `itemize`, `elist`, `lnum`, `member`, `match`, `element`, `matchall`, `grab`, `graball`, `linsert`, `insert`, `lreplace`, `replace`, `remove`, `ldelete`, `splice`, `step`, `munge`, `mix`, `setdiff`, `setinter`, `setunion`, `setsymdiff`, `sort`, `sortby`, `sortkey`, `shuffle`, `scramble`, `unique`, `words`, `wordpos`, `randword`, `pickrand`, `randextract`, `table`.
- Patterns: `regedit`, `regediti`, `regeditall`, `regeditalli`, `regsub`, `regmatch`, `regmatchi`, `regrab`, `regrabi`, `regraball`, `regraballi`, `regrep`, `regrepi`, `reglmatch`, `reglmatchi`, `reglmatchall`, `reglmatchalli`, `isregexp`.
- Logic and flow: `and`, `or`, `not`, `xor`, `nand`, `nor`, `cand`, `ncand`, `cor`, `ncor`, `eq`, `neq`, `gt`, `gte`, `lt`, `lte`, `if`, `ifelse`, `ifeq`, `cond`, `condall`, `ncond`, `ncondall`, `switch`, `switchall`, `case`, `caseall`, `reswitch`, `reswitchall`, `reswitchi`, `reswitchalli`, `firstof`, `allof`, `strfirstof`, `strallof`, `t`, `eval`, `objeval`, `default`, `edefault`, `v`, `get`, `xget`, `setq`, `setr`, `r`, `ufun`, `u`.
- Objects & Catalog: `name`, `fullname`, `nameformat`, `namelist`, `namegrab`, `namegraball`, `owner`, `type`, `flags`, `lflags`, `powers`, `hasflag`, `haspower`, `hasattr`, `hastype`, `loc`, `rloc`, `con`, `exits`, `lexits`, `home`, `where`, `nearby`, `controls`, `visible`, `parent`, `lparent`, `zone`, `inzone`, `lzone`, `num`, `rnum`, `obj`, `objid`, `testlock`, `attrcnt`, `lattr`, `nattr`, `mtime`, `functions`, `list`.
- Time: `time`, `secs`, `convsecs`, `convtime`, `timefmt`, `etimefmt`, `secs2time`, `isdaylight`, `tzoffset`.
- Other registered behavior: `cemit`, `cron_info`, `cron_list`, and the client functions `caudio`, `cedit`, `columns`, `cstyle`, `cwindow`. `async`, `bgtask`, and `parallel_map` are this server's queue helpers.


## 1. [Done] Register the other name of a function that already runs

Implemented (`flip`, `linsert`, `lreplace`, plus aliases `avg`, `val`, `exp`, `delete`, `element`, `reverse` moved to `flip`, and `parse`). Note: `hostname`, `idlesecs`, `cname`, and `speakpenn` remain deferred with their canonical functions (`host`, `idle`, `moniker`, `speak`) in Sections 9 and 12.

`flip`, `linsert`, `lreplace`

## 2. [Done] Logic, comparisons, and the function form of `@switch`

Implemented in `LogicFunctions.cs` (`cand`, `ncand`, `cor`, `ncor`, `if`, `ifelse`, `cond`, `condall`, `ncond`, `ncondall`, `switch`, `switchall`, `case`, `caseall`, `reswitch`, `reswitchall`, `reswitchi`, `reswitchalli`, `firstof`, `allof`, `strfirstof`, `strallof` using `FN_NOPARSE` lazy evaluation; plus eager `and`, `or`, `not`, `xor`, `nand`, `nor`, `eq`, `neq`, `gt`, `gte`, `lt`, `lte`).

`and`, `or`, `not`, `xor`, `nand`, `nor`, `ncor`, `ncand`, `eq`, `neq`, `gt`, `gte`, `lt`, `lte`, `ifelse`, `cond`, `condall`, `ncond`, `ncondall`, `switch`, `switchall`, `case`, `caseall`, `reswitch`, `reswitchall`, `reswitchi`, `reswitchalli`, `firstof`, `allof`, `strfirstof`, `strallof`

## 3. [Done] String, list, and math siblings

Implemented across `StringFunctions.cs`, `ListFunctions.cs`, `LogicFunctions.cs`, `DbFunctions.cs`, and `MathFunctions.cs` (including `soundlike` and `modulus` aliases, multi-argument folding on `sub`/`div`/`fdiv`/`remainder`/`band`/`bor`/`bxor`, and angle-unit support on `sin`/`cos`/`tan`/`asin`/`acos`/`atan`).

`pos`, `left`, `right`, `squish`, `brackets`, `align`, `lalign`, `lcstr2`, `ucstr2`, `tr`, `strinsert`, `strdelete`, `stripaccents`, `decompose`, `trimpenn`, `trimtiny`, `strmatch`, `strreplace`, `soundslike`, `ordinal`, `namelist`, `namegrab`, `namegraball`, `merge`, `lmath`, `setr`, `setsymdiff`, `sortkey`, `wordpos`, `randword`, `randextract`, `mix`, `elist`, `baseconv`, `floordiv`, `modulo`, `fmod`, `pi`, `e`, `atan2`, `fraction`, `rand`, `die`, `ctu`, `bnand`, `vadd`, `vcross`, `vsub`, `vmax`, `vmin`, `vmul`, `vdot`, `vmag`, `vdim`, `vunit`, `list`, `functions`

## 4. [Done] Regular-expression forms of functions that already run

Implemented in `RegexFunctions.cs` (`regedit[all][i]` with multi-pair support, `regrab[all][i]` and `reglmatch[all][i]` with `osep` support, and PennMUSH attribute-matching `regrep`/`regrepi`).

`regediti`, `regeditalli`, `regrabi`, `regraballi`, `regrepi`, `reglmatch`, `reglmatchi`, `reglmatchall`, `reglmatchalli`

## 5. [Done] Values an object already stores

Implemented `aposs`, `AttributePermissions.CanRead` (including `` ` `` branch restriction propagation and `examine` integration), and all 66 DB/lock/flag/walker/memory/stats functions in `DbFunctions.cs` (plus `cname` alias for `moniker`).

`aposs`, `money`, `moniker`, `doing`, `locks`, `llocks`, `lockflags`, `llockflags`, `lockowner`, `elock`, `atrlock`, `lock`, `hasattrval`, `hasattrp`, `hasattrpval`, `lattrp`, `nattrp`, `xattr`, `xattrp`, `andflags`, `andlflags`, `andlpowers`, `orflags`, `orlflags`, `orlpowers`, `children`, `nchildren`, `entrances`, `locate`, `pmatch`, `player`, `room`, `exit`, `next`, `nextdbref`, `alias`, `fullalias`, `lplayers`, `lthings`, `lcon`, `ncon`, `nexits`, `nplayers`, `nthings`, `lvcon`, `lvexits`, `lvplayers`, `lvthings`, `nvcon`, `nvexits`, `nvplayers`, `nvthings`, `xcon`, `xexits`, `xplayers`, `xthings`, `xvcon`, `xvexits`, `xvplayers`, `xvthings`, `findable`, `hidden`, `height`, `width`, `objmem`, `playermem`, `lstats`

## 6. [Done] Search attributes and the database

Implemented `grep`, `pgrep`, `grepi`, `wildgrep`, `wildgrepi`, `reglattr`, `reglattrp`, `regnattr`, `regnattrp`, `regxattr`, `regxattrp`, `lsearch`, `lsearchr`, `nlsearch`, `nsearch`, `lockfilter` in `DbFunctions.cs` (reusing `SearchCommandHandler.Matches` and `AttributePermissions.CanRead`), plus `filterbool` and `foreach` in `ListFunctions.cs`.

`grep`, `pgrep`, `grepi`, `wildgrep`, `wildgrepi`, `reglattr`, `reglattrp`, `regnattr`, `regnattrp`, `regxattr`, `regxattrp`, `lsearch`, `lsearchr`, `nlsearch`, `nsearch`, `lockfilter`, `filterbool`, `foreach`

## 7. Type tests and the names the server already knows

`type`, `num`, and `objid` already run. These tests only look at the shape of a string or a dbref. `version`, `numversion`, `mudname`, `mudurl`, and `cmds` read the server's own name, version, and command table.

`valid`, `isdbref`, `isint`, `isnum`, `isobjid`, `isword`, `version`, `numversion`, `mudname`, `mudurl`, `cmds`

## 8. Clock and time arithmetic

`time`, `secs`, `convsecs`, `convtime`, and `timefmt` already run. These read the same clock or do arithmetic on timestamps. `uptime` and `starttime` are the process start the `@uptime` command reports.

`convutcsecs`, `convutctime`, `csecs`, `ctime`, `etime`, `msecs`, `secscalc`, `starttime`, `stringsecs`, `timecalc`, `timestring`, `uptime`, `utctime`, `restarttime`, `restarts`

## 9. Functions that perform a command the server already has

The function evaluates its arguments and calls the matching command. `set()` calls `@set`. `wipe()` calls `@wipe`. `attrib_set()` calls the same write as `&`, `@_`, and `@` plus an attribute name. Those commands are in the server. This function is still unregistered. `speak` formats dialogue and may call a transform attribute through the existing `ufun`. `zemit` and the `ns` names call the zone broadcast and the nospoof prefix, which are already registered.

`emit`, `pemit`, `remit`, `oemit`, `lemit`, `prompt`, `message`, `set`, `wipe`, `attrib_set`, `lset`, `create`, `open`, `link`, `tel`, `dig`, `clone`, `pcreate`, `speak`

## 10. Zone broadcast and nospoof, after those commands exist

Do this with the command block that adds `@zemit` and the `@ns` emits. The function and the command share one implementation.

`zemit`, `nscemit`, `nsemit`, `nslemit`, `nsoemit`, `nspemit`, `nsprompt`, `nsremit`, `nszemit`

## 11. The evaluation frame `iter` and `ufun` still need

`iter`, `setq`, `ufun`, `eval`, and `objeval` already run. `letq` binds `%q` registers for one evaluation and then restores them. `localize` and `ulocal` do that around a default or a user function. `udefault`, `ulambda`, and `uldefault` are the other `ufun` forms. `lit` returns its argument unparsed. `null` returns nothing, and `@@` is the same function under a name that is not an identifier. `ilev`, `inum`, `itext`, and `iname` are the `iter` stack that `%i` reads. `slev` and `stext` are the `switch` stack. `ibreak` stops the loop. `get_eval` evaluates an attribute in the caller's frame. `zfun` is `ufun` on the zone object. `registers` lists the `%q` registers `listq` also reports.

`letq`, `listq`, `unsetq`, `localize`, `ulocal`, `udefault`, `ulambda`, `uldefault`, `pfun`, `lit`, `null`, `ibreak`, `ilev`, `inum`, `itext`, `iname`, `get_eval`, `zfun`, `slev`, `stext`, `registers`, `fn`

## 12. Who is connected

Login already knows the connection list, which is what the `who` command will print. These functions read that list. `host`, `ipaddr`, `ports`, and `terminfo` need the connection to remember the host and the client. `idle` is how long since the connection last sent text. `lcon` on a player is that player's connections. The `mwho` and `zwho` names are the same list filtered by location or zone.

`conn`, `idle`, `lwho`, `lwhoid`, `nwho`, `xwho`, `xwhoid`, `ports`, `lports`, `host`, `ipaddr`, `terminfo`, `mwho`, `mwhoid`, `nmwho`, `xmwho`, `xmwhoid`, `zmwho`, `zwho`

## 13. Channels

`cemit` already sends to a channel. `ChannelRegistry` already joins, titles, and lists members in memory. These functions read that registry. They do not need a second channel system. Channel membership is still not saved.

`cbuffer`, `cbufferadd`, `cdesc`, `cflags`, `channels`, `clflags`, `clock`, `cmogrifier`, `cmsgs`, `cowner`, `crecall`, `cstatus`, `ctitle`, `cusers`, `cwho`

## 14. ANSI, HTML, and accent text

Nothing in the evaluator stores color yet. `%k` is unimplemented for the same reason. `ansi`, `stripansi`, `colors`, and `accent` wait until color and accented names exist. `html`, `tag`, `endtag`, and `tagwrap` are the HTML forms a wizard can emit. `html` is wizard-only in the original.

`ansi`, `colors`, `stripansi`, `accent`, `accname`, `render`, `html`, `tag`, `endtag`, `tagwrap`, `pueblo`, `oob`, `wshtml`

## 15. JSON

There is no JSON parser in the server. Add one parser, then these functions are reads and edits of that value. `isjson` is the test.

`json`, `json_map`, `json_mod`, `json_query`, `isjson`, `wsjson`

## 16. Encoding and cryptography

These need a hash or a cipher the process does not have. `encode64`, `decode64`, `urlencode`, and `urldecode` are the encodings and can come first inside this block. `sha0` is marked deprecated in the original table. `sqlescape` is only useful once `sql()` exists, so it stays with this block rather than with the SQL functions.

`encode64`, `decode64`, `urlencode`, `urldecode`, `formdecode`, `digest`, `encrypt`, `decrypt`, `hmac`, `sha0`, `ssl`, `sqlescape`

## 17. Mail

Mail exists in memory and is not saved. The `@malias` command is the alias table. These functions read and send that mail. Do them with the mail-alias command, not before it.

`mail`, `maillist`, `mailfrom`, `mailsend`, `mailstats`, `maildstats`, `mailfstats`, `mailstatus`, `mailsubject`, `mailtime`, `malias`, `folderstats`, `sent`, `recv`

## 18. The queue, once `@ps` can see it

`@ps` is the command that lists `ActionQueue` entries. These functions are that list as softcode. `scan` reports the `$` listeners `@scan` will report.

`pidinfo`, `getpids`, `lpids`, `scan`

## 19. Wait for the same foundation as the late commands

These functions are blocked by something `command-port-order.md` already postpones. Passwords are not saved. Quota and followers are not stored. Config is parsed but not writable. SQL and HTTP are not in the process. Connection logs and the suggestion file are wizard files.

`quota`, `followers`, `following`, `config`, `checkpass`, `sql`, `mapsql`, `addrlog`, `connlog`, `connrecord`, `beep`, `benchmark`, `poll`, `suggest`, `textentries`, `textfile`, `textsearch`, `root`

## Not part of this port

`ansigen` and `pe_regs_dump` are inside debug ifdefs in the original table. A normal PennMUSH build does not have them.

## Counts

Sections 1–6 implemented 185 canonical `flist` names (plus 10 aliases). The remaining sections (7–19) name 177 functions, which is every remaining original identifier that is not yet registered and is not one of the two debug-only names. `@@` is the extra non-identifier, and it is the same implementation as `null`.

## Player-facing wording, after the sections above

Stock sentences that are not `#-1` results stay as the original text while the sections above are ported. `#-1` results already go through `MushError`. When these functions are in the server, their other stock sentences join the catalog at the end of `command-port-order.md`.
