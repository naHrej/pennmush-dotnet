---
topics: [ATTRIBUTE TREES2, ATTR TREES2, ATTRIB TREES2, `2]
---

Attribute trees provide two immediate benefits. First, they reduce spam when examining objects. The usual * and ? wildcards for attributes do not match the ` character; the new ** wildcard does. Some examples of using examine:
```text
   examine obj              displays top-level attributes (plus object header)
   examine obj/*            displays top-level attributes
   examine obj/BRANCH`      displays only attributes immediately under BRANCH
   examine obj/BRANCH`*     displays only attributes immediately under BRANCH
   examine obj/BRANCH`**    displays entire tree under BRANCH
   examine obj/**           displays all attributes of object

```

The same principles apply to lattr(). @decompile obj is a special case, and displays all attributes.

Branch attributes will be displayed with a ` in the attribute flags on examine.

See 'help attribute trees3' for more information and examples.
**See also:** WILDCARDS
