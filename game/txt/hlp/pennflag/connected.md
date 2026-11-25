---
topics: [CONNECTED]
---

Flag:  CONNECTED  (players)

This flag is used internally by the MUSH to track whether a player is currently connected. It cannot be set or cleared manually.

Prior to 1.8.5p6, mortal objects couldn't use hasflag(<player>, connected) to see if a player was online, and needed to instead use conn() or lwho(). Starting with that version, any object can use hasflag() to see if they can see that a player is online. Be careful, when coding on a privileged object, not to give away the presence of hidden players!

**See also:** conn(), lwho(), mwho()
