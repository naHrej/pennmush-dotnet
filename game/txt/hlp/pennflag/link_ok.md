---
topics: [LINK_OK]
---

Flag: LINK_OK  (rooms, things)

If a room or object is LINK_OK, anyone who passes the room's @lock/link can link exits to it (but still not from it). Also, LINK_OK overrides the TRUST protection against @trigger (although not @force or @set).

Anyone who passes a LINK_OK object's @lock/parent can @parent an object to it.

**See also:** @link, TRUST, @parent, PARENTS, OPEN_OK
