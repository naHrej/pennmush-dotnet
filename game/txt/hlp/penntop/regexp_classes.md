---
topics: [REGEXP CLASSES]
---

In a character class, you can use a number of additional keywords that match certain types of characters. The keywords are enclosed in [: and :], within the character class, so the whole thing looks like [[:NAME:]].

These keywords can be mixed with other things in the character class, like [ab[:digit:]], which will match 'a, 'b', or a digit. [:^NAME:] reverses the meaning of NAME - it expands to everything but characters that would match [:NAME:].

Some recognized NAMEs:
digit, for numbers. [[:digit:]] is the same as \d. [[:^digit:]] is the same as \D.
alpha, for letters.
alnum, for numbers and letters.
lower, for lower-case letters.
upper, for upper-case letters.
word, for word characters. [[:word:]] is the same as \w. [[:^word:]] is the same as \W.
space, for whitespace characters. [[:space:]] is the same as \s. [[:^space:]] is the same as \S.

Continued in 'help regexp classes2'.
