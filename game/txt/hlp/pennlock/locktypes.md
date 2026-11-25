---
topics: [locktypes, locklist, lock types, lock list]
---

These are the standard lock types supported by PennMUSH. For more detailed information about any lock type, see "help @lock/<lock>".

```text
  @lock/basic           Who can pick up the player/thing, or go through the exit.
  @lock/enter           Who can enter the player/object (aka @elock)
  @lock/teleport        Who can teleport to the room
  @lock/use             Who can use the object (aka @ulock)
  @lock/page            Who can page/@pemit the player
  @lock/zone            Who can control objects on this zone
  @lock/parent          Who can @parent something to this object/room
  @lock/link            Who can @link something to this object/room or who can @link this unlinked exit.
  @lock/open            Who can @open an exit from this room
  @lock/mail            Who can @mail the player
  @lock/user:<name>     User-defined. No built-in function of this lock, but users can test it with elock()

```

Continued in 'help locktypes2'.
