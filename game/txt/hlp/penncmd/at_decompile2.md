---
topics: ["@decompile2"]
---

@decompile takes the following switches, which can be combined:

@decompile/name
```text
    This switch causes @decompile to use the object's name, instead of its dbref. This is the default.
```

@decompile/db
```text
    This switch makes @decompile use the object's dbref instead of its name, which is useful for editing code off-MUSH.
```

@decompile/flags
```text
    Only the code to @create the object and set flags/powers/locks is printed. When an <attribute pattern> is given, this switch is ignored, and @decompile only prints the matching attributes.
```

@decompile/attribs
```text
    Only the code to set the object's attributes is printed. Same as @decompile <object>/**
```

@decompile/skipdefaults
```text
    Don't output commands to set attribute flags if those flags are the defaults for that attribute on that MUSH.
```

@decompile/tf
```text
    Explained in 'help @decompile3'.

```

Continued in 'help @decompile3'.
