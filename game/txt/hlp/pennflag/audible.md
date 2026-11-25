---
topics: [AUDIBLE]
---

Flag:  AUDIBLE  (all types)

Objects which are set AUDIBLE forward sound in a number of ways. Only sound passing the object's @filter, and its @lock/filter, is forwarded. The lock receives the sound heard as %0. The forwarded sound is always prefixed with the object's @prefix.

If an AUDIBLE object has an @forwardlist attribute set, sound heard by the object is forwarded to the objects listed in the @forwardlist. See 'help @forwardlist' for more information.

When a THING is set AUDIBLE, any sound made inside it is broadcast to other objects in its location. This is useful for coding vehicles. Only sound passing the @filter is broadcast, and is prefixed with the thing's @prefix, or "From [name(<thing>)], " if no @prefix is set.

Setting the AUDIBLE flag on a ROOM activates audible exits in that room. EXITs which are set AUDIBLE propagate sound from their source room to their destination, prefixed with the exit's @prefix, or "From [name(home(<exit>))], " if no @prefix is set.

**See also:** @forwardlist, @filter, @prefix
