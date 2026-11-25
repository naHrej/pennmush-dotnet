---
topics: [STACK]
---

For those unfamiliar with the term stack, it refers to a programming data structure that follows a LIFO (Last-In-First-Out) principle. The stack in MUSH holds the REGISTERS. The first ten registers can be accessed via the %0-%9 %-substitutions and v(0)-v(29), while all 30 registers can be accessed via the r() function (r(0, args) up to v(29,args)).

When using regexp $-commands with named subpatterns, the named arguments can be accessed via r(<name>, args).

**See also:** REGISTERS, r(), v()
