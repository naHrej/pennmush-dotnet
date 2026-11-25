---
topics: [SEMAPHORES5]
---

Normally, semaphores use the SEMAPHORE attribute. However, other attributes can be used, as long as they follow a few simple rules: If the attribute is already set, it has to have the same owner (God) and flags as the SEMAPHORE attribute would (typically no_inherit, no_clone, and locked - see 'help @set' and '@atrlock'), and have a numeric or empty value. If it's not set, it can't be one of the built in attributes (See @list attribs) unless, naturally, it is SEMAPHORE.

See the help on @wait, @notify and @drain for details, but, briefly, you can use named semaphores with <object>/<attribute> where you would normally just use <object> in those commands. This means you can't have an untimed semaphore on an attribute with a numeric name.

See 'help semaphores6' for an example.

**See also:** @wait, @drain, @notify
