---
topics: [mail-admin]
---

The @mail command can also take the following switches:

```text
    @mail/stats [<player>]    --  Basic mail statistics.
    @mail/dstats [<player>]   --  Also provides read/unread count.
    @mail/fstats [<player>]   --  Does all that, plus gives space usage.

    @mail/debug <action>[=<player>]
    @mail/nuke

```

Only wizards may stats players other than themselves.

The /debug switch does sanity checking on the mail database, and may only be used by a wizard. "@mail/debug sanity" just does the check; the command "@mail/debug clear=<player name or dbref number>" wipes mail for an object. "@mail/debug fix" attempts to repair problems noted in the sanity check.

The /nuke switch destroys the post office, erasing all @mail everywhere. It may only be used by God.
