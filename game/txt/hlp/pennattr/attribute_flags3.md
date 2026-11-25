---
topics: [ATTRIBUTE FLAGS3]
---

```text
  aahear (A)        ^-listens on this attribute match like @aahear
  amhear (M)        ^-listens on this attribute match like @amhear
  prefixmatch       When set with @<attrib>, this attribute will be matched down to its unique prefixes. This flag is primarily used internally, but also useful in @attribute/access.
  quiet (Q)         When altering the attribute's value or flags, don't show the usual confirmation message

```

These attribute flags are only used internally. They cannot be set, but seen on 'examine' and flags()/lflags(), tested for with hasflag(), etc:
```text
  branch (`)        This attribute is a branch. See: help ATTRIBUTE TREES

```

**See also:** @set, @attribute, ATTRIBUTE TREES
