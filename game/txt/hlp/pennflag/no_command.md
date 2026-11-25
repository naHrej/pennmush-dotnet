---
topics: [NO_COMMAND]
---

Flag:  NO_COMMAND  (all types)

The NO_COMMAND flag disables the checking of $-commands on an object. The server runs faster when fewer objects are checked for $-commands; thus, any object which does not have $-commands on it should be set NO_COMMAND. Many MUSHes choose to have all objects initially set NO_COMMAND at creation. The flag has no effect on exits, which are never checked for $-commands.

There is also a NO_COMMAND attribute flag, which disables $-command AND ^-listen checking for a single attribute. See 'help attribute flags'.

**See also:** USER-DEFINED COMMANDS
