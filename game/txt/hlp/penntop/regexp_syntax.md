---
topics: [REGEXP SYNTAX]
---

PennMUSH uses PCRE for its regular expression engine. PCRE is an open source library of functions to support regular expressions whose syntax and semantics are as close as possible to those of the Perl 5 language. The text below is excerpted from its man page. PCRE was written by Philip Hazel <ph10@cam.ac.uk>, and is Copyright (c) 1997-1999 University of Cambridge, England. You can find it at ftp://ftp.csx.cam.ac.uk/pub/software/programming/pcre/

(Note that in PennMUSH, if the regular expression is in an eval'd context (like an argument to regmatch), you'll have to do a lot of escaping to make things work right. One way to escape an argument like %0 is: regeditall(%0,\\W,\\$0) or similar).

Regular expression matching in PennMUSH can be used on user-defined command or listen patterns. In this usage, regular expressions are matched case-insensitively unless the attribute has the CASE flag set. Regular expressions can also be matched in MUSHcode using regmatch(), regrab(), regedit, etc. function families, which usually come in case-sensitive and case-insensitive versions.

Continued in 'help regexp syntax2'.
