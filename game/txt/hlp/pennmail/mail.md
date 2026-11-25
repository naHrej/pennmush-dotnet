---
topics: [mail, "@mail"]
---

@mail[/<switches>] [<msg-list>[=<target>]]
@mail[/<switches>] <player-list>=[<subject>/]<message>

@mail invokes the built-in MUSH mailer, which allows players to send and receive mail. Pronoun/function substitution is performed on any messages you may try to send.

A <msg-list> is one of the following:
```text
        A single msg # (ex: 3)
        A message range (ex: 2-5, -7, 3-)
        A folder number and message number/range (ex: 0:3, 1:2-5, 2:-7)
        A sender (ex: *paul)
        An age of mail in days (ex: ~3 (exactly 3), <2, >1)
           "days" here means 24-hour periods from the current time.
        One of the following: "read", "unread", "cleared", "tagged", "urgent", "folder" (all messages in the current folder), "all" (all messages in all folders).

```

Unless a folder is explicitly specified, or all is used, only messages in the current folder will be counted.

A <player-list> is a space-separated list of recipients, which may be:
```text
        Player names
        Player dbref #'s
        Message #'s, in which case you send to the sender of that message.
        An alias name (see help @malias)

  See also the following topics:    mail-sending   mail-reading
      mail-folders   mail-forward   mail-other     mail-admin
      @malias        mail-reviewing @mailquota
```
