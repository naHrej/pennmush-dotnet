---
topics: ["@RESPOND3"]
---

To vaguely comply with most HTTP requirements:

@respond <code> <text>
```text
      <code> must be 3 digits, followed by a space, then printable ascii text.
             Total length must be < 40 characters.
             This will be prepended by HTTP/1.1 when sent back to the client.

```

@respond/header <name>: <value>
```text
      <name> must be printable ascii characters (No accents, no %r)
      <value> must be printable, but accents allowed (No %r)

```

@respond/type <ctype>
```text
      <ctype> should be alphanumeric, +, ., /, -. HTTP/1.1 does allow for parameters (text/plain; content-encoding=...), so we don't enforce anything at present except printability().
```
