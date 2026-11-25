---
topics: ["@lock-host"]
---

HOST LOCKS

You can check to make sure an object is owned by a player connected from a specific host or IP address using the following:

```text
    @lock <object>=ip^<ipaddress>
    @lock <object>=hostname^<hostname>

```

<ipaddress> and <hostname> can contain wildcards. <object> must be able to see the LASTIP attribute (for ip locks) or LASTSITE attribute (for hostname locks) on the enactor's owner.

For example:
```text
    @lock <object>=ip^127.0.0.1
      This locks <object> to players (and the objects of players) currently connected from the computer the MUSH is running on.

```

**See also:** ipaddr(), hostname(), LASTSITE
