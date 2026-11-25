---
topics: [CLOCK()]
---

clock(<channel>[/<locktype>][, <new lock>])

With one argument, returns the value of a lock on a channel, if you own the channel or are See_All. If no locktype is given, "JOIN" is assumed. With two arguments, sets the lock if you would be able to do so via @clock.

**See also:** @clock, @lock, channel functions
