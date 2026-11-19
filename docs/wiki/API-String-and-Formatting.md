# API: String & Formatting

String indexing, searching, editing, character escapes, ANSI-aware justification/wrapping/column alignment, English prose helpers, phonetics, color introspection, accents, and HTML tag generation extracted from `PennMUSH.Core/MUSHcode/Functions/StringFunctions.cs` (**63** functions) and `PennMUSH.Core/MUSHcode/Functions/TextFormatFunctions.cs` (**11** formatting/Pueblo functions).

* Note that `game/alias.cnf` also defines `soundlike` -> `soundslike` (already registered directly in `StringFunctions.cs`).
* `oob()`/`wshtml()` in `TextFormatFunctions.cs` belong to out-of-band WebSocket UI frames; see `oob()` and `wshtml()` in [Client & MUjs UI](API-Client-and-MUjs-UI) and the color palette overview in [Colors, Markup & Presentation](Colors-Markup-and-Presentation).

> [!NOTE]
> **0-Based vs 1-Based String Offsets**: `mid()`, `lpos()`, `strinsert()`, `strdelete()`, `delete()`, and `strreplace()` use **0-based** character indices (`mid()` is ANSI-aware via `AnsiString`), whereas `pos()` and `rpos()` return **1-based** substring positions.

---

## Joining, Length, Case & Comparison

| Function | Signature | Notes |
|---|---|---|
| `cat` | `cat([<s1>...])` | Joins all arguments with single spaces (`" "`) |
| `strcat` | `strcat([<s1>...])` | Concatenates all arguments with no separator |
| `strlen` | `strlen([<str>])` | Visible character count ignoring ANSI codes (`AnsiString.VisibleLength`) |
| `ucstr` | `ucstr(<str>)` | Uppercases (`ToUpperInvariant`) |
| `ucstr2` | `ucstr2(<str>)` | Alias of `ucstr()` |
| `lcstr` | `lcstr(<str>)` | Lowercases (`ToLowerInvariant`) |
| `lcstr2` | `lcstr2(<str>)` | Alias of `lcstr()` |
| `capstr` | `capstr(<str>)` | Uppercases only the first character |
| `comp` | `comp(<a, b>[, <type>])` | Compares `<a, b>` by `A` (ASCII), `I` (nocase), `N` (int), `F` (float), `D` (`#dbref`) -> `-1`/`0`/`1` |
| `alphamax` | `alphamax(<s1>[, <s2>...])` | Lexicographically greatest argument (`OrdinalIgnoreCase`) |
| `alphamin` | `alphamin(<s1>[, <s2>...])` | Lexicographically smallest argument (`OrdinalIgnoreCase`) |
| `repeat` | `repeat(<str>, <count>)` | Repeats `<str>` `<count>` times (clamped at `1000`; `""` if `<= 0`) |
| `space` | `space(<count>)` | Emits `<count>` spaces (clamped at `2000`; `""` if `<= 0`) |
| `flip` | `flip(<str>)` | Reverses character order |
| `reverse` | `reverse(<str>)` | Alias of `flip()` |

* `comp()` defaults `<type>` to `A`, returns `#-1 INVALID COMPARE TYPE` on unknown types, `#-1 ARGUMENT MUST BE INTEGER`/`NUMBER` for `N`/`F`, and `#-1 TOO FEW ARGUMENTS` if < 2 args.

---

## Searching, Substrings & Wildcard Matching

