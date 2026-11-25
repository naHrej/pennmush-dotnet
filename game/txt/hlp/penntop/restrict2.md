---
topics: [RESTRICT2]
---

For commands, <restriction> should be an @lock-style boolexp (though, for backwards compatability, the restrictions below can be used, and will be converted into an @lock automatically). For functions, <restriction> should be any combination of the phrases below. For attributes, <restriction> is a list of attribute flags, or "none" to create a standard attribute with no restrictions (see 'help attribute flags').

```text
    god        Command or function is usable only by God.
    wizard     Usable only by wizards.
    admin      Usable only by Wiz/Roy.
    nogagged   Usable only by non-GAGGED objects.
    nofixed    Usable only by non-FIXED objects.
    noguest    Usable only by non-guest @powered objects.
    nobody     Nothing can use it. Same as the /disable switch to @command or @function.
    logname    When used, log cmd/fun name, and who is using it
    logargs    When used, log cmd/fun name and args, and who is using it

```

Functions only:
```text
    noparse    Function arguments are not evaluated. Only applies to @functions.
    localize   %q-registers are saved/restored when evaluating, as if the @function were wrapped in localize().
    userfn     Function can only be called from within an @function.
    nosidefx   Don't allow side-effects for this function. See also the function_side_effects @config option.
    deprecated This function should no longer be used. Warns the executor's owner whenever someone uses the function.

```

Commands only:
```text
     noplayer   Cannot be used by players.
```

Commands can also give any flag, power or type, to restrict to objects with one of those flags or powers, or of one of those types.

Continued in 'help restrict3'.
