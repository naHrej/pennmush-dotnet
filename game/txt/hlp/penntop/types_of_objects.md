---
topics: [TYPES OF OBJECTS, TYPES, OBJECTS]
---

Everything on a MUSH is an object in the MUSH database. There are four main types of objects: players, things (once called 'objects'), rooms and exits. You can see the type of an object when you 'examine' it, or with the type() function.

There is also a 'garbage' type, used for objects which have been created and then @destroyed. Garbage objects cannot be used in any way, and their dbrefs will be recycled (with a new objid) when something new is created.

For more information on any of the types, see 'help <type>'.

The @stats command lists how many objects of each type currently exits in the database.

**See also:** type(), hastype()
