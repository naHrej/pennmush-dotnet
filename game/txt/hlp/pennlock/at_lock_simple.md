---
topics: ["@lock-simple", "@lock-objid"]
---

SIMPLE LOCKS

You can lock an object in several different ways. The simplest lock is one that always succeeds (#true) or always fails (#false), or that matches a specific object by prefixing it with an "=":

> @lock My Toy = #false
```text
     This lock will always fail.

```

> @lock My Toy = =me
```text
     This locks the object "My Toy" to you and you alone. It is recommended that you @lock me = =me in order to prevent anyone else from picking you up. The two = signs are NOT a typo! The first is part of the @lock syntax (as shown at the top of 'help @lock') the second is a lock key that means "only this exact object".

```

For backwards compatability, OBJID^<object> is an alias for =<object>.
