---
topics: [ANONYMOUS2, LAMBDA2, "#LAMBDA2"]
---

A typical usage of anonymous attributes would be to convert a list of dbrefs to names, as so:

> say map(#lambda/name(\%0), #3 #12 #23)
You say, "Joe Robert Sally"

The following version uses #apply instead:

> say map(#apply/name, #3 #12 #23)

Because the code is parsed twice, you can actually build parts of it in place, which is very convenient. Consider this implementation of a lattrval function, which is like lattr() but it only returns non-empty attributes:

> &lattrval me=filter(#lambda/hasattrval([decompose(before(%0, /))], \%0), lattr(%0))

The first time '#lambda/hasattrval([decompose(before(%0, /))], \%0)' is parsed in a call like 'u(lattrval, #1234)', it is turned into '#lambda/hasattrval(#1234, %0)', thus avoiding the need for a setq() or the like to store the top-level %0 for use in a real attribute called by filter(). However, this can lead to problems with evaluating un-trusted code. Use decompose() where neccessary.

See 'HELP ANONYMOUS3' for more examples.
