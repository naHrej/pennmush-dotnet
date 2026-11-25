---
topics: [ANCESTORS]
---

ANCESTORS

Objects can inherit attributes and locks from other objects through the use of parents. An object's parent, its parent's parent, its parent's parent's parent, etc. constitute the object's "parent chain" and lookups work the way up the chain until an inheritance occurs.

Ancestors are "virtual parents" that are assumed to be last on every parent chain. There is one ancestor for each object type (room, exit, thing, player), and @config lists the dbref of each ancestor object (@config ancestor_room, etc). Under normal circumstances, if an attribute/lock can't be retrieved from an object or any of its explicit parents, the attribute will be looked for on the appropriate ancestor. The ORPHAN flag may be set on an object to cause lookups on that object to ignore ancestors (like the pre-ancestor behavior).

Ancestors may themselves have parent chains, but these are (obviously) not virtually terminated by ancestors.

Note that the choice of which ancestor to look up is based on the type of the *child* object, as is the check of the ORPHAN flag. Also note that ancestors are *not* checked for $-commands or ^-commands; you should use the master room for global commands, instead.

**See also:** PARENTS, ORPHAN
