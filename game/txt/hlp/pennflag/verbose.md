---
topics: [VERBOSE]
---

Flag:  VERBOSE  (all types)

An object set VERBOSE echoes the commands it executes to its owner before executing them. This differs from the PUPPET flag in that the owner sees the command itself, rather than the output from the command.  This flag is extremely useful in debugging, especially if used in conjunction with the PUPPET and DEBUG flags.

Setting the VERBOSE flag on a PUPPET also causes the puppet to relay sound to its owner, even when they are in the same room.

Example:
```text
    > @create Test
    Created: Object #5.
    > @set Test=VERBOSE
    Test - VERBOSE set.
    > @force Test=say capstr(hello)
    #5] say capstr(hello)
    Test says, "Hello"

```

**See also:** PUPPET, DEBUG
