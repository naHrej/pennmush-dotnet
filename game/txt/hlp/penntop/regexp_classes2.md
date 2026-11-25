---
topics: [REGEXP CLASSES2]
---

These keywords (Or the corresponding \codes) should be used instead of explicit ranges where possible to improve portability. For example, [A-Za-z] and [[:alpha:]] are not the same thing in languages with accented characters.

Examples:
```text
    > say regmatch(foo_bar, lit(^[[:word:]]+$))
    You say "1"
    > say regmatch(foo bar, lit(^[[:word:]]+$))
    You say "0"

```

Other, less useful, character class keywords include ascii, cntrl, graph, print, punct, and xdigit.
