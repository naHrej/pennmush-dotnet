---
topics: [regexp syntax4]
---

A backlash can also be used for two useful assertions -- conditions that must be met at a particular point in a match:

```text
       \b     word boundary
       \B     not a word boundary

```

A word boundary is a position in the subject string where the current character and the previous character do not both match \w or \W (i.e. one matches \w and  the  other  matches \W), or the start
or end of the string if the first or last character matches \w, respectively.

Continued in '"help regexp syntax5'.
