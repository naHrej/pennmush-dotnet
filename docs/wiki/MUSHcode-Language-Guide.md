# MUSHcode Language Guide

This guide documents the exact behavior of `ExpressionEvaluator`, `EvaluationContext`, `FunctionRegistry`, and `MushError` in `PennMUSH.Core/MUSHcode/`.

## Evaluation Rules & Syntax

`ExpressionEvaluator.Evaluate(string expression, EvaluationContext context)` evaluates left-to-right in a single pass with explicit nesting delimiters:

| Construct | Behavior | Notes |
|---|---|---|
| `\c` | Escapes character `c` literally | Consumes `\`, appends next char verbatim |
| `{ ... }` | Prevents evaluation of contents | Strips the outer `{}` pair and appends inner text verbatim (nested braces preserved inside) |
| `[ ... ]` | Forces evaluation inside text | Calls `EvaluateBracketContent`: invokes `fn(...)` if shaped like a function call, otherwise recursively evaluates |
| `fn(a, b)` | Function invocation (when preceding text matches a registered function name) | Arguments split on top-level `,` (respecting `\`, `()`, `[]`, `{}`) and `TrimStart()`-ed per PennMUSH spacing rules; only the first `(` per frame parses as a call unless enclosed in `[]` |
| `%<code>` | Percent-substitution | Expanded immediately (see complete table below) |
| `$<n>`, `$<name>`, `$$` | Regex capture substitution | Active when `EvaluationContext.RegexCaptures != null` (`$0–$9`, `$<name>`, `$$` → literal `$`) |

### `noParse` Functions & Argument Evaluation
- By default, all comma-separated arguments to `fn(a1, a2, ...)` are evaluated *before* calling `fn`.
- **40 built-in functions** (plus `iter()` and `parse()`, plus user functions flagged `noParse` via `@function`) are flagged `noParse: true` in `FunctionRegistry` (`PennMUSH.Core/MUSHcode/FunctionRegistry.cs`). For those functions, raw unevaluated argument strings are passed directly to the function implementation, which evaluates only selected branches on demand (e.g., `if()`, `ifelse()`, `switch()`, `cand()`, `cor()`, `localize()`, `letq()`, `iter()`, `filter()`, `map()`, `@@()`).

### Side-Effect Gating (`SideEffectsEnabled` / `NoSideFX`)
- **29 built-in functions** are marked `sideEffect: true` (e.g., `pemit()`, `remit()`, `set()`, `create()`, `link()`, `tel()`, `cemit()`).
- In `FunctionRegistry.DispatchBuiltin()`, if `!SideEffectsEnabled || context.NoSideFX`, invoking any `sideEffect: true` function immediately returns `#-1 PERMISSION DENIED` (`MushError.PermissionDenied`).

---

## Execution Safety & Budget (`EvaluationContext`)

Every recursive `Evaluate()` call checks `EvaluationContext.CheckExecutionBudget()`:

| Guard | Default Limit | Error Returned |
|---|---|---|
| Recursion depth (`RecursionDepth > MaxRecursionDepth`) | **50** (`MaxRecursionDepth = 50`) | `#-1 FUNCTION RECURSION LIMIT EXCEEDED` |
| Invocation count (`++InvocationCount > MaxInvocations`) | **250,000** (`MaxInvocations = 250000`) | `#-1 FUNCTION INVOCATION LIMIT EXCEEDED` |
| Call-cycle detection (`PushCall(dbref, attr)`) | Repeated `(object, attribute)` pair on active call stack | `#-1 RECURSION CYCLE DETECTED` |
| Cancellation token / execution deadline | When triggered / expired | `#-1 TIMEOUT (RUNAWAY LOOP DETECTED)` |

---

## Complete `%`-Substitution Table

Directly verified against `ExpressionEvaluator.cs` (lines 144–256 & 407–556):

| Code | Implemented Meaning | Case / Notes |
|---|---|---|
| `%%` | Literal `%` | — |
| `%r` / `%R` | Newline (`\n`) | — |
| `%t` / `%T` | Horizontal tab (`\t`) | — |
| `%b` / `%B` | Single space (`' '`) | — |
| `%#` | Enactor dbref (`#n`) | From `context.Enactor` |
| `%!` | Executor dbref (`#n`) | From `context.Executor` |
| `%@` | Caller dbref (`#n`) | From `context.Caller` |
| `%l` / `%L` | Enactor/context location dbref (`#n`) | From `context.Location` |
| `%0` – `%9` | Positional call arguments | Empty string if out of bounds |
| `%+` | Number of passed arguments (`%0..`) | `context.Arguments.Length` |
| `%q0`–`%qz`, `%q<name>` | Q-register lookup (case-insensitive) | Single alphanumeric char or `<name>` tag; `%Q...` behaves identically |
| `%n` / `%N` | Enactor name | `%N` capitalizes first non-ANSI character |
| `%k` / `%K` | Enactor `MONIKER` (or name if unset) | `%K` capitalizes first non-ANSI character |
| `%s` / `%S` | Subjective pronoun (`he` / `she` / `they` / `it`) | Derived from enactor `SEX` attr (`male`/`m`, `female`/`f`, `plural`/`p`, else neuter); `%S` capitalizes |
| `%o` / `%O` | Objective pronoun (`him` / `her` / `them` / `it`) | `%O` capitalizes |
| `%p` / `%P` | Possessive determiner (`his` / `her` / `their` / `its`) | `%P` capitalizes |
| `%a` / `%A` | Absolute possessive (`his` / `hers` / `theirs` / `its`) | `%A` capitalizes |
| `%:` | Enactor objid (`#dbref:creationUnixSeconds`) | Same format as `objid(%#)` |
| `%?` | `<InvocationCount> <RecursionDepth>` | Evaluation diagnostics |
| `%va`–`%vz`, `%wa`–`%wz`, `%xa`–`%xz` | Reads `VA`–`VZ`, `WA`–`WZ`, `XA`–`XZ` on executor (`%!`) | Uppercase `%V`/`%W`/`%X` capitalizes leading non-ANSI char |

Any unrecognized `%<code>` sequence is preserved literally in the output (`%` followed by the character). Note the following PennMUSH 1.8 codes are **not implemented** in `ExpressionEvaluator.cs` (use softcode alternatives like `itext()`/`ilev()` instead of `%i*`): `%~`, `%c`/`%C`, `%u`/`%U`, `%=`, `% ` (percent-space), `%i0–%i9`/`%il`, `%$0–%$9`/`%$l`.

---

## Centralized Error Catalog (`MushError`)

All softcode error strings come from `MushErrorText` (`PennMUSH.Core/MUSHcode/MushError.cs` — **91** enum entries):

<details>
<summary>Complete table of all 91 MushError strings</summary>

| Enum Member | Returned String |
|---|---|
| `NoMatch` | `#-1 NO MATCH` |
| `TooFewArguments` | `#-1 TOO FEW ARGUMENTS` |
| `Nothing` | `#-1` |
| `RegexTimeout` | `#-1 REGEX TIMEOUT` |
| `InvalidPlayer` | `#-1 INVALID PLAYER` |
| `DivisionByZero` | `#-1 DIVISION BY ZERO` |
| `FunctionNotFound` | `#-1 FUNCTION ({{function}}) NOT FOUND` |
| `LogOfNonpositive` | `#-1 LOG OF NONPOSITIVE` |
| `OutOfRange` | `#-1 OUT OF RANGE` |
| `RunawayLoop` | `#-1 TIMEOUT (RUNAWAY LOOP DETECTED)` |
| `ExecutionHalted` | `#-1 EXECUTION HALTED` |
| `FunctionInvocationLimitExceeded` | `#-1 FUNCTION INVOCATION LIMIT EXCEEDED` |
| `FunctionRecursionLimitExceeded` | `#-1 FUNCTION RECURSION LIMIT EXCEEDED` |
| `InvalidTarget` | `#-1 INVALID TARGET` |
| `NoCode` | `#-1 NO CODE` |
| `NoCommand` | `#-1 NO COMMAND` |
| `PermissionDenied` | `#-1 PERMISSION DENIED` |
| `QueueFull` | `#-1 QUEUE FULL` |
| `RecursionCycleDetected` | `#-1 RECURSION CYCLE DETECTED` |
| `SquareRootOfNegative` | `#-1 SQUARE ROOT OF NEGATIVE` |
| `FunctionDisabled` | `#-1 FUNCTION DISABLED` |
| `NoSuchAttribute` | `#-1 NO SUCH ATTRIBUTE` |
| `InvalidSecondArgument` | `#-1 INVALID SECOND ARGUMENT` |
| `FunctionExpects` | `#-1 FUNCTION ({{function}}) EXPECTS {{range}} ARGUMENTS BUT GOT {{got}}` |
| `SeparatorMustBeOneCharacter` | `#-1 SEPARATOR MUST BE ONE CHARACTER` |
| `FillerMustBeOneCharacter` | `#-1 FILLER MUST BE ONE CHARACTER` |
| `ArgumentMustBeInteger` | `#-1 ARGUMENT MUST BE AN INTEGER` |
| `ArgumentsMustBeIntegers` | `#-1 ARGUMENTS MUST BE INTEGERS` |
| `ArgumentMustBeNumber` | `#-1 ARGUMENT MUST BE A NUMBER` |
| `ArgumentsMustBeNumbers` | `#-1 ARGUMENTS MUST BE NUMBERS` |
| `ArgumentMustBePositiveInteger` | `#-1 ARGUMENT MUST BE A POSITIVE INTEGER` |
| `ArgumentsMustBePositiveIntegers` | `#-1 ARGUMENTS MUST BE POSITIVE INTEGERS` |
| `NumberOutOfRange` | `#-1 NUMBER OUT OF RANGE` |
| `ComparisonRequiresTwoOrMoreNumbers` | `#-1 COMPARISON REQUIRES 2 OR MORE NUMBERS` |
| `UnknownOperation` | `#-1 UNKNOWN OPERATION` |
| `FromBaseOutOfRange` | `#-1 FROM BASE OUT OF RANGE` |
| `ToBaseOutOfRange` | `#-1 TO BASE OUT OF RANGE` |
| `MalformedNumber` | `#-1 MALFORMED NUMBER` |
| `InvalidAngleType` | `#-1 INVALID ANGLE TYPE` |
| `VectorsMustBeSameDimensions` | `#-1 VECTORS MUST BE SAME DIMENSIONS` |
| `VectorMustNotBeEmpty` | `#-1 VECTOR MUST NOT BE EMPTY` |
| `VectorsMustBeThreeDimensional` | `#-1 VECTORS MUST BE THREE-DIMENSIONAL` |
| `StringLengthsMustBeEqual` | `#-1 STRING LENGTHS MUST BE EQUAL` |
| `TrCannotAcceptNonprintingChars` | `#-1 TR CANNOT ACCEPT NONPRINTING CHARS` |
| `InvalidAlignString` | `#-1 INVALID ALIGN STRING` |
| `NotEnoughColumnsForAlign` | `#-1 NOT ENOUGH COLUMNS FOR ALIGN` |
| `TooManyColumnsForAlign` | `#-1 TOO MANY COLUMNS FOR ALIGN` |
| `InvalidNumberOfArgumentsToAlign` | `#-1 INVALID NUMBER OF ARGUMENTS TO ALIGN` |
| `CannotHaveColumnsThatLarge` | `#-1 CANNOT HAVE COLUMNS THAT LARGE` |
| `UnknownHashType` | `#-1 UNKNOWN HASH TYPE` |
| `RegisterNameInvalid` | `#-1 REGISTER NAME INVALID` |
| `NoSuchLock` | `#-1 NO SUCH LOCK` |
| `NotOnChannel` | `#-1 NOT ON CHANNEL` |
| `NoSuchLockType` | `#-1 NO SUCH LOCK TYPE` |
| `NoChannelGiven` | `#-1 NO CHANNEL GIVEN` |
| `InvalidArgument` | `#-1 INVALID ARGUMENT` |
| `InvalidZone` | `#-1 INVALID ZONE` |
| `UnsupportedDigestType` | `#-1 UNSUPPORTED DIGEST TYPE` |
| `NotConnected` | `#-1 NOT CONNECTED` |
| `WrongNumberOfArguments` | `#-1 WRONG NUMBER OF ARGUMENTS` |
| `InvalidValue` | `#-1 INVALID VALUE` |
| `InvalidType` | `#-1 INVALID TYPE` |
| `InvalidJson` | `#-1 INVALID JSON` |
| `NotAPlayer` | `#-1 NOT A PLAYER` |
| `NoSuchPlayer` | `#-1 NO SUCH PLAYER` |
| `SqlIsNotEnabled` | `#-1 SQL IS NOT ENABLED` |
| `SqlError` | `#-1 SQL ERROR: {{error}}` |
| `MissingArguments` | `#-1 MISSING ARGUMENTS` |
| `NoSuchRecord` | `#-1 NO SUCH RECORD` |
| `NoSuchFile` | `#-1 NO SUCH FILE` |
| `NoEntry` | `#-1 NO ENTRY` |
| `RootOutOfRange` | `#-1 ROOT OUT OF RANGE` |
| `ImaginaryNumber` | `#-1 IMAGINARY NUMBER` |
| `CanOnlyTestOneElement` | `#-1 CAN ONLY TEST ONE ELEMENT` |
| `NumberOfWordsMustBeEqual` | `#-1 NUMBER OF WORDS MUST BE EQUAL` |
| `ListsMustBeOfEqualSize` | `#-1 LISTS MUST BE OF EQUAL SIZE` |
| `FunctionExpectsEvenArguments` | `#-1 FUNCTION ({{function}}) EXPECTS AN EVEN NUMBER OF ARGUMENTS` |
| `FunctionExpectsOddArguments` | `#-1 FUNCTION ({{function}}) EXPECTS AN ODD NUMBER OF ARGUMENTS` |
| `InvalidQRegisterName` | `#-1 INVALID Q-REGISTER NAME` |
| `FolderMustBeInteger` | `#-1 FOLDER MUST BE INTEGER` |
| `ArgumentMustEndInAnInteger` | `#-1 ARGUMENT MUST END IN AN INTEGER` |
| `BaseOutOfRange` | `#-1 BASE OUT OF RANGE` |
| `ArgumentsMustBeUnsignedIntegers` | `#-1 ARGUMENTS MUST BE UNSIGNED INTEGERS` |
| `ArgumentMustBeUnsignedInteger` | `#-1 ARGUMENT MUST BE UNSIGNED INTEGER` |
| `RegexpError` | `#-1 REGEXP ERROR: {{error}}` |
| `InvalidCompareType` | `#-1 INVALID COMPARE TYPE` |
| `InvalidAnsiDefinition` | `#-1 INVALID ANSI DEFINITION` |
| `InvalidColor` | `#-1 INVALID COLOR` |
| `ColorsRequiresAtLeastOneColor` | `#-1 COLORS() REQUIRES AT LEAST ONE COLOR` |
| `NoMatchingColorName` | `#-1 NO MATCHING COLOR NAME` |
| `NoValidPlayers` | `#-1 NO VALID PLAYERS` |

</details>

---

## Softcode Functions (`@function`)

`@function <name>=<obj>/<attr>[, <minargs>[, <maxargs>]]` binds persistent user-defined functions stored in SQLite (`soft_functions` table via `RuntimeStateStore`) with optional restriction flags (`NoGagged`, `NoFixed`, `NoGuest`, `Admin`, `Wizard`, `God`, `NoSideFX`, `LogArgs`, `LogName`, `NoParse`, `Localize`, `Userfn`, `StripAnsi`, `Literal`, `Deprecated`, `Nobody`). Built-in functions can also be restricted or disabled via `@function/restrict` and `@function/disable` (`restrict_function` in `mush.cnf`).
