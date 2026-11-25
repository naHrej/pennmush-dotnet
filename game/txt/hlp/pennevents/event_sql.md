---
topics: [EVENT SQL]
---

sql`connect (platform)
```text
      Triggered on successful connect to the SQL database. <platform> is 'mysql', 'postgresql' or 'sqlite3'.

```

sql`connectfail (platform, error message)
```text
      Triggered on unsuccessful connect to the SQL database.

```

sql`disconnect (platform, error message)
```text
      Triggered if SQL disconnects for any reason. Usually not a worry since Penn will auto-reconnect if it can.
```
