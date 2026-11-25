---
topics: [QUEUE]
---

QUEUE

The queue is the waiting line for action lists to be executed by the MUSH.  Each time you enter an action list, it goes into the queue and stays there until its turn comes up, at which time the MUSH processes the commands and you see the results. The MUSH can execute thousands of commands every second, so normally you see results right away.

There are actually two distinct queues:
* Incoming socket commands. These are run immediately, so a "look" or "@ps" will happen before any other softcode that is waiting to run. A socket may 'burst' up to 100 commands, then 1 per second after that.
* Command queue. These enter the queue via @trigger, @force, or many other ways.

There is also a Wait queue, where commands queued via the @wait command go, and a Semaphore queue, where semaphore @waits go. These commands are moved to the command queue at the appropriate time, either when the @wait time elapses, or when the semaphore is @notified.

You can see which commands are currently queued with the @ps command, and with the getpids(), lpids() and pidinfo() functions. Queued commands can be cancelled with @halt and/or @drain.

Continued in 'help queue2'.
