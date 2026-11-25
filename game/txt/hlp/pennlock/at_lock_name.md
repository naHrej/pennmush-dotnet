---
topics: ["@lock-name"]
---

NAME LOCKS
You can test for objects matching a given name by using the below format

```text
    @lock <object>=name^<pattern>

```

It is similar to performing strmatch(%n,<pattern>), though will also match for a player/exit with <pattern> as one of its @aliases.

For example, to lock "Bob's Tools" to only people with a name beginning with Bob:
```text
    @lock/use Bob's Tools=name^bob*
```
