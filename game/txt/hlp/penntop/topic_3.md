---
topics: [^, ^-LISTENS, LISTENING2]
---

If you need an object to "listen" for more than one pattern, you can also use ^-patterns. These work similar to user-defined commands, using ^ instead of $. An object must be set MONITOR to have ^-patterns activated.

Syntax:  &<attribute> <object> = ^<pattern>:<action list>

For example:
```text
    > @set Welcome Mat = MONITOR
    > &greet Welcome Mat=^* has arrived.:"Welcome, %n!
    > &goodbye Welcome Mat=^* has left.:POSE says as %n leaves, "Bye!"
    Grimlock has arrived.
    Welcome Mat says, "Welcome, Grimlock!"
    Grimlock has left.
    Welcome Mat says as Grimlock leaves, "Bye!"

```

Such attributes can also be @triggered as if the ^<pattern>: did not exist.

In order to trigger an object's ^-listen patterns, you must pass BOTH its @lock/use and its @lock/listen.

Continued in 'help listening3'.
