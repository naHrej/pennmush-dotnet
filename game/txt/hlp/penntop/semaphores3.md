---
topics: [SEMAPHORES3]
---

It's important to remember that the actions will be carried out NOT by the object that they are being @waited on, but by whichever object entered the @wait.

Examples:
```text
    > @wait semaphore=:tests.
    > @notify semaphore
    Wizard tests.

    > @wait timer/30=:waits 30 seconds.
    [ 30 seconds passes. ]
    Wizard waits 30 seconds.

```

Continued in 'help semaphores4'.
