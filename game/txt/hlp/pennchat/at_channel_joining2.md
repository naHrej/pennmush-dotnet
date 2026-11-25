---
topics: ["@CHANNEL JOINING2", "@channel/gag", "@channel/ungag", "@channel/hide", "@channel/unhide", "@channel/mute", "@channel/combine", "@channel/uncombine"]
---

@channel/gag [<channel>][=<yes|no>]
@channel/mute [<channel>][=<yes|no>]
@channel/hide [<channel>][=<yes|no>]
@channel/combine [<channel>][=<yes|no>]

@channel/gag allows you to stay on a channel but stop receiving messages on it. Channels are automatically ungagged when you disconnect. You cannot speak on channels you're gagging unless they have the "open" priv.

Channels without the 'quiet' priv broadcast messages when players connect or disconnect from the MUSH. You can use @channel/mute to suppress these messages if you don't want to see them.

On channels with the 'hide_ok' priv, @channel/hide lets you hide from the @channel/who list if you pass the channel's @clock/hide.

Connect and disconnect messages across all channels you have marked with @channel/combine will be combined into a single message with a |-separated list of all channel names. Only players can use this.

For all four of these commands, you can specify a single channel to affect, or omit <channel> to affect all channels you're on. To undo the gag/mute/hide, either use @channel/<switch> [<channel>]=no or
@channel/un<switch> [<channel>].

**See also:** @channel/who, cstatus(), cowner(), cflags(), channels(), @channel/privs
