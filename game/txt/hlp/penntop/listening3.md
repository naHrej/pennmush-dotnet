---
topics: [LISTENING3]
---

By default, ^-patterns work like @ahear. To have them work like @amhear, set the AMHEAR attribute flag on the attribute; to have them work like @aahear, set the AAHEAR attribute flag on the attribute (Note that the triggering object is whatever happens to be %#, so, for example, when you @set an object MONITOR, you are %# with regard to the "Object is now listening" message, and this message can be picked up with an ^-pattern.)

Additionally, unlike $-commands, ^-patterns are NOT inherited via @parent, unless the LISTEN_PARENT flag is set on the listener. @listen is never inherited.

Listen patterns are checked after the object's normal @listen attribute.

**See also:** @listen, @ahear, @amhear, @aahear, MONITOR, LISTEN_PARENT, USER-DEFINED COMMANDS, INTERIORS
