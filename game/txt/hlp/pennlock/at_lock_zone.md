---
topics: ["@lock/zone", "@lock/chzone", "@lock/chown", "@lock/parent", "@lock/link", "@lock/open"]
---

@lock/zone
```text
    Objects which pass a SHARED player's @lock/zone control all the objects the shared player owns. If the zone_control_zmp_only @config option is off, anything passing the @lock/zone of other objects will control everything @chzoned to the object.
```

**See also:** @chzone, SHARED, ZONES, ZMR

@lock/chzone
```text
    If set, controls who can @chzone an object to this zone.
```

**See also:** @chzone, ZONES

@lock/chown
```text
    If set, controls who can change the owner of this CHOWN_OK object via @chown.
```

**See also:** CHOWN_OK, @chown

@lock/parent
```text
    Controls who can @parent something to this LINK_OK object.
```

**See also:** @parent, LINK_OK

@lock/link
```text
    Controls who can @link this unlinked exit, or who can @link an exit to this LINK_OK room/thing.
```

**See also:** @link, LINK_OK, LINK_ANYWHERE POWER

@lock/open
```text
    Controls who can @open an exit from this OPEN_OK room.
```

**See also:** @open, @dig, OPEN_OK, OPEN_ANYWHERE POWER
