---
topics: [WARNINGS LIST2]
---

These warnings combine the functionality of multiple warnings above:

```text
  serious      exit-unlinked, thing-desc, room-desc, my-desc, lock-checks
  normal       serious, exit-oneway, exit-multiple, exit-msgs
  extra        normal, thing-msgs
  all          all of the above

```

The warning "none" indicates no warnings.
You can exclude warnings from a larger list by using !<warning> after the larger list. For example: @warnings me=all !exit-oneway
