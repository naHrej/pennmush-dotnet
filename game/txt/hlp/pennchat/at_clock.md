---
topics: ["@clock"]
---

@clock/join <channel>[=<key>]
@clock/speak <channel>[=<key>]
@clock/see <channel>[=<key>]
@clock/hide <channel>[=<key>]
@clock/mod <channel>[=<key>]

The @clock command modifies the @lock-style lock on a chat channel. If no <key> is specified, the lock is removed. See 'help lockkeys' for an explaination of <key>.

The "join" lock restricts who can join the channel.
The "speak" lock restricts who can speak on the channel.
The "see" lock restricts who can see the channel on @channel/list
The "hide" lock restricts @channel/hide if the channel is hide_ok.
The "mod" lock restricts who can modify the channel. If you pass the mod lock on the channel, you can do anything short of deleting it.

When new channels are added, the mod lock is set to the creator/owner (=<creator>), and all other locks are unlocked.

Continued in 'help @clock2'.