| Function | Signature | Notes |
|---|---|---|
| `mid` | `mid(<str>, <first>, <len>)` | ANSI-aware substring at 0-based `<first>` (`<len> < 0` extracts backward ending at `<first>`) |
| `left` | `left(<str>, <len>)` | First `<len>` characters (`< 0` -> `#-1 OUT OF RANGE`) |
| `right` | `right(<str>, <len>)` | Last `<len>` characters (`< 0` -> `#-1 OUT OF RANGE`) |
| `before` | `before(<str>[, <search>])` | Text before first `<search>` (`Ordinal`, default `" "`; returns `<str>` if absent) |
| `after` | `after(<str>[, <search>])` | Text after first `<search>` (`Ordinal`, default `" "`; returns `""` if absent) |
| `pos` | `pos(<needle>, <haystack>)` | **1-based** case-sensitive (`Ordinal`) index of `<needle>`, or `#-1` |
| `rpos` | `rpos(<haystack>, <needle>)` | **1-based** case-insensitive last index of `<needle>`, or `0` |
| `lpos` | `lpos(<str>, <char>)` | Space-separated **0-based** positions matching `<char>[0]` |
| `strmatch` | `strmatch(<str>, <pattern>[, <reglist>])` | Wildcard match (`1` or `0`), optionally writing captures into `<reglist>` |
| `brackets` | `brackets(<str>)` | Counts six delimiter characters: `<[> <]> <(> <)> <{> <}>` |

* Note the argument order difference: `pos(<needle>, <haystack>)` puts the search string first, whereas `rpos(<haystack>, <needle>)` puts the searched string first.
* `strmatch()` only permits `[A-Za-z0-9_]` in `<reglist>` names (appending `#-1 REGISTER NAME INVALID` to the return value for invalid names, or `-` to skip a positional capture).

---

## Editing, Trimming & Character Escaping

