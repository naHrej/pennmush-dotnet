# API: Lists & Iteration

Word-list manipulation, 1-based indexing, sorting, set algebra, functional combinators, loop iteration (`iter()` / `parse()`), English list formatting, and random sampling extracted from `PennMUSH.Core/MUSHcode/Functions/ListFunctions.cs` (**53** functions).

* Note that `game/alias.cnf` also defines `parse` -> `iter` and `pickrand` -> `randword` (both aliases are additionally registered directly in `ListFunctions.cs`).
* Unless stated otherwise, `<delim>` defaults to `" "` (splitting collapses runs of spaces and trims tokens via `RemoveEmptyEntries | TrimEntries`; any non-space delimiter preserves empty items via exact `.Split(delim)`). List indices are **1-based**.

---

## Positional Access & Lookup

| Function | Signature | Notes |
|---|---|---|
| `first` | `first(<list>[, <delim>])` | Returns first element, or `""` |
| `rest` | `rest(<list>[, <delim>])` | Returns all elements after the first, joined by `<delim>` |
| `last` | `last(<list>[, <delim>])` | Returns final element, or `""` |
| `words` | `words(<list>[, <delim>])` | Count of parsed elements (`0` on empty) |
| `items` | `items(<list>, <delim>)` | Raw count of delimiter boundaries (`1 + occurrences(delim)`, `0` if `< 2` args) |
| `match` | `match(<list>, <pattern>[, <delim>])` | **1-based** index of first wildcard match (`0` if none) |
| `element` | `element(<list>, <pattern>[, <delim>])` | Alias of `match()` |
| `matchall` | `matchall(<list>, <pattern>[, <delim>])` | Space-separated **1-based** indices of all wildcard matches |
| `grab` | `grab(<list>, <pattern>[, <delim>])` | First element matching wildcard `<pattern>` (`""` if none) |
| `graball` | `graball(<list>, <pattern>[, <delim>])` | All elements matching wildcard `<pattern>`, joined by `<delim>` |
| `member` | `member(<list>, <word>[, <delim>])` | **1-based** exact case-sensitive match (`Ordinal`, `0` if none) |
| `extract` | `extract(<list>, <first>, <len>[, <delim>])` | Slice of `<len>` elements starting at 1-based `<first>` |
| `index` | `index(<list>, <delim>, <start>, <len>)` | Slice with `<delim>` passed as 2nd arg before `<start>` and `<len>` |
| `elements` | `elements(<list>, <posList>[, <delim>[, <osep>]])` | Extracts elements at 1-based space-separated positions in `<posList>` |
| `wordpos` | `wordpos(<str>, <charPos>[, <sep>])` | **1-based** word index containing **1-based** character position `<charPos>` |

* `member()` rejects `<word>` containing a 1-character `<delim>` with `#-1 CAN ONLY TEST ONE ELEMENT`.
* `wordpos()` returns `#-1` (`MushError.Nothing`) when `<charPos>` is out of range (`<= 0` or `> Length`), `#-1 ARGUMENT MUST BE INTEGER` if invalid, or `#-1 SEPARATOR MUST BE ONE CHARACTER` if `<sep>` exceeds 1 char.

---

## Mutation, Splicing & Reordering

