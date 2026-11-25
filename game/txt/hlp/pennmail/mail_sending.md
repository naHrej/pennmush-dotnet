---
topics: [mail-sending, "@mail/send", "@mail/fwd"]
---

@mail[/switch] <player-list>=[<subject>]/<msg>
```text
        This sends the message <msg> to all players in <player-list>.

        If no subject is given, the message subject is the beginning of the message itself. To include a literal / in the subject, double it up (//).

        All function substitutions are valid in <msg> including mail(#) which will allow you to forward mail you have received to other users.

        The following switches are available:
                /send   - same as no switch
                /urgent - mail is marked as "Urgent"
                /silent - no notification to sender that mail was sent
                /nosig  - no mail signature

        If you have an @mailsignature attribute set on yourself, its contents will be evaluated and appended to the message unless the /nosig switch is given.

```

@mail/fwd <msg-list>=<player-list>
```text
        This sends a copy of all the messages in <msg-list> to all the players in <player-list>. The copy will appear to have been sent by you (not the original sender), and its status will be "Forwarded".
```
