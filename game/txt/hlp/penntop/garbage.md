---
topics: [GARBAGE]
---

Garbage objects previously existed as one of the four main types (player, thing, exit or room) but were destroyed with @destroy/@nuke. They exist only as placeholders in the database; you can do nothing with them. The dbrefs of garbage objects will be reused when new objects are created (but with a different creation time/object id).

The total number of garbage objects, and the next garbage object to be recycled, is shown in @stats. You can use lsearch(all,type,garbage) to get a list of all garbage dbrefs.

**See also:** @destroy, @stats
