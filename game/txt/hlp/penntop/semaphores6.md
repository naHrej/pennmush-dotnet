---
topics: [SEMAPHORES6]
---

An example:

> @wait me/semtest=think blah
> ex me/semtest
SEMTEST [#1ic+]: 1
> @ps
...
Semaphore Queue:
[#8/SEMTEST]Raevnos(#8P):think blah
...
> @notify me/semtest
Notified.
blah

This allows you to use one object to control many different things -- for example, fights in a turn-based combat sytem.
