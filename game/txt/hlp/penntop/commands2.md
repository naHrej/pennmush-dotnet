---
topics: [$-COMMANDS2, MACROS2, USER-DEFINED2]
---

Any number of wildcards, * and ?, may be in present in <command pattern>. A * matches any number of characters (including none), and ? matches exactly one character. When the action list is executed, the values on the stack in %0-%9 and v(10)-v(29) are the portions of what the user types that match the first 30 *'s or ?'s. You can also match a regular expression rather than wildcards by setting the REGEXP attribute flag on <attribute>; see 'help regexps' for details. When using named regexp captures, the named arguments can be accessed via r(<name>, args).

For example, to make a 'wave' command, you could do the following:
```text
    > &DO_WAVE me=$wave *: pose waves to %0.
```

You could then type:
```text
    > wave Guest
    Rhyanna waves to Guest.

```

If a command would match, but the enactor can't pass the lock, the object may define generic failure behavior by setting the COMMAND_LOCK`FAILURE, COMMAND_LOCK`OFAILURE, and/or COMMAND_LOCK`AFAILURE attributes. These are triggered on all objects with matching commands and failing locks, but only if no command successfully matched, and take the place of the usual "Huh?" message.

*BE SURE TO @LOCK/USE ME==ME IF YOU SET $-COMMANDS ON YOURSELF!*

**See also:** GLOBAL COMMANDS, EVALUATION ORDER, STACK, SUBSTITUTIONS, WILDCARDS, @lock
