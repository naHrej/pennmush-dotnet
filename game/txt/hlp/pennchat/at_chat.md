---
topics: ["@chat", +]
---

@chat <channel>=<message>
+<channel> <message>

The @chat command is used to speak on channels. Everyone on the channel will see your message, and it will be added to the channel's recall buffer, if it has one. If <message> begins with a ':' or ';' it will be posed (or semiposed) instead of spoken. You will usually need to join a channel before you can speak on it.

+<channel> <message> is short-hand for the @chat command.

Example:
> @chat pub=Hello
<Public> Mike says, "Hello"
> +pub :waves
<Public> Mike waves

**See also:** @channel, @cemit
