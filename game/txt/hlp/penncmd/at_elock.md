---
topics: ["@elock", "@eunlock"]
---

@elock <object>[=<key>]
@eunlock <object>

@elock sets the Enter @lock for <object> to <key>, or clears the the lock if no <key. is given. @eunlock removes the Enter @lock for <object>.

@elock and @eunlock are deprecated and uses of them should be replaced by
```text
    @lock/enter <object>[=<key>]
```

and
```text
    @lock/enter <object>

```

**See also:** @lock, locktypes, enter, ENTER_OK
