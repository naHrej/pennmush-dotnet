---
topics: [DECOMPOSE()]
---

decompose(<string>)

decompose() works like escape() with the additional caveat that it inserts parse-able characters to recreate <string> exactly after one parsing. It takes care of multiple spaces, '%r's, and '%t's.

Example:
```text
    > think decompose(This is \[a [ansi(y,test)]\][space(3)])
    This is \[a%b[ansi(y,test)]\] %b%b

```

**See also:** @decompile2, escape(), secure(), ]
