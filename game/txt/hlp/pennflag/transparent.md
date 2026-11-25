---
topics: [TRANSPARENT]
---

Flag:  TRANSPARENT  (all types)

Transparent rooms show the "Obvious Exits" list in a long format; instead of showing all exits on a single line, each exit is displayed on its own line, with the name of its location. Any exits set OPAQUE are still shown in short format, so you can mix the two.

Transparent exits show the description and contents of the destination room, as well as the exit's desc, when looked at. If the exit is also set CLOUDY, the destination's contents is not shown.

Example:
```text
    > @set here=transparent
    > look
    A Room
    Obvious Exits:
      Yellow Brick Road leads to Oz.
      Looking Glass leads to Wonderland.
      Locked Door leads nowhere.

```

**See also:** OPAQUE, CLOUDY, @exitformat
