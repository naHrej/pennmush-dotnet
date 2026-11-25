---
topics: ["@CHANNEL ADMIN3", "@channel/mogrifier", chat mogrifying, "@chat mogrifying", mogrify, mogrifier]
---

@channel/mogrifier <channel>=<object>

@channel/mogrifier sets the mogrifier object for <channel>. <object> must be an object that you control.

Mogrifiers let you tweak every aspect of a channel's output, before it goes to individual players' @chatformats.

Before it begins mogrifying, three mogrifiers that alter the way chats are handled are run. First, <object>'s MOGRIFY`BLOCK attribute is called. If MOGRIFY`BLOCK returns a non-empty string, then the resultant string is sent back to the player, and no message is broadcast on the channel.

Next, MOGRIFY`OVERRIDE is called. If it returns a true value, player's individual @chatformats will not be called for the message.

The MOGRIFY`NOBUFFER attribute is then called. If it returns a true value, the message will not be included in the channel's recall buffer.

These three all set %0 as chat type, %1 as the channel name, %2 as the message, %3 as the player name, and %4 as the channel title.

Continued in 'help mogrify2'.
