---
topics: [SHUFFLE()]
---

shuffle(<list>[, <delimiter>[, <osep>]])

This function shuffles the order of the items of a list, returning a random permutation of its elements.

<delimiter> defaults to a space, and <osep> defaults to <delimiter>.

Example:
```text
    > say shuffle(foo bar baz gleep)
    You say, "baz foo gleep bar"

```

**See also:** scramble(), pickrand()
