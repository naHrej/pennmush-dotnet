---
topics: ["@lock/user", "@lock/user:<name>"]
---

@lock/user:<name>
```text
    User-defined locks have no hardcoded meaning. They allow you to set locks for any purpose, which you can test using the elock() function. <name> can be anything which is a valid attribute name. For example, in a combat system you might use a "wield" @lock on weapons, similar to

      > @lock/user:wield War Hammer=strength:>20

      and then test it with elock(War Hammer/wield, %#).
```

**See also:** elock(), valid()

**See also:** @lock, locktypes, lockkeys
