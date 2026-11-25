---
topics: [BOOLEAN3]
---

Examples (assuming tiny_booleans is "no"):
```text
    not(foo) = 0
    not(<null string>) = 1
    not(-66) = 0
    not(0) = 1
    not(#-1) = 1
    not(#12) = 0
```

And so on...
(note: These rules only apply when a function expects a Boolean value, not for strings that expect other values.)

**See also:** BOOLEAN FUNCTIONS, not(), t()
