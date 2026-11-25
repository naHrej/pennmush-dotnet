---
topics: [ACTION2]
---

Example 1:
```text
    > @asuccess Gift=@pemit %#={The box pops open; surprise!} ; @name me=New Toy ; @desc me=A shiny new toy, just for %n!
    > take gift
    The box pops open; surprise!
    > look new toy
    New Toy
    A shiny new toy, just for Cyclonus!

```

Example 2:
```text
    > &TEST me=$test:@emit {Testing; testing; one, two.} ; @dolist 1 2 3={think Test %i0, success.}
    > test
    Testing; testing; one, two.
    Test 1, success.
    Test 2, success.
    Test 3, success.

```

**See also:** ATTRIBUTES, SUBSTITUTION, @asuccess, @dolist
