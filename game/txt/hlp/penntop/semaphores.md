---
topics: [SEMAPHORES]
---

The most complicated thing about semaphores is their name. Before you try to use semaphores, you should first be familiar with the "@wait" command. If you are, then you know that normally, you type:

```text
    @wait <number of seconds>=<action>

```

and the action takes place after that number of seconds has passed. With a semaphore, you instead type:

```text
    @wait <object>=<action>
    @wait <object>/<number of seconds before timeout>=<action>

```

and the action takes place after the object has been "notified" that its time for it to happen. You can also set a timeout -- if the object hasn't been notified by the time that number of seconds has passed, the action will take place. Any object (player, thing, exit, room) that you control or that is set LINK_OK can be used to wait actions on.

Continued in 'help semaphores2'.
