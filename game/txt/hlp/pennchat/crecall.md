---
topics: [CRECALL()]
---

crecall(<channel>[, <lines> [, <start line> [, <osep> [, <timestamps?> ]]]])

This function is the functional form of @channel/recall, and returns a string containing the recalled lines from the channel, separated by <osep> (which defaults to a space). If <timestamps?> is a true value, the recalled lines will be prefixed with the timestamp; otherwise, timestamps are omitted.

<lines> can be either a number of lines (in which case that many lines will be returned, starting from the <start line>th line), or a duration of time (for example, '5h' or '10m30s'), in which case all lines from the previous <lines> time are returned.

**See also:** @channel/recall, cbufferadd(), channel functions
