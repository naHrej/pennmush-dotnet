---
topics: [PARENTS2]
---

Note that the only properties inherited are attributes and locks. In particular, flags and exits are NOT inherited from the parent object. Also, commands which walk the attribute list (such as "examine", the LATTR() function, the HASATTR() function, @set, and @edit) only affect attributes that are on the object itself, although there are variants which also check parents (examine/parent, lattrp(), hasattrp(), etc).

There are some limitations to the use of @parent. The most important is that ^-pattern checking is not done on the parent of an object, unless the object is set LISTEN_PARENT. For the purposes of automated game checks, the following attributes are not inherited:
```text
    CHARGES, EALIAS, LALIAS, LAST, LASTSITE, LISTEN, QUEUE, RQUOTA, SEMAPHORE, and STARTUP.

```

Continued in 'help parents3'.
