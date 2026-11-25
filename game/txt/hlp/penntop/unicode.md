---
topics: [UNICODE]
---

At the moment, PennMUSH has very minimal support for Unicode. Almost all text is treated internally as being in the Latin-1 character set. Clients that support telnet character set negotiation can send and receive UTF-8, but only characters in the Basic Latin and Latin-1 Supplement blocks are accepted (Others are replaced with question marks).

A few functions support Unicode-aware text transformations:

```text
    stripaccents()

```

When the MUSH is compiled with the ICU library (See @config compile), additional functions support Unicode-aware text transformations:

```text
    lcstr2()     ucstr2()

```

When using these functions, do not assume that they return the same number of characters as their argument.
