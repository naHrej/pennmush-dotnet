---
topics: [mogrify2]
---

After that, mogrifiers which alter individual portions of the message are called. See 'help mogrify3' for details.

After all these mogrifiers are called, MOGRIFY`FORMAT is evaluated. It has the same use and arguments as @chatformat, but on a channel-wide level. It receives all mogrified text (if any), so is generally intended only for reformatting the whole channel text. It is also overriden by individual @chatformats (unless MOGRIFY`OVERRIDE returned true).

See 'help mogrify3' for info on specific mogrifiers, and 'help mogrify4' for some examples.
**See also:** cmogrifier()
