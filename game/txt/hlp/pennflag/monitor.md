---
topics: [MONITOR]
---

Flag:  MONITOR  (players, things, rooms)

The MONITOR flag activates the ^-listen patterns on an object. Objects which have ^-listen patterns but are not set MONITOR do not check those patterns. Unlike $-commands, ^-listens are not inherited from parents by default; LISTEN_PARENT must be set as well as MONITOR to enable this.

The NO_COMMAND attribute flag disables ^-listen matching for a single attribute; see 'help attribute flags' for details.

If using the LISTEN Attribute Tree, you must set the Root Attribute !NO_COMMAND. By default this Attribute/Attribute Root has been set NO_COMMAND by the system. Attributes which are set NO_COMMAND will not be checked for ^-listen patterns.

In order to trigger an object's ^-listen patterns, you must pass both its @lock/use and its @lock/listen.

This flag used to allow players to hear when someone connected to the game, but that is now controlled by the HEAR_CONNECT flag.

**See also:** LISTENING, @listen, LISTEN_PARENT, HEAR_CONNECT
