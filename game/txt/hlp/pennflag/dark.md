---
topics: [DARK]
---

Flag:  DARK  (all types)

When a room is set DARK, only objects which are set LIGHT will show up in the rooms contents list when someone "look"s in the room. Exits which are set DARK do not show up in a room's Obvious Exits list.

DARK players and things do not show up in the contents list of rooms. Only Wizard players can set themselves dark. Puppets and Audible things with an @forwardlist will not be hidden by the flag.

Note that players and things still trigger enter/leave messages, etc, in DARK rooms.

If the "full_invis" @config option is on, DARK players and things will show up as "Someone" or "Something", respectively, when they speak.

In the past, players set DARK were automatically hidden from the WHO list via @hide, but this no longer happens. If you wish to disappear completely, you must use @hide after setting yourself DARK.

**See also:** LIGHT, @hide
