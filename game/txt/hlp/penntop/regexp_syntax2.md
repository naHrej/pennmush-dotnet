---
topics: [regexp syntax2]
---

A regular expression is a pattern that is matched against a subject string from left to right. Most characters stand for themselves in a pattern, and match the corresponding characters in the subject.

There are two different sets of meta-characters: those that are recognized anywhere in the pattern except within square brackets, and those that are recognized in square brackets.  Outside square brackets, the meta-characters are as follows:

```text
       \      general escape character with several uses
       ^      assert start of subject
       $      assert end of subject
       .      match any character except newline
       [      start character class definition
       |      start of alternative branch ("or")
       (      start subpattern
       )      end subpattern
       ?      0 or 1 quantifier (after a unit to quantify) or, minimal match (after a quantifier) or, extends the meaning of ( after a (
       *      0 or more quantifier
       +      1 or more quantifier

```

Continued in '"help regexp syntax3'.
