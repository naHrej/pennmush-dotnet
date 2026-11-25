---
topics: ["%#", %n, %k, %~, "%:", ENACTOR]
---

The enactor is the object which causes something to happen. This is an important concept in MUSH, because the way many commands work will depend on who enters the command (ie, who the enactor is). Note that while the enactor is the object which -caused- code/a command to run, it is not necessarily the object which is running the code (that's the executor). Any type of object can be an enactor.

There are eight %-substitutions that involve the enactor:
```text
    %# = the enactor's dbref
    %n = the enactor's name
    %~ = the enactor's accented name
    %k = the enactor's name, colored by their @moniker (if any)
    %: = the enactor's unique identifier, like objid(%#)
    %a, %o, %p, %s = pronoun substitutions, based on the enactor's @sex. See 'help gender' for more information.

```

If, for example, you have an @osuccess on an object that includes the %n subtitution, whenever someone picks up the object, that %n will evaluate to the name of the enactor (the person who typed 'get <object>' in this case).

**See also:** EXECUTOR, CALLER, SUBSTITUTIONS, DBREF
