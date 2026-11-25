---
topics: [ATTRIBUTE FLAGS]
---

Attribute flags are set on an object's attributes using @set, or applied to attributes globally using @attribute. Their names (and, when applicable, the character used in examine as shorthand for the flag) are shown below.

These attribute flags restrict access, and are inherited down attribute trees (if FOO is no_command, FOO`BAR is automatically no_command too):

```text
  no_command ($)    Attribute won't be checked for $-commands or ^-listen patterns.
  no_inherit (i)    Attribute will not be inherited by the children of this object.
  no_clone (c)      Attribute will not be copied if the object is @clone'd.
  mortal_dark (m)   Attribute cannot be seen by mortals. This flag can only be set by royalty and wizards. "hidden" is a synonym.
  wizard (w)        Attribute can only be set by wizards. This flag can only be set by royalty and wizards.
  veiled (V)        Attribute value won't be shown on default examine, but is still otherwise accessible (for spammy attribs).
  nearby (n)        Even if the attribute is visual, it can only be retrieved if you're near the object (see 'help nearby()').
  locked (+)        Attribute is locked with @atrlock.
  safe (S)          Attribute can't be modified without unsetting this flag.

```

Continued in 'help attribute flags2'
