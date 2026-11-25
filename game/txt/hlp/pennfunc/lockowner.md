---
topics: [LOCKOWNER()]
---

lockowner(<object>[/<locktype>])

This function returns the dbref of the player who owns the <locktype> lock on <object>, or the Basic lock if no <locktype> is given. You must be able to examine the lock to use this function.

**See also:** lockflags(), llockflags(), lset(), lock(), llocks()
