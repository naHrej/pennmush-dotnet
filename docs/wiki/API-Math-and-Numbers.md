# API: Math & Numbers

Arithmetic, rounding, roots/logarithms, trigonometry, bitwise operations, statistics, base conversion, randomness, and vector algebra extracted from `PennMUSH.Core/MUSHcode/Functions/MathFunctions.cs` (**66** functions) and `LateSubsystemFunctions.cs` (`root()`, **1** function).

* Note that `game/alias.cnf` also defines `val` -> `trunc`, `mod` -> `modulo`, and `modulus` -> `modulo` (all 66 C# names are registered directly in `MathFunctions.cs` as well).
* Exact integer results within `[-9e18, 9e18]` are formatted without decimals; fractional results use `G` formatting (`InvariantCulture`).

---

## Arithmetic, Bounding & Rounding

| Function | Signature | Notes |
|---|---|---|
| `add` | `add(<n1>[, <n2>...])` | Sum (`0` on 0 args; non-numeric tokens treated as `0`) |
| `sub` | `sub(<n1>[, <n2>...])` | Left-associative difference (`n1 - n2 - ...`) |
| `mul` | `mul(<n1>[, <n2>...])` | Product (`0` if empty or any token unparseable) |
| `div` | `div(<i1>, <i2>[, <i3>...])` | Truncating integer division (`long`); zero divisor -> `#-1 DIVISION BY ZERO` |
| `floordiv` | `floordiv(<i1>, <i2>[, <i3>...])` | Floored integer division toward `-∞` (`long`) |
| `fdiv` | `fdiv(<n1>, <n2>[, <n3>...])` | Floating-point division (`double`) |
| `mod` | `mod(<i1>, <i2>[, <i3>...])` | Alias of `modulo()` (`long` Euclidean remainder matching divisor sign) |
| `modulo` | `modulo(<i1>, <i2>[, <i3>...])` | Floored integer modulo (result shares divisor sign) |
| `modulus` | `modulus(<i1>, <i2>[, <i3>...])` | Alias of `modulo()` |
| `remainder` | `remainder(<i1>, <i2>[, <i3>...])` | C# `%` integer remainder (result shares dividend sign) |
| `fmod` | `fmod(<n1>, <n2>)` | Floating-point remainder (`a % b`) |
| `abs` | `abs(<num>)` | Absolute value |
| `min` | `min(<n1>[, <n2>...])` | Minimum value (`0` if empty or no valid numbers) |
| `max` | `max(<n1>[, <n2>...])` | Maximum value (`0` if empty or no valid numbers) |
| `bound` | `bound(<val>, <min>[, <max>])` | Clamps `<val>` to `[min, max]` (or `max(val, min)` if `<max>` omitted/blank) |
| `inc` | `inc(<int\|prefixInt>)` | Increments integer or trailing integer digits on a string (`room9` -> `room10`) |
| `dec` | `dec(<int\|prefixInt>)` | Decrements integer or trailing integer digits on a string |
| `sign` | `sign(<num>)` | Returns `-1`, `0`, or `1` |
| `round` | `round(<num>, <digits>[, <pad?>])` | `AwayFromZero` rounding; truthy `<pad?>` forces trailing zeros (`F<digits>`) |
| `floor` | `floor(<num>)` | Greatest integer `<= <num>` |
| `ceil` | `ceil(<num>)` | Least integer `>= <num>` |
| `trunc` | `trunc(<str>)` | Leading signed integer digits from `<str>` (`0` if non-numeric start) |
| `val` | `val(<str>)` | Alias of `trunc()` |

### Precision & Error Notes
- `div`, `floordiv`, and `remainder` require strict integers (`#-1 ARGUMENTS MUST BE INTEGERS`) and >= 2 args (`#-1 TOO FEW ARGUMENTS`).
- `inc()` / `dec()` accept empty string as `"1"` / `"-1"` and return `#-1 ARGUMENT MUST END IN AN INTEGER` when the trimmed string has no trailing integer suffix.
- `round()` requires `digits >= 0` (`#-1 ARGUMENT MUST BE INTEGER`) and numeric 1st arg (`#-1 ARGUMENTS MUST BE NUMBERS`).

---

## Powers, Roots, Logarithms & Fractions

| Function | Signature | Notes |
|---|---|---|
| `sqrt` | `sqrt(<num>)` | Square root (`< 0` -> `#-1 SQUARE ROOT OF NEGATIVE NUMBER`) |
| `root` | `root(<x>, <n>)` | *(`LateSubsystemFunctions.cs`)* Real `n`-th root; supports odd roots of negative `x` |
| `power` | `power(<base>, <exp>)` | Exponentiation (`base ^ exp`) |
| `log` | `log(<val>[, <base\|e>])` | Logarithm (`<= 0` -> `#-1 LOG OF NONPOSITIVE NUMBER`; default base `10`) |
| `ln` | `ln(<val>)` | Natural logarithm (base `e`) |
| `pi` | `pi()` | Mathematical constant π (`Math.PI`) |
| `e` | `e([<x>])` | Returns `Math.E` (0 args) or `exp(<x>)` (1 arg) |
| `exp` | `exp([<x>])` | Alias of `e()` |
| `fraction` | `fraction(<num>[, <mixed?>])` | Continued-fraction rational approximation (denominator `<= 100000`) |

* **`root(<x>, <n>)`**: `n == 0` returns `1`; `n < 0` returns `#-1 ROOT OUT OF RANGE`; `x < 0` with even `n` returns `#-1 IMAGINARY NUMBER`. Results within `1e-12` of an integer snap to exact integer output.
* **`log()`**: Passing `<base>` = `"e"` computes `Math.Log(val)`; numeric bases `<= 1` return `#-1 BASE OUT OF RANGE`.
* **`fraction()`**: With a truthy 2nd argument (`<mixed?>`), improper fractions format as `<whole> <num>/<den>` (e.g. `1 1/2`).

---

## Trigonometry & Distance

Angles default to **radians** (`r`). Where `<unit>` is accepted, the first character (case-insensitive) selects **`r`** (radians), **`d`** (degrees), or **`g`** (gradians); any other value returns `#-1 INVALID ANGLE TYPE`.

| Function | Signature | Notes |
|---|---|---|
| `sin` | `sin(<angle>[, <unit>])` | Sine of `<angle>` |
| `cos` | `cos(<angle>[, <unit>])` | Cosine of `<angle>` |
| `tan` | `tan(<angle>[, <unit>])` | Tangent of `<angle>` |
| `asin` | `asin(<val>[, <unit>])` | Arcsine for `val` in `[-1, 1]` (`#-1 OUT OF RANGE` otherwise) |
| `acos` | `acos(<val>[, <unit>])` | Arccosine for `val` in `[-1, 1]` |
| `atan` | `atan(<val>[, <unit>])` | Arctangent in `[-π/2, π/2]` (or converted unit) |
| `atan2` | `atan2(<y>, <x>[, <unit>])` | Two-argument arctangent (`Math.Atan2(y, x)`) |
| `ctu` | `ctu(<angle>, <from>, <to>)` | Converts angle value between `r`, `d`, and `g` units |
| `dist2d` | `dist2d(<x1>, <y1>, <x2>, <y2>)` | Euclidean 2D distance |
| `dist3d` | `dist3d(<x1>, <y1>, <z1>, <x2>, <y2>, <z2>)` | Euclidean 3D distance |

---

## Bitwise Operations (64-bit Unsigned)

All bitwise functions operate on unsigned 64-bit integers (`ulong`), returning `#-1 ARGUMENTS MUST BE UNSIGNED INTEGERS` (or singular for `bnot`) on invalid/negative inputs:

| Function | Signature | Notes |
|---|---|---|
| `band` | `band(<u1>, <u2>[, <u3>...])` | Bitwise AND across >= 2 integers |
| `bor` | `bor(<u1>, <u2>[, <u3>...])` | Bitwise OR across >= 2 integers |
| `bxor` | `bxor(<u1>, <u2>[, <u3>...])` | Bitwise XOR across >= 2 integers |
| `bnand` | `bnand(<u1>, <u2>)` | Bitwise clear: `u1 & ~u2` |
| `bnot` | `bnot(<u>)` | 64-bit bitwise complement (`~u`) |
| `shl` | `shl(<u>, <bits>)` | Left shift (`0` when `<bits> < 0` or `> 63`) |
| `shr` | `shr(<u>, <bits>)` | Logical right shift (`0` when `<bits> < 0` or `> 63`) |

---

## Statistics, List Math, Base Conversion & Randomness

| Function | Signature | Notes |
|---|---|---|
| `mean` | `mean(<list1>[, <list2>...])` | Arithmetic mean across space-separated numbers |
| `avg` | `avg(<list1>[, <list2>...])` | Alias of `mean()` |
| `median` | `median(<list1>[, <list2>...])` | Median across numeric tokens (averages middle two on even count) |
| `stddev` | `stddev(<list1>[, <list2>...])` | Sample standard deviation (`N - 1` denominator; `0` if `N <= 1`) |
| `lmath` | `lmath(<op>, <list>[, <sep>])` | Applies `<op>` across `<sep>`-delimited elements of `<list>` |
| `baseconv` | `baseconv(<num>, <from>, <to>)` | Converts integer representation across bases `2`–`64` |
| `rand` | `rand([<bound> \| <low>, <high>])` | Random float in `[0, 1)`, `[0, bound)`, or inclusive `[low, high]` |
| `die` | `die(<n>, <sides>[, <showAll?>])` | Rolls `<n>` (`1..700`) `<sides>`-sided dice (`sum`, or individual rolls) |

### Details
* **`lmath(<op>, <list>[, <sep>])`**: `<sep>` defaults to `" "` (at most 1 character, else `#-1 SEPARATOR MUST BE ONE CHARACTER`). Supported `<op>` values:
  - Arithmetic/Stats: `add`/`+`, `sub`/`-`, `mul`/`*`, `div`/`/`, `floordiv`, `fdiv`, `modulo`/`mod`/`modulus`/`%`, `fmod`, `remainder`, `max`, `min`, `mean`/`avg`, `median`, `stddev`, `dist2d`, `dist3d`
  - Bitwise: `band`/`&`, `bor`/`|`, `bxor`/`^`, `bnand`
  - Boolean/Compare: `and`/`&&`, `or`/`||`, `xor`, `nand`, `nor`, `eq`/`=`/`==`, `neq`/`!=`/`<>`, `gt`/`>`, `gte`/`>=`, `lt`/`<`, `lte`/`<=`
  - Unrecognized `<op>` returns `#-1 UNKNOWN OPERATION`.
* **`baseconv(<num>, <from>, <to>)`**: Bases must be in `2..64` (`#-1 FROM BASE OUT OF RANGE` / `#-1 TO BASE OUT OF RANGE`). Bases `<= 36` use `0–9a–z` (case-insensitive input); bases `37–64` use `A–Z a–z 0–9 - _` (also accepting `+` and `/` as digits 62/63 on input). Leading `-` is preserved when both bases are `< 63`.
* **`rand()` & `die()`**: `rand(0)` returns `#-1 ARGUMENT MUST BE POSITIVE INTEGER`; negative 1-arg `rand(-n)` returns `-rand(|n|)`. `die()` rejects `n > 700` with `#-1 NUMBER OUT OF RANGE`.

```text
think [lmath(+, 10 20 30)]        -> 60
think [baseconv(ff, 16, 2)]       -> 11111111
think [die(3, 6, 1)]              -> e.g. 4 2 6
```

---

## Vector Operations

Vectors default to space separation with optional trailing `<sep>` and `<osep>` arguments (max 1 char each, else `#-1 SEPARATOR MUST BE ONE CHARACTER`):

| Function | Signature | Notes |
|---|---|---|
| `vadd` | `vadd(<v1>, <v2>[, <sep>[, <osep>]])` | Element-wise vector sum |
| `vsub` | `vsub(<v1>, <v2>[, <sep>[, <osep>]])` | Element-wise difference (`v1 - v2`) |
| `vmul` | `vmul(<v1>, <v2>[, <sep>[, <osep>]])` | Scalar * vector (if either is length 1) or Hadamard element-wise product |
| `vdot` | `vdot(<v1>, <v2>[, <sep>])` | Dot product scalar |
| `vcross` | `vcross(<v1>, <v2>[, <sep>[, <osep>]])` | 3D cross product (`#-1 VECTORS MUST BE THREE-DIMENSIONAL` otherwise) |
| `vmag` | `vmag(<v>[, <sep>])` | Euclidean magnitude (`|v|`) |
| `vdim` | `vdim(<v>[, <sep>])` | Dimension count (`0` on empty) |
| `vunit` | `vunit(<v>[, <sep>[, <osep>]])` | Unit vector `v / |v|` (returns zero vector unchanged when `|v| == 0`) |
| `vmax` | `vmax(<v1>, <v2>[, <sep>[, <osep>]])` | Element-wise maximum |
| `vmin` | `vmin(<v1>, <v2>[, <sep>[, <osep>]])` | Element-wise minimum |

---

See also: [Lists & Iteration](API-Lists-and-Iteration) · [Logic, Control & Registers](API-Logic-Control-and-Registers) · [Functions Index](Functions-Index) · [Home](Home)
