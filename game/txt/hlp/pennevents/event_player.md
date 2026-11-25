---
topics: [EVENT PLAYER]
---

player`create (objid, name, how, descriptor, email)
```text
      Triggered when a player is created. If the player was @pcreated, then %# will be the person who did the @pcreate. If player was created by using 'create' at the connect screen, then %# will be #-1 and <descriptor> will be non-null. <how> is one of: "pcreate", "create" or "register". If created using 'register', <email> will be set appropriately.

```

player`connect (objid, number of connections, descriptor)
```text
      Similar to @aconnect, but for events, and so you can use descriptor.

```

player`disconnect (objid, number of remaining connections, hidden?, cause of disconnection, ip, descriptor, conn() secs, idle() secs, recv bytes/sent bytes/command count)
```text
      Similar to @adisconnect, but for event system, and with more information available.
```

player`inactivity: Triggered when idle players are disconnected. Only run if at least one player gets idlebooted (Or auto-hidden), not at every inactivity check.
