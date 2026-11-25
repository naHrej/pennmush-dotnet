---
topics: [regexp syntax5]
---

Quantifiers specify repetition of characters. Four are available:
```text
       *    match 0 or more of whatever came before
       +    match 1 or more of whatever came before
       ?    match 0 or 1 of whatever came before
```

	 {m,n}  match between 'm' and 'n' of whatever came before. if 'm' is omitted, it matches between 0 and 'n'. if 'n' is omitted, matches at least 'm'. Note the MUSH parser often requires escaping the braces and the comma.

Quantifiers are usually greedy -- they match as much as possible. Adding a ? after a quantifier causes it to match as little as possible instead.

Continued in '"help regexp syntax6'.
