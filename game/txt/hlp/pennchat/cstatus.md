---
topics: [CSTATUS()]
---

cstatus(<channel>, <object>)

Returns <object>'s status with respect to <channel>, which may be "Off", "On", or "Gag". You must either be able to examine the object, or it must visible be on the channel; "Off" is returned for objects that you cannot see on the channel.

**See also:** cwho(), cflags(), @channel/on, @channel/gag, channel functions
