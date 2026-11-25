---
topics: [mogrify4]
---

For Talk Like a Pirate Day (Sep 19):

> @create Pirate Filter
> @chan/mogrifier public=Pirate Filter
> &MOGRIFY`BLOCK Pirate Filter=switch(%5,*ninja*,We don't need no ninjas!)
> &MOGRIFY`TITLE Pirate Filter=switch(poss(%#),her,Wench,Buccaneer)
> &MOGRIFY`SPEECHTEXT Pirate Filter=yo-hos
> &MOGRIFY`MESSAGE Pirate Filter=edit(%0,r,rrr)
> &MOGRIFY`CHANNAME Pirate Filter=(%1-Yarr!)

Output:
> +public Hello
(Public-Yarr!) Buccaneer Walker yo-hos, "Hello"

> +public :thinks it's talk like a pirate day?
(Public-Yarr!) Buccaneer Walker thinks it's talk like a pirrrate day?

> +public Pirates suck, Ninjas are better!
We don't need no ninjas!
(And the message is blocked, won't be sent out).

See 'help mogrify5' for more examples.
