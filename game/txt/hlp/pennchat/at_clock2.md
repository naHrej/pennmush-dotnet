---
topics: ["@clock2"]
---

Locks are checked as if they were set on the object attemping to pass them - for example, in the channel lock:
```text
    > @clock/join Public=test/1
```

the attribute "test" will be evaluated on the object trying to join the Public channel.

You can use indirect @clocks to lock a channel to an @lock on an object. Objects attempting to pass the lock must be able to see the indirect lock, so it's recommended you set either the lock or the object VISUAL. This channel can only be joined by objects which aren't set UNFINDABLE:

> @clock/join unfindchannel=@#10
> @lock/user:ChanJoinLock #10=!flag^unfindable
```text
  > @lset #10/ChanJoinLock=VISUAL   OR   @set #10=VISUAL

  @clock                  Corresponding default user: lock for object
  join                    ChanJoinLock
  speak                   ChanSpeakLock
  see                     ChanSeeLock
  hide                    ChanHideLock
  mod                     ChanModLock

```

If you want to store different locks for different channels on the same object, you can use a specific indirect lock instead of the default one:

> @lock/user:onechanneljoin #10=flag^foo
> @clock/join onechannel=@#10/onechanneljoin
> @lock/user:anotherchanneljoin #10=flag^bar
> @clock/join anotherchannel=@#10/anotherchanneljoin

**See also:** clock(), @lock, @channel/privs
