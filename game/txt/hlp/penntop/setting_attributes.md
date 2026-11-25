---
topics: [SETTING-ATTRIBUTES, SETTING ATTRIBUTES]
---

Standard attributes are set using @<attrib> <obj>=<value>
Nonstandard attributes are set using &<attrib> <obj>=<value>
Attributes may also be set using @set <obj>=<attrib>:<value> or the attrib_set() or set() functions.

Attributes are cleared using @<attrib> <obj> or &<attrib> <obj>, attrib_set(), or with @wipe or wipe().

Note that if the empty_attrs configuration option is set (@config empty_attrs to check), there is a difference between clearing an attribute and setting an attribute to a null value:
```text
    @va me       <--- wipes out my VA attribute
    @va me=      <--- sets my VA attribute to be empty

```

Empty attributes retain their flags and atrlock status. Wiped attributes are gone forever.

**See also:** ATTRIBUTES, NON-STANDARD ATTRIBUTES, @set, @wipe, attrib_set(), set(), wipe(), ATTRIBUTE FLAGS
