---
topics: [NON-STANDARD ATTRIBUTES]
---

While there are many standard attributes in MUSH, objects can also have an enormous number of attributes, with any name you wish to use. In the past, you were limited to attributes named VA-VZ, WA-WZ, XA-XZ; these are still available as standard attributes. However, it is strongly recommended that you use non-standard attributes and meaningful names in order to make maintaining your MUSHCode easier.

To set a non-standard attribute, you can use these formats:
```text
      &<attribute name> <obj>=<value>  OR
      @_<attribute_name> <obj>=<value> OR
      @set <obj>=<attribute_name>:<value>

```

You can get the value of attributes using the functions v(), get(), and xget(). You can evaluate attributes using u(), eval(), and get_eval(). All attributes can be used in attribute locks and can be 'owned' independent of object ownership.

**See also:** ATTRIBUTES, ATTRIB-OWNERSHIP, Attribute Functions, ATTRIBUTE TREES, ATTRIBUTE FLAGS