| Function | Signature | Notes |
|---|---|---|
| `linsert` | `linsert(<list>, <pos>, <word>[, <delim>])` | Inserts `<word>` at `<pos>` (`-1` appends; supports negative indices) |
| `insert` | `insert(<list>, <pos>, <word>[, <delim>])` | Alias of `linsert()` |
| `lreplace` | `lreplace(<list>, <posList>, <word>[, <delim>[, <osep>]])` | Replaces one or more 1-based positions (supports negative indices) |
| `replace` | `replace(<list>, <posList>, <word>[, <delim>[, <osep>]])` | Alias of `lreplace()` |
| `ldelete` | `ldelete(<list>, <posList>[, <delim>[, <osep>]])` | Deletes all listed 1-based positions (supports negative indices) |
| `remove` | `remove(<list>, <words>[, <delim>])` | Removes first case-sensitive occurrence of each item in `<words>` |
| `revwords` | `revwords(<list>[, <delim>])` | Reverses element order |
| `unique` | `unique(<list>[, <sortType>[, <delim>[, <osep>]]])` | Removes adjacent duplicates (`i` case-insensitive, `n`/`f` numeric `1e-12`, or `Ordinal`) |
| `shuffle` | `shuffle(<list>[, <delim>])` | Randomly permutes elements (Fisher-Yates) |
| `splice` | `splice(<l1>, <l2>, <word>[, <delim>[, <osep>]])` | Replaces case-insensitive `<word>` matches in `<l1>` with positional item from `<l2>` |
| `itemize` | `itemize(<list>[, <delim>[, <conj>[, <punc>]]])` | Formats English list with `<conj>` (default `and`) and `<punc>` (default `,`) |
| `elist` | `elist(<list>[, <conj>[, <delim>[, <osep>[, <punc>]]]])` | Formats English list (`<conj>` 2nd arg, custom `<osep>`) |

* `linsert`/`lreplace`/`ldelete` validate that single-char delimiters do not exceed 1 char (`#-1 SEPARATOR MUST BE ONE CHARACTER`).
* `splice()` requires equal-length `<l1>` and `<l2>` (`#-1 NUMBER OF WORDS MUST BE EQUAL`).

---

## Sorting & Set Algebra

| Function | Signature | Notes |
|---|---|---|
| `sort` | `sort(<list>[, <sortType>[, <delim>[, <osep>]]])` | Sorts by `<sortType>`: auto-detected (`n`/`f`/`d`/`a`) or explicit `a`, `i`, `n`, `f`, `d` |
| `sortby` | `sortby(<[obj/]attr>, <list>[, <delim>])` | Sorts with comparator attribute receiving `%0`, `%1` -> `<0`/`0`/`>0` |
| `sortkey` | `sortkey(<[obj/]attr>, <list>[, <sortType>[, <delim>[, <osep>]]])` | Schwartzian stable sort on keys returned by `Ufun([attr, item])` |
| `setunion` | `setunion(<l1>, <l2>[, <delim>[, <sortType>[, <osep>]]])` | Sorted deduplicated union |
| `setinter` | `setinter(<l1>, <l2>[, <delim>[, <sortType>[, <osep>]]])` | Sorted deduplicated intersection |
| `setdiff` | `setdiff(<l1>, <l2>[, <delim>[, <sortType>[, <osep>]]])` | Sorted deduplicated items in `<l1>` absent from `<l2>` |
| `setsymdiff` | `setsymdiff(<l1>, <l2>[, <delim>[, <sortType\|osep>[, <osep>]]])` | Sorted case-insensitive symmetric difference |

* When `<sortType>` is omitted or blank in `sort`, `setunion`, `setinter`, and `setdiff`, `DetectSortType` picks integer (`n`), float (`f`), `#dbref` (`d`), or ASCII (`a`). In `setsymdiff()`, a 4th argument matching `a`/`i`/`n`/`f`/`d` selects sort order, while any other string acts as `<osep>`.

---

## Iteration, Loop Control & Combinators

