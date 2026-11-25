---
topics: [EVENT LOG]
---

Events in the log tree get triggered whenever the game logs any information to a log file (Either because of @log, or something else happening.) They all get passed a single argument, the message being logged.

log`err
```text
     Errors and the general catch-all.
```

log`cmd
```text
     Logged commands.
```

log`wiz
```text
     Logged wizard activity.
```

log`conn
```text
     Connection notifications.
```

log`trace
```text
     Memory tracking notifications.
```

log`check
```text
     Save-releated log messages.
```

log`huh
```text
     Commands that generate huh messages.
```
