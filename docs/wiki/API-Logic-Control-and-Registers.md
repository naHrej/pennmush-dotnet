# API: Logic, Control & Registers

Boolean gates, numeric comparisons, lazy short-circuiting, branching (`switch*`, `case*`, `reswitch*`), register management (`setq`, `letq`, `localize`), switch-stack introspection, and dynamic built-in dispatch extracted from `PennMUSH.Core/MUSHcode/Functions/LogicFunctions.cs` (**50** functions, **27** of which are `noParse`).

* Truthiness across all boolean/conditional functions follows `MushBoolean.IsTrue()` (influenced by `tiny_booleans`; see [MUSHcode Language Guide](MUSHcode-Language-Guide)).
* Register and evaluation-frame functions interoperate with `%q<name>`, `%r`, `%$n`, `#$`, and iterator helpers covered in [Lists & Iteration](API-Lists-and-Iteration) and [Regex & Wildcards](API-Regex-and-Wildcards).

---

## Truthiness & Boolean Logic

| Function | Signature | Notes |
|---|---|---|
| `t` | `t([<expr>])` | Returns `1` if `<expr>` is truthy, else `0` |
| `and` | `and(<b1>[, <b2>...])` | Eager logical AND (`1` only if >= 1 arg and all truthy) |
| `or` | `or([<b1>...])` | Eager logical OR (`1` if any arg is truthy) |
| `not` | `not([<bool>])` | Logical negation (`1` if empty or falsy, `0` if truthy) |
| `xor` | `xor([<b1>...])` | Returns `1` if **exactly one** argument is truthy (even with > 2 args) |
| `nand` | `nand([<b1>...])` | Negated `and()` (`1` if empty or any arg is falsy) |
| `nor` | `nor([<b1>...])` | Negated `or()` (`1` if no arg is truthy) |
| `cand` | `cand([<expr1>...])` | `` `noParse` `` Lazy short-circuit AND (stops on first falsy arg; returns `1` on 0 args) |
| `ncand` | `ncand([<expr1>...])` | `` `noParse` `` Lazy short-circuit NAND (stops on first falsy arg; returns `0` on 0 args) |
| `cor` | `cor([<expr1>...])` | `` `noParse` `` Lazy short-circuit OR (stops on first truthy arg; returns `0` on 0 args) |
| `ncor` | `ncor([<expr1>...])` | `` `noParse` `` Lazy short-circuit NOR (stops on first truthy arg; returns `1` on 0 args) |

---

## Numeric Comparisons

| Function | Signature | Notes |
|---|---|---|
| `eq` | `eq(<n1>, <n2>[, <n3>...])` | Chained `a == b` across all adjacent pairs (`1` or `0`) |
| `neq` | `neq(<n1>, <n2>[, <n3>...])` | Returns `1` if any adjacent pair differs (`a != b`), else `0` |
| `gt` | `gt(<n1>, <n2>[, <n3>...])` | Strictly decreasing chain (`n1 > n2 > ...`) |
| `gte` | `gte(<n1>, <n2>[, <n3>...])` | Non-increasing chain (`n1 >= n2 >= ...`) |
| `lt` | `lt(<n1>, <n2>[, <n3>...])` | Strictly increasing chain (`n1 < n2 < ...`) |
| `lte` | `lte(<n1>, <n2>[, <n3>...])` | Non-decreasing chain (`n1 <= n2 <= ...`) |

All 6 numeric comparisons use `FloatEpsilon = 1e-9` tolerance for equality, accept 2 or more numbers (`#-1 COMPARISON REQUIRES TWO OR MORE NUMBERS` if < 2 args), and return `#-1 ARGUMENTS MUST BE NUMBERS` if any argument fails `double.TryParse` (`InvariantCulture`).

---

## Conditionals & Selectors

