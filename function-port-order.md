# Port order for the built-in softcode functions

Original PennMUSH lists its built-in functions in `src/function.c` as `flist`. That table has 528 identifier names. `@@` is also in the table and is not an identifier. `ansigen` and `pe_regs_dump` are compiled only into debug builds, so they are not part of this port.

## Backwards compatibility

Softcode written for original PennMUSH has to run here as it was written. Every original command and every original function has a place in this list. That place is finished when the command or function itself is in the server and behaves as it does in PennMUSH. This server may also carry extra commands and functions. An extra path leaves the original row unfinished.

Aspace is the exception. The space engine is being redesigned in pennmush-space. This list is base PennMUSH.


`PennMUSH.Core/MUSHcode/FunctionRegistry.cs` registers 190 names. 164 of those are original names. The other 26 are not original table entries. `insert`, `replace`, `reverse`, and `u` are original aliases registered here before the canonical name (`linsert`, `lreplace`, `flip`, and `ufun`). The rest are this server's own functions: `async`, `attrcnt`, `bgtask`, `caudio`, `cedit`, `columns`, `cron_info`, `cron_list`, `cstyle`, `cwindow`, `exits`, `ifeq`, `inzone`, `lzone`, `mod`, `ordnumber`, `parallel_map`, `regsub`, `rpos`, `secs2time`, `strip`, and `tzoffset`. Those 26 stay. They are not gaps.

362 original names still have no registration. A function belongs early when it only calls code that already runs. Later sections add one foundation and then the functions that need it. Player commands stay in `command-port-order.md`. The attribute setters, `@suggest`, `@readcache`, and the follower commands are in the server. The next command block is `@sql`.

## What already runs

A new function can call these without a new subsystem:

- Math: `abs`, `add`, `sub`, `mul`, `div`, `fdiv`, `mod`, `remainder`, `sign`, `min`, `max`, `bound`, `inc`, `dec`, `sqrt`, `round`, `floor`, `ceil`, `trunc`, `mean`, `median`, `stddev`, `dist2d`, `dist3d`, the trig functions, `ln`, `log`, `shl`, `shr`, `band`, `bor`, `bnot`, `bxor`.
- Strings: `after`, `before`, `cat`, `capstr`, `center`, `edit`, `escape`, `extract`, `mid`, `trim`, `lcstr`, `ucstr`, `strlen`, `strcat`, `repeat`, `space`, `chr`, `ord`, `spellnum`, `wrap`, `ljust`, `rjust`, `reverse`, `revwords`, `strip`.
- Lists: `iter`, `map`, `filter`, `fold`, `first`, `rest`, `last`, `elements`, `index`, `items`, `itemize`, `lnum`, `member`, `match`, `matchall`, `grab`, `graball`, `insert`, `replace`, `remove`, `ldelete`, `splice`, `setdiff`, `setinter`, `setunion`, `sort`, `sortby`, `shuffle`, `scramble`, `unique`, `words`, `table`.
- Patterns: `regedit`, `regeditall`, `regmatch`, `regmatchi`, `regrab`, `regraball`, `regrep`, `regsub`, `isregexp`, `soundex`, `comp`.
- Logic and flow: `if`, `ifeq`, `cand`, `cor`, `t`, `eval`, `objeval`, `default`, `edefault`, `v`, `get`, `xget`, `setq`, `ufun`, `u`. `not` is in section 2.
- Objects: `name`, `fullname`, `owner`, `type`, `flags`, `lflags`, `powers`, `hasflag`, `haspower`, `hasattr`, `hastype`, `loc`, `rloc`, `con`, `exits`, `lexits`, `home`, `where`, `nearby`, `controls`, `visible`, `parent`, `lparent`, `zone`, `inzone`, `lzone`, `num`, `obj`, `objid`, `testlock`, `attrcnt`, `lattr`, `nattr`, `mtime`.
- Time: `time`, `secs`, `convsecs`, `convtime`, `timefmt`, `etimefmt`, `secs2time`, `isdaylight`, `tzoffset`.
- Other registered behavior: `cemit`, `cron_info`, `cron_list`, and the client functions `caudio`, `cedit`, `columns`, `cstyle`, `cwindow`. `async`, `bgtask`, and `parallel_map` are this server's queue helpers.


## 1. Register the other name of a function that already runs

These names are the original function, and the name already registered here is the alias. Registering the missing name is the whole change. When `e()` exists, also register `exp`. When `host()`, `idle()`, `strdelete()`, `moniker()`, `speak()`, and `match()` exist, also register `hostname`, `idlesecs`, `delete`, `cname`, `speakpenn`, and `element`. `avg` is the alias of `mean()`, which is already registered.

`flip`, `linsert`, `lreplace`

## 2. Logic, comparisons, and the function form of `@switch`

`cand`, `cor`, and `if` already run. These are the other boolean and comparison functions, plus `switch()` and `case()`, which stop at the first match the way `@select` does. The `all` forms keep going, the way `@switch` does with `/all`. `reswitch` is the same match with a regular expression. `firstof` and `allof` are the short-circuit forms. `ncand` is `cand` with a different empty-argument rule.

