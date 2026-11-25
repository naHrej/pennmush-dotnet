---
topics: [MUXCOMSYS, MUXCOMM, ADDCOM, DELCOM, "@CLIST", COMTITLE, COMLIST]
---

PennMUSH offers some compatability commands for players more familiar with the MUX communication system. For help on PennMUSH's channel system, see 'help channels'. The following commands are available:

addcom <alias>=<channel>
```text
    Join <channel> if you aren't already on it, and add <alias> as an alias for <channel>.
```

<alias> WHO
```text
    Alias for @channel/who <channel>
```

<alias> OFF
```text
    Alias for @channel/gag <channel>
```

<alias> ON
```text
    Alias for @channel/ungag <channel>
```

<alias> <message>
```text
    Say <message> on the channel matching <alias>. Does not evaluate <message>. Equivilent to +<channel> <message>.
```

delcom <alias>
```text
    Deletes the alias <alias> and, if it's the last alias you have for the matching channel, leave the channel.
```

@clist[/full]
```text
    List all channels. Alias of @channel/list. /full does nothing.
```

comtitle <alias>=<title>
```text
    Alias of @channel/title <channel>=<title>
```

comlist
```text
    Lists all aliases and their matching channels.

```

See 'help muxcomsys2' for how to enable MUX com aliases.
**See also:** CHANNELS, @channel, @chat
