---
topics: [lockkeys, lock keys]
---

There are many key types, and it is also possible to form more complex locks by using boolean symbols and grouping. See HELP @LOCK-COMPLEX for examples.

The types of keys are outlined below. Detailed help for each is available by typing 'help @lock-<key>', replacing <key> with the word on the left.

```text
  Simple     - Always true, always false, or locking to a specific object.
  Name       - Check the name of the object attempting to pass the lock.
  Owner      - Lock to objects owned by the owner of an object.
  Carry      - Lock to someone carrying an object, such as a key.
  Indirect   - Use the result of another @lock.
```

Attribute  - Check an attribute on the object trying to pass the lock.
Evaluation - Evaluate an attribute on the object the lock is on.
```text
  Bit        - Check for a flag, type, power, or channel membership.
```

Dbreflist  - Check if the dbref of the object trying to pass the lock is in a list set in an attribute.
```text
  Host       - Check for players connecting from a particular host/ip.

```

You can negate lock keys, and combine multiple keys, as explained in 'help lockkeys2'.
