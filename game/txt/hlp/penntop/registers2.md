---
topics: [REGISTERS2]
---

As you can see from the above example, the command trigger had two wildcards separated by a "=" sign. When the user types in the command with some words taking the place of the wildcards, the first register (register 0) is filled with whatever part of the command replaces the first wildcard (in this case, "whee") and the second register is filled with whatever replaces the second ("blert foo").

They can also be filled with information that is passed by an @trigger command:

```text
    > &SOMECODE me=@emit %0 is in register 0 and %1 is in register 1.
    > @trigger me/somecode=whee,foo bar
```

whee is in register 0 and foo bar is in register 1.

Please see 'help setq()' for more information about the setq registers.

**See also:** SUBSTITUTIONS, @trigger, USER-DEFINED COMMANDS, WILDCARDS, setq(), v(), r(), registers(), listq(), letq(), unsetq(), strmatch()
