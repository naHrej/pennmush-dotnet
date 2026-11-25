---
topics: [QUEUE2]
---

There are several @config options which affect queueing.

The option 'player_queue_limit' controls how many action lists can be queued by one object at any given time. Wizards and objects with the Queue @power can queue more commands (equal to the player_queue_limit plus the current number of objects in the database, including garbage).

Normally each object has its own queue count, but if the 'owner_queues' option is enabled, objects share a queue count with their owner.

'queue_chunk' controls how many commands PennMUSH runs before checking again for incoming socket commands or connections.

It costs a certain number of pennies to queue an action list; the exact amount is set in the 'queue_cost' @config option. These pennies are returned after the action list is run. Sometimes, you'll lose a penny when queueing a command; the chance of this happening is controlled by the 'queue_loss' option.

**See also:** @ps, LOOPING, ACTION LISTS
