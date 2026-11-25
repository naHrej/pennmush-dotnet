---
topics: [TAGWRAP()]
---

Function: tagwrap(<name>[, <parameters>], <string>)

This function outputs <string>, wrapped in the <name> HTML/Pueblo tag with the specified <paramaters>.

Example:
```text
    tagwrap(a,href="http://download.pennmush.org",PennMUSH Downloads)]

```

Will output (in HTML):
```text
    <a href="http://download.pennmush.org">PennMUSH Downloads</a>

```

A particularly important use of this function is tagwrap(pre, <string>). Because Pueblo works like an html browser, spaces and tabs are compressed to a single space. If you have code (a +who function, for example) that relies on exact spacing, surround its output with a tagwrap(pre,...) so that Pueblo will render it as "preformatted" text.

**See also:** tag(), endtag(), html()
