---
topics: [ZONE MASTER ROOMS, ZMRs]
---

Zone Master Rooms are a subset of zones. If a room is used as a zone, it is called a Zone Master Room (ZMR). ZMRs are like local "master" rooms; exits in the ZMR are global to that zone, and $-commands on objects in the ZMR are global to that zone ($-commands on the ZMR itself, like $-commands on the master room, are ignored). If a ZMR is a player's personal zone, objects in the ZMR are checked for commands that the player can use anywhere (but exits are not checked unless the player is in a zoned room).

Zone Master Rooms are useful either if you need global exits for the zone, or if the zone has a lot of $-commands which need to be restricted to different groups, as they can go on separate use-locked objects.

**See also:** ZONES, MASTER ROOM, EVALUATION
