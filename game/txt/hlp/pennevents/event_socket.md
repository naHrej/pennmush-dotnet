---
topics: [EVENT SOCKET]
---

socket`connect (descriptor, ip)
```text
      Triggered when a socket first connects to the port. Using both this and player`connect could be spammy. This happens when a connecting socket sees the connect screen.

```

socket`disconnect (former descriptor, former ip, cause of disconnection, recv bytes/sent bytes/command count)
```text
      Triggered when a socket disconnects. Using this and player`disconnect could be spammy.

```

socket`loginfail (descriptor, IP, count, reason, playerobjid, name)
```text
      Triggered when a login attempt fails. <count> is the number of fails in the past 10 minutes. If used in conjuction with the config option connect_fail_limit, then any failures after the limit is reached will NOT trigger socket`loginfail. If the connect is a failed attempt to log into a valid player, <playerobjid> will be set to that objid. Otherwise it will be set to #-1. <name> is the name the connection attempted to connect with, and is only set when <playerobjid> is #-1.

```

socket`createfail (descriptor, ip, count, reason, name[, error])
```text
      Triggered when a player create attempt fails. <count> is the # of fails caused by this ip. If the failure is from an attempt to register a player via email, the error code of the mailer program is provided as <error>.

    A sitelock rule with deny_silent will not trigger socket`createfail or socket`createfail.
```
