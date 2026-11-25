---
topics: [LOOPING]
---

Looping in an object can have its good parts and its bad parts. The good part is when you activate part of a program multiple times to exhaustively perform an operation. This can be done like this:

```text
    &PART1 object=<action list> ; @trigger me/PART2
    &PART2 object= @select <test for being done>=<false>,@trigger me/PART1

```

Looping can be a problem when it goes on without stopping. The @ps command can be used to see if you are looping. Beware! A looping machine that isn't @halt'd will drain your pennies while you are away from the mush!

The @retry and @include commands, and %= substitution, can also be useful for building code which needs to loop.

**See also:** @ps, HALT, COSTS, @trigger, @retry, SUBSTITUTIONS3
