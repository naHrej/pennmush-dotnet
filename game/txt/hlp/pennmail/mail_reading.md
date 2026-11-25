---
topics: [mail-reading, "@mail/read", "@mail/list", "@mail/cstats"]
---

@mail <msg #>
@mail/read <msg-list>
```text
        This displays messages which match the msg# or msg-list from your current folder.

```

@mail
@mail <msg-list, but not a single msg #>
@mail/list <msg-list>
```text
        This gives a brief list of all mail in the current folder, with sender name, time sent, and message status.
        The status field is a set of characters (ex: NC-UF+) which mean:
                N = New (unread) message
                C = Cleared message
                U = Urgent message
                F = Forwarded message
                + = Tagged message
        The opposites of these (read messages, etc.) are indicated with a '-' in the status field in that position.

```

@mail/cstats
```text
        Shows how many messages you have, in the same format as the automatic mail check when you connect.
```
