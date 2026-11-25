---
topics: [lock types2, locktypes2]
---

More standard lock types:

```text
  @lock/speech          Who can speak/pose/emit in this room
  @lock/listen          Who can trigger my @ahear/^-pattern actions
  @lock/command         Who can trigger my $-pattern commands
  @lock/leave           Who can leave this object (or room, via exits/@tel)
  @lock/drop            Who can drop this object
  @lock/dropin          Who can drop objects into this location.
  @lock/give            Who can give this object
  @lock/from            Who can give things to this object
  @lock/pay             Who can give pennies to/buy from this object
  @lock/receive         What things can be given to this object
  @lock/follow          Who can follow this object
  @lock/examine         Who can examine this object if it's VISUAL
  @lock/chzone          Who can @chzone to this object if it's a ZMO
  @lock/forward         Who can @forwardlist a message to this object
  @lock/filter          Controls if the message %0 should be filtered
  @lock/infilter        Controls if the message %0 should be infiltered
  @lock/control         Who can control this object (only if set; non-player)
  @lock/dropto          Who can trigger this container's drop-to.
  @lock/destroy         Who can destroy this object if it's DESTROY_OK
  @lock/interact        Who can send sound (say/pose/emit/etc) to this object
  @lock/take            Who can get things contained in this object
  @lock/mailforward     Who can forward mail to this object via @mailforward
  @lock/chown           Who can @chown this CHOWN_OK object?

```

**See also:** @lock, @lset, @clock, FAILURE
