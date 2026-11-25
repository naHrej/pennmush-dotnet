---
topics: [PUPPETS]
---

A thing is turned into a puppet by setting the PUPPET flag on it. A puppet object is an extension of its owner and relays everything it sees and hears to its owner, except if it is in the same room as the owner (a puppet with the VERBOSE flag will relay even if it's in the same room). Things relayed by the puppet will be prefixed by the name of the puppet.

Puppets are useful for keeping track of what is going on in two rooms at once, as extensions of a player (such as a pet, for example), or for testing code.

You can control your puppets using the @force command. It is important to remember the DBREF numbers of your puppets so you can control them even if they are not in the same room with you. You can also have your puppets follow you by using the 'follow' command.

See 'help puppets2' for examples.
