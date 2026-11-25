---
topics: [EVALUATION2]
---

```text
    If still nothing is matched:
      User-defined commands on the player's personal zone
    If nothing, including zone commands, has been matched:
      Global exits
      Global user-defined commands: all $-commands in the Master Room are matched. Local commands are always checked first and ALWAYS negate global commands.
    If still nothing has matched, run huh_command to show a Huh? message

```

Because local $-commands overrule global $-commands, you can easily prevent a global $-command from working in a specific room by setting a copy of the global $-command in that room. Alternatively, if a global $-command is oddly not working in a room, you should check for copies of the command word in the room (using @scan). Wizards who want to ensure a global $-command always takes precedence over a local one should use @command/add and @hook/override, to make the command run as a regular game command instead of a softcoded global.

**See also:** @command, @hook, $-commands, huh_command
