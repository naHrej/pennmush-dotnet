---
topics: [SWITCHES]
---

Commands can have "switches" which modify the behavior of the command. Switches are attached after the end of a command. For example, most people are familiar with the command

```text
    @lock me==me

```

The "enter" switch to @lock allows you to lock who can enter:

```text
    @lock/enter me==me

```

A command may have multiple switches:

```text
    @pemit/noeval/silent me=Hi!

```

Help on the switches available for a command is available in the help file for that command.
(If you are looking for information on @switch, see 'help @switch'.)
