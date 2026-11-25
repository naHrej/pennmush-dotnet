---
topics: [CBUFFERADD()]
---

cbufferadd(<channel>, <text>[, <spoof>])

Adds text to a channel buffer without broadcasting it to all people on the channel. If <spoof> is true, it will spoof the enactor (%#).

This function is only usable by objects that pass the channel's @clock/mod, and who can use the @cemit command (or @nscemit if <spoof> is true).

**See also:** crecall(), cbuffer(), @channel/recall, @channel/buffer, channel functions
