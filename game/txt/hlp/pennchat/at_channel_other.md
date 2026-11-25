---
topics: ["@CHANNEL OTHER", "@channel/title", "@channel/recall"]
---

@channel/title <channel>[=<message>]
@channel/recall[/quiet] <channel>[=<lines|duration>[, <start line>]]

@channel/title lets you set a channel title, which is shown before your name whenever you speak on a channel. If =<message> is omitted, your current title on the channel is shown. Use an empty <message> to clear your title.
The maximum length of titles is set in the 'chan_title_len' @config option.

@channel/recall shows you the most recent messages on the channel, if it has been given a recall buffer with @channel/buffer. With no extra arguments, the 10 most recent lines are shown. You can specify either a number of <lines> to show, or a <duration> (such as '1h30m') to show only that many lines/messages that recent. If <start line> is given, lines before the <start line>th will not be shown. Timestamps are shown with the messages unless /quiet is given.

**See also:** crecall(), cbufferadd(), @channel/buffer
