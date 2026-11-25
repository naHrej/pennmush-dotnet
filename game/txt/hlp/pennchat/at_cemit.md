---
topics: ["@CEMIT", "@NSCEMIT", CEMIT(), NSCEMIT()]
---

@cemit[/noisy|/silent][/noeval] <channel>=<message>
@nscemit[/noisy|/silent][/noeval] <channel>=<message>
cemit(<channel>, <message>[, <noisy>])
nscemit(<channel>, <message>[, <noisy>])

@cemit emits <message> on <channel>. It does not include your name. The channel prefix is included if the /noisy switch is given, and omitted if /silent is given - if neither is given, the default behaviour is controlled by the noisy_cemit @config option. The /noeval switch prevents <message> from being evaluated.

You must be able to speak on the channel, or have the See_All and Pemit_All @powers, to @cemit on the channel.

@nscemit is exactly the same, but does not produce nospoof information when used by players with the Can_spoof @power.

cemit() and nscemit() work the same as @cemit/silent and @nscemit/silent, respectively. If <noisy> is given as a true value, they work like @cemit/noisy and @nscemit/noisy, respectively, instead.

@cemit is intended for use in writing extended chat systems.

**See also:** @chat
