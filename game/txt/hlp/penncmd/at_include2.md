---
topics: ["@include2"]
---

@include takes the following switches to alter its behaviour:
```text
  /nobreak:   Prevents an @break/@assert in the included attribute from breaking the including action list.
```

/localize:  Saves all q-registers before including the attribute, and restores them after including the attribute.
/clearregs: Clears all q-registers before including the attribute.

**See also:** @trigger, ufun(), @break
