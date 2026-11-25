---
topics: [ATTRIB-OWNERSHIP]
---

ATTRIBUTE OWNERSHIP

The latest person to set an attribute on an object is the owner of that attribute. If you lock an attribute, using the @atrlock command, only the person who owns the attribute will be able to alter the attribute. This allows you to create standard commands on objects and then @chown them to others without letting them alter them.

Attribute ownership is NOT changed when the object itself is @chown'ed. To change attribute ownership, you must use the @atrchown command.

You must control an object in order to set attributes on it.

**See also:** @atrlock, @atrchown, owner(), ATTRIBUTES
