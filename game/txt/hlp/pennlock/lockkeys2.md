---
topics: [lockkeys2, lock keys2, "@lock-complex"]
---

A lock key can be negated by prefixing the key with an "!". For example:

```text
    > @lock North=flag^wizard
    > @lock South=!flag^wizard

```

only lets those with the Wizard flag pass through the North exit, while only allowin those who do NOT have the Wizard flag to go South.

You can combine keys, either allowing someone to pass a lock if they pass any of the keys given, or requiring that they pass all of the keys, using the "|" (or) and "&" (and) symbols. For example:

```text
    > @lock OOC Room= status:OOC | power^guest

```

locks the exit "OOC Room" so that only those with their STATUS attribute set to "OOC", or those with the Guest @power, can pass, while

```text
    > @lock Men's Room= Sex:Male & +Bathroom Key

```

only allows those with their @sex set to Male who are carrying a "Bathroom Key" object to pass.

You can group together different sets of keys by enclosing each group in parenthesis "()". For instance,

```text
    > @lock Entrance=!type^player | (type^player & !flag^unregistered)

```

allows non-players to pass, or players who do not have the "unregistered" flag set.

**See also:** @lock, locktypes, @clock, objid()
