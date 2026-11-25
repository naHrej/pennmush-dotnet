---
topics: [MALIAS()]
---

malias([<delimiter>])
malias(<malias name>)
malias(<malias name>[,<delimiter>])

With no arguments, malias() returns the list of all malias names which are visible to the player. With two arguments, returns the list of dbrefs that are members of the given malias, delimited by <delimiter>.

With one argument, the behavior is ambiguous. If the argument matches a malias, returns the list of dbrefs that are memebrs of the malias, space-delimited. If not, it's treated as a no-argument case with a delimiter.
