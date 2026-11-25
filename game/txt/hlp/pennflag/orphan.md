---
topics: [ORPHAN]
---

Flag:  ORPHAN  (all types)

Normally, all objects have an ancestor at the top of their @parent chain, and attributes, locks, etc, are inherited from the ancestor after all real @parents have been checked. When an object is set ORPHAN, its ancestor is ignored.

**See also:** @parent, ancestors
