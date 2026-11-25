---
topics: [VERBS2, NONAME, NOSPACE]
---

Normally, the enactor's name and a space are prepended to the 'overb' and 'oxverb' attributes automatically. The NOSPACE attribute flag prevents a space being placed between the name and attribute value, and the NONAME attribute flag stops the name being added at all.

Example:
```text
    > @ouse Foo=has used Foo!
    > use Foo
    Sketch has used Foo!

    > @ouse Foo='s used Foo!
    > @set Foo/ouse=nospace
    > use Foo
    Sketch's used Foo!

    > @ouse Foo=Foo has been used by %n!
    > @set Foo/ouse=noname
    > use Foo
    Foo has been used by Sketch!

```

**See also:** @verb, attribute flags
