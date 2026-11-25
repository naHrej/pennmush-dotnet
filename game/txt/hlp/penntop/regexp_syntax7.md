---
topics: [regexp syntax7]
---

An assertion is a test on the characters following or preceding the current matching point that does not actually consume any characters. There are two kinds: those that look ahead of the current position in the subject string, and those that look behind it.

An assertion subpattern is matched in the normal way, except that it does not cause the current matching position to be changed. Lookahead assertions start with (?= for positive assertions and (?! for negative assertions. For example, Lookbehind assertions start with (?<= for positive assertions and (?<! for negative assertions.

Assertion subpatterns are not capturing subpatterns, and may not be repeated, because it makes no sense to assert the same thing several times. If an assertion contains capturing subpatterns within it, these are always counted for the purposes of numbering the capturing subpatterns in the whole pattern.

Continued in '"help regexp syntax8'.
