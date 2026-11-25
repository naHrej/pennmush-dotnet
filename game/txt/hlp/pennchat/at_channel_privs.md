---
topics: ["@channel privs", channel-privs]
---

The <privs> for @channel/add and @channel/privs should be a space-separated
list of priv names, or a string of priv chars, from the list below:

* "player" (P) - players may use the channel
* "object" (O) - non-players may use the channel
* "admin" (A) - only royalty/wizards/chat_privs may use the channel
* "wizard" (W) - only wizards may use the channel
* "quiet" (Q) - channel will not show connection messages
* "open" (o) - you may speak even if you aren't listening to the channel
* "hide_ok" (H) - you may hide from the channel who list
* "notitles" (T) - chantitles are not displayed in channel messages
* "nonames" (N) - player names are not displayed in channel messages
* "nocemit" (C) - @cemit is prohibited on the channel
* "interact" (I) - Interaction rules (defined in local.c) are applied to the channel
* "disabled" (D) - noone can join or speak on the channel

The default privs are determined by the 'channel_flags' @config option.

Example:
```text
    > @channel/add Public=player quiet open nocemit

```

**See also:** cflags(), clflags(), channel functions
