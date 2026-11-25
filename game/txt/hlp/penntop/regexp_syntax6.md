---
topics: [regexp syntax6]
---

Outside a character class, a backslash followed by a digit greater than 0 (and possibly further digits) is a back reference to a capturing subpattern earlier (i.e. to its left) in the pattern, provided there have been that many previous capturing left parentheses. A back reference matches whatever actually matched the capturing subpattern in the current subject string, rather than anything matching the subpattern itself. So the pattern

```text
    (sens|respons)e and \1ibility

```

matches "sense and sensibility" and "response and responsibility", but not "sense and responsibility".

You can give names to subpatterns and refer to them that way instead of using numbers.

(?P<NAME>subexpr) (Note: Literal <>'s) is a named capture, and (?P=NAME) refers back to it. The above pattern might be written:

(?P<word>sens|respons)e and (?P=word)ibility

In a $-command, the value of the named pattern can be accessed via the r(<name>, args). Softcode functions which work with regexps allow you to access the named subpatterns via $<NAME> (the <> are literal here).

Continued in '"help regexp syntax7'.
