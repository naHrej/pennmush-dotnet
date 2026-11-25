---
topics: ["@lock-evaluation"]
---

EVALUATION LOCK
An evaluation lock is set using this format:

```text
    @lock <object>=<attribute>/<value>

```

The difference between this and an attribute lock is that the <attribute> is taken from <object> rather than from the person trying to pass the lock. When someone tries, <attribute> is evaluated, and the result is compared to <value>. If it matches, then the person passes the lock.

The person trying to pass the lock is %# and <object> is %! when the evaluation takes place. The evaluation is done with the powers of <object>. If you try to do something (like [get(%#/<attribute>)]) and <object> doesn't have permission to do that, the person will automatically fail to pass the lock.

Continued in 'help @lock-eval2'.
