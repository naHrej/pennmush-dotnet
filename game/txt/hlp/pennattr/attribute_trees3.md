---
topics: [ATTRIBUTE TREES3, ATTR TREES3, ATTRIB TREES3, `3]
---

The second benefit of attributes trees is convenient access control. Attribute flags that restrict attribute access or execution (no_inherit, no_command, mortal_dark, wizard) propagate down attribute trees, so if a branch is set mortal_dark, mortals can not read any of its leaves or subbranches either.

Attribute flags that grant access (e.g. visual) do NOT propagate down trees.

These properties make attribute trees ideal for data attributes:
> &DATA bank = Data for each depositor is stored here, by dbref
> @set bank/DATA = no_command
> &DATA`#30 bank = $2000 savings:$1000 loan @ 5%
etc.

They're also handy for things like character attributes:
> @attribute/access CHAR = wizard mortal_dark no_clone no_inherit
> &CHAR #30 = Character data
> &CHAR`SKILLS #30 = coding:3 documentation:1 obfuscation:5
etc.

See 'help attribute trees4' for information about @parent and attribute trees.
