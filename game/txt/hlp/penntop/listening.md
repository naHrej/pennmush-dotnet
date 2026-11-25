---
topics: [LISTENING]
---

There are two basic ways to trigger action on the MUSH. The basic way is to type in commands such as 'look' or '@emit'. These commands are not seen or heard by other players, although the results of the commands may be.

The other way is to "listen" for something said/emitted in your hearing. There are two ways to listen for something in a room. The easiest way is to use a combination of @listen and @ahear/@aahear/@amhear.

For example:
```text
    > @listen Welcome Mat=* has arrived.
    > @ahear Welcome Mat="Welcome, %n!
    Breaker has arrived.
    Welcome Mat says, "Welcome, Breaker!"

```

To trigger an object's @listen, you must pass its @lock/listen.

Continued in 'help listening2'.
