---
topics: [MATCHING]
---

Matching is the process the MUSH uses to determine which object you mean when you try to do something with an object. Different commands and functions do matching in different ways, but most will allow you to specify an object as:
```text
    * its dbref (#7) or objid (#7:123456789)
    * the string "me" (yourself)
    * the string "here" (the room you're in)
    * a '*' followed by a playername (*javelin)
    * its full name (Box of chocolates)
    * part of any word in its name, if nothing else shares that part (Box)

```

Using the object's name only works if the object is near you.

You can usually qualify an object with an adjective (English matching) to help the MUSH determine which object you mean Adjectives include:
```text
    * my <obj> - an object you're carrying
    * this <obj> - an object in your location (also: this here <obj>)
    * toward <exit> - an exit in your location
    * 1st, 2nd, etc. <obj> - one of a set of objects with the same names. Objects are ordered in the order in which they're listed in your inventory, room contents, and exit list (in that order). If there aren't enough objects, this will fail.	You can use an adjective with an ordinal (my 1st <obj>, this 2nd <obj>, etc)

```

In commands that take a list of space-separated names (like page), you'll need to enclose names with spaces in "double quotes". The same is true on the login screen. For example:
```text
    page "Leeroy Jenkins"=Stop doing that.
```
