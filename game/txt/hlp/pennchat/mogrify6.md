---
topics: [mogrify6]
---

This example tweaks individual elements of the channel output, and then uses MOGRIFY`FORMAT to recolour the entire string.

> @chan/mogrifier Public=Mog
> &MOGRIFY`CHANNAME Mog=\[%1\]
> &MOGRIFY`PLATERNAME Mog=ucstr(%0)
> &MOGRIFY`FORMAT Mog=ansi(h,%5)

Stop anything by an admin from being altered by chatformats, and prevent anything said by Wiggles from being recorded in the channel's recall buffer.
> &MOGRIFY`OVERRIDE Mog=orlflags(%#, Wizard Royalty)
> &MOGRIFY`NOBUFFER Mog=switch(%#, pmatch(Wiggles), 1, 0)