| Function | Signature | Notes |
|---|---|---|
| `if` | `if(<cond>, <true>[, <false>])` | `` `noParse` `` Alias of `ifelse()` / `cond()` |
| `ifelse` | `ifelse(<cond>, <true>[, <false>])` | `` `noParse` `` Multi-clause `cond()` evaluator; returns first matching branch |
| `cond` | `cond(<c1>, <e1>[, <c2>, <e2>...][, <def>])` | `` `noParse` `` Evaluates conditions in order; runs first truthy branch (or trailing `<def>`) |
| `condall` | `condall(<c1>, <e1>[, <c2>, <e2>...][, <def>])` | `` `noParse` `` Evaluates every truthy branch and concatenates results |
| `ncond` | `ncond(<c1>, <e1>[, <c2>, <e2>...][, <def>])` | `` `noParse` `` Inverse of `cond()` (matches first falsy condition) |
| `ncondall` | `ncondall(<c1>, <e1>[, <c2>, <e2>...][, <def>])` | `` `noParse` `` Inverse of `condall()` (runs all falsy branches) |
| `ifeq` | `ifeq(<s1>, <s2>, <true>[, <false>])` | Eager case-insensitive string equality (`OrdinalIgnoreCase`) |
| `firstof` | `firstof(<expr1>[, <expr2>...])` | `` `noParse` `` Evaluates left-to-right; returns first truthy value (or last evaluated) |
| `allof` | `allof(<expr1>[, <expr2>...], <sep>)` | `` `noParse` `` Evaluates `args[0..^2]`, joins truthy results with evaluated `<sep>` |
| `strfirstof` | `strfirstof(<expr1>[, <expr2>...])` | `` `noParse` `` Evaluates left-to-right; returns first non-empty string (or last evaluated) |
| `strallof` | `strallof(<expr1>[, <expr2>...], <sep>)` | `` `noParse` `` Evaluates `args[0..^2]`, joins non-empty results with evaluated `<sep>` |

* Notice `allof()` and `strallof()` treat the **final** argument as the separator expression (`Eval(args[^1])`) when >= 2 arguments are supplied (with 1 argument, returns that argument if truthy/non-empty, else `""`).
* `if()` / `ifelse()` / `cond()` are all registered to the same handler (`DoCond(..., findTrue: true, findAll: false)`), so `if()` and `ifelse()` also support multiple condition/result pairs.

```text
think [ifelse(0, a, 1, b, c)]   -> b
think [allof(0, foo, bar, |)]   -> foo|bar
think [cand(1, [pemit(#1, ran)], 0, [pemit(#1, skipped)])]
```

---

## Pattern & Regex Switching

| Function | Signature | Notes |
|---|---|---|
| `switch` | `switch(<target>, <pat1>, <act1>[, ...][, <def>])` | `` `noParse` `` Wildcard + `<num`/`>num` range match; first match wins |
| `switchall` | `switchall(<target>, <pat1>, <act1>[, ...][, <def>])` | `` `noParse` `` Evaluates & concatenates all matching branches |
| `case` | `case(<target>, <str1>, <act1>[, ...][, <def>])` | `` `noParse` `` Exact case-sensitive (`Ordinal`) match; first match wins (`#$` not replaced) |
| `caseall` | `caseall(<target>, <str1>, <act1>[, ...][, <def>])` | `` `noParse` `` Concatenates all exact-matching branches |
| `reswitch` | `reswitch(<target>, <re1>, <act1>[, ...][, <def>])` | `` `noParse` `` Case-sensitive `.NET Regex` switch; first match binds `%$0`–`%$9` |
| `reswitchall` | `reswitchall(<target>, <re1>, <act1>[, ...][, <def>])` | `` `noParse` `` Concatenates all matching regex branches |
| `reswitchi` | `reswitchi(<target>, <re1>, <act1>[, ...][, <def>])` | `` `noParse` `` Case-insensitive regex switch |
| `reswitchalli` | `reswitchalli(<target>, <re1>, <act1>[, ...][, <def>])` | `` `noParse` `` Case-insensitive `reswitchall()` |
| `slev` | `slev()` | Current nesting depth (`SwitchStack.Count - 1`; `-1` outside switch) |
| `stext` | `stext(<level\|l>)` | Evaluated target string at switch nesting `<level>` (`l` = outermost) |

