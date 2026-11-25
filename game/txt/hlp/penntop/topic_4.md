---
topics: [%, %b, %t, %%, SUBSTITUTIONS]
---

The % symbol is used in MUSH commands to indicate a substitution -- some other character(s) or words are substituted for whatever follows the % symbol. Some common substitutions are:

```text
     %b = a single space (just like [space(1)])
     %r = a blank line
     %t = A tab. Note that this may not look right on some screens.
     %# = dbref of the ENACTOR (object that set off the command)
     %n = the ENACTOR's name
     %N = the ENACTOR's name, first letter capitalized
     %~ = the ENACTOR's accented name, like accname(%#)
     %: = the ENACTOR's unique identifier, like objid(%#)
     %k = the ENACTOR's name, colored by @moniker, like moniker(%#)
     %% = a literal %

```

Case makes a difference in all substitutions; capitalizing the first letter after the % will capitalize the first letter of the resulting substitution.

Continued in 'help substitutions2'.