| Function | Signature | Notes |
|---|---|---|
| `edit` | `edit(<str>, <s1>, <r1>[, <s2>, <r2>...])` | Multi-pair replace (`^` prepends, `$` appends, `""` inserts around every char) |
| `trim` | `trim(<str>[, <chars>[, <style>]])` | Alias of `trimpenn()` |
| `trimpenn` | `trimpenn(<str>[, <chars>[, <style>]])` | Strips `<chars>` (default `" "`) from `l`/`r`/both ends |
| `trimtiny` | `trimtiny(<str>[, <style>[, <chars>]])` | TinyMUSH argument order (`<style>` before `<chars>`) |
| `squish` | `squish(<str>[, <sep>])` | Trims and collapses repeated `<sep>` (default `' '`) to single `<sep>` |
| `strinsert` | `strinsert(<str>, <pos>, <ins>)` | Inserts `<ins>` at 0-based `<pos>` (appends if `>= Length`) |
| `strdelete` | `strdelete(<str>, <pos>, <len>)` | Deletes `<len>` chars from 0-based `<pos>` (`<len> < 0` deletes backward ending at `<pos>`) |
| `delete` | `delete(<str>, <pos>, <len>)` | Alias of `strdelete()` |
| `strreplace` | `strreplace(<str>, <pos>, <len>, <ins>)` | Replaces `<len>` chars at 0-based `<pos>` with `<ins>` (`pos`/`len < 0` -> `#-1 OUT OF RANGE`) |
| `tr` | `tr(<str>, <from>, <to>)` | Translates characters; supports ascending/descending ranges (`a-z`, `Z-A`) |
| `merge` | `merge(<s1>, <s2>, <matchChars>)` | Overwrites chars of `<s1>` in `<matchChars>` (default `" "`) with matching index from `<s2>` |
| `scramble` | `scramble(<str>)` | Randomly shuffles all characters of `<str>` |
| `chr` | `chr(<code>)` | Converts UTF-16 code (`0..65535`) to character (`""` if invalid/out of range) |
| `ord` | `ord(<str>)` | Numeric character code of `<str>[0]` (`0` if empty) |
| `strip` | `strip(<str>)` | Strips ANSI SGR sequences (and renders WebSocket frames to plain text first) |
| `stripansi` | `stripansi(<str>)` | Alias of `strip()` |
| `stripaccents` | `stripaccents(<str>)` | Folds accented Unicode characters to plain ASCII via `AccentStripper.Strip` |
| `secure` | `secure(<str>)` | Replaces `%[]{}(),;$\^` with spaces |
| `escape` | `escape(<str>)` | Escapes MUSH special characters (`%[]{}(),;$\^`) with `\` and prepends a leading `\` |
| `decompose` | `decompose(<str>)` | Converts raw text into evaluatable escapes (`%%`, `%r`, `%t`, `%b`, `\\`, `\[`, ...) |

* `tr()` requires `<from>` and `<to>` to expand to equal lengths (`#-1 STRING LENGTHS MUST BE EQUAL`) and rejects control chars `< 32` (except `\n`) or `127` (`#-1 TR CANNOT ACCEPT NONPRINTING CHARACTERS`). `merge()` also requires `<s1>` and `<s2>` to have equal length.

---

## Alignment, Padding, Columns & Wrapping

| Function | Signature | Notes |
|---|---|---|
| `ljust` | `ljust(<str>, <width>[, <fill>[, <trunc?>]])` | ANSI-aware left-align/right-pad (`trunc?` clips overflow) |
| `rjust` | `rjust(<str>, <width>[, <fill>[, <trunc?>]])` | ANSI-aware right-align/left-pad (`trunc?` clips overflow) |
| `center` | `center(<str>, <width>[, <lfill>[, <rfill>]])` | ANSI-aware centering with optional distinct left/right fill strings |
| `columns` | `columns(<list>, <numCols>[, <width>[, <delim>]])` | Fixed-width grid (`<width>` defaults to `78 / numCols`) |
| `table` | `table(<list>[, <fieldW>[, <lineW>[, <delim>[, <osep>]]]])` | ANSI-aware tabular grid (`<N`/`>N`/`-N` align prefix, default `10` / `78`) |
| `wrap` | `wrap(<str>, <width>[, <width1st>[, <lineSep>]])` | ANSI-aware word wrap (`width >= 2`; hyphenates words exceeding width) |
| `align` | `align(<spec>, <c1>, ...[, <fill>[, <colSep>[, <rowSep>]]])` | Multi-column ANSI-aware layout with coalesce/repeat/wrap flags |
| `lalign` | `lalign(<spec>, <list>[, <delim>[, <fill>[, <colSep>[, <rowSep>]]]])` | List-fed version of `align()` |

### `align()` / `lalign()` Spec Syntax
Up to **30** columns (`#-1 TOO MANY COLUMNS FOR ALIGN`) and combined width <= **8192** (`#-1 CANNOT HAVE COLUMNS THAT LARGE`):
- **Alignment prefix**: `<` (left, default), `>` (right), `-` (center), `_` (full justify), `=` (word-processor full justify) + non-negative integer width.
- **Modifiers**: `.` repeat column on every row; `` ` `` merge empty column left; `'` merge empty column right; `$` no trailing filler padding; `x` truncate each line; `X` truncate remaining cell text; `#` omit column separator after this column; `(<ansiSpec>)` wrap column output in ANSI styles.

---

## English Grammar & Phonetics

| Function | Signature | Notes |
|---|---|---|
| `art` | `art(<word>)` | `"an"` if `<word>` starts with `a/e/i/o/u` after trimming leading spaces, else `"a"` |
| `subj` | `subj(<obj>)` | Subject pronoun from `SEX` attr (`he`, `she`, `they`, `it`) |
| `poss` | `poss(<obj>)` | Possessive determiner (`his`, `her`, `their`, `its`) |
| `aposs` | `aposs(<obj>)` | Absolute possessive (`his`, `hers`, `theirs`, `its`) |
| `obj` | `obj(<obj>)` | Object pronoun (`him`, `her`, `them`, `it`) |
| `spellnum` | `spellnum(<int>)` | English words for signed integers up to `999,999,999` |
| `ordinal` | `ordinal(<int>)` | Spelled English ordinal (`1` -> `first`, `21` -> `twenty-first`, `0` -> `zeroth`) |
| `ordnumber` | `ordnumber(<int>)` | Numeric ordinal suffix (`1st`, `2nd`, `11th`, etc.; `0th` if non-int) |
| `soundex` | `soundex(<word>[, <type>])` | Phonetic hash: `"soundex"` (default 4-char code) or `"phone"` |
| `soundslike` | `soundslike(<w1>, <w2>[, <type>])` | Returns `1` if phonetic hashes match, else `0` |
| `soundlike` | `soundlike(<w1>, <w2>[, <type>])` | Alias of `soundslike()` |

* `subj`/`poss`/`aposs`/`obj` recognize `male`/`m`/`man`/`boy`, `female`/`f`/`woman`/`girl`, and `plural`/`p`/`they`, defaulting everything else to neuter (`it`/`its`), and return `#-1 TOO FEW ARGUMENTS` (0 args) or `#-1 NO MATCH` (missing object).
* `soundex`/`soundslike` accept `type` `"soundex"` or `"phone"`, returning `#-1 UNKNOWN HASH TYPE` otherwise.

---

## Colors, Accents, Rendering & Pueblo HTML (`TextFormatFunctions.cs`)

| Function | Signature | Notes |
|---|---|---|
| `ansi` | `ansi(<spec>, <text>)` | Wraps `<text>` in ANSI SGR/extended color sequences (`""` if `<text>` empty) |
| `colors` | `colors([<pattern\|spec>[, <formats>]])` | Lists color names or converts a color spec across formats |
| `accent` | `accent(<text>, <template>)` | Applies diacritics character-by-character (`#-1 STRING LENGTHS MUST BE EQUAL` on mismatch) |
| `accname` | `accname(<obj>)` | Applies `<obj>`'s `NAMEACCENT` attribute to its name (or returns plain name) |
| `render` | `render(<text>[, <flags>])` | Converts `<text>` using `ansi`, `markup`, `ascii`, `noaccents`, `html` |
| `html` | `html(<text>)` | Passes raw HTML (`Wizard` or `Pueblo_Send` power required; else `#-1 PERMISSION DENIED`) |
| `tag` | `tag(<name>[, <attr>...])` | Builds opening `<name attr1 attr2...>` tag |
| `endtag` | `endtag(<name>)` | Builds closing `</name>` tag |
| `tagwrap` | `tagwrap(<name>[, <attr>...], <inner>)` | Wraps `<inner>` in `<name ...>` ... `</name>` |
| `pueblo` | `pueblo([<player>])` | Returns `1` if target descriptor supports Pueblo/HTML, `0` otherwise (`#-1 NOT CONNECTED`) |

### Formatting Details
- **`colors()`**: With `0` args lists `NamedColorNames`; with `1` arg matches `AllColorNames` by wildcard `<pattern>`; with `2` args converts `<spec>` using output format tokens (`hex`/`x` default, `rgb`/`r`, `256color`/`xterm256`/`d`, `xterm256x`/`h`, `xterm`, `16color`/`c`, `name`, `auto`, plus `styles` flag emitting `h`/`u`/`f`/`i`). Invalid color -> `#-1 INVALID COLOR`; no color -> `#-1 COLORS REQUIRES AT LEAST ONE COLOR`.
- **`render(<text>[, <flags>])`**: Defaults `<flags>` to `"ansi"`. Flag `ansi` checks `CanSeeAll` (`#-1 PERMISSION DENIED`), while `markup` preserves ANSI without requiring `CanSeeAll`. Combining `html` with `ansi`/`markup` translates ANSI codes to HTML via `AnsiToHtmlTranslator`; otherwise `html` calls `WebUtility.HtmlEncode`. Invalid flag -> `#-1 INVALID SECOND ARGUMENT`.
- **`accent()` modifiers**: `'` acute (`áéíóúý`), `` ` `` grave, `^` circumflex, `:` umlaut/diaeresis, `~` tilde (`ãñõ`), `,` cedilla (`ç`), `o` ring (`å`), `/` slash (`ø`), `e`/`E` ligature (`æ`/`Æ`), `-` eth (`ð`), `u` inverted punct on `?`/`!` (`¿`/`¡`), `B` eszett on `s` (`ß`).

```text
think [ansi(+cyan!bold, Hello)]
think [colors(red, hex)]                  -> #ff0000
think [align(10 -10 >5, Left, Mid, Right)]
think [tagwrap(a, href="https://example.com", Click)]
```

---

See also: [Colors, Markup & Presentation](Colors-Markup-and-Presentation) · [Lists & Iteration](API-Lists-and-Iteration) · [Client & MUjs UI](API-Client-and-MUjs-UI) · [Functions Index](Functions-Index) · [Home](Home)
