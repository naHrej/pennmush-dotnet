---
topics: [ACTION LISTS]
---

An "action list" is simply a list of MUSH commands which are run together, one after the other. Each command in the list is separated by a semicolon. Action lists appear in many places: in user-defined commands, triggered in @a-attributes by the MUSH, and even as arguments to other commands, like @switch and @dolist.

If part of the command (such as the text in an @emit, for example) contains a semi-colon, you may need to enclose that part in curly braces {}. You can also nest action lists inside each other by enclosing each action list in braces {}.

Substitution will be performed on the contents of action lists before they are executed.

See 'help action2' for some examples.
**See also:** @-ATTRIBUTES, VERBS, $-COMMANDS
