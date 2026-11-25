---
topics: [FULLNAME()]
---

fullname(<object>)

fullname() returns the full name of object <object>. It is identical to name() except that for exits, fullname() returns the complete exit name, including all aliases.

Example:
```text
    > say fullname(south)
    You say, "South;sout;sou;so;s"

```

**See also:** name(), accname(), iname(), alias(), fullalias()
