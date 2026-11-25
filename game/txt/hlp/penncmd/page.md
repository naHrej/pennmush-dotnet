---
topics: [page]
---

page[/<switch>] [<player-list>=]<message>

This command sends a message to a player or list of players. If the player's name contains spaces, surround it with double-quotes. If you have already paged someone since connecting, just typing:

```text
    'page <message>' or 'page =<message>'

```

will send the message to the last person paged. You cannot page a player if they are set HAVEN or if you do not pass their @lock/page. In the latter case, the player's PAGE_LOCK`FAILURE, PAGE_LOCK`OFAILURE, and PAGE_LOCK`AFAILURE attributes will be activated if set.

Examples:
```text
    > page airwolf=hi there!
    You paged Airwolf with 'hi there!'.
    > page see, I don't have to retype the name.
    You paged Airwolf with 'see, I don't have to retype the name.'.
    > page "John Lennon" Ringo=Paul's fine!

```

Continued in 'help page2'.
