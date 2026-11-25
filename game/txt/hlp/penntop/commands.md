---
topics: [$-COMMANDS, MACROS, USER-DEFINED COMMANDS]
---

User-defined commands can be created by setting $-commands on players, things, and rooms. Exits are not checked for $-commands. To set a $-command:

```text
    &<attribute> <object>=$<command pattern>:<action list>

```

Whenever someone in the same room as the object types the command name, the action list is carried out by the object, as long as:

- the person typing the command is not set GAGGED, passes the object's @lock/use and @lock/command
- the object is not set NO_COMMAND or HALT

$-commands can be run by players, things, and exits.

Such attributes can also be @triggered as if the $<command name>: did not exist.

It is recommended that <command pattern> not begin with "@", as many of PennMUSH's built-in commands start with "@". Conventionally, global $-commands are often named with the "+" prefix, and local $-commands commonly have a "+" or "." prefix.

Continued in 'help user-defined2'.
