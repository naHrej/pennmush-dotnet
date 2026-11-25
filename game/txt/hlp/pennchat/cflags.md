---
topics: [CFLAGS(), CLFLAGS()]
---

cflags(<channel>)
cflags(<channel>, <object>)
clflags(<channel>)
clflags(<channel>, <object>)

With one argument, cflags() returns a list of priv flags set on the given channel, represented as a string of characters. See 'help @channel privs' for the list. You must be able to see the channel to use this function.

With two arguments, cflags() returns a list of flags for that object on that channel, currently a string consisting of zero or more of "G" (Gag), "Q" (Quiet), "H" (Hide) and "C" (Combine). You must be able to see that channel and to examine the object to use this function. If the object is not on the channel, an error is returned.

The clflags() versions return a space-separated list of flag names, rather than a string of flag characters.

**See also:** cstatus(), channel functions
