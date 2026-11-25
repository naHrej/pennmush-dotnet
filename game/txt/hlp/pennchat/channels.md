---
topics: [CHANNELS()]
---

channels([<delimiter>])
channels(<object>)
channels(<object>[, <delimiter>])

With no arguments, channels() returns the list of all channel names which are visible to the player. With two arguments, returns the list of channel names to which the object is listening, delimited by <delimiter>.

With one argument, the behavior is ambiguous. If the argument matches an object, it returns a space-separated list of channels to which the object is listening. If not, it's treated as a no-argument case with a delimiter.

If you can examine the object, the function returns all the channels it's on. Otherwise, it will only return those channels you can see that <object> is a member of with @channel/who.

**See also:** @channel/list, @channel/who, cwho(), channel functions
