---
topics: [RESTRICT3]
---

In cases where there are a function and command that do the same thing (like pemit() and @pemit), the command's restrictions are also checked when the function is called, so to use pemit() you must also be able to use @pemit. However, a function's restrictions are not checked when a command is called, to allow disabling side-effect functions.

Some functions (like name()) have non-side-effect and side-effect versions depending on how many arguments they're called with. The side-effect version can be disabled while keeping the safe non-side-effect form with the 'nosidefx' restriction. This can also be used to disable pure side-effect functions.

Examples:
Only allow admin to use ansi():
```text
    > @function/restrict ansi=admin

```

Don't let anyone set SUSPECT or GAGGED use @emit, and log the name of anyone who uses it.
```text
    > @command/restrict @emit=logname
    > @command/restrict @emit=!flag^suspect&!flag^gagged

```

**See also:** @command, @function, @attribute, @lock
