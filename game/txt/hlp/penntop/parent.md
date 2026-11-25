---
topics: [PARENT, PARENTS, OBJECT PARENTS]
---

Objects may have "parent" objects, from which they can inherit attributes. Once an object is given a parent, it may use the attributes on the parent just as if the attributes were on the object itself, including checking for $-commands. Use the @parent command to change the parent of an object.

Objects may have multiple levels of parents - thus, if #100 is the parent of #101, which is the parent of #102, object #102 checks itself, #101, and #100 for attributes. Attributes are checked on the object itself first, followed by its parent, followed by that parent's parent, and so on. There is a (configurable) maximum length of the parent chain for an object; the default is 10.

After the parent chain is exhausted, the type-specific ancestor is also checked in similar fashion. See 'help ANCESTORS' for more about ancestors.

Continued in 'help parents2'.
