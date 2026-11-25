---
topics: ["@lock-owner", "@lock-carry"]
---

OWNER LOCK

An "owner" lock allows you to lock something to anything owned by the same player:
```text
    @lock Box = $My Toy
```

This locks "Box" to anything owned by the owner of "My Toy" (since players own themselves, that includes the owner as well).

CARRY LOCK
You can lock an object to something that has to be carried:
```text
    @lock Door = +Secret Door Key
      This locks the exit "Door" to someone carrying the object "Secret Door Key". Anyone carrying that object will be able to go through the exit.

```

You can lock an object to -either- an object or to someone carrying the object with:
```text
    @lock Disneyworld Entrance = Child
      This locks the exit "Disneyworld Entrance" to either the object "Child" -or- to someone carrying the object "Child". (OK, so it's a weird example.)

```

This is the same as @lock Entrance=+Child|=Child.
