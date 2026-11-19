# API: Async & Background Tasks

Concurrent softcode execution and queue submission extensions extracted from `PennMUSH.Core/MUSHcode/Functions/AsyncFunctions.cs` (**3** functions).

> [!NOTE]
> All three functions evaluate their arguments eagerly (`noParse` is `false`). Wrap `<code>` in braces `{...}` or escape brackets when passing softcode expressions into `async()` so evaluation happens on the background thread rather than before `async()` is invoked.

---

## Functions (`AsyncFunctions.cs`)

| Function | Signature | Notes |
|---|---|---|
| `async` | `async(<code>[, <callbackCmd>])` | Runs `<code>` on a `Task.Run` worker thread; returns `TASK#<id>` |
| `parallel_map` | `parallel_map(<list>, <delimiter>, <ufun>)` | Evaluates `u(<ufun>, <item>)` concurrently via `Parallel.For` |
| `bgtask` | `bgtask(<command>)` | Enqueues `<command>` directly into `ActionQueue`; returns `PID#<pid>` |

---

## Behavior & Edge Cases

* **`async(<code>[, <callbackCmd>])`**:
  - Returns `#-1 NO CODE` if no arguments are supplied or `FunctionRegistry.Evaluator` is unavailable.
  - Clones an isolated `EvaluationContext` (`Executor`, `Enactor`, `Caller`, `Location`, `Arguments.Clone()`) so background execution does not mutate the caller's temporary stack.
  - When a non-blank `<callbackCmd>` finishes and `ActionQueue` is available (`reg.Queue ?? _queue`), replaces literal `%0` in `<callbackCmd>` with the evaluated result (or appends `" <result>"` if `%0` is absent) and enqueues it with `%0` set to the result. Unhandled exceptions inside the worker task are suppressed to protect the server tick.
* **`parallel_map(<list>, <delimiter>, <ufun>)`**:
  - Expects 3 arguments (`<delimiter>` empty defaults to `" "`): splits `<list>` with `StringSplitOptions.None` (preserving empty segments), creates a child `EvaluationContext` per element, evaluates `[u(<ufun>, <item>)]` across worker threads via `Parallel.For`, and rejoins outputs with `<delimiter>`.
  - Returns `""` if fewer than 3 arguments are supplied or `<list>` is empty.
* **`bgtask(<command>)`**:
  - Requires at least 1 argument (`#-1 NO COMMAND` if empty or no queue attached).
  - Enqueues `<command>` for `ctx.Executor` / `ctx.Enactor` onto `ActionQueue`; returns `PID#<pid>` on success or `#-1 QUEUE FULL` if the queue rejects the entry.

```text
think [async({[iter(lnum(1000), add(##, 1))]}, @pemit me=Sum done: %0)]
-> TASK#1

think [parallel_map(a|b|c, |, me/UPPER_FN)]
-> A|B|C

think [bgtask(@emit Deferred tick work)]
-> PID#42
```

---

See also: [Architecture & Concepts](Architecture-and-Concepts) · [SQL, HTTP & System](API-SQL-HTTP-and-System) · [Functions Index](Functions-Index) · [Home](Home)
