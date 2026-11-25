---
topics: ["@lock/follow", "@lock/forward", "@lock/dropto"]
---

@lock/follow
```text
    For players and things, controls who may "follow" the object. Has no meaning for rooms or exits.
```

**See also:** follow, FAILURE

@lock/forward
```text
    For players, things and rooms, controls who can forward sound to an object, via @forwardlist or @debugforwardlist. Meaningless for exits.
```

**See also:** @forwardlist, @debugforwardlist, @lock/mailforward

@lock/dropto
```text
    For rooms, only objects which pass this lock will be sent to the rooms Drop-To. Has no meaning for players, things or exits.
```

**See also:** DROP-TOS, drop, empty

**See also:** @lock, locktypes, lockkeys
