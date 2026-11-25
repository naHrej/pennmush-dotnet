---
topics: [DROP-TOS, DROPTOS]
---

When you use the @link command on a room, it sets another room or object as the DROP-TO location. By default, any non-STICKY object that someone drops in the room will automatically be transported to the drop-to location, rather than staying in the room. Any STICKY object dropped in the room will go to its home.

If the room is set STICKY, objects will stay in the room until the last player leaves or disconnects, at which point they will be transported as described above.

Drop-tos work on things and players alike.

If the room has a @lock/dropto set on it, only objects that pass the lock will be transported (either immediately or when the last player leaves if the room is STICKY). This can be used to prevent the dropto from acting on, say, objects containing connected players.

**See also:** @link, STICKY, LINK_OK, @lock
