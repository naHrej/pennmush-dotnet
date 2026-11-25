---
topics: ["@lock-attribute"]
---

ATTRIBUTE LOCKS
You can lock an object to an attribute on the person trying to pass the lock (as long as the object can "see" that attribute):

```text
    @lock <object>=<attribute>:<value>

```

<value> can contain wildcards (*), greater than (>) or less than (<) symbols.

For example:
```text
    @lock Men's Room = sex:m*
      This would lock the exit "Men's Room" to anyone with a SEX attribute starting with the letter "m".
    @lock A-F = icname:<g
      This would lock the exit "A-F" to anyone with a ICNAME attribute starting with a letter "less than" the letter "g". This assumes that ICNAME is visual or the object with the lock can see it.
```
