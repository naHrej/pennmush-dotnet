---
topics: [REGEXP, REGEXPS]
---

(This help text is largely from TinyMUSH 2.2.4, with permission)

The majority of matching in MUSH is done with wildcard ("globbing") patterns. There is a second type of matching, using regular expressions, that is available in certain circumstances.

For attributes that are $-commands or ^-listen-patterns, setting that attribute "regexp" (with '@set <object>/<attribute>=regexp') causes patterns to be matched using regular expressions rather than globbing. In addition, the function regmatch() performs regular expression matching.

In a regular expression match, the substring of the string which matched the regexp pattern is %0; %1 through %9 are the substrings of the string which matched parenthesized expressions within the regexp pattern.

Continued in 'help regexps2'.
