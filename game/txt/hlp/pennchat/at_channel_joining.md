---
topics: ["@CHANNEL JOINING", "@channel/list", "@channel/what", "@channel/who", "@channel/on", "@channel/join", "@channel/off", "@channel/leave"]
---

@channel/list[/on|/off][/quiet] [<prefix>]
@channel/what [<prefix>]
@channel/who <channel>
@channel/on <channel>[=<player>]
@channel/off <channel>[=<player>]

@channel/list shows a list of all the channels you can see, along with some basic information such as whether you are on the channel, how it's locked, etc. 'help @channel list' explains the output in detail. If a <prefix> is given, only channels whose names begin with <prefix> are shown. If the /on switch is given, only channels you've joined are shown. If /off is given, channels you are on will not be shown. The /quiet switch shows just a list of channel names, without any extra information.

@channel/what shows the name, description, owner, priv flags, mogrifier and buffer size for all channels, or all channels whose names begin with <prefix> if one is given.

@channel/who lists all the players on the given channel.

@channel/on and @channel/off add or remove you from the given <channel>. You only hear messages for channels you're on, and most channels require you to join them before you can speak on them. /join and /leave are aliases for /on and /off.

Continued in 'help @channel joining2'.
