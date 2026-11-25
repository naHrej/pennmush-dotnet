---
topics: [REGEXP EXAMPLES]
---

The regexp pattern '.' is equivalent to the wildcard '?'; it matches one and only one of an arbitrary character.

The regexp pattern '.*' is equivalent to the wildcard '*'; it matches zero or more arbitrary characters. To match one or more arbitrary characters, the regexp pattern is '.+'.

```text
  To match a string of numbers, use:       [0-9]+    or \d+
```

To match a string of letters only, use:  [A-Za-z]+ or \w+

See 'help regexp syntax' for a more detailed explanation.
