---
topics: [HOMES, HOME]
---

Every thing or player has a home, which is usually the room where it was created. You can reset your home or the home of any object you own with the @link command: @link [me | <object>]=<location>. You must also control <location>, unless that location (room or thing) is set ABODE or LINK_OK.

When a player types 'home', she is sent back to the home room. When a thing with the STICKY flag set on it is dropped, it also goes to its home location. Note that if the FIXED flag is set on a player, she cannot use the 'home' command.

You can create an exit that sends players home by doing:
```text
    > @link <exit name>=home
```

You can set the drop-to in a room to home by doing:
```text
    > @link <room dbref or "here">=home

```

The home of an exit is its source (the room it's located in). You can change the home/source of an exit by @teleporting it to another room.

The home of a room is its drop-to.

**See also:** DROP-TOS, @link, STICKY, LINK_OK, FIXED, EXITS, home(), loc()
