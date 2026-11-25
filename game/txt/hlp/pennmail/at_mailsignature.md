---
topics: ["@mailsignature", mailsignature]
---

@mailsignature <object>[=<signature>]

When set, this attribute is evaluated and appended to any @mail messages sent by <object>, unless the @mail/nosig command is used.

Example:
> @mailsignature me=%r%r-- %n%r%r[u(funny_quote)]

**See also:** @mail, mail-sending
