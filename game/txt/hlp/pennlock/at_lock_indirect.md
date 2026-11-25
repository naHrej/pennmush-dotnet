---
topics: ["@lock-indirect"]
---

INDIRECT LOCKS
An "indirect" lock allows you to lock something to the same thing as another object (very useful in setting channel locks; see help @clock):
```text
    @lock Second Puppet=@First Puppet
      This locks the object "Second Puppet" to whatever the object "First Puppet" is locked to. Normally, the lock type that is checked is the same as the lock on the first. You can specify a different lock type with @object/LOCKNAME. For example:
    @lock Second Puppet = @First Puppet/Use
      Second Puppet's basic lock now checks First Puppet's use lock.
```
