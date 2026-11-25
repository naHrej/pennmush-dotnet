---
topics: [ROOMS]
---

Rooms are outermost-containers: they cannot be moved. They are created with @dig. Rooms can contain exits. They are checked for $-commands and ^-listens. Rooms do not have aliases.

@linking a room creates a drop-to. Rooms have no location; loc() on a room returns its drop-to (as does home()).

**See also:** TYPES OF OBJECTS, type(), hastype(), @dig, DROP-TOS
