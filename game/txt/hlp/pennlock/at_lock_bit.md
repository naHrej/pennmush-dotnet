---
topics: ["@lock-bit", "@lock-flag", "@lock-type", "@lock-power", "@lock-channel"]
---

BIT LOCKS
You can test for set flags, powers, or object types in a lock directly, without using an evaluation lock, with these formats:

```text
    @lock <object>=flag^<flag>
    @lock <object>=power^<power>
    @lock <object>=type^<type>

```

These locks act like the object the lock is on does a hasflag(%#, <flag>), or haspower(%#, <power>), hastype(%#, <type>) succeeding only if the flag/power is set, or the object is of the specified type.

For example,
```text
    @lock/use Admin Commands=flag^wizard|flag^royalty

```

You can also test for channel membership with:

```text
    @lock <object>=channel^<channel>
```
