---
topics: [SUBSTITUTIONS2, %2, %v, %w, %x]
---

If the ENACTOR's gender is set, you can use these substitutions to get the
right pronoun for him/her:
```text
     %s = subjective pronoun: he, she, it, they. Same as subj(%#)
     %o = objective pronoun: him, her, it, them. Same as obj(%#)
     %p = possessive pronoun: his, her, its, their. Same as poss(%#).
     %a = absolute possessive: his, hers, its, theirs. Same as aposs(%#).

    Case makes a difference: %S will return He, She, It, They.

```

Some attributes can be retrieved via substitutions:
```text
     %va-%vz = the contents of the object's VA-VZ attributes, respectively
     %wa-%wz, %xa-%xz = as above, for WA-WZ and XA-XZ
```

These are the equivilent of get(me/<attribute>).

Continued in 'help substitutions3'.
