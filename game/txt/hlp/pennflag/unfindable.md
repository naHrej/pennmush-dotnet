---
topics: [UNFINDABLE]
---

Flag:  UNFINDABLE  (all types)

If a player is set UNFINDABLE, he cannot be found by the @whereis command. You also cannot use loc(), locate(), and similar functions to find his location, unless you have the see_all power or equivalent.

If a room is set UNFINDABLE, you cannot locate any of its contents via any means (@whereis, the loc() function, etc.) unless you are see_all.

Wizards, Royalty, and players with the Hide and Idle @powers who are idle for longer than the idle_timeout @config option who are set UNFINDABLE will automatically be @hidden.

**See also:** loc(), @whereis, @hide
