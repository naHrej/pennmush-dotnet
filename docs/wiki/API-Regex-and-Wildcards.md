# API: Regex & Wildcards

Regular expression matching, lazy match-time replacement, list filtering, attribute scanning, and pattern validation functions extracted from `PennMUSH.Core/MUSHcode/Functions/RegexFunctions.cs` (**18** functions).

* All regex functions execute .NET `System.Text.RegularExpressions.Regex` with `DefaultRegexTimeout` = **100 ms** (`#-1 REGEX TIMEOUT` on timeout).
* For wildcard string matching (`strmatch()`), see [String & Formatting](API-String-and-Formatting); for wildcard list matching (`match()`, `grab()`), see [Lists & Iteration](API-Lists-and-Iteration); for regex branch control (`reswitch*()`), see [Logic, Control & Registers](API-Logic-Control-and-Registers); for regex/wildcard attribute lookups (`reglattr*()`, `wildgrep*()`), see [Attributes, Flags & Locks](API-Attributes-Flags-and-Locks).

---

## Matching & Capture Assignment

| Function | Signature | Notes |
|---|---|---|
| `regmatch` | `regmatch(<string>, <regexp>[, <reglist...>])` | Case-sensitive regex match; returns `1` or `0` |
| `regmatchi` | `regmatchi(<string>, <regexp>[, <reglist...>])` | Case-insensitive regex match; returns `1` or `0` |
| `isregexp` | `isregexp(<pattern>)` | Returns `1` if `<pattern>` compiles as a valid .NET regex, else `0` |

### Register Capture Rules (`regmatch()`, `regmatchi()`)

`args[2..]` are joined by space and split into capture tokens:
- `-`: Skips positional capture group `i` without writing a register.
- `<qreg>`: Captures positional group `i` (group `0` = full match, `1` = first capture group, ...) into Q-register `<qreg>` (leading `%q`/`Q` stripped).
- `<groupKey>:<qreg>`: Captures numbered group `<groupKey>` or named group `(?<name>...)` into Q-register `<qreg>`.
- All specified Q-registers are cleared to `""` **before** matching, so registers are blanked even when the match returns `0`. Invalid regex syntax returns `#-1 REGEXP ERROR`.

```text
think [regmatch(foo=42, ^([a-z]+)=([0-9]+)$, 0:full 1:k 2:v)] -> 1
think [r(k)]:[r(v)] -> foo:42
```

---

## Regex Search & Replace (`noParse`)

| Function | Signature | Notes |
|---|---|---|
| `regedit` | `regedit(<string>, <regexp1>, <repl1>[, <regexp2>, <repl2>...])` | `` `noParse` `` Case-sensitive first match per pair |
| `regediti` | `regediti(<string>, <regexp1>, <repl1>[, <regexp2>, <repl2>...])` | `` `noParse` `` Case-insensitive first match per pair |
| `regeditall` | `regeditall(<string>, <regexp1>, <repl1>[, <regexp2>, <repl2>...])` | `` `noParse` `` Case-sensitive global replace per pair |
| `regeditalli` | `regeditalli(<string>, <regexp1>, <repl1>[, <regexp2>, <repl2>...])` | `` `noParse` `` Case-insensitive global replace per pair |
| `regsub` | `regsub(<string>, <regexp1>, <repl1>[, <regexp2>, <repl2>...])` | `` `noParse` `` Alias of `regedit()` (case-sensitive single replace) |

Because all 5 functions are `noParse`, `<string>` and each `<regexp>` are evaluated first, while each `<repl>` expression is evaluated **per match** with `ctx.RegexCaptures` (`%$0`, `%$1`, ...) bound to that match (`BuildRegexCaptures`). If fewer than 3 arguments are passed, `<string>` is evaluated and returned unchanged (`""` on 0 args). Invalid regex returns `#-1 REGEXP ERROR`.

```text
think [regeditall(a1 b2, ([a-z])(\d), [ucstr(%$1)]#%$2)]
-> A#1 B#2
```

---

## Regex List & Attribute Queries

| Function | Signature | Notes |
|---|---|---|
| `regrab` | `regrab(<list>, <regexp>[, <delim>])` | Returns first matching word, or `""` |
| `regrabi` | `regrabi(<list>, <regexp>[, <delim>])` | Case-insensitive `regrab()` |
| `regraball` | `regraball(<list>, <regexp>[, <delim>[, <osep>]])` | Returns all matching words joined by `<osep>` (default `<delim>`) |
| `regraballi` | `regraballi(<list>, <regexp>[, <delim>[, <osep>]])` | Case-insensitive `regraball()` |
| `reglmatch` | `reglmatch(<list>, <regexp>[, <delim>])` | Returns **1-based** index of first matching word, or `""` if none |
| `reglmatchi` | `reglmatchi(<list>, <regexp>[, <delim>])` | Case-insensitive `reglmatch()` |
| `reglmatchall` | `reglmatchall(<list>, <regexp>[, <delim>[, <osep>]])` | Returns **1-based** indices of all matching words |
| `reglmatchalli` | `reglmatchalli(<list>, <regexp>[, <delim>[, <osep>]])` | Case-insensitive `reglmatchall()` |
| `regrep` | `regrep(<obj>, <attrPattern>, <regexp>)` | Sorted list of attr names matching wildcard `<attrPattern>` whose values match `<regexp>` |
| `regrepi` | `regrepi(<obj>, <attrPattern>, <regexp>)` | Case-insensitive `<regexp>` version of `regrep()` |

### List & Attribute Behavior Notes
- `<delim>` defaults to `" "` and `<osep>` defaults to `<delim>`; passing > 1 character for either returns `#-1 SEPARATOR MUST BE ONE CHARACTER`.
- Note that `reglmatch()` / `reglmatchi()` return `""` (empty string, not `0`) on no match, and list/attribute helpers (`regrab*`, `reglmatch*`, `regrep*`) return `""` on malformed regex (while still returning `#-1 REGEX TIMEOUT` on timeout).
- `regrep()` / `regrepi()` return `#-1 NO MATCH` if `<obj>` is invalid and `#-1 NO SUCH ATTRIBUTE` if `<attrPattern>` is empty.

---

See also: [MUSHcode Language Guide](MUSHcode-Language-Guide) · [Lists & Iteration](API-Lists-and-Iteration) · [Logic, Control & Registers](API-Logic-Control-and-Registers) · [Functions Index](Functions-Index) · [Home](Home)
