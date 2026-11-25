---
topics: [RESTRICT]
---

Commands, functions and attributes can have their permission levels controlled in the mush config files, or by wizards from the game via @command, @function and @attribute.

In the config file, the syntax is:
```text
    restrict_command <command-name> <restriction> [" <error message>]
    restrict_function <function-name> <restriction>
    restrict_attribute <attribute-name> <restriction>

```

From the game:
```text
    @command/restrict <command-name>=<restriction> [" <error message>]
    @function/restrict <function-name>=<restriction>
    @attribute/access <attribute-name>=<restriction>

```

For commands, if <error message> is given, that message is sent to the player who runs it instead of a generic, unhelpful error message.

Continued in 'help restrict2'.
