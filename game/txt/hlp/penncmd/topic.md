---
topics: ["]"]
---

"]" is a special prefix which can be used before any command. It instructs the MUSH that it shouldn't evaluate the arguments to the command (similar to the "/noeval" switch available on some commands). For example:

> say [add(1,1)]
You say, "2"

> say \[add(1,1)\]
You say, "[add(1,1)]"

> ]say [add(1,1)]
You say, "[add(1,1)]"

> ]"[add(1,1)]
You say, "[add(1,1)]"

This can be used to pass unevaluated MUSHcode to softcoded commands without having to escape every special character, or to help objects set attributes to contain unevaluated code.

See 'help ]2' for more examples.

**See also:** lit(), decompose(), escape(), @command, }
