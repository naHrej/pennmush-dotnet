---
topics: [BOOLEAN2]
---

If tiny_booleans is...
```text
  no                       FALSE: null string, 0, any negative db
                           TRUE:  everything else
  yes                      TRUE:  numbers other than 0, strings beginning with numbers other than 0
                           FALSE: everything else

```

Or, put another way:
```text
  Value                 tiny_booleans=no        tiny_booleans=yes  Gotcha
  0                     FALSE                   FALSE
  non-zero number       TRUE                    TRUE
  #<non-negative>       TRUE                    FALSE               *
  #<negative>           FALSE                   FALSE

  null string           FALSE                   FALSE
  0<non-numbers..>      TRUE                    FALSE               *
  <non-numbers...>      TRUE                    FALSE               *

```

Continued in 'help boolean3'.
