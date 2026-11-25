---
topics: ["%@", CALLER]
---

The caller is the object which causes an attribute to be evaluated (for instance, by using ufun() or a similar function). The substitution %@ evaluates to the caller's dbref. It's particularly useful for functions with side-effects, to check that the object evaluating the function has permission.

Example:
```text
    > &cmd_test Foo=$test: @emit ufun(Bar/fun_test)
    > &fun_test Bar=%n(%#) typed 'test', and [name(%@)](%@) ufun()'d this!
    > test
    Mike(#5) typed 'test', and Foo(#6) ufun()'d this!

    > &wizfun Foo=if(hasflag(%@, Wizard), ufun(wizfun2), #-1 Sorry)

```

**See also:** ENACTOR, EXECUTOR
