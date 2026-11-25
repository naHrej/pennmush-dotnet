---
topics: [MAILLIST()]
---

maillist([<player>, ]<message-list>)

maillist() returns a list of all <player>'s @mail messages which match the given <message-list> (the same as @mail/list <message-list>). If no <player> is given, the executor's mail is matched. The <message-list> argument is described in 'help mail'.

Examples:
```text
    > think maillist()
    0:1 0:2 0:3
    > think maillist(all)
    0:1 0:2 0:3 1:1 1:2
    > think maillist(1:)
    1:1 1:2

```

**See also:** mail(), mailfrom()
