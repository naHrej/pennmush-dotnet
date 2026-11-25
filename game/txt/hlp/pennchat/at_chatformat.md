---
topics: ["@CHATFORMAT"]
---

@chatformat <object>[=<message>]

The chatformat attribute is evaluated when an object receives a channel message. If the attribute exists, its evaluated result is shown to the object instead of the default message. If the attribute exists but returns nothing, the object will not see anything.

Registers:
```text
    %0:  The 'type' of the message. It is a single character that will always be set:
           '"', ';' or ':' for say, semipose and pose, respectively.
           '|' for an @cemit.
           '@' for a "system" message - such as "Walker has connected."
    %1:  The channel name. e.g: "Public", "Admin", "Softcode"
    %2:  The message as typed (post-evaluation, if necessary) by the speaker. Be warned, though - if type is '@', then %2 will contain the entire message, and will include the name of the speaker that caused it.
    %3:  The speaker name, unless channel is set NO_NAME.
    %4:  The speaker's channel title, unless none is set, or the channel is NO_TITLE.
    %5:  The default message, as shown when no chatformat is set.
    %6:  The 'say' string for the message. This will usually be "says", unless altered by the SPEECHTEXT mogrifier.
    %7:  A space-separated list of extra options. Currently can contain: "silent" if a silent @cemit caused this message, otherwise "noisy"

    If the channel is NO_NAME, and the speaker either has no title or the channel is also set NO_TITLE, then %3 will be "Someone".

```

Continued in 'help @chatformat2'.
**See also:** @chat, @pageformat, @message, speak(), mogrify
