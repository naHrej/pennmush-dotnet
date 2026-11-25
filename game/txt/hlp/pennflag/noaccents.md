---
topics: [NOACCENTS]
---

Flag:  NOACCENTS  (players)

This flag causes all accented characters to be converted to non-accented alternatives before being sent to a connection. See HELP STRIPACCENTS() for caveats.

To strip accents for a specific connection, instead of all connections, you can use
```text
    @sockset <descriptor>=stripaccents,yes

```

**See also:** i18n, accent(), stripaccents(), @sockset
