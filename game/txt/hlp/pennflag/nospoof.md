---
topics: [NOSPOOF, PARANOID]
---

Flag:  NOSPOOF  (all types)
Flag:  PARANOID  (all types)

If an object is set NOSPOOF, messages of all kinds will be tagged with the name of the person/object causing them. This prevents spoofing and lets you see where such messages originated. When a NOSPOOF object is also set PARANOID, the dbref of the emitting object is also shown. You will not see nospoof information for your own messages unless you are also set PARANOID.

When a player is set NOSPOOF or PARANOID, everything it owns is also treated as having the flag.

Note that NOSPOOF output can be spammy. Nospoof information is not matched against @listen or ^-listen patterns.

The nospoof tag is [Source:], and the paranoid tag is [Source(#dbref)].

**See also:** SPOOFING, @emit, @pemit, @remit, @oemit, @nspemit
