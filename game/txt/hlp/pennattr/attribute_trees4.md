---
topics: [ATTRIBUTE TREES4, ATTR TREES4, ATTRIB TREES4, `4]
---

Attribute trees interact with @parent in several ways.

As usual, children inherit attributes from their parent unless the child has its own overriding attribute. However, children that wish to override a leaf attribute must also have their own (overriding) copy of all branches leading to that leaf. This means that when you do:

> &BRANCH parent = a branch
> &BRANCH`LEAF parent = a leaf
> &BRANCH`LEAF child = a new leaf

In this case, a new BRANCH attribute will be created on the child, so '-[get(child/BRANCH)]-' will return '--'. This may not be what you actually want. In these cases, the pfun() function can be useful:

> &BRANCH child=pfun(BRANCH)

If a branch on the parent is set no_inherit, it will not be inherited, regardless of any other flags that may be present. If a branch is inherited, the child object can not loosen any access restrictions to inherited attributes that are set by the parent (although it may loosen access restrictions to its own attributes on the same branch). The child object may impose stricter restrictions, however, and these may prevent access to inherited parent data.
