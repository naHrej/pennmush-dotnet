---
topics: [XVTHINGS(), XTHINGS()]
---

xthings(<object>, <start>, <count>)
xvthings(<object>, <start>, <count>)

xthings() fetches <count> or fewer non-player dbrefs from <object>'s contents starting at position <start>. It is useful when the number of things in a container causes lthings() to exceed the buffer limit. It is equivalent to:

```text
    extract(lthings(<object>), <start>, <count>)

```

xvthings() is identical, except it follows the restrictions of lvthings().

**See also:** nthings(), lthings(), lvthings(), xplayers(), xexits()