`and`, `or`, `not`, `xor`, `nand`, `nor`, `ncor`, `ncand`, `eq`, `neq`, `gt`, `gte`, `lt`, `lte`, `ifelse`, `cond`, `condall`, `ncond`, `ncondall`, `switch`, `switchall`, `case`, `caseall`, `reswitch`, `reswitchall`, `reswitchi`, `reswitchalli`, `firstof`, `allof`, `strfirstof`, `strallof`

## 3. String, list, and math siblings

`pos` is the plain substring search next to `lpos` and `rpos`. `left`, `right`, `squish`, `brackets`, `align`, and `tr` sit next to `mid`, `trim`, `center`, and `edit`. `lcstr2` and `ucstr2` can call the existing `lcstr` and `ucstr` until real case-folding exists. `bnand` sits next to `band`, `bor`, `bnot`, and `bxor`. `modulo` sits next to `mod`. `e` is the exponential, and `exp` is its alias. The vector functions are arithmetic on number lists. `list()` here is the function catalog (`builtin`, `local`, or `all`), the same table `@list functions` reads, and `functions()` is that list as well.

`pos`, `left`, `right`, `squish`, `brackets`, `align`, `lalign`, `lcstr2`, `ucstr2`, `tr`, `strinsert`, `strdelete`, `stripaccents`, `decompose`, `trimpenn`, `trimtiny`, `strmatch`, `strreplace`, `soundslike`, `ordinal`, `namelist`, `namegrab`, `namegraball`, `merge`, `lmath`, `setr`, `setsymdiff`, `sortkey`, `wordpos`, `randword`, `randextract`, `mix`, `elist`, `baseconv`, `floordiv`, `modulo`, `fmod`, `pi`, `e`, `atan2`, `fraction`, `rand`, `die`, `ctu`, `bnand`, `vadd`, `vcross`, `vsub`, `vmax`, `vmin`, `vmul`, `vdot`, `vmag`, `vdim`, `vunit`, `list`, `functions`

## 4. Regular-expression forms of functions that already run

`regedit`, `regmatch`, `regrab`, and `regrep` already run. These are the case-insensitive forms and the list-wide forms of the same operations.

`regediti`, `regeditalli`, `regrabi`, `regraballi`, `regrepi`, `reglmatch`, `reglmatchi`, `reglmatchall`, `reglmatchalli`

## 5. Values an object already stores

Attributes, flags, powers, locks, pennies, parents, the exit chain, and `MONIKER` are already stored. `poss` already reads gender. `aposs` is the absolute possessive from that same gender. `testlock` already evaluates a key. `lock` evaluates one stored lock. `money` reads pennies. `moniker` reads `MONIKER`. `doing` reads the `DOING` attribute. A grave accent in an attribute name is a branch separator, and a dot is one character of a single name. `get()`, `xget()`, `lattr()`, `hasattr()`, and `examine` still have to apply a branch's restrictions to the names under it. A flag that grants access stays on the attribute where it was set. The names that start with `l`, `n`, `lv`, `nv`, `x`, or `xv` list an object's contents, exits, players, or things: every match, the visible matches, or the matches the caller does not see. `children` walks parents in reverse. `entrances` is the same walk as the `@entrances` command.

`aposs`, `money`, `moniker`, `doing`, `locks`, `llocks`, `lockflags`, `llockflags`, `lockowner`, `elock`, `atrlock`, `lock`, `hasattrval`, `hasattrp`, `hasattrpval`, `lattrp`, `nattrp`, `xattr`, `xattrp`, `andflags`, `andlflags`, `andlpowers`, `orflags`, `orlflags`, `orlpowers`, `children`, `nchildren`, `entrances`, `locate`, `pmatch`, `player`, `room`, `exit`, `next`, `nextdbref`, `alias`, `fullalias`, `lplayers`, `lthings`, `lcon`, `ncon`, `nexits`, `nplayers`, `nthings`, `lvcon`, `lvexits`, `lvplayers`, `lvthings`, `nvcon`, `nvexits`, `nvplayers`, `nvthings`, `xcon`, `xexits`, `xplayers`, `xthings`, `xvcon`, `xvexits`, `xvplayers`, `xvthings`, `findable`, `hidden`, `height`, `width`, `objmem`, `playermem`, `lstats`

## 6. Search attributes and the database

`@grep` already searches one object's own attributes. `grep`, `pgrep`, and `grepi` are that search as a function, including parents. `wildgrep` and the `reglattr` family are the wildcard and regular-expression forms. `lsearch` is the database walk `@search` will use, with the criteria on the right. `lockfilter` keeps the items whose lock passes. `filterbool` is `filter` with a true-or-false test. `foreach` is `iter` that evaluates an action for each item.

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

The sections above name 362 functions. That is every original identifier that is not registered and is not one of the two debug-only names. `@@` is the extra non-identifier, and it is the same implementation as `null`.

## Player-facing wording, after the sections above

Stock sentences that are not `#-1` results stay as the original text while the sections above are ported. `#-1` results already go through `MushError`. When these functions are in the server, their other stock sentences join the catalog at the end of `command-port-order.md`.