| Function | Signature | Notes |
|---|---|---|
| `iter` | `iter(<list>, <pattern>[, <delim>[, <osep>]])` | `` `noParse` `` Evaluates `<pattern>` per element (`##` = text, `#@` = 1-based index) |
| `parse` | `parse(<list>, <pattern>[, <delim>[, <osep>]])` | `` `noParse` `` Alias of `iter()` |
| `ilev` | `ilev()` | Current `iter()` nesting depth (`-1` outside loop) |
| `inum` | `inum([<level\|l>])` | 1-based iteration index (`0` = innermost, `l` = outermost) |
| `itext` | `itext([<level\|l>])` | Current element text at iteration `<level>` (`l` = outermost) |
| `iname` | `iname([<level\|l>])` | `name()` of `itext(<level>)` |
| `ibreak` | `ibreak()` | Stops innermost `iter()` after the current iteration completes |
| `map` | `map(<[obj/]attr>, <list>[, <delim>[, <osep>]])` | Calls `<[obj/]attr>` with `%0`=item, `%1`=1-based index |
| `filter` | `filter(<[obj/]attr>, <list>[, <delim>[, <osep>[, <args...>]]])` | Keeps items where trimmed `Ufun` result equals `"1"` |
| `filterbool` | `filterbool(<[obj/]attr>, <list>[, <delim>[, <osep>[, <args...>]]])` | Keeps items where `Ufun` result is truthy (`IsTruthy`) |
| `foreach` | `foreach(<[obj/]attr>, <str>[, <start>[, <end>]])` | Invokes `Ufun([attr, char, pos0])` for each character in `[start..end]` slice |
| `fold` | `fold(<[obj/]attr>, <list>[, <init>[, <delim>]])` | Left fold (`%0`=accumulator, `%1`=item; defaults `<init>` to `first(list)`) |
| `step` | `step(<[obj/]attr>, <list>, <size>[, <delim>[, <osep>]])` | Evaluates `Ufun` in chunks of `size` (`1..10` -> `#-1 OUT OF RANGE` otherwise) |
| `munge` | `munge(<[obj/]attr>, <l1>, <l2>[, <delim>[, <osep>]])` | Passes joined `<l1>` & `<delim>` to `Ufun` to permute `<l2>` (`#-1 LISTS MUST BE OF EQUAL SIZE`) |
| `mix` | `mix(<[obj/]attr>, <l1>, <l2>[, <l3>...][, <delim>])` | Zips 2+ lists passing `%0..%<n-1>` (`args[^1]` is `<delim>` when > 3 args) |
| `lnum` | `lnum(<count>)` / `lnum(<start>, <end>[, <osep>[, <step>]])` | Numeric sequence (up to `10,000` items; 1-arg returns `0..count-1`) |

### Loop & Combinator Notes
- `iter()` / `parse()` evaluate `<list>`, `<delim>`, and `<osep>` eagerly, then substitute literal `##` and `#@` in `<pattern>` before evaluating each element with `IterStack` updated (`itext()`, `inum()`, `ilev()`, `iname()`). Out-of-range `<level>` on `inum`/`itext`/`iname` returns `#-1 OUT OF RANGE`.
- `mix()` with 3 arguments zips 2 lists using `" "`; with 4+ arguments, the **last** argument `args[^1]` is `<delim>` (max 1 char) across `args[1..^2]` padded with `""`.

```text
think [iter(a b c, #@:[ucstr(##)], , |)]
-> 1:A|2:B|3:C

think [lnum(2, 10, -, 2)]
-> 2-4-6-8-10
```

---

## Random Selection

| Function | Signature | Notes |
|---|---|---|
| `randword` | `randword(<list>[, <delim>])` | Selects one uniform random item (`""` if empty) |
| `pickrand` | `pickrand(<list>[, <delim>])` | Alias of `randword()` |
| `randextract` | `randextract(<list>[, <count>[, <delim>[, <mode>[, <osep>]]]])` | Draws `<count>` (default `1`) items by `<mode>` (`R`, `D`, `L`, `l`) |

### `randextract()` Modes
- `R` (default): Random sampling **without replacement** (up to `list.Count` items).
- `D`: Random sampling **with replacement** (duplicates allowed).
- `L`: Contiguous random slice of length `min(count, list.Count)` starting in `[0, list.Count - take]`.
- `l`: Contiguous random slice starting at any index in `[0, list.Count - 1]` (may yield fewer than `<count>` near the end).

---

See also: [Logic, Control & Registers](API-Logic-Control-and-Registers) · [Regex & Wildcards](API-Regex-and-Wildcards) · [String & Formatting](API-String-and-Formatting) · [Functions Index](Functions-Index) · [Home](Home)
