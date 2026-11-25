---
topics: ["@lock-dbreflist", "@lock-list"]
---

LIST LOCK
You can test to see if the enactor is a member of a space-separated list of dbrefs or objids on an attribute on the object, with:

```text
    @lock <object>=dbreflist^<attributename>

```

For example,
```text
    &allow Commands = #1 #7 #23 #200:841701384
    &deny commands = #200 #1020
    @lock/use commands = !dbreflist^deny & dbreflist^allow
```
