---
topics: ["@lock/page", "@lock/speech", "@lock/mail", "@lock/mailforward", "@lock/interact"]
---

@lock/page
```text
    For players, things and rooms, you must pass this lock to page or @pemit to the object, or @remit inside it. Meaningless for exits.
```

**See also:** FAILURE, @haven

@lock/speech
```text
    Controls who can speak (via say, pose, @*emit or teach) inside an object. Meaningless for exits.
```

**See also:** FAILURE

@lock/mail
```text
    Controls who can send @mail to this object.
```

**See also:** @mail, FAILURE

@lock/mailforward
```text
    Controls who can forward @mail to this object via @mailforward.
```

**See also:** @mail, @mailforward, @lock/forward

@lock/interact
```text
    Controls whose indirect speech you'll hear (from say, pose, channels, @emit, etc). Does not block sound directed specifically at you, such as page, whisper, @pemit, etc; use @lock/page for those. Note that if sound is blocked by the interact lock, the speaker will not be informed.

```

**See also:** @lock, locktypes, lockkeys
