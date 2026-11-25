---
topics: [CWHO()]
---

cwho(<channel>[, <status>[, <skip gagged?>]])

This function returns the dbrefs of objects which are on <channel>. <status> can be one of "on", "off" or "all" to control whether online players, offline players, or all players are returned; it defaults to "on". (Things are always returned.) When used by mortals, hidden/dark players are shown as being offline.

If <skip gagged?> is given, and is true, gagged objects will not be included in the results.

**See also:** cstatus(), @channel/who, channel functions
