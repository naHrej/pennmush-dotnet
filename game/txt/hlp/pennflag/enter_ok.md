---
topics: [ENTER_OK]
---

Flag:  ENTER_OK  (all types)

If a player or thing is ENTER_OK, someone may:
```text
    * "enter" it, if they pass its @lock/enter,
    * "give" to it, if they pass its @lock/from, or
    * use "get <object>'s <item>" to take something from its inventory, if they pass its @lock/take (and possessive_get is enabled; see "help get").

```

This flag has no effect on rooms or exits.

**See also:** enter, leave, give, get, @lock, @ealias