* All 8 switch functions require at least 3 arguments (`#-1 TOO FEW ARGUMENTS`), push evaluated `<target>` onto `ctx.SwitchStack`, and treat an even total argument count as having a trailing `<default>` clause run when no pattern matches.
* `switch` / `switchall` / `reswitch*` substitute literal `#$` with `<target>` before evaluating the matched action; `switch` / `switchall` also bind wildcard `%0`–`%9` captures (`ctx.Arguments`) and accept numeric threshold patterns `<N` and `>N`. `case` / `caseall` perform exact string equality without modifying `#$` or `%0`–`%9`.
* `stext()` treats `0` as the innermost switch target (`l` = outermost); invalid integer returns `#-1 ARGUMENT MUST BE INTEGER` and invalid level returns `#-1 OUT OF RANGE`.

---

## Registers, Evaluation Frames & Dynamic Dispatch

| Function | Signature | Notes |
|---|---|---|
| `setq` | `setq(<reg1>, <val1>[, <reg2>, <val2>...])` | Sets Q-registers; returns `""` |
| `setr` | `setr(<reg1>, <val1>[, <reg2>, <val2>...])` | Sets Q-registers; returns `<val1>` |
| `r` | `r(<name>[, <type>])` | Reads register (`qregisters`, `regexp`, `switch`, `iter`, `args`/`stack`) |
| `letq` | `letq([<r1>, <v1>, ... ,]<body>)` | `` `noParse` `` Scoped Q-registers around `<body>` (requires odd arg count) |
| `localize` | `localize(<body>)` | `` `noParse` `` Runs `<body>` with cloned Q-registers, restoring outer registers afterwards |
| `listq` | `listq([<pattern>])` | Sorted space-separated non-empty Q-register names matching `<pattern>` (default `*`) |
| `registers` | `registers([<pat>[, <types>[, <osep>]]])` | Lists active prefixed keys (`Q*`, `R*`, `S*`, `T*`, `A*`) |
| `unsetq` | `unsetq([<patterns>])` | Clears Q-registers matching wildcard `<patterns>`, or all if omitted/blank |
| `lit` | `lit([<expr>])` | `` `noParse` `` Returns `<expr>` literally without evaluation |
| `null` | `null([<expr...>])` | Evaluates arguments for side effects and returns `""` |
| `@@` | `@@([<comment...>])` | `` `noParse` `` Comment function; neither evaluates arguments nor returns output |
| `fn` | `fn(<[obj/]name>[, <args...>])` | `` `noParse` `` Dynamically dispatches built-in function `<name>` |

### Register & Dispatch Details
- **Valid Q-register names** (`IsValidQregName`): `1`–`64` characters, no whitespace, control characters, `<`, `>`, or `=`. Violations return `#-1 INVALID Q-REGISTER NAME`. `setq`/`setr` require an even count >= 2 (`#-1 FUNCTION (...) EXPECTS AN EVEN NUMBER OF ARGUMENTS`).
- **`r(<name>[, <type>])`**: `<type>` defaults to `qregisters`; invalid `<type>` returns `#-1`. `switch` and `iter` take `0`-based offsets from the top of their respective stacks.
- **`registers()`**: Defaults `<pat>` = `*`, `<types>` = `"qregisters regexp switch iter args"`, `<osep>` = `" "`. Returns sorted case-insensitive keys prefixed with `Q` (`qregisters`), `R` (`regexp`), `S` (`switch`), `T` (`iter`), `A` (`args`/`stack`).
- **`fn(<[obj/]name>[, <args...>])`**: Preserves `noParse` semantics of `<name>` (`IsBuiltinNoParse(name)`) when forwarding `<args...>`. Prefixing `<obj>/` runs the built-in with executor context `<obj>` (requires `Controls` or `CanSeeAll`; otherwise `#-1 PERMISSION DENIED`, or `#-1 NO MATCH` if object missing).

```text
think [letq(x, 10, y, 20, [add(%qx, %qy)])]   -> 30
think [lit([add(1, 2)])]                      -> [add(1, 2)]
think [fn(me/add, 3, 4)]                      -> 7
```

---

See also: [MUSHcode Language Guide](MUSHcode-Language-Guide) · [Lists & Iteration](API-Lists-and-Iteration) · [Regex & Wildcards](API-Regex-and-Wildcards) · [Functions Index](Functions-Index) · [Home](Home)
