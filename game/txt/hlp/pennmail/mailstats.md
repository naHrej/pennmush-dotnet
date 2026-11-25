---
topics: [MAILSTATS(), MAILDSTATS(), MAILFSTATS()]
---

mailstats([<player>])
maildstats([<player>])
mailfstats([<player>])

The mail*stats() functions return data like @mail/*stats does. You either must use this on yourself, or you must be a wizard. The information will be joined together as a space separated list of numbers.

Example:
> think mailstats(One)
<# sent> <# received>
> think mailfstats(One)
<# sent> <# sent unread> <# sent cleared> <# sent bytes> <# received>
<# received unread> <# received cleared> <# received bytes>

**See also:** folderstats()
