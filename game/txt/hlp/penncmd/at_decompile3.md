---
topics: ["@decompile3"]
---

@decompile/tf <object>[/<attribute>]

The /tf works the same as if you'd typed:
```text
    @decompile/db <obj>[/<attrs>]=[default(me/TFPREFIX, FugueEdit >%b)]

```

with the exception that @decompile/tf does not include commands for setting attribute flags. If you have a TFPREFIX attribute set, the (unevaluated) contents of that attribute is used as the prefix. Otherwise, the string "FugueEdit > " is used. It's useful for automatically copying @decompile output into your client to alter. It is highly recommended that you set a TFPREFIX attribute, to prevent others from maliciously placing code in your client's command line.

To set up @decompile/tf:

```text
    In TinyFugue:
      /def -ag -mglob -p100 -t"FugueEdit > *" fe = /grab %-2

    In SimpleMU:
      Set your Options -> Grab Password
      @set me=tfprefix:<grabpassword>FugueEdit >%b

```

**See also:** CLIENTS, ATTRIBUTES, WILDCARDS, MUSHCODE
