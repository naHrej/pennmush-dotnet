---
topics: [POWERS2, POWERS LIST2]
---

```text
  no_pay *              Doesn't need money for anything
  no_quota *            Has an unlimited quota
  open_anywhere         Can @open an exit from any room.
  pemit_all             Can @pemit to HAVEN/ulocked players.
  pick_dbref            Can recycle specific garbage dbrefs when creating objects.
  player_create         Can use @pcreate command.
  poll                  Can use @poll command.
  queue                 Has queue limit equal to the size of the database.
  quotas                Can use @quota commands on other players.
  search                Can do @search, @stats, and @entrances on anything.
  see_all               Sees everything as if it were Visual.
  see_queue             Can do @ps on anyone, and @ps/all.
  send_oob              Can use oob(), and the xch_cmd and send pueblo tags
  sql_ok                Can perform SQL queries
  tport_anything        Can @teleport anything.
  tport_anywhere        Can @teleport to anywhere.
  can_spoof             Can use @ns*emit, ns*emit() and @*emit/spoof
  can_dark              Can @set themselves dark
  can_http              Can use @http
  html                  Can use html() without restriction.

```

* When these powers are set on a player, all the objects they own which are not set MISTRUST are also considered to have the power.

**See also:** help @power, and especially @power/list
