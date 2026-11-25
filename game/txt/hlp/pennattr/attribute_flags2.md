---
topics: [ATTRIBUTE FLAGS2]
---

These attribute flags grant access. They are not inherited down attribute trees, and must be set on a branch attribute as well as a leaf to take effect (to make FOO`BAR visual, FOO must be visual too):

```text
  visual (v)        Attribute can be seen by anyone via examine, get(), eval(), ufun(), zfun(), and similar functions.
  public (p)        This attribute can be evaluated by any object, even if safer_ufun is in use. DANGEROUS! AVOID!

```

These attribute flags alter the way attributes are used in commands and ^-listens. They always only affect the attribute they're set on, regardless of attribute trees:

```text
  debug (b)         Start showing debug output while this attr is evaluated.
  no_debug (B)      Stop showing debug output when this attr is evaluated
  regexp (R)        Match $-commands and ^-listens using regular expressions. See 'help regexps'.
  case (C)          Match $-commands and ^-listens case sensitively.
  nospace (s)       Attribute won't add a space after the object name in @o-* messages. See 'help verbs'.
  noname (N)        Attribute won't show name in @o-* messages.

```

Continued in 'help attribute flags3'
