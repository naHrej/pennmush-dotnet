---
topics: [PARENTS5]
---

While ancestors are checked for attributes at the end of the parent chain, they are NOT checked for $-commands or ^-listens.

If you are "mass-marketing" your objects, you can create blank copies, and @parent those copies to a template object. You can then customize necessary attributes on the copy. When a buyer @chowns his copy, the parent does not change, so unless you're putting data into the parent that you want to make impossible to read, it's safe to allow the purchasers of your object to @chown their copy.

Locks can also be inherited, but are flagged no-inherit by default. Use @lset to change that on a per-lock basis.

**See also:** @parent, $-COMMANDS, ATTRIBUTES, ANCESTORS, ORPHAN, pfun()
