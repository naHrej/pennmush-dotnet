---
topics: [PUEBLO()]
---

pueblo(<player|descriptor>)

This function returns 1 if the given player or descriptor is currently Pueblo-enabled, and 0 otherwise.

If used on a player/descriptor which is not connected, pueblo() returns #-1 NOT CONNECTED. Mortals can only give a <descriptor> for their own connections (but can give any <player> arg), while See_All objects can check any descriptor.

When used with a <player> argument, the most recently active connection is used if the <player> is logged in more than once.

**See also:** terminfo(), html(), PUEBLO
