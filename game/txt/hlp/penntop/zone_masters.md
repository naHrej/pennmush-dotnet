---
topics: [ZONE MASTERS, ZMPs, SHARED PLAYERS]
---

SHARED PLAYERS

Shared players are player objects which are used to mediate shared control of objects. A shared player is an object of type PLAYER which has the SHARED flag set. They are created like ordinary players, and can connect, build, etc. The only difference is that objects owned by a shared player are controlled by anything that passes the @lock/zone of the shared player.

Anyone who passes the @lock/zone of the shared player can @chown objects to it. This, however, does not refund the original creator's money or quota, as does normal.

Shared players used to be known as Zone Masters. The term was changed to emphasize the fact that they are not related to Zone Master Objects, which are used to allow area-specific $-commands.

Continued in HELP SHARED PLAYERS2
