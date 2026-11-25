---
topics: [FLAGS2]
---

You can see the list of flags set on an object in several ways.

1. If you are allowed to examine the object. The flags are listed in expanded word format on the line just below the object's name, after the word "Flags:".
2. Flag abbreviations are also visible after the object's name in the room description, if the object is not set OPAQUE and you are not set MYOPIC.
3. The lflags() and flags()s function will return a list of flag names and abbreviations for an object, respectively.

Note: The object type (player, thing, room, exit or garbage) is not actually a flag. See 'help types of objects' for more information.

**See also:** examine, flags(), hasflag(), orflags(), andflags(), orlflags(), andlflags(), types of objects, type(), hastype(), @flag, FLAG LIST, @set, set(), attribute flags
