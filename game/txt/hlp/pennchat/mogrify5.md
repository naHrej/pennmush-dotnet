---
topics: [mogrify5]
---

For keeping a channel PG and safe, and altering the channel name from
<Public> to [Public], with a green "Public".

> @create PG Channel Mogrifier
> @chan/mogrifier public=PG Channel Mogrifier
> &BADWORDS PG=list of bad words
> &MOGRIFY`MESSAGE PG=regeditall(%0,\\b([edit(v(badwords),%b,|)])\\b,***)
> &MOGRIFY`CHANNAME PG=\[[ansi(g,%1)]\]
> &MOGRIFY`title PG=if(strlen(%0),\(%0\))

Output: (With a channel title of "Fast") ("Public" is green)
> +p Hello
[Public] (Fast) Walker says, "hello"
> +p what the list is going on bad bad?
[Public] (Fast) Walker says, "what the *** is going on *** ***?"

Combine MOGRIFY`FORMAT with speak() and you can have plenty of fun: On-channel language systems and more!

See 'help mogrify6' for more examples.
