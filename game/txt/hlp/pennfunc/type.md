---
topics: [TYPE()]
---

type(<object>)

This function returns the type of an object - one of PLAYER, THING, EXIT, ROOM or GARBAGE - or #-1 if the object can't be found.

Examples:
```text
    > @create Test
    > think type(Test)
    THING
    > think type(me)
    PLAYER
    > think type(here)
    ROOM

```

**See also:** hastype(), TYPES OF OBJECTS
