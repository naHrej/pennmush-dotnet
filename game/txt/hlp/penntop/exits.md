---
topics: [EXITS]
---

Exits are created with @open. They link rooms together, and can be used by players and things to move from one room to another with the GOTO command. Exits cannot move (though they can be @teleported to another room). They can have multiple aliases in their @alias. They are NOT checked for $-commands or ^-listens. Exits can have variable destinations; see 'help @destination' for more information.

You can change the destination of an exit with the @link command. home() returns the source room of an exit, and loc() returns its destination.

Setting an exit CLOUDY and/or TRANSPARENT causes its destination's description and/or contents to be shown after the exit's description when looked at.

Sound is propagated through exits which are set AUDIBLE, as long as their source room (home) is also set AUDIBLE.

**See also:** TYPES OF OBJECTS, type(), hastype(), @open, @link, @destination, AUDIBLE
