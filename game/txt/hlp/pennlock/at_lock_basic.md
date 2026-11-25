---
topics: ["@lock/basic", "@lock/enter", "@lock/leave", "@lock/teleport"]
---

@lock/basic
```text
    For exits, this lock controls who can pass through the exit.
    For players and things, it controls who can "get" the object.
    For rooms, it determines whether the @success or @failure verbs are triggered when someone "look"s at the room. However, even when the lock is failed, the "look" still occurs.
```

**See also:** @success, @failure, goto, get, look

@lock/enter
```text
    For players and things, the Enter lock controls who can "enter" an ENTER_OK object, as well as who can "empty" it. It has no meaning for exits or rooms.
```

**See also:** @enter, @efail, ENTER_OK, enter, empty

@lock/leave
```text
    For players, things and rooms, the Leave lock controls who can leave the object, via "leave", "@teleport" or "goto". It has no meaning for exits.
```

**See also:** @leave, @lfail, leave

@lock/teleport
```text
    For rooms, the Teleport lock controls who can "@teleport" into the room, if it has the JUMP_OK flag set. It has no meaning for players, things or exits.
```

**See also:** JUMP_OK, @teleport

**See also:** @lock, locktypes, lockkeys
