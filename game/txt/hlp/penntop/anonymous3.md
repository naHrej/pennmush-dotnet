---
topics: [ANONYMOUS3, LAMBDA3, "#LAMBDA3"]
---

You can also use lit() to avoid having the code evaluated twice, if needed. For example, this code, which returns all unlinked exits in a room:

&lunlinked me=filter(lit(#lambda/strmatch(loc(%0), #-1)), lexits(%0))

This approach is useful both for security in making it harder to evaluate a string that shouldn't be, and for making the code look nicer by not having to escape percent signs, brackets, and other special characters. However, it also makes it harder to build the code string on the fly. Use what's most appropriate.

Finally, a multiple argument example of #apply, which requires less escaping than #lambda for cases where you're just calling another function:

> think mix(#apply2/ansi, r g b, foo bar baz)

See 'HELP ANONYMOUS4' for a list of functions that support anonymous attributes.
