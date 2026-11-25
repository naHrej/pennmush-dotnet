---
topics: ["@CHANNEL ADMIN", "@channel/add", "@channel/privs", "@channel/describe", "@channel/buffer", "@channel/decompile"]
---

@channel/add <channel>[=<privs>]
@channel/privs <channel>=<privs>
@channel/describe <channel>=<description>
@channel/buffer <channel>=<lines>
@channel/decompile[/brief] <prefix>

@channel/add creates a new channel named <channel>, with the given <privs>. If you don't specify the <privs>, the defaults set in the channel_flags @config option are used. There may be a limit on the number of channels mortals can create, and a charge for doing so; see '@config chat'.

@channel/privs changes the privs for <channel>. See 'help @channel privs' for an explaination of all the possible priviledges. More specific and/or complex restrictions can be set via @clocks (channel locks).

@channel/describe sets a description for the channel, which is shown to players in @channel/what. Limited to 256 characters. If the desc includes commas, the whole desc should be enclosed in {}s.

@channel/buffer sets the maximum number of full-length lines (8192 bytes) that the channel will buffer for @channel/recall. Many more shorter lines may actually be buffered. Setting it to 0 turns off buffering.

@channel/decompile produces a decompile of all channels whose name begin with <prefix>, showing the commands needed to recreate the channels. If the /brief switch is also given, commands to re-add members of the channel are omitted.

Continued in 'help @channel admin2'.
