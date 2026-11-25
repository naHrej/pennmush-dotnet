---
topics: ["@lock/use", "@lock/command", "@lock/listen"]
---

@lock/use
```text
    For players, things and rooms, this lock controls who may "use" the object. You must also pass an object's Use lock to trigger $-commands or ^-listens on it (as well as the Command/Listen lock; see below). When an object is used as a Channel Mogrifier, only players who pass the object's Use lock will have their speech on the channel mogrified. Has no meaning for exits.
```

**See also:** @use, @ufail, use, $-commands, ^, MOGRIFY

@lock/command
```text
    For players, things and rooms, you must pass this lock (as well as the Use lock) to trigger $-commands on the object. Meaningless for exits.
```

**See also:** $-commands, FAILURE

@lock/listen
```text
    For players, things and rooms, you must pass this lock (as well as the Use lock) to trigger ^-listen patterns on the object when it's set MONITOR. Meaningless for exits.
```

**See also:** ^

**See also:** @lock, locktypes, lockkeys
