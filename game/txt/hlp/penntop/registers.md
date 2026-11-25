---
topics: [REGISTERS]
---

A register is essentially a little reserved piece of computer memory that can hold some variable information that you want to pass on to another command. There are thirty registers on the MUSH available via the %0-%9 substitutions and v(0)-v(9) and r(0,args) to r(29,args). There are also many setq registers available via %q- substitution (%q0 - %q9, %qA - %qZ and arbitrary names), or the r() function.

The basic registers are filled with information that matches the wildcard pattern of the command trigger. (Before you say "Huh?", here's an example.)

```text
    > &COMMAND me=$command *=*:@emit %0 is in register 0 and %1 is in register 1.
    > command whee=blert foo
    whee is in register 0 and blert foo is in register 1.

```

Continued in 'help registers2'.
