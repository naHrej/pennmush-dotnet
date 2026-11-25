---
topics: [%!, EXECUTOR]
---

The executor of a command is the object actually carrying out the command or running the code. This differs from the enactor, because the enactor is the object that sets off the command. In some cases, the enactor and the executor will be the same. The substitution %! evaluates to the dbref of the executor of the code.

For example:
```text
    > @emit %n (%#) is the enactor and %! is the executor!
    Cyclonus (#6) is the enactor and #6 is the executor!
    > @create Box
    Created: Object #10
    > &EMIT box=$emit: @emit %n (%#) is the enactor and %! is the executor!
    > emit
    Cyclonus (#6) is the enactor and #10 is the executor!

```

In the first case, Cyclonus directly entered the command and was therefore both the enactor and the executor. In the second, Cyclonus set off the command on the box, so Cyclonus was still the enactor, but the box was the object that was actually doing the @emit, and was thus the executor.

**See also:** ENACTOR, CALLER, SUBSTITUTIONS
