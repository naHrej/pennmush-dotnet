---
topics: [SEMAPHORES4]
---

Semaphores can be used to enforce mutual exclusion - to prevent the same object from being used simultaneously by two players. The basic strategy is to ensure that the object always has a SEMAPHORE of -1, to enclose commands in an @wait, and to conclude the set of commands with an @notify me:

```text
    > &doit obj=$doit: @wait me={&doer me = %n; @trigger me/report}
    > &report obj=say [v(doer)] did it!; @notify me
    > @startup obj=@drain me; @notify me
    > @notify obj
    > ex obj/SEMAPHORE
    SEMAPHORE [#1ic+]: -1
    > doit
    obj says, "Talek did it!
    > ex obj/SEMAPHORE
    SEMAPHORE [#1ic+]: -1

```

If a second player types doit as well, the second player's command is put on the semaphore queue and not run until the @notify me at the end of the REPORT attribute. Note the STARTUP attribute - because semaphores are cleared when the MUSH starts up, you must insure that the object gets @notify'd once when it starts up.

Continued in 'help semaphores5'.
