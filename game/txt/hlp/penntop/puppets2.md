---
topics: [PUPPETS2]
---

An example of a puppet:

> @create Punch
Created: Object #18.
> drop Punch
Dropped.
> @set punch=puppet
Punch is now listening.
Punch - PUPPET set.
> @force punch=go north
Punch has left.
Punch> The Finishing Place
Punch>
Punch> Obvious exits:
Punch> Door <S>
> @force #18=:waves hello
Punch> Punch waves hello
> #18 say Hello.
Punch> You say, "Hello."

To have an object relay things it hears to players other than its owner, use @forwardlist.

**See also:** PUPPET, @force, DBREF
