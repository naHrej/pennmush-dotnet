---
topics: [SUBSTITUTIONS3, %3, %L, %c, %u, %?, %=, %+]
---

Other substitutions:
```text
    %0-%9   = the contents of the REGISTERS 0-9, respectively
    %@ = the caller's dbref number. Initially same as %#, changes when something like ufun() is called.
    %! = the dbref number of the object the command is on (the EXECUTOR)
    %L = the dbref of the ENACTOR's location
    %c = text of the last command, _before_ evaluation
    %u = text of the last command, after evaluation, available to locks/hooks
    %? = The current function invocation and depth counts
    %= = The dbref/attribute currently being evaluated
    %+ = The number of arguments passed to the current ufun.
    %qN = the equivalent of r(N) for registers 0-9 and A-Z set by the setq() function
    %q<N> = the equivalent of r(N) for a named register set by the setq() function
    %iN = equivalent of itext(N), the list element for iter()/@dolist.
    %$N = equivalent of stext(N), the <string> in switch()/@switch.

```

See 'help substitutions4' for examples.
**See also:** EVALUATION, ENACTOR, EXECUTOR, DBREFS, REGISTERS, v()
