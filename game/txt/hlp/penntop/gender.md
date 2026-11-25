---
topics: [GENDER, SEX]
---

Gender on a MUSH is entirely up to you. You can set yourself (or any of your objects) to be male, female, neuter, or plural. If whatever is in the SEX attribute is not recognizable, the MUSH will assume the object is neuter. Setting a gender attribute will enable pronoun substitution by the MUSH. The SEX attribute is visual to anyone who wants to see it.

The obj(), subj(), poss() and aposs() functions return different pronouns for an object based on its @sex, and the %o, %s, %p and %a substitutions return the same pronouns for the enactor.

**See also:** @sex, SUBSTITUTION
